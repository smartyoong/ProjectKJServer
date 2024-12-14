using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.PacketList;
using System.Numerics;

namespace GameServer.Object
{
    using Vector3 = System.Numerics.Vector3;

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
    internal class PlayerCharacter : Pawn
    {
        private CharacterAccountInfo AccountInfo;
        private CharacterJobInfo JobInfo;
        private ChracterAppearanceInfo AppearanceInfo;
        private CharacterLevelInfo LevelInfo;
        private KinematicComponent MovementComponent;
        private CollisionComponent CircleCollisionComponent;
        private CollisionComponent LineTracerComponent;
        private PathComponent PathComponent;
        private int CurrentMapID = 0;
        private HealthPointComponent HealthPointComponent;
        private MagicPointComponent MagicPointComponent;

        public bool IsLineCollideObstacle { get; set; } = false;
        public bool IsCircleCollideObstacle { get; set; } = false;

        public CharacterAccountInfo GetAccountInfo { get { return AccountInfo; } }

        public CharacterJobInfo GetJobInfo { get { return JobInfo; } }

        public ChracterAppearanceInfo GetAppearanceInfo { get { return AppearanceInfo; } }

        public CharacterLevelInfo GetLevelInfo { get { return LevelInfo; } }

        public KinematicComponent GetMovementComponent { get { return MovementComponent; } }

        public CollisionComponent GetLineComponent { get { return LineTracerComponent; } }

        public PathComponent GetPathComponent { get { return PathComponent; } }

        public MagicPointComponent GetMPComponent { get { return MagicPointComponent; } }
        public HealthPointComponent GetHPComponent { get { return HealthPointComponent; } }

        public int GetCurrentMapID { get { return CurrentMapID; } }

        public CollisionComponent GetCollisionComponent { get { return CircleCollisionComponent; } }

        public PawnType GetPawnType { get { return PawnType.Player; } }

        public string GetName { get { return AccountInfo.AccountID; } }

        public PlayerCharacter(string AccountID, string NickName, int MapID, int Job, int JobLevel, int Level, int EXP, int PresetNum, int Gender, Vector3 StartPosition, uint HP, uint MP)
        {
            AccountInfo = new CharacterAccountInfo(AccountID, NickName);
            JobInfo = new CharacterJobInfo(Job, JobLevel);
            AppearanceInfo = new ChracterAppearanceInfo(Gender, PresetNum);
            LevelInfo = new CharacterLevelInfo(Level, EXP);
            CurrentMapID = MapID;

            // 현재는 서버세팅으로 해놨는데 리소스화 시키자
            MovementComponent = new KinematicComponent(this,StartPosition, GameServerSettings.Default.MaxSpeed, GameServerSettings.Default.MaxAccelrate,
              GameServerSettings.Default.MaxRotation, GameServerSettings.Default.BoardRadius, 42f);
            MainProxy.GetSingletone.AddKinematicMoveComponent(MovementComponent);

            CircleCollisionComponent = new CollisionComponent(MapID, this, StartPosition, CollisionType.Circle, 42f, OwnerType.Player);
            LineTracerComponent = new CollisionComponent(MapID, this, StartPosition, CollisionType.Line, 400f, OwnerType.Player);

            // 충돌시 응답 델리게이트 추가
            CircleCollisionComponent.BeginCollideWithObstacleDelegate = OnObstacleBlock;
            LineTracerComponent.BeginCollideWithObstacleDelegate = OnObstacleBlock;
            CircleCollisionComponent.EndCollideWithObstacleDelegate = OnEndObsatcleBlock;
            LineTracerComponent.EndCollideWithObstacleDelegate = OnEndObsatcleBlock;
            CircleCollisionComponent.CollideWithPawnDelegate = OnPawnBlock;

            MainProxy.GetSingletone.AddCollisionComponent(CircleCollisionComponent);
            MainProxy.GetSingletone.AddCollisionComponent(LineTracerComponent);

            PathComponent = new PathComponent(10); // 일단 임시로 이렇게 사용 가능하다~ 알려주기 위함 위에선 null을 줌
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(CurrentMapID, Position);
        }

        public void RemoveCharacter()
        {
            MainProxy.GetSingletone.RemoveKinematicMoveComponent(MovementComponent, 0);
            MainProxy.GetSingletone.RemoveCollisionComponent(CircleCollisionComponent, 0);
            MainProxy.GetSingletone.RemoveCollisionComponent(LineTracerComponent, 0);
        }
        // 여기서 병목이 일어날 수도 있다. Remove하고 Add하면 알아서 CurrentMapID를 읽어와서 추가한다.
        public void MoveToAnotherMap(int MapID)
        {
            MainProxy.GetSingletone.RemoveUserFromMap(this);
            CurrentMapID = MapID;
            MainProxy.GetSingletone.AddUserToMap(this);
        }

        public void SendAnotherUserArrivedDestination()
        {
            Vector3 CurrentPosition = MovementComponent.CharcaterStaticData.Position;
            SendUserMoveArrivedPacket Packet = new SendUserMoveArrivedPacket(GetAccountInfo.AccountID, CurrentMapID, (int)CurrentPosition.X, (int)CurrentPosition.Y);
            MainProxy.GetSingletone.SendToSameMap(CurrentMapID, GamePacketListID.SEND_USER_MOVE_ARRIVED, Packet);
        }

        private void OnObstacleBlock(CollisionType Type, ConvertObstacles Obstacle, Vector2 Normal, Vector2 HitPoint)
        {
            // 여기엔 나중에 데미지 같은거 계산할때 사용하자
            switch (Type)
            {
                case CollisionType.Circle:
                    IsCircleCollideObstacle = true;
                    break;
                case CollisionType.Line:
                    IsLineCollideObstacle = true;
                    break;
            }

        }

        private void OnEndObsatcleBlock(CollisionType Type, ConvertObstacles Obstacle)
        {
            //현재는 Obstacle의 종류를 세부적으로 비교를 안하는데,
            //일단은 Begin End가 잘되는지 테스트하기 위함이고, 나중에는 여기를 제대로 수정해서 사용하자
            switch (Type)
            {
                case CollisionType.Circle:
                    IsCircleCollideObstacle = false;
                    break;
                case CollisionType.Line:
                    IsLineCollideObstacle = false;
                    break;
            }
        }

        private void OnPawnBlock(CollisionType Type, PawnType PAwnType, Pawn Who, Vector2 Normal, Vector2 HitPoint)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Who}에게 {Type} 충돌했습니다.");
        }
    }
}
