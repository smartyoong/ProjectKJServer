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
using CoreUtility.GlobalVariable;
using CoreUtility.SQLCore;
using CoreUtility.Utility;
using LoginServer.SocketConnect;
using LoginServer.Packet_SPList;
using static System.Net.Mime.MediaTypeNames;
using LoginServer.MainUI;

namespace LoginServer.PacketPipeLine
{
    public class AccountSQLManager
    {
        private SQLExecuter SQLWorker;

        private TaskCompletionSource<bool>? SQLReadyEvent;

        //private static readonly Lazy<AccountSQLManager> instance = new Lazy<AccountSQLManager>(() => new AccountSQLManager());
        //public static AccountSQLManager GetSingletone => instance.Value;

        private Channel<(LOGIN_SP ID, SqlParameter[] parameters, int ClientID)> SQLChannel = Channel.CreateUnbounded<(LOGIN_SP ID, SqlParameter[], int)>();

        private CancellationTokenSource SQLCancelToken = new CancellationTokenSource();

        private Dictionary<Type, Action<ISQLPacket>> ParameterLookUpTable;
        private Dictionary<LOGIN_SP, Func<SqlParameter[], int, Task>> SQLLookUpTable;


        public AccountSQLManager()
        {
            ParameterLookUpTable = new Dictionary<Type, Action<ISQLPacket>>()
            {
                { typeof(SQLLoginRequest), SQL_LOGIN_REQUEST },
                { typeof(SQLIDUniqueCheckRequest), SQL_ID_UNIQUE_CHECK_REQUEST },
                { typeof(SQLRegistAccountRequest), SQL_REGIST_ACCOUNT_REQUEST },
                { typeof(SQLCreateNickNameRequest), SQL_CREATE_NICKNAME_REQUEST }
            };

            SQLLookUpTable = new Dictionary<LOGIN_SP, Func<SqlParameter[], int, Task>>()
            {
                { LOGIN_SP.SP_LOGIN, SQL_RESULT_LOGIN_RESPONSE },
                { LOGIN_SP.SP_ID_UNIQUE_CHECK, SQL_RESULT_ID_UNIQUE_CHECK_RESPONSE },
                { LOGIN_SP.SP_REGIST_ACCOUNT, SQL_RESULT_REGIST_ACCOUNT_RESPONSE },
                { LOGIN_SP.SP_CREATE_NICKNAME, CALL_SQL_CREATE_NICKNAME_RESPONSE }
            };

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

        public void HandleSQLPacket(ISQLPacket Packet)
        {
            if (ParameterLookUpTable.TryGetValue(Packet.GetType(), out var ParamFunc))
            {
                ParamFunc(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"SQL 처리할 수 없는 패킷이 들어왔습니다. {Packet}");
            }
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
                        if (SQLLookUpTable.TryGetValue(item.ID, out var SQLFunc))
                        {
                            await SQLFunc(item.parameters, item.ClientID).ConfigureAwait(false);
                        }
                        else
                        {
                            LogManager.GetSingletone.WriteLog($"SQL 처리할 수 없는 패킷이 들어왔습니다. {item.ID}");
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

        private void SQL_LOGIN_REQUEST(ISQLPacket Packet)
        {
            if(Packet is not SQLLoginRequest ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_LOGIN_REQUEST에서 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] Params =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@PW", SqlDbType.VarChar, 50) { Value = ValidPacket.Password },
                new SqlParameter("@NickName", SqlDbType.NVarChar, 16) { Direction = ParameterDirection.Output }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_LOGIN, Params, ValidPacket.ClientID));
        }
        private void SQL_ID_UNIQUE_CHECK_REQUEST(ISQLPacket Packet)
        {
            if (Packet is not SQLIDUniqueCheckRequest ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_ID_UNIQUE_CHECK_REQUEST에서 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] Params =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_ID_UNIQUE_CHECK, Params, ValidPacket.ClientID));
        }

        private void SQL_REGIST_ACCOUNT_REQUEST(ISQLPacket Packet)
        {
            if (Packet is not SQLRegistAccountRequest ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_REGIST_ACCOUNT_REQUEST에서 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] Params =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@PW", SqlDbType.VarChar, 50) { Value = ValidPacket.Password },
                new SqlParameter("@IP", SqlDbType.VarChar, 50) { Value = ValidPacket.IPAddr }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_REGIST_ACCOUNT, Params, ValidPacket.ClientID));
        }

        private void SQL_CREATE_NICKNAME_REQUEST(ISQLPacket Packet)
        {
            if (Packet is not SQLCreateNickNameRequest ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_CREATE_NICKNAME_REQUEST에서 다른 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] Params =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@NickName", SqlDbType.NVarChar, 16) { Value = ValidPacket.NickName }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_CREATE_NICKNAME, Params, ValidPacket.ClientID));
        }


        private async Task SQL_RESULT_LOGIN_RESPONSE(SqlParameter[] Parameters, int ClientID)
        {
            const int LOGIN_SUCCESS = 2;
            const int HASH_CODE_CREATE_FAIL = 3;
            (int ReturnValue, dynamic NickName) Item = await SQLWorker.ExecuteSqlSPWithOneOutPutParamAsync(LOGIN_SP.SP_LOGIN.ToString(), Parameters).ConfigureAwait(false);

            string HashCode = string.Empty;

            if (Item.ReturnValue == LOGIN_SUCCESS)
                HashCode = MainProxy.GetSingletone.MakeAuthHashCode((string)Parameters[0].Value/*AccountID*/, ClientID);
            if (string.IsNullOrEmpty(HashCode) && Item.ReturnValue == LOGIN_SUCCESS)
            {
                Item.ReturnValue = HASH_CODE_CREATE_FAIL;
                HashCode = "NONEHASH";
                LogManager.GetSingletone.WriteLog($"해시 코드 생성에 실패했습니다. {Item.NickName} {ClientID}");
            }
            else
            {
                // 해시 코드 생성에 성공했다면, 게임 서버한테 전달한다.
                MainProxy.GetSingletone.ProcessSendPacketToGameServer(LoginGamePacketListID.SEND_USER_HASH_INFO,
                    new SendUserHashInfoPacket((string)Parameters[0].Value/*AccountID*/, HashCode, ClientID, MainProxy.GetSingletone.GetIPAddrByClientID(ClientID)));
                // 소켓과 클라이언트 ID를 매핑한다 추후 편하게 사용하기 위함.
                MainProxy.GetSingletone.MappingSocketAccountID(MainProxy.GetSingletone.GetClientSocket(ClientID)!, (string)Parameters[0].Value/*AccountID*/);
            }

            // 클라이언트한테는 어떻게든 전달한다
            LoginResponsePacket Packet = new LoginResponsePacket(Item.NickName, HashCode, Item.ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.LOGIN_RESPONESE, Packet, ClientID);
        }

        private async Task SQL_RESULT_ID_UNIQUE_CHECK_RESPONSE(SqlParameter[] Parameters, int ClientID)
        {
            int ReturnValue = (int)GeneralErrorCode.ERR_SQL_RETURN_ERROR;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_ID_UNIQUE_CHECK.ToString(), Parameters).ConfigureAwait(false);

            bool IsUnique = ReturnValue == 0 ? true : false;
            IDUniqueCheckResponsePacket IDUniquePacket = new IDUniqueCheckResponsePacket(IsUnique);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.ID_UNIQUE_CHECK_RESPONESE, IDUniquePacket, ClientID);
        }

        private async Task SQL_RESULT_REGIST_ACCOUNT_RESPONSE(SqlParameter[] Parameters, int ClientID)
        {
            int ReturnValue = (int)GeneralErrorCode.ERR_SQL_RETURN_ERROR;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_REGIST_ACCOUNT.ToString(), Parameters).ConfigureAwait(false);

            RegistAccountResponsePacket RegistPacket = new RegistAccountResponsePacket(ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.REGIST_ACCOUNT_RESPONESE, RegistPacket, ClientID);
        }

        private async Task CALL_SQL_CREATE_NICKNAME_RESPONSE(SqlParameter[] Parameters, int ClientID)
        {
            //const int NO_ACCOUNT_ERROR = -1;
            //const int CREATE_FAIL = -2;
            int ReturnValue = (int)GeneralErrorCode.ERR_SQL_RETURN_ERROR;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_CREATE_NICKNAME.ToString(), Parameters);
            CreateNickNameResponsePacket Packet = new CreateNickNameResponsePacket((string)Parameters[1].Value,ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.CREATE_NICKNAME_RESPONESE, Packet, ClientID);
        }

    }
}
