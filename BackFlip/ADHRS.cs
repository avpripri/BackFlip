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
        static SerialPort _serialPort = null;

        public bool IsOpen { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }

        public ADHRS(string comPort, int baudRate = 9600)
        {
            IsOpen = false;
            ComPort = comPort; 
            BaudRate = baudRate;
        }

        private void OpenPort()
        {
            IsOpen = false;
            if (null != _serialPort)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }

            _serialPort = new SerialPort(ComPort, BaudRate);

            try
            {
                _serialPort.Open();
                IsOpen = _serialPort.IsOpen;
            }
            catch (System.IO.IOException) { }
            catch (System.UnauthorizedAccessException) { }
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
            if (!IsOpen)
                OpenPort();

            try
            {
                while (IsOpen && _serialPort.BytesToRead > 0)
                {
                    try
                    {
                        var lin = _serialPort.ReadLine();
                        var ahrsLine = lin.Split(',').Skip(1).Select(v => v.Split(':')).ToDictionary(k => k[0][0], v => float.Parse(v[1]));

                        if (ahrsLine.Count() >= 9)
                        {
                            return ahrsLine;
                        }
                    }
                    catch { break; }
                }
            }
            catch (System.UnauthorizedAccessException) { IsOpen = false; }
            catch (System.InvalidOperationException) { IsOpen = false; }


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
