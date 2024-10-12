﻿using CoreUtility.GlobalVariable;
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
            MovementComponent = new KinematicComponent(StartPosition, GameServerSettings.Default.MaxSpeed, GameServerSettings.Default.MaxAccelrate,
              GameServerSettings.Default.MaxRotation, GameServerSettings.Default.BoardRadius, null);
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
            switch (Type)
            {
                case CollisionType.Circle:
                    IsCircleCollideObstacle = true;
                    break;
                case CollisionType.Line:
                    IsLineCollideObstacle = true;
                    break;
            }

            if (IsCircleCollideObstacle)
            {
                LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 장애물에 막혔습니다. {Normal} {Obstacle.MeshName} {Type}");

                // 이거 0이 계속 나온다 그래서 미정의 현상이 발생한다 -> 이거 Boolean으로 Lock걸어서 상태만 재설정하고,
                // KinematicComponent에서 매 Position Update할때 Lock으로 현재 충돌했는지 체크하고 슬라이딩 벡터 설정해야겠다 클라는 잘된다 그럼 그게 맞다.
                if (Type == CollisionType.Circle)
                {
                    // 캐릭터의 현재 위치를 가져옵니다.
                    Vector2 MixPosition = new Vector2(MovementComponent.CharcaterStaticData.Position.X, MovementComponent.CharcaterStaticData.Position.Y);
                    Vector2 CurrentPosition = new Vector2(MovementComponent.PreviusCharacterStaticData.X, MovementComponent.PreviusCharacterStaticData.Y);

                    // 충돌 지점에서의 법선 벡터를 이용하여 슬라이딩 벡터 계산
                    Vector2 SlideDirection = Vector2.Normalize(Vector2.Reflect(MixPosition - CurrentPosition, Normal));
                    Vector2 AdjustPosition = MixPosition + SlideDirection;

                    MovementComponent.StopMove(new Vector3(AdjustPosition, 0));
                    LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 원에 막혀 위치가 조정됩니다. {AdjustPosition}");
                }
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

            if (IsCircleCollideObstacle && !IsLineCollideObstacle)
                LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 원은 장애물에 겹쳤으나 라인은 안겹쳤습니다 하지만 신뢰 불가능입니다.");
            if (!IsCircleCollideObstacle && !IsLineCollideObstacle)
                LogManager.GetSingletone.WriteLog($"캐릭터 {AccountInfo.NickName}이 장애물에서 완전 벗어났습니다 현재 이동을 막지 않는 중 입니다.");
        }
    }
}
