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
                rro.oS = FiddlerApplication.oProxy.SendRequestAndWait(oIcantwSession.oRequest.headers,
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