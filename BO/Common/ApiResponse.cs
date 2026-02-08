using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Common
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public string? ErrorCode { get; set; } // Nullable


        public ApiResponse(int status, string message, T? data)
        {
            Status = status;
            Message = message;
            Data = data;
            ErrorCode = null;
        }

        
        public ApiResponse(int status, string message, string? errorCode)
        {
            Status = status;
            Message = message;
            Data = default;
            ErrorCode = errorCode;
        }
    }
}
