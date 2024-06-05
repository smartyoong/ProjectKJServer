using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ABI.System.Collections.Generic;
using KYCException;
using KYCInterface;
using KYCLog;
using KYCPacket;
using KYCSocketCore;
using KYCUIEventManager;

namespace GameServer
{
    internal class ClientAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<ClientAcceptor> Lazy = new Lazy<ClientAcceptor>(() => new ClientAcceptor());
        public static ClientAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        private ConcurrentDictionary<string, string> AuthHashAndNickNameDictionary = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<Socket, string> SocketNickNameDictionary = new ConcurrentDictionary<Socket, string>();
        private ConcurrentDictionary<string, Socket> NickNameSocketDictionary = new ConcurrentDictionary<string, Socket>();


        private ClientAcceptor() : base(GameServerSettings.Default.ClientAcceptCount, "GameServerClient")
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public new void Start()
        {
            Init(IPAddress.Any, GameServerSettings.Default.ClientAcceptPort);
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
            Task.Run(async () => {
                while (!CheckCancelToken.IsCancellationRequested)
                {
                    if (IsConnected())
                        UIEvent.GetSingletone.UpdateGameServerStatus(true);
                    else
                        UIEvent.GetSingletone.UpdateGameServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
                // 프로세스 체크가 종료되었다면 끊겼다고 한다 (주로 서버 종료시 발생)
                UIEvent.GetSingletone.UpdateGameServerStatus(false);
            }, CheckCancelToken.Token);
        }

        // 클라이언트를 Accept할 때에는 아래의 함수를 재정의 해야한다
        protected override void PushToPipeLine(Memory<byte> Data, Socket Sock)
        {
            // int로 형변환
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
        protected override void LogOut(Socket ClientSock)
        {
            var Addr = ClientSock.RemoteEndPoint is IPEndPoint RemoteEndPoint ? RemoteEndPoint.Address : IPAddress.Any;
            if (KeyValuePairs.TryRemove(ClientSock, out int ClientID))
            {
                if (ClientSocks.TryRemove(ClientID, out _))
                {
                    LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 로그아웃 하였습니다.");
                }
            }
            if(SocketNickNameDictionary.TryRemove(ClientSock, out string? NickName))
            {
                if(!string.IsNullOrEmpty(NickName))
                    NickNameSocketDictionary.TryRemove(NickName, out _);
            }

            UIEvent.GetSingletone.IncreaseUserCount(false);
            LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 연결이 끊겼습니다.");
            RemoveHashCodeBySocket(ClientSock);
            SocketManager.GetSingletone.ReturnSocket(ClientSock);
        }

        // 근데 로그인 서버랑 게임서버랑 ID가 다르잖아?
        // IP 기반으로 가야하나?
        public GeneralErrorCode AddHashCodeAndNickName(string NickName, string HashValue, int ClientID, string IPAddr)
        {
            if(AuthHashAndNickNameDictionary.ContainsKey(NickName))
                return GeneralErrorCode.ERR_HASH_CODE_NICKNAME_DUPLICATED;
            if(AuthHashAndNickNameDictionary.TryAdd(NickName, HashValue))
            {
                return GeneralErrorCode.ERR_AUTH_SUCCESS;
            }
            return GeneralErrorCode.ERR_AUTH_FAIL;
        }

        public GeneralErrorCode GetAuthHashCode(string NickName, ref string HashCode)
        {
            if (!AuthHashAndNickNameDictionary.ContainsKey(NickName))
                return GeneralErrorCode.ERR_HASH_CODE_IS_NOT_REGIST;

            string? Value;
            //Success가 아니면 HashCode는 null이다.
            if (AuthHashAndNickNameDictionary.TryGetValue(NickName, out Value))
            {
                HashCode = Value ?? "NONEHASH";
                return GeneralErrorCode.ERR_AUTH_SUCCESS;
            }
            else
                return GeneralErrorCode.ERR_AUTH_FAIL;
        }

        public void RemoveHashCodeBySocket(Socket Sock)
        {
            string? NickName;
            if(SocketNickNameDictionary.TryGetValue(Sock,out NickName))
            {
                if(!string.IsNullOrEmpty(NickName))
                    AuthHashAndNickNameDictionary.TryRemove(NickName, out _);
            }
        }
        public void RemoveHashCodeByNickName(string NickName)
        {
            AuthHashAndNickNameDictionary.TryRemove(NickName, out _);
        }

        public Socket? GetClientSocketByNickName(string NickName)
        {
            if (NickNameSocketDictionary.TryGetValue(NickName, out Socket? Sock))
                return Sock;
            return null;
        }

        public string GetNickNameByClientSocket(Socket Sock)
        {
            if (SocketNickNameDictionary.TryGetValue(Sock, out string? NickName))
                return NickName;
            return string.Empty;
        }

        // 이건 추후 클라가 해시 인증 성공하면 매핑하자 
        public void MapSocketNickName(Socket Sock, string NickName)
        {
            SocketNickNameDictionary.TryAdd(Sock, NickName);
            NickNameSocketDictionary.TryAdd(NickName, Sock);
        }
        public void KickClient(Socket Sock)
        {
            string Addr = GetIPAddrByClientSocket(Sock);
            if (KeyValuePairs.TryRemove(Sock, out int ClientID))
            {
                if (ClientSocks.TryRemove(ClientID, out _))
                {
                    LogManager.GetSingletone.WriteLog($"클라이언트 {Addr} {GetPortByClientSocket(Sock)}이 강제 추방되었습니다.");
                    ClientsSocksAddr.TryRemove($"{Addr}{GetPortByClientSocket(Sock)}", out _);
                }
            }
            if (SocketNickNameDictionary.TryRemove(Sock, out string? NickName))
            {
                if (!string.IsNullOrEmpty(NickName))
                    NickNameSocketDictionary.TryRemove(NickName, out _);
            }
            UIEvent.GetSingletone.IncreaseUserCount(false);
            LogManager.GetSingletone.WriteLog($"클라이언트 {Addr}이 연결을 끊었습니다.");
            SocketManager.GetSingletone.ReturnSocket(Sock);
        }

        public void KickClientByID(int ClientID)
        {
            KickClient(GetClientSocket(ClientID)!);
        }
    }
}
