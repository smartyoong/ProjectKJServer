using LoginServer.Properties;
using System.Net;
using KYCUIEventManager;
using KYCSocketCore;
using KYCException;
using KYCLog;
using System.Net.Sockets;

namespace LoginServer
{

    /// <summary>
    /// 게임서버와 연결을 담당하는 클래스입니다.
    /// 사실상 모든 연결 기능은 Connector 클래스의 기능을 사용합니다.
    /// 이 클래스를 만든 이유는, 명시적으로 DB서버와 통신하는 객체인것을 알려주기 위함이고
    /// UI EVENT와 DB서버 관련 패킷 Process의 매개자 역할을 하기 위함입니다.
    /// 즉, 핵심 기능들은 Connector 클래스에 존재하고 상속을 받아서, 명시적 표현 및 결합도 관련 로직을 담당합니다.
    /// </summary>
    internal class GameServerConnector : Connector, IDisposable
    {
        private bool IsAlreadyDisposed = false;
        /// <value>지연 생성 및 싱글톤 패턴을 사용합니다.</value>
        private static readonly Lazy<GameServerConnector> Lazy = new Lazy<GameServerConnector>(() => new GameServerConnector());

        public static GameServerConnector GetSingletone { get { return Lazy.Value; } }

        private CancellationTokenSource CheckProcessToken;

        private TaskCompletionSource<bool>? ServerReadeyEvent;


        /// <summary>
        /// GameServer 클래스의 생성자입니다.
        /// 소켓 연결 갯수만큼 클래스를 생성하고, 초기화시킵니다.
        /// </summary>
        private GameServerConnector() : base(Settings.Default.GameServerConnectCount)
        {
            CheckProcessToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Game서버와의 연결을 시도합니다.
        /// 모든 소켓이 연결에 성공하면 DB서버와 연결되었다는 이벤트를 발생시킵니다.
        /// </summary>
        public void Start(TaskCompletionSource<bool>? ServerEvent)
        {
            Start(new IPEndPoint(IPAddress.Parse(Settings.Default.GameServerIPAddress), Settings.Default.GameServerPort), "Game서버");
            ProcessCheck();
            // Recv하는거 추가해야함
            ServerReadeyEvent = ServerEvent;
            GetRecvPacket();
        }

        /// <summary>
        /// Game서버와의 연결을 종료합니다.
        /// 모든 소켓이 연결 종료에 성공하면 DB서버와 종료되었다는 이벤트를 발생시킵니다.
        /// 반드시 부모의 Stop 메서드를 호출하고
        /// 본인의 Dispose 메서드를 호출하세요.
        /// DelayTime도 몇초 정도 설정해놔야 문제없이 종료됩니다.
        /// </summary>
        public async Task Stop()
        {
            await Stop("Game서버",TimeSpan.FromSeconds(3));
            UIEvent.GetSingletone.UpdateGameServerStatus(false);
            Dispose();
        }


        /// <summary>
        /// 종료자입니다.
        /// 최후의 수단이며, 직접 사용은 절대하지 마세요.
        /// </summary>
        ~GameServerConnector()
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
            CheckProcessToken.Dispose();
            base.Dispose(Disposing);
            IsAlreadyDisposed = true;
        }

        private void ProcessCheck()
        {
            Task.Run(async () => {
                while (!CheckProcessToken.IsCancellationRequested)
                {
                    if (IsConnected())
                    {
                        UIEvent.GetSingletone.UpdateGameServerStatus(true);
                        if(ServerReadeyEvent != null)
                        {
                            ServerReadeyEvent.SetResult(true);
                            ServerReadeyEvent = null;
                        }
                    }
                    else
                        UIEvent.GetSingletone.UpdateGameServerStatus(false);
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }, CheckProcessToken.Token);
        }

        public void GetRecvPacket()
        {
            Task.Run(async () =>
            {
                while (!CheckProcessToken.IsCancellationRequested)
                {
                    try
                    {
                        var DataBuffer = await RecvData().ConfigureAwait(false);
                        // 파이프라인에 추가하는거 작업해야함
                    }
                    catch (ConnectionClosedException e)
                    {
                        // 연결종료됨 다시 연결을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e);
                        LogManager.GetSingletone.WriteLog("GameServer와 연결이 끊겼습니다");
                        // 만약 서버가 죽은거라면 관련된 모든 소켓이 죽었을 것이기 때문에 모두 처음부터 다시 연결을 해야한다
                        PrepareToReConnect("GameServer");
                        Start(new IPEndPoint(IPAddress.Parse(Settings.Default.GameServerIPAddress), Settings.Default.GameServerPort), "GameServer");
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 가용가능한 소켓이 없음 1초 대기후 다시 소켓을 받아오도록 지시
                        LogManager.GetSingletone.WriteLog(e);
                        LogManager.GetSingletone.WriteLog("LoginServerAcceptor에서 데이터를 Recv할 Socket이 부족하여 TimeOut이 되었습니다.");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // 그외 에러
                        LogManager.GetSingletone.WriteLog(e);
                    }
                }
            });
        }
        public async Task<int> Send(Memory<byte> Data)
        {
            return await SendData(Data).ConfigureAwait(false);
        }
    }
}
