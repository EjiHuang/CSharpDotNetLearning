using System;
using System.Net.NetworkInformation;

namespace PingDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            PingHelper p = new PingHelper();
            string host = "14.215.177.38";

            p.PingCompleted += (s, e, ps) =>
            {
                Console.WriteLine("{0}  {1}", e.Reply?.Address.ToString(), e.Reply?.RoundtripTime);
            };

            Console.WriteLine(p.PingIP(String.Empty, null));
            p.PingIP("xxx", null);
            p.PingIP(host, null);

            Console.ReadKey();
        }
    }

    public class PingHelper
    {

        public delegate void DlgPingCompleteHandler(object sender, PingCompletedEventArgs p, params object[] parameters);
        public event DlgPingCompleteHandler PingCompleted = null;

        private object[] ps;

        public bool PingIP(string ip, params object[] ps)
        {
            Ping p = new Ping();
            this.ps = ps;
            p.PingCompleted += new PingCompletedEventHandler(PingCompletedEx);
            if (!string.IsNullOrWhiteSpace(ip))
            {
                p.SendAsync(ip, 5000, null);
                return true;
            }
            return false;
            
        }

        private void PingCompletedEx(object sender, PingCompletedEventArgs e)
        {
            PingCompleted(this, e, ps);
            Ping ping = (Ping) sender;
            ping.Dispose();
        }

    }
}
