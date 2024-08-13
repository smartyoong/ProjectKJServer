using CoreUtility.Utility;
using DBServer.Packet_SPList;
using DBServer.PacketPipeLine;
using DBServer.SocketConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer.MainUI
{
    internal class MainProxy
    {
        /// <value>지연 생성 및 싱글톤 패턴과 프록시 패턴을 위해 Lazy를 사용합니다.</value>
        private static readonly Lazy<MainProxy> Instance = new Lazy<MainProxy>(() => new MainProxy());
        public static MainProxy GetSingletone => Instance.Value;

        private GameSQLPipeLine SQLPipeLineClass;
        private GameServerAcceptor GameServerAcceptorClass;
        private GameServerSendPacketPipeline GameServerSendPacketPipelineClass;
        private GameServerRecvPacketPipeline GameServerRecvPacketPipelineClass;
        private MainProxy()
        {
            SQLPipeLineClass = new GameSQLPipeLine();
            GameServerAcceptorClass = new GameServerAcceptor();
            GameServerSendPacketPipelineClass = new GameServerSendPacketPipeline();
            GameServerRecvPacketPipelineClass = new GameServerRecvPacketPipeline();
        }

        ////////////////////////////////////////////////////////////////////////
        public async Task ConnectToSQLServer(TaskCompletionSource<bool> SQLEvent)
        {
            await SQLPipeLineClass.ConnectToSQL(SQLEvent);
        }

        public async Task DisconnectSQLServer()
        {
            await SQLPipeLineClass.StopSQL();
        }

        public void HandleSQLPacket(IGameSQLPacket Packet)
        {
            switch (Packet)
            {
                case GameSQLReadCharacterPacket ReadCharacterPacket:
                    SQLPipeLineClass.SQL_READ_CHARACTER(ReadCharacterPacket.AccountID);
                    break;
                case GameSQLCreateCharacterPacket CreateCharacterPacket:
                    SQLPipeLineClass.SQL_CREATE_CHARACTER(CreateCharacterPacket.AccountID, CreateCharacterPacket.Gender, CreateCharacterPacket.PresetID);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog($"Unknown SQL Packet Type : {Packet.GetType().Name}");
                    break;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        
        public void StartAcceptGameServer(TaskCompletionSource<bool> ServerEvent)
        {
            GameServerAcceptorClass.Start(ServerEvent);
        }

        public async Task StopAcceptGameServer()
        {
            await GameServerAcceptorClass.Stop();
        }

        public async Task SendDataToGameServer(Memory<byte> data)
        {
            await GameServerAcceptorClass.Send(data).ConfigureAwait(false);
        }

        ////////////////////////////////////////////////////////////////////////

        public void StopGameServerSendPacketPipeline()
        {
            GameServerSendPacketPipelineClass.Cancel();
        }

        public void SendToGameServer(DBPacketListID ID, dynamic packet)
        {
            GameServerSendPacketPipelineClass.PushToPacketPipeline(ID, packet);
        }

        ////////////////////////////////////////////////////////////////////////
        
        public void StopGameServerRecvPacketPipeline()
        {
            GameServerRecvPacketPipelineClass.Cancel();
        }
        public void ProcessPacketFromGameServer(Memory<byte> data)
        {
            GameServerRecvPacketPipelineClass.PushToPacketPipeline(data);
        }
    }
}
