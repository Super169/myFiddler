using System;
using System.Collections.Generic;
using System.Threading;
using Fiddler;

namespace Demo
{
    class Program
    {
        static Proxy oSecureEndpoint;
        static string sSecureEndpointHostname = "localhost";
        static int iSecureEndpointPort = 7777;

        static string sCutomHost = "super169.home";


        public static void ConsoleWriteLine(string s, ConsoleColor c)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(s);
            Console.ForegroundColor = oldColor;
        }

        public static void WriteCommandResponse(string s)
        {
            ConsoleWriteLine(s, ConsoleColor.Yellow);
        }

        public static void DoQuit()
        {
            ConsoleWriteLine("Shutting down...", ConsoleColor.Red);
            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
            Fiddler.FiddlerApplication.Shutdown();
            Thread.Sleep(500);
        }

        private static string Ellipsize(string s, int iLen)
        {
            if (s.Length <= iLen) return s;
            return s.Substring(0, iLen - 3) + "...";
        }


        private static void WriteSessionList(List<Fiddler.Session> oAllSessions)
        {
            ConsoleWriteLine("------ Session list contains", ConsoleColor.Yellow);
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                Monitor.Enter(oAllSessions);
                foreach (Session oS in oAllSessions)
                {
                    Console.Write(String.Format("{0} {1} {2}\n{3} {4}\n\n", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType));
                }
            }
            finally
            {
                Monitor.Exit(oAllSessions);
            }
            Console.WriteLine();
            Console.ForegroundColor = oldColor;
            ConsoleWriteLine("------ End of Session list", ConsoleColor.Yellow);
        }

        static void Main(string[] args)
        {
            List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();

            // <-- Personalize for your Application, 64 chars or fewer
            Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp");

            #region AttachEventListeners

            Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA)
            {
                // Console.WriteLine("** NotifyUser: " + oNEA.NotifyString);
            };

            Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA)
            {
                // Console.WriteLine("** LogString: " + oLEA.LogString); 
            };

            Fiddler.FiddlerApplication.BeforeRequest += delegate (Fiddler.Session oS)
            {
                oS.bBufferResponse = false;
                /*
                Monitor.Enter(oAllSessions);
                oAllSessions.Add(oS);
                Monitor.Exit(oAllSessions);
                */


                if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
                {
                    ConsoleWriteLine("Secure host", ConsoleColor.Green);
                    oS.utilCreateResponseAndBypassServer();
                    oS.oResponse.headers.SetStatus(200, "Ok");
                    oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                    oS.oResponse["Cache-Control"] = "private, max-age=0";
                    oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                } else if (oS.hostname == sCutomHost) {
                    ConsoleWriteLine("Custom host", ConsoleColor.Green);
                    oS.utilCreateResponseAndBypassServer();
                    oS.oResponse.headers.SetStatus(200, "Ok");
                    oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                    oS.oResponse["Cache-Control"] = "private, max-age=0";
                    oS.utilSetResponseBody("<html><body><h1>Return from " + sCutomHost + "</h1>Request for http://" + sSecureEndpointHostname + ":" + oS.port.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                }

            };

            Fiddler.FiddlerApplication.AfterSessionComplete += delegate (Fiddler.Session oS)
            {
                string hostname = oS.hostname.ToLower();
                if (hostname.Contains("hafm") && hostname.Contains("cms.server.ha.org.hk"))
                {
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                    //Console.WriteLine("Finished session:\t" + oS.fullUrl); 

                    Console.Title = ("Session list contains: " + oAllSessions.Count.ToString() + " sessions");
                    Console.Write(String.Format("{0} {1} {2} -> {3} {4}\n", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType));
                }
            };

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            #endregion AttachEventListeners

            string sSAZInfo = "NoSAZ";

            Console.WriteLine(String.Format("Starting {0} ({1})...", Fiddler.FiddlerApplication.GetVersionString(), sSAZInfo));

            Fiddler.CONFIG.IgnoreServerCertErrors = false;

            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;

            int iPort = 8877;
            Fiddler.FiddlerApplication.Startup(iPort, oFCSF);

            FiddlerApplication.Log.LogFormat("Created endpoint listening on port {0}", iPort);

            FiddlerApplication.Log.LogFormat("Starting with settings: [{0}]", oFCSF);
            FiddlerApplication.Log.LogFormat("Gateway: {0}", CONFIG.UpstreamGateway.ToString());

            Console.WriteLine("Hit CTRL+C to end session.");

            oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);
            if (null != oSecureEndpoint)
            {
                FiddlerApplication.Log.LogFormat("Created secure endpoint listening on port {0}, using a HTTPS certificate for '{1}'", iSecureEndpointPort, sSecureEndpointHostname);
            }

            bool bDone = false;
            do
            {
                ConsoleWriteLine("\nEnter a command [C=Clear; L=List; G=Collect Garbage; W=write SAZ; R=read SAZ;\n\tS=Toggle Forgetful Streaming; T=Trust Root Certificate; Q=Quit]:", ConsoleColor.DarkYellow);
                Console.Write(">");
                ConsoleKeyInfo cki = Console.ReadKey();
                Console.WriteLine();
                switch (Char.ToLower(cki.KeyChar))
                {
                    case 'c':
                        Monitor.Enter(oAllSessions);
                        oAllSessions.Clear();
                        Monitor.Exit(oAllSessions);
                        WriteCommandResponse("Clear...");
                        FiddlerApplication.Log.LogString("Cleared session list.");
                        break;

                    case 'd':
                        FiddlerApplication.Log.LogString("FiddlerApplication::Shutdown.");
                        FiddlerApplication.Shutdown();
                        break;

                    case 'l':
                        WriteSessionList(oAllSessions);
                        break;

                    case 'g':
                        Console.WriteLine("Working Set:\t" + Environment.WorkingSet.ToString("n0"));
                        Console.WriteLine("Begin GC...");
                        GC.Collect();
                        Console.WriteLine("GC Done.\nWorking Set:\t" + Environment.WorkingSet.ToString("n0"));
                        break;

                    case 'q':
                        bDone = true;
                        DoQuit();
                        break;

                    case 'r':
                        WriteCommandResponse("This demo was compiled without SAZ_SUPPORT defined");
                        break;

                    case 'w':
                        WriteCommandResponse("This demo was compiled without SAZ_SUPPORT defined");
                        break;

                    case 't':
                        try
                        {
                            WriteCommandResponse("Result: " + Fiddler.CertMaker.trustRootCert().ToString());
                        }
                        catch (Exception eX)
                        {
                            WriteCommandResponse("Failed: " + eX.ToString());
                        }
                        break;

                    // Forgetful streaming
                    case 's':
                        bool bForgetful = !FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.ForgetStreamedData", false);
                        FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.ForgetStreamedData", bForgetful);
                        ConsoleWriteLine(bForgetful ? "FiddlerCore will immediately dump streaming response data." : "FiddlerCore will keep a copy of streamed response data.", ConsoleColor.Green);
                        break;

                }
            } while (!bDone);
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DoQuit();
        }
    }
}

