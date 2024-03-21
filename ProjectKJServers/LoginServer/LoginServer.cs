using System.Windows.Forms;

namespace LoginServer
{
    public partial class LoginServer : Form
    {
        public LoginServer()
        {
            InitializeComponent();
            LogManager.GetSingletone.LogEvent += Log =>
            {
                LogListBox.Invoke((Action)(() =>
                {
                    LogListBox.Items.Add(Log);
                    LogListBox.TopIndex = LogListBox.Items.Count - 1;
                }));
            };
        }

        private void ServerStartButton_Click(object sender, EventArgs e)
        {
            LogManager.GetSingletone.WriteLog("서버 시작");
        }
    }
}
