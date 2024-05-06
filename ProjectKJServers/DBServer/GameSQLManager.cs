using DBServer;
using KYCUIEventManager;

namespace KYCSQL
{
    public class GameSQLManager
    {
        private SQLExecuter SQLWorker;

        private static readonly Lazy<GameSQLManager> instance = new Lazy<GameSQLManager>(() => new GameSQLManager());
        public static GameSQLManager GetSingletone => instance.Value;



        private GameSQLManager()
        {
            SQLWorker = new SQLExecuter(DBServerSettings.Default.SQLDataSoruce, DBServerSettings.Default.SQLGameDataBaseName,
                DBServerSettings.Default.SQLSecurity, DBServerSettings.Default.SQLPoolMinSize, DBServerSettings.Default.SQLPoolMaxSize, DBServerSettings.Default.SQLTimeOut);
        }

        public async Task ConnectToSQL()
        {
            await SQLWorker.TryConnect().ConfigureAwait(false);
            UIEvent.GetSingletone.UpdateDBServerStatus(true);
        }

        public async Task StopSQL()
        {
            await SQLWorker.Cancel().ConfigureAwait(false);
            UIEvent.GetSingletone.UpdateDBServerStatus(false);
        }
    }
}
