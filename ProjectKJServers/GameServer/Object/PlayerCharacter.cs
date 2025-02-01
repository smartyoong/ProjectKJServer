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
        public bool IsGM { get; set; } = false;
    }
    internal class PlayerCharacter : Pawn
    {
        private CharacterAccountInfo AccountInfo;
        private JobComponent JobComponent;
        private AppearanceComponent AppearanceComponent;
        private LevelComponent LevelExpComponent;
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

        public JobComponent GetJobComponent { get { return JobComponent; } }

        public AppearanceComponent GetAppearanceComponent { get { return AppearanceComponent; } }

        public KinematicComponent GetMovementComponent { get { return MovementComponent; } }

        public CollisionComponent GetLineComponent { get { return LineTracerComponent; } }

        public PathComponent GetPathComponent { get { return PathComponent; } }

        public MagicPointComponent GetMPComponent { get { return MagicPointComponent; } }
        public HealthPointComponent GetHPComponent { get { return HealthPointComponent; } }

        public LevelComponent GetLevelComponent { get { return LevelExpComponent; } }

        public int GetCurrentMapID { get { return CurrentMapID; } }

        public CollisionComponent GetCollisionComponent { get { return CircleCollisionComponent; } }

        public PawnType GetPawnType { get { return PawnType.Player; } }

        public string GetName { get { return AccountInfo.AccountID; } }
        public void ActiveGMAuthority() { AccountInfo.IsGM = true; }

        public PlayerCharacter(string AccountID, string NickName, int MapID, int Job, int JobLevel, int Level, int EXP, int PresetNum, int Gender, Vector3 StartPosition, int HP, int MP)
        {
            AccountInfo = new CharacterAccountInfo(AccountID, NickName);
            JobComponent = new JobComponent(this, Job, JobLevel);
            AppearanceComponent = new AppearanceComponent(this,Gender, PresetNum);
            CurrentMapID = MapID;

            LevelExpComponent = new LevelComponent(this, Level, EXP);
            MainProxy.GetSingletone.AddLevelExpComponent(LevelExpComponent);

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

            int MaxHP = JobLevel * GameServerSettings.Default.LevelHPRate;
            int MaxMP = JobLevel * GameServerSettings.Default.LevelMPRate;
            HealthPointComponent = new HealthPointComponent(this, MaxHP, HP,Death);
            MagicPointComponent = new MagicPointComponent(this, MaxMP, MP);
            MainProxy.GetSingletone.AddHealthPointComponent(HealthPointComponent);
            MainProxy.GetSingletone.AddMagicPointComponent(MagicPointComponent);
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(CurrentMapID, Position);
        }

        // 최종 로그아웃 혹은 강제 종료때 호출되는 함수
        public void RemoveCharacter()
        {
            // 로그 아웃시에 저장되어야할 정보들
            HealthPointComponent.UpdateHPInfoToDBForce();
            MagicPointComponent.UpdateMPInfoToDBForce();
            LevelExpComponent.UpdateEXPInfoToDBForce();

            MainProxy.GetSingletone.RemoveKinematicMoveComponent(MovementComponent, 0);
            MainProxy.GetSingletone.RemoveCollisionComponent(CircleCollisionComponent, 0);
            MainProxy.GetSingletone.RemoveCollisionComponent(LineTracerComponent, 0);
            MainProxy.GetSingletone.RemoveHealthPointComponent(HealthPointComponent, 0);
            MainProxy.GetSingletone.RemoveMagicPointComponent(MagicPointComponent, 0);
            MainProxy.GetSingletone.RemoveLevelExpComponent(LevelExpComponent, 0);
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

        private void Death()
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 사망했습니다.");
            // 여기서 캐릭터 사망처리를 하자
        }
    }
}
