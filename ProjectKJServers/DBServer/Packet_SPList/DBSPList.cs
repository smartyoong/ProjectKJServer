﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer.Packet_SPList
{
    enum DB_SP
    {
        SP_INVALID = 0,
        SP_TEST = 1,
        SP_READ_CHARACTER = 2,
        SP_CREATE_CHARACTER = 3,
        SP_UPDATE_HP = 4,
        SP_UPDATE_MP = 5,
        SP_UPDATE_LEVEL_EXP = 6,
        SP_UPDATE_JOB_LEVEL = 7,
        SP_UPDATE_JOB = 8,
        SP_UPDATE_GENDER = 9,
        SP_UPDATE_PRESET = 10
    }

    public interface IGameSQLPacket
    {
        public string AccountID { get; set; }
    }

    public record GameSQLReadCharacterPacket(string AccountID, string NickName) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    public record GameSQLCreateCharacterPacket(string AccountID, int Gender, int PresetID) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Gender { get; set; } = Gender;
        public int PresetID { get; set; } = PresetID;
    }

    public record GameSQLUpdateHealthPoint(string AccountID, int CurrentHP) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int CurrentHP { get; set; } = CurrentHP;
    }

    public record GameSQLUpdateMagicPoint(string AccountID, int CurrentMP) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int CurrentMP { get; set; } = CurrentMP;
    }

    public record GameSQLUpdateLevelEXP(string AccountID, int Level, int CurrentEXP) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Level { get; set; } = Level;
        public int CurrentEXP { get; set; } = CurrentEXP;
    }

    public record GameSQLUpdateJobLevel(string AccountID, int Level) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Level { get; set; } = Level;
    }

    public record GameSQLUpdateJob(string AccountID, int Job) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Job { get; set; } = Job;
    }

    public record GameSQLUpdateGender(string AccountID, int Gender) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Gender { get; set; } = Gender;
    }

    public record GameSQLUpdatePreset(string AccountID, int PresetNumber) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int PresetNumber { get; set; } = PresetNumber;
    }
}
