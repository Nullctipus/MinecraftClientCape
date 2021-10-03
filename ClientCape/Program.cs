using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;

namespace ClientCape
{
    class Program
    {
        static string CapeFile = "Cape.png";
        static string Username = "";
        static HttpListener listener = new HttpListener();

        public static async Task HandleConnections()
        {
            byte[] capedata = File.ReadAllBytes(CapeFile);
            WebClient Resender = new WebClient();
            int RequestCount = 0;
            for(; ; )
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                //Log
                Console.WriteLine("Request #{0}:",++RequestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                string re = req.Url.ToString().Split(new string[] { "s.optifine.net" }, StringSplitOptions.None)[1];
                Console.WriteLine(re);
                Console.WriteLine();

                if ((string.IsNullOrEmpty(Username) && re.StartsWith("/capes")) || (re == "/capes/" + Username+".png"))
                {
                    resp.ContentType = "image/png";
                    resp.ContentLength64 = capedata.LongLength;
                    await resp.OutputStream.WriteAsync(capedata, 0, capedata.Length);
                    resp.Close();
                }
                else
                {
                    byte[] data = null;
                    try
                    {
                        data = Resender.DownloadData("http://35.190.10.249" + re);
                        if (re.ToLower().EndsWith(".png"))
                            resp.ContentType = "image/png";
                        else if (req.AcceptTypes.Length > 0) resp.ContentType = req.AcceptTypes[0];
                        resp.ContentLength64 = data.LongLength;
                    }
                    catch { }
                    if(data != null)
                    {
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine("Redirected {0} of data, of type {1}", data.LongLength, resp.ContentType);
                        resp.Close();
                    }
                    else
                    {
                        resp.StatusCode = 404;
                        resp.Close();
                    }
                }
            }
        }
        public static bool IsLocalIpAddress(string host)
        {
            try
            { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }
        static void Main(string[] args)
        {
            // Check If Being Redirected Properly
            {
                if (!IsLocalIpAddress("s.optifine.net"))
                {
                    Console.WriteLine("Not Redirecting Properly, Please Restart");
                    Process.Start("RUNFIRST.bat");
                    Console.Read();
                    Process.GetCurrentProcess().Close();
                }
            }
            if (args.Length >= 1) CapeFile = args[0];
            if (args.Length >= 2) Username = args[1];

            if (!File.Exists(CapeFile)) CapeFile = AppDomain.CurrentDomain.BaseDirectory + CapeFile;

            Console.WriteLine("Using Cape File " + CapeFile);
            Console.WriteLine("Replacing Cape For " + (string.IsNullOrEmpty(Username)?"Everyone":Username));
            listener.Prefixes.Add("http://s.optifine.net/");
            listener.Prefixes.Add("https://s.optifine.net/");
            listener.Start();
            Console.WriteLine("Started Server");
            Task li = HandleConnections();
            li.GetAwaiter().GetResult();
            listener.Close();
        }
    }
}
