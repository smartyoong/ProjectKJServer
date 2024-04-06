using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    internal class LoginServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<LoginServerAcceptor> Lazy = new Lazy<LoginServerAcceptor>(() => new LoginServerAcceptor());
        public static LoginServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private LoginServerAcceptor()
        {

        }

        public void Start()
        {
            Init(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }), 11000);
            base.Start();
            // UI 갱신하는 부분을 생각해보자
            UIEvent.GetSingletone.UpdateLoginServerStatus(IsConnected());
        }

        public void Stop()
        {
            base.Stop();
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
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
        }
    }
}
