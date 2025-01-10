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
        }
        public void RequestChangePresetNumber()
        {
            //DB에게 프리셋 변경 요청
        }

        public void ApplyChangeGender()
        {
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

        public void ApplyChangePresetNumber()
        {
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

        // 이거 완성하면 채팅 기능 만들자
        // 그래서 채팅 기능으로 명령어로 로직 문제 없는지 확인하면 될듯
    }
}
