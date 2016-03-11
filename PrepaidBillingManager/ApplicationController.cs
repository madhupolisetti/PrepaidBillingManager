using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PrepaidBillingManager
{
    internal class ApplicationController
    {
        public ApplicationController() {
            this.LoadConfig();
        }
        public void Start() {
            if (SharedClass.Listener.Ip.Length > 7 && SharedClass.Listener.Port > 0)
            {
                SharedClass.IsServiceCleaned = false;
                SharedClass.Logger.Info("Starting Listener");
                SharedClass.Listener.Initialize();
            }
            else {
                throw new ArgumentNullException("IP and PORT are mandatory to start the listener. Please set them in config file");                
            }
        }
        public void Stop() {
            if (SharedClass.Listener != null)
            {
                SharedClass.Listener.Destroy();
            }
            SharedClass.IsServiceCleaned = true;
        }
        private void LoadConfig() {
            SharedClass.Listener.Ip = ConfigurationManager.AppSettings["ListenerIp"] == null ? "" : ConfigurationManager.AppSettings["ListenerIp"].ToString();
            SharedClass.Listener.Port = ConfigurationManager.AppSettings["ListenerPort"] == null ? 0 : (int)Convert.ToInt16(ConfigurationManager.AppSettings["ListenerPort"]);
            if (ConfigurationManager.AppSettings["AllowedIpAddresses"] != null) {
                foreach (string ip in ConfigurationManager.AppSettings["AllowedIpAddresses"].ToString().Split(',')) {
                    SharedClass.AllowedIpAddresses.Add(ip);
                }
            }
        }
    }
}
