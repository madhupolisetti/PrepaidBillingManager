using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PrepaidBillingManager
{
    public class Account
    {
        private long id = 0;
        private float balance = 0;
        private Dictionary<long, Call> callsMap = null;
        private System.Threading.Mutex balanceMutex = null;
        public Account(long myId, short siteId) {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            System.IO.StreamReader sReader = null;
            this.id = myId;
            try
            {
                if (siteId == 1) {
                    request = (HttpWebRequest)WebRequest.Create("http://www.smscountry.com/GetBalance.ashx?UserId=" + this.id);
                }
                else if (siteId == 2) {
                    request = (HttpWebRequest)WebRequest.Create("http://voiceapi.smscountry.com/GetBalance?UserId=" + this.id);
                }
                request.Method = "GET";
                response = (HttpWebResponse)request.GetResponse();
                sReader = new System.IO.StreamReader(response.GetResponseStream());
                Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(sReader.ReadToEnd());
                sReader.Close();
                if (jObject.SelectToken("Success") != null && Convert.ToBoolean(jObject.SelectToken("Success").ToString()) == true && jObject.SelectToken("Balance") != null)
                {
                    float tempBalance = 0;                    
                    float.TryParse(jObject.SelectToken("Balance").ToString(), out tempBalance);
                    this.balance = tempBalance;
                }
                else {
                    throw new NotSupportedException("Success/Balance Not Found Or Success Flag Is False");
                }
            }
            catch (Exception e) {
                SharedClass.Logger.Error("Error Fetching Account Balance : " + e.ToString());
                throw new WebException(e.Message);
            }
            //this.balance = 1;
            callsMap = new Dictionary<long, Call>();
            balanceMutex = new System.Threading.Mutex();
        }        
        public bool CanDial(float pricePerPulse) {
            return (this.balance >= pricePerPulse);
        }
        public Newtonsoft.Json.Linq.JObject UpdateBalance(Call call) {
            Newtonsoft.Json.Linq.JObject responseObject = new Newtonsoft.Json.Linq.JObject();
            try
            {
                while (!balanceMutex.WaitOne())
                {
                    System.Threading.Thread.Sleep(10);
                }
                this.balance -= call.PricePerPulse;
                call.PulsesElapsed += 1;
                balanceMutex.ReleaseMutex();
                if (call.IsCompleted)
                {
                    RemoveCall(call.Id);
                }
                if (balance <= 0)
                {
                    SharedClass.Logger.Info("Balance Threshold Reached For Account " + this.id + ", Total Calls In Progress : " + this.callsMap.Count + ", Hangingup all calls now.");
                    List<KeyValuePair<long, Call>> callsList = callsMap.ToList();
                    foreach (KeyValuePair<long, Call> activeCall in callsList)
                    {
                        SharedClass.Logger.Info("HangingUp Call " + activeCall.Key + ", UUID : " + activeCall.Value.UUID);
                        activeCall.Value.Hangup();
                        RemoveCall(activeCall.Value.Id);
                    }
                }                
                responseObject = new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("Success", true), new Newtonsoft.Json.Linq.JProperty("Message", "OK"), new Newtonsoft.Json.Linq.JProperty("StatusCode", 200));
            }
            catch (Exception e)
            {
                responseObject = new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("Success", false), new Newtonsoft.Json.Linq.JProperty("Message", e.Message), new Newtonsoft.Json.Linq.JProperty("StatusCode", 500));
            }
            return responseObject;
        }
        public bool AddCall(Call call) {
            bool isAdded = false;
            try {
                lock (callsMap) {
                    if (!callsMap.ContainsKey(call.Id))
                    {
                        callsMap.Add(call.Id, call);
                    }
                }
                isAdded = true;
            }
            catch (Exception e) {
                SharedClass.Logger.Error("Error Adding Call (UUID : " + call.UUID + ", GatewayId : " + call.GatewayId + ", PricePerPulse : " + call.PricePerPulse + ") : " + e.ToString());
            }
            return isAdded;
        }
        public bool RemoveCall(long id) {
            bool isRemoved = false;
            try {
                lock (callsMap) {
                    if (callsMap.ContainsKey(id)) {
                        callsMap.Remove(id);
                    }
                    if (callsMap.Count == 0) {
                        SharedClass.RemoveAccount(this.id);
                    }
                }
                isRemoved = true;                
            }
            catch (Exception e) {
                SharedClass.Logger.Error("Error Removing Call (" + id + ") : " + e.ToString());
            }
            return isRemoved;
        }
        public long Id { get { return id; } set { id = value; } }
        public float Balance { get { return balance; } set { balance = value; } }
        public Dictionary<long, Call> Calls { get { return callsMap; } set { callsMap = value; } }
    }
}
