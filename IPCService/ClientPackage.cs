using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCService
{
    public interface IClientPackage
    {
        bool IsConnect { get; }
        bool Write(byte[] data);
    }
    internal class ClientPackage : IClientPackage
    {
        private System.Threading.CancellationTokenSource ReadListenerHandleCancelTokenSource;
        internal System.Threading.CancellationToken ReadListenerHandleCancelToken;
        private Task ListenerHandlerTask;
        internal Socket Socket { get; }
        internal byte[] Buffer { get; }
        public bool IsConnect { get; internal set; }
        internal ClientPackage(Socket socket, int BufferSize, Action<ClientPackage> listenerHandler)
        {
            Socket = socket;
            
            Buffer = new byte[BufferSize];
            ReadListenerHandleCancelTokenSource = new System.Threading.CancellationTokenSource();
            ReadListenerHandleCancelToken = ReadListenerHandleCancelTokenSource.Token;
            ListenerHandlerTask = new Task(() => listenerHandler(this), ReadListenerHandleCancelToken);
            IsConnect = true;
        }

        internal void Start()
        {
            ListenerHandlerTask.Start();
        }

        internal void Close()
        {
            ReadListenerHandleCancelTokenSource?.Cancel();
            ReadListenerHandleCancelTokenSource?.Dispose();
            ListenerHandlerTask?.Dispose();
            Socket?.Close();
            Socket?.Dispose();
            IsConnect = false;
        }

        public bool Write(byte[] data)
        {
            TCPSpec.Instance.TransferSpecConvert(data, out byte[] buffer);
            Socket socket = this.Socket;
            try
            {
                socket.Send(buffer);
            }
            catch (ArgumentNullException ane)
            {
#if DEBUG
                Debug.WriteLine(ane.Message);
#endif
                return false;
            }
            catch (SocketException se)
            {
#if DEBUG
                Debug.WriteLine(se.Message);
#endif
                Close();
                return false;
            }
            catch (ObjectDisposedException ode)
            {
#if DEBUG
                Debug.WriteLine(ode.Message);
#endif
                Close();
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(128 * Buffer.Length);
            sb.Append($"Buffer Len [{BitConverter.ToInt32(Buffer, 0)}]");
            sb.Append("Buffer Detail Datas:");
            for (int idx = 0; idx < Buffer.Length; idx++)
            {
                sb.AppendLine($"Buff[{idx}] = {Buffer[idx]}");
            }

            return sb.ToString();
        }
    }
}
