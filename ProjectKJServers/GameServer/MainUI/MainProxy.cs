using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.GameSystem;
using GameServer.PacketList;
using GameServer.PacketPipeLine;
using GameServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GameServer.Component;

namespace GameServer.MainUI
{
    internal class MainProxy
    {
        /// <value>지연 생성 및 싱글톤 패턴과 프록시 패턴을 위해 Lazy를 사용합니다.</value>
        private static readonly Lazy<MainProxy> Instance = new Lazy<MainProxy>(() => new MainProxy());
        public static MainProxy GetSingletone => Instance.Value;

        private delegate void DelegateWriteLog(string Log);
        private delegate void DelegateWriteErrorLog(Exception ex);
        private DelegateWriteLog WriteFileLog;
        private DelegateWriteErrorLog WriteErrorLog;

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
            WriteFileLog = LogManager.GetSingletone.WriteLog;
            WriteErrorLog = LogManager.GetSingletone.WriteLog;
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

        public void AddUniformVelocityMovementComponent(UniformVelocityMovementComponent Component)
        {
            GameEngineClass.AddUniformVelocityMovementComponentToSystem(Component);
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

        public async Task SendToLoginServer(Memory<byte> Data)
        {
            await LoginServerAcceptorClass.Send(Data).ConfigureAwait(false);
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

        public async Task SendToDBServer(Memory<byte> Data)
        {
            await DBServerConnectorClass.Send(Data).ConfigureAwait(false);
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

        public async Task SendToClient(Socket Socket, Memory<byte> Data)
        {
            await ClientAcceptorClass.Send(Socket, Data).ConfigureAwait(false);
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

        public int GetClientID(Socket ClientSock)
        {
            return ClientAcceptorClass.GetClientID(ClientSock);
        }

        public GeneralErrorCode AddHashCodeAndAccountID(string AccountID, string HashCode)
        {
            return ClientAcceptorClass.AddHashCodeAndAccountID(AccountID, HashCode);
        }

        public void RemoveHashCodeByAccountID(string AccountID)
        {
            ClientAcceptorClass.RemoveHashCodeByAccountID(AccountID);
        }

        public GeneralErrorCode CheckAuthHashCode(string AccountID, ref string HashCode)
        {
            return ClientAcceptorClass.CheckAuthHashCode(AccountID, ref HashCode);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopLoginServerRecvPacketPipeline()
        {
            LoginServerRecvPacketPipelineClass.Cancel();
        }

        public void ProcessRecvPacketFromLoginServer(Memory<byte> Data)
        {
            LoginServerRecvPacketPipelineClass.PushToPacketPipeline(Data);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopLoginServerSendPacketPipeline()
        {
            LoginServerSendPacketPipelineClass.Cancel();
        }
        public void SendToLoginServer(GameLoginPacketListID ID, dynamic Packet)
        {
            LoginServerSendPacketPipelineClass.PushToPacketPipeline(ID, Packet);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopDBServerRecvPacketPipeline()
        {
            DBServerRecvPacketPipelineClass.Cancel();
        }
        public void ProcessRecvPacketFromDBServer(Memory<byte> Data)
        {
            DBServerRecvPacketPipelineClass.PushToPacketPipeline(Data);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopDBServerSendPacketPipeline()
        {
            DBServerSendPacketPipelineClass.Cancel();
        }

        public void SendToDBServer(GameDBPacketListID ID, dynamic Packet)
        {
            DBServerSendPacketPipelineClass.PushToPacketPipeline(ID, Packet);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopClientRecvPacketPipeline()
        {
            ClientRecvPacketPipelineClass.Cancel();
        }

        public void ProcessRecvPacketFromClient(Memory<byte> Data, Socket Sock)
        {
            // int로 형변환
            ClientRecvPacketPipelineClass.PushToPacketPipeline(Data, GetClientID(Sock));
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopClientSendPacketPipeline()
        {
            ClientSendPacketPipelineClass.Cancel();
        }

        public void SendToClient(GamePacketListID ID, dynamic Packet, int ClientID)
        {
            ClientSendPacketPipelineClass.PushToPacketPipeline(ID, Packet, ClientID);
        }

        public void SendToClient(GamePacketListID ID, dynamic Packet, string AccountID)
        {
            Socket? Sock = GetClientSocketByAccountID(AccountID);
            if(Sock != null)
                ClientSendPacketPipelineClass.PushToPacketPipeline(ID, Packet,GetClientID(Sock));
            else
                WriteFileLog($"SendToClient 실패: AccountID {AccountID}에 해당하는 소켓이 없습니다.");
        }
    }
}
