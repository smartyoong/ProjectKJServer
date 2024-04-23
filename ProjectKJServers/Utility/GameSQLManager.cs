using CoreUtility;

namespace KYCSQL
{
    public class GameSQLManager
    {
        private SQLExecuter SQLWorker;

        private static readonly Lazy<GameSQLManager> instance = new Lazy<GameSQLManager>(() => new GameSQLManager());
        public static GameSQLManager GetSingletone => instance.Value;



        private GameSQLManager()
        {
            SQLWorker = new SQLExecuter(CoreSettings.Default.SQLDataSoruce, CoreSettings.Default.SQLGameDataBaseName,
                CoreSettings.Default.SQLSecurity, CoreSettings.Default.SQLPoolMinSize, CoreSettings.Default.SQLPoolMaxSize, CoreSettings.Default.SQLTimeOut);
        }

        public async Task ConnectToSQL()
        {
            await SQLWorker.TryConnect().ConfigureAwait(false);
        }

        public async Task StopSQL()
        {
            await SQLWorker.Cancel().ConfigureAwait(false);
        }
    }
}
