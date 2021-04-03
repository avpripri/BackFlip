using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackFlip
{
    public class ADHRS : IDisposable, IADHRS
    {
        static SerialPort _serialPort = null;

        public bool IsOpen { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }

        public ADHRS(string comPort, int baudRate = 18200)
        {
            IsOpen = false;
            BaudRate = baudRate;
            ComPort = FindCommPort(comPort);
        }

        private bool CheckPort(string commPort)
        {
            using (var port = new SerialPort(commPort, BaudRate))
            {
                try
                {
                    port.Open();
                    Debug.WriteLine($"Timeout = {port.ReadTimeout}");
                    port.ReadTimeout = 500;
                    var line = port.ReadLine();
                    port.Close();
                    return line.StartsWith("F:") && line.Split(',').Count() > 8;
                }
                catch (TimeoutException) { return false; }
            }
        }

        private string FindCommPort(string portIn)
        {
            if (CheckPort(portIn))
                return portIn;

            return SerialPort.GetPortNames().FirstOrDefault(CheckPort);
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
                _serialPort.ReadTimeout = -1;
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
                while (IsOpen && _serialPort.ReadBufferSize > 0)
                {
                    StringBuilder sbT = new StringBuilder(128);
                    try
                    {
                        var start = DateTime.Now;
                        int chrT;
                        do
                        {
                            chrT = _serialPort.ReadChar();
                            sbT.Append((char)chrT);
                        } while (chrT != '\r');
                        sbT.Length = sbT.Length-1; // truncated the \r
                        var lin = sbT.ToString();
                        _serialPort.ReadChar(); // '\n'
                        Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);

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
