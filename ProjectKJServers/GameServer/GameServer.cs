using KYCLog;
using KYCSocketCore;
using KYCUIEventManager;
using System.Net.Sockets;

namespace GameServer
{
    public partial class GameServer : Form
    {
        private int CurrentUserCount = 0;
        private TaskCompletionSource<bool> LoginServerReadyEvent = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> DBServerReadyEvent = new TaskCompletionSource<bool>();
        public GameServer()
        {
            InitializeComponent();
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LogManager.SetLogPath(GameServerSettings.Default.LogDirectory);
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
            UIEvent.GetSingletone.SubscribeDBServerStatusEvent(
                IsConnected =>
                {
                    DBServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            DBServerStatusTextBox.BackColor = Color.Blue;
                        }
                        else
                        {
                            DBServerStatusTextBox.BackColor = Color.Red;
                        }
                    }
                    );
                }
            );
            UIEvent.GetSingletone.SubscribeGameServerStatusEvent(
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
            UIEvent.GetSingletone.SubscribeLoginServerStatusEvent(
                IsConnected =>
                {
                    LoginServerStatusTextBox.Invoke(() =>
                    {
                        if (IsConnected)
                        {
                            LoginServerStatusTextBox.BackColor = Color.Blue;

                        }
                        else
                        {
                            LoginServerStatusTextBox.BackColor = Color.Red;
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
            LoginServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.BackColor = Color.Red;
            ServerStatusTextBox.BackColor = Color.Red;
            DBServerStatusTextBox.ReadOnly = true;
            DBServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            LoginServerStatusTextBox.ReadOnly = true;
            LoginServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.Text = CurrentUserCount.ToString();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("게임 서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            LogManager.GetSingletone.WriteLog("게임 엔진을 시작합니다.");
            GameEngine.GetSingletone.Start();
            LogManager.GetSingletone.WriteLog("로그인 서버의 연결을 대기합니다.");
            LoginServerAcceptor.GetSingletone.Start(LoginServerReadyEvent);
            await LoginServerReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("로그인 서버와 연결되었습니다.");
            LogManager.GetSingletone.WriteLog("DB 서버의 연결을 대기합니다.");
            DBServerConnector.GetSingletone.Start(DBServerReadyEvent);
            await DBServerReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("DB 서버와 연결되었습니다.");
            LogManager.GetSingletone.WriteLog("클라이언트의 연결을 받겠습니다..");
            ClientAcceptor.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("로그인 서버와 연결을 중단합니다.");
            await LoginServerAcceptor.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("로그인 서버 송신 파이프라인을 중단합니다.");
            LoginServerSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("로그인 서버 수신 파이프라인을 중단합니다.");
            LoginServerRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB 서버와 연결을 중단합니다.");
            await DBServerConnector.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB 서버 송신 파이프라인을 중단합니다.");
            DBServerSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB 서버 수신 파이프라인을 중단합니다.");
            DBServerRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("클라이언트와 연결을 중단합니다.");
            await ClientAcceptor.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("클라이언트 송신 파이프라인을 중단합니다.");
            ClientSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("클라이언트 수신 파이프라인을 중단합니다.");
            ClientRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("연결중인 모든 소켓을 중단합니다.");
            await SocketManager.GetSingletone.Cancel();
            LogManager.GetSingletone.WriteLog("잠시후 로그 매니저를 종료하고 게임 서버를 종료합니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Application.Exit();
        }
    }
}
