using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using System.Numerics;
using Windows.ApplicationModel.VoiceCommands;

namespace GameServer.Object
{
    struct CharacterAccountInfo
    {
        public string AccountID { get; set; }
        public string NickName { get; set; }
    }
    struct CharacterPosition
    {
        public int MapID { get; set; }
    }
    struct CharacterJobInfo
    {
        public int Job { get; set; }
        public int Level { get; set; }
    }
    struct ChracterAppearanceInfo
    {
        public int Gender { get; set; }
        public int PresetNumber { get; set; }
    }
    struct CharacterLevelInfo
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
    }
    internal class PlayerCharacter
    {
        public CharacterAccountInfo AccountInfo;
        public CharacterPosition CurrentPosition;
        public CharacterJobInfo JobInfo;
        public ChracterAppearanceInfo AppearanceInfo;
        public CharacterLevelInfo LevelInfo;
        private KinematicComponent MovementComponent;
        private MapComponent MapComponent;
        private KinematicHandle KinematicHandle;
        public PlayerCharacter()
        {
            AccountInfo = new CharacterAccountInfo();
            CurrentPosition = new CharacterPosition();
            JobInfo = new CharacterJobInfo();
            AppearanceInfo = new ChracterAppearanceInfo();
            LevelInfo = new CharacterLevelInfo();
            MovementComponent = new KinematicComponent();
            MapComponent = new MapComponent(1); // 일단 임시로 대충
            KinematicHandle = new KinematicHandle();
        }

        public void SetMovement(int Speed, Vector3 Position)
        {
            MainProxy.GetSingletone.AddKinematicMoveComponent(KinematicHandle,MovementComponent);
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(CurrentPosition.MapID,Position);
        }

        public void RemoveCharacter()
        {
            MainProxy.GetSingletone.RemoveUserFromMap(MapComponent);
            MainProxy.GetSingletone.RemoveKinematicMoveComponent(KinematicHandle, MovementComponent,0);
        }
    }
}
