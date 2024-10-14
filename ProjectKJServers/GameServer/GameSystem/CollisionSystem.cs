using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;
using System.Net.Sockets;

namespace GameServer.GameSystem
{
    using CustomVector3 = CoreUtility.GlobalVariable.Vector3;

    internal class CollisionSystem : IComponentSystem
    {
        private readonly object _lock = new object();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        //ConCurrentBag는 편의성 좋은 메서드가 하나도 없네, AddRemove때만 Lock을 잘 걸자
        private List<List<Pawn>>? MapUserList;
        private long LastTickCount = 0;

        public CollisionSystem()
        {
        }

        public void SetMapData(ref Dictionary<int, MapData> MapDataDictionary)
        {
            this.MapDataDictionary = MapDataDictionary;
            int MaxMapID = MapDataDictionary.Keys.Max() + 1;
            MapUserList = new List<List<Pawn>>(MaxMapID);

            // 각 내부 리스트를 초기화
            for (int i = 0; i < MaxMapID; i++)
            {
                MapUserList.Add(new List<Pawn>());
            }
        }

        public void Update()
        {
            // 모든 Collision 관련 업데이트를 진행합니다.
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                //병렬로 위치 업데이트를 시킨다
                if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
                {
                    return;
                }

                float DeltaTime = (CurrentTickCount - LastTickCount);
                if (MapUserList == null)
                    return;

                // 2중 for문 미쳤네 ㅋㅋ
                Parallel.ForEach(MapUserList, pawnList =>
                {
                    foreach (var pawn in pawnList)
                    {
                        int MapID = pawn.GetCurrentMapID();
                        // 각 캐릭터가 CollisionComponent를 업데이트 시키도록하자 각 캐릭터가 몇개의 CollisionComponent를 가지고 있는지는 알 수 없다.
                        pawn.UpdateCollisionComponents(DeltaTime, MapDataDictionary[MapID], GetMapUsers(MapID));
                    }
                });

                LastTickCount = CurrentTickCount;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message);
            }
        }

        private bool ValidPawnCheck(Pawn Character)
        {
            if (MapUserList == null)
            {
                LogManager.GetSingletone.WriteLog("맵 유저 리스트가 초기화 되지 않았습니다.");
                return false;
            }
            int MapID = Character.GetCurrentMapID();
            if (MapUserList.Count <= MapID)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에 해당하는 맵 정보가 없습니다.");
                return false;
            }
            return true;
        }

        public void AddUser(Pawn Character)
        {
            if (!ValidPawnCheck(Character))
            {
                return;
            }
            int MapID = Character.GetCurrentMapID();
            lock (_lock)
            {
                MapUserList![MapID].Add(Character);
            }
        }

        public void RemoveUser(Pawn Character)
        {
            if (!ValidPawnCheck(Character))
            {
                return;
            }
            int MapID = Character.GetCurrentMapID();

            if (MapUserList![MapID] == null)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에는 유저가 존재하지 않습니다.");
                return;
            }

            lock (_lock)
            {
                MapUserList![MapID].Remove(Character);
            }
        }

        private bool ValidMapIDCheck(int MapID)
        {
            if (MapDataDictionary == null)
            {
                LogManager.GetSingletone.WriteLog("맵 데이터가 없습니다.");
                return false;
            }

            if (!MapDataDictionary.ContainsKey(MapID))
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에 해당하는 맵 정보가 없습니다.");
                return false;
            }
            return true;
        }
        private bool CheckMapUserList(int MapID)
        {
            try
            {
                if (MapUserList == null || MapUserList[MapID] == null)
                {
                    LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에는 유저가 존재하지 않습니다.");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}에는 유저가 존재하지 않습니다. {MapDataDictionary.Keys.Max() + 1}");
                return false;
            }
            return true;
        }

        public void SendPacketToSameMapUsers<T>(int MapID, GamePacketListID PacketID, T Packet) where T : struct
        {
            if (!ValidMapIDCheck(MapID))
            {
                return;
            }

            if (!CheckMapUserList(MapID))
            {
                return;
            }


            foreach (Pawn User in MapUserList![MapID])
            {
                Socket? Sock = MainProxy.GetSingletone.GetClientSocketByAccountID(User.GetAccountID());
                if (Sock != null)
                    MainProxy.GetSingletone.SendToClient(PacketID, Packet, MainProxy.GetSingletone.GetClientID(Sock));
            }
        }

        public List<Pawn>? GetMapUsers(int MapID)
        {
            if (!ValidMapIDCheck(MapID))
            {
                return null;
            }

            if (!CheckMapUserList(MapID))
            {
                return null;
            }

            lock (_lock)
            {
                return MapUserList![MapID];
            }
        }

        public List<ConvertObstacles> GetMapObstacles(int MapID)
        {
            if (!ValidMapIDCheck(MapID))
            {
                return new List<ConvertObstacles>();
            }

            return MapDataDictionary[MapID].Obstacles;
        }

        private bool BoundaryCheck(int MapID, ref readonly CustomVector3 Position)
        {
            MapData Data = MapDataDictionary[MapID];
            if (Position.X < 0 || Position.X > Data.MapBoundX)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 X축 경계를 벗어났습니다. {Position.X}");
                return false;
            }
            if (Position.Y < 0 || Position.Y > Data.MapBoundY)
            {
                LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 Y축 경계를 벗어났습니다. {Position.Y}");
                return false;
            }
            return true;
        }

        private bool PositionAABBCheck(in int MapID, ref readonly MapData Data, ref readonly CustomVector3 Position)
        {
            // Z축은 사용하지 않는다고 가정
            foreach (ConvertObstacles ObstacleData in Data.Obstacles)
            {
                //바닥은 건너뛰자
                if (ObstacleData.MeshName == "SM_Floor")
                {
                    continue;
                }
                //사각형, 실린더, 구까지는 각 X,Y 꼭짓점을 구하도록 작업했음 그래서 영역체크는 이걸로 가능 (충돌 일때는 Radius 사용할 예정)
                float MinX = ObstacleData.Points.Min(x => x.X);
                float MaxX = ObstacleData.Points.Max(x => x.X);
                float MinY = ObstacleData.Points.Min(x => x.Y);
                float MaxY = ObstacleData.Points.Max(x => x.Y);
                // 충돌 체크 범위가 올바른지 확인
                if (Position.X >= MinX && Position.X <= MaxX &&
                    Position.Y >= MinY && Position.Y <= MaxY)
                {
                    LogManager.GetSingletone.WriteLog($"맵 ID {MapID}의 장애물에 부딪혔습니다.{MinX} {MinY} {MaxX} {MaxY} {ObstacleData.MeshName}");
                    return false;
                }
            }
            return true;
        }

        public bool CanMove(int MapID, CustomVector3 Position)
        {
            if (!ValidMapIDCheck(MapID))
            {
                return false;
            }
            if (!BoundaryCheck(MapID, ref Position))
            {
                return false;
            }
            MapData Data = MapDataDictionary[MapID];
            if (!PositionAABBCheck(MapID, ref Data, ref Position))
            {
                return false;
            }
            return true;
        }
    }
}
