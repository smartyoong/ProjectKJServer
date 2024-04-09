using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DBServer
{
    internal class LoginServerAcceptor : Acceptor, IDisposable
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
    }
}
