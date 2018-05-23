using DIOControl.Config;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIOControl.Controller
{
    class ICPconDigitalController : IController
    {

        IDIOReport _Report;
        CtrlConfig _Cfg;
        Modbus.Device.ModbusIpMaster Master;
        ConcurrentDictionary<int, bool> IN = new ConcurrentDictionary<int, bool>();
        ConcurrentDictionary<int, bool> OUT = new ConcurrentDictionary<int, bool>();

        public ICPconDigitalController(CtrlConfig Config, IDIOReport TriggerReport)
        {
            _Cfg = Config;
            _Report = TriggerReport;

            switch (Config.ConnectionType)
            {
                case "Socket":
                    TcpClient tt = new TcpClient(Config.IPAdress, Config.Port);

                    Master = Modbus.Device.ModbusIpMaster.CreateIp(tt);
                    break;
            }
            Master.Transport.Retries = Config.Retries;
            Master.Transport.ReadTimeout = Config.ReadTimeout;

            Thread ReceiveTd = new Thread(Polling);
            ReceiveTd.IsBackground = true;
            ReceiveTd.Start();
        }

        private void Polling()
        {
            while (true)
            {

                bool[] Response = Master.ReadInputs(_Cfg.slaveID, 0, Convert.ToUInt16(_Cfg.DigitalInputQuantity));

                for (int i = 0; i < _Cfg.DigitalInputQuantity; i++)
                {
                    if (IN.ContainsKey(i))
                    {
                        bool org;
                        IN.TryGetValue(i, out org);
                        if (org != Response[i])
                        {
                            IN.TryUpdate(i, Response[i], org);
                            _Report.On_Data_Chnaged(_Cfg.DeviceName, "IN", i.ToString(), Response[i].ToString());
                        }
                    }
                    else
                    {
                        IN.TryAdd(i, Response[i]);
                        _Report.On_Data_Chnaged(_Cfg.DeviceName, "IN", i.ToString(), Response[i].ToString());
                    }
                }

                Response = Master.ReadCoils(_Cfg.slaveID, 0, Convert.ToUInt16(_Cfg.DigitalInputQuantity));

                for (int i = 0; i < _Cfg.DigitalInputQuantity; i++)
                {
                    if (OUT.ContainsKey(i))
                    {
                        bool org;
                        OUT.TryGetValue(i, out org);
                        if (org != Response[i])
                        {
                            OUT.TryUpdate(i, Response[i], org);
                            _Report.On_Data_Chnaged(_Cfg.DeviceName, "OUT", i.ToString(), Response[i].ToString());
                        }
                    }
                    else
                    {
                        OUT.TryAdd(i, Response[i]);
                        _Report.On_Data_Chnaged(_Cfg.DeviceName, "OUT", i.ToString(), Response[i].ToString());
                    }
                }
                SpinWait.SpinUntil(() => false, _Cfg.Delay);

            }
        }

        public void SetOut(string Address, string Value)
        {
            ushort adr = Convert.ToUInt16(Address);
            Master.WriteSingleCoil(_Cfg.slaveID, adr, Convert.ToBoolean(Value));

            bool[] Response = Master.ReadCoils(_Cfg.slaveID, adr, 1);
            bool org;
            OUT.TryGetValue(adr, out org);
            if (org != Response[0])
            {
                OUT.TryUpdate(adr, Response[0], org);
                _Report.On_Data_Chnaged(_Cfg.DeviceName, "OUT", adr.ToString(), Response[0].ToString());
            }
        }

        public string GetIn(string Address)
        {
            bool result = false;
            int key = Convert.ToInt32(Address);
            if (IN.ContainsKey(key))
            {
                if (!IN.TryGetValue(key, out result))
                {
                    throw new Exception("Address " + Address + " get fail!");
                }
            }
            else
            {
                throw new Exception("Address " + Address + " not exist!");
            }
            return result.ToString();
        }

        public string GetOut(string Address)
        {
            bool result = false;
            int key = Convert.ToInt32(Address);
            if (OUT.ContainsKey(key))
            {
                if (!OUT.TryGetValue(key, out result))
                {
                    throw new Exception("Address " + Address + " get fail!");
                }
            }
            else
            {
                throw new Exception("Address " + Address + " not exist!");
            }
            return result.ToString();
        }


    }
}
