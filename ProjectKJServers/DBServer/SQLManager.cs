using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    internal class SQLManager
    {
        private SQLExecuter SQLWorker;
        
        private static readonly Lazy<SQLManager> instance = new Lazy<SQLManager>(() => new SQLManager());
        public static SQLManager GetSingletone => instance.Value;


        private SQLManager()
        {
                SQLWorker = new SQLExecuter(DBServerSettings.Default.SQLDataSoruce, DBServerSettings.Default.SQLAccountDataBaseName,
                    DBServerSettings.Default.SQLSecurity, DBServerSettings.Default.SQLPoolMinSize, DBServerSettings.Default.SQLPoolMaxSize,DBServerSettings.Default.SQLTimeOut);
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
