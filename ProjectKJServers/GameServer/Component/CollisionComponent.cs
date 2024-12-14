using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.MainUI;
using GameServer.Object;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Windows.UI.Input.Inking;

namespace GameServer.Component
{
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using Vector3 = System.Numerics.Vector3;

    enum CollisionType
    {
        Line,
        Circle,
        Square,
    }

    enum OwnerType
    {
        Player,
        Monster,
        NPC,
        Object,
    }

    internal class CollisionComponent
    {
        private Vector3 Position;
        private Vector3 EndPosition;
        private Pawn Owner;
        private CollisionType Type;
        private OwnerType OwnerType;
        private float Radius;
        private float Orientation = 0f;
        private Vector3[] SquarePoints;

        private int MapID;
        private List<ConvertObstacles> PreviousHitObstacles = new List<ConvertObstacles>();

        public Action<CollisionType, ConvertObstacles, Vector2, Vector2>? BeginCollideWithObstacleDelegate;
        public Action<CollisionType, ConvertObstacles>? EndCollideWithObstacleDelegate;
        public Action<CollisionType,PawnType, Pawn, Vector2, Vector2>? CollideWithPawnDelegate;
        
        public Pawn GetOwner { get { return Owner; } }

        public CollisionComponent(int MapID, Pawn Provider, Vector3 StartPosition, CollisionType type, float Size, OwnerType ownerType)
        {
            this.MapID = MapID;
            Owner = Provider;
            Radius = Size;
            Type = type;
            Position = StartPosition;
            SquarePoints = new Vector3[4];
            OwnerType = ownerType;
        }

        public void Update(float DeltaTime, in MapData Data, in List<Pawn>? Characters)
        {
            Position = Owner.GetMovementComponent.CharcaterStaticData.Position;
            Orientation = Owner.GetMovementComponent.CharcaterStaticData.Orientation;
            switch (Type)
            {
                case CollisionType.Line:
                    UpdateLineCollision();
                    break;
                case CollisionType.Circle:
                    UpdateCircleCollision();
                    break;
                case CollisionType.Square:
                    // 사각형 갱신
                    UpdateSquareCollision();
                    break;
            }
            List<ConvertObstacles> HitObstacles = new List<ConvertObstacles>();
            List<Pawn> HitPawns = new List<Pawn>();
            List<Vector2> ImpactNormals = new List<Vector2>();
            List<Vector2> HitPoints = new List<Vector2>();
            // 갱신 후에 전달받은 데이터를 토대로 obstacle과 collision이 충돌하는지 체크하자
            // 정확한지는 모르겠지만 일단 되는거 확인!
            if (CollideObstacleCheck(in Data, ref HitObstacles, ref ImpactNormals, ref HitPoints))
            {
                for(int i = 0; i < HitObstacles.Count; ++i)
                {
                    BeginCollideWithObstacleDelegate?.Invoke(Type, HitObstacles[i], ImpactNormals[i], HitPoints[i]);
                }
            }
            if (CollidePawnCheck(in Characters, ref HitPawns, ref ImpactNormals, ref HitPoints))
            {
                for(int i = 0; i < HitPawns.Count; ++i)
                {
                    //자기 자신은 제외
                    if (HitPawns[i] == Owner)
                        continue;

                    CollideWithPawnDelegate?.Invoke(Type, HitPawns[i].GetPawnType, HitPawns[i], ImpactNormals[i], HitPoints[i]);
                }
            }

            // 교차에서 벗어난 Obstacle을 감지하고 EndCollideWithObstacleDelegate 호출
            for (int i = 0; i < PreviousHitObstacles.Count; ++i)
            {
                if (!HitObstacles.Contains(PreviousHitObstacles[i]))
                {
                    EndCollideWithObstacleDelegate?.Invoke(Type, PreviousHitObstacles[i]);
                }
            }

            // 현재 프레임의 Obstacles를 저장하여 다음 프레임에서 비교할 수 있도록 함
            PreviousHitObstacles = new List<ConvertObstacles>(HitObstacles);
        }

        // 선형 도형 StartPosition과 EndPosition 재조정
        private void UpdateLineCollision()
        {
            Vector3 Direction = new Vector3((float)Math.Cos(Orientation), (float)Math.Sin(Orientation), 0);
            EndPosition = Position + (Direction * Radius);
        }

        private void UpdateCircleCollision()
        {
            // 원형 도형 갱신
            // 원형은 원점과 반지름만 있으면 충분하다.
            // 추후 변동이 필요하면 여기서 작업하자
            return;
        }

        //정사각형 도형 갱신
        private void UpdateSquareCollision()
        {
            SquarePoints[0] = Position + new Vector3(-Radius, -Radius, 0);
            SquarePoints[1] = Position + new Vector3(Radius, -Radius, 0);
            SquarePoints[2] = Position + new Vector3(-Radius, Radius, 0);
            SquarePoints[3] = Position + new Vector3(Radius, Radius, 0);
        }

        private bool CollideObstacleCheck(in MapData Data, ref List<ConvertObstacles> HitObstacles, ref List<Vector2> ImpactNormals, ref List<Vector2> HitPoints)
        {
            bool Result = false;
            ConcurrentBag<ConvertObstacles> ConcurrentHitObstacles = new ConcurrentBag<ConvertObstacles>();
            ConcurrentBag<Vector2> ConcurrentImpactNormals = new ConcurrentBag<Vector2>();
            ConcurrentBag<Vector2> ConcurrentHitPoints = new ConcurrentBag<Vector2>();
            // 장애물과 충돌한거니까 본인한테만 시그널이가면 됨, 만약에 벽을 부시는 거면 그때는 다른 처리가 필요함
            Parallel.ForEach(Data.Obstacles, (Obstacle) =>
            {
                Vector2 ImpactNormal = Vector2.Zero;
                Vector2 HitPoint = Vector2.Zero;
                bool LocalResult = false;
                switch (Obstacle.Type)
                {
                    case ObjectType.Square:
                        LocalResult = InterSectCheckWithSquare(Obstacle, out ImpactNormal, out HitPoint);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal);
                            ConcurrentHitPoints.Add(HitPoint);
                        }
                        break;
                    case ObjectType.Sphere:
                        LocalResult = InterSectCheckWithCircle(Obstacle, false, out ImpactNormal, out HitPoint);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal);
                            ConcurrentHitPoints.Add(HitPoint);
                        }
                        break;
                    case ObjectType.Cylinder:
                        LocalResult = InterSectCheckWithCircle(Obstacle, true, out ImpactNormal, out HitPoint);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal);
                            ConcurrentHitPoints.Add(HitPoint);
                        }
                        break;
                }
                if (LocalResult)
                {
                    Result = true;
                }
            });
            HitObstacles = new List<ConvertObstacles>(ConcurrentHitObstacles);
            ImpactNormals = new List<Vector2>(ConcurrentImpactNormals);
            HitPoints = new List<Vector2>(ConcurrentHitPoints);
            return Result;
        }

        private bool CollidePawnCheck(in List<Pawn>? PawnList, ref List<Pawn> HitPawns, ref List<Vector2> ImpactNormals, ref List<Vector2> HitPoints)
        {
            if(PawnList == null)
                return false;

            ConcurrentBag<Pawn> HitPawn = new ConcurrentBag<Pawn>();
            ConcurrentBag<Vector2> ImpactNormalBag = new ConcurrentBag<Vector2>();
            ConcurrentBag<Vector2> HitPointBag = new ConcurrentBag<Vector2>();
            bool Result = false;

            Parallel.ForEach(PawnList, (Pawns) =>
            {
                CollisionComponent Component =  Pawns.GetCollisionComponent;
                bool LocalResult = false;
                Vector2 ImpactNormal = Vector2.Zero;
                Vector2 HitPoint = Vector2.Zero;
                // 본인 타입과 상대방 타입에 따라서 달라져야한다.
                if (Type == CollisionType.Line && Component.Type == CollisionType.Line)
                {
                    LocalResult = LineIntersectsLine(new Vector2(Position.X, Position.Y), new Vector2(EndPosition.X, EndPosition.Y),
                        new Vector2(Component.Position.X, Component.Position.Y), new Vector2(Component.EndPosition.X, Component.EndPosition.Y), out ImpactNormal, out HitPoint);
                }
                else if(Type == CollisionType.Line && Component.Type == CollisionType.Circle)
                {
                    LocalResult = LineIntersectsCircle(new Vector2(Position.X, Position.Y), new Vector2(EndPosition.X, EndPosition.Y),
                        new Vector2(Component.Position.X, Component.Position.Y), Component.Radius, out ImpactNormal, out HitPoint);
                }
                else if(Type == CollisionType.Line && Component.Type == CollisionType.Square)
                {
                    LocalResult = LineIntersectsSquare(new Vector2(Position.X, Position.Y), new Vector2(EndPosition.X, EndPosition.Y),
                        new Vector2(Component.SquarePoints[0].X, Component.SquarePoints[0].Y), new Vector2(Component.SquarePoints[3].X, Component.SquarePoints[3].Y), out ImpactNormal , out HitPoint);
                }
                else if (Type == CollisionType.Circle && Component.Type == CollisionType.Line)
                {
                    // 매개변수 위치에 주의! 뒤에가 본인이다!
                    LocalResult = LineIntersectsCircle(new Vector2(Component.Position.X, Component.Position.Y), new Vector2(Component.EndPosition.X, Component.EndPosition.Y),
                        new Vector2(Position.X, Position.Y), Radius, out ImpactNormal, out HitPoint);
                }
                else if (Type == CollisionType.Circle && Component.Type == CollisionType.Circle)
                {
                    LocalResult = CircleIntersectsCircle(new Vector2(Position.X, Position.Y), Radius,
                        new Vector2(Component.Position.X, Component.Position.Y), Component.Radius, out ImpactNormal, out HitPoint);
                }
                else if (Type == CollisionType.Circle && Component.Type == CollisionType.Square)
                {
                    LocalResult = CircleIntersectsSquare(new Vector2(Position.X, Position.Y), Radius,
                        new Vector2(Component.SquarePoints[0].X, Component.SquarePoints[0].Y), new Vector2(Component.SquarePoints[3].X, Component.SquarePoints[3].Y), out ImpactNormal, out HitPoint);
                }
                else if (Type == CollisionType.Square && Component.Type == CollisionType.Line)
                {
                    // 매개변수 위치에 주의! 뒤에가 본인이다!
                    LocalResult = LineIntersectsSquare(new Vector2(Component.Position.X, Component.Position.Y), new Vector2(Component.EndPosition.X, Component.EndPosition.Y),
                        new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y), out ImpactNormal, out HitPoint);
                }
                else if (Type == CollisionType.Square && Component.Type == CollisionType.Circle)
                {
                    // 매개변수 위치에 주의! 뒤에가 본인이다!
                    LocalResult = CircleIntersectsSquare(new Vector2(Component.Position.X, Component.Position.Y), Component.Radius,
                        new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y), out ImpactNormal, out HitPoint);
                }
                else if (Type == CollisionType.Square && Component.Type == CollisionType.Square)
                {
                    LocalResult = SquareIntersectsSquare(new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y),
                        new Vector2(Component.SquarePoints[0].X, Component.SquarePoints[0].Y), new Vector2(Component.SquarePoints[3].X, Component.SquarePoints[3].Y), out ImpactNormal, out HitPoint);
                }
                if (LocalResult)
                {
                    HitPawn.Add(Pawns);
                    ImpactNormalBag.Add(ImpactNormal);
                    HitPointBag.Add(HitPoint);
                    Result = true;
                }
            });
            HitPawns = new List<Pawn>(HitPawn);
            ImpactNormals = new List<Vector2>(ImpactNormalBag);
            HitPoints = new List<Vector2>(HitPointBag);
            return Result;
        }

        private bool InterSectCheckWithSquare(ConvertObstacles Obstacle, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            // 본인의 CollisionType에 따라서 처리
            switch (Type)
            {
                case CollisionType.Line:
                    return LineIntersectsSquare(new Vector2(Position.X,Position.Y), new Vector2(EndPosition.X,EndPosition.Y), 
                        GetMinPointInSquare(Obstacle), GetMaxPointInSquare(Obstacle), out ImpactNormal, out HitPoint);
                case CollisionType.Circle:
                    return CircleIntersectsSquare(new Vector2(Position.X, Position.Y), Radius,
                        GetMinPointInSquare(Obstacle), GetMaxPointInSquare(Obstacle), out ImpactNormal, out HitPoint);
                case CollisionType.Square:
                    return SquareIntersectsSquare(new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y),
                        GetMinPointInSquare(Obstacle), GetMaxPointInSquare(Obstacle), out ImpactNormal, out HitPoint);
            }
            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        private bool InterSectCheckWithCircle(ConvertObstacles Obstacle, bool IsCylinder, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            // 본인의 CollisionType에 따라서 처리
            Vector2 MinPoints = GetMinPointInSquare(Obstacle);
            Vector2 Center = new Vector2(MinPoints.X, MinPoints.Y);
            if (IsCylinder)
                Center += new Vector2(Obstacle.CylinderRadius, Obstacle.CylinderRadius);
            else
                Center += new Vector2(Obstacle.SphereRadius, Obstacle.SphereRadius);
            
            // 원형 장애물의 중점을 가져온다.

            switch (Type)
            {
                case CollisionType.Line:
                    if(IsCylinder)
                        return LineIntersectsCircle(new Vector2(Position.X, Position.Y), new Vector2(EndPosition.X, EndPosition.Y),
                            Center, Obstacle.CylinderRadius, out ImpactNormal, out HitPoint);
                    else
                        return LineIntersectsCircle(new Vector2(Position.X, Position.Y), new Vector2(EndPosition.X, EndPosition.Y),
                            Center, Obstacle.SphereRadius, out ImpactNormal, out HitPoint);
                case CollisionType.Circle:
                    if (IsCylinder)
                        return CircleIntersectsCircle(new Vector2(Position.X, Position.Y), Radius,
                            Center, Obstacle.CylinderRadius, out ImpactNormal, out HitPoint);
                    else
                        return CircleIntersectsCircle(new Vector2(Position.X, Position.Y), Radius,
                            Center, Obstacle.SphereRadius, out ImpactNormal, out HitPoint);
                case CollisionType.Square:
                    // 매개변수 위치 주의! 본인이 Square다!
                    if (IsCylinder)
                        return CircleIntersectsSquare(Center, Obstacle.CylinderRadius,
                            new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y), out ImpactNormal, out HitPoint);
                    else
                        return CircleIntersectsSquare(Center, Obstacle.SphereRadius,
                            new Vector2(SquarePoints[0].X, SquarePoints[0].Y), new Vector2(SquarePoints[3].X, SquarePoints[3].Y), out ImpactNormal, out HitPoint);
            }
            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }


        private bool LineIntersectsSquare(Vector2 LineStart, Vector2 LineEnd, Vector2 MinSquarePoint, Vector2 MaxSquarePoint, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            Vector2[] SquarePoints =
            {
                new Vector2(MinSquarePoint.X, MinSquarePoint.Y),
                new Vector2(MaxSquarePoint.X, MinSquarePoint.Y),
                new Vector2(MaxSquarePoint.X, MaxSquarePoint.Y),
                new Vector2(MinSquarePoint.X, MaxSquarePoint.Y)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector2 SquareLineStart = SquarePoints[i];
                Vector2 SquareLineEnd = SquarePoints[(i + 1) % 4];

                if (LineIntersectsLine(LineStart, LineEnd, SquareLineStart, SquareLineEnd, out ImpactNormal, out HitPoint))
                {
                    return true;
                }
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        private Vector2 GetMinPointInSquare(ConvertObstacles Obstacle)
        {
            float MinX = Obstacle.Points.Min(x => x.X);
            float MinY = Obstacle.Points.Min(x => x.Y);
            return new Vector2(MinX, MinY);
        }

        private Vector2 GetMaxPointInSquare(ConvertObstacles Obstacle)
        {
            float MaxX = Obstacle.Points.Max(x => x.X);
            float MaxY = Obstacle.Points.Max(x => x.Y);
            return new Vector2(MaxX, MaxY);
        }

        private bool LineIntersectsLine(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            // 2차 행렬이므로 행렬식을 구하는 방법이 빠르다. ad-bc 하드 코딩 ㄱㄱ
            float d = (p2.X - p1.X) * (q2.Y - q1.Y) - (p2.Y - p1.Y) * (q2.X - q1.X);
            // 두 선은 평행하다. 혹은 완전히 일치한다.
            if (d == 0)
            {
                ImpactNormal = Vector2.Zero;
                HitPoint = Vector2.Zero;
                return false;
            }

            // 교차점 체크 P = P1 + u(P2 - P1) , Q = Q1 + v(Q2 - Q1) 여기서 두 선의 교차점은 유일해가 존재한다는 것이다.
            float u = ((q1.X - p1.X) * (q2.Y - q1.Y) - (q1.Y - p1.Y) * (q2.X - q1.X)) / d;
            float v = ((q1.X - p1.X) * (p2.Y - p1.Y) - (q1.Y - p1.Y) * (p2.X - p1.X)) / d;

            // 교차점이 범위 내에 존재한다. (크래머 규칙 u = Du/D, v = Dv/D, 위에서 d를 나누어준 이유)
            if ((u >= 0 && u <= 1) && (v >= 0 && v <= 1))
            {
                // 교차 지점에서의 법선 벡터 계산
                HitPoint = p1 + u * (p2 - p1);
                Vector2 edge = q2 - q1;
                ImpactNormal = new Vector2(-edge.Y, edge.X);
                ImpactNormal = Vector2.Normalize(ImpactNormal);
                return true;
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        private bool LineIntersectsCircle(Vector2 LineStart, Vector2 LineEnd, Vector2 Center, float Radius, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            // 선분의 길이
            Vector2 d = LineEnd - LineStart;
            // 두 점 사이의 거리
            Vector2 f = LineStart - Center;

            // P(t) = p1 + t * d
            float a = Vector2.Dot(d, d);
            // 두 벡터의 관계 2를 곱하는 이유는 이차 방정식의 형태에서 b가 선형 항의 계수가 되어야 하기 때문
            float b = 2 * Vector2.Dot(f, d);
            // (x^2 + y^2 = r^2)
            float c = Vector2.Dot(f, f) - Radius * Radius;
            // at2(제곱) + bt + c = 0 형태로 변환

            // 판별식
            float Discriminant = b * b - 4 * a * c;
            // 접점이 없음
            if (Discriminant < 0)
            {
                ImpactNormal = Vector2.Zero;
                HitPoint = Vector2.Zero;
                return false;
            }
            else
            {
                // 근의 공식
                Discriminant = (float)Math.Sqrt(Discriminant);
                float t1 = (-b - Discriminant) / (2 * a);
                float t2 = (-b + Discriminant) / (2 * a);

                // 1개의 근이라도 정상 범위라면 통과
                if (t1 >= 0 && t1 <= 1 || t2 >= 0 && t2 <= 1)
                {
                    // 충돌 지점에서의 법선 벡터 계산
                    float t = t1 >= 0 && t1 <= 1 ? t1 : t2;
                    HitPoint = LineStart + t * d;
                    ImpactNormal = (HitPoint - Center);
                    ImpactNormal = Vector2.Normalize(ImpactNormal);
                    return true;
                }
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }


        private bool CircleIntersectsCircle(Vector2 Center1, float Radius1, Vector2 Center2, float Radius2, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            float Distance = Vector2.Distance(Center1, Center2);
            if (Distance <= (Radius1 + Radius2))
            {
                // 충돌 지점에서의 법선 벡터 계산
                ImpactNormal = Center2 - Center1;
                ImpactNormal = Vector2.Normalize(ImpactNormal);
                HitPoint = Center1 + ImpactNormal * Radius1;
                return true;
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        private bool CircleIntersectsSquare(Vector2 Center, float Radius, Vector2 MinPoint, Vector2 MaxPoint, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            float ClosestX = Math.Clamp(Center.X, MinPoint.X, MaxPoint.X);
            float ClosestY = Math.Clamp(Center.Y, MinPoint.Y, MaxPoint.Y);

            float DistanceX = Center.X - ClosestX;
            float DistanceY = Center.Y - ClosestY;

            float DistanceSquared = (DistanceX * DistanceX) + (DistanceY * DistanceY);
            if (DistanceSquared < (Radius * Radius))
            {
                // 충돌 지점에서의 법선 벡터 계산
                ImpactNormal = new Vector2(DistanceX, DistanceY);
                ImpactNormal = Vector2.Normalize(ImpactNormal);
                HitPoint = new Vector2(ClosestX, ClosestY);
                return true;
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        private bool SquareIntersectsSquare(Vector2 Min1, Vector2 Max1, Vector2 Min2, Vector2 Max2, out Vector2 ImpactNormal, out Vector2 HitPoint)
        {
            // 사실상 AABB 충돌 체크
            if ((Min1.X <= Max2.X && Max1.X >= Min2.X) &&
                (Min1.Y <= Max2.Y && Max1.Y >= Min2.Y))
            {
                // 충돌 지점에서의 법선 벡터 계산 (간단한 예로, 첫 번째 사각형의 중심에서 두 번째 사각형의 중심으로 향하는 벡터)
                Vector2 Center1 = (Min1 + Max1) / 2;
                Vector2 Center2 = (Min2 + Max2) / 2;
                ImpactNormal = Center2 - Center1;
                ImpactNormal = Vector2.Normalize(ImpactNormal);
                HitPoint = (Center1 + Center2) / 2;
                return true;
            }

            ImpactNormal = Vector2.Zero;
            HitPoint = Vector2.Zero;
            return false;
        }

        // 외부에서 특정 위치에 원형(Collision)을 기준으로 장애물과 충돌하는지 체크
        public bool CheckPositionBlockByWall(int MapID, Vector3 ToPosition, float Radius, ref List<ConvertObstacles> HitObstacles, ref List<Vector2> ImpactNormals, ref List<Vector2> HitPoints)
        {
            bool Result = false;
            ConcurrentBag<ConvertObstacles> ConcurrentHitObstacles = new ConcurrentBag<ConvertObstacles>();
            ConcurrentBag<Vector2> ConcurrentImpactNormals = new ConcurrentBag<Vector2>();
            ConcurrentBag<Vector2> ConcurrentHitPoints = new ConcurrentBag<Vector2>();
            List<ConvertObstacles> Obstacles = MainProxy.GetSingletone.GetMapObstacles(MapID);
            // 장애물과 충돌한거니까 본인한테만 시그널이가면 됨, 만약에 벽을 부시는 거면 그때는 다른 처리가 필요함
            Parallel.ForEach(Obstacles, (Obstacle) =>
            {
                bool LocalResult = false;
                Vector2 ObstacleMin = GetMinPointInSquare(Obstacle);
                Vector2 ObstacleMax = GetMaxPointInSquare(Obstacle);
                Vector2 ObstacleCenter = ObstacleMin + (ObstacleMax - ObstacleMin) / 2; // 장애물의 중심 계산 이거 잘되는지 나중에 로그 찍어보자
                Vector2 NextPosition = new Vector2(ToPosition.X, ToPosition.Y);
                switch (Obstacle.Type)
                {
                    case ObjectType.Square:
                        Vector2 ImpactNormal = Vector2.Zero;
                        Vector2 HitPoint = Vector2.Zero;
                        LocalResult = CircleIntersectsSquare(NextPosition, Radius, ObstacleMin, ObstacleMax, out ImpactNormal, out HitPoint);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal);
                            ConcurrentHitPoints.Add(HitPoint);
                        }
                        break;
                    case ObjectType.Sphere:
                        Vector2 ImpactNormal2 = Vector2.Zero;
                        Vector2 HitPoint2 = Vector2.Zero;
                        LocalResult = CircleIntersectsCircle(NextPosition, Radius, ObstacleCenter, Obstacle.SphereRadius, out ImpactNormal2, out HitPoint2);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal2);
                            ConcurrentHitPoints.Add(HitPoint2);
                        }
                        break;
                    case ObjectType.Cylinder:
                        Vector2 ImpactNormal3 = Vector2.Zero;
                        Vector2 HitPoint3 = Vector2.Zero;
                        LocalResult = CircleIntersectsCircle(NextPosition, Radius, ObstacleCenter, Obstacle.CylinderRadius, out ImpactNormal3, out HitPoint3);
                        if (LocalResult)
                        {
                            ConcurrentHitObstacles.Add(Obstacle);
                            ConcurrentImpactNormals.Add(ImpactNormal3);
                            ConcurrentHitPoints.Add(HitPoint3);
                        }
                        break;
                }
            });
            HitObstacles = new List<ConvertObstacles>(ConcurrentHitObstacles);
            ImpactNormals = new List<Vector2>(ConcurrentImpactNormals);
            HitPoints = new List<Vector2>(ConcurrentHitPoints);

            if(HitObstacles.Count > 0)
            {
                Result = true;
            }

            return Result;
        }

    }
}
