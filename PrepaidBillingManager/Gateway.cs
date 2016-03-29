using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;

namespace PrepaidBillingManager
{
    public class Gateway
    {
        private short id = 0;
        private string url = "";
        public JObject HangupCall(Call call) {
            JObject jsonResponse = null;
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader sReader = null;
            StreamWriter sWriter = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(this.url + "/HangupCall/");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                sWriter = new StreamWriter(request.GetRequestStream());
                sWriter.Write("CallUUID=" + call.UUID);
                sWriter.Flush();
                sWriter.Close();
                response = (HttpWebResponse)request.GetResponse();
                sReader = new StreamReader(response.GetResponseStream());
                jsonResponse = JObject.Parse(sReader.ReadToEnd());
                sReader.Close();
            }
            catch (Exception e)
            {
                jsonResponse = new JObject(new JProperty("Success", false), new JProperty("Message", e.ToString()));
            }
            finally {
                request = null;
                response = null;                
            }
            return jsonResponse;
        }
        public short Id { get { return id; } set { id = value; } }
        public string Url { get { return url; } set { url = value; } }
    }
}
