﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KYCException;
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


        private ClientAcceptor() : base(GameServerSettings.Default.ClientAcceptCount)
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start()
        {
            Init(IPAddress.Any, GameServerSettings.Default.ClientAcceptPort);
            Start("Client");
            ProcessCheck();
        }

        public async Task Stop()
        {
            await Stop("Client", TimeSpan.FromSeconds(3)).ConfigureAwait(false);
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
        protected override async Task Process(Socket ClientSocket)
        {
            while(!CheckCancelToken.IsCancellationRequested)
            {
                try
                {
                    var Data = await RecvClientData(ClientSocket).ConfigureAwait(false);
                    ClientRecvPacketPipeline.GetSingletone.PushToPacketPipeline(Data, ClientSocket);
                }
                catch (ConnectionClosedException e)
                {
                    LogManager.GetSingletone.WriteLog(e.Message);
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog(e);
                    break;
                }
            }
        }

        public async Task<int> Send(Socket ClientSocket, Memory<byte> Data)
        {
            return await SendClientData(Data, ClientSocket).ConfigureAwait(false);
        }

    }
}
