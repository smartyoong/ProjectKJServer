using KYCException;
using KYCLog;
using KYCPacket;
using KYCUIEventManager;
using System.Net;
using System.Net.Sockets;

namespace KYCSocketCore
{
    public class Acceptor : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        private int Booleanflag = 0;

        protected bool IsAccepted
        {
            get { return Booleanflag != 0; }
            set
            {
                Interlocked.Exchange(ref Booleanflag, value ? 1 : 0);
            }
        }

        private CancellationTokenSource AcceptCancelToken;

        private List<Task> TotalTaskList = new List<Task>();

        private IPAddress AllowSpecificIP = IPAddress.Any;

        private int MaxAcceptCount;

        public const int MAX_ACCEPT_INFINITE = -1;

        Socket ListenSocket;

        int CurrentGroupID = -1;


        protected Acceptor(int MaxAcceptCount)
        {
            AcceptCancelToken = new CancellationTokenSource();
            this.MaxAcceptCount = MaxAcceptCount;
            ListenSocket = SocketManager.GetSingletone.BorrowSocket();
            IsAccepted = false;
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
            Task.Run(async () => { await AcceptStart(ServerName); });
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
                    Socket ClientSocket = SocketManager.GetSingletone.BorrowSocket();
                    await ListenSocket.AcceptAsync(ClientSocket, AcceptCancelToken.Token).ConfigureAwait(false);
                    TotalTaskList.Add(Task.Run(() => Process(ClientSocket)));
                    UIEvent.GetSingletone.IncreaseUserCount(true);
                    IsAccepted = true;
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
                    Socket ClientSocket = SocketManager.GetSingletone.BorrowSocket();
                    await ListenSocket.AcceptAsync(ClientSocket, AcceptCancelToken.Token).ConfigureAwait(false);
                    if (CheckIsAllowedIP(ClientSocket))
                    {
                        await LogManager.GetSingletone.WriteLog($"{ServerName}랑 {i + 1}개 연결되었습니다.").ConfigureAwait(false);

                        if (SocketManager.GetSingletone.IsAlreadyGroup(CurrentGroupID))
                        {
                            SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, ClientSocket);
                        }
                        else
                        {
                            CurrentGroupID = SocketManager.GetSingletone.MakeNewSocketGroup(ClientSocket);
                        }
                        IsAccepted = true;
                    }
                    else
                    {
                        SocketManager.GetSingletone.ReturnSocket(ClientSocket);
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
            await CleanUpAcceptTask(ServerName).ConfigureAwait(false);
        }

        public virtual bool IsConnected()
        {
            // 무한으로 연결을 받는 서버라면 모든 소켓을 체크할 필요가 없다
            if (MaxAcceptCount == MAX_ACCEPT_INFINITE)
            {
                // 사실 accept하는 소켓을 체크할 필요는 없다
                // 대부분 True가 나오겠지만, 로컬 Port에 연결 안된 소켓은 에러가 발생한것이다
                if (!ListenSocket.IsBound)
                    return false;
                return true;
            }

            // 아직 그룹이 할당되지 않았다.
            if (!IsAccepted)
                return false;
            try
            {
                foreach (var Socket in SocketManager.GetSingletone.GetSocketGroup(CurrentGroupID))
                {
                    if (!Socket.Connected || Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0)
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e).Wait();
                return false;
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
            await Task.Delay(TimeSpan.FromMilliseconds(1));
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
                RecvSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                RecvSocket.ReceiveTimeout = 500;
                byte[] DataSizeBuffer = new byte[sizeof(int)];
                await RecvSocket.ReceiveAsync(DataSizeBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                byte[] DataBuffer = new byte[PacketUtils.GetSizeFromPacket(ref DataSizeBuffer)];
                await RecvSocket.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, RecvSocket);
                return DataBuffer;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                //연결이 끊겼다. 재사용 가능한 소켓으로 반납시킨후 재사용 가능하도록 준비한다
                // 끊겼다는 에러는 소켓을 받아왔지만, 해당 소켓이 ReceiveAsync중에 끊긴 것이다. 그룹에서는 제거된다.
                if(RecvSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                IsAccepted = false;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 {CurrentGroupID} 그룹 소켓이 종료되었습니다.");
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                // RecvTimeout이 발생했다. 그룹으로 리턴시켜준다. 만약 그룹 값이 이상하다면 재사용소켓으로 넘긴다 (그룹에서는 제거)
                if(RecvSocket != null)
                {
                    if (SocketManager.GetSingletone.IsAlreadyGroup(CurrentGroupID))
                        SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, RecvSocket);
                    else
                        SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                }
                throw new TimeoutException();
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
            }
        }

        protected virtual async Task<int> SendData(byte[] DataBuffer)
        {
            Socket? SendSocket = null;
            try
            {
                SendSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                SendSocket.SendTimeout = 500;
                int SendSize = await SendSocket.SendAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, SendSocket);
                return SendSize;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                if (SendSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(SendSocket);
                IsAccepted = false;
                throw new ConnectionClosedException("Send를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                // SendTimeout이 발생했다. 그룹으로 리턴시켜준다. 만약 그룹 값이 이상하다면 재사용소켓으로 넘긴다 (그룹에서는 제거)
                if (SendSocket != null)
                {
                    if (SocketManager.GetSingletone.IsAlreadyGroup(CurrentGroupID))
                        SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, SendSocket);
                    else
                        SocketManager.GetSingletone.ReturnSocket(SendSocket);
                }
                throw new TimeoutException();
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message).Wait();
                throw;
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
                SocketManager.GetSingletone.ReturnSocket(ClientSock);
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 종료되었습니다.");
            }
            // 연결 취소는 곧 서버 종료이므로 재사용 준비를 할 필요가 없다
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
                SocketManager.GetSingletone.ReturnSocket(ClientSock);
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

        protected void PrepareToReAccept(string ServerName)
        {
            CancelAccept(TimeSpan.FromSeconds(1)).Wait();
            AcceptCancelToken = new CancellationTokenSource();
            LogManager.GetSingletone.WriteLog($"{ServerName} Accept를 재시작합니다.").Wait();
        }
    }
}
