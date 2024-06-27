using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Windows.Security.DataProtection;
using CoreUtility.SocketCore;
using CoreUtility.GlobalVariable;
using DBServer.PacketPipeLine;

namespace DBServer.SocketConnect
{
    internal class GameServerAcceptor : Acceptor, IDisposable
    {
        private static readonly Lazy<GameServerAcceptor> Lazy = new Lazy<GameServerAcceptor>(() => new GameServerAcceptor());
        public static GameServerAcceptor GetSingletone { get { return Lazy.Value; } }

        private bool IsAlreadyDisposed = false;

        private CancellationTokenSource CheckCancelToken;

        TaskCompletionSource<bool>? ServerReadeyEvent;

        private GameServerAcceptor() : base(DBServerSettings.Default.GameServerAcceptCount, "GameServer")
        {
            CheckCancelToken = new CancellationTokenSource();
        }

        public void Start(TaskCompletionSource<bool> ServerEvent)
        {
            Init(IPAddress.Parse(DBServerSettings.Default.GameServerIPAdress), DBServerSettings.Default.GameServerAcceptPort);
            base.Start();
            ProcessCheck();
            ServerReadeyEvent = ServerEvent;
        }

        public async Task Stop()
        {
            await Stop(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            Dispose();
        }

        ~GameServerAcceptor()
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
            Task.Run(async () =>
            {
                while (!CheckCancelToken.IsCancellationRequested)
                {
                    if (IsConnected())
                    {
                        UIEvent.GetSingletone.UpdateGameServerStatus(true);
                        // 준비가 되었음을 표시
                        if (ServerReadeyEvent != null)
                        {
                            ServerReadeyEvent.SetResult(true);
                            ServerReadeyEvent = null;
                        }
                    }
                    else
                        UIEvent.GetSingletone.UpdateGameServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckCancelToken.Token);
        }

        protected override void PushToPipeLine(Memory<byte> Data)
        {
            GameServerRecvPacketPipeline.GetSingletone.PushToPacketPipeline(Data);
        }

        protected override void PushToPipeLine(Memory<byte> Data, Socket Sock)
        {
            throw new NotImplementedException();
        }

        public async Task<int> Send(Memory<byte> Data)
        {
            return await SendData(Data).ConfigureAwait(false);
        }
    }
}
