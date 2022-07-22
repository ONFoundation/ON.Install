using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ON.Content.Rumble.Service.Models
{
    public class AppSettings
    {
        public string DataStore { get; set; } = "/data";
        public string RumbleToken { get; set; }
        public string RumblePlatformToken { get; set; }
    }
}
