namespace CoreUtility.GlobalVariable
{
    /// <summary>
    /// UI관련 업데이트가 필요할때마다 중간에서 매개체 역할을 하는 클래스입니다.
    /// 싱글톤 패턴입니다.
    /// </summary>
    public class UIEvent
    {
        private static UIEvent? Instance = null;

        // ListBox 등 UI에 표현하기 위해 이벤트 사용
        private event Action<string>? LogEvent;

        /// <value> DB 서버 UI 갱신용 이벤트.</value>
        private event Action<bool>? DBServerEvent;

        /// <value> Game 서버 UI 갱신용 이벤트.</value>
        private event Action<bool>? GameServerEvent;

        /// <value> SQL 서버 UI 갱신용 이벤트.</value>
        private event Action<bool>? SQLEvent;

        /// <value> 로그인 서버 UI 갱신용 이벤트.</value>
        private event Action<bool>? LoginServerEvent;

        /// <value> 동접자 수 UI 갱신용 이벤트.</value>
        private event Action<bool>? UserCountEvent;

        /// <value> 로그 에러 전용 메세지 박스 출력용 이벤트.</value>
        private event Action<string>? LogErrorEvent;

        /// <value> CPU 사용량 출력용 이벤트.</value>
        private event Action<float>? CPUUsageEvent;

        /// <value> 메모리 사용량 출력용 이벤트.</value>
        private event Action<float>? MemoryUsageEvent;

        /// <value> 스레드 사용량 출력용 이벤트.</value>
        private event Action<int>? ThreadUsageEvent;

        /// <value> 디스크 사용량 출력용 이벤트.</value>
        private event Action<float>? DiskIOEvent;

        /// <value> 네트워크 사용량 출력용 이벤트.</value>
        private event Action<float>? NetworkUsageEvent;

        /// <value> 페이지 사용량 출력용 이벤트.</value>
        private event Action<float>? PageUsageEvent;

        /// <value> 파일 사용량 출력용 이벤트.</value>
        private event Action<float>? FileIOEvent;

        /// <value> 2세대 가비지컬렉션 출력용 이벤트.</value>
        private event Action<long>? GarbageCollectionEvent;

        /// <value> CPU 온도 출력용 이벤트.</value>
        private event Action<float>? CPUTemperatureEvent;

        /// <value> 시스템 로그 출력용 이벤트.</value>
        private event Action<string>? SystemLogEvent;

        private UIEvent()
        {
        }

        public static UIEvent GetSingletone
        {
            get
            {
                if (Instance == null)
                {
                    Instance = new UIEvent();
                }
                return Instance;
            }
            private set { }
        }

        public void SubscribeLogEvent(Action<string> action)
        {
            LogEvent += action;
        }

        public void UnsubscribeLogEvent(Action<string> action)
        {
            LogEvent -= action;
        }

        public void SubscribeDBServerStatusEvent(Action<bool> action)
        {
            DBServerEvent += action;
        }

        public void UnsubscribeDBServerStatusEvent(Action<bool> action)
        {
            DBServerEvent -= action;
        }

        public void SubscribeLoginServerStatusEvent(Action<bool> action)
        {
            LoginServerEvent += action;
        }

        public void UnsubscribeLoginServerStatusEvent(Action<bool> action)
        {
            LoginServerEvent -= action;
        }

        public void SubscribeGameServerStatusEvent(Action<bool> action)
        {
            GameServerEvent += action;
        }

        public void UnsubscribeGameServerStatusEvent(Action<bool> action)
        {
            GameServerEvent -= action;
        }

        public void SubscribeUserCountEvent(Action<bool> action)
        {
            UserCountEvent += action;
        }

        public void UnsubscribeUserCountEvent(Action<bool> action)
        {
            UserCountEvent -= action;
        }

        public void SubscribeSQLStatusEvent(Action<bool> action)
        {
            SQLEvent += action;
        }

        public void UnsubscribeSQLStatusEvent(Action<bool> action)
        {
            SQLEvent -= action;
        }

        public void SubscribeLogErrorEvent(Action<string> action)
        {
            LogErrorEvent += action;
        }

        public void UnsubscribeLogErrorEvent(Action<string> action)
        {
            LogErrorEvent -= action;
        }

        public void SubscribeCPUUsageEvent(Action<float> action)
        {
            CPUUsageEvent += action;
        }

        public void UnsubscribeCPUUsageEvent(Action<float> action)
        {
            CPUUsageEvent -= action;
        }

        public void SubscribeMemoryUsageEvent(Action<float> action)
        {
            MemoryUsageEvent += action;
        }

        public void UnsubscribeMemoryUsageEvent(Action<float> action)
        {
            MemoryUsageEvent -= action;
        }

        public void SubscribeThreadUsageEvent(Action<int> action)
        {
            ThreadUsageEvent += action;
        }

        public void UnsubscribeThreadUsageEvent(Action<int> action)
        {
            ThreadUsageEvent -= action;
        }

        public void SubscribeDiskIOEvent(Action<float> action)
        {
            DiskIOEvent += action;
        }

        public void UnsubscribeDiskIOEvent(Action<float> action)
        {
            DiskIOEvent -= action;
        }

        public void SubscribeNetworkUsageEvent(Action<float> action)
        {
            NetworkUsageEvent += action;
        }

        public void UnsubscribeNetworkUsageEvent(Action<float> action)
        {
            NetworkUsageEvent -= action;
        }

        public void SubscribePageUsageEvent(Action<float> action)
        {
            PageUsageEvent += action;
        }

        public void UnsubscribePageUsageEvent(Action<float> action)
        {
            PageUsageEvent -= action;
        }

        public void SubscribeFileIOEvent(Action<float> action)
        {
            FileIOEvent += action;
        }

        public void UnsubscribeFileIOEvent(Action<float> action)
        {
            FileIOEvent -= action;
        }

        public void SubscribeGarbageCollectionEvent(Action<long> action)
        {
            GarbageCollectionEvent += action;
        }

        public void UnsubscribeGarbageCollectionEvent(Action<long> action)
        {
            GarbageCollectionEvent -= action;
        }

        public void SubscribeCPUTemperatureEvent(Action<float> action)
        {
            CPUTemperatureEvent += action;
        }

        public void UnsubscribeCPUTemperatureEvent(Action<float> action)
        {
            CPUTemperatureEvent -= action;
        }

        public void SubscribeSystemLogEvent(Action<string> action)
        {
            SystemLogEvent += action;
        }

        public void UnsubscribeSystemLogEvent(Action<string> action)
        {
            SystemLogEvent -= action;
        }

        public void AddLogToUI(string log)
        {
            LogEvent?.Invoke(log);
        }

        public void UpdateLoginServerStatus(bool IsConnected)
        {
            LoginServerEvent?.Invoke(IsConnected);
        }

        public void UpdateDBServerStatus(bool IsConnected)
        {
            // DB서버와 연결 상태를 UI에 표시하기 위한 이벤트
            DBServerEvent?.Invoke(IsConnected);
        }
        public void UpdateGameServerStatus(bool IsConnected)
        {
            // Game서버와 연결 상태를 UI에 표시하기 위한 이벤트
            GameServerEvent?.Invoke(IsConnected);
        }
        public void UpdateSQLStatus(bool IsConnected)
        {
            // SQL서버와 연결 상태를 UI에 표시하기 위한 이벤트
            SQLEvent?.Invoke(IsConnected);
        }
        public void IncreaseUserCount(bool IsIncrease)
        {
            UserCountEvent?.Invoke(IsIncrease);
        }

        public void ShowMessageBoxLogError(string? Log)
        {
            if (Log == null)
                return;
            LogErrorEvent?.Invoke(Log);
        }

        public void UpdateCPUUsage(float Usage)
        {
            CPUUsageEvent?.Invoke(Usage);
        }

        public void UpdateMemoryUsage(float Usage)
        {
            MemoryUsageEvent?.Invoke(Usage);
        }

        public void UpdateThreadUsage(int Usage)
        {
            ThreadUsageEvent?.Invoke(Usage);
        }

        public void UpdateDiskIO(float Usage)
        {
            DiskIOEvent?.Invoke(Usage);
        }

        public void UpdateNetworkUsage(float Usage)
        {
            NetworkUsageEvent?.Invoke(Usage);
        }

        public void UpdatePageUsage(float Usage)
        {
            PageUsageEvent?.Invoke(Usage);
        }

        public void UpdateFileIO(float Usage)
        {
            FileIOEvent?.Invoke(Usage);
        }

        public void UpdateGarbageCollection(long Usage)
        {
            GarbageCollectionEvent?.Invoke(Usage);
        }

        public void UpdateCPUTemperature(float Usage)
        {
            CPUTemperatureEvent?.Invoke(Usage);
        }

        public void UpdateSystemLog(string Log)
        {
            SystemLogEvent?.Invoke(Log);
        }

    }
}
