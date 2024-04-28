using System.Data;
using KYCLog;
using KYCUIEventManager;
using System.Data.SqlClient;


namespace KYCSQL
{
    public class SQLExecuter : IDisposable
    {
        private readonly string ConnectString;
        private CancellationTokenSource CancelSQL = new CancellationTokenSource();
        private bool IsAlreadyDisposed = false;
        int SQLTimeout = 30;

        public SQLExecuter(string DBSource, string DBName, bool UseSecurity, int MinPoolSize = 2, int MaxPoolSize = 100, int TimeOut = 30)
        {
            ConnectString = $@"Data Source={DBSource};Initial Catalog={DBName};Integrated Security={UseSecurity};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};Connection Timeout={TimeOut}";
            SQLTimeout = TimeOut;
        }

        public async Task TryConnect()
        {
            while (!CancelSQL.Token.IsCancellationRequested)
            {
                await LogManager.GetSingletone.WriteLog("SQL 서버와 연결을 시도합니다.").ConfigureAwait(false);

                if (await ConnectCheckAsync().ConfigureAwait(false))
                {
                    break;
                }
                else
                {
                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> ConnectCheckAsync()
        {
            try
            {
                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token).ConfigureAwait(false);
                    UIEvent.GetSingletone.UpdateSQLStatus(true);
                    await LogManager.GetSingletone.WriteLog("SQL 서버와 연결되었습니다.").ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                UIEvent.GetSingletone.UpdateSQLStatus(false);
                await LogManager.GetSingletone.WriteLog(e.Message).ConfigureAwait(false);
                return false;
            }
        }

        // SP는 무조건 마지막 리턴값으로 에러코드를 전달해야한다.
        public async Task<int> ExecuteSqlSPAsync(string SPName, params SqlParameter[] SqlParameters)
        {
            try
            {
                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token).ConfigureAwait(false);
                    using (SqlCommand SQLCommand = new SqlCommand(SPName, Connection))
                    {
                        SQLCommand.CommandTimeout = SQLTimeout;
                        SQLCommand.CommandType = CommandType.StoredProcedure;

                        SQLCommand.Parameters.AddRange(SqlParameters);

                        SqlParameter ReturnParameter = SQLCommand.Parameters.Add("@ReturnVal", SqlDbType.Int);
                        ReturnParameter.Direction = ParameterDirection.ReturnValue;


                        await SQLCommand.ExecuteNonQueryAsync(CancelSQL.Token).ConfigureAwait(false);

                        // 반환 값을 얻습니다.
                        return (int)ReturnParameter.Value;
                    }
                }
            }
            catch (SqlException se) when (se.Number == -2)
            {
                UIEvent.GetSingletone.UpdateSQLStatus(false);
                await LogManager.GetSingletone.WriteLog("SQL 서버와 연결이 끊어졌습니다.").ConfigureAwait(false);
                return (int)SP_ERROR.CONNECTION_ERROR;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message).ConfigureAwait(false);
                return (int)SP_ERROR.SQL_QUERY_ERROR;
            }
        }

        public async Task<(int ErrorCode, List<List<object>> ValueList)> ExecuteSqlSPGeResulttListAsync(string SPName, params SqlParameter[] SQLParameters)
        {
            List<List<object>> ResultList = new List<List<object>>();
            try
            {

                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token).ConfigureAwait(false);
                    using (SqlCommand SQLCommand = new SqlCommand(SPName, Connection))
                    {
                        SQLCommand.CommandTimeout = SQLTimeout;
                        SQLCommand.CommandType = CommandType.StoredProcedure;

                        SQLCommand.Parameters.AddRange(SQLParameters);

                        SqlParameter ReturnParameter = SQLCommand.Parameters.Add("@ReturnVal", SqlDbType.Int);
                        ReturnParameter.Direction = ParameterDirection.ReturnValue;


                        using (SqlDataReader SQLReader = await SQLCommand.ExecuteReaderAsync(CancelSQL.Token).ConfigureAwait(false))
                        {
                            do
                            {
                                while (await SQLReader.ReadAsync(CancelSQL.Token).ConfigureAwait(false) && !CancelSQL.Token.IsCancellationRequested)
                                {
                                    List<object> Row = new List<object>();
                                    for (int i = 0; i < SQLReader.FieldCount; i++)
                                    {
                                        Row.Add(SQLReader.GetValue(i));
                                    }
                                    ResultList.Add(Row);
                                }
                            } while (await SQLReader.NextResultAsync(CancelSQL.Token).ConfigureAwait(false) && !CancelSQL.Token.IsCancellationRequested);
                        }
                        return ((int)ReturnParameter.Value, ResultList);
                    }
                }
            }
            catch (SqlException se) when (se.Number == -2)
            {
                UIEvent.GetSingletone.UpdateSQLStatus(false);
                await LogManager.GetSingletone.WriteLog("SQL 서버와 연결이 끊어졌습니다.").ConfigureAwait(false);
                return ((int)SP_ERROR.CONNECTION_ERROR, ResultList);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message).ConfigureAwait(false);
                return ((int)SP_ERROR.SQL_QUERY_ERROR, ResultList);
            }
        }

        public async Task Cancel()
        {
            CancelSQL.Cancel();
            UIEvent.GetSingletone.UpdateSQLStatus(false);
            await Task.Delay(3000).ConfigureAwait(false);
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsAlreadyDisposed)
            {
                return;
            }

            if (disposing)
            {
                CancelSQL.Dispose();
            }

        }

        ~SQLExecuter()
        {
            Dispose(false);
        }

    }
}
