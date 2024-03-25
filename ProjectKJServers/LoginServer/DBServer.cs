using LoginServer.Properties;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace LoginServer
{
    internal class DBServer : IDisposable
    {
        private bool IsAlreadyDisposed = false;
        /// <value>지연 생성 및 싱글톤 패턴을 사용합니다.</value>
        private static readonly Lazy<DBServer> Lazy = new Lazy<DBServer>(() => new DBServer());

        /// <value> UI 갱신용 이벤트.</value>
        public event Action<bool>? DBServerEvent;

        public static DBServer GetSingletone { get { return Lazy.Value; } }

        private List<Socket> DBSocketList;
        private CancellationTokenSource DBServerCancelToken;
        private List<Task> DBServerTaskList = new List<Task>();


        /// <summary>
        /// DBServer 클래스의 생성자입니다.
        /// 소켓 연결 갯수만큼 클래스를 생성하고, 초기화시킵니다.
        /// </summary>
        private DBServer()
        {
            DBSocketList = new List<Socket>();
            DBServerCancelToken = new CancellationTokenSource();
            for (int i = 0; i < Settings.Default.DBServerConnectCount; i++)
            {
                DBSocketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }
        }

        /// <summary>
        /// DB서버와의 연결을 시도합니다.
        /// 모든 소켓이 연결에 성공하면 DB서버와 연결되었다는 이벤트를 발생시킵니다.
        /// </summary>
        public void Start()
        {
            if (DBServerCancelToken.Token.IsCancellationRequested)
                return;
            DBServerTaskList.Add(ConnectToDBServer(Settings.Default.DBServerConnectCount));
            UpdateDBServerStatus(IsDBServerConnected());
        }

        /// <summary>
        /// DB서버와의 모든 로직을 종료시키고, 리소스를 정리합니다.
        /// 만약 Running 상태의 Task를 전부 대기하고 리소스를 정리합니다.
        /// </summary>
        /// <exception>
        /// Task.Status가 Wait For Activation 상태일 경우 Dispose가 정상적으로 작동하지 않습니다.
        /// </exception>
        public async void Stop()
        {
            List<Task> WaitForCompleteTaskList = new List<Task>();
            DBServerCancelToken.Cancel();
            // Cancel 시키고 바로 종료하면 Status가 바뀌지 않는다. 그래서 3초 대기
            await Task.Delay(3000);
            foreach(var DBTask in DBServerTaskList)
            {
                if(DBTask.Status == TaskStatus.Running)
                {
                    WaitForCompleteTaskList.Add(DBTask);
                    DBServerTaskList.Remove(DBTask);
                }
            }
            await LogManager.GetSingletone.WriteLog("DB서버와 실행중인 남은 Task 완료를 대기합니다.");
            Task.WaitAll(WaitForCompleteTaskList.ToArray());
            Dispose();
        }

        /// <summary>
        /// DB서버와 연결을 시도하는 메서드입니다.
        /// 만약 연결에 실패하면 지속적으로 연결을 재시도합니다.
        /// </summary>
        /// <param name="ConnectCount">
        /// 최대로 연결할 소켓의 갯수입니다.
        /// 이는 중도에 연결이 끊길경우 끊긴 소켓만큼 연결을 재시도할때도 사용됩니다.
        /// </param>
        /// <returns>
        /// 반환 값은 없습니다.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// 취소 프로토콜을 지원합니다. 취소가 요청되면 예외가 발생합니다.
        /// </exception>
        private async Task ConnectToDBServer(int ConnectCount)
        {
            int i = 0;
            for (; i < ConnectCount; i++)
            {
                try
                {
                    if (DBServerCancelToken.Token.IsCancellationRequested)
                        return;
                    await DBSocketList[i].ConnectAsync(new IPEndPoint(IPAddress.Parse(Settings.Default.DBServerIPAddress), Settings.Default.DBServerPort), DBServerCancelToken.Token);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    await LogManager.GetSingletone.WriteLog($"DB서버와 연결에 실패하였습니다. {i}번째 객체 시도중");
                    i--;
                }
                catch (OperationCanceledException)
                {
                    // 연결이 취소되었으므로 메서드를 종료합니다.
                    return;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await LogManager.GetSingletone.WriteErrorLog(e);
                }
            }
        }

        /// <summary>
        /// 정해진 소켓이 전부 연결되었는지 확인하는 메서드입니다.
        /// </summary>
        /// <returns>
        /// 전부 연결되었으면 true, 아니면 false를 반환합니다.
        /// </returns>
        private bool IsDBServerConnected()
        {
            foreach (var DBSocket in DBSocketList)
            {
                if (!DBSocket.Connected)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// UI 갱신용 이벤트를 발생시키는 메서드입니다.
        /// </summary>
        /// <param name="IsConnected">
        /// true면 연결되었다는 이벤트를 발생시키고, false면 연결이 끊겼다는 이벤트를 발생시킵니다.
        /// </param>
        private void UpdateDBServerStatus(bool IsConnected)
        {
            DBServerEvent?.Invoke(IsConnected);
        }
        /// <summary>
        /// 종료자입니다.
        /// 최후의 수단이며, 직접 사용은 절대하지 마세요.
        /// </summary>
        ~DBServer()
        {
            Dispose(false);
        }
        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Close 메서드를 사용하세요.
        /// </summary>
        public void Dispose()
        {
            if (IsAlreadyDisposed)
            {
                return;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Stop 메서드를 사용하세요.
        /// </summary>
        /// <seealso cref="Stop()"/>"/>
        protected virtual void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {

            }
            foreach (var DBSocket in DBSocketList)
            {
                DBSocket.Close();
            }
            foreach (var DBServerTask in DBServerTaskList)
            {
                if(DBServerTask.Status == TaskStatus.Canceled || DBServerTask.Status == TaskStatus.RanToCompletion || DBServerTask.Status == TaskStatus.Faulted)
                {
                    DBServerTask.Dispose();
                }
            }
            DBServerCancelToken.Dispose();
            IsAlreadyDisposed = true;
        }
    }
}
