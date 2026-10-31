using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceTraffic.Models
{
    public class Destination
    {
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public int Probability { get; set; }
    }
}
