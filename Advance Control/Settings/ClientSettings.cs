using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Settings
{
    public class ClientSettings
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public bool RememberLogin { get; set; }
        public int DefaultTimeoutSeconds { get; set; } = 30;
    }
}
