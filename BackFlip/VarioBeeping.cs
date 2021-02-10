using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackFlip
{
    public class VarioBeeping
    {
        public bool beepInSink = true;
        public Func<float> GetVerticalVelocityMPS = () => 0;
        public Func<bool> IsRunning = () => true;

        public async void Start()
        {
            await Task.Run(() => BeepVario());
        }

        const int freqMin = 400;
        const int freqMax = 1500;
        const int durationMin = 500;
        const int durationMax = 100;
        const float vvMinDuration = 0.5f;
        const float vvMin = -2.5f;
        const float vvMax = 2.5f;

        const float vvDeadMax = 0.25f;
        const float vvDeadMin = -0.25f;

        private void BeepVario()
        {
            while (IsRunning())
            {
                var vv = GetVerticalVelocityMPS();

                // linear map vv to freq
                var freqTone = (int)Math.Max(freqMin, Math.Min(freqMax, freqMin + (vv - vvMin) * (freqMax - freqMin) / (vvMax - vvMin)));
                var duration = (int)Math.Max(durationMax, Math.Min(durationMin, durationMin + (vv - vvMinDuration) * (durationMax - durationMin) / (vvMax - vvMinDuration)));

                // only beep above zero, by config
                if (!Mute && ((beepInSink && vv < vvDeadMin) || vv > vvDeadMax))
                    Console.Beep(freqTone, duration);

                if (duration > 250)
                    System.Threading.Thread.Sleep(duration);
            }
        }

        public bool Mute { get; set; }

    }
}
