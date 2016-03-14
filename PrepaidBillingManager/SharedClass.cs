using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace PrepaidBillingManager
{
    public class SharedClass
    {
        public static string connectionString = null;
        private static ILog logger = (ILog)null;
        private static ILog dumpLogger = (ILog)null;
        private static ILog requestLogger = null;
        private static bool hasStopSignal = false;
        private static Listener listener = (Listener)null;
        private static bool isServiceCleaned = true;
        private static List<string> allowedIpAddresses = new List<string>();
        private static Dictionary<long, Account> accountMap = new Dictionary<long, Account>();
        private static Dictionary<short, Gateway> gatewayMap = new Dictionary<short, Gateway>();

        public static void InitiaLizeLogger()
        {
            GlobalContext.Properties["LogName"] = DateTime.Now.ToString("yyyyMMdd");
            log4net.Config.XmlConfigurator.Configure();
            SharedClass.logger = LogManager.GetLogger("Log");
            SharedClass.dumpLogger = LogManager.GetLogger("DumpLogger");
            SharedClass.requestLogger = LogManager.GetLogger("RequestLogger");
        }
        public static bool AddAcount(long accountId, Account account) {
            bool isAdded = false;
            try
            {
                lock (accountMap)
                {
                    if (!accountMap.ContainsKey(accountId))
                    {
                        accountMap.Add(accountId, account);
                    }
                    isAdded = true;
                }
            }
            catch (Exception e) {
                isAdded = false;
                SharedClass.logger.Error("Error Adding Account to Map : " + e.ToString());
            }
            return isAdded;
        }
        public static bool RemoveAccount(long accountId) {
            bool isRemoved = false;
            try
            {
                lock (accountMap)
                {
                    if (accountMap.ContainsKey(accountId))
                    {
                        accountMap.Remove(accountId);
                    }
                    isRemoved = false;
                }
            }
            catch (Exception e) {
                isRemoved = false;
                SharedClass.logger.Error("Error Removing Account From Map : " + e.ToString());
            }
            return isRemoved;
        }

        public static string ConnectionString { get { return connectionString; } set { connectionString = value; } }
        public static ILog Logger { get { return logger; } }
        public static ILog DumpLogger { get { return dumpLogger; } }
        public static ILog RequestLogger { get { return requestLogger; } }
        public static bool HasStopSignal { get { return hasStopSignal; } set { hasStopSignal = value; } }
        public static Listener Listener { get { if (SharedClass.listener == null) SharedClass.listener = new Listener(); return SharedClass.listener; } set { SharedClass.listener = value; } }
        public static bool IsServiceCleaned { get { return SharedClass.isServiceCleaned; } set { SharedClass.isServiceCleaned = value; } }
        public static List<string> AllowedIpAddresses { get { return allowedIpAddresses; } set { allowedIpAddresses = value; } }
        public static Dictionary<long, Account> Accounts { get { return accountMap; } set { accountMap = value; } }
        public static Dictionary<short, Gateway> Gateways { get { return gatewayMap; } set { gatewayMap = value; } }
    }
}
