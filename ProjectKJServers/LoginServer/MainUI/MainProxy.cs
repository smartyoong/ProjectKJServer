using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using LoginServer.Packet_SPList;
using LoginServer.PacketPipeLine;
using LoginServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.MainUI
{
    internal class MainProxy
    {
        /// <value>지연 생성 및 싱글톤 패턴과 프록시 패턴을 위해 Lazy를 사용합니다.</value>
        private static readonly Lazy<MainProxy> Instance = new Lazy<MainProxy>(() => new MainProxy());
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
            //MainThread에서 호출하는 메서드는 ConfigureAwait(false)를 사용하지 않습니다.
            await AccountSQLClass.ConnectToSQL(SQLEvent);
        }
        public async Task CloseAccountSQL()
        {
            await AccountSQLClass.StopSQL();
        }

        public void HandleSQLPacket(ISQLPacket Packet)
        {
            AccountSQLClass.HandleSQLPacket(Packet);
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

        public async Task SendToGameServer(Memory<byte> Data)
        {
            await GameServerConnectorClass.Send(Data).ConfigureAwait(false);
        }


        ///////////////////////////////////////////////////////////////////
        public void StartClientAcceptor()
        {
            ClientAcceptorClass.Start();
        }
        public async Task SendToClient(Socket Socket, Memory<byte> Data)
        {
            await ClientAcceptorClass.Send(Socket, Data).ConfigureAwait(false);
        }
        public string MakeAuthHashCode(string AccountID, int ClientID)
        {
            return ClientAcceptorClass.MakeAuthHashCode(AccountID, ClientID);
        }
        public Socket? GetClientSocketByAccountID(string AccountID)
        {
           return ClientAcceptorClass.GetClientSocketByAccountID(AccountID);
        }

        public void MappingSocketAccountID(Socket Sock, string AccountID)
        {
            ClientAcceptorClass.MappingSocketAccountID(Sock, AccountID);
        }

        public void KickClient(Socket Sock)
        {
            ClientAcceptorClass.KickClient(Sock);
        }

        public void KickClientByID(int ClientID)
        {
            ClientAcceptorClass.KickClientByID(ClientID);
        }

        public string GetIPAddrByClientSocket(Socket ClientSock)
        {
            return ClientAcceptorClass.GetIPAddrByClientSocket(ClientSock);
        }

        public string GetIPAddrByClientID(int ClientID)
        {
            return ClientAcceptorClass.GetIPAddrByClientID(ClientID);
        }

        public int GetPortByClientSocket(Socket ClientSock)
        {
            return ClientAcceptorClass.GetPortByClientSocket(ClientSock);
        }

        public int GetPortByClientID(int ClientID)
        {
            return ClientAcceptorClass.GetPortByClientID(ClientID);
        }

        public Socket? GetClientSocket(int ClientID)
        {
            return ClientAcceptorClass.GetClientSocket(ClientID);
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

        public void ProcessRecvPacketFromClient(Memory<byte> Data, int ClientID)
        {
            ClientRecvPacketPipelineClass.PushToPacketPipeline(Data, ClientID);
        }


        ///////////////////////////////////////////////////////////////////

        public void StopClientSendPacketPipeline()
        {
            ClientSendPacketPipelineClass.Cancel();
        }

        public void SendToClient(LoginPacketListID ID, dynamic Packet, int ClientID)
        {
            ClientSendPacketPipelineClass.PushToPacketPipeline(ID, Packet, ClientID);
        }


        ///////////////////////////////////////////////////////////////////
        public void StopGameServerRecvPacketPipeline()
        {
            GameServerRecvPacketPipelineClass.Cancel();
        }

        public void ProcessRecvPacketFromGameServer(Memory<byte> Data)
        {
            GameServerRecvPacketPipelineClass.PushToPacketPipeline(Data);
        }

        ///////////////////////////////////////////////////////////////////
        public void StopGameServerSendPacketPipeline()
        {
            GameServerSendPacketPipelineClass.Cancel();
        }
        public void ProcessSendPacketToGameServer(LoginGamePacketListID ID, dynamic packet)
        {
            GameServerSendPacketPipelineClass.PushToPacketPipeline(ID, packet);
        }
    }
}
