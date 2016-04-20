using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using Fiddler;

namespace aIcantwEx03
{
    partial class MainWindow
    {
        private Queue<string> requestQueue = new Queue<string>();

        private void requestHandler_Init()
        {
            requestHandler.WorkerReportsProgress = true;
            requestHandler.WorkerSupportsCancellation = false;
            requestHandler.DoWork += requestHandler_DoWork;
            requestHandler.ProgressChanged += requestHandler_ProgressChanged;
            requestHandler.RunWorkerCompleted += requestHandler_RunWorkerCompleted;
        }

        private void addRequest(string request)
        {
            requestQueue.Enqueue(request);
        }

        private string getRequest()
        {
            return requestQueue.Dequeue();
        }

        private bool hasPendingRequest()
        {
            return (requestQueue.Count > 0);
        }

        private void clearRequestQueue()
        {
            requestQueue.Clear();
        }

        private void requestHandler_DoWork(object sender, DoWorkEventArgs e)
        {
            while (hasPendingRequest())
            {
                requestReturnObject rro;

                string requestText = getRequest();
                requestHandler.ReportProgress(0, rro = sendRequest(requestText));

                if (!rro.success)
                {
                    // TODO: Clear the queue and stop
                    // May have better handling later using request Group, only stop one group instead of clear the queue
                    clearRequestQueue();
                }
            }

        }

        private void requestHandler_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is requestReturnObject) {
                UpdateRequestInfo((requestReturnObject)e.UserState);
            }
        }

        private void requestHandler_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        public struct requestReturnObject
        {
            public bool success;
            public string msg;
            public Session oS;

        }

        private requestReturnObject sendRequest(string requestText)
        {
            requestReturnObject rro = new requestReturnObject();
            rro.success = false;
            rro.msg = "";
            rro.oS = null;

            if (oIcantwSession == null)
            {
                rro.msg = "<<No session captured>>";
                return rro;
            }

            try
            {
                string jsonString = requestText;
                byte[] requestBodyBytes = Encoding.UTF8.GetBytes(jsonString);
                oIcantwSession.oRequest["Content-Length"] = requestBodyBytes.Length.ToString();

                startFiddler(false);
                HTTPRequestHeaders oH = new HTTPRequestHeaders();

                oH.HTTPMethod = "POST";
                oH.HTTPVersion = "HTTP/1.1";
                byte[] rawPath = { 47, 109, 46, 100, 111 };
                oH.RawPath = rawPath;
                oH.RequestPath = "/m.do";
                oH.UriScheme = "http";
                oH.Add("Host", "kings52.icantw.com");
                oH.Add("Connection", "keep-alive");
                oH.Add("Content-Length", requestBodyBytes.Length.ToString());
                oH.Add("Origin", "http://kingres.icantw.com");
                oH.Add("X-Requested-With", " ShockwaveFlash/21.0.0.216");
                oH.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.112 Safari/537.36");
                oH.Add("Content-Type", " application/x-www-form-urlencoded");
                oH.Add("Accept", "*.*");
                oH.Add("Referer", "http://kingres.icantw.com/snres/loader_3691_2_40_1.swf?1/[[DYNAMIC]]/1/[[DYNAMIC]]/4");
                oH.Add("Accept-Encoding", " gzip, deflate");
                oH.Add("Accept-Language", " en-US,en;q=0.8,zh-TW;q=0.6,zh;q=0.4,zh-CN;q=0.2");
                oH.Add("Cookie", " _ga=GA1.2.2102237671.1451608934");


                // Proxy information 
                oH.Add("Proxy-Authorization", "Basic amtrbWE6UmpwdThwdGw=");
                oH.Add("Proxy-Connection", "keep-alive");


                HTTPRequestHeaders oOK = oIcantwSession.oRequest.headers;

                rro.oS = FiddlerApplication.oProxy.SendRequestAndWait(oH,
                                                                   requestBodyBytes, null, OnStageChangeHandler);
                stopFiddler();
                rro.success = true;
            }
            catch (Exception ex)
            {
                rro.msg = ex.Message;
            }

            return rro;
        }

        private void OnStageChangeHandler(object sender, StateChangeEventArgs e)
        {
            if (e.newState == SessionStates.Done)
            {
            }
        }



    }
}