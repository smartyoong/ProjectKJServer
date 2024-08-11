using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using LoginServer.PacketPipeLine;
using LoginServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.MainUI
{
    internal class MainProxy
    {
        /// <value>지연 생성 및 싱글톤 패턴과 프록시 패턴을 위해 Lazy를 사용합니다.</value>
        public static readonly Lazy<MainProxy> Instance = new Lazy<MainProxy>(() => new MainProxy());
        public static MainProxy GetSingletone => Instance.Value;

        AccountSQLManager AccountSQLClass;
        GameServerConnector GameServerConnectorClass;
        ClientAcceptor ClientAcceptorClass;
        ClientRecvPacketPipeline ClientRecvPacketPipelineClass;
        ClientSendPacketPipeline ClientSendPacketPipelineClass;
        GameServerRecvPacketPipeline GameServerRecvPacketPipelineClass;
        GameServerSendPacketPipeline GameServerSendPacketPipelineClass;
        private MainProxy()
        {
            AccountSQLClass = new AccountSQLManager();
            GameServerConnectorClass = new GameServerConnector();
            ClientAcceptorClass = new ClientAcceptor();
            ClientRecvPacketPipelineClass = new ClientRecvPacketPipeline();
            ClientSendPacketPipelineClass = new ClientSendPacketPipeline();
            GameServerRecvPacketPipelineClass = new GameServerRecvPacketPipeline();
            GameServerSendPacketPipelineClass = new GameServerSendPacketPipeline();
        }
        ///////////////////////////////////////////////////////////////////
        public async Task ConnectToAccountSQL(TaskCompletionSource<bool> SQLEvent)
        {
            await AccountSQLClass.ConnectToSQL(SQLEvent);
        }
        public async Task CloseAccountSQL()
        {
            await AccountSQLClass.StopSQL();
        }
        ///////////////////////////////////////////////////////////////////
        public void StartGameServerConnect(TaskCompletionSource<bool> ServerEvent)
        {
            GameServerConnectorClass.Start(ServerEvent);
        }
        public async Task CloseGameServerConnect()
        {
            await GameServerConnectorClass.Stop();
        }
        ///////////////////////////////////////////////////////////////////
        public void StartClientAcceptor()
        {
            ClientAcceptorClass.Start();
        }
        public async Task CloseClientAcceptor()
        {
            await ClientAcceptorClass.Stop();
        }
        ///////////////////////////////////////////////////////////////////
        public void StopClientRecvPacketPipeline()
        {
            ClientRecvPacketPipelineClass.Cancel();
        }
        ///////////////////////////////////////////////////////////////////
        public void StopClientSendPacketPipeline()
        {
            ClientSendPacketPipelineClass.Cancel();
        }
        ///////////////////////////////////////////////////////////////////
        public void StopGameServerRecvPacketPipeline()
        {
            GameServerRecvPacketPipelineClass.Cancel();
        }
        ///////////////////////////////////////////////////////////////////
        public void StopGameServerSendPacketPipeline()
        {
            GameServerSendPacketPipelineClass.Cancel();
        }
    }
}
