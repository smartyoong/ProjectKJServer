using GameServer.GameSystem;
using GameServer.PacketPipeLine;
using GameServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.MainUI
{
    internal class MainProxy
    {
        /// <value>지연 생성 및 싱글톤 패턴과 프록시 패턴을 위해 Lazy를 사용합니다.</value>
        private static readonly Lazy<MainProxy> Instance = new Lazy<MainProxy>(() => new MainProxy());
        public static MainProxy GetSingletone => Instance.Value;

        private GameEngine GameEngineClass;
        private LoginServerAcceptor LoginServerAcceptorClass;
        private DBServerConnector DBServerConnectorClass;
        private ClientAcceptor ClientAcceptorClass;
        private LoginServerSendPacketPipeline LoginServerSendPacketPipelineClass;
        private LoginServerRecvPacketPipeline LoginServerRecvPacketPipelineClass;
        private DBServerSendPacketPipeline DBServerSendPacketPipelineClass;
        private DBServerRecvPacketPipeline DBServerRecvPacketPipelineClass;
        private ClientSendPacketPipeline ClientSendPacketPipelineClass;
        private ClientRecvPacketPipeline ClientRecvPacketPipelineClass;

        private MainProxy()
        {
            GameEngineClass = new GameEngine();
            LoginServerAcceptorClass = new LoginServerAcceptor();
            DBServerConnectorClass = new DBServerConnector();
            ClientAcceptorClass = new ClientAcceptor();
            LoginServerSendPacketPipelineClass = new LoginServerSendPacketPipeline();
            LoginServerRecvPacketPipelineClass = new LoginServerRecvPacketPipeline();
            DBServerSendPacketPipelineClass = new DBServerSendPacketPipeline();
            DBServerRecvPacketPipelineClass = new DBServerRecvPacketPipeline();
            ClientSendPacketPipelineClass = new ClientSendPacketPipeline();
            ClientRecvPacketPipelineClass = new ClientRecvPacketPipeline();
        }
        ////////////////////////////////////////////////////////////////////////
        public void StartGameEngine()
        {
            GameEngineClass.Start();
        }

        public void StopGameEngine()
        {
            GameEngineClass.Stop();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StartAcceptLoginServer(TaskCompletionSource<bool> LoginServerReadyEvent)
        {
            LoginServerAcceptorClass.Start(LoginServerReadyEvent);
        }

        public async Task StopAcceptLoginServer()
        {
            await LoginServerAcceptorClass.Stop();
        }

        ////////////////////////////////////////////////////////////////////////

        public void ConnectToDBServer(TaskCompletionSource<bool> DBServerReadyEvent)
        {
            DBServerConnectorClass.Start(DBServerReadyEvent);
        }

        public async Task StopConnectToDBServer()
        {
            await DBServerConnectorClass.Stop();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StartAcceptClient()
        {
            ClientAcceptorClass.Start();
        }

        public async Task StopAcceptClient()
        {
            await ClientAcceptorClass.Stop();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopLoginServerRecvPacketPipeline()
        {
            LoginServerRecvPacketPipelineClass.Cancel();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopLoginServerSendPacketPipeline()
        {
            LoginServerSendPacketPipelineClass.Cancel();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopDBServerRecvPacketPipeline()
        {
            DBServerRecvPacketPipelineClass.Cancel();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopDBServerSendPacketPipeline()
        {
            DBServerSendPacketPipelineClass.Cancel();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopClientRecvPacketPipeline()
        {
            ClientRecvPacketPipelineClass.Cancel();
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopClientSendPacketPipeline()
        {
            ClientSendPacketPipelineClass.Cancel();
        }

    }
}
