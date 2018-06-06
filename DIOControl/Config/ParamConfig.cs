using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIOControl.Config
{
    class ParamConfig
    {
        public string DeviceName { get; set; }
        public string Type { get; set; }
        public string Address { get; set; }
        public string Parameter { get; set; }
        public bool Reverse { get; set; }
    }
}
