using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCService
{
    public delegate void OnIPCServerReadHandler(byte[] data, IClientPackage clientPackage);
    public delegate void OnIPCServerSocketError(SocketError socketError);
    public class IPCServer : IDisposable
    {
        private TcpListener tcpListener;
        private Task AcceptSocketHandlerTask;
        public bool IsOpen { get; private set; }
        private List<ClientPackage> ClientPackages;
        public event OnIPCServerReadHandler OnIPCRead = null;
        public event OnIPCServerSocketError OnIPCReceiveSocketError = null;
        public IPCServer()
        {
            IsOpen = false;
            ClientPackages = new List<ClientPackage>(8);

            tcpListener = new TcpListener(TCPSpec.Instance.IPCIPAddress, TCPSpec.Instance.Port);

            AcceptSocketHandlerTask = new Task(AcceptSocketHandler);
        }

        public void Open()
        {
            IsOpen = true;
            tcpListener.Start();
            AcceptSocketHandlerTask.Start();
        }

        public bool Write(byte[] data, IClientPackage package)
        {
            TCPSpec.Instance.TransferSpecConvert(data, out byte[] buffer);
            return package.Write(buffer);
        }

        private void AcceptSocketHandler()
        {
            do
            {
                Socket accpectSocket = null;
                try
                {
                    accpectSocket = tcpListener.AcceptSocket();
                }
                catch (InvalidOperationException ioe)
                {
#if DEBUG
                    Debug.WriteLine(ioe.Message);
#endif
                    return;
                }
                ClientPackage clientPackage = new ClientPackage(accpectSocket, TCPSpec.Instance.BufferTotalBytes, ReadListenerHandler);
                clientPackage.Start();

            } while (IsOpen);
        }

        private void ReadListenerHandler(ClientPackage clientPackage)
        {
            Stopwatch timeoutWatch = new Stopwatch();
            bool isReading = false;
            Socket socket = clientPackage.Socket;
            int totalReceiveCnt = 0;

            Action ReceiveDoneHandler = () =>
            {
                timeoutWatch.Stop();
                totalReceiveCnt = 0;
                clientPackage.Buffer.Clear();
                isReading = false;
            };

            Action ReceivedHandler = () =>
            {
                TCPSpec.Instance.ReceiveSpecConvert(clientPackage.Buffer, out byte[] data);
                OnIPCRead?.Invoke(data, clientPackage);
            };

            Action ReceiveTimeoutHandler = () =>
            {
                OnIPCReceiveSocketError?.Invoke(SocketError.TimedOut);
            };
            
            try
            {
                do
                {

                    if (socket.Poll(100, SelectMode.SelectRead))
                    {
                        if(!isReading)
                        {
                            isReading = true;
                            timeoutWatch.Restart();
                        }

                        int receiveCnt = socket.Receive(clientPackage.Buffer, totalReceiveCnt, TCPSpec.Instance.BufferTotalBytes, SocketFlags.None);
                        totalReceiveCnt += receiveCnt;
                        if(totalReceiveCnt == TCPSpec.Instance.BufferTotalBytes)
                        {
                            ReceivedHandler();
                            ReceiveDoneHandler();
                        }
                        else if(TCPSpec.Instance.IsTimeout(timeoutWatch.ElapsedMilliseconds))
                        {
#if DEBUG
                            Debug.WriteLine($"IPC Server Socket Receive Timeout");
#endif
                            ReceiveTimeoutHandler();
                            ReceiveDoneHandler();
                        }
                    }
                    else if (clientPackage.ReadListenerHandleCancelToken.IsCancellationRequested)
                    {
                        clientPackage.ReadListenerHandleCancelToken.ThrowIfCancellationRequested();
                        break;
                    }
                } while (IsOpen);
            }
            catch(SocketException se)
            {
                OnIPCReceiveSocketError?.Invoke(se.SocketErrorCode);
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e.Message);
#endif
                clientPackage.Close();
            }

        }

        public void Close()
        {
            IsOpen = false;
            ClientPackages.Close();
        }

        public void Dispose()
        {
            Close();
            tcpListener.Stop();
        }
    }
}
