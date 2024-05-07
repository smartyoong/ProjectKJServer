using DBServer;
using KYCUIEventManager;

namespace KYCSQL
{
    public class GameSQLPipeLine
    {
        private SQLExecuter SQLWorker;

        private static readonly Lazy<GameSQLPipeLine> instance = new Lazy<GameSQLPipeLine>(() => new GameSQLPipeLine());
        public static GameSQLPipeLine GetSingletone => instance.Value;

        private TaskCompletionSource<bool>? ServerReadeyEvent;



        private GameSQLPipeLine()
        {
            SQLWorker = new SQLExecuter(DBServerSettings.Default.SQLDataSoruce, DBServerSettings.Default.SQLGameDataBaseName,
                DBServerSettings.Default.SQLSecurity, DBServerSettings.Default.SQLPoolMinSize, DBServerSettings.Default.SQLPoolMaxSize, DBServerSettings.Default.SQLTimeOut);
        }

        public async Task ConnectToSQL(TaskCompletionSource<bool> ServerEvent)
        {
            ServerReadeyEvent = ServerEvent;
            await SQLWorker.TryConnect().ConfigureAwait(false);
            if (ServerReadeyEvent != null)
            {
                ServerReadeyEvent.SetResult(true);
                ServerReadeyEvent = null;
            }
        }

        public async Task StopSQL()
        {
            await SQLWorker.Cancel().ConfigureAwait(false);
        }
    }
}
