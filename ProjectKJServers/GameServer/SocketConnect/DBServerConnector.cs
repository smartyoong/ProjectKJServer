﻿using System.Net;
using System.Net.Sockets;
using GameServer.PacketPipeLine;
using CoreUtility.SocketCore;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.MainUI;

namespace GameServer.SocketConnect
{

    /// <summary>
    /// 게임서버와 연결을 담당하는 클래스입니다.
    /// 사실상 모든 연결 기능은 Connector 클래스의 기능을 사용합니다.
    /// 이 클래스를 만든 이유는, 명시적으로 DB서버와 통신하는 객체인것을 알려주기 위함이고
    /// UI EVENT와 DB서버 관련 패킷 Process의 매개자 역할을 하기 위함입니다.
    /// 즉, 핵심 기능들은 Connector 클래스에 존재하고 상속을 받아서, 명시적 표현 및 결합도 관련 로직을 담당합니다.
    /// </summary>
    internal class DBServerConnector : Connector, IDisposable
    {
        private bool IsAlreadyDisposed = false;
        /// <value>지연 생성 및 싱글톤 패턴을 사용합니다.</value>
        //private static readonly Lazy<DBServerConnector> Lazy = new Lazy<DBServerConnector>(() => new DBServerConnector());

        //public static DBServerConnector GetSingletone { get { return Lazy.Value; } }

        private CancellationTokenSource CheckProcessToken;

        private TaskCompletionSource<bool>? ServerReadeyEvent;


        /// <summary>
        /// GameServer 클래스의 생성자입니다.
        /// 소켓 연결 갯수만큼 클래스를 생성하고, 초기화시킵니다.
        /// </summary>
        public DBServerConnector() : base(new IPEndPoint(IPAddress.Parse(GameServerSettings.Default.DBServerIPAdress), GameServerSettings.Default.DBServerConnectPort), GameServerSettings.Default.DBServerConnectCount, "DBServer")
        {
            CheckProcessToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Game서버와의 연결을 시도합니다.
        /// 모든 소켓이 연결에 성공하면 DB서버와 연결되었다는 이벤트를 발생시킵니다.
        /// </summary>
        public void Start(TaskCompletionSource<bool>? ServerEvent)
        {
            Start();
            ProcessCheck();
            // Recv하는거 추가해야함
            ServerReadeyEvent = ServerEvent;
        }

        /// <summary>
        /// Game서버와의 연결을 종료합니다.
        /// 모든 소켓이 연결 종료에 성공하면 DB서버와 종료되었다는 이벤트를 발생시킵니다.
        /// 반드시 부모의 Stop 메서드를 호출하고
        /// 본인의 Dispose 메서드를 호출하세요.
        /// DelayTime도 몇초 정도 설정해놔야 문제없이 종료됩니다.
        /// </summary>
        public async Task Stop()
        {
            await Stop(TimeSpan.FromSeconds(3));
            UIEvent.GetSingletone.UpdateGameServerStatus(false);
            Dispose();
        }


        /// <summary>
        /// 종료자입니다.
        /// 최후의 수단이며, 직접 사용은 절대하지 마세요.
        /// </summary>
        ~DBServerConnector()
        {
            Dispose(false);
        }

        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Close 메서드를 사용하세요.
        /// </summary>
        public override void Dispose()
        {
            if (IsAlreadyDisposed)
            {
                return;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Stop 메서드를 사용하세요.
        /// </summary>
        /// <seealso cref="Stop()"/>"/>
        protected override void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {

            }
            CheckProcessToken.Dispose();
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
        }

        private void ProcessCheck()
        {
            Task.Run(async () =>
            {
                while (!CheckProcessToken.IsCancellationRequested)
                {
                    if (IsConnected())
                    {
                        UIEvent.GetSingletone.UpdateDBServerStatus(true);
                        if (ServerReadeyEvent != null)
                        {
                            ServerReadeyEvent.SetResult(true);
                            ServerReadeyEvent = null;
                        }
                    }
                    else
                        UIEvent.GetSingletone.UpdateDBServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckProcessToken.Token);
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

        protected override void PushToPipeLine(Memory<byte> DataBuffer)
        {
            MainProxy.GetSingletone.ProcessRecvPacketFromDBServer(DataBuffer);
        }
    }
}
