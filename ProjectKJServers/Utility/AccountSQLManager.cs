using CoreUtility;

namespace KYCSQL
{
    public class AccountSQLManager
    {
        private SQLExecuter SQLWorker;

        private static readonly Lazy<AccountSQLManager> instance = new Lazy<AccountSQLManager>(() => new AccountSQLManager());
        public static AccountSQLManager GetSingletone => instance.Value;



        private AccountSQLManager()
        {
            SQLWorker = new SQLExecuter(CoreSettings.Default.SQLDataSoruce, CoreSettings.Default.SQLAccountDataBaseName,
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

        // 이제 패킷 매개변수를 가지고 SP 호출하는 서브 함수를 만들고
        // 외부에 공개하는건 패킷만 던져주면 알아서 만드는걸로 해야겠다.


    }
}
