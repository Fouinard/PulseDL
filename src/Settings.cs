using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace PulseDL.src
{
    class Settings
    {
        public required string DownloadPath { get; set; }
        public string DefaultBrowser { get; set; } = "Sans navigateur";
    }
}
