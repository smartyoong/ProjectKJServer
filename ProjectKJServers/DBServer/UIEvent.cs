﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
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
        private event Action<bool>? DBServerEvent;

        /// <value> UI 갱신용 이벤트.</value>
        private event Action<bool>? LoginServerEvent;

        /// <value> UI 갱신용 이벤트.</value>
        private event Action<bool>? UserCountEvent;

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

        public void SubscribeUserCountEvent(Action<bool> action)
        {
            UserCountEvent += action;
        }

        public void UnsubscribeUserCountEvent(Action<bool> action)
        {
            UserCountEvent -= action;
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
            // SQL서버와 연결 상태를 UI에 표시하기 위한 이벤트
            DBServerEvent?.Invoke(IsConnected);
        }
        public void IncreaseUserCount(bool IsIncrease)
        {
            UserCountEvent?.Invoke(IsIncrease);
        }


    }
}
