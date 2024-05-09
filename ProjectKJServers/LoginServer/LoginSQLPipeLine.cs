using KYCSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoginServer.Properties;
using System.Data;
using System.Threading.Channels;
using System.Data.SqlClient;
using System.Net.Sockets;
using KYCLog;

namespace LoginServer
{
    public class AccountSQLManager
    {
        private SQLExecuter SQLWorker;

        private TaskCompletionSource<bool>? SQLReadyEvent;

        private static readonly Lazy<AccountSQLManager> instance = new Lazy<AccountSQLManager>(() => new AccountSQLManager());
        public static AccountSQLManager GetSingletone => instance.Value;

        private Channel<(LOGIN_SP ID, SqlParameter[] parameters, int ClientID)> SQLChannel = Channel.CreateUnbounded<(LOGIN_SP ID, SqlParameter[], int)>();

        private CancellationTokenSource SQLCancelToken = new CancellationTokenSource();
      

        private AccountSQLManager()
        {
            SQLWorker = new SQLExecuter(Settings.Default.SQLDataSoruce, Settings.Default.SQLAccountDataBaseName,
                Settings.Default.SQLSecurity, Settings.Default.SQLPoolMinSize, Settings.Default.SQLPoolMaxSize, Settings.Default.SQLTimeOut);
            StartSQLProcess();
        }

        public async Task ConnectToSQL(TaskCompletionSource<bool> SQLEvent)
        {
            SQLReadyEvent = SQLEvent;
            await SQLWorker.TryConnect().ConfigureAwait(false);
            if (SQLReadyEvent != null) 
            {
                SQLReadyEvent.TrySetResult(true);
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
                            case LOGIN_SP.SP_LOGIN:
                                // 여기 이제 리턴값 받고 Send 시키는거 작업해야함
                                (int ReturnValue, dynamic NickName) Item = await SQLWorker.ExecuteSqlSPWithOneOutPutParamAsync(LOGIN_SP.SP_LOGIN.ToString(), item.parameters).ConfigureAwait(false);
                                LoginResponsePacket Packet = new LoginResponsePacket(Item.NickName, Item.ReturnValue);
                                ClientSendPacketPipeline.GetSingletone.PushToPacketPipeline(LoginPacketListID.LOGIN_RESPONESE, Packet, item.ClientID);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    LogManager.GetSingletone.WriteLog("SQL 작업이 중단되었습니다.");
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e);
                }
            }
        }

        public void SQL_LOGIN_REQUEST(string AccountID, string AccountPW, int ClientID)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@PW", SqlDbType.VarChar, 50) { Value = AccountPW },
                new SqlParameter("@NickName", SqlDbType.NVarChar, 16) { Direction = ParameterDirection.Output }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_LOGIN, parameters, ClientID));
        }

    }
}
