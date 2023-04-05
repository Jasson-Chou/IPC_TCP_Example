using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IPCService
{
    internal class TCPSpecIni
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileInt(string Section, string KeyName, int Default, string FileName);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileInt(string Section, string KeyName, int value, string FileName);
        
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string Section, string KeyName, string Default, StringBuilder Val, int Size, string FileName);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string Section, string KeyName, string String, string FileName);
        
        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                if(!File.Exists(value))
                {
                    FileNotFoundHandler(value);
                }

                _fileName = value;
                RefreshFileHandler();
            }
        }
        private const string INISectionName = "SPEC";
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public int BufferBytes { get; private set; }
        public int ReceiveTimeout { get; private set; }

        //private FileSystemWatcher fileSystemWatcher { get; }

        public TCPSpecIni(string fileName)
        {
            this.FileName = fileName;
        }

        private void RefreshFileHandler()
        {
            IPAddress = ReadString(_fileName, INISectionName, nameof(IPAddress));
            Port = ReadInt(_fileName, INISectionName, nameof(Port));
            BufferBytes = ReadInt(_fileName, INISectionName, nameof(BufferBytes));
            ReceiveTimeout = ReadInt(_fileName, INISectionName, nameof(ReceiveTimeout));
#if DEBUG
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine("INI File Content:");
            sb.AppendLine($"{nameof(IPAddress)} = {IPAddress}");
            sb.AppendLine($"{nameof(Port)} = {Port}");
            sb.AppendLine($"{nameof(BufferBytes)} = {BufferBytes}");
            sb.AppendLine($"{nameof(ReceiveTimeout)} = {ReceiveTimeout}");
            Debug.WriteLine(sb.ToString());
#endif

        }

        private void FileNotFoundHandler(string fileName)
        {
            WriteString(fileName, INISectionName, nameof(IPAddress), "127.0.0.1");
            WriteInt(fileName, INISectionName, nameof(Port), 4096);
            WriteInt(fileName, INISectionName, nameof(BufferBytes), 128 * 1024);
            WriteInt(fileName, INISectionName, nameof(ReceiveTimeout), 5000);
        }

        private void WriteInt(string fileName, string Section, string key, int value)
        {
            //WritePrivateProfileInt(Section, key, value, fileName);
            WriteString(fileName, Section, key, value.ToString());
        }

        private void WriteString(string fileName, string Section, string key, string value)
        {
            WritePrivateProfileString(Section, key, value, fileName);
        }

        private int ReadInt(string fileName, string Section, string key)
        {
            var val_str = ReadString(fileName, Section, key);
            var _try = int.TryParse(val_str, out int result);
            if (_try) return result;
            else return int.MinValue;
        }

        private string ReadString(string fileName, string Section, string Key)
        {
            StringBuilder sb = new StringBuilder(512);
            GetPrivateProfileString(Section, Key, "Error", sb, 512, fileName);
            return sb.ToString();
        }
        

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine($"INI File Name at {FileName}");
            sb.AppendLine("INI File Content:");
            sb.AppendLine($"{nameof(IPAddress)} = {IPAddress}");
            sb.AppendLine($"{nameof(Port)} = {Port}");
            sb.AppendLine($"{nameof(BufferBytes)} = {BufferBytes}");
            sb.AppendLine($"{nameof(ReceiveTimeout)} = {ReceiveTimeout} ms");
            return sb.ToString();
        }
    }
}
