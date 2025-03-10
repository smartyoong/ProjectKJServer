﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Mehtod;
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
        private Dictionary<string, Action<string>> GMCommandLookUpTable;
        public ChatComponent(Pawn Owner)
        {
            this.Owner = Owner;
            GMCommandLookUpTable = new Dictionary<string, Action<string>>
            {
                { "ChangeJob", CommandChangeJob },
                { "ChangeGender", CommandChangeGender },
                { "ChangePreset", CommandChangePreset }
            };
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
            // 여기 테스트 해보자 버그 수정 필요
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

            // 명령어 필터링
            if(Chat.Type == ChatType.BroadcastToSameMap && SplitedMessage[2] == "/o")
            {
                Chat.Type = ChatType.OperatiorCommand;
                // 3을 더하는 이유는 Split한 기준이 띄어쓰기이기 때문에 띄어쓰기가 3개이기 때문
                Chat.Message = Message.Substring(SplitedMessage[0].Length + SplitedMessage[1].Length + SplitedMessage[2].Length + 3);
            }

            return Chat;
        }


        private void GiveOrder(ChatMessage Message)
        {
            if (!IsOwnerGM())
                return;

            LogManager.GetSingletone.WriteLog("운영자 명령어가 들어옴");
            // 어차피 ChatMessage의 Message는 띄어쓰기가 구분되어 있는 상태로 들어옴 위의 함수에서는 타입만 제거한 상태로 들어옴
            // 그래서 여기서 Split으로 써도 무방함
            // 추후에 작업하자

            string[] SplitedMessage = Message.Message.Split(' ');
            if (GMCommandLookUpTable.ContainsKey(SplitedMessage[0]))
            {
                //[0] 은 명령어이기 때문에 제외하고 넘겨준다.
                GMCommandLookUpTable[SplitedMessage[0]](Message.Message.Substring(SplitedMessage[0].Length));
            }
        }

        private void Whisper(ChatMessage Message)
        {
        }

        private void BroadcastToSameParty(ChatMessage Message)
        {
        }

        private void BroadcastToSameMap(ChatMessage Message)
        {
            //LogManager.GetSingletone.WriteLog($"[{Message.Sender}] : {Message.Message} : {Message.Time}");
            SendUserSayPacket Packet = new SendUserSayPacket(Owner.GetName, Message.Sender, Message.Message, ((int)ChatType.BroadcastToSameMap));
            MainProxy.GetSingletone.SendToSameMap(Owner.GetCurrentMapID, GamePacketListID.SEND_USER_SAY, Packet);
        }

        private void BroadcastToAll(ChatMessage Message)
        {
        }

        private bool IsOwnerGM()
        {
            PlayerCharacter? Player = Owner as PlayerCharacter;
            if (Player == null)
                return false;
            if (Player.GetAccountInfo.IsGM == false)
                return false;

            return true;
        }


        private void CommandChangeJob(string Message)
        {
            if (!IsOwnerGM())
                return;

            PlayerCharacter? User = Owner as PlayerCharacter;
            GMCommand.CommandChangeJob(User!, Convert.ToInt32(Message));
        }

        private void CommandChangeGender(string Message)
        {
            if (!IsOwnerGM())
                return;
            PlayerCharacter? User = Owner as PlayerCharacter;
            GMCommand.CommandChangeGender(User!);
        }

        private void CommandChangePreset(string Message)
        {
            if (!IsOwnerGM())
                return;

            PlayerCharacter? User = Owner as PlayerCharacter;
            GMCommand.CommandChangePreset(User!, Convert.ToInt32(Message));
        }
    }
}
