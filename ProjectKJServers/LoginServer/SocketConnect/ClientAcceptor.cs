using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LoginServer.Properties;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using CoreUtility.SocketCore;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using LoginServer.PacketPipeLine;

namespace LoginServer.SocketConnect
{
    internal class ClientAcceptor : Acceptor, IDisposable
    {
        //private static readonly Lazy<ClientAcceptor> Lazy = new Lazy<ClientAcceptor>(() => new ClientAcceptor());
        //public static ClientAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        private ConcurrentDictionary<Socket, string> SocketAccountIDDictionary = new ConcurrentDictionary<Socket, string>();

        public ClientAcceptor() : base(Settings.Default.ClientAcceptCount, "LoginServerClient")
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public new void Start()
        {
            Init(IPAddress.Any, Settings.Default.ClientAcceptPort);
            base.Start();
            ProcessCheck();
        }

        public async Task Stop()
        {
            await Stop(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            CheckCancelToken.Cancel();
            Dispose();
        }

        ~ClientAcceptor()
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
            IsAlreadyDisposed = true;
            base.Dispose(Disposing);
        }

        private void ProcessCheck()
        {
            Task.Run(async () =>
            {
                while (!CheckCancelToken.IsCancellationRequested)
                {
                    if (IsConnected())
                        UIEvent.GetSingletone.UpdateLoginServerStatus(true);
                    else
                        UIEvent.GetSingletone.UpdateLoginServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
                // 프로세스 체크가 종료되었다면 끊겼다고 한다 (주로 서버 종료시 발생)
                UIEvent.GetSingletone.UpdateLoginServerStatus(false);
            }, CheckCancelToken.Token);
        }

        // 클라이언트를 Accept할 때에는 아래의 함수를 재정의 해야한다
        protected override void PushToPipeLine(Memory<byte> Data, Socket Sock)
        {
            ClientRecvPacketPipeline.GetSingletone.PushToPacketPipeline(Data, GetClientID(Sock));
        }

        protected override void PushToPipeLine(Memory<byte> Data)
        {
            throw new NotImplementedException();
        }

        public async Task<int> Send(Socket ClientSocket, Memory<byte> Data)
        {
            try
            {
                return await SendClientData(Data, ClientSocket).ConfigureAwait(false);
            }
            catch (ConnectionClosedException)
            {
                return -1;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                return -1;
            }
        }

        public string MakeAuthHashCode(string AccountID, int ClientID)
        {
            string Addr = GetIPAddrByClientID(ClientID);
            string Port = GetPortByClientID(ClientID).ToString();

            if (string.IsNullOrEmpty(Addr))
                return string.Empty;

            SHA256 Secret = SHA256.Create();
            byte[] HashValue = Secret.ComputeHash(Encoding.UTF8.GetBytes(AccountID + Addr + Port));
            StringBuilder StringMaker = new StringBuilder();
            foreach (var v in HashValue)
            {
                StringMaker.Append(v.ToString("x2"));
            }
            return StringMaker.ToString();
        }

        protected override void LogOut(Socket ClientSock)
        {
            var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
            int Port = ClientSock.RemoteEndPoint is IPEndPoint RemotePort ? RemotePort.Port : 0;
            if (KeyValuePairs.TryRemove(ClientSock, out int ClientID))
            {
                if (ClientSocks.TryRemove(ClientID, out _))
                {
                    LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 로그아웃 하였습니다.");
                }
            }
            if (SocketAccountIDDictionary.TryRemove(ClientSock, out string? AccountID))
            {
                if (!string.IsNullOrEmpty(AccountID))
                    LogManager.GetSingletone.WriteLog($"클라이언트 {AccountID}가 로그아웃 했습니다.");
            }

            UIEvent.GetSingletone.IncreaseUserCount(false);
            LogManager.GetSingletone.WriteLog($"클라이언트 {Addr} {Port}이 연결이 끊겼습니다.");
            SocketManager.GetSingletone.ReturnSocket(ClientSock);
        }

        public Socket? GetClientSocketByAccountID(string AccountID)
        {
            // 이 메서드는 탐색 속도가 느립니다.
            Socket Sock = SocketAccountIDDictionary.FirstOrDefault(x => x.Value == AccountID).Key;
            if (Sock != null)
                return Sock;
            return null;
        }

        // 이건 추후 클라가 해시 인증 성공하면 매핑하자 
        public void MappingSocketAccountID(Socket Sock, string AccountID)
        {
            if (Sock == null)
            {
                LogManager.GetSingletone.WriteLog("MappingSocket이 null입니다.");
                return;
            }
            SocketAccountIDDictionary.TryAdd(Sock, AccountID);
        }

        public void KickClient(Socket Sock)
        {
            string Addr = GetIPAddrByClientSocket(Sock);
            int Port = Sock.RemoteEndPoint is IPEndPoint RemotePort ? RemotePort.Port : 0;
            if (KeyValuePairs.TryRemove(Sock, out int ClientID))
            {
                if (ClientSocks.TryRemove(ClientID, out _))
                {
                    LogManager.GetSingletone.WriteLog($"클라이언트 {Addr} {GetPortByClientSocket(Sock)}이 강제 추방되었습니다.");
                }
            }
            if (SocketAccountIDDictionary.TryRemove(Sock, out string? AccountID))
            {
                if (!string.IsNullOrEmpty(AccountID))
                    LogManager.GetSingletone.WriteLog($"클라이언트 {AccountID}가 강제 추방되었습니다.");
            }
            UIEvent.GetSingletone.IncreaseUserCount(false);
            LogManager.GetSingletone.WriteLog($"클라이언트 {Addr} {Port}의 연결을 끊었습니다.");
            Sock.Close(); // 강제로 Close 시켰으므로, 재사용 소켓에 안돌아간다.
        }

        public void KickClientByID(int ClientID)
        {
            KickClient(GetClientSocket(ClientID)!);
        }

    }
}
