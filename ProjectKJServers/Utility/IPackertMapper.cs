using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public interface IPackertMapper<E>
    {
        public void GetPacketClassByID(E ID);
    }
}
