using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Object
{
    public interface IGOAPAction
    {
        public float GetGoalChange(GOAPGoal goal);
    }
    public struct GOAPGoal
    {
        public string Name;
        public float Value;
    }

    public class TestGOAPAction : IGOAPAction
    {
        public float GetGoalChange(GOAPGoal Goal)
        {
            switch(Goal.Name)
            {
                case "Test":
                    return -1;
                default:
                    return 0;
            }
        }
    }
}
