using KYCException;
using KYCLog;
using KYCPacket;
using KYCUIEventManager;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace KYCSocketCore
{
    public class Acceptor : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        private ManualResetEvent ServerAccepted = new ManualResetEvent(false);

        private CancellationTokenSource AcceptCancelToken;

        private List<Task> TotalTaskList = new List<Task>();

        private IPAddress AllowSpecificIP = IPAddress.Any;

        private int MaxAcceptCount;

        public const int MAX_ACCEPT_INFINITE = -1;

        private Socket ListenSocket;

        private int CurrentGroupID = -1;

        // 클라이언트 소켓을 관리하기 위한 변수 속도를 위해 메모리를 희생

        private ConcurrentDictionary<int, Socket> ClientSocks;

        private ConcurrentDictionary<Socket, int> KeyValuePairs;

        private int CurrentClientID = 0;


        protected Acceptor(int MaxAcceptCount)
        {
            AcceptCancelToken = new CancellationTokenSource();
            this.MaxAcceptCount = MaxAcceptCount;
            ClientSocks = new ConcurrentDictionary<int, Socket>();
            KeyValuePairs = new ConcurrentDictionary<Socket, int>();
            ListenSocket = SocketManager.GetSingletone.BorrowSocket();
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
                LogManager.GetSingletone.WriteLog(e);
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
            LogManager.GetSingletone.WriteLog($"{ServerName}의 무한정 연결을 대기합니다.");
            while (!AcceptCancelToken.Token.IsCancellationRequested)
            {
                try
                {
                    Socket ClientSocket = SocketManager.GetSingletone.BorrowSocket();
                    await ListenSocket.AcceptAsync(ClientSocket, AcceptCancelToken.Token).ConfigureAwait(false);
                    ServerAccepted.Set();
                    // 클라의 경우 await로 Accept를 받고 시작하기에 문제가 없다.
                    LogOn(ClientSocket);
                    TotalTaskList.Add(Task.Run(() => Process(ClientSocket)));
                    UIEvent.GetSingletone.IncreaseUserCount(true);
                }
                catch (OperationCanceledException)
                {
                    // 취소가 요청된것이므로 중단 시킨다
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.");
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog(e.Message);
                }
            }
        }

        private async Task LimitedAccept(string ServerName)
        {
            for (int i = 0; i < MaxAcceptCount; i++)
            {
                LogManager.GetSingletone.WriteLog($"{ServerName}의 {i + 1}번째 연결을 대기합니다.");

                if (AcceptCancelToken.Token.IsCancellationRequested)
                {
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.");
                    break;
                }
                try
                {
                    Socket ClientSocket = SocketManager.GetSingletone.BorrowSocket();
                    await ListenSocket.AcceptAsync(ClientSocket, AcceptCancelToken.Token).ConfigureAwait(false);
                    if (CheckIsAllowedIP(ClientSocket))
                    {
                        LogManager.GetSingletone.WriteLog($"{ServerName}랑 {i + 1}개 연결되었습니다.");

                        if (SocketManager.GetSingletone.IsAlreadyGroup(CurrentGroupID))
                        {
                            SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, ClientSocket);
                        }
                        else
                        {
                            CurrentGroupID = SocketManager.GetSingletone.MakeNewSocketGroup(ClientSocket);
                        }
                        ServerAccepted.Set();
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
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.");
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    i--;
                    LogManager.GetSingletone.WriteLog(e);
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
            if (CurrentGroupID == -1)
                return false;

            ServerAccepted.WaitOne();

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
                LogManager.GetSingletone.WriteLog(e);
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
            LogManager.GetSingletone.WriteLog($"{ServerName}와 실행중인 남은 Task 완료를 대기합니다.");
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

        private void LogOn(Socket ClientSock)
        {
            if (!ClientSocks.TryAdd(CurrentClientID, ClientSock))
                return;
            if (!KeyValuePairs.TryAdd(ClientSock, CurrentClientID))
            {
                ClientSocks.TryRemove(CurrentClientID, out _);
                return;
            }
            Interlocked.Increment(ref CurrentClientID);
            var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
            UIEvent.GetSingletone.IncreaseUserCount(true);
            LogManager.GetSingletone.WriteLog($"새로운 클라이언트 {Addr}이 연결되었습니다.");
        }

        private void LogOut(Socket ClientSock)
        {
            var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
            if (KeyValuePairs.TryRemove(ClientSock, out int ClientID))
            {
                if (ClientSocks.TryRemove(ClientID, out _))
                {
                    LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 로그아웃 하였습니다.");
                }
            }
            UIEvent.GetSingletone.IncreaseUserCount(false);
            LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 연결이 끊겼습니다.");
            SocketManager.GetSingletone.ReturnSocket(ClientSock);
        }

        public int GetClientID(Socket ClientSock)
        {
            if (KeyValuePairs.TryGetValue(ClientSock, out int ClientID))
                return ClientID;
            return -1;
        }

        public Socket? GetClientSocket(int ClientID)
        {
            if (ClientSocks.TryGetValue(ClientID, out Socket? ClientSock))
                return ClientSock;
            return null;
        }

        //클라 Recv Send랑 분리하자 서버간 연결은 소켓 그룹으로 관리할 것이다
        //클라는 개개인의 Socket이 있어야 하지만, 서버는 그룹단위 소켓 풀로 관리할 것이기 때문에
        //소켓을 굳이 매개변수로 줄 필요가 없다. 또한 클라에게 Send 및 서버에게 동시에 Send하는 경우에도
        //2개의 Send가 필요하기에 한가지의 PipeLine으로 Send를 할 수 없다. 어
        protected virtual async Task<Memory<byte>> RecvData()
        {
            Socket? RecvSocket = null;
            ServerAccepted.WaitOne();
            try
            {
                // 나중에 메세지 버퍼 크기를 대략적으로 조사한 후에 고정 패킷을 사용하는걸 고려해봐야할듯
                RecvSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                RecvSocket.ReceiveTimeout = 500;
                Memory<byte> DataSizeBuffer = new byte[sizeof(int)];
                int RecvSize = await RecvSocket.ReceiveAsync(DataSizeBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                if(RecvSize <= 0)
                {
                    if (RecvSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                    ServerAccepted.Reset();
                    throw new ConnectionClosedException($"Recv를 시도하던 중에 {CurrentGroupID} 그룹 소켓이 종료되었습니다.");
                }

                Memory<byte> DataBuffer = new byte[PacketUtils.GetSizeFromPacket(DataSizeBuffer)];
                RecvSize = await RecvSocket.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                if (RecvSize <= 0)
                {
                    if (RecvSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                    ServerAccepted.Reset();
                    throw new ConnectionClosedException($"Recv를 시도하던 중에 {CurrentGroupID} 그룹 소켓이 종료되었습니다.");
                }
                SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, RecvSocket);
                return DataBuffer;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e);
                //연결이 끊겼다. 재사용 가능한 소켓으로 반납시킨후 재사용 가능하도록 준비한다
                // 끊겼다는 에러는 소켓을 받아왔지만, 해당 소켓이 ReceiveAsync중에 끊긴 것이다. 그룹에서는 제거된다.
                if(RecvSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                ServerAccepted.Reset();  
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
                LogManager.GetSingletone.WriteLog(e);
                throw;
            }
        }

        protected virtual async Task<int> SendData(Memory<byte> DataBuffer)
        {
            Socket? SendSocket = null;
            ServerAccepted.WaitOne();
            try
            {
                SendSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                SendSocket.SendTimeout = 500;
                int SendSize = await SendSocket.SendAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, SendSocket);
                if(SendSize > 0)
                    return SendSize;
                else
                {
                    if (SendSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(SendSocket);
                    ServerAccepted.Reset();
                    return -1;
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e);
                if (SendSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(SendSocket);
                ServerAccepted.Reset();
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
                LogManager.GetSingletone.WriteLog(e);
                throw;
            }
        }

        protected virtual async Task<Memory<byte>> RecvClientData(Socket ClientSock)
        {
            try
            {
                // 추후 고정패킷으로 길이를 바꿔서 Recveive를 2번하는것을 막아보자
                Memory<byte> DataSizeBuffer = new byte[sizeof(int)];

                int RecvSize = await ClientSock.ReceiveAsync(DataSizeBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                if (RecvSize <= 0)
                {
                    LogOut(ClientSock);
                    throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
                }

                Memory<byte> DataBuffer = new byte[PacketUtils.GetSizeFromPacket(DataSizeBuffer)];
                RecvSize = await ClientSock.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                if (RecvSize <= 0)
                {
                    LogOut(ClientSock);
                    throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
                }
                return DataBuffer;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogOut(ClientSock);
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
            }
            // 연결 취소는 곧 서버 종료이므로 재사용 준비를 할 필요가 없다
            catch (OperationCanceledException)
            {
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Recv를 시도하던 중에 클라이언트 소켓 {Addr}이 취소되었습니다.");
            }
            catch(ConnectionClosedException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                throw;
            }
        }
        protected virtual async Task<int> SendClientData(Memory<byte> DataBuffer, Socket ClientSock)
        {
            try
            {
                int SendSize = await ClientSock.SendAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                if (SendSize > 0)
                {
                    return SendSize;
                }
                else
                {
                    LogOut(ClientSock);
                    return SendSize;
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogOut(ClientSock);
                throw new ConnectionClosedException($"Send를 시도하던 중에 클라이언트 소켓이 종료되었습니다.");
            }
            catch (OperationCanceledException)
            {
                var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
                throw new ConnectionClosedException($"Send를 시도하던 중에 클라이언트 소켓 {Addr}이 취소되었습니다.");
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                throw;
            }
        }

        protected void PrepareToReAccept(string ServerName)
        {
            CancelAccept(TimeSpan.FromSeconds(1)).Wait();
            AcceptCancelToken = new CancellationTokenSource();
            LogManager.GetSingletone.WriteLog($"{ServerName} Accept를 재시작합니다.");
            ServerAccepted.Reset();
        }
    }
}
