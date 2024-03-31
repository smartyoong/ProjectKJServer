using System.Net;
using System.Net.Sockets;

namespace LoginServer
{
    internal class Connector : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        protected List<Socket> ConnectSocketList;

        protected private CancellationTokenSource ConnectCancelToken;

        private List<Task> TryConnectTaskList = new List<Task>();

        protected Connector()
        {
            ConnectCancelToken = new CancellationTokenSource();
            ConnectSocketList = new List<Socket>();
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

            if (ConnectSocketList != null)
            {
                foreach (var Socket in ConnectSocketList)
                {
                    Socket.Close();
                }
                ConnectSocketList.Clear();
            }

            foreach (var ConnectTask in TryConnectTaskList)
            {
                if (ConnectTask.Status == TaskStatus.Canceled || ConnectTask.Status == TaskStatus.RanToCompletion || ConnectTask.Status == TaskStatus.Faulted)
                {
                    ConnectTask.Dispose();
                }
            }

            ConnectCancelToken.Dispose();

            IsAlreadyDisposed = true;
        }

        protected virtual void Init(int MakeSocketCount)
        {
            for (int i = 0; i < MakeSocketCount; i++)
            {
                ConnectSocketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }
        }

        protected virtual void Start(IPEndPoint IPAddr, string ServerName)
        {
            if (ConnectCancelToken.Token.IsCancellationRequested)
                return;
            TryConnectTaskList.Add(TryConnect(IPAddr, ServerName));
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
            await CancelConnect(DelayTime);
            await CleanUpConnectTask(ServerName);
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
            int i = 0;
            for (; i < ConnectSocketList.Count; i++)
            {
                try
                {
                    if (ConnectCancelToken.Token.IsCancellationRequested)
                        return;
                    await ConnectSocketList[i].ConnectAsync(IPAddr, ConnectCancelToken.Token);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    await LogManager.GetSingletone.WriteLog($"{ServerName}와 연결에 실패하였습니다. {i}번째 객체 시도중");
                    i--;
                }
                catch (OperationCanceledException)
                {
                    // 연결이 취소되었으므로 메서드를 종료합니다.
                    return;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await LogManager.GetSingletone.WriteLog(e);
                }
            }
            UIEvent.GetSingletone.UpdateDBServerStatus(IsConnected());
        }


        /// <summary>
        /// 정해진 소켓이 전부 연결되었는지 확인하는 메서드입니다.
        /// </summary>
        /// <returns>
        /// 전부 연결되었으면 true, 아니면 false를 반환합니다.
        /// </returns>
        public virtual bool IsConnected()
        {
            if(ConnectSocketList.Count == 0 || ConnectSocketList == null)
            {
                return false;
            }

            foreach (var Socket in ConnectSocketList)
            {
                if (!Socket.Connected)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task CancelConnect(TimeSpan DelayTime)
        {
            ConnectCancelToken.Cancel();
            // Cancel 시키고 바로 종료하면 Status가 바뀌지 않는다. 그래서 3초 대기
            await Task.Delay(DelayTime);
        }

        private async Task CleanUpConnectTask(string ServerName)
        {
            List<Task> WaitForCompleteTaskList = new List<Task>();
            foreach (var ConnectTask in TryConnectTaskList)
            {
                if (ConnectTask.Status == TaskStatus.Running)
                {
                    WaitForCompleteTaskList.Add(ConnectTask);
                    TryConnectTaskList.Remove(ConnectTask);
                }
            }
            await LogManager.GetSingletone.WriteLog($"{ServerName}와 실행중인 남은 Task 완료를 대기합니다.");
            Task.WaitAll(WaitForCompleteTaskList.ToArray());
            foreach (var WaitConnectTask in WaitForCompleteTaskList)
            {
                // Task가 완료되었으므로 Dispose하기 위해 다시 List에 추가.
                TryConnectTaskList.Add(WaitConnectTask);
            }
        }
    }
}
