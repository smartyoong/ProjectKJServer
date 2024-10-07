using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using GameServer.Object;
using CoreUtility.Utility;
using CoreUtility.GlobalVariable;

namespace GameServer.Component
{
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
        private Vector3 Normal;
        private Pawn Owner;
        private CollisionType Type;
        private OwnerType OwnerType;
        private float Radius;
        private float Orientation = 0f;
        private Vector3[] SquarePoints;

        private int MapID;

        public CollisionComponent(int MapID, Pawn Provider, Vector3 StartPosition, CollisionType type ,float Size, OwnerType ownerType)
        {
            this.MapID = MapID;
            Owner = Provider;
            Radius = Size;
            Type = type;
            Position = StartPosition;
            SquarePoints = new Vector3[4];
            OwnerType = ownerType;
        }

        public void Update(float DeltaTime, MapData Data, List<Pawn>? Characters)
        {
            Position = Owner.GetCurrentPosition();
            Orientation = Owner.GetOrientation();
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
            // 갱신 후에 전달받은 데이터를 토대로 obstacle과 collision이 충돌하는지 체크하자
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

        public void MoveToAnotherMap(int MapID)
        {
            // 맵 이동 관련 처리가 필요할 경우 여기서 진행함
            // 추후 포탈 기능 만들면 포탈이랑 이게 충돌하면 다른맵으로 이동하도록 하자
            // 나중에 맵 이동할 때 CollisionSystem 리스트에서 값 변경하는것도 잊지말자
            this.MapID = MapID;
        }

        // update문에서 System으로부터 MapData를 받아와서 각 Component가 충돌 검사를 진행하자
        private bool LineIntersectsRect(Vector2 p1, Vector2 p2, Vector2 min, Vector2 max)
        {
            // 사각형의 네 변을 정의
            Vector2[] rectPoints = {
            new Vector2(min.X, min.Y),
            new Vector2(max.X, min.Y),
            new Vector2(max.X, max.Y),
            new Vector2(min.X, max.Y)
        };

            // 사각형의 네 변과 선분의 충돌 검사
            for (int i = 0; i < 4; i++)
            {
                Vector2 q1 = rectPoints[i];
                Vector2 q2 = rectPoints[(i + 1) % 4];

                if (LineIntersectsLine(p1, p2, q1, q2))
                {
                    return true;
                }
            }

            return false;
        }

        private bool LineIntersectsLine(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d = (p2.X - p1.X) * (q2.Y - q1.Y) - (p2.Y - p1.Y) * (q2.X - q1.X);
            if (d == 0) return false;

            float u = ((q1.X - p1.X) * (q2.Y - q1.Y) - (q1.Y - p1.Y) * (q2.X - q1.X)) / d;
            float v = ((q1.X - p1.X) * (p2.Y - p1.Y) - (q1.Y - p1.Y) * (p2.X - p1.X)) / d;

            return (u >= 0 && u <= 1) && (v >= 0 && v <= 1);
        }
    }
}
