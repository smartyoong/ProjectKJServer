using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KYCException;
using KYCLog;
using KYCPacket;
using KYCSocketCore;
using KYCUIEventManager;
using LoginServer.Properties;

namespace LoginServer
{
    internal class ClientAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<ClientAcceptor> Lazy = new Lazy<ClientAcceptor>(() => new ClientAcceptor());
        public static ClientAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        private RecvPacketProcessor RecvProcessor = new RecvPacketProcessor();

        private ClientAcceptor() : base(Settings.Default.ClientAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start()
        {
            Init(IPAddress.Any, Settings.Default.ClientAcceptPort);
            Start("Client");
            ProcessCheck();
            GetRecvPacket();
        }

        public async Task Stop()
        {
            await Stop("Client", TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            Dispose();
        }

        ~ClientAcceptor()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {
                base.Dispose();
            }
            IsAlreadyDisposed = true;
        }

        private void ProcessCheck()
        {
            Task.Run(async () => {
                while (!CheckCancelToken.IsCancellationRequested)
                {
                    if (IsConnected())
                        UIEvent.GetSingletone.UpdateLoginServerStatus(true);
                    else
                        UIEvent.GetSingletone.UpdateLoginServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckCancelToken.Token);
        }

        public void GetRecvPacket()
        {
            Task.Run(async () =>
            {
                while (!CheckCancelToken.IsCancellationRequested)
                {
                    try
                    {
                        byte[] DataBuffer = await RecvData().ConfigureAwait(false);
                        if (DataBuffer == null)
                            continue;
                        RecvProcessor.PushToPacketPipeline(DataBuffer);
                    }
                    catch (ArgumentException e)
                    {
                        // ID 매개변수 불일치
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 어캐할까?
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // 그외 에러
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                }
            });
        }


        // 클라이언트를 Accept할 때에는 Recv랑 Send를 재정의 해야한다
        protected override async Task<byte[]> RecvData()
        {
            Socket? RecvSocket = null;
            try
            {
                RecvSocket = await ClientSocketList.GetAvailableSocket().ConfigureAwait(false);
                RecvSocket.ReceiveTimeout = 500;
                byte[] DataSizeBuffer = new byte[sizeof(int)];
                await RecvSocket.ReceiveAsync(DataSizeBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                byte[] DataBuffer = new byte[PacketUtils.GetSizeFromPacket(ref DataSizeBuffer)];
                await RecvSocket.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                return DataBuffer;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw new ConnectionClosedException("Recv를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                throw new TimeoutException();
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
            }
            finally
            {
                if (RecvSocket != null && ClientSocketList.CanReturnSocket())
                    ClientSocketList.ReturnSocket(RecvSocket);
                else if (RecvSocket != null && !ClientSocketList.CanReturnSocket())
                    RecvSocket.Close();
            }
        }

        protected override async Task<int> SendData(byte[] DataBuffer)
        {
            Socket? SendSocket = null;
            try
            {
                SendSocket = await ClientSocketList.GetAvailableSocket().ConfigureAwait(false);
                SendSocket.SendTimeout = 500;
                return await SendSocket.SendAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw new ConnectionClosedException("Send를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                throw new TimeoutException();
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
            }
            finally
            {
                if (SendSocket != null && ClientSocketList.CanReturnSocket())
                    ClientSocketList.ReturnSocket(SendSocket);
                else if (SendSocket != null && !ClientSocketList.CanReturnSocket())
                    SendSocket.Close();
            }
        }
    }
}
