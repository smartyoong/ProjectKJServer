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
    internal class PlayerCharacter
    {
        int ClientID = 0;
        int CurrentMapID = 0;
        string Name = string.Empty;
        Vector3 Position = Vector3.Zero;
        UniformVelocityMovementComponent MovementComponent = new UniformVelocityMovementComponent();
        public PlayerCharacter()
        {
            MainProxy.GetSingletone.AddUniformVelocityMovementComponent(MovementComponent);
        }
    }
}
