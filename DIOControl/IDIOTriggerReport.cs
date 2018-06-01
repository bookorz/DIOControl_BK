﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIOControl
{
    public interface IDIOTriggerReport
    {
        void On_Data_Chnaged(string Parameter, string Value);
        void On_Error_Occurred(string DIOName, string ErrorMsg);
        void On_Connection_Status_Report(string DIOName, string Status);
    }
}
