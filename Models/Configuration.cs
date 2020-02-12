using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Configuration
    {
        public string URL { get; set; }
        public int Depth { get; set; }
        public int LoadFromSiteMap { get; set; } = 3;
        public bool IsHttps { get; set; } = false;
    }
}
