using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrepaidBillingManager
{
    public class Call
    {
        private long id = 0;
        private string uuid = null;
        private short gatewayId = 0;
        private short pulse = 0;
        private float pricePerPulse = 0;
        private int pulsesElapsed = 0;
        private bool isCompleted = false;

        public void Hangup() {
            lock (SharedClass.Gateways) {
                Gateway gateway = null;
                SharedClass.Gateways.TryGetValue(this.gatewayId, out gateway);
                if (gateway == null)
                {
                    SharedClass.Logger.Error("Invalid Gateway");
                }
                else {
                    gateway.HangupCall(this);
                }
            }
        }
        public long Id { get { return id; } set { id = value; } }
        public string UUID { get { return uuid; } set { uuid = value; } }
        public short GatewayId { get { return gatewayId; } set { gatewayId = value; } }
        public short Pulse { get { return pulse; } set { pulse = value; } }
        public float PricePerPulse { get { return pricePerPulse; } set { pricePerPulse = value; } }
        public int PulsesElapsed { get { return pulsesElapsed; } set { pulsesElapsed = value; } }
        public bool IsCompleted { get { return isCompleted; } set { isCompleted = value; } }
    }
}
