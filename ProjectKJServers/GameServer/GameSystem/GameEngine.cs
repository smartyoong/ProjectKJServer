using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.MainUI;
using GameServer.Object;
using GameServer.PacketList;
using GameServer.Resource;
using System.Collections.Concurrent;
using Windows.ApplicationModel.VoiceCommands;

namespace GameServer.GameSystem
{
    internal class GameEngine : IDisposable
    {
        public static readonly int UPDATE_INTERVAL_20PERSEC = 50;

        private bool IsAlreadyDisposed = false;
        private CancellationTokenSource GameEngineCancleToken = new CancellationTokenSource();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        private Dictionary<int, CharacterPresetData> ChracterPresetDictionary = new Dictionary<int, CharacterPresetData>();
        private List<int> RequireEXPList = new List<int>(GameServerSettings.Default.MaxLevel);
        private Dictionary<int, Graph> MapGraphDictionary = new Dictionary<int, Graph>();
        private ConcurrentDictionary<string, PlayerCharacter> OnlineCharacterDictionary = new ConcurrentDictionary<string, PlayerCharacter>();
        private ConcurrentDictionary<string, string> NickNameMap = new ConcurrentDictionary<string, string>();

        private CollisionSystem CollisionSystem;
        private KinematicMoveSystem KinematicMovementSystem;
        private ResourceLoader ResourceLoader;
        private ArcKinematicSystem ArcKinematicSystem;
        private BehaviorTreeSystem BehaviorSystem;
        private GOAPSystem GOAPSystem;
        private ActionManagerSystem ActionManagerSystem;
        private ProcessMonitor ProcessMonitorSystem;
        private AStarPathFindSystem AStarPathFindSystem;
        private EuclideanHuristic EuclidHuristicMethod;
        private EXPSystem EXPSystem;
        private HealthSystem HealthSystem;

        public GameEngine()
        {
            CollisionSystem = new CollisionSystem();
            KinematicMovementSystem = new KinematicMoveSystem();
            ResourceLoader = new ResourceLoader();
            ArcKinematicSystem = new ArcKinematicSystem();
            BehaviorSystem = new BehaviorTreeSystem();
            GOAPSystem = new GOAPSystem();
            ActionManagerSystem = new ActionManagerSystem();
            ProcessMonitorSystem = new ProcessMonitor();
            AStarPathFindSystem = new AStarPathFindSystem();
            EuclidHuristicMethod = new EuclideanHuristic();
            EXPSystem = new EXPSystem();
            HealthSystem = new HealthSystem();
        }

        public void Start()
        {
            LoadResource();
            Task.Run(() => Run());
        }

#if DEBUG
        private void TestAStar()
        {
            LogManager.GetSingletone.WriteLog("A* 알고리즘을 테스트합니다.");
            EuclideanHuristic HuristicMethod = new EuclideanHuristic();
            AStarPathFindSystem AStarPathFindSystem = new AStarPathFindSystem();
            List<Node>? Result = AStarPathFindSystem.FindPath(MapGraphDictionary[0], new Node(200, 200), new Node(2800, 3200), HuristicMethod);

            float MaxX = MapDataDictionary[0].MapBoundX;
            float MaxY = MapDataDictionary[0].MapBoundY;
            float MinX = 0;
            float MinY = 0;
            int NodeSize = 100;
            string[,] map = new string[(int)MaxX + 1, (int)MaxY + 1];
            for (int i = (int)MinX; i <= (int)MaxX; i += (int)NodeSize)
            {
                for (int j = (int)MinY; j <= (int)MaxY; j += (int)NodeSize)
                {
                    map[i, j] = "-     ";
                }
            }

            if (Result == null)
                LogManager.GetSingletone.WriteLog("경로를 찾을 수 없습니다.");
            else
            {
                LogManager.GetSingletone.WriteLog("경로를 찾았습니다.");

                foreach (var Node in Result)
                {
                    LogManager.GetSingletone.WriteLog($"Node ({Node.GetX()}, {Node.GetY()})");
                    map[(int)Node.GetX(), (int)Node.GetY()] = "*     ";
                }
            }

            for (int j = (int)MaxX; j >= (int)MinX; j--)
            {
                string DebugString = string.Empty;
                for (int i = (int)MinY; i <= (int)MaxY; i += (int)NodeSize)
                {
                    DebugString += map[j, i];
                }
                if (DebugString != string.Empty)
                    LogManager.GetSingletone.WriteLog(DebugString);
            }
        }
#endif

        private void LoadResource()
        {
            LogManager.GetSingletone.WriteLog("리소스를 로드합니다.");
            ResourceLoader.LoadMapData(ref MapDataDictionary);
            CollisionSystem.SetMapData(ref MapDataDictionary);
            ResourceLoader.LoadCharacterPreset(ref ChracterPresetDictionary);
            ResourceLoader.LoadRequireEXP(ref RequireEXPList);
            ResourceLoader.MakeMapGraph(ref MapGraphDictionary, in MapDataDictionary);
        }

        private void Run()
        {
            while (!GameEngineCancleToken.IsCancellationRequested)
            {
                try
                {
                    CollisionSystem.Update();
                    KinematicMovementSystem.Update();
                    ArcKinematicSystem.Update();
                    BehaviorSystem.Update();
                    GOAPSystem.Update();
                    ActionManagerSystem.Update();
                    ProcessMonitorSystem.Update();
                    EXPSystem.Update();
                    HealthSystem.Update();
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e.Message);
                }
            }
        }

        public void Stop()
        {
            LogManager.GetSingletone.WriteLog("게임 엔진을 중단합니다.");
            GameEngineCancleToken.Cancel();
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {
                GameEngineCancleToken.Dispose();
            }
            IsAlreadyDisposed = true;
        }

        public List<Node>? FindPath(in Graph G, Node Start, Node End)
        {
            List<Node>? Result = AStarPathFindSystem.FindPath(G,Start,End, EuclidHuristicMethod);
            return Result;
        }

        public void AddActionManagerComponentToSystem(ActionManager Component)
        {
            ActionManagerSystem.AddComponent(Component);
        }

        public void RemoveActionManagerComponentFromSystem(ActionManager Component, int Count)
        {
            ActionManagerSystem.RemoveComponent(Component, Count);
        }

        public void AddGOAPComponentToSystem(GOAPComponent Component)
        {
            GOAPSystem.AddComponent(Component);
        }

        public void RemoveGOAPComponentFromSystem(GOAPComponent Component, int Count)
        {
            GOAPSystem.RemoveComponent(Component, Count);
        }
        public void AddBehaviorTreeComponentToSystem(BehaviorTreeComponent Behavior)
        {
            BehaviorSystem.AddComponent(Behavior);
        }

        public void RemoveBehaviorTreeComponentFromSystem(BehaviorTreeComponent Behavior, int Count)
        {
            BehaviorSystem.RemoveComponent(Behavior, Count);
        }

        public void AddKinematicComponentToSystem(KinematicComponent Component)
        {
            KinematicMovementSystem.AddComponent(Component);
        }

        public void RemoveKinematicComponentFromSystem(KinematicComponent Component, int Count)
        {
            KinematicMovementSystem.RemoveComponent(Component, Count);
        }

        public void AddArcKinematicComponentToSystem(ArcKinematicComponent Component)
        {
            ArcKinematicSystem.AddComponent(Component);
        }

        public void RemoveArcKinematicComponentFromSystem(ArcKinematicComponent Component, int Count)
        {
            ArcKinematicSystem.RemoveComponent(Component, Count);
        }

        public void AddCollisionComponentToSystem(CollisionComponent Component)
        {
            CollisionSystem.AddComponent(Component);
        }

        public void RemoveCollisionComponentFromSystem(CollisionComponent Component, int Count)
        {
            CollisionSystem.RemoveComponent(Component, Count);
        }

        public void AddLevelExpComponentToSystem(LevelComponent Component)
        {
            EXPSystem.AddComponent(Component);
        }

        public void RemoveLevelExpComponentFromSystem(LevelComponent Component, int Count)
        {
            EXPSystem.RemoveComponent(Component, Count);
        }

        public void AddHealthPointComponentToSystem(HealthPointComponent Component)
        {
            HealthSystem.AddHealthPointComponent(Component);
        }

        public void AddMagicPointComponentToSystem(MagicPointComponent Component)
        {
            HealthSystem.AddMagicPointComponent(Component);
        }

        public void RemoveHealthPointComponentFromSystem(HealthPointComponent Component, int Count)
        {
            HealthSystem.RemoveComponent(Component, Count);
        }

        public void RemoveMagicPointComponentFromSystem(MagicPointComponent Component, int Count)
        {
            HealthSystem.RemoveComponent(Component, Count);
        }

        public void AddUserToMap(Pawn Character)
        {
            CollisionSystem.AddUser(Character);
        }

        public void RemoveUserFromMap(Pawn Character)
        {
            CollisionSystem.RemoveUser(Character);
        }

        public bool CanMove(int MapID, Vector3 Position)
        {
            return CollisionSystem.CanMove(MapID, Position);
        }

        public void AddNickName(string AccountID, string NickName)
        {
            NickNameMap.TryAdd(AccountID, NickName);
        }

        public string GetNickName(string AccountID)
        {
            if (NickNameMap.TryGetValue(AccountID, out string? NickName))
            {
                return NickName;
            }
            else
            {
                return string.Empty;
            }
        }

        public void RemoveNickName(string AccountID)
        {
            string? NickName;
            if (NickNameMap.TryRemove(AccountID, out NickName))
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임 {NickName}을 제거했습니다.");
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 닉네임을 찾을 수 없습니다.");
            }
        }

        public void SendToSameMap<T>(int MapID, GamePacketListID PacketID, T Packet) where T : struct
        {
            CollisionSystem.SendPacketToSameMapUsers(MapID, PacketID, Packet);
        }

        public List<Pawn>? GetMapUsers(int MapID)
        {
            return CollisionSystem.GetMapUsers(MapID);
        }

        public List<ConvertObstacles> GetMapObstacles(int MapID)
        {
            return CollisionSystem.GetMapObstacles(MapID);
        }

        public void CreateCharacter(ResponseDBCharBaseInfoPacket Info)
        {

            PlayerCharacter NewCharacter = new PlayerCharacter(Info.AccountID, Info.NickName, Info.MapID, Info.Job, 
                Info.JobLevel, Info.Level, Info.EXP, Info.PresetNumber, Info.Gender, new System.Numerics.Vector3(Info.X, Info.Y, 0)
                ,Info.HP,Info.MP);

            if (OnlineCharacterDictionary.TryAdd(Info.AccountID, NewCharacter))
                LogManager.GetSingletone.WriteLog($"계정 {Info.AccountID} {Info.NickName}의 캐릭터를 생성했습니다.");

            MainProxy.GetSingletone.AddNickName(Info.AccountID, Info.NickName);
            MainProxy.GetSingletone.AddUserToMap(NewCharacter);
        }

        public PlayerCharacter? GetCharacter(string AccountID)
        {
            if(OnlineCharacterDictionary.TryGetValue(AccountID, out PlayerCharacter? Character))
                return Character;
            return null;
        }

        public PlayerCharacter? GetCharacterByAccountID(string AccountID)
        {
            if(OnlineCharacterDictionary.TryGetValue(AccountID, out PlayerCharacter? Character))
            {
                return Character;
            }
            else
            {
                return null;
            }
        }

        public void RemoveCharacter(string AccountID)
        {
            PlayerCharacter? TempChar;
            RemoveNickName(AccountID);
            if (OnlineCharacterDictionary.TryRemove(AccountID, out TempChar))
            {
                if(TempChar != null)
                {
                    TempChar.RemoveCharacter();
                    MainProxy.GetSingletone.RemoveUserFromMap(TempChar);
                }
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 제거했습니다.");
            }
            else
            {
                //캐릭터 생성안하고 종료하면 이렇게될 수도
                LogManager.GetSingletone.WriteLog($"계정 {AccountID}의 캐릭터를 찾을 수 없습니다.");
            }
        }

        public Graph? GetMapGraph(int MapID)
        {
            if(MapGraphDictionary.ContainsKey(MapID))
                return MapGraphDictionary[MapID];
            return null;
        }

        public int GetRequireEXP(int Level)
        {
            return RequireEXPList[Level-1];
        }
    }
}
