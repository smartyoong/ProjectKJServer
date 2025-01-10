using CoreUtility.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Object;
using GameServer.MainUI;
using GameServer.PacketList;

namespace GameServer.Component
{
    enum JobType
    {
        COMMON_PEOPLE = 0
    }
    internal class JobComponent
    {
        private const int MAX_JOB_LEVEL = 10;
        private JobType Job;
        private int Level;
        private Pawn Owner;

        public JobComponent(Pawn User,int JobKind, int Level)
        {
            Owner = User;

            // 정의 되지 않은건 에러다 일반인으로 일단 세팅시킨다.
            if (Enum.IsDefined(typeof(JobType), JobKind))
            {
                Job = (JobType)JobKind;
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"정의되지 않은 직업입니다. 일반인으로 세팅합니다. {Job} {Level} {Owner.GetName}");
                Job = JobType.COMMON_PEOPLE;
            }
            this.Level = Level;
        }

        public JobType GetJob { get { return Job; } }

        public int GetJobLevel { get { return Level; } }

        public void JobLevelUp(int Increasement)
        {
            if (Level >= MAX_JOB_LEVEL)
                return;
            Level += Increasement;

            if(Level >= MAX_JOB_LEVEL)
            {
                Level = MAX_JOB_LEVEL;
            }
            RequestDBUpdateJobLevelPacket Packet = new RequestDBUpdateJobLevelPacket(Owner.GetName,Level);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_JOB_LEVEL,Packet);
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

        public void JobChange(int JobKind)
        {
            if(!Enum.IsDefined(typeof(JobType), JobKind))
            {
                LogManager.GetSingletone.WriteLog($"정의되지 않은 직업입니다. {Owner.GetName} {JobKind}");
                return;
            }

            Job = (JobType)JobKind;

            RequestDBUpdateJobPacket Packet = new RequestDBUpdateJobPacket(Owner.GetName, JobKind);
            MainProxy.GetSingletone.SendToDBServer(GameDBPacketListID.REQUEST_UPDATE_JOB, Packet);
            //클라한테 전송해야한다. 이건 좀따가 만들자
        }

    }
}
