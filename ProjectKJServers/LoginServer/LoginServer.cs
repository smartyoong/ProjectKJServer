using System.Windows.Forms;

namespace LoginServer
{
    /// <summary>
    /// LoginServer Ŭ���� �Դϴ�.
    /// MainForm������ �ϸ�, UI �� ����ڿ��� ��ȣ�ۿ��� ����մϴ�.
    /// </summary>
    public partial class LoginServer : Form
    {
        /// <summary>
        /// LoginServer �������Դϴ�.
        /// ��� LoginServer�� Ŭ������ �ʱ�ȭ �۾��� �����մϴ�.
        /// LogManager Ŭ������ ���� ���� �۾��� �����մϴ�.
        /// LogManager Ŭ������ LogEvent �̺�Ʈ�� �����մϴ�.
        /// LogManager Ŭ���� ������ ������ �߻��ϸ� ���α׷��� ����˴ϴ�.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">LogManager ������ ���丮 ������ �����ϸ� ���α׷��� ����˴ϴ�.</exception>
        /// <see cref="LogManager()"/>
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
            LogManager.GetSingletone.WriteLog("���� ����");
        }
    }
}
