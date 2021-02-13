using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackFlip
{
    public class Instruments
    {
        public static int localBaro = 3004;
        public static float seaLevelMp; // stdPres = 1013.25f
        public static float mbOffset = 0f;
        public static double totalEnergyVV = 0; // total energy vertical velocity equivilent in m/s
        public static float alphaCal = 0f;

        public string airspeed = "0";
        public string vsi = "0";
        public string vsi30 = "30s 0";

        public string heading = "352";
        public int altitude = 0;
        public float roll = 0f;
        public float pitch = 0f;

        SimpleKalmanFilter vsiFilter = new SimpleKalmanFilter()
        {
            Q = 0.000001, // this seems to really smooth it out nicely
            R = 0.01
        };

        double totalEnergyLast = 0;
        int displayUpdateCounter = 30;

        float dp_Coef = 4.91744f; // <-- calibrate this, currently for m/s
        float AIS_Baseline = 2178;
        // House altitude 892.7 '
        const double coefOfPressChange = 0.19029495718363463368220742150333d;  /* 1 / 5.25 */

        Queue<float> baroHist = new Queue<float>();     // Used to calculate baro average
        float runningMeanVertVelocity;
        float meanVerticalVelocity;
        int sampleTick = 0;

        double speedLast;
        double baro2XLast;

        // Common unit conversion factors
        // NOTE: All base calculations are SI units (m, m/s, mb).  When displayed, they are converted to pilot prefered units
        const float mph2mps = 0.44704f;
        const float mps2kts = 1.943844f;
        const float mps2fpm = 196.85039370078738f;
        private const int averagingSampleCount = (30 * 30); // typically we're around 30fps, and I want 30 second average.


        DateTime lastVsiUpdate = DateTime.Now;
        const float dtVsiUpdate = 0.2f;

        TimedAverageDelta tdiVsi30 = new TimedAverageDelta((int)(30 * 6 / dtVsiUpdate), TimeSpan.FromSeconds(30));

        public void Draw(RenderTarget renderer, SolidColorBrush brush)
        {
            var ClientRectangle = Stock.ClientRectangle;

            renderer.DrawText(heading, Stock.TextFormatCenter, new RectangleF(0, 0, ClientRectangle.Width, 100), brush);
            renderer.DrawText((altitude / 100).ToString(), Stock.TextFormatRight, new RectangleF(0, 5, ClientRectangle.Width - 50, ClientRectangle.Height), brush);
            renderer.DrawText((altitude % 100).ToString().PadLeft(2, '0'), Stock.TextFormatRightSmall, ClientRectangle, brush);

            renderer.DrawText((localBaro / 100f).ToString("0.00"), Stock.TextFormatRightSmall, new RectangleF(0, ClientRectangle.Height - 200, ClientRectangle.Width, 200), brush);

            renderer.DrawText(airspeed, Stock.TextFormatLeft, Stock.ClientRectangle, brush);

            renderer.DrawText(vsi, Stock.TextFormatRight, new RectangleF(0, 10, ClientRectangle.Width - 15, 150), brush);
            renderer.DrawText(vsi30, Stock.TextFormatRightSmall, new RectangleF(0, 10, ClientRectangle.Width, 50), brush);
        }

        public void UpdateBaro(int deltaInchHg_100 = 0)
        {
            localBaro += deltaInchHg_100;
            seaLevelMp = localBaro * 1017.25f / 2992f;
        }

        private void CalculateFromPressures(float pitotPress, float staticPress)
        {
            var speedMps = Math.Sqrt(dp_Coef * Math.Max(0, pitotPress - AIS_Baseline));
            var speedKts = speedMps * mps2kts;

            // Compute the airspeed
            airspeed = ((int)(speedKts < 30d ? 0 : speedKts)).ToString();

            var baro2X = Math.Pow((double)((staticPress - mbOffset) / seaLevelMp), coefOfPressChange);

            var now = DateTime.Now;
            var tDelta = now - lastVsiUpdate;
            if (tDelta.Ticks == 0)
                tDelta = new TimeSpan(200);
            var dT = Math.Min(0.3d, Math.Max(-0.3d, tDelta.TotalMilliseconds / 1000.0d));
            lastVsiUpdate = now;

            // initial state
            if (baro2XLast == 0)
                baro2XLast = baro2X;
            if (speedLast == 0)
                speedLast = speedMps;


            var kinetticEnergy = speedMps * speedMps / 19.8d; // must be signed to work... lossing velocity needs to drop total energy
            var potentialEnergy = 44330d * (1.0d - baro2X);

            var totalEnergy = potentialEnergy + kinetticEnergy;
            if (totalEnergyLast == 0)
                totalEnergyLast = totalEnergy;

            // Altituded change from pressure difference derivation
            // --- Given
            // k = 44330
            // x = 1 / 5.25
            // p0 = 1013.25
            // --- stubtracting two pressures equations yields;
            // [k * (1 - (p2 / p0) ^ x)] - [k * (1 - (p1 / p0) ^ x)]
            // -- Then
            // k * [(1 - (p2 / p0) ^ x) - (1 - (p1 / p0) ^ x)]
            // k * (1 - (p2 / p0) ^ x - 1 + (p1 / p0) ^ x)
            // k * ((p1 / p0) ^ x - (p2 / p0) ^ x)  => p1^x / p0^x
            // k * (p1 ^ x - p2 ^ x) / p0 ^ x
            // --- QED
            // k / p0 ^ x * (p1 ^ x - p2 ^ x), or
            // 11862.610784520926279471081940874 * (p1 ^ x - p2 ^ x)
            const double k_over_p02x = 11862.610784520926279471081940874;

            // Convert the baro pressure then add the kinnetic energy factor to generate a total energy
            totalEnergyVV = vsiFilter.Update(2d * (totalEnergy - totalEnergyLast) / dT);

            var vv30 = (int)(3d * k_over_p02x * tdiVsi30.Push((float)baro2X, now) * mps2fpm);
            var vsiT = (int)(totalEnergyVV * mps2fpm);

            if (displayUpdateCounter-- == 0)
            {
                displayUpdateCounter = 10;
                vsi = (20 * (int)Math.Round(vsiT / 20d)).ToString();
                vsi30 = "30s " + (10 * (int)Math.Round(vv30 / 10d)).ToString();
                altitude = (int)(/* meters=> 44330*/145439.6d * (1.0 - baro2X));
            }

            baro2XLast = baro2X;
            speedLast = speedMps;
            totalEnergyLast = totalEnergy;
        }

        public void SetFromAhrs(Dictionary<char, float> attitude)
        {
            roll = attitude[ADHRS.Roll];
            heading = (5 * ((int)attitude[ADHRS.Heading] / 5)).ToString();
            pitch = -10 * attitude[ADHRS.Pitch];
            CalculateFromPressures(attitude[ADHRS.IAS], attitude[ADHRS.Baro]);
        }

        public static void Configure(Dictionary<string, string> config)
        {
            localBaro = int.Parse(config["localBaro"]);
            mbOffset = float.Parse(config["mbOffset"]);
            alphaCal = float.Parse(config["alphaCal"]);
        }
    }
}
