using System.Net;
using System.Net.Sockets;

namespace DBServer
{
    internal class Acceptor : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        private List<Socket> ClientSocketList;

        private CancellationTokenSource AcceptCancelToken;

        private List<Task> TryAcceptTaskList = new List<Task>();

        private Socket ListenSocket;

        private IPAddress AllowSpecificIP = IPAddress.Any;

        protected Acceptor()
        {
            AcceptCancelToken = new CancellationTokenSource();
            ClientSocketList = new List<Socket>();
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

            if (ClientSocketList != null)
            {
                foreach (var Socket in ClientSocketList)
                {
                    Socket.Close();
                }
                ClientSocketList.Clear();
            }

            AcceptCancelToken.Dispose();

            ListenSocket.Close();

            IsAlreadyDisposed = true;
        }

        protected virtual void Init(IPAddress AlloweSpecificIP ,int PortNumber)
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

        protected virtual void Start(string ServerName, int MaxAcceptCount)
        {
            TryAcceptTaskList.Add(Task.Run(async () =>
            {
                for(int i=0; i < MaxAcceptCount; i++)
                {
                    await LogManager.GetSingletone.WriteLog($"{ServerName}의 {i + 1}번째 연결을 대기합니다.").ConfigureAwait(false);

                    if(AcceptCancelToken.Token.IsCancellationRequested)
                    {
                        LogManager.GetSingletone.WriteLog($"{ServerName}와 연결이 취소되었습니다.").Wait();
                        break;
                    }
                    try
                    {
                        Socket ClientSocket = await ListenSocket.AcceptAsync();
                        if (CheckIsAllowedIP(ClientSocket))
                        {
                            ClientSocketList.Add(ClientSocket);
                            await LogManager.GetSingletone.WriteLog($"{ServerName}랑 {i + 1}개 연결되었습니다.").ConfigureAwait(false);
                        }
                        else
                        {
                            ClientSocket.Close();
                            i--;
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        i--;
                        LogManager.GetSingletone.WriteLog(e.Message).Wait();
                    }
                }
            }));
        }

        protected virtual async Task Stop(string ServerName, TimeSpan DelayTime)
        {
            await CancelAccept(DelayTime).ConfigureAwait(false);
            await CleanUpAcceptTask(ServerName).ConfigureAwait(false);
        }

        public virtual bool IsConnected()
        {
            if (ClientSocketList.Count == 0 || ClientSocketList == null)
            {
                return false;
            }

            foreach (var Socket in ClientSocketList)
            {
                if (!Socket.Connected || Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0)
                {
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
            List<Task> RunningTasks = TryAcceptTaskList.Where(task => task.Status == TaskStatus.Running).ToList();
            TryAcceptTaskList = TryAcceptTaskList.Except(RunningTasks).ToList();
            await LogManager.GetSingletone.WriteLog($"{ServerName}와 실행중인 남은 Task 완료를 대기합니다.").ConfigureAwait(false);
            await Task.WhenAll(RunningTasks).ConfigureAwait(false);
        }
        protected virtual void Process(Socket ClientSocket)
        {
            throw new NotImplementedException();
        }

        private bool CheckIsAllowedIP(Socket ClientSocket)
        {
            if(AllowSpecificIP == IPAddress.Any)
            {
                return true;
            }
            if(ClientSocket.RemoteEndPoint is IPEndPoint RemoteEndPoint)
            {
                if(RemoteEndPoint.Address.Equals(AllowSpecificIP))
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
    }
}
