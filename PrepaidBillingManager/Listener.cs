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
        private List<string> ipsList = new List<string>();
        private int port = 0;
        private HttpListener listener = null;
        private List<string> prefixes = null;
        public void Initialize()
        {
            this.prefixes = new List<string>();
            foreach (string ip in this.ipsList) {
                this.prefixes.Add("http://" + ip + ":" + this.port + "/Call/");
                this.prefixes.Add("http://" + ip + ":" + this.port + "/PulseNotify/");
                this.prefixes.Add("http://" + ip + ":" + this.port + "/Accounts/");
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
            Account account = null;
            Call call = null;
            foreach (string uriPrefix in this.prefixes)
            {
                //SharedClass.Logger.Info((object)("Adding Url " + uriPrefix + " To Listener"));
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
            foreach (string prefix in listener.Prefixes) {
                SharedClass.Logger.Info("Started Listening On " + prefix);
            }
            while (!SharedClass.HasStopSignal) {
                try
                {
                    try
                    {
                        context = null;
                        requestUUID = null;
                        jsonResponse = null;
                        statusCode = 200;
                        account = null;
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
                                if (context.Request.RawUrl.StartsWith("/Call")) {
                                    if (context.Request.QueryString["AccountId"] == null || context.Request.QueryString["GatewayId"] == null || context.Request.QueryString["PricePerPulse"] == null || context.Request.QueryString["UUID"] == null || context.Request.QueryString["CallId"] == null || context.Request.QueryString["SiteId"] == null)
                                    {
                                        statusCode = 400;
                                        jsonResponse.Add(new JProperty("Success", false));
                                        jsonResponse.Add(new JProperty("Message", "AccountId,GatewayId,UUID,PricePerPulse,CallId,SiteId are Mandatory"));
                                        continue;
                                    }
                                    SharedClass.Accounts.TryGetValue(Convert.ToInt64(context.Request.QueryString["AccountId"]), out account);
                                    if (account == null)
                                    {
                                        account = new Account(Convert.ToInt64(context.Request.QueryString["AccountId"]), Convert.ToSByte(context.Request.QueryString["SiteId"]));
                                        SharedClass.AddAcount(account.Id, account);
                                    }
                                    call = new Call();
                                    call.Id = Convert.ToInt64(context.Request.QueryString["CallId"]);
                                    call.UUID = context.Request.QueryString["UUID"];
                                    call.GatewayId = Convert.ToInt16(context.Request.QueryString["GatewayId"]);
                                    call.PricePerPulse = float.Parse(context.Request.QueryString["PricePerPulse"], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                                    account.AddCall(call);
                                    if (account.CanDial(call.PricePerPulse))
                                    {   
                                        jsonResponse.Add(new JProperty("Success", true));
                                        jsonResponse.Add(new JProperty("Message", "OK"));
                                    }
                                    else
                                    {
                                        statusCode = 403;
                                        jsonResponse.Add(new JProperty("Success", false));
                                        jsonResponse.Add(new JProperty("Message", "Insufficient Balance"));
                                        account.RemoveCall(call.Id);
                                    }
                                }
                                else if (context.Request.RawUrl.StartsWith("/PulseNotify"))
                                {
                                    if (context.Request.QueryString["CallId"] == null || context.Request.QueryString["AccountId"] == null)
                                    {
                                        statusCode = 400;
                                        jsonResponse.Add(new JProperty("Success", false));
                                        jsonResponse.Add(new JProperty("Message", "CallId, AccountId are Mandatory"));
                                        continue;
                                    }
                                    SharedClass.Accounts.TryGetValue(Convert.ToInt64(context.Request.QueryString["AccountId"]), out account);
                                    if (account == null) {
                                        statusCode = 404;
                                        jsonResponse.Add(new JProperty("Success", false));
                                        jsonResponse.Add(new JProperty("Message", "Account Not Found In Map"));
                                        continue;
                                    }
                                    account.Calls.TryGetValue(Convert.ToInt64(context.Request.QueryString["CallId"]), out call);
                                    if (call == null) {
                                        statusCode = 404;
                                        jsonResponse.Add(new JProperty("Success", false));
                                        jsonResponse.Add(new JProperty("Message", "Call Not Found In Map"));
                                        continue;
                                    }
                                    bool isCompleted = false;
                                    if (context.Request.QueryString["IsCompleted"] != null && bool.TryParse(context.Request.QueryString["IsCompleted"], out isCompleted)) {
                                        call.IsCompleted = isCompleted;
                                    }
                                    jsonResponse = account.UpdateBalance(call);
                                    statusCode = Convert.ToInt16(jsonResponse.SelectToken("StatusCode").ToString());
                                    jsonResponse.Remove("StatusCode");
                                }
                                else if (context.Request.RawUrl.StartsWith("/Accounts")) {
                                    jsonResponse.Add(new JProperty("Success", true));
                                    jsonResponse.Add(new JProperty("Message", "OK"));
                                    jsonResponse.Add(new JProperty("AccountCount", SharedClass.Accounts.Count));
                                    JArray accountsArray = new JArray();
                                    JArray callsArray = null;
                                    JObject tempAccount = null;
                                    JObject tempCall = null;
                                    bool renderCalls = false;
                                    if (context.Request.QueryString["RenderCalls"] != null) {
                                        bool.TryParse(context.Request.QueryString["RenderCalls"].ToString(), out renderCalls);
                                    }
                                    foreach (KeyValuePair<long, Account> activeAccount in SharedClass.Accounts) {
                                        tempAccount = new JObject();                                        
                                        tempAccount.Add(new JProperty("Id", activeAccount.Value.Id));
                                        tempAccount.Add(new JProperty("Balance", activeAccount.Value.Balance));
                                        tempAccount.Add(new JProperty("CallsCount", activeAccount.Value.Calls.Count));
                                        if (renderCalls) {
                                            callsArray = new JArray();
                                            foreach (KeyValuePair<long, Call> activeCall in activeAccount.Value.Calls) {
                                                tempCall = new JObject();
                                                tempCall.Add(new JProperty("Id", activeCall.Value.Id));
                                                tempCall.Add(new JProperty("UUID", activeCall.Value.UUID));
                                                tempCall.Add(new JProperty("GatewayId", activeCall.Value.GatewayId));
                                                tempCall.Add(new JProperty("IsCompleted", activeCall.Value.IsCompleted));
                                                tempCall.Add(new JProperty("PricePerPulse", activeCall.Value.PricePerPulse));
                                                tempCall.Add(new JProperty("Pulse", activeCall.Value.Pulse));
                                                tempCall.Add(new JProperty("PulsesElapsed", activeCall.Value.PulsesElapsed));
                                                callsArray.Add(tempCall);
                                            }
                                            tempAccount.Add(new JProperty("Calls", callsArray));
                                        }
                                        accountsArray.Add(tempAccount);
                                    }
                                    jsonResponse.Add(new JProperty("Accounts", accountsArray));
                                }
                            }
                        }
                    }
                    catch (HttpListenerException e)
                    {
                        //SharedClass.Logger.Error("HttpListener Exception : " + e.ToString());
                    }
                    catch (Exception e)
                    {
                        statusCode = 500;
                        jsonResponse = new JObject(new JProperty("Success", false), new JProperty("Message", e.Message));
                        SharedClass.Logger.Error(e.ToString());
                    }
                    finally {
                        try {
                            if (context != null) {
                                context.Response.StatusCode = statusCode;
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
        public List<string> IpsList { get { return this.ipsList; } set { this.ipsList = value; } }
        public int Port { get { return this.port; } set { this.port = value; } } 
    }
}
