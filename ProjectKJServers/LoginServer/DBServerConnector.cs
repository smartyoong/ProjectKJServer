using LoginServer.Properties;
using System.Net;
using System.Net.Sockets;

namespace LoginServer
{
    internal class DBServerConnector : Connector, IDisposable
    {
        private bool IsAlreadyDisposed = false;
        /// <value>지연 생성 및 싱글톤 패턴을 사용합니다.</value>
        private static readonly Lazy<DBServerConnector> Lazy = new Lazy<DBServerConnector>(() => new DBServerConnector());

        public static DBServerConnector GetSingletone { get { return Lazy.Value; } }


        /// <summary>
        /// DBServer 클래스의 생성자입니다.
        /// 소켓 연결 갯수만큼 클래스를 생성하고, 초기화시킵니다.
        /// </summary>
        private DBServerConnector()
        {
            Init(Settings.Default.DBServerConnectCount);
        }

        protected override void Init(int MakeSocketCount)
        {
            for (int i = 0; i < MakeSocketCount; i++)
            {
                ConnectSocketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }
        }

        /// <summary>
        /// DB서버와의 연결을 시도합니다.
        /// 모든 소켓이 연결에 성공하면 DB서버와 연결되었다는 이벤트를 발생시킵니다.
        /// </summary>
        public void Start()
        {
            Start("DB서버");
        }

        /// <summary>
        /// DB서버와의 연결을 종료합니다.
        /// 모든 소켓이 연결 종료에 성공하면 DB서버와 종료되었다는 이벤트를 발생시킵니다.
        /// 반드시 부모의 Stop 메서드를 호출하고
        /// 본인의 Dispose 메서드를 호출하세요.
        /// DelayTime도 몇초 정도 설정해놔야 문제없이 종료됩니다.
        /// </summary>
        public async Task Stop()
        {
            await Stop("DB서버",3000);
            UIEvent.GetSingletone.UpdateDBServerStatus(false);
            Dispose();
        }


        /// <summary>
        /// 종료자입니다.
        /// 최후의 수단이며, 직접 사용은 절대하지 마세요.
        /// </summary>
        ~DBServerConnector()
        {
            Dispose(false);
        }

        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Close 메서드를 사용하세요.
        /// </summary>
        public override void Dispose()
        {
            if (IsAlreadyDisposed)
            {
                return;
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 모든 비관리 리소스를 해제합니다.
        /// Stop 메서드를 사용하세요.
        /// </summary>
        /// <seealso cref="Stop()"/>"/>
        protected override void Dispose(bool Disposing)
        {
            if (IsAlreadyDisposed)
                return;
            if (Disposing)
            {

            }
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
        }
    }
}
