using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCException
{
    public class ConnectionClosedException : Exception
    {
        public ConnectionClosedException()
        {
        }

        public ConnectionClosedException(string Message) : base(Message)
        {
        }

        public ConnectionClosedException(string Message, Exception InnerException) : base(Message, InnerException)
        {
        }
    }
}
