using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.PacketList;
using System.Numerics;

namespace GameServer.Object
{
    using Vector3 = System.Numerics.Vector3;
    enum PawnType
    {
        Player = 0,
        Monster = 1,
        NPC = 2,
        Projectile = 3
    }

    interface Pawn
    {
        Vector3 GetCurrentPosition();
        string GetAccountID();
        float GetOrientation();
        int GetCurrentMapID();
        void UpdateCollisionComponents(float DeltaTime, MapData Data, List<Pawn>? Characters);
        CollisionComponent GetCollisionComponent();
        PawnType GetPawnType();
        PathComponent GetPathComponent();
    }

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

        public bool IsLineCollideObstacle { get; set; } = false;
        public bool IsCircleCollideObstacle { get; set; } = false;

        public CharacterAccountInfo GetAccountInfo() => AccountInfo;
        public CharacterJobInfo GetJobInfo() => JobInfo;
        public ChracterAppearanceInfo GetAppearanceInfo() => AppearanceInfo;
        public CharacterLevelInfo GetLevelInfo() => LevelInfo;
        public KinematicComponent GetMovementComponent() => MovementComponent;
        public CollisionComponent GetLineComponent() => LineTracerComponent;
        public PathComponent GetPathComponent() => PathComponent;

        public PlayerCharacter(string AccountID, string NickName, int MapID, int Job, int JobLevel, int Level, int EXP, int PresetNum, int Gender, Vector3 StartPosition)
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

            PathComponent = new PathComponent(10); // 일단 임시로 이렇게 사용 가능하다~ 알려주기 위함 위에선 null을 줌
            MainProxy.GetSingletone.AddUserToMap(this);
        }

        public bool MoveToLocation(Vector3 Position)
        {
            LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 {Position}으로 이동합니다.");
            return MovementComponent.MoveToLocation(CurrentMapID, Position);
        }

        public void RemoveCharacter()
        {
            MainProxy.GetSingletone.RemoveUserFromMap(this);
            MainProxy.GetSingletone.RemoveKinematicMoveComponent(MovementComponent, 0);
        }

        public void SendAnotherUserArrivedDestination()
        {
            Vector3 CurrentPosition = MovementComponent.CharcaterStaticData.Position;
            SendUserMoveArrivedPacket Packet = new SendUserMoveArrivedPacket(GetAccountInfo().AccountID, CurrentMapID, (int)CurrentPosition.X, (int)CurrentPosition.Y);
            MainProxy.GetSingletone.SendToSameMap(CurrentMapID, GamePacketListID.SEND_USER_MOVE_ARRIVED, Packet);
        }

        public Vector3 GetCurrentPosition()
        {
            return MovementComponent.CharcaterStaticData.Position;
        }

        public string GetAccountID()
        {
            return AccountInfo.AccountID;
        }

        public float GetOrientation()
        {
            return MovementComponent.CharcaterStaticData.Orientation;
        }

        public int GetCurrentMapID()
        {
            return CurrentMapID;
        }

        public CollisionComponent GetCollisionComponent()
        {
            return CircleCollisionComponent;
        }

        public void UpdateCollisionComponents(float DeltaTime, MapData Data, List<Pawn>? Characters)
        {
            Parallel.ForEach(new CollisionComponent[] { CircleCollisionComponent, LineTracerComponent }, (Component) =>
            {
                Component.Update(DeltaTime, Data, Characters);
            });
        }

        public PawnType GetPawnType()
        {
            return PawnType.Player;
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
    }
}
