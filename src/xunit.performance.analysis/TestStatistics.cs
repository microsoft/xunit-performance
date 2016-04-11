using System;
using System.Collections.Generic;
#if !LINUX_BUILD
using MathNet.Numerics.Statistics;
#endif

namespace Microsoft.Xunit.Performance.Analysis
{
    public class TestStatistics
    {
        private bool _readOnly;
        private List<double> _values;
        private RunningStatistics _cachedStatistics;

        public TestStatistics()
        {
            _values = new List<double>();
        }

        public void Push(double value)
        {
            if (_readOnly)
                throw new InvalidOperationException("Statistics have already been calculated.  Data is now read-only.");
            _values.Add(value);
        }

        public RunningStatistics RunningStatistics
        {
            get
            {
                if(_cachedStatistics == null)
                {
                    _readOnly = true;
                    TransformData();
                    _cachedStatistics = new RunningStatistics(_values);
                }

                return _cachedStatistics;
            }
        }

        public int IterationCount
        {
            get { return _values.Count; }
        }

        private void TransformData()
        {
            double[] values = _values.ToArray();

            // Sort the values.
            Array.Sort(values);

            // Get the 1st and 99th percentile values.
            double firstPercentileVal = PercentileInSortedArray(values, 1);
            double ninetyninthPercentileVal = PercentileInSortedArray(values, 99);

            int startIndex = 0;
            int stopIndex = values.Length - 1;
            
            // Find the new start-index.
            for(int i=0; i<values.Length; i++)
            {
                if(values[i] > firstPercentileVal)
                {
                    startIndex = i;
                    break;
                }
            }

            // Find the new end-index.
            for(int i=values.Length-1; i>=0; i--)
            {
                if(values[i] < ninetyninthPercentileVal)
                {
                    stopIndex = i;
                    break;
                }
            }

            // Subset the array.
            // Add 1 because we want to include stopIndex in the new data set.
            int newArrayLength = stopIndex - startIndex + 1;
            double[] newValues = new double[newArrayLength];
            int newArrayIndex = 0;
            for(int i=startIndex; i<=stopIndex; i++)
            {
                newValues[newArrayIndex++] = values[i];
            }

            // Swap in the updated set of values.
            _values = new List<double>(newValues);
        }

        private static double PercentileInSortedArray(double[] values, int percentage)
        {
            if(values == null || values.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            if(percentage < 0 || percentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage));
            }

            // Calculate the array slot where the nth percentile lives.
            int n = (percentage / 100) * (values.Length-1);

            return values[n];
        }
    }

#if LINUX_BUILD
    // MathNet nuget package can't be used on Linux,
    // but we don't need most of its functionality anyway.
    public class RunningStatistics
    {
        public RunningStatistics()
        {
            values = new List<Double>();
        }

        public RunningStatistics(IEnumerable<double> values)
        {
            values = new List<double>(values);
        }

        public List<Double> values;
        public void Push(Double value)
        {
            values.Add(value);
        }
    }
#endif
}
