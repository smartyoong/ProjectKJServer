﻿using System.Net;
using System.Net.Sockets;
using System.Threading;
using PacketUtility;

namespace DBServer
{
    internal class Acceptor : IDisposable
    {
        private bool IsAlreadyDisposed = false;

        private SocketManager ClientSocketList;

        private CancellationTokenSource AcceptCancelToken;

        private List<Task> TryAcceptTaskList = new List<Task>();

        private Socket ListenSocket;

        private IPAddress AllowSpecificIP = IPAddress.Any;

        private int MaxAcceptCount;

        protected Acceptor(int MaxAcceptCount)
        {
            AcceptCancelToken = new CancellationTokenSource();
            ClientSocketList = new SocketManager(MaxAcceptCount,false);
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

        protected virtual void Start(string ServerName)
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
                            ClientSocketList.AddSocket(ClientSocket);
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
            await ClientSocketList.Cancel().ConfigureAwait(false);
            await CleanUpAcceptTask(ServerName).ConfigureAwait(false);
        }

        public virtual bool IsConnected()
        {
            if (ClientSocketList.GetCount() == 0 || ClientSocketList == null)
            {
                return false;
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
        protected virtual async Task<byte[]> RecvData()
        {
            while(!AcceptCancelToken.Token.IsCancellationRequested)
            {
                Socket? RecvSocket = null;
                try
                {
                    RecvSocket = await ClientSocketList.GetAvailableSocket().ConfigureAwait(false);
                    RecvSocket.ReceiveTimeout = 500;
                    byte[] DataSizeBuffer = new byte[sizeof(int)];
                    await RecvSocket.ReceiveAsync(DataSizeBuffer,AcceptCancelToken.Token).ConfigureAwait(false);
                    byte[] DataBuffer = new byte[PacketUtils.GetSizeFromPacket(DataSizeBuffer)];
                    await RecvSocket.ReceiveAsync(DataBuffer, AcceptCancelToken.Token).ConfigureAwait(false);
                    return DataBuffer;

                    // 이 아래부터는 어떻게 할지 생각해보자
                }
                catch(SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                {
                    byte[] DataSizeBuffer = new byte[sizeof(int)];
                    return DataSizeBuffer;
                }
                catch(Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e.Message).Wait();
                    byte[] DataSizeBuffer = new byte[sizeof(int)];
                    return DataSizeBuffer;
                }
                finally
                {
                    if (RecvSocket != null && ClientSocketList.CanReturnSocket())
                        ClientSocketList.ReturnSocket(RecvSocket);
                    else if (RecvSocket != null && !ClientSocketList.CanReturnSocket())
                        RecvSocket.Close();
                }
            }
            return new byte[0];
        }
    }
}
