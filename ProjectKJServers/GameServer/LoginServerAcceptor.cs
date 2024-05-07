using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using KYCUIEventManager;
using KYCLog;
using KYCSocketCore;
using System.Net.Sockets;
using KYCPacket;
using KYCException;
using Windows.Security.DataProtection;

namespace GameServer
{
    internal class LoginServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<LoginServerAcceptor> Lazy = new Lazy<LoginServerAcceptor>(() => new LoginServerAcceptor());
        public static LoginServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        TaskCompletionSource<bool>? ServerReadeyEvent;

        // 추후 파이프 라인 추가하자

        private LoginServerAcceptor() : base(GameServerSettings.Default.LoginServerAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start(TaskCompletionSource<bool> ServerEvent)
        {
            Init(IPAddress.Parse(GameServerSettings.Default.LoginServerIPAddress), GameServerSettings.Default.LoginServerAcceptPort);
            Start("LoginServer");
            ProcessCheck();
            GetRecvPacket();
            ServerReadeyEvent = ServerEvent;
        }

        public async Task Stop()
        {
            await Stop("LoginServer",TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            Dispose();
        }

        ~LoginServerAcceptor()
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

            }
            CheckCancelToken.Dispose();
            IsAlreadyDisposed = true;
            base.Dispose(Disposing);
        }

        private void ProcessCheck()
        {
            Task.Run(async () => {
               while(!CheckCancelToken.IsCancellationRequested)
                {
                     if(IsConnected())
                    {
                        UIEvent.GetSingletone.UpdateLoginServerStatus(true);
                        if (ServerReadeyEvent != null)
                        {
                            ServerReadeyEvent.SetResult(true);
                            ServerReadeyEvent = null;
                        }
                    }
                     else
                        UIEvent.GetSingletone.UpdateLoginServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckCancelToken.Token);
        }
        
        public void GetRecvPacket()
        {
            Task.Run( async() =>
            {
                while(!CheckCancelToken.IsCancellationRequested)
                {
                    try
                    {
                        var DataBuffer = await RecvData().ConfigureAwait(false);
                        // 파이프라인에 추가하는거 작업해야함
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 다시 연결을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e);
                        LogManager.GetSingletone.WriteLog("LoginServer와 연결이 끊겼습니다");
                        // 만약 서버가 죽은거라면 관련된 모든 소켓이 죽었을 것이기 때문에 모두 처음부터 다시 연결을 해야한다
                        PrepareToReAccept("LoginServer");
                        Start("LoginServer");
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음 1초 대기후 다시 소켓을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e);
                        LogManager.GetSingletone.WriteLog("LoginServerAcceptor에서 데이터를 Recv할 Socket이 부족하여 TimeOut이 되었습니다.");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // 그외 에러
                        LogManager.GetSingletone.WriteLog(e);
                    }
                }
            });
        }
        public async Task<int> Send(Memory<byte> Data)
        {
            return await SendData(Data).ConfigureAwait(false);
        }
    }
}
