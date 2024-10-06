using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.PacketList;
using System.Numerics;

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
        private PathComponent PathComponent;

        public CharacterAccountInfo GetAccountInfo() => AccountInfo;
        public CharacterJobInfo GetJobInfo() => JobInfo;
        public ChracterAppearanceInfo GetAppearanceInfo() => AppearanceInfo;
        public CharacterLevelInfo GetLevelInfo() => LevelInfo;
        public KinematicComponent GetMovementComponent() => MovementComponent;
        public MapComponent GetMapComponent() => MapComponent;
        public PathComponent GetPathComponent() => PathComponent;

        public PlayerCharacter(string AccountID, string NickName, int MapID, int Job, int JobLevel, int Level, int EXP, int PresetNum, int Gender, Vector3 StartPosition)
        {
            AccountInfo = new CharacterAccountInfo(AccountID, NickName);
            JobInfo = new CharacterJobInfo(Job, JobLevel);
            AppearanceInfo = new ChracterAppearanceInfo(Gender, PresetNum);
            LevelInfo = new CharacterLevelInfo(Level, EXP);
            // 현재는 서버세팅으로 해놨는데 리소스화 시키자
            MovementComponent = new KinematicComponent(StartPosition,GameServerSettings.Default.MaxSpeed, GameServerSettings.Default.MaxAccelrate, 
              GameServerSettings.Default.MaxRotation, GameServerSettings.Default.BoardRadius, null);
            MapComponent = new MapComponent(MapID, AccountID);
            MainProxy.GetSingletone.AddKinematicMoveComponent(MovementComponent);
            MainProxy.GetSingletone.AddUserToMap(MapComponent);
            PathComponent = new PathComponent(10); // 일단 임시로 이렇게 사용 가능하다~ 알려주기 위함 위에선 null을 줌
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(MapComponent.GetCurrentMapID(), Position);
        }

        public void RemoveCharacter()
        {
            MainProxy.GetSingletone.RemoveUserFromMap(MapComponent);
            MainProxy.GetSingletone.RemoveKinematicMoveComponent(MovementComponent, 0);
        }

        public void SendAnotherUserArrivedDestination()
        {
            int CurrentMapID = MapComponent.GetCurrentMapID();
            Vector3 CurrentPosition = MovementComponent.CharcaterStaticData.Position;
            SendUserMoveArrivedPacket Packet = new SendUserMoveArrivedPacket(GetAccountInfo().AccountID, CurrentMapID, (int)CurrentPosition.X, (int)CurrentPosition.Y);
            MainProxy.GetSingletone.SendToSameMap(MapComponent.GetCurrentMapID(), GamePacketListID.SEND_USER_MOVE_ARRIVED, Packet);
        }
    }
}
