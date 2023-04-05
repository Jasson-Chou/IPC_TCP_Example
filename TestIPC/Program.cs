using IPCService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestIPC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IPCServer ipcServer = new IPCServer();
            ipcServer.OnIPCRead += IpcServer_OnIPCRead;
            ipcServer.OnIPCReceiveSocketError += IpcServer_OnIPCReceiveSocketError;
            ipcServer.Open();

            Client client= new Client();
            client.Open();
            Thread.Sleep(1000);
            client.Write(new byte[1] { 0xFF });
            Thread.Sleep(1000);

            Console.ReadLine();

            client.Close();
            ipcServer.Close();
        }

        private static void IpcServer_OnIPCReceiveSocketError(System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"Server Error:{socketError.ToString()}");
        }

        private static void IpcServer_OnIPCRead(byte[] data, IClientPackage clientPackage)
        {
            Console.WriteLine($"Server Receive: {string.Join(",", data.Select(val => val.ToString()))}");
        }
    }
}
