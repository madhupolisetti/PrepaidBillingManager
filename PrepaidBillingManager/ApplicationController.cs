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
            if (SharedClass.Gateways.Count > 0)
            {
                if (SharedClass.Listener.IpsList.Count > 0 && SharedClass.Listener.Port > 0)
                {
                    SharedClass.IsServiceCleaned = false;
                    SharedClass.Logger.Info("Starting Listener");
                    SharedClass.Listener.Initialize();
                }
                else
                {
                    throw new ArgumentNullException("IP and PORT are mandatory to start the listener. Please set them in config file");
                }
            }
            else {
                throw new ArgumentNullException("No Gateways Found");
            }
        }
        public void Stop() {
            if (SharedClass.Listener != null)
            {
                SharedClass.Listener.Destroy();
            }
            SharedClass.IsServiceCleaned = true;
        }
        public void LoadGateways() {
            System.Data.SqlClient.SqlConnection sqlCon = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString);
            System.Data.SqlClient.SqlCommand sqlCmd = new System.Data.SqlClient.SqlCommand("Select Id, Url From VoiceGateWays with(nolock)", sqlCon);
            System.Data.SqlClient.SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter();
            System.Data.DataSet ds = new System.Data.DataSet();
            da.SelectCommand = sqlCmd;
            da.Fill(ds);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) {
                foreach (System.Data.DataRow row in ds.Tables[0].Rows) {
                    Gateway gateway = new Gateway();
                    gateway.Id = Convert.ToSByte(row["Id"]);
                    gateway.Url = row["Url"].ToString();
                    SharedClass.Gateways.Add(gateway.Id, gateway);
                }
            }
        }
        private void LoadConfig() {
            if (ConfigurationManager.AppSettings["ListenerIpsList"] != null) {
                foreach (string ip in ConfigurationManager.AppSettings["ListenerIpsList"].ToString().Split(',')) {
                    SharedClass.Listener.IpsList.Add(ip);
                }
            }
            SharedClass.Listener.Port = ConfigurationManager.AppSettings["ListenerPort"] == null ? 0 : (int)Convert.ToInt16(ConfigurationManager.AppSettings["ListenerPort"]);
            if (ConfigurationManager.AppSettings["AllowedIpAddresses"] != null) {
                foreach (string ip in ConfigurationManager.AppSettings["AllowedIpAddresses"].ToString().Split(',')) {
                    SharedClass.AllowedIpAddresses.Add(ip);
                }
            }
        }
    }
}
