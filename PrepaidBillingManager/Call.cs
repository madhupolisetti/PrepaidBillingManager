using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrepaidBillingManager
{
    public class Call
    {
        private string uuid = null;
        private short gatewayId = 0;
        public string UUID { get { return uuid; } set { uuid = value; } }
        public short GatewayId { get { return gatewayId; } set { gatewayId = value; } }
    }
}
