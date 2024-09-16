using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using Microsoft.VisualBasic.Logging;
using System.Numerics;
using Windows.ApplicationModel.VoiceCommands;

namespace GameServer.Object
{
    struct CharacterAccountInfo(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }
    struct CharacterJobInfo(int Job, int Level)
    {
        public int Job { get; set; } = Job;
        public int Level { get; set; } = Level;
    }
    struct ChracterAppearanceInfo(int Gender, int PresetNumber)
    {
        public int Gender { get; set; } = Gender;
        public int PresetNumber { get; set; } = PresetNumber;
    }
    struct CharacterLevelInfo(int Level, int CurrentEXP)
    {
        public int Level { get; set; } = Level;
        public int CurrentExp { get; set; } = CurrentEXP;
    }
    internal class PlayerCharacter
    {
        private CharacterAccountInfo AccountInfo;
        private CharacterJobInfo JobInfo;
        private ChracterAppearanceInfo AppearanceInfo;
        private CharacterLevelInfo LevelInfo;
        private KinematicComponent MovementComponent;
        private MapComponent MapComponent;
        public PlayerCharacter(string AccountID, string NickName,int MapID, int Job, int JobLevel, int Level, int EXP, int PresetNum, int Gender, Vector3 StartPosition)
        {
            AccountInfo = new CharacterAccountInfo(AccountID, NickName);
            JobInfo = new CharacterJobInfo(Job,JobLevel);
            AppearanceInfo = new ChracterAppearanceInfo(Gender,PresetNum);
            LevelInfo = new CharacterLevelInfo(Level,EXP);
            MovementComponent = new KinematicComponent(300,1,StartPosition);
            MapComponent = new MapComponent(MapID);
            MainProxy.GetSingletone.AddKinematicMoveComponent(MovementComponent);
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(MapComponent.GetCurrentMapID(),Position);
        }

        public void RemoveCharacter()
        {
            MainProxy.GetSingletone.RemoveUserFromMap(MapComponent);
            MainProxy.GetSingletone.RemoveKinematicMoveComponent(MovementComponent,0);
        }
    }
}
