using DBServer;
using KYCLog;
using KYCUIEventManager;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Channels;

namespace KYCSQL
{
    public class GameSQLPipeLine
    {
        private SQLExecuter SQLWorker;

        private static readonly Lazy<GameSQLPipeLine> instance = new Lazy<GameSQLPipeLine>(() => new GameSQLPipeLine());
        public static GameSQLPipeLine GetSingletone => instance.Value;

        private Channel<(DB_SP ID, SqlParameter[] parameters)> SQLChannel = Channel.CreateUnbounded<(DB_SP ID, SqlParameter[])>();

        private CancellationTokenSource SQLCancelToken = new CancellationTokenSource();

        private TaskCompletionSource<bool>? SQLReadyEvent;



        private GameSQLPipeLine()
        {
            SQLWorker = new SQLExecuter(DBServerSettings.Default.SQLDataSoruce, DBServerSettings.Default.SQLGameDataBaseName,
                DBServerSettings.Default.SQLSecurity, DBServerSettings.Default.SQLPoolMinSize, DBServerSettings.Default.SQLPoolMaxSize, DBServerSettings.Default.SQLTimeOut);
            StartSQLProcess();
        }

        public async Task ConnectToSQL(TaskCompletionSource<bool> SQLEvent)
        {
            SQLReadyEvent = SQLEvent;
            await SQLWorker.TryConnect().ConfigureAwait(false);
            if (SQLReadyEvent != null)
            {
                SQLReadyEvent.TrySetResult(true);
                UIEvent.GetSingletone.UpdateDBServerStatus(true);
                SQLReadyEvent = null;
            }
        }

        public async Task StopSQL()
        {
            SQLCancelToken.Cancel();
            SQLChannel.Writer.Complete();
            await SQLWorker.Cancel().ConfigureAwait(false);
        }

        private void StartSQLProcess()
        {
            Task.Run(SQLProcess);
        }

        private async Task SQLProcess()
        {
            while (!SQLCancelToken.IsCancellationRequested)
            {
                try
                {
                    await SQLChannel.Reader.WaitToReadAsync(SQLCancelToken.Token).ConfigureAwait(false);
                    while (SQLChannel.Reader.TryRead(out var item))
                    {
                        switch (item.ID)
                        {
                            case DB_SP.SP_TEST:
                                // 여기 이제 리턴값 받고 Send 시키는거 작업해야함
                                int ReturnValue  = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_TEST.ToString(), item.parameters).ConfigureAwait(false);
                                LogManager.GetSingletone.WriteLog("AccountID : DB서버 입니다. 답장갔나요? NickName : 오른쪽에  있는 값들은 SQL쿼리 응답입니다.");
                                ResponseDBTestPacket Packet = new ResponseDBTestPacket("DB서버 입니다. 답장갔나요?","오른쪽에 있는 값들은 SQL쿼리 응답입니다.",ReturnValue,12321421);
                                GameServerSendPacketPipeline.GetSingletone.PushToPacketPipeline(DBPacketListID.RESPONSE_DB_TEST, Packet);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog("SQL 작업이 중단되었습니다.");
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e);
                }
            }
        }

        public void SQL_DB_TEST(string AccountID, string NickName)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@NickName", SqlDbType.VarChar, 50) { Value = NickName },
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_TEST, parameters));
        }
    }
}
