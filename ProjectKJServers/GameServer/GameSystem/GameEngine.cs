using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.Resource;

namespace GameServer.GameSystem
{
    internal class GameEngine : IDisposable
    {
        public const int FPS_60_UPDATE_INTERVAL = 17; // 60FPS (16.67 반올림)
        private int LastTickCount = 0;
        //private static Lazy<GameEngine> instance = new Lazy<GameEngine>(() => new GameEngine());
        //public static GameEngine GetSingletone { get { return instance.Value; } }

        private bool IsAlreadyDisposed = false;
        private CancellationTokenSource GameEngineCancleToken = new CancellationTokenSource();
        private Dictionary<int, MapData> MapDataDictionary = new Dictionary<int, MapData>();
        private Dictionary<int, CharacterPresetData> ChracterPresetDictionary = new Dictionary<int, CharacterPresetData>();

        MapSystem MapSystem;
        UniformVelocityMovementSystem UniformVelocityMovementSystem;
        ResourceLoader ResourceLoader;

        public GameEngine()
        {
            MapSystem = new MapSystem();
            UniformVelocityMovementSystem = new UniformVelocityMovementSystem();
            ResourceLoader = new ResourceLoader();
        }

        public void Start()
        {
            LoadResource();
            Task.Run(() => Run());
        }

        private void LoadResource()
        {
            LogManager.GetSingletone.WriteLog("리소스를 로드합니다.");
            ResourceLoader.LoadMapData(ref MapDataDictionary);
            ResourceLoader.LoadCharacterPreset(ref ChracterPresetDictionary);
        }

        private void Run()
        {
            LastTickCount = Environment.TickCount;
            while (!GameEngineCancleToken.IsCancellationRequested)
            {
                MapSystem.Update();
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

        public void AddUniformVelocityMovementComponentToSystem(UniformVelocityMovementComponent Component)
        {
            UniformVelocityMovementSystem.AddComponent(Component);
        }

    }
}
