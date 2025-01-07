using CoreUtility.GlobalVariable;
using CoreUtility.SQLCore;
using CoreUtility.Utility;
using DBServer.MainUI;
using DBServer.Packet_SPList;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Channels;

namespace DBServer.PacketPipeLine
{
    public class GameSQLPipeLine
    {
        private SQLExecuter SQLWorker;

        //private static readonly Lazy<GameSQLPipeLine> instance = new Lazy<GameSQLPipeLine>(() => new GameSQLPipeLine());
        //public static GameSQLPipeLine GetSingletone => instance.Value;

        private Channel<(DB_SP ID, SqlParameter[] parameters)> SQLChannel = Channel.CreateUnbounded<(DB_SP ID, SqlParameter[])>();

        private CancellationTokenSource SQLCancelToken = new CancellationTokenSource();

        private TaskCompletionSource<bool>? SQLReadyEvent;



        public GameSQLPipeLine()
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

        public void DispatchSQLPacket(IGameSQLPacket Packet)
        {
            switch (Packet)
            {
                case GameSQLReadCharacterPacket ReadCharacterPacket:
                    SQL_READ_CHARACTER(ReadCharacterPacket.AccountID, ReadCharacterPacket.NickName);
                    break;
                case GameSQLCreateCharacterPacket CreateCharacterPacket:
                    SQL_CREATE_CHARACTER(CreateCharacterPacket.AccountID, CreateCharacterPacket.Gender, CreateCharacterPacket.PresetID);
                    break;
                case GameSQLUpdateHealthPoint UpdateHealthPacket:
                    SQL_UPDATE_HP(UpdateHealthPacket.AccountID, UpdateHealthPacket.CurrentHP);
                    break;
                case GameSQLUpdateMagicPoint UpdateMagicPacket:
                    SQL_UPDATE_MP(UpdateMagicPacket.AccountID, UpdateMagicPacket.CurrentMP);
                    break;
                case GameSQLUpdateLevelEXP UpdateLevelEXPPacket:
                    SQL_UPDATE_LEVEL_EXP(UpdateLevelEXPPacket.AccountID, UpdateLevelEXPPacket.Level, UpdateLevelEXPPacket.CurrentEXP);
                    break;
                case GameSQLUpdateJobLevel UpdateJobLevelPacket:
                    SQL_UPDATE_JOB_LEVEL(UpdateJobLevelPacket.AccountID, UpdateJobLevelPacket.Level);
                    break;
                case GameSQLUpdateJob UpdateJobPacket:
                    SQL_UPDATE_JOB(UpdateJobPacket.AccountID, UpdateJobPacket.Job);
                    break;
                default:
                    LogManager.GetSingletone.WriteLog($"Unknown SQL Packet Type : {Packet.GetType().Name}");
                    break;
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
                        switch (item.ID)
                        {
                            case DB_SP.SP_READ_CHARACTER:
                                await CALL_SQL_READ_CHARACTER(item.parameters);
                                break;
                            case DB_SP.SP_CREATE_CHARACTER:
                                await CALL_SQL_CREATE_CHARACTER(item.parameters);
                                break;
                            case DB_SP.SP_UPDATE_HP:
                                await CALL_SQL_UPDATE_HP(item.parameters);
                                break;
                            case DB_SP.SP_UPDATE_MP:
                                await CALL_SQL_UPDATE_MP(item.parameters);
                                break;
                            case DB_SP.SP_UPDATE_LEVEL_EXP:
                                await CALL_SQL_UPDATE_LEVEL_EXP(item.parameters);
                                break;
                            case DB_SP.SP_UPDATE_JOB_LEVEL:
                                await CALL_SQL_UPDATE_JOB_LVEL(item.parameters);
                                break;
                            case DB_SP.SP_UPDATE_JOB:
                                await CALL_SQL_UPDATE_JOB(item.parameters);
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
        public void SQL_READ_CHARACTER(string AccountID, string NickName)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@NickName", SqlDbType.VarChar, 50) { Value = NickName }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_READ_CHARACTER, sqlParameters));
        }
        public void SQL_CREATE_CHARACTER(string AccountID, int Gender, int PrestID)
        {
            const int NO_JOB = 0;
            //닉네임은 따로 가야하는데
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@Job",SqlDbType.Int) {Value = NO_JOB },
                new SqlParameter("@Gender", SqlDbType.Int) { Value = Gender },
                new SqlParameter("@Prest_ID", SqlDbType.Int) { Value = PrestID }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_CREATE_CHARACTER, sqlParameters));
        }

        public void SQL_UPDATE_HP(string AccountID, int CurrentHP)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@HP", SqlDbType.Int) { Value = CurrentHP },
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_HP, sqlParameters));
        }

        public void SQL_UPDATE_MP(string AccountID, int CurrentMP)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@MP", SqlDbType.Int) { Value = CurrentMP }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_MP, sqlParameters));
        }

        public void SQL_UPDATE_LEVEL_EXP(string AccountID, int Level, int CurrentEXP)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@LEVEL", SqlDbType.Int) { Value = Level },
                new SqlParameter("@EXP", SqlDbType.Int) { Value = CurrentEXP }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_LEVEL_EXP, sqlParameters));
        }

        public void SQL_UPDATE_JOB_LEVEL(string AccountID, int Level)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@JOB_LEVEL", SqlDbType.Int) { Value = Level }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_JOB_LEVEL, sqlParameters));
        }

        public void SQL_UPDATE_JOB(string AccountID, int Job)
        {
            SqlParameter[] sqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = AccountID },
                new SqlParameter("@JOB", SqlDbType.Int) { Value = Job }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_JOB, sqlParameters));
        }

        public async Task CALL_SQL_READ_CHARACTER(SqlParameter[] Parameters)
        {
            const int NEED_TO_MAKE_CHARACTER = -1;
            int ReturnValue = 99999;
            //닉네임을 어떻게든 전달하기 위한 꼼수임 닉네임은 1번 파라미터이고 사용 안함
            SqlParameter[] NewParameters = new SqlParameter[Parameters.Length - 1];
            Array.Copy(Parameters, NewParameters, Parameters.Length - 1);

            List<List<object>> CharacterInfoList;
            (ReturnValue, CharacterInfoList) = await SQLWorker.ExecuteSqlSPGetResultListAsync(DB_SP.SP_READ_CHARACTER.ToString(), NewParameters).ConfigureAwait(false);
            if (ReturnValue == NEED_TO_MAKE_CHARACTER)
            {
                // 0번 파라미터가 AccountID임
                ResponseDBNeedToMakeCharacterPacket MakeCharacterPacket = new ResponseDBNeedToMakeCharacterPacket((string)Parameters[0].Value);
                MainProxy.GetSingletone.SendToGameServer(DBPacketListID.RESPONSE_NEED_TO_MAKE_CHARACTER, MakeCharacterPacket);
            }
            else
            {
                ResponseDBCharBaseInfoPacket CharBaseInfoPacket = new ResponseDBCharBaseInfoPacket((string)CharacterInfoList[0][0],
                    (int)CharacterInfoList[0][1], (int)CharacterInfoList[0][2], (int)CharacterInfoList[0][3], (int)CharacterInfoList[0][4],
                    (int)CharacterInfoList[0][5], (int)CharacterInfoList[0][6], (int)CharacterInfoList[0][7], (int)CharacterInfoList[0][8], (int)CharacterInfoList[0][9],
                    (string)Parameters[1].Value, (int)CharacterInfoList[0][10], (int)CharacterInfoList[0][11]);
                MainProxy.GetSingletone.SendToGameServer(DBPacketListID.RESPONSE_CHAR_BASE_INFO, CharBaseInfoPacket);
            }
        }

        public async Task CALL_SQL_CREATE_CHARACTER(SqlParameter[] Parameters)
        {
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_CREATE_CHARACTER.ToString(), Parameters).ConfigureAwait(false);
            //이제 다시 캐릭 생성 여부를 응답하자 일단 닉네임 만들도록 생성 요청부터
            // 캐릭 생성중 에러 발생 에러 패킷 전송(Error는 -1)
            ResponseDBCreateCharacterPacket Packet = new ResponseDBCreateCharacterPacket((string)Parameters[0].Value, ReturnValue);
            MainProxy.GetSingletone.SendToGameServer(DBPacketListID.RESPONSE_CREATE_CHARACTER, Packet);
        }

        public async Task CALL_SQL_UPDATE_HP(SqlParameter[] Parameters)
        {
            const int SUCCESS = 0;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_HP.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"HP, MP 업데이트 실패 {(string)Parameters[0].Value}");
            }
        }

        public async Task CALL_SQL_UPDATE_MP(SqlParameter[] Parameters)
        {
            const int SUCCESS = 0;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_MP.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"MP 업데이트 실패 {(string)Parameters[0].Value}");
            }
        }

        public async Task CALL_SQL_UPDATE_LEVEL_EXP(SqlParameter[] Parameters)
        {
            const int SUCCESS = 0;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_LEVEL_EXP.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"Level, EXP 업데이트 실패 {(string)Parameters[0].Value}");
            }
        }

        public async Task CALL_SQL_UPDATE_JOB_LVEL(SqlParameter[] Parameters)
        {
            const int SUCCESS = 0;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_JOB_LEVEL.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"직업 레벨 업데이트 실패 {(string)Parameters[0].Value}");
            }
        }

        public async Task CALL_SQL_UPDATE_JOB(SqlParameter[] Parameters)
        {
            const int SUCCESS = 0;
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_JOB.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"직업 업데이트 실패 {(string)Parameters[0].Value}");
            }
        }
    }
}
