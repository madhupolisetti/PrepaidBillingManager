using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PrepaidBillingManager
{
    public class Listener
    {
        private string ip = "";
        private int port = 0;
        private HttpListener listener = null;
        private List<string> prefixes = null;
        public void Initialize()
        {
            this.prefixes = new List<string>();
            foreach (string tempip in "127.0.0.1,192.168.1.35".Split(',')) {
                this.prefixes.Add("http://" + tempip + ":" + this.port + "/Call/");
                this.prefixes.Add("http://" + tempip + ":" + this.port + "/Notify/");
            }
            new Thread(new ThreadStart(this.Start)) { Name = "Listener" }.Start();
        }
        private void Start()
        {
            this.listener = new HttpListener();
            HttpListenerContext context = null;
            string requestUUID = null;
            JObject jsonResponse = null;
            int statusCode = 200;
            foreach (string uriPrefix in this.prefixes)
            {
                SharedClass.Logger.Info((object)("Adding Url " + uriPrefix + " To Listener"));
                try
                {
                    this.listener.Prefixes.Add(uriPrefix);
                }
                catch (Exception ex)
                {
                    SharedClass.Logger.Error((object)("Error Adding prefix To Listener, Reason : " + ex.ToString()));
                }
            }
            this.listener.Start();
            SharedClass.Logger.Info((object)"Started Listening On " + this.ip + ":" + this.port);
            while (!SharedClass.HasStopSignal) {
                try
                {
                    try
                    {
                        context = null;
                        requestUUID = null;
                        jsonResponse = null;
                        statusCode = 200;
                        context = this.listener.GetContext();
                        if (context != null)
                        {
                            requestUUID = System.Guid.NewGuid().ToString();
                            jsonResponse = new JObject();
                            SharedClass.RequestLogger.Info("New Request [" + requestUUID + "] from  " + context.Request.RemoteEndPoint.Address + ":" + context.Request.RemoteEndPoint.Port + " To [" + context.Request.HttpMethod + "] " + context.Request.RawUrl);
                            if (!SharedClass.AllowedIpAddresses.Contains(context.Request.RemoteEndPoint.Address.ToString()))
                            {
                                statusCode = 503;
                                jsonResponse.Add(new JProperty("Success", false));
                                jsonResponse.Add(new JProperty("Message", "Service Unavailable"));
                            }
                            else {
                                jsonResponse.Add(new JProperty("Success", true));
                                jsonResponse.Add(new JProperty("Message", "OK"));
                            }
                        }
                    }
                    catch (HttpListenerException e)
                    {
                        //SharedClass.Logger.Error("HttpListener Exception : " + e.ToString());
                    }
                    catch (Exception e)
                    {
                        SharedClass.Logger.Error(e.ToString());
                    }
                    finally {
                        try {
                            if (context != null) {
                                context.Response.OutputStream.Write(System.Text.Encoding.UTF8.GetBytes(jsonResponse.ToString()), 0, jsonResponse.ToString().Length);
                                context.Response.Close();
                            }
                        }
                        catch (Exception e) { 

                        }
                    }
                }
                catch (Exception e) {
                    SharedClass.Logger.Error("Error Processing Request : " + e.ToString());
                }
            }
            //do
            //    ;
            //while (!SharedClass.HasStopSignal);
            //SharedClass.Logger.Info((object)"Listener Exited From While Loop");
        }
        public void Destroy()
        {
            if (this.listener == null)
            {
                return;
            }
            try
            {
                this.listener.Stop();
                SharedClass.Logger.Info((object)"WebListener Stop Executed");
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error((object)("Error Stopping WebListener : " + ex.Message));
            }
            try
            {
                this.listener.Close();
                SharedClass.Logger.Info((object)"Listener Close Executed");
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error((object)("Error Closing Listener " + ex.ToString()));
            }
            try
            {
                this.listener.Abort();
                SharedClass.Logger.Info((object)"Listener Abort Executed");
            }
            catch (Exception ex)
            {
                SharedClass.Logger.Error((object)("Error Aborting Listener " + ex.ToString()));
            }
            this.listener = (HttpListener)null;
        }
        public string Ip { get { return this.ip; } set { this.ip = value; } }
        public int Port { get { return this.port; } set { this.port = value; } } 
    }
}
