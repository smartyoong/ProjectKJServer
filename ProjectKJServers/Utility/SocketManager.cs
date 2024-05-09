using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KYCLog;
using CoreUtility;
using System.Text.RegularExpressions;
namespace KYCSocketCore
{

    public class SocketManager : IDisposable
    {
        // 특별히 그룹화해서 관리가 필요한 소켓들을 묶기 위한 클래스 입니다.
        // 모든 소켓의 Close 소켓 매니저가 진행하므로 걱정할 필요가 없습니다.
        public class SocketGroup : IEnumerable<Socket>
        {
            // 가장 최근에 사용한 소켓은 페이징 아웃되지 않을 가능성이 높기에 스택을 사용한다
            public Stack<Socket> AvailableMemberSockets = new Stack<Socket>();
            public SemaphoreSlim Sync = new SemaphoreSlim(0, CoreSettings.Default.MaxSocketCountPerGroup);
            public ReaderWriterLockSlim ReadWriteLock = new ReaderWriterLockSlim();
            public IEnumerator<Socket> GetEnumerator()
            {
                return AvailableMemberSockets.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            public List<Socket> GetSockets()
            {
                ReadWriteLock.EnterReadLock();
                try
                {
                    return AvailableMemberSockets.ToList();
                }
                finally
                {
                    ReadWriteLock.ExitReadLock();
                }
            }
        }

        // 가장 최근에 사용한 소켓은 페이징 아웃되지 않을 가능성이 높기에 스택을 사용한다
        private Stack<Socket> AvailableSockets = new Stack<Socket>();
        private CancellationTokenSource SocketManagerCancelToken;
        private bool IsAlreadyDisposed = false;
        static Lazy<SocketManager> Instance = new Lazy<SocketManager>(() => new SocketManager());
        private List<Socket> Sockets = new List<Socket>();
        private List<SocketGroup> Groups = new List<SocketGroup>();

        public static SocketManager GetSingletone { get { return Instance.Value; } }

        private SocketManager()
        {
            SocketManagerCancelToken = new CancellationTokenSource();
            for (int i = 0; i < CoreSettings.Default.ReadySocketCount; i++)
            { 
                var Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                AvailableSockets.Push(Sock);
                Sockets.Add(Sock);
            }
        }

        public Socket BorrowSocket()
        {
            lock (AvailableSockets)
            {
                if (AvailableSockets.Count == 0)
                {
                    var Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Sockets.Add(Sock);
                    return Sock;
                }
                else
                    return AvailableSockets.Pop();
            }
        }

        public void ReturnSocket(Socket Socket)
        {
            if(Socket.Connected)
                Socket.Disconnect(true);
            lock (AvailableSockets)
            {
                AvailableSockets.Push(Socket);
            }
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
                foreach (var Group in Groups)
                {
                    Group.Sync.Dispose();
                }
            }
            SocketManagerCancelToken.Dispose();

            foreach (var Socket in Sockets)
            {
                Socket.Close();
            }
            AvailableSockets.Clear();
            Sockets.Clear();
            Groups.Clear();
        }

        public async Task Cancel()
        {
            SocketManagerCancelToken.Cancel();
            LogManager.GetSingletone.WriteLog("소켓 매니저를 종료합니다.");
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            Dispose();
        }

        ~SocketManager()
        {
            Dispose(false);
        }

        public int MakeNewSocketGroup(Socket Sock)
        {
            var NewGroup = new SocketGroup();
            NewGroup.AvailableMemberSockets.Push(Sock);
            NewGroup.Sync.Release();
            Groups.Add(NewGroup);
            return Groups.Count - 1;
        }

        public void RemoveGroup(int GroupID)
        {
            if (!IsAlreadyGroup(GroupID))
            {
                throw new IndexOutOfRangeException($"GetAvailableSocketFromGroup {GroupID}번 그룹이 존재하지 않습니다.");
            }
            lock (Groups)
                Groups.RemoveAt(GroupID);
        }

        public bool IsAlreadyGroup(int GroupID)
        {
            if (GroupID < 0)
                return false;
            return Groups.Count > GroupID;
        }

        public SocketGroup GetSocketGroup(int GroupID)
        {
            if (!IsAlreadyGroup(GroupID))
            {
                throw new IndexOutOfRangeException($"GetAvailableSocketFromGroup {GroupID}번 그룹이 존재하지 않습니다.");
            }

            return Groups[GroupID];
        }

        public void AddSocketToGroup(int GroupID, Socket Sock)
        {
            if (!IsAlreadyGroup(GroupID))
            {
                throw new IndexOutOfRangeException($"GetAvailableSocketFromGroup {GroupID}번 그룹이 존재하지 않습니다.");
            }

            if (Groups[GroupID].AvailableMemberSockets.Count == CoreSettings.Default.MaxSocketCountPerGroup)
            {
                LogManager.GetSingletone.WriteLog($"{GroupID}번 그룹에 소켓을 추가할 수 없습니다. 그룹이 가득 찼습니다.");
                return;
            }

            Groups[GroupID].AvailableMemberSockets.Push(Sock);

            Groups[GroupID].Sync.Release();
        }

        public void RemoveSocketFromGroup(int GroupID, Socket Sock)
        {
            if (!IsAlreadyGroup(GroupID))
            {
                throw new IndexOutOfRangeException($"GetAvailableSocketFromGroup {GroupID}번 그룹이 존재하지 않습니다.");
            }
            Groups[GroupID].AvailableMemberSockets = new Stack<Socket>(Groups[GroupID].AvailableMemberSockets.Where(x => x != Sock));
        }


        public async Task<Socket> GetAvailableSocketFromGroup(int GroupID)
        {
            if (!IsAlreadyGroup(GroupID))
            {
                throw new IndexOutOfRangeException($"GetAvailableSocketFromGroup {GroupID}번 그룹이 존재하지 않습니다.");
            }
            try
            {
                await Groups[GroupID].Sync.WaitAsync(SocketManagerCancelToken.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                throw;
            }
            return Groups[GroupID].AvailableMemberSockets.Pop();
        }


        public bool IsGroupConnected(int GroupID)
        {
            // 아직 그룹이 할당되지 않았다.
            if (GroupID < 0)
                return false;
            try
            {
                foreach (var Socket in GetSocketGroup(GroupID).GetSockets())
                {
                    if (!Socket.Connected || Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0)
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                return false;
            }
            return true;
        }
    }
}
