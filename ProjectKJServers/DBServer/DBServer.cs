using KYCUIEventManager;
using KYCLog;
using KYCSQL;

namespace DBServer
{
    public partial class DBServer : Form
    {
        public DBServer()
        {
            InitializeComponent();
            LogManager.SetLogPath(DBServerSettings.Default.LogDirectory);
            ServerStopButton.Enabled = false;
            ServerStatusTextBox.BackColor = Color.Red;
            LoginServerStatusTextBox.BackColor = Color.Red;
            GameServerStatusTextBox.BackColor = Color.Red;
            LoginServerStatusTextBox.ReadOnly = true;
            LoginServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            GameServerStatusTextBox.ReadOnly = true;
            GameServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            ServerStatusTextBox.ReadOnly = true;
            ServerStatusTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            CurrentUserCountTextBox.ReadOnly = true;
            CurrentUserCountTextBox.GotFocus += (s, e) => { LogListBox.Focus(); };
            SubscribeAllEvent();
        }

        private void SubscribeAllEvent()
        {
            UIEvent.GetSingletone.SubscribeLogEvent(Log =>
            {
                LogListBox.Invoke((() =>
                {
                    LogListBox.Items.Add(Log);
                    LogListBox.TopIndex = LogListBox.Items.Count - 1;
                }));
            }
);
            UIEvent.GetSingletone.SubscribeDBServerStatusEvent(IsConnected =>
            {
                ServerStatusTextBox.Invoke((() =>
                {
                    if (IsConnected)
                    {
                        ServerStatusTextBox.BackColor = Color.Blue;
                    }
                    else
                    {
                        ServerStatusTextBox.BackColor = Color.Red;
                    }
                }));
            }
            );
            UIEvent.GetSingletone.SubscribeLoginServerStatusEvent(IsConnected =>
            {
                ServerStatusTextBox.Invoke((() =>
                {
                    if (IsConnected)
                    {
                        LoginServerStatusTextBox.BackColor = Color.Blue;
                    }
                    else
                    {
                        LoginServerStatusTextBox.BackColor = Color.Red;
                    }
                }));
            }
            );
            UIEvent.GetSingletone.SubscribeLogErrorEvent(log => MessageBox.Show(log));
        }

        private async void ServerStartButton_Click(object sender, EventArgs e)
        {
            ServerStartButton.Enabled = false;
            ServerStopButton.Enabled = true;
            await LogManager.GetSingletone.WriteLog("서버를 가동합니다.").ConfigureAwait(false);
            await GameSQLManager.GetSingletone.ConnectToSQL().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("로그인 서버의 연결의 대기합니다.").ConfigureAwait(false);
            GameServerAcceptor.GetSingletone.Start();
        }

        private async void ServerStopButton_Click(object sender, EventArgs e)
        {
            ServerStopButton.Enabled = false;
            await LogManager.GetSingletone.WriteLog("서버를 중지합니다.").ConfigureAwait(false);
            await GameSQLManager.GetSingletone.StopSQL().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("SQL 서버와 연결을 중단했습니다.").ConfigureAwait(false);
            await GameServerAcceptor.GetSingletone.Stop().ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("로그인 서버와의 연결을 중단했습니다.").ConfigureAwait(false);
            await LogManager.GetSingletone.WriteLog("서버를 중지했습니다. 잠시후 종료됩니다.").ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            LogManager.GetSingletone.Close();
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            Environment.Exit(0);
        }
    }
}
