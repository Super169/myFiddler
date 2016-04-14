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

        public struct LoginInfo
        {
            public string sid;
            public string serverTitle;
            public string account;
            public string nickName;
            public string CORPS_NAME;
            public string LEVEL;
            public string VIP_LEVEL;
        }

        private static string CleanUpResponse(string responseText)
        {
            if (responseText == null) return null;

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

            if (!myFiddler.IsStarted())
            {
                rro.msg = "Fiddler engine not yet started";
                return rro;
            }

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

                // For safety, try not to start Fiddler inside, should be started before calling this method
                // if (!myFiddler.IsStarted()) myFiddler.Startup(false);

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

        private static dynamic getJsonFromResponse(string responseText)
        {
            dynamic json = null;
            try
            {
                string jsonString = CleanUpResponse(responseText);
                json = Json.Decode(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting response:\n{0}", ex.Message);
            }
            return json;
        }

        private static dynamic getJsonFromResponse(Session oS)
        {
            string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);
            return getJsonFromResponse(responseText);
        }

        public static LoginInfo getLogin_login(Fiddler.Session oS, string sid)
        {
            LoginInfo info = new LoginInfo() { sid = null};
            string sBody = string.Format("{{\"type\":\"WEB_BROWSER\", \"loginCode\":\"{0}\"}}", sid);
            requestReturnObject rro = goGenericRequest(oS, sid, "Login.login", false, sBody);
            if (!rro.success) return info;
            dynamic json = getJsonFromResponse(rro.session);
            info.sid = sid;
            info.account = json.account;
            info.serverTitle = json.serverTitle;
            info.nickName = json.nickName;

            rro = goGenericRequest(oS, sid, "Player.getProperties");
            if (!rro.success)   return info;
            json = getJsonFromResponse(rro.session);
            DynamicJsonArray pvs = (DynamicJsonArray)json.pvs;
            foreach (dynamic j in pvs)
            {
                if (j.p == "CORPS_NAME") info.CORPS_NAME = j.v;
                else if (j.p == "LEVEL") info.LEVEL = j.v;
                else if (j.p == "VIP_LEVEL") info.VIP_LEVEL = j.v;
            }
            return info;
        }
        

        public static string getHeroInfo(Session oS, string sid)
        {
            requestReturnObject rro = goGenericRequest(oS, sid, "Hero.getPlayerHeroList");
            if (!rro.success) return ("Fail getting Hero Information:\n" + rro.msg);
            return ExtractHeroInfo(rro.session);
        }

        // For Hero.getPlayerHeroList
        public static string ExtractHeroInfo(Session oS)
        {
            string info = "";
            try
            {
                dynamic json = getJsonFromResponse(oS);

                DynamicJsonArray heros = json.heros;

                foreach (dynamic hero in heros)
                {
                    string heroInfo = string.Format("{0} : {1} : {2} : {3} : {4} : {5}", hero.idx, hero.nm, hero.army, hero.lv, hero.power, hero.cfd);
                    heroInfo += string.Format(" : {0} : {1} : {2} : {3} : {4} : {5}", hero.intl, hero.strg, hero.chrm, hero.attk, hero.dfnc, hero.spd);
                    if (hero.amftLvs is DynamicJsonArray)
                    {
                        DynamicJsonArray s = (DynamicJsonArray)hero.amftLvs;
                        heroInfo += string.Format(" : [{0},{1},{2},{3},{4}]", s.ElementAt(0), s.ElementAt(1), s.ElementAt(2), s.ElementAt(3), s.ElementAt(4));
                    }
                    info += heroInfo + "\n";
                }
            }
            catch (Exception ex)
            {
                info = "Fail getting hero info:\n" + ex.Message;
            }

            return info;
        }


    }
}
