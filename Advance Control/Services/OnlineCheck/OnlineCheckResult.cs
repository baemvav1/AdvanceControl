using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.OnlineCheck
{
    public class OnlineCheckResult
    {
        public bool IsOnline { get; set; }
        public int? StatusCode { get; set; }
        public string ErrorMessage { get; set; }

        public static OnlineCheckResult Success() =>
            new OnlineCheckResult { IsOnline = true, StatusCode = 200, ErrorMessage = null };

        public static OnlineCheckResult FromHttpStatus(int statusCode, string errorMessage = null) =>
            new OnlineCheckResult { IsOnline = statusCode >= 200 && statusCode <= 299, StatusCode = statusCode, ErrorMessage = errorMessage };

        public static OnlineCheckResult FromException(string message) =>
            new OnlineCheckResult { IsOnline = false, StatusCode = null, ErrorMessage = message };
    }
}
