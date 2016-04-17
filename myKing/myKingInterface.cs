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


        private static string GetSystemTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }


        public static string CleanUpResponse(string responseText)
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

        public static dynamic getJsonFromResponse(string responseText, bool cleanUp = true)
        {
            dynamic json = null;
            try
            {
                string jsonString = (cleanUp ? CleanUpResponse(responseText) : responseText);
                json = Json.Decode(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting response:\n{0}", ex.Message);
            }
            return json;
        }

        public static dynamic getJsonFromResponse(Session oS, bool cleanUp = true)
        {
            string responseText = GetResponseText(oS);
            return getJsonFromResponse(responseText, cleanUp);
        }

        public static string GetResponseText(Session oS)
        {
            return Encoding.UTF8.GetString(oS.responseBodyBytes);
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

        public static requestReturnObject getHeroInfo(Session oS, string sid)
        {
            return goGenericRequest(oS, sid, "Hero.getPlayerHeroList");
        }

        public static DynamicJsonArray ExtractHeros(Session oS)
        {
            DynamicJsonArray heros;
            try
            {
                dynamic json = getJsonFromResponse(oS);
                heros = json.heros;
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                heros = null;
            }
            return heros;
        }

        public static bool readHerosInfo(GameAccount oGA)
        {
            oGA.Heros.Clear();
            myFiddler.Startup(false);
            myKingInterface.requestReturnObject rro = myKingInterface.getHeroInfo(oGA.Session, oGA.Sid);
            myFiddler.Shutdown();

            bool success = false;
            try
            {
                if (success = rro.success)
                {
                    DynamicJsonArray heros = myKingInterface.ExtractHeros(rro.session);
                    if (heros != null)
                    {
                        foreach (dynamic hero in heros)
                        {
                            HeroInfo hi = new HeroInfo()
                            {
                                idx = hero.idx,
                                nm = hero.nm,
                                army = hero.army,
                                lv = hero.lv,
                                power = hero.power,
                                cfd = hero.cfd,
                                intl = hero.intl,
                                strg = hero.strg,
                                chrm = hero.chrm,
                                attk = hero.attk,
                                dfnc = hero.dfnc,
                                spd = hero.spd
                            };
                            oGA.Heros.Add(hi);
                        };
                        success = true;
                    }
                }

            }
            catch (Exception)
            {
                success = false;
            }
            return success;
        }

        public static requestReturnObject getDecreeInfo(Session oS, string sid)
        {
            return goGenericRequest(oS, sid, "Manor.decreeInfo");
        }

        public static bool GoBossWarOnce(Session oS, string sid, string bossWarBody, out string info)
        {
            string actionInfo;
            bool actionSuccess;
            info = "";

            actionInfo = "";
            actionSuccess = GoBossWarEnterWar(oS, sid, out actionInfo);
            info += actionInfo + "\n";

            // sendTroops only if successfully entered.
            if (actionSuccess)
            {
                actionInfo = "";
                actionSuccess = GoBossWarSendTroop(oS, sid, bossWarBody, out actionInfo);
                info += actionInfo + "\n";
            }

            // Must leave once for future, just for safety
            // Overall success must include previous success
            actionInfo = "";
            actionSuccess &= GoBossWarLeaveWar(oS, sid, out actionInfo);
            info += actionInfo + "\n";

            return (actionSuccess);
        }

        public static bool GoBossWarEnterWar(Session oS, string sid, out string info)
        {
            info = GetSystemTime() + " | enterWar | ";
            requestReturnObject rro = goGenericRequest(oS, sid, "BossWar.enterWar");
            if (!rro.success)
            {
                info += "FAIL | " + rro.msg;
                return false;
            }
            string responseText = GetResponseText(rro.session);
            dynamic json = getJsonFromResponse(responseText, false);
            if (json == null)
            {
                info += "FAIL | " + responseText;
                return false;
            }
            info += string.Format("SUCCESS | sent: {0}", json.sendCount);
            if (json.bossInfo != null) info += string.Format(" | Boss HP: {0}", json.bossInfo.hpp);
            return true;
        }


        public static bool GoBossWarLeaveWar(Session oS, string sid, out string info)
        {
            info = GetSystemTime() + " | leaveWar | ";
            requestReturnObject rro = goGenericRequest(oS, sid, "BossWar.leaveWar");
            if (!rro.success)
            {
                info += "FAIL | " + rro.msg;
                return false;
            }
            string responseText = GetResponseText(rro.session);
            dynamic json = getJsonFromResponse(responseText, false);
            if (json == null)
            {
                info += "FAIL | " + responseText;
                return false;
            }
            if (json.ok != 1)
            {
                info += "FAIL | " + responseText;
                return true;
            }
            info += "SUCCESS";
            return true;
        }

        public static bool GoBossWarSendTroop(Session oS, string sid, string body, out string info)
        {
            info = GetSystemTime() + " | enterWar | ";
            if ((body == null) || (body == "")) {
                info += "FAIL | missing troop information";
                return false;
            }

            requestReturnObject rro = goGenericRequest(oS, sid, "BossWar.sendTroop", true, body);
            if (!rro.success)
            {
                info += "FAIL | " + rro.msg;
                return false;
            }
            string responseText = GetResponseText(rro.session);
            dynamic json = getJsonFromResponse(responseText, false);
            if (json == null)
            {
                info += "FAIL | " + responseText;
                return false;
            }
            if (json.ok == 1)
            {
                info += "SUCCESS";
                return true;

            }
            info += "FAIL | ";
            if (json.prompt != null)
            {
                info += json.prompt;
            } else
            {
                info += responseText;
            }
            return false;
        }




    }
}
