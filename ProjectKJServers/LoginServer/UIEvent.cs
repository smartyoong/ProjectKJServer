using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    /// <summary>
    /// UI관련 업데이트가 필요할때마다 중간에서 매개체 역할을 하는 클래스입니다.
    /// 싱글톤 패턴입니다.
    /// </summary>
    internal class UIEvent
    {
        private static UIEvent? Instance = null;

        // ListBox 등 UI에 표현하기 위해 이벤트 사용
        private event Action<string>? LogEvent;

        /// <value> UI 갱신용 이벤트.</value>
        public event Action<bool>? DBServerEvent;

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

        public void AddLogToUI(string log)
        {
            LogEvent?.Invoke(log);
        }

        public void UpdateDBServerStatus(bool IsConnected)
        {
            DBServerEvent?.Invoke(IsConnected);
        }

    }
}
