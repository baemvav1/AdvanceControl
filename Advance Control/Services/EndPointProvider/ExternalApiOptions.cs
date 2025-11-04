using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    public class ExternalApiOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
    }
}