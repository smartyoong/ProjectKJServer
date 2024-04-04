namespace DBServer
{
    public partial class DBServer : Form
    {
        public DBServer()
        {
            InitializeComponent();
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
                            ServerStatusTextBox.BackColor = Color.Green;
                            ServerStatusTextBox.Text = "서버 가동중";
                        }
                        else
                        {
                            ServerStatusTextBox.BackColor = Color.Red;
                            ServerStatusTextBox.Text = "서버 중지중";
                        }
                    }));
                }
            );
        }
    }
}
