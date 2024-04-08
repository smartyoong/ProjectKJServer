using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    // 유저는 개개인별로 Socket을 들고있게하면되는데,,,
    // 음,,, Acceptor는 어캐해야하냐
    // GeyAvailableSocket () = await AcceptAsync?? 이렇게 사용해봐?
    internal class SocketManager : IDisposable
    {
        private Queue<Socket> AvailableSockets = new Queue<Socket>();
        private SemaphoreSlim AvailableSocketSync;
        private CancellationTokenSource SocketManagerCancelToken;
        private bool IsAlreadyDisposed = false;

        public SocketManager(int MaxSocketCount)
        {
            AvailableSocketSync = new SemaphoreSlim(MaxSocketCount);
            SocketManagerCancelToken = new CancellationTokenSource();
            for (int i = 0; i < MaxSocketCount; i++)
            {
                AvailableSockets.Enqueue(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }
        }

        public async Task<Socket> GetAvailableSocket()
        {
            await AvailableSocketSync.WaitAsync(SocketManagerCancelToken.Token).ConfigureAwait(false);
            lock (AvailableSockets)
            {
                return AvailableSockets.Dequeue();
            }
        }

        public void ReturnSocket(Socket Socket)
        {
            lock (AvailableSockets)
            {
                AvailableSockets.Enqueue(Socket);
            }
            AvailableSocketSync.Release();
        }


        public int GetCurrentCount()
        {
            return AvailableSockets.Count;
        }

        public void Dispose()
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
                AvailableSocketSync.Dispose();
            }
            SocketManagerCancelToken.Dispose();

            foreach (var Socket in AvailableSockets)
            {
                Socket.Close();
            }
            AvailableSockets.Clear();
        }
        public async Task Cancel()
        {
            SocketManagerCancelToken.Cancel();
            LogManager.GetSingletone.WriteLog("소켓 매니저를 종료합니다.").Wait();
            await Task.Delay(3000).ConfigureAwait(false);
            Dispose();
        }

        ~SocketManager()
        {
            Dispose(false);
        }
    }
}
