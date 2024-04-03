using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBServer
{
    internal class SQLExecuter : IDisposable
    {
        private readonly string ConnectString;
        private CancellationTokenSource CancelSQL = new CancellationTokenSource();
        private ConcurrentStack<Task> TaskStatcks = new ConcurrentStack<Task>();
        private bool IsAlreadyDisposed = false;

        SQLExecuter(string DBSource, string DBName, bool UseSecurity, int MinPoolSize = 2, int MaxPoolSize = 100)
        {
            ConnectString = $@"Data Source={DBSource};Initial Catalog={DBName};Integrated Security={UseSecurity};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize}";
        }

        public async Task<bool> ConnectCheckAsync()
        {
            try
            {
                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
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
            catch (Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message).ConfigureAwait(false);
                return (int)SP_ERROR.CONNECTION_ERROR;
            }
        }

        public async Task<(int ErrorCode ,List<List<object>> ValueList)> ExecuteSqlSPGetListResultAsync(string SPName ,params SqlParameter[] SQLParameters)
        {
            List<List<object>> ResultList = new List<List<object>>();
            try
            {

                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token).ConfigureAwait(false);
                    using (SqlCommand SQLCommand = new SqlCommand(SPName, Connection))
                    {
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
            catch(Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message).ConfigureAwait(false);
                return ((int)SP_ERROR.CONNECTION_ERROR,ResultList);
            }
        }

        public async Task Cancel()
        {
            CancelSQL.Cancel();
            await Task.Delay(3000).ConfigureAwait(false);
            CleanUp();
            Dispose();
        }

        // 여기서 실행중인 Task는 대기하고 끝난 Task는 Dispose할 수 있도록 정리하자 컨테이너를 정리하자
        private void CleanUp()
        {

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

            TaskStatcks.ToList().ForEach(x => x.Dispose());
        }

        ~SQLExecuter()
        {
            Dispose(false);
        }

    }
}
