﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KYCException;
using KYCInterface;
using KYCLog;
using KYCPacket;
using KYCSocketCore;
using KYCUIEventManager;
using LoginServer.Properties;
using System.Security.Cryptography;

namespace LoginServer
{
    internal class ClientAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<ClientAcceptor> Lazy = new Lazy<ClientAcceptor>(() => new ClientAcceptor());
        public static ClientAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;


        private ClientAcceptor() : base(Settings.Default.ClientAcceptCount, "LoginServerClient")
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
            Task.Run(async () => {
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

        public string MakeAuthHashCode(string NickName, int ClientID)
        {
            string Addr = GetIPAddrByClientID(ClientID);

            if(string.IsNullOrEmpty(Addr))
                return string.Empty;

            SHA256 Secret = SHA256.Create();
            byte[] HashValue = Secret.ComputeHash(Encoding.UTF8.GetBytes(NickName + Addr));
            StringBuilder StringMaker = new StringBuilder();
            foreach(var v in HashValue)
            {
                StringMaker.Append(v.ToString("x2"));
            }
            return StringMaker.ToString();
        }

    }
}
