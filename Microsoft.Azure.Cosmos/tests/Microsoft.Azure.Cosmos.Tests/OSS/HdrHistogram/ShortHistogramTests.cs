﻿// This file isn't generated, but this comment is necessary to exclude it from StyleCop analysis.
// <auto-generated/>

using Xunit;

namespace HdrHistogram.UnitTests
{
    
    internal class ShortHistogramTests : HistogramTestBase
    {
        protected override int WordSize => sizeof(short);

        internal override HistogramBase Create(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            //return new ShortHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            return HistogramFactory.With16BitBucketSize()
                .WithValuesUpTo(highestTrackableValue)
                .WithPrecisionOf(numberOfSignificantValueDigits)
                .Create();
        }

        internal override HistogramBase Create(long lowestTrackableValue, long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            //return new ShortHistogram(lowestTrackableValue, highestTrackableValue, numberOfSignificantValueDigits);
            return HistogramFactory.With16BitBucketSize()
                .WithValuesFrom(lowestTrackableValue)
                .WithValuesUpTo(highestTrackableValue)
                .WithPrecisionOf(numberOfSignificantValueDigits)
                .Create();
        }
    }
}