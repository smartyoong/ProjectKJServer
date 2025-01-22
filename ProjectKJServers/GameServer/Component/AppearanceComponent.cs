using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Object;
using GameServer.MainUI;
using GameServer.PacketList;
using CoreUtility.Utility;

namespace GameServer.Component
{
    enum GenderType
    {
        MALE,
        FEMALE
    }

    internal class AppearanceComponent
    {
        private Pawn Owner;
        private GenderType Gender;
        private int PresetNumber;

        public AppearanceComponent(Pawn Owner, int Gender, int PresetNumber)
        {
            this.Owner = Owner;
            this.PresetNumber = PresetNumber;

            if (Enum.IsDefined(typeof(GenderType), Gender))
            {
                this.Gender = (GenderType)Gender;
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"정의되지 않은 성별입니다. 남성으로 세팅합니다. {Owner.GetName}");
                this.Gender = GenderType.MALE;
            }
        }

        public GenderType GetGender { get { return Gender; } }

        public int GetPresetNumber { get { return PresetNumber; } }

        public void RequestChangeGender()
        {
            //DB에게 성별 변경 요청

            // 성별은 2개뿐이니까 0 혹은 1로 무조건 나온다. +보다 %가 우선순위가 더 높아서 괄호를 쳐줘야한다.
            RequestDBUpdateGenderPacket Packet = new RequestDBUpdateGenderPacket(Owner.GetName, (((int)Gender+1)%2));
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_GENDER, Packet);
        }
        public void RequestChangePresetNumber(int GoalNumber)
        {
            //DB에게 프리셋 변경 요청
            RequestDBUpdatePresetPacket Packet = new RequestDBUpdatePresetPacket(Owner.GetName, GoalNumber);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_PRESET, Packet);
        }

        public void ApplyChangeGender()
        {
            // 성별은 0 혹은 1밖에 없다.
            Gender = (GenderType)(((int)Gender + 1) % 2);
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

        public void ApplyChangePresetNumber(int NewPresetNumber)
        {
            PresetNumber = NewPresetNumber;
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

        // 이거 완성하면 채팅 기능 만들자
        // 그래서 채팅 기능으로 명령어로 로직 문제 없는지 확인하면 될듯
    }
}
