using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackFlip
{
    public class ADHRS : IDisposable
    {
        static SerialPort _serialPort;

        public bool IsOpen { get { return _serialPort.IsOpen; } }

        public ADHRS(string comPort, int baudRate = 9600)
        {
            _serialPort = new SerialPort(comPort, baudRate);

            try { _serialPort.Open(); } catch (System.IO.IOException) { }
        }

        public const char Flags = 'F';
        public const char Roll = 'R';
        public const char Pitch = 'P';
        public const char Heading = 'H';
        public const char Gs = 'G';
        public const char Yaw = 'Y';
        public const char Baro = 'B';
        public const char Temp = 'T';
        public const char IAS = 'A';

        public Dictionary<char, float> RawRead()
        {
            while (IsOpen && _serialPort.BytesToRead > 0)
            {
                try
                {
                    var lin = _serialPort.ReadLine();
                    var ahrsLine = lin.Split(',').Skip(1).Select(v => v.Split(':')).ToDictionary(k => k[0][0], v => float.Parse(v[1]));

                    if (ahrsLine.Count() >= 8)
                    {
                        return ahrsLine;
                    }
                }
                catch { break; }
            }

            return new Dictionary<char, float>();
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public static int readAltitude(float pressure, float seaLevelhPa)
        {
            return (int)(/* meters 44330 */145439.6f * (1.0f - Math.Pow(pressure / seaLevelhPa, 0.1903f)));
        }
    }
}
