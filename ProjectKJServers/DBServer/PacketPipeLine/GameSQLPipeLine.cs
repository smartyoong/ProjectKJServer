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

    public enum COMMON_SQL_ERROR
    {
        SUCCESS = 0,
        NOT_FOUND_USER = 999
    }

    public class GameSQLPipeLine
    {
        private SQLExecuter SQLWorker;

        //private static readonly Lazy<GameSQLPipeLine> instance = new Lazy<GameSQLPipeLine>(() => new GameSQLPipeLine());
        //public static GameSQLPipeLine GetSingletone => instance.Value;

        private Channel<(DB_SP ID, SqlParameter[] parameters)> SQLChannel = Channel.CreateUnbounded<(DB_SP ID, SqlParameter[])>();

        private CancellationTokenSource SQLCancelToken = new CancellationTokenSource();

        private TaskCompletionSource<bool>? SQLReadyEvent;

        private Dictionary<DB_SP, Func<SqlParameter[], Task>> SQLLookUpTable;

        private Dictionary<Type, Action<IGameSQLPacket>> ParameterLookUpTable;


        public GameSQLPipeLine()
        {
            ParameterLookUpTable = new Dictionary<Type, Action<IGameSQLPacket>>()
                {
                { typeof(GameSQLReadCharacterPacket), SQL_READ_CHARACTER },
                { typeof(GameSQLCreateCharacterPacket), SQL_CREATE_CHARACTER },
                { typeof(GameSQLUpdateHealthPoint), SQL_UPDATE_HP },
                { typeof(GameSQLUpdateMagicPoint), SQL_UPDATE_MP },
                { typeof(GameSQLUpdateLevelEXP), SQL_UPDATE_LEVEL_EXP },
                { typeof(GameSQLUpdateJobLevel), SQL_UPDATE_JOB_LEVEL },
                { typeof(GameSQLUpdateJob), SQL_UPDATE_JOB },
                { typeof(GameSQLUpdateGender), SQL_UPDATE_GENDER },
                { typeof(GameSQLUpdatePreset), SQL_UPDATE_PRESET }
            };
            SQLLookUpTable = new Dictionary<DB_SP, Func<SqlParameter[], Task>>()
            {
                { DB_SP.SP_READ_CHARACTER, CALL_SQL_READ_CHARACTER },
                { DB_SP.SP_CREATE_CHARACTER, CALL_SQL_CREATE_CHARACTER },
                { DB_SP.SP_UPDATE_HP, CALL_SQL_UPDATE_HP },
                { DB_SP.SP_UPDATE_MP, CALL_SQL_UPDATE_MP },
                { DB_SP.SP_UPDATE_LEVEL_EXP, CALL_SQL_UPDATE_LEVEL_EXP },
                { DB_SP.SP_UPDATE_JOB_LEVEL, CALL_SQL_UPDATE_JOB_LVEL },
                { DB_SP.SP_UPDATE_JOB, CALL_SQL_UPDATE_JOB },
                { DB_SP.SP_UPDATE_GENDER, CALL_SQL_UPDATE_GENDER },
                { DB_SP.SP_UPDATE_PRESET, CALL_SQL_UPDATE_PRESET }
            };
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
            if(ParameterLookUpTable.TryGetValue(Packet.GetType(), out var SQLParameterFunc))
            {
                SQLParameterFunc(Packet);
            }
            else
            {
                LogManager.GetSingletone.WriteLog($"SQLPipeLine에 일치하는 타입의 패킷이 없습니다. {Packet}");
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
                            await SQLFunc(item.parameters).ConfigureAwait(false);
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
        public void SQL_READ_CHARACTER(IGameSQLPacket Packet)
        {
            if(Packet is not GameSQLReadCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_READ_CHARACTER에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@NickName", SqlDbType.VarChar, 50) { Value = ValidPacket.NickName }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_READ_CHARACTER, SqlParameters));
        }
        public void SQL_CREATE_CHARACTER(IGameSQLPacket Packet)
        {
            if(Packet is not GameSQLCreateCharacterPacket ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_CREATE_CHARACTER에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            const int NO_JOB = 0;
            //닉네임은 따로 가야하는데
            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@Job",SqlDbType.Int) {Value = NO_JOB },
                new SqlParameter("@Gender", SqlDbType.Int) { Value = ValidPacket.Gender },
                new SqlParameter("@Prest_ID", SqlDbType.Int) { Value = ValidPacket.PresetID }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_CREATE_CHARACTER, SqlParameters));
        }

        public void SQL_UPDATE_HP(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdateHealthPoint ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_HP에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@HP", SqlDbType.Int) { Value = ValidPacket.CurrentHP },
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_HP, SqlParameters));
        }

        public void SQL_UPDATE_MP(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdateMagicPoint ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_MP에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@MP", SqlDbType.Int) { Value = ValidPacket.CurrentMP }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_MP, SqlParameters));
        }

        public void SQL_UPDATE_LEVEL_EXP(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdateLevelEXP ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_LEVEL_EXP에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }
            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@LEVEL", SqlDbType.Int) { Value = ValidPacket.Level },
                new SqlParameter("@EXP", SqlDbType.Int) { Value = ValidPacket.CurrentEXP }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_LEVEL_EXP, SqlParameters));
        }

        public void SQL_UPDATE_JOB_LEVEL(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdateJobLevel ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_JOB_LEVEL에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@JOB_LEVEL", SqlDbType.Int) { Value = ValidPacket.Level }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_JOB_LEVEL, SqlParameters));
        }

        public void SQL_UPDATE_JOB(IGameSQLPacket Packet)
        {
            if(Packet is not GameSQLUpdateJob ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_JOB에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@JOB", SqlDbType.Int) { Value = ValidPacket.Job }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_JOB, SqlParameters));
        }

        public void SQL_UPDATE_GENDER(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdateGender ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_GENDER에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@Gender", SqlDbType.Int) { Value = ValidPacket.Gender }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_GENDER, SqlParameters));
        }

        public void SQL_UPDATE_PRESET(IGameSQLPacket Packet)
        {
            if (Packet is not GameSQLUpdatePreset ValidPacket)
            {
                LogManager.GetSingletone.WriteLog($"SQL_UPDATE_PRESET에 일치하지 않는 타입의 패킷이 들어왔습니다. {Packet}");
                return;
            }

            SqlParameter[] SqlParameters =
            [
                new SqlParameter("@ID", SqlDbType.VarChar, 50) { Value = ValidPacket.AccountID },
                new SqlParameter("@PRESET_NUMBER", SqlDbType.Int) { Value = ValidPacket.PresetNumber }
            ];
            SQLChannel.Writer.TryWrite((DB_SP.SP_UPDATE_PRESET, SqlParameters));
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
                //CharacterInfoList[0][0] = AccountID였으나, int로 저장으로 바꿈에 따라 Parameter에서 주는 AccountID를 그대로 사용
                ResponseDBCharBaseInfoPacket CharBaseInfoPacket = new ResponseDBCharBaseInfoPacket((string)Parameters[0].Value,
                    (int)CharacterInfoList[0][1], (int)CharacterInfoList[0][2], (int)CharacterInfoList[0][3], (int)CharacterInfoList[0][4],
                    (int)CharacterInfoList[0][5], (int)CharacterInfoList[0][6], (int)CharacterInfoList[0][7], (int)CharacterInfoList[0][8], (int)CharacterInfoList[0][9],
                    (string)Parameters[1].Value, (int)CharacterInfoList[0][10], (int)CharacterInfoList[0][11], (bool)CharacterInfoList[0][12]);
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

        public async Task CALL_SQL_UPDATE_GENDER(SqlParameter[] Parameters)
        {
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_GENDER.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != (int)COMMON_SQL_ERROR.SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"성별 업데이트 실패 {(string)Parameters[0].Value} ErrorCode : {ReturnValue}");
            }
            ResponseDBUpdateGenderPacket Packet = new ResponseDBUpdateGenderPacket((string)Parameters[0].Value, ReturnValue);
            MainProxy.GetSingletone.SendToGameServer(DBPacketListID.RESPONSE_UPDATE_GENDER, Packet);
        }

        public async Task CALL_SQL_UPDATE_PRESET(SqlParameter[] Parameters)
        {
            int ReturnValue = 99999;
            ReturnValue = await SQLWorker.ExecuteSqlSPAsync(DB_SP.SP_UPDATE_PRESET.ToString(), Parameters).ConfigureAwait(false);
            if (ReturnValue != (int)COMMON_SQL_ERROR.SUCCESS)
            {
                LogManager.GetSingletone.WriteLog($@"프리셋 업데이트 실패 {(string)Parameters[0].Value} ErrorCode : {ReturnValue}");
            }
            ResponseDBUpdatePresetPacket Packet = new ResponseDBUpdatePresetPacket((string)Parameters[0].Value, ReturnValue, (int)Parameters[1].Value); // 1번 파라미터가 프리셋 번호
            MainProxy.GetSingletone.SendToGameServer(DBPacketListID.RESPONSE_UPDATE_PRESET, Packet);
        }
    }
}
