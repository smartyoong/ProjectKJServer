using KYCLog;
using KYCUIEventManager;

namespace GameServer
{
    public partial class GameServer : Form
    {
        private int CurrentUserCount = 0;
        public GameServer()
        {
            InitializeComponent();
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

        private void ServerStartButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("게임 서버를 시작합니다.");
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("잠시후 게임 서버를 종료합니다.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            Environment.Exit(0);
        }
    }
}
