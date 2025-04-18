﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Linq
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal class DefaultCosmosLinqSerializer : ICosmosLinqSerializerInternal
    {
        private readonly CosmosPropertyNamingPolicy PropertyNamingPolicy;

        public DefaultCosmosLinqSerializer(CosmosPropertyNamingPolicy propertyNamingPolicy)
        {
            this.PropertyNamingPolicy = propertyNamingPolicy;
        }

        public bool RequiresCustomSerialization(MemberExpression memberExpression, Type memberType)
        {
            // There are two ways to specify a custom attribute
            // 1- by specifying the JsonConverterAttribute on a Class/Enum
            //      [JsonConverter(typeof(StringEnumConverter))]
            //      Enum MyEnum
            //      {
            //           ...
            //      }
            //
            // 2- by specifying the JsonConverterAttribute on a property
            //      class MyClass
            //      {
            //           [JsonConverter(typeof(StringEnumConverter))]
            //           public MyEnum MyEnum;
            //      }
            //
            // Newtonsoft gives high precedence to the attribute specified
            // on a property over on a type (class/enum)
            // so we check both attributes and apply the same precedence rules
            // JsonConverterAttribute doesn't allow duplicates so it's safe to
            // use FirstOrDefault()
            CustomAttributeData memberAttribute = memberExpression.Member.CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(Newtonsoft.Json.JsonConverterAttribute));
            CustomAttributeData typeAttribute = memberType.GetsCustomAttributes().FirstOrDefault(ca => ca.AttributeType == typeof(Newtonsoft.Json.JsonConverterAttribute));

            return memberAttribute != null || typeAttribute != null;
        }

        public string Serialize(object value, MemberExpression memberExpression, Type memberType)
        {
            CustomAttributeData memberAttribute = memberExpression.Member.CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(Newtonsoft.Json.JsonConverterAttribute));
            CustomAttributeData typeAttribute = memberType.GetsCustomAttributes().FirstOrDefault(ca => ca.AttributeType == typeof(Newtonsoft.Json.JsonConverterAttribute));
            CustomAttributeData converterAttribute = memberAttribute ?? typeAttribute;

            Debug.Assert(converterAttribute.ConstructorArguments.Count > 0, $"{nameof(DefaultCosmosLinqSerializer)} Assert!", "At least one constructor argument exists.");
            Type converterType = (Type)converterAttribute.ConstructorArguments[0].Value;

            string serializedValue = converterType.GetConstructor(Type.EmptyTypes) != null
                ? JsonConvert.SerializeObject(value, (Newtonsoft.Json.JsonConverter)Activator.CreateInstance(converterType))
                : JsonConvert.SerializeObject(value);

            return serializedValue;
        }

        public string SerializeScalarExpression(ConstantExpression inputExpression)
        {
            JsonSerializerSettings settings = this.PropertyNamingPolicy == CosmosPropertyNamingPolicy.CamelCase
                ? new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
                : new JsonSerializerSettings();

            return JsonConvert.SerializeObject(inputExpression.Value, settings);
        }

        public string SerializeMemberName(MemberInfo memberInfo)
        {
            string memberName = memberInfo.Name;

            // Check if Newtonsoft JsonExtensionDataAttribute is present on the member, if so, return empty member name.
            Newtonsoft.Json.JsonExtensionDataAttribute jsonExtensionDataAttribute = memberInfo.GetCustomAttribute<Newtonsoft.Json.JsonExtensionDataAttribute>(true);
            if (jsonExtensionDataAttribute != null && jsonExtensionDataAttribute.ReadData)
            {
                return null;
            }

            // Json.Net honors JsonPropertyAttribute more than DataMemberAttribute
            // So we check for JsonPropertyAttribute first.
            JsonPropertyAttribute jsonPropertyAttribute = memberInfo.GetCustomAttribute<JsonPropertyAttribute>(true);
            if (jsonPropertyAttribute != null && !string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
            {
                memberName = jsonPropertyAttribute.PropertyName;
            }
            else
            {
                DataContractAttribute dataContractAttribute = memberInfo.DeclaringType.GetCustomAttribute<DataContractAttribute>(true);
                if (dataContractAttribute != null)
                {
                    DataMemberAttribute dataMemberAttribute = memberInfo.GetCustomAttribute<DataMemberAttribute>(true);
                    if (dataMemberAttribute != null && !string.IsNullOrEmpty(dataMemberAttribute.Name))
                    {
                        memberName = dataMemberAttribute.Name;
                    }
                }
            }

            memberName = CosmosSerializationUtil.GetStringWithPropertyNamingPolicy(this.PropertyNamingPolicy, memberName);

            return memberName;
        }
    }
}
