using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCService
{
    public delegate void OnIPCClientReadHandler(byte[] data);
    public class Client
    {
        private byte[] rxbuffer = new byte[TCPSpec.Instance.BufferTotalBytes];
        private Socket clientSocket;
        private TcpClient tcpClient;
        private Task ReadListenerHandlerTask;
        private System.Threading.CancellationTokenSource ReadListenerHandleCancelTokenSource;
        private System.Threading.CancellationToken ReadListenerHandleCancelToken;
        private int receiveTotalCnt;
        public bool IsOpen { get; private set; }

        public event OnIPCClientReadHandler OnIPCRead = null;
        public event OnIPCServerSocketError OnIPCReceiveSocketError = null;
        public Client()
        {
            IsOpen = false;
            ReadListenerHandleCancelTokenSource = new System.Threading.CancellationTokenSource();
            ReadListenerHandleCancelToken = ReadListenerHandleCancelTokenSource.Token;
            ReadListenerHandlerTask = new Task(ReadListenerHandler, ReadListenerHandleCancelToken);
            tcpClient = new TcpClient();
            tcpClient.ReceiveBufferSize = TCPSpec.Instance.BufferBytes;
        }

        public void Open()
        {
            try
            {
                tcpClient.Connect(TCPSpec.Instance.IPCIPAddress, TCPSpec.Instance.Port);
                ConnectedHandler();
            }
            catch (OperationCanceledException oce)
            {
#if DEBUG
                Debug.WriteLine($"Cancel Read Listener Handler.{Environment.NewLine}[{oce.Message}]");
#endif
            }
            finally
            {

            }
        }

        public async Task OpenAsync()
        {
            try
            {
                await tcpClient.ConnectAsync(TCPSpec.Instance.IPCIPAddress, TCPSpec.Instance.Port);
                ConnectedHandler();
            }
            catch (OperationCanceledException oce)
            {
#if DEBUG
                Debug.WriteLine($"Cancel Read Listener Handler.{Environment.NewLine}[{oce.Message}]");
#endif
            }
            finally
            {

            }
        }

        private void ConnectedHandler()
        {
            clientSocket = tcpClient.Client;
            IsOpen = true;
            ReadListenerHandlerTask.Start();
        }

        

        public bool Write(byte[] data)
        {
            TCPSpec.Instance.TransferSpecConvert(data, out byte[] buffer);
            try
            {
                clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch (ArgumentNullException ane)
            {
#if DEBUG
                Debug.WriteLine(ane.Message);
#endif
                return false;
            }
            catch (ArgumentOutOfRangeException aore)
            {
#if DEBUG
                Debug.WriteLine(aore.Message);
#endif
                Close();
                return false;
            }
            catch (System.IO.IOException ioe)
            {
#if DEBUG
                Debug.WriteLine(ioe.Message);
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

        private void ReadListenerHandler()
        {
            Stopwatch timeoutWatch = new Stopwatch();
            bool isReading = false;
            Socket socket = this.clientSocket;
            int totalReceiveCnt = 0;

            Action ReceiveDoneHandler = () =>
            {
                timeoutWatch.Stop();
                totalReceiveCnt = 0;
                rxbuffer.Clear();
                isReading = false;
            };

            Action ReceivedHandler = () =>
            {
                TCPSpec.Instance.ReceiveSpecConvert(rxbuffer, out byte[] data);
                OnIPCRead?.Invoke(data);
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
                        if (!isReading)
                        {
                            isReading = true;
                            timeoutWatch.Restart();
                        }

                        int receiveCnt = socket.Receive(rxbuffer, totalReceiveCnt, TCPSpec.Instance.BufferTotalBytes, SocketFlags.None);
                        totalReceiveCnt += receiveCnt;
                        if (totalReceiveCnt == TCPSpec.Instance.BufferTotalBytes)
                        {
                            ReceivedHandler();
                            ReceiveDoneHandler();
                        }
                        else if (TCPSpec.Instance.IsTimeout(timeoutWatch.ElapsedMilliseconds))
                        {
#if DEBUG
                            Debug.WriteLine($"IPC Server Socket Receive Timeout");
#endif
                            ReceiveTimeoutHandler();
                            ReceiveDoneHandler();
                        }
                    }
                    else if (ReadListenerHandleCancelToken.IsCancellationRequested)
                    {
                        ReadListenerHandleCancelToken.ThrowIfCancellationRequested();
                    }
                } while (IsOpen);
            }
            catch (SocketException se)
            {
                OnIPCReceiveSocketError?.Invoke(se.SocketErrorCode);
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e.Message);
#endif
                Close();
            }
        }

        public void Close()
        {
            IsOpen = false;
            ReadListenerHandleCancelTokenSource.Cancel();
        }

        public void Dispose()
        {
            ReadListenerHandlerTask.Dispose();
            ReadListenerHandleCancelTokenSource.Dispose();
        }
    }

    

    
}
