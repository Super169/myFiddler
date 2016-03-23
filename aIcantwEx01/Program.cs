using System;
using System.Collections.Generic;
using System.Threading;
using Fiddler;

namespace aIcantwEx01
{
    class Program
    {

        static string sIcantwHost = "icantw.com";
        static string sIcantwPath = "/m.do";
        static Session savedSession = null;

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
                    Console.Write(String.Format("{0} {1} {2}\n{3} {4}\n", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType));
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(oS.requestBodyBytes));
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(oS.responseBodyBytes));
                    Console.WriteLine();
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
            };

            Fiddler.FiddlerApplication.AfterSessionComplete += delegate (Fiddler.Session oS)
            {
                string hostname = oS.hostname.ToLower();
                if (hostname.Contains(sIcantwHost) && oS.PathAndQuery.Equals(sIcantwPath))
                {

                    if (savedSession == null)
                    {
                        savedSession = oS;
                    }

                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                    Console.Title = ("Session list contains: " + oAllSessions.Count.ToString() + " sessions");
                    Console.WriteLine(String.Format("{0} {1} {2} -> {3} {4}", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType));
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

                    case 'x':
                        if (savedSession == null)
                        {
                            ConsoleWriteLine("Session not yet captured", ConsoleColor.Yellow);
                        }
                        else
                        {
                            ConsoleWriteLine("Resent request", ConsoleColor.Yellow);
                            Session newSession = FiddlerApplication.oProxy.SendRequest(savedSession.oRequest.headers,
                                                                                       savedSession.requestBodyBytes, null, OnStageChangeHandler);
                        }
                        break;

                    case 'm':
                        Fiddler.FiddlerApplication.Shutdown();
                        Thread.Sleep(5000);
                        ConsoleWriteLine("Fiddler stopped", ConsoleColor.Red);
                        Console.WriteLine(savedSession.requestBodyBytes);
                        break;

                }
            } while (!bDone);
        }


        private static void OnStageChangeHandler(object sender, StateChangeEventArgs e)
        {
            if (e.newState == SessionStates.Done)
            {
                Session oS = (Session)sender;
                ConsoleWriteLine("OnStageChangeHandler - " + e.newState, ConsoleColor.Cyan);
                Console.WriteLine("ID: {0}", oS.id);
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(oS.requestBodyBytes));
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(oS.responseBodyBytes));
            }

        }


        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DoQuit();
        }
    }
}

