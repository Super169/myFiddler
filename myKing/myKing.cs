﻿using Fiddler;
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

        public struct LoginInfo
        {
            public string sid;
            public string serverTitle;
            public string account;
            public string nickName;
        }

        private static string getJsonFromResponse(string responseText)
        {
            string jsonString = null;

            string[] data = responseText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < data.Length; i++)
            {
                if (jsonString == null)
                {
                    // Not yet started, JSON string will start will "{", ignore all others
                    if (data[i].StartsWith("{")) jsonString = data[i];
                }
                else
                {
                    // study studying the behavious, it seesm that the dummy row has only few characters (hexdecimal value?), but not exactly the length
                    if (data[i].Length > 6) jsonString += data[i];
                }
            }

            return jsonString;
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

        public static LoginInfo getLogin_login(Fiddler.Session oS, string sid)
        {
            LoginInfo info = new LoginInfo() { sid = null};
            string sBody = string.Format("{{\"type\":\"WEB_BROWSER\", \"loginCode\":\"{0}\"}}", sid);
            requestReturnObject rro = goGenericRequest(oS, sid, "Login.login", false, sBody);
            if (rro.success)
            {
                string responseText = Encoding.UTF8.GetString(rro.session.responseBodyBytes);
                string jsonString = getJsonFromResponse(responseText);
                dynamic json = Json.Decode(jsonString);

                info.sid = sid;
                info.serverTitle = json.serverTitle;
                info.nickName = json.nickName;
            }
            return info;
        }
    }
}
