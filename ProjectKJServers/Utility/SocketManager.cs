using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KYCLog;
namespace KYCSocketCore
{
    public class SocketManager : IDisposable, IEnumerable<Socket>
    {
        private Queue<Socket> AvailableSockets = new Queue<Socket>();
        private SemaphoreSlim AvailableSocketSync;
        private CancellationTokenSource SocketManagerCancelToken;
        private bool IsAlreadyDisposed = false;
        private int MaximumSocketCount;
        private bool IsReadyToUse = false;

        public SocketManager(int MaxSocketCount, bool HasResponsibility)
        {

            MaximumSocketCount = MaxSocketCount;

            AvailableSocketSync = new SemaphoreSlim(MaxSocketCount);
            SocketManagerCancelToken = new CancellationTokenSource();
            if (HasResponsibility)
            {
                for (int i = 0; i < MaxSocketCount; i++)
                {
                    AvailableSockets.Enqueue(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                }
                IsReadyToUse = true;
            }
        }

        public void AddSocket(Socket Socket)
        {
            if (AvailableSockets.Count >= MaximumSocketCount || IsReadyToUse)
            {
                throw new InvalidOperationException("소켓 매니저에 추가할 수 있는 소켓의 개수를 초과했습니다.");
            }

            lock (AvailableSockets)
            {
                AvailableSockets.Enqueue(Socket);
            }

            if (AvailableSockets.Count == MaximumSocketCount)
            {
                IsReadyToUse = true;
            }
        }

        public async Task<Socket> GetAvailableSocket()
        {
            if (!IsReadyToUse)
            {
                throw new InvalidOperationException("소켓 매니저가 아직 사용할 준비가 되지 않았습니다.");
            }

            await AvailableSocketSync.WaitAsync(SocketManagerCancelToken.Token).ConfigureAwait(false);
            lock (AvailableSockets)
            {
                return AvailableSockets.Dequeue();
            }
        }

        public void ReturnSocket(Socket Socket)
        {
            if (AvailableSockets.Count >= MaximumSocketCount)
            {
                throw new InvalidOperationException("반납하는 소켓의 갯수가 소켓 매니저의 최대치를 넘습니다.");
            }
            lock (AvailableSockets)
            {
                AvailableSockets.Enqueue(Socket);
            }
            AvailableSocketSync.Release();
        }

        public int GetCount()
        {
            return AvailableSockets.Count;
        }

        public IEnumerator<Socket> GetEnumerator()
        {
            return AvailableSockets.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool CanReturnSocket()
        {
            return AvailableSockets.Count < MaximumSocketCount;
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
