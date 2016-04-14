using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace myKing
{
    static class myKingInterface
    {

        public struct requestReturnObject
        {
            public bool success;
            public string msg;
            public Session session;
        }

        private static requestReturnObject goGenericRequest(Session oS, string sid, string act, bool addSId = true, string body = null)
        {
            dynamic json;
            string requestText = "";
            requestReturnObject rro;

            try
            {
                json = Json.Decode("{}");
                json.act = act;
                if (addSId) json.sid = sid;
                if (body != null) json.body = body;
                requestText = Json.Encode(json);
                rro = sendRequest(oS, requestText);
            }
            catch (Exception ex)
            {
                rro = new requestReturnObject();
                rro.success = false;
                rro.msg = ex.Message;
                rro.session = null;
            }
            return rro;
        }

        static requestReturnObject sendRequest(Session oS, string requestText)
        {
            requestReturnObject rro = new requestReturnObject();
            rro.success = false;
            rro.msg = "";
            rro.session = null;

            if (oS == null)
            {
                rro.msg = "<<No session captured>>";
                return rro;
            }

            try
            {
                string jsonString = requestText;
                byte[] requestBodyBytes = Encoding.UTF8.GetBytes(jsonString);
                oS.oRequest["Content-Length"] = requestBodyBytes.Length.ToString();

                if (!myFiddler.IsStarted()) myFiddler.Startup(false);

                // TODO: need to have OnStageChangeHandler for waiting method?
                // rro.oS = FiddlerApplication.oProxy.SendRequestAndWait(oS.oRequest.headers, requestBodyBytes, null, OnStageChangeHandler);
                rro.session = FiddlerApplication.oProxy.SendRequestAndWait(oS.oRequest.headers, requestBodyBytes, null, null);
                rro.success = true;
            }
            catch (Exception ex)
            {
                rro.msg = ex.Message;
            }
            return rro;
        }

        public static string getLogin_login(Fiddler.Session oS, string sid)
        {
            String retVal = "";
            string sBody = string.Format("{{\"type\":\"WEB_BROWSER\", \"loginCode\":\"{0}\"}}", sid);
            requestReturnObject rro = goGenericRequest(oS, sid, "Login.login", false, sBody);
            if (rro.success)
            {
                string responseText = Encoding.UTF8.GetString(rro.session.responseBodyBytes);
                retVal = responseText;

            }
            return retVal;
        }
    }
}
