using KYCException;
using KYCLog;
using KYCPacket;
using System.Net;
using System.Net.Sockets;

namespace KYCSocketCore
{
    public class Connector : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        protected private CancellationTokenSource ConnectCancelToken;

        private List<Task> TryConnectTaskList = new List<Task>();

        int NeedConnectCount;

        int CurrentGroupID = -1;

        private ManualResetEvent ServerConnected = new ManualResetEvent(false);

        protected Connector(int ConnectCount)
        {
            ConnectCancelToken = new CancellationTokenSource();
            NeedConnectCount = ConnectCount;
        }

        ~Connector()
        {
            Dispose(false);
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsAlreadyDisposed)
                return;

            if (disposing)
            {

            }


            ConnectCancelToken.Dispose();

            IsAlreadyDisposed = true;
        }

        protected virtual void Start(IPEndPoint IPAddr, string ServerName)
        {
            if (ConnectCancelToken.Token.IsCancellationRequested)
                return;
            Task.Run(async () => { await TryConnect(IPAddr, ServerName); });

        }

        /// <summary>
        /// 연결 시도 중인 모든 로직을 종료시킵니다.
        /// 만약 Running 상태의 Task를 전부 대기합니다.
        /// 반드시 Stop 메서드를 호출한 후 Dispose 메서드를 호출해야 합니다.
        /// </summary>
        /// <exception>
        /// Task.Status가 Wait For Activation 상태일 경우 Dispose가 정상적으로 작동하지 않습니다.
        /// </exception>
        protected virtual async Task Stop(string ServerName, TimeSpan DelayTime)
        {
            await CancelConnect(DelayTime).ConfigureAwait(false);
            await CleanUpConnectTask(ServerName).ConfigureAwait(false);
        }


        /// <summary>
        /// DB서버와 연결을 시도하는 메서드입니다.
        /// 만약 연결에 실패하면 지속적으로 연결을 재시도합니다.
        /// </summary>
        /// <param name="IPAddr">
        /// 서버의 IP주소입니다.
        /// <param name="ServerName">
        /// 로그용 서버 이름입니다.
        /// </param>
        /// <returns>
        /// Task를 반환합니다.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// 취소 프로토콜을 지원합니다. 취소가 요청되면 예외가 발생합니다.
        /// </exception>
        protected virtual async Task TryConnect(IPEndPoint IPAddr, string ServerName)
        {
            for (int i = 0; i < NeedConnectCount; i++)
            {
                if (ConnectCancelToken.Token.IsCancellationRequested)
                    break;
                Socket Sock = SocketManager.GetSingletone.BorrowSocket();
                try
                {
                    await Sock.ConnectAsync(IPAddr, ConnectCancelToken.Token).ConfigureAwait(false);
                    LogManager.GetSingletone.WriteLog($"{i + 1}번째 객체가 {ServerName}와 연결에 성공하였습니다.");

                    if (SocketManager.GetSingletone.IsAlreadyGroup(CurrentGroupID))
                    {
                        SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, Sock);
                        ServerConnected.Set();
                    }
                    else
                    {
                        CurrentGroupID = SocketManager.GetSingletone.MakeNewSocketGroup(Sock);
                    }
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    LogManager.GetSingletone.WriteLog($"{ServerName}와 연결에 실패하였습니다. {i + 1}번째 객체 시도중");
                    SocketManager.GetSingletone.ReturnSocket(Sock);
                    i--;
                }
                catch (OperationCanceledException)
                {
                    // 연결이 취소되었으므로 메서드를 종료합니다.
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog(e);
                }
            }
        }


        /// <summary>
        /// 정해진 소켓이 전부 연결되었는지 확인하는 메서드입니다.
        /// 이 메서드는 정해진 시간동안 해당 스레드가 블로킹되기 때문에
        /// 비동기적으로 호출하는 것을 권장합니다
        /// </summary>
        /// <returns>
        /// 전부 연결되었으면 true, 아니면 false를 반환합니다.
        /// </returns>
        public virtual bool IsConnected()
        {
            // 아직 그룹이 정해지지 않았다
            if (CurrentGroupID == -1)
                return false;

            ServerConnected.WaitOne();

            try
            {
                foreach (var Socket in SocketManager.GetSingletone.GetSocketGroup(CurrentGroupID)!)
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

        private async Task CancelConnect(TimeSpan DelayTime)
        {
            ConnectCancelToken.Cancel();
            // Cancel 시키고 바로 종료하면 Status가 바뀌지 않는다. 그래서 3초 대기
            await Task.Delay(DelayTime).ConfigureAwait(false);
        }

        private async Task CleanUpConnectTask(string ServerName)
        {
            List<Task> RunningTasks = TryConnectTaskList.Where(task => task.Status == TaskStatus.Running).ToList();
            TryConnectTaskList = TryConnectTaskList.Except(RunningTasks).ToList();
            LogManager.GetSingletone.WriteLog($"{ServerName}와 실행중인 남은 Task 완료를 대기합니다.");
            await Task.WhenAll(RunningTasks).ConfigureAwait(false);
        }

        // Acceptor와 Send Recv는 동일 코드
        protected virtual async Task<Memory<byte>> RecvData()
        {
            Socket? RecvSocket = null;
            try
            {
                ServerConnected.WaitOne();
                // 나중에 메세지 버퍼 크기를 대략적으로 조사한 후에 고정 패킷을 사용하는걸 고려해봐야할듯
                RecvSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                RecvSocket.ReceiveTimeout = 500;
                Memory<byte> DataSizeBuffer = new byte[sizeof(int)];
                int RecvSize = await RecvSocket.ReceiveAsync(DataSizeBuffer, ConnectCancelToken.Token).ConfigureAwait(false);
                if (RecvSize <= 0)
                {
                    if (RecvSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                    ServerConnected.Reset();
                    throw new ConnectionClosedException($"Recv를 시도하던 중에 {CurrentGroupID} 그룹 소켓이 종료되었습니다.");
                }

                Memory<byte> DataBuffer = new byte[PacketUtils.GetSizeFromPacket(DataSizeBuffer)];
                RecvSize = await RecvSocket.ReceiveAsync(DataBuffer, ConnectCancelToken.Token).ConfigureAwait(false);
                if (RecvSize <= 0)
                {
                    if (RecvSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                    ServerConnected.Reset();
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
                if (RecvSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(RecvSocket);
                ServerConnected.Reset();
                throw new ConnectionClosedException($"Recv를 시도하던 중에 {CurrentGroupID} 그룹 소켓이 종료되었습니다.");
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                // RecvTimeout이 발생했다. 그룹으로 리턴시켜준다. 만약 그룹 값이 이상하다면 재사용소켓으로 넘긴다 (그룹에서는 제거)
                if (RecvSocket != null)
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
            try
            {
                ServerConnected.WaitOne();
                SendSocket = await SocketManager.GetSingletone.GetAvailableSocketFromGroup(CurrentGroupID).ConfigureAwait(false);
                SendSocket.SendTimeout = 500;
                int SendSize = await SendSocket.SendAsync(DataBuffer, ConnectCancelToken.Token).ConfigureAwait(false);
                SocketManager.GetSingletone.AddSocketToGroup(CurrentGroupID, SendSocket);
                if (SendSize > 0)
                    return SendSize;
                else
                {
                    if (SendSocket != null)
                        SocketManager.GetSingletone.ReturnSocket(SendSocket);
                    ServerConnected.Reset();
                    return -1;
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                LogManager.GetSingletone.WriteLog(e);
                if (SendSocket != null)
                    SocketManager.GetSingletone.ReturnSocket(SendSocket);
                ServerConnected.Reset();
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

        protected void PrepareToReConnect(string ServerName)
        {
            CancelConnect(TimeSpan.FromSeconds(3)).Wait();
            ConnectCancelToken = new CancellationTokenSource();
            LogManager.GetSingletone.WriteLog($"{ServerName} Accept를 재시작합니다.");
            ServerConnected.Reset();
        }
    }
}
