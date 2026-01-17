using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Exceptions
{
    public class DomainExceptions : Exception
    {
        public string ErrorCode { get; }

        public DomainExceptions(string message, string errorCode = "ERR_VALIDATE")
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
