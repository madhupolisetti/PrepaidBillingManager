using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrepaidBillingManager
{
    public class Account
    {
        private long id = 0;
        private float balance = 0;
        public Account() {
            this.balance = 100;
        }
        public long Id { get { return id; } set { id = value; } }
        public float Balance { get { return balance; } set { balance = value; } }
    }
}
