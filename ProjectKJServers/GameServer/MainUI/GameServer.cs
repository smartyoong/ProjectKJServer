using CoreUtility.GlobalVariable;
using CoreUtility.SocketCore;
using CoreUtility.Utility;
using GameServer.Component;
using GameServer.GameSystem;
using GameServer.MainUI;
using GameServer.PacketPipeLine;
using GameServer.SocketConnect;
using System.Net.Sockets;

namespace GameServer
{
    public partial class GameServer : Form
    {
        private int CurrentUserCount = 0;
        private TaskCompletionSource<bool> LoginServerReadyEvent = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> DBServerReadyEvent = new TaskCompletionSource<bool>();

        private delegate void DelegateWriteLog(string Log);
        private delegate void DelegateWriteErrorLog(Exception ex);
        private DelegateWriteLog WriteFileLog;
        private DelegateWriteErrorLog WriteErrorLog;

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
            WriteFileLog = LogManager.GetSingletone.WriteLog;
            WriteErrorLog = LogManager.GetSingletone.WriteLog;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.Message, "GameServer Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            MessageBox.Show((e.ExceptionObject as Exception)?.StackTrace, "GameServer Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "GameServer Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            MessageBox.Show(e.Exception.StackTrace, "GameServer Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            WriteFileLog("게임 서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            WriteFileLog("게임 엔진을 시작합니다.");
            MainProxy.GetSingletone.StartGameEngine();
            WriteFileLog("로그인 서버의 연결을 대기합니다.");
            MainProxy.GetSingletone.StartAcceptLoginServer(LoginServerReadyEvent);
            await LoginServerReadyEvent.Task;
            WriteFileLog("로그인 서버와 연결되었습니다.");
            WriteFileLog("DB 서버의 연결을 대기합니다.");
            MainProxy.GetSingletone.ConnectToDBServer(DBServerReadyEvent);
            await DBServerReadyEvent.Task;
            WriteFileLog("DB 서버와 연결되었습니다.");
            WriteFileLog("클라이언트의 연결을 받겠습니다..");
            MainProxy.GetSingletone.StartAcceptClient();

            // 임시 테스트 코드
            ArcKinematicComponent Test = new ArcKinematicComponent(new System.Numerics.Vector3(0,0,0), new System.Numerics.Vector3(100, 100, 0), 500000);
            MainProxy.GetSingletone.AddArcKinematicComponent(Test);
            LogManager.GetSingletone.WriteLog("테스트 시작.");
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            WriteFileLog("게임 서버를 중단합니다.");
            MainProxy.GetSingletone.StopGameEngine();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopAcceptLoginServer();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopLoginServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("로그인 서버 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopLoginServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopConnectToDBServer();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopDBServerSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("DB 서버 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopDBServerRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트와 연결을 중단합니다.");
            await MainProxy.GetSingletone.StopAcceptClient();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트 송신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopClientSendPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("클라이언트 수신 파이프라인을 중단합니다.");
            MainProxy.GetSingletone.StopClientRecvPacketPipeline();
            await Task.Delay(TimeSpan.FromSeconds(2));
            WriteFileLog("연결중인 모든 소켓을 중단합니다.");
            await SocketManager.GetSingletone.Cancel();
            WriteFileLog("잠시후 로그 매니저를 종료하고 게임 서버를 종료합니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Application.Exit();
        }
    }
}
