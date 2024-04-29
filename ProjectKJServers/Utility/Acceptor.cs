using KYCLog;
using KYCPacket;
using KYCSocketCore;
using System.Net;
using System.Net.Sockets;
using KYCException;
using System.Linq.Expressions;
using KYCUIEventManager;

namespace KYCSocketCore
{
    public class Acceptor : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        private SocketManager ClientSocketList;

        private CancellationTokenSource AcceptCancelToken;

        private List<Task> TotalTaskList = new List<Task>();

        private Socket ListenSocket;

        private IPAddress AllowSpecificIP = IPAddress.Any;

        private int MaxAcceptCount;

        public const int MAX_ACCEPT_INFINITE = -1;


        protected Acceptor(int MaxAcceptCount)
        {
            AcceptCancelToken = new CancellationTokenSource();
            if(MaxAcceptCount == MAX_ACCEPT_INFINITE)
            {
                ClientSocketList = new SocketManager(0, false);
            }
            else
            {
                ClientSocketList = new SocketManager(MaxAcceptCount, false);
            }
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.MaxAcceptCount = MaxAcceptCount;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;

            if (Disposing)
            {

            }

            AcceptCancelToken.Dispose();

            ListenSocket.Close();

            IsAlreadyDisposed = true;
        }

        protected virtual void Init(IPAddress AlloweSpecificIP, int PortNumber)
        {
            try
            {
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, PortNumber));
                ListenSocket.Listen(64);

                AllowSpecificIP = AlloweSpecificIP;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
            }
        }

        protected virtual void Start(string ServerName)
        {
            Task.Run(async () =>{ await AcceptStart(ServerName);});
        }

        private async Task AcceptStart(string ServerName)
        {
            if (MaxAcceptCount == MAX_ACCEPT_INFINITE)
            {
                await InfiniteAccept(ServerName).ConfigureAwait(false);
            }
            else
            {
                await LimitedAccept(ServerName).ConfigureAwait(false);
            }
        }

        private async Task InfiniteAccept(string ServerName)
        {
            await LogManager.GetSingletone.WriteLog($"{ServerName}의 무한정 연결을 대기합니다.").ConfigureAwait(false);
            while (!AcceptCancelToken.Token.IsCancellationRequested)
            {
                try
                {
                    Socket ClientSocket = await ListenSocket.AcceptAsync(AcceptCancelToken.Token);
                    // 추후 자동으로 소켓을 전부 정리하기 위해 리스트에 추가
                    ClientSocketList.AddSocket(ClientSocket);
                    TotalTaskList.Add(Task.Run(() => Process(ClientSocket)));
                    UIEvent.GetSingletone.IncreaseUserCount(true);
                }
                catch (OperationCanceledException)
                {
                    // 취소가 요청된것이므로 중단 시킨다
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.").Wait();
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog(e.Message).Wait();
                }
            }
        }

        private async Task LimitedAccept(string ServerName)
        {
            for (int i = 0; i < MaxAcceptCount; i++)
            {
                await LogManager.GetSingletone.WriteLog($"{ServerName}의 {i + 1}번째 연결을 대기합니다.").ConfigureAwait(false);

                if (AcceptCancelToken.Token.IsCancellationRequested)
                {
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.").Wait();
                    break;
                }
                try
                {
                    Socket ClientSocket = await ListenSocket.AcceptAsync(AcceptCancelToken.Token);
                    if (CheckIsAllowedIP(ClientSocket))
                    {
                        ClientSocketList.AddSocket(ClientSocket);
                        await LogManager.GetSingletone.WriteLog($"{ServerName}랑 {i + 1}개 연결되었습니다.").ConfigureAwait(false);
                    }
                    else
                    {
                        ClientSocket.Close();
                        i--;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 취소가 요청된것이므로 중단 시킨다
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.").Wait();
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    i--;
                    LogManager.GetSingletone.WriteLog(e.Message).Wait();
                }
            }
        }

        protected virtual async Task Stop(string ServerName, TimeSpan DelayTime)
        {
            await CancelAccept(DelayTime).ConfigureAwait(false);
            await ClientSocketList.Cancel().ConfigureAwait(false);
            await CleanUpAcceptTask(ServerName).ConfigureAwait(false);
        }

        public virtual bool IsConnected()
        {
            if (ClientSocketList.GetCount() == 0 || ClientSocketList == null)
            {
                return false;
            }

            // 무한으로 연결을 받는 서버라면 모든 소켓을 체크할 필요가 없다
            if(MaxAcceptCount == MAX_ACCEPT_INFINITE)
            {
                // 사실 accept하는 소켓을 체크할 필요는 없다
                // 대부분 True가 나오겠지만, 로컬 Port에 연결 안된 소켓은 에러가 발생한것이다
                if (!ListenSocket.IsBound)
                    return false;
                return true; 
            }

            foreach (var Socket in ClientSocketList)
            {
                try
                {
                    if (!Socket.Connected || Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0)
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e).Wait();
                    return false;
                }
            }
            return true;
        }

        private async Task CancelAccept(TimeSpan DelayTime)
        {
            AcceptCancelToken.Cancel();
            // Cancel 시키고 바로 종료하면 Status가 바뀌지 않는다. 그래서 3초 대기
            await Task.Delay(DelayTime).ConfigureAwait(false);
        }

        private async Task CleanUpAcceptTask(string ServerName)
        {
            List<Task> RunningTasks = TotalTaskList.Where(task => task.Status == TaskStatus.Running).ToList();
            TotalTaskList = TotalTaskList.Except(RunningTasks).ToList();
            await LogManager.GetSingletone.WriteLog($"{ServerName}와 실행중인 남은 Task 완료를 대기합니다.").ConfigureAwait(false);
            await Task.WhenAll(RunningTasks).ConfigureAwait(false);
        }
        protected virtual async Task Process(Socket ClientSocket)
        {
            await Task.Yield();
        }


        private bool CheckIsAllowedIP(Socket ClientSocket)
        {
            if (AllowSpecificIP == IPAddress.Any)
            {
                return true;
            }
            if (ClientSocket.RemoteEndPoint is IPEndPoint RemoteEndPoint)
            {
                if (RemoteEndPoint.Address.Equals(AllowSpecificIP))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        protected virtual async Task<byte[]> RecvData()
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

        protected virtual async Task<int> SendData(byte[]DataBuffer)
        {
            Socket? SendSocket = null;
            try
            {
                SendSocket = await ClientSocketList.GetAvailableSocket().ConfigureAwait(false);
                SendSocket.SendTimeout = 500;
                return await SendSocket.SendAsync(DataBuffer,AcceptCancelToken.Token).ConfigureAwait(false);
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

        protected virtual async Task<byte[]> RecvClientData(Socket ClientSock)
        {
            try
            {
                byte[] DataSizeBuffer = new byte[sizeof(int)];
                await ClientSock.ReceiveAsync(DataSizeBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                byte[] DataBuffer = new byte[PacketUtils.GetSizeFromPacket(ref DataSizeBuffer)];
                await ClientSock.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                return DataBuffer;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 종료되었습니다.");
            }
            catch(OperationCanceledException)
            {
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 취소되었습니다.");
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
            }
        }
        protected virtual async Task<int> SendClientData(byte[] DataBuffer, Socket ClientSock)
        {
            try
            {
                return await ClientSock.SendAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 종료되었습니다.");
            }
            catch (OperationCanceledException)
            {
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 취소되었습니다.");
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
            }
        }
    }
}
