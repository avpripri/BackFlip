using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace BackFlip
{
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }
    public class ADHRSXPlane : IDisposable
    {
        //static SerialPort _serialPort = null;

        public bool IsOpen { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        private string LastMessage = "";

        public ADHRSXPlane(string comPort, int baudRate = 9600)
        {
            IsOpen = false;
            ComPort = comPort;
            BaudRate = baudRate;
            AsynchronousSocketListener.MessageReceived += AsynchronousSocketListener_MessageReceived;
            Thread thread1 = new Thread(AsynchronousSocketListener.StartListening);
            thread1.Start();
        }

        private void AsynchronousSocketListener_MessageReceived(object sender, MessageRecievedEventArgs e)
        {
            LastMessage = e.Message;
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
            var ahrsLine = LastMessage.Split(',').Skip(1).Select(v => v.Split(':')).ToDictionary(k => k[0][0], v => float.Parse(v[1]));

            if (ahrsLine.Count() >= 7)
            {
                return ahrsLine;
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
