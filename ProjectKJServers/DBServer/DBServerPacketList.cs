using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    enum DBPacketListID
    {
        REQUST_CHRACTER_INFO = 0
    }

    struct RequestCharacterInfoPacket(int AccountID, int CharacterID)
    {
        public int AccountID = AccountID;
        public int CharacterID = CharacterID;
    }

}
