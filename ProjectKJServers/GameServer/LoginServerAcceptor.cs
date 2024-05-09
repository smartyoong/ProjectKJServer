﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using KYCUIEventManager;
using KYCLog;
using KYCSocketCore;
using System.Net.Sockets;
using KYCPacket;
using KYCException;
using Windows.Security.DataProtection;

namespace GameServer
{
    internal class LoginServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<LoginServerAcceptor> Lazy = new Lazy<LoginServerAcceptor>(() => new LoginServerAcceptor());
        public static LoginServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        TaskCompletionSource<bool>? ServerReadeyEvent;

        // 추후 파이프 라인 추가하자

        private LoginServerAcceptor() : base(GameServerSettings.Default.LoginServerAcceptCount, "LoginServer")
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start(TaskCompletionSource<bool> ServerEvent)
        {
            Init(IPAddress.Parse(GameServerSettings.Default.LoginServerIPAddress), GameServerSettings.Default.LoginServerAcceptPort);
            Start();
            ProcessCheck();
            ServerReadeyEvent = ServerEvent;
        }

        public async Task Stop()
        {
            await Stop(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
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
            IsAlreadyDisposed = true;
            base.Dispose(Disposing);
        }

        private void ProcessCheck()
        {
            Task.Run(async () => {
               while(!CheckCancelToken.IsCancellationRequested)
                {
                     if(IsConnected())
                    {
                        UIEvent.GetSingletone.UpdateLoginServerStatus(true);
                        if (ServerReadeyEvent != null)
                        {
                            ServerReadeyEvent.SetResult(true);
                            ServerReadeyEvent = null;
                        }
                    }
                     else
                        UIEvent.GetSingletone.UpdateLoginServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckCancelToken.Token);
        }

        protected override void PushToPipeLine(Memory<byte> Data)
        {
            throw new NotImplementedException();
        }

        protected override void PushToPipeLine(Memory<byte> Data, Socket Sock)
        {
            ClientRecvPacketPipeline.GetSingletone.PushToPacketPipeline(Data, GetClientID(Sock));
        }

        public async Task<int> Send(Memory<byte> Data)
        {
            try
            {
                return await SendData(Data).ConfigureAwait(false);
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
    }
}
