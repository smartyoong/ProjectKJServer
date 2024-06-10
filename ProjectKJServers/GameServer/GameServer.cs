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
            LogManager.GetSingletone.WriteLog("���� ������ �����մϴ�.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            LogManager.GetSingletone.WriteLog("���� ������ �����մϴ�.");
            GameEngine.GetSingletone.Start();
            LogManager.GetSingletone.WriteLog("�α��� ������ ������ ����մϴ�.");
            LoginServerAcceptor.GetSingletone.Start(LoginServerReadyEvent);
            await LoginServerReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("�α��� ������ ����Ǿ����ϴ�.");
            LogManager.GetSingletone.WriteLog("DB ������ ������ ����մϴ�.");
            DBServerConnector.GetSingletone.Start(DBServerReadyEvent);
            await DBServerReadyEvent.Task;
            LogManager.GetSingletone.WriteLog("DB ������ ����Ǿ����ϴ�.");
            LogManager.GetSingletone.WriteLog("Ŭ���̾�Ʈ�� ������ �ްڽ��ϴ�..");
            ClientAcceptor.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("�α��� ������ ������ �ߴ��մϴ�.");
            await LoginServerAcceptor.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("�α��� ���� �۽� ������������ �ߴ��մϴ�.");
            LoginServerSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("�α��� ���� ���� ������������ �ߴ��մϴ�.");
            LoginServerRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB ������ ������ �ߴ��մϴ�.");
            await DBServerConnector.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB ���� �۽� ������������ �ߴ��մϴ�.");
            DBServerSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("DB ���� ���� ������������ �ߴ��մϴ�.");
            DBServerRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("Ŭ���̾�Ʈ�� ������ �ߴ��մϴ�.");
            await ClientAcceptor.GetSingletone.Stop();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("Ŭ���̾�Ʈ �۽� ������������ �ߴ��մϴ�.");
            ClientSendPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("Ŭ���̾�Ʈ ���� ������������ �ߴ��մϴ�.");
            ClientRecvPacketPipeline.GetSingletone.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.WriteLog("�������� ��� ������ �ߴ��մϴ�.");
            await SocketManager.GetSingletone.Cancel();
            LogManager.GetSingletone.WriteLog("����� �α� �Ŵ����� �����ϰ� ���� ������ �����մϴ�.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Application.Exit();
        }
    }
}
