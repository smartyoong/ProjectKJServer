using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;

namespace GameServer.Component
{
    internal class ChatComponent
    {
        private enum ChatType
        {
            BroadcastToSameMap,
            BroadcastToAll,
            BroadcastToFriend ,
            Whisper,
            BroadcastToSameParty,
            OperatiorCommand,
            Error,
        }

        private struct ChatMessage
        {
            public ChatType Type;
            public string Message;
            public string Sender;
            public string Receiver;
            public DateTime Time;
        }

        private Pawn Owner;
        public ChatComponent(Pawn Owner)
        {
            this.Owner = Owner;
        }

        public void Say(string Message)
        {
            ChatMessage Chat = ParsingMessage(Message);

            switch (Chat.Type)
            {
                case ChatType.Whisper:
                    Whisper(Chat);
                    break;
                case ChatType.BroadcastToSameParty:
                    BroadcastToSameParty(Chat);
                    break;
                case ChatType.BroadcastToSameMap:
                    BroadcastToSameMap(Chat);
                    break;
                case ChatType.BroadcastToAll:
                    BroadcastToAll(Chat);
                    break;
                case ChatType.OperatiorCommand:
                    GiveOrder(Chat);
                    break;
                default:
                    break;
            }
        }

        private ChatMessage ParsingMessage(string Message)
        {
            ChatMessage Chat = new ChatMessage();
            string[] SplitedMessage = Message.Split(' ');
            switch(SplitedMessage[0])
            {
                case "/w":
                    Chat.Type = ChatType.Whisper;
                    break;
                case "/p":
                    Chat.Type = ChatType.BroadcastToSameParty;
                    break;
                case "/a":
                    Chat.Type = ChatType.BroadcastToAll;
                    break;
                case "/o":
                    Chat.Type = ChatType.OperatiorCommand;
                    break;
                case "/f":
                    Chat.Type = ChatType.BroadcastToFriend;
                    break;
                default:
                    Chat.Type = ChatType.BroadcastToSameMap;
                    break;
            }

            if (Owner.GetPawnType == PawnType.Player)
            {
                PlayerCharacter? Player = Owner as PlayerCharacter;

                if (Player == null)
                {
                    Chat.Type = ChatType.Error;
                    return Chat;
                }

                Chat.Sender = Player.GetAccountInfo.NickName;
            }
            else
            {
                Chat.Sender = Owner.GetName;
            }


            if(Chat.Type == ChatType.Whisper)
            {
                Chat.Receiver = SplitedMessage[1];
            }

            Chat.Message = Message.Substring(SplitedMessage[0].Length + SplitedMessage[1].Length + 2);
            Chat.Time = DateTime.Now;
            return Chat;
        }


        private void GiveOrder(ChatMessage Message)
        {
            PlayerCharacter? Player = Owner as PlayerCharacter;
            if (Player == null)
                return;
            if (Player.GetAccountInfo.IsGM == false)
                return;
            LogManager.GetSingletone.WriteLog("운영자 명령어가 들어옴");
            // 어차피 ChatMessage의 Message는 띄어쓰기가 구분되어 있는 상태로 들어옴 위의 함수에서는 타입만 제거한 상태로 들어옴
            // 그래서 여기서 Split으로 써도 무방함
            // 추후에 작업하자
        }

        private void Whisper(ChatMessage Message)
        {
        }

        private void BroadcastToSameParty(ChatMessage Message)
        {
        }

        private void BroadcastToSameMap(ChatMessage Message)
        {
            LogManager.GetSingletone.WriteLog($"[{Message.Sender}] : {Message.Message} : {Message.Time}");
            SendUserSayPacket Packet = new SendUserSayPacket(Owner.GetName, Message.Sender, Message.Message, ((int)ChatType.BroadcastToSameMap));
            MainProxy.GetSingletone.SendToSameMap(Owner.GetCurrentMapID, GamePacketListID.SEND_USER_SAY, Packet);
        }

        private void BroadcastToAll(ChatMessage Message)
        {
        }
    }
}
