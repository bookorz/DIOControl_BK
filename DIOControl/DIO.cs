using DIOControl.Config;
using DIOControl.Controller;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIOControl
{
    public class DIO : IDIOReport
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DIO));
        IDIOTriggerReport _Report;
        ConcurrentDictionary<string, IController> Ctrls = new ConcurrentDictionary<string, IController>();
        ConcurrentDictionary<string, ParamConfig> Params = new ConcurrentDictionary<string, ParamConfig>();
        ConcurrentDictionary<string, ControlConfig> Controls = new ConcurrentDictionary<string, ControlConfig>();

        public DIO(IDIOTriggerReport ReportTarget)
        {
            _Report = ReportTarget;
            Thread InitTd = new Thread(Initial);
            InitTd.IsBackground = true;
            InitTd.Start();
        }

        public void Connect()
        {
            foreach (IController each in Ctrls.Values)
            {
                each.Connect();
            }
        }

        public void Close()
        {
            foreach (IController each in Ctrls.Values)
            {
                each.Close();
            }
        }

        private void Initial()
        {
            ConfigTool<CtrlConfig> configTool = new ConfigTool<CtrlConfig>();
            foreach (CtrlConfig each in configTool.ReadFileByList("config/DIO/DigitalList.json"))
            {
                IController eachCtrl = null;
                switch (each.DeviceType)
                {
                    case "ICPCONDIGITAL":
                        eachCtrl = new ICPconDigitalController(each, this);

                        break;
                }
                if (eachCtrl != null)
                {
                    Ctrls.TryAdd(each.DeviceName, eachCtrl);
                }
            }
            ConfigTool<ParamConfig> configTool2 = new ConfigTool<ParamConfig>();
            foreach (ParamConfig each in configTool2.ReadFileByList("config/DIO/ParameterSetting.json"))
            {
                Params.TryAdd(each.DeviceName + each.Address + each.Type, each);
            }
            ConfigTool<ControlConfig> configTool3 = new ConfigTool<ControlConfig>();
            foreach (ControlConfig each in configTool3.ReadFileByList("config/DIO/ControlSetting.json"))
            {
                Controls.TryAdd(each.Parameter, each);
            }


            Thread BlinkTd = new Thread(Blink);
            BlinkTd.IsBackground = true;
            BlinkTd.Start();
        }

        private void Blink()
        {
            string Current = "TRUE";
            while (true)
            {
                var find = from Out in Controls.Values.ToList()
                           where Out.Status.Equals("Blink")
                           select Out;
                foreach (ControlConfig each in find)
                {
                    IController ctrl;
                    if (Ctrls.TryGetValue(each.DeviceName, out ctrl))
                    {

                        ctrl.SetOut(each.Address, Current);
                        _Report.On_Data_Chnaged(each.Parameter, Current);
                    }
                    else
                    {
                        logger.Debug("SetIO:DeviceName is not exist.");
                    }
                }
                if (Current.Equals("TRUE"))
                {
                    Current = "FALSE";
                }
                else
                {
                    Current = "TRUE";
                }
                SpinWait.SpinUntil(() => false, 700);
            }
        }

        public bool SetIO(string Parameter, string Value)
        {
            bool result = false;
            try
            {
                ControlConfig ctrlCfg;
                if (Controls.TryGetValue(Parameter, out ctrlCfg))
                {
                    IController ctrl;
                    if (Ctrls.TryGetValue(ctrlCfg.DeviceName, out ctrl))
                    {
                        ctrlCfg.Status = Value;
                        ctrl.SetOut(ctrlCfg.Address, Value);
                        _Report.On_Data_Chnaged(Parameter, Value);
                    }
                    else
                    {
                        logger.Debug("SetIO:DeviceName is not exist.");
                    }
                }
                else
                {
                    logger.Debug("SetIO:Parameter is not exist.");
                }
            }
            catch (Exception e)
            {
                logger.Debug("SetIO:" + e.Message);
            }
            return result;
        }

        public bool SetBlink(string Parameter, string Value)
        {
            bool result = false;
            try
            {
                ControlConfig ctrlCfg;
                if (Controls.TryGetValue(Parameter, out ctrlCfg))
                {
                    if (Value.ToUpper().Equals("TRUE"))
                    {
                        ctrlCfg.Status = "Blink";
                    }

                }
                else
                {
                    logger.Debug("SetIO:Parameter is not exist.");
                }
            }
            catch (Exception e)
            {
                logger.Debug("SetBlink:" + e.Message);
            }
            return result;
        }

        public string GetIO(string Type, string Parameter)
        {

            string result = "";
            try
            {
                if (Type.Equals("OUT"))
                {
                    ControlConfig outCfg;
                    if (Controls.TryGetValue(Parameter, out outCfg))
                    {
                        IController ctrl;
                        if (Ctrls.TryGetValue(outCfg.DeviceName, out ctrl))
                        {
                            result = ctrl.GetOut(outCfg.Address);
                        }
                    }
                }
                else
                {
                    ParamConfig inCfg;
                    if (Params.TryGetValue(Parameter, out inCfg))
                    {
                        IController ctrl;
                        if (Ctrls.TryGetValue(inCfg.DeviceName, out ctrl))
                        {
                            result = ctrl.GetIn(inCfg.Address);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Debug("GetIO:" + e.Message);
            }
            return result;
        }

        public void On_Data_Chnaged(string DIOName, string Type, string Address, string Value)
        {
            string key = DIOName + Address + Type;
            ParamConfig param;
            if (Params.ContainsKey(key))
            {
                Params.TryGetValue(key, out param);
                _Report.On_Data_Chnaged(param.Parameter, Value);
            }
        }

        public void On_Error_Occurred(string DIOName, string ErrorMsg)
        {
            _Report.On_Error_Occurred(DIOName, ErrorMsg);
        }

        public void On_Connection_Status_Report(string DIOName, string Status)
        {
            if (Status.Equals("Connected"))
            {
                var find = from cfg in Controls.Values.ToList()
                           where cfg.DeviceName.Equals(DIOName)
                           select cfg;

                foreach (ControlConfig cfg in find)
                {
                    if (cfg.DeviceName.Equals(DIOName))
                    {
                        _Report.On_Data_Chnaged(cfg.Parameter, GetIO("OUT", cfg.Parameter));
                    }
                }
            }
            _Report.On_Connection_Status_Report(DIOName, Status);
        }
    }
}
