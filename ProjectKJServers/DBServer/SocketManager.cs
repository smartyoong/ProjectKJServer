using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    internal class SocketManager : IDisposable
    {
        private Queue<Socket> AvailableSockets = new Queue<Socket>();
        private SemaphoreSlim AvailableSocketSync;
        private CancellationTokenSource SocketManagerCancelToken;
        private bool IsAlreadyDisposed = false;
        private int MaxCount = 0;

        public SocketManager(int MaxSocketCount)
        {
            AvailableSocketSync = new SemaphoreSlim(MaxSocketCount);
            SocketManagerCancelToken = new CancellationTokenSource();
        }

        public void AddSocket(Socket Socket)
        {
            AvailableSockets.Enqueue(Socket);
            MaxCount++;
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

        public int GetMaxCount()
        {
            return MaxCount;
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
