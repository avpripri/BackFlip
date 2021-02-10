using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BackFlip
{
    public class RunningAverage
    {
        private float[] values;
        private float runningSum;
        private int _length;
        private int idx;
        private int cnt;
        private int cntResum = 1024;

        public RunningAverage(int length)
        {
            values = new float[_length = length];
            Array.Clear(values, 0, length);
            cnt = length - 1;
            runningSum = 0;
        }

        public float Push(float value)
        {
            if (cnt > 0)
                cnt--;

            if (cntResum > 0)
            {
                cntResum--;
                runningSum += (value - values[idx]);
            }
            else
            {
                // float addition has rounding errors
                cntResum = 1024;
                runningSum = values.Sum();
            }

            idx = (idx + 1) % values.Length;
            values[idx] = value;

            return Average();
        }

        public float Average()
        {
            return (cnt > 0) ? 0f : values.Average();
        }
    }


    /// <summary>
    /// Tracks a TimedAverageDelta
    /// Useful for determing deltas on a time inverval
    /// </summary>
    public class TimedAverageDelta
    {
        private Tuple<DateTime, float>[] values;
        private TimeSpan spanAverage;
        private float runningSum;
        private int _length;
        private int idx;
        private int cnt;
        private int cntResum = 1024;

        public TimedAverageDelta(int length, TimeSpan timeSpanToAverage)
        {
            spanAverage = timeSpanToAverage;
            values = new Tuple<DateTime, float>[_length = length];
            Array.Clear(values, 0, length);
            cnt = length - 1;
            runningSum = 0;
        }

        public float Push(float value, DateTime now)
        {
            var newItem = new Tuple<DateTime, float>(now, value);
            if (cnt >= 0)
            {
                cnt--;               
                values[idx] = newItem;
                idx = (idx + 1) % values.Length;
                return 0;
            }

            Tuple<DateTime,float> oldVal;
            var nextIdx = (idx + 1) % values.Length;
            oldVal = values[nextIdx];
            values[idx = nextIdx] = newItem;

            return (float)((oldVal.Item2 - value) / (now - oldVal.Item1).TotalSeconds);
        }
    }
}
