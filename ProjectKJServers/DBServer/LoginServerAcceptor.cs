using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using UIEventManager;
using AcceptUtility;
using Utility;
using LogUtility;
using System.Net.Sockets;
using PacketUtility;

namespace DBServer
{
    internal class LoginServerAcceptor : Acceptor, IDisposable, IPacketProcess
    {
        private static readonly Lazy<LoginServerAcceptor> Lazy = new Lazy<LoginServerAcceptor>(() => new LoginServerAcceptor());
        public static LoginServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;


        private LoginServerAcceptor() : base(DBServerSettings.Default.LoginServerAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start()
        {
            Init(IPAddress.Parse(DBServerSettings.Default.LoginServerIPAdress), DBServerSettings.Default.LoginServerAcceptPort);
            Start("LoginServer");
            ProcessCheck();
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
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
        }

        private void ProcessCheck()
        {
            Task.Run(async () => {
               while(!CheckCancelToken.IsCancellationRequested)
                {
                     if(IsConnected())
                        UIEvent.GetSingletone.UpdateLoginServerStatus(true);
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
                        byte[] DataBuffer = await RecvData().ConfigureAwait(false);
                    }
                    catch (ArgumentException e)
                    {
                        // ID 매개변수 불일치
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 어캐할까?
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음
                    }
                    catch (Exception e)
                    {
                        // 그외 에러
                    }
                }
            });
        }
    }
}
