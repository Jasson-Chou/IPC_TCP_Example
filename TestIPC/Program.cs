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
            client.OnIPCRead += Client_OnIPCRead;
            client.OnIPCReceiveSocketError += Client_OnIPCReceiveSocketError;
            client.Open();
            Thread.Sleep(1000); //waiting for server listen accept.
            client.Write(new byte[1] { 0xFF });

            string arg = string.Empty;
            do
            {
                arg = Console.ReadLine();
                var values = ParseArgument(arg);
                if (values is null && !arg.ToLower().Contains("exit"))
                {
                    Console.WriteLine("Input Error");
                }
                else
                {
                    client.Write(values.ToArray());
                }


            } while (!arg.ToLower().Contains("exit"));
                

            client.Close();
            ipcServer.Close();
        }

        static IReadOnlyList<byte> ParseArgument(string arg)
        {
            var buffer = new List<byte>();
            var args = arg.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                foreach (var para in args)
                {
                    var value = para.Trim();
                    if (value.StartsWith("0x"))
                    {
                        var byteValue = Convert.ToByte(value.Remove(0, 2), 16);
                        buffer.Add(byteValue);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
            
            return buffer.Count == 0 ? null : buffer;
        }

        private static void IpcServer_OnIPCReceiveSocketError(System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"Server Error:{socketError.ToString()}");
        }

        private static void IpcServer_OnIPCRead(byte[] data, IClientPackage clientPackage)
        {
            Console.WriteLine($"Server Receive: {string.Join(",", data.Select(val => val.ToString()))}");
            var result = clientPackage.Write(new byte[1] { 0x11 });
            if (!result) Console.WriteLine($"Client Write Fail!");
        }

        private static void Client_OnIPCReceiveSocketError(System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"Client Error:{socketError.ToString()}");
        }

        private static void Client_OnIPCRead(byte[] data)
        {
            Console.WriteLine($"Client Receive: {string.Join(",", data.Select(val => val.ToString()))}");
        }
    }
}
