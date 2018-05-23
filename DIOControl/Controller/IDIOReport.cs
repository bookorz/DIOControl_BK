using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIOControl.Controller
{
    public interface IDIOReport
    {
        void On_Data_Chnaged(string DIOName, string Type, string Address, string Value);
        void On_Error_Occurred(string DIOName, string ErrorMsg);
    }
}
