using KYCLog;
using KYCSocketCore;
using KYCSQL;
using KYCUIEventManager;
using LoginServer.Properties;

namespace LoginServer
{
    /// <summary>
    /// LoginServer 클래스 입니다.
    /// MainForm역할을 하며, UI 및 사용자와의 상호작용을 담당합니다.
    /// </summary>
    public partial class LoginServer : Form
    {
        private int CurrentUserCount = 0;
        private TaskCompletionSource<bool> GameServerReadyEvent = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> SQLReadyEvent = new TaskCompletionSource<bool>();
        /// <summary>
        /// LoginServer 생성자입니다.
        /// 모든 LoginServer의 클래스의 초기화 작업을 진행합니다.
        /// LogManager 클래스의 내부 생성 작업을 진행합니다.
        /// LogManager 클래스의 LogEvent 이벤트를 구독합니다.
        /// LogManager 클래스 생성중 에러가 발생하면 프로그램이 종료됩니다.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">LogManager 생성시 디렉토리 생성에 실패하면 프로그램이 종료됩니다.</exception>
        /// <see cref="LogManager()"/>
        public LoginServer()
        {
            InitializeComponent();
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LogManager.SetLogPath(Settings.Default.LogDirectory);
            UIEvent.GetSingletone.SubscribeLogErrorEvent(log => MessageBox.Show(log));
            UIEvent.GetSingletone.SubscribeLogEvent(
            Log =>
                {
                    LogListBox.Invoke(() =>
                    {
                        LogListBox.Items.Add(Log);
                        LogListBox.TopIndex = LogListBox.Items.Count - 1;
                    });
                }
            );
            UIEvent.GetSingletone.SubscribeSQLStatusEvent(
                IsConnected =>
                {
                    SQLStatusTextBox.Invoke(() =>
                        {
                            if (IsConnected)
                            {
                                SQLStatusTextBox.BackColor = Color.Blue;
                            }
                            else
                            {
                                SQLStatusTextBox.BackColor = Color.Red;
                            }
                        }
                    );
                }
            );
            UIEvent.GetSingletone.SubscribeGameServerStatusEvent(
                IsConnected =>
                {
                    GameServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            GameServerStatusTextBox.BackColor = Color.Blue;

                        }
                        else
                        {
                            GameServerStatusTextBox.BackColor = Color.Red;
                        }
                    });
                }
            );
            UIEvent.GetSingletone.SubscribeLoginServerStatusEvent(
                IsConnected =>
                {
                    ServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            ServerStatusTextBox.BackColor = Color.Blue;

                        }
                        else
                        {
                            ServerStatusTextBox.BackColor = Color.Red;
                        }
                    });
                }
             );
            UIEvent.GetSingletone.SubscribeUserCountEvent(
                IsIncrease =>
                {
                    ServerStatusTextBox.Invoke(() =>
                    {
                        if (IsIncrease)
                        {
                            CurrentUserCount++;
                            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
                        }
                        else
                        {
                            CurrentUserCount--;
                            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
                        }
                    });
                }
             );
            ServerStopButton.Enabled = false;
            ServerStatusTextBox.BackColor = Color.Red;
            SQLStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            SQLStatusTextBox.ReadOnly = true;
            SQLStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
        }

        // MiddleWare 패턴 같이 처리안된 모든 에러를 처리
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // UI와 작동하는 스레드 이기때문에 ConfigureAwait(false)를 사용하지 않습니다.
        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            await AccountSQLManager.GetSingletone.ConnectToSQL(SQLReadyEvent);
            await SQLReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("SQL 서버와 연결이 완료됐습니다.");
            GameServerConnector.GetSingletone.Start(GameServerReadyEvent);
            await GameServerReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("게임 서버와 연결이 완료됐습니다.");
            ClientAcceptor.GetSingletone.Start();
            LogManager.GetSingletone.WriteLog("서버를 시작완료");

        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("접속한 유저들과 연결을 끊습니다");
            await ClientAcceptor.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("유저들의 수신 패킷 파이프라인을 종료합니다");
            ClientRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("유저들의 송신 패킷 파이프라인을 종료합니다");
            ClientSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("게임서버와 연결을 끊습니다");
            await GameServerConnector.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("게임서버의 수신 파이프라인을 종료합니다");
            GameServerRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("게임서버의 송신 파이프라인을 종료합니다");
            GameServerSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("게임 서버와 연결을 종료했습니다.");
            LogManager.GetSingletone.WriteLog("SQL 서버를 종료합니다.");
            await AccountSQLManager.GetSingletone.StopSQL();
            LogManager.GetSingletone.WriteLog("SQL 서버와 연결이 종료됐습니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await SocketManager.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("모든 소켓이 연결 종료되었습니다 로그 매니저 차단후 프로그램 종료됩니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GameServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(LoginGamePacketListID.REQUEST_USER_INFO_SUMMARY, new RequestUserInfoSummaryPacket("sex", "sex"));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GameServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(LoginGamePacketListID.REQUEST_USER_INFO_SUMMARY, new RequestUserInfoSummaryPacket("sex", "sex"));
        }
    }
}
