using GameServer.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Mehtod
{
    class GMCommand
    {
        public static void CommandChangeGender(PlayerCharacter Who)
        {
            Who.GetAppearanceComponent.RequestChangeGender();
        }

        public static void CommandChangeJob(PlayerCharacter Who, int JobCode)
        {
            Who.GetJobComponent.JobChange(JobCode);
        }

        public static void CommandChangePreset(PlayerCharacter Who, int PresetNumber)
        {
            Who.GetAppearanceComponent.RequestChangePresetNumber(PresetNumber);
        }
    }
}
