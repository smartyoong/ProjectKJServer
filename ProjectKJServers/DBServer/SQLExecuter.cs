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
    internal class SQLExecuter
    {
        private readonly string ConnectString;
        private CancellationTokenSource CancelSQL = new CancellationTokenSource();
        private ConcurrentStack<Task> TaskStatcks = new ConcurrentStack<Task>();

        SQLExecuter(string DBSource, string DBName, bool UseSecurity, int MinPoolSize= 2, int MaxPoolSize = 100)
        {
            ConnectString = $@"Data Source={DBSource};Initial Catalog={DBName};Integrated Security={UseSecurity};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize}";
        }
        public async Task <bool> ConnectCheckAsync()
        {
            try
            {
                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token);
                    return true;
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message);
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
                    await Connection.OpenAsync(CancelSQL.Token);
                    using (SqlCommand SQLCommand = new SqlCommand(SPName, Connection))
                    {
                        SQLCommand.CommandType = CommandType.StoredProcedure;

                        SQLCommand.Parameters.AddRange(SqlParameters);

                        SqlParameter ReturnParameter = SQLCommand.Parameters.Add("@ReturnVal", SqlDbType.Int);
                        ReturnParameter.Direction = ParameterDirection.ReturnValue;


                        await SQLCommand.ExecuteNonQueryAsync(CancelSQL.Token);

                        // 반환 값을 얻습니다.
                        return (int)ReturnParameter.Value;
                    }
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message);
                return (int)SP_ERROR.CONNECTION_ERROR;
            }
        }

        public async Task<int> ExecuteSqlSPAsync(string SPName, List<List<object>>ResultList ,params SqlParameter[] SQLParameters)
        {
            try
            {
                using (SqlConnection Connection = new SqlConnection(ConnectString))
                {
                    await Connection.OpenAsync(CancelSQL.Token);
                    using (SqlCommand SQLCommand = new SqlCommand(SPName, Connection))
                    {
                        SQLCommand.CommandType = CommandType.StoredProcedure;

                        SQLCommand.Parameters.AddRange(SQLParameters);

                        SqlParameter ReturnParameter = SQLCommand.Parameters.Add("@ReturnVal", SqlDbType.Int);
                        ReturnParameter.Direction = ParameterDirection.ReturnValue;


                        using (SqlDataReader SQLReader = await SQLCommand.ExecuteReaderAsync(CancelSQL.Token))
                        {
                            do
                            {
                                while (await SQLReader.ReadAsync(CancelSQL.Token) && !CancelSQL.Token.IsCancellationRequested)
                                {
                                    List<object> Row = new List<object>();
                                    for (int i = 0; i < SQLReader.FieldCount; i++)
                                    {
                                        Row.Add(SQLReader.GetValue(i));
                                    }
                                    ResultList.Add(Row);
                                }
                            } while (await SQLReader.NextResultAsync(CancelSQL.Token) && !CancelSQL.Token.IsCancellationRequested);
                        }
                        return (int)ReturnParameter.Value;
                    }
                }
            }
            catch(Exception e) when (e is not OperationCanceledException)
            {
                await LogManager.GetSingletone.WriteLog(e.Message);
                return (int)SP_ERROR.CONNECTION_ERROR;
            }
        }

        public void Cancel()
        {
            CancelSQL.Cancel();
            
        }

    }
}
