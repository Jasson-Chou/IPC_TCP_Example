using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace IPCService
{
    internal sealed class TCPSpec
    {
        
        private readonly string SpecIniFile = AppDomain.CurrentDomain.BaseDirectory + "TPCSpec.ini";

        public int BufferBytes { get; private set; }
        public const int BufferDataLenBytes = sizeof(int);
        public int BufferTotalBytes => BufferBytes + BufferDataLenBytes;

        public int Port { get; private set; }
        public string HostName { get; private set; }
        public System.Net.IPAddress IPCIPAddress { get; private set; }

        public int ReceiveTimeout { get; private set; }

        public static TCPSpec Instance { get; }
        private TCPSpecIni specIni { get; }
        static TCPSpec()
        {
            Instance = new TCPSpec();
        }

        internal TCPSpec()
        {
            specIni = new TCPSpecIni(SpecIniFile);
            RefreshTCPSpec();
        }

        public void RefreshTCPSpec()
        {
            IPCIPAddress = System.Net.IPAddress.Parse(specIni.IPAddress);
            BufferBytes = specIni.BufferBytes;
            Port = specIni.Port;
            ReceiveTimeout = specIni.ReceiveTimeout;
        }

        public void ReceiveSpecConvert(byte[] data, out byte[] buffer)
        {
            int datalen = BitConverter.ToInt32(data, 0);
            buffer = new byte[datalen];
            data.CopyTo(4, buffer, 0, datalen);
        }

        public void TransferSpecConvert(byte[] data, out byte[] buffer)
        {
            int datalen = data.Length;
            buffer = new byte[BufferTotalBytes];
            BitConverter.GetBytes(datalen).CopyTo(buffer, 0, BufferDataLenBytes);
            data.CopyTo(buffer, BufferDataLenBytes, datalen);
        }

        public bool IsTimeout(long ElapsedMilliseconds)
        {
            return ReceiveTimeout <= ElapsedMilliseconds;
        }
    }

    


}
