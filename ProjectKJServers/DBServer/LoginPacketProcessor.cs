using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utility;
using LogUtility;

namespace DBServer
{
    internal class LoginPacketProcessor : IPacketProcessor<LoginPacketList>
    {
        private Channel<byte[]> PacketChannel = Channel.CreateUnbounded<byte[]>();
        private CancellationTokenSource CancelToken = new CancellationTokenSource();

        LoginPacketProcessor()
        {
            Task.Run(() => ProcessPacket(),CancelToken.Token);
        }
        public dynamic MakePacketStruct(LoginPacketList ID, params dynamic[] PacketParams)
        {
            switch(ID)
            {
                case LoginPacketList.LoginRequest:
                    return new LoginRequestPacket((string)PacketParams[0], (string)PacketParams[1]);
                case LoginPacketList.LoginResponse:
                    return new LoginResponsePacket((bool)PacketParams[0], (int)PacketParams[1]);
                case LoginPacketList.RegistAccountRequest:
                    return new RegistAccountRequestPacket((string)PacketParams[0], (string)PacketParams[1]);
                case LoginPacketList.RegistAccountResponse:
                    return new RegistAccountResponsePacket((bool)PacketParams[0], (int)PacketParams[1]);
                default:
                    return new ErrorPacket(GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED);
            }
        }

        public async Task ProcessPacket()
        {
            while(!CancelToken.Token.IsCancellationRequested)
            {
                try
                {
                    var Packet = await PacketChannel.Reader.WaitToReadAsync(CancelToken.Token).ConfigureAwait(false);
                }
                catch(Exception e) when (e is not OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog(e).Wait();
                }
            }
        }

        public void PushToPacketQueue(byte[] Packet)
        {
            PacketChannel.Writer.TryWrite(Packet);
        }

        public bool IsErrorPacket(dynamic Packet)
        {
            if (Packet is ErrorPacket)
            {
                ProcessGeneralErrorCode(Packet.ErrorCode, "LoginPacketProcessor");
                return true;
            }
            return false;

        }

        public void ProcessGeneralErrorCode(GeneralErrorCode ErrorCode, string Message)
        {
            switch(ErrorCode)
            {
                case GeneralErrorCode.ERR_PACKET_IS_NOT_ASSIGNED:
                    StringBuilder ErrorLog = new StringBuilder();
                    ErrorLog.Append("Error: Packet is not assigned ");
                    ErrorLog.Append(Message);
                    LogManager.GetSingletone.WriteLog(ErrorLog.ToString()).Wait();
                    break;
            }
        }
    }
}
