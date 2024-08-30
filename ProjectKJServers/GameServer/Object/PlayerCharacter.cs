using GameServer.Component;
using GameServer.MainUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Object
{
    struct CharacterAccountInfo
    {
        public string AccountID { get; set; }
        public string NickName { get; set; }
    }
    struct CharacterPosition
    {
        public int MapID { get; set; }
        public Vector3 Position { get; set; }
    }
    struct CharacterJobInfo
    {
        public int Job { get; set; }
        public int Level { get; set; }
    }
    struct ChracterAppearanceInfo
    {
        public int Gender { get; set; }
        public int PresetNumber { get; set; }
    }
    struct CharacterLevelInfo
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
    }
    internal class PlayerCharacter
    {
        public CharacterAccountInfo AccountInfo;
        public CharacterPosition CurrentPosition;
        public CharacterJobInfo JobInfo;
        public ChracterAppearanceInfo AppearanceInfo;
        public CharacterLevelInfo LevelInfo;
        private UniformVelocityMovementComponent MovementComponent;
        public PlayerCharacter()
        {
            AccountInfo = new CharacterAccountInfo();
            CurrentPosition = new CharacterPosition();
            JobInfo = new CharacterJobInfo();
            AppearanceInfo = new ChracterAppearanceInfo();
            LevelInfo = new CharacterLevelInfo();
            //MovementComponent = new UniformVelocityMovementComponent();
        }

        public void SetMovement(int Speed, Vector3 Position)
        {
            MainProxy.GetSingletone.AddUniformVelocityMovementComponent(MovementComponent);
            MovementComponent.InitMovementComponent(Speed, Position);
        }
    }
}
