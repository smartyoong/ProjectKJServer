﻿using System;
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


        public AccountSQLManager()
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
                        int ReturnValue = (int)GeneralErrorCode.ERR_SQL_RETURN_ERROR;
                        switch (item.ID)
                        {
                            case LOGIN_SP.SP_LOGIN:
                                // 여기 이제 리턴값 받고 Send 시키는거 작업해야함
                                (int ReturnValue, dynamic NickName) Item = await SQLWorker.ExecuteSqlSPWithOneOutPutParamAsync(LOGIN_SP.SP_LOGIN.ToString(), item.parameters).ConfigureAwait(false);
                                SQL_RESULT_LOGIN_RESPONSE(Item, (string)item.parameters[0].Value/*AccountID*/, item.ClientID);
                                break;
                            case LOGIN_SP.SP_ID_UNIQUE_CHECK:
                                ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_ID_UNIQUE_CHECK.ToString(), item.parameters).ConfigureAwait(false);
                                SQL_RESULT_ID_UNIQUE_CHECK_RESPONSE(ReturnValue, item.ClientID);
                                break;
                            case LOGIN_SP.SP_REGIST_ACCOUNT:
                                ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_REGIST_ACCOUNT.ToString(), item.parameters).ConfigureAwait(false);
                                SQL_RESULT_REGIST_ACCOUNT_RESPONSE(ReturnValue, item.ClientID);
                                break;
                            case LOGIN_SP.SP_CREATE_NICKNAME:
                                await CALL_SQL_CREATE_NICKNAME_RESPONSE(item.parameters, item.ClientID).ConfigureAwait(false);
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

        public void SQL_RESULT_LOGIN_RESPONSE((int ReturnValue, dynamic NickName) Item, string AccountID, int ClientID)
        {
            const int LOGIN_SUCCESS = 2;
            const int HASH_CODE_CREATE_FAIL = 3;

            string HashCode = string.Empty;

            if (Item.ReturnValue == LOGIN_SUCCESS)
                HashCode = MainProxy.GetSingletone.MakeAuthHashCode(AccountID, ClientID);
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
                    new SendUserHashInfoPacket(AccountID, HashCode, ClientID, MainProxy.GetSingletone.GetIPAddrByClientID(ClientID)));
                // 소켓과 클라이언트 ID를 매핑한다 추후 편하게 사용하기 위함.
                MainProxy.GetSingletone.MappingSocketAccountID(MainProxy.GetSingletone.GetClientSocket(ClientID)!, AccountID);
            }

            // 클라이언트한테는 어떻게든 전달한다
            LoginResponsePacket Packet = new LoginResponsePacket(Item.NickName, HashCode, Item.ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.LOGIN_RESPONESE, Packet, ClientID);
        }

        public void SQL_ID_UNIQUE_CHECK_REQUEST(string AccountID, int ClientID)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_ID_UNIQUE_CHECK, parameters, ClientID));
        }

        public void SQL_RESULT_ID_UNIQUE_CHECK_RESPONSE(int ReturnValue, int ClientID)
        {
            bool IsUnique = ReturnValue == 0 ? true : false;
            IDUniqueCheckResponsePacket IDUniquePacket = new IDUniqueCheckResponsePacket(IsUnique);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.ID_UNIQUE_CHECK_RESPONESE, IDUniquePacket, ClientID);
        }

        public void SQL_REGIST_ACCOUNT_REQUEST(string AccountID, string AccountPW, string IPAddr, int ClientID)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@PW", SqlDbType.VarChar, 50) { Value = AccountPW },
                new SqlParameter("@IP", SqlDbType.VarChar, 50) { Value = IPAddr }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_REGIST_ACCOUNT, parameters, ClientID));
        }

        public void SQL_RESULT_REGIST_ACCOUNT_RESPONSE(int ReturnValue, int ClientID)
        {
            RegistAccountResponsePacket RegistPacket = new RegistAccountResponsePacket(ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.REGIST_ACCOUNT_RESPONESE, RegistPacket, ClientID);
        }

        public void SQL_CREATE_NICKNAME_REQUEST(string AccountID, string NickName, int ClientID)
        {
            SqlParameter[] parameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@NickName", SqlDbType.NVarChar, 16) { Value = NickName }
            ];
            SQLChannel.Writer.TryWrite((LOGIN_SP.SP_CREATE_NICKNAME, parameters, ClientID));
        }

        public async Task CALL_SQL_CREATE_NICKNAME_RESPONSE(SqlParameter[] Parameters, int ClientID)
        {
            //const int NO_ACCOUNT_ERROR = -1;
            //const int CREATE_FAIL = -2;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(LOGIN_SP.SP_CREATE_NICKNAME.ToString(), Parameters);
            CreateNickNameResponsePacket Packet = new CreateNickNameResponsePacket((string)Parameters[1].Value,ReturnValue);
            MainProxy.GetSingletone.SendToClient(LoginPacketListID.CREATE_NICKNAME_RESPONESE, Packet, ClientID);
        }

    }
}
