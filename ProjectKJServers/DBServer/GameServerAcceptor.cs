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

namespace DBServer
{
    internal class GameServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<GameServerAcceptor> Lazy = new Lazy<GameServerAcceptor>(() => new GameServerAcceptor());
        public static GameServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;


        private GameServerAcceptor() : base(DBServerSettings.Default.GameServerAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start()
        {
            Init(IPAddress.Parse(DBServerSettings.Default.GameServerIPAdress), DBServerSettings.Default.GameServerAcceptPort);
            Start("Server");
            ProcessCheck();
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
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
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

        public void GetRecvPacket()
        {
            Task.Run( async() =>
            {
                while(!CheckCancelToken.IsCancellationRequested)
                {
                    try
                    {
                        byte[] DataBuffer = await RecvData().ConfigureAwait(false);
                    }
                    catch (ArgumentException e)
                    {
                        // ID 매개변수 불일치
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 어캐할까?
                        LogManager.GetSingletone.WriteLog(e).Wait();
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음
                        LogManager.GetSingletone.WriteLog(e).Wait();
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
