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

namespace DBServer
{
    internal class GameServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<GameServerAcceptor> Lazy = new Lazy<GameServerAcceptor>(() => new GameServerAcceptor());
        public static GameServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        private GameServerRecvPacketPipeline RecvProcessor;


        private GameServerAcceptor() : base(DBServerSettings.Default.GameServerAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
            RecvProcessor = new GameServerRecvPacketPipeline();
        }

        public void Start()
        {
            Init(IPAddress.Parse(DBServerSettings.Default.GameServerIPAdress), DBServerSettings.Default.GameServerAcceptPort);
            Start("GameServer");
            ProcessCheck();
            GetRecvPacket();
        }

        public async Task Stop()
        {
            await Stop("GameServer",TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            Dispose();
        }

        ~GameServerAcceptor()
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
                        UIEvent.GetSingletone.UpdateGameServerStatus(true);
                     else
                        UIEvent.GetSingletone.UpdateGameServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckCancelToken.Token);
        }
        
        // 이하 메서드는 추가 작업이 필요하며, 에러가 발생할 수 있다
        public void GetRecvPacket()
        {
            Task.Run( async() =>
            {
                while(!CheckCancelToken.IsCancellationRequested)
                {
                    if (!IsAccepted)
                        return;
                    try
                    {
                        byte[] DataBuffer = await RecvData().ConfigureAwait(false);
                        if (DataBuffer == null)
                            continue;
                        RecvProcessor.PushToPacketPipeline(DataBuffer);
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 다시 연결을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e).Wait();
                        LogManager.GetSingletone.WriteLog("GameServer와 연결이 끊겼습니다").Wait();
                        // 만약 서버가 죽은거라면 관련된 모든 소켓이 죽었을 것이기 때문에 모두 처음부터 다시 연결을 해야한다
                        PrepareToReAccept("GameServer");
                        Start("GameServer");
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음 1초 대기후 다시 소켓을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e).Wait();
                        LogManager.GetSingletone.WriteLog("GameServerAcceptor에서 데이터를 Recv할 Socket이 부족하여 TimeOut이 되었습니다.").Wait();
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // 그외 에러
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                }
            });
        }
    }
}
