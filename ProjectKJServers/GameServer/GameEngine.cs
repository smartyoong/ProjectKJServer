using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KYCCoreDataStruct;
using KYCLog;

namespace GameServer
{
    internal class GameEngine : IDisposable
    {
        const int MINIMUM_UPDATE_INTERVAL = 1000 / 60;
        private int LastTickCount = 0;
        private static Lazy<GameEngine> instance = new Lazy<GameEngine>(() => new GameEngine());
        public static GameEngine GetSingletone { get { return instance.Value; } }

        private bool IsAlreadyDisposed = false;
        private CancellationTokenSource GameEngineCancleToken= new CancellationTokenSource();
        private Dictionary<int,MapData> MapDataDictionary = new Dictionary<int, MapData>();

        private GameEngine()
        {
        }
        public void Start()
        {
            LoadResource();
            Task.Run(() => Run());
        }

        public void LoadResource()
        {
            LogManager.GetSingletone.WriteLog("리소스를 로드합니다.");
            MapSystem.GetSingletone.LoadMapResource();
        }

        public void Run()
        {
            while(!GameEngineCancleToken.IsCancellationRequested)
            {
                MapSystem.GetSingletone.Update();
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

    }
}
