using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;


namespace aIcantwEx03
{
    public partial class MainWindow 
    {

        private string getJsonFromResponse(string responseText)
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


        private string showLogin(string responseText)
        {
            string info = "";
            try
            {
                string jsonString = getJsonFromResponse(responseText);
                dynamic json = Json.Decode(jsonString);
                info = string.Format("{0} : {1}", json.serverTitle, json.nickname);
            } catch (Exception ex)
            {
                info = "Fail getting login info:\n" + ex.Message;
            }

            return info;
        }

        // For Hero.getPlayerHeroList
        private string showHero(string responseText)
        {
            string info = "";
            try
            {
                string jsonString = getJsonFromResponse(responseText);
                dynamic json = Json.Decode(jsonString);

                DynamicJsonArray heros = json.heros;

                foreach (dynamic hero in heros)
                {
                    string heroInfo = string.Format("{0} : {1} : {2} : {3} : {4} : {5}", hero.idx, hero.nm, hero.army, hero.lv,hero.power, hero.cfd);
                    heroInfo += string.Format(" : {0} : {1} : {2} : {3} : {4} : {5}", hero.intl, hero.strg, hero.chrm, hero.attk, hero.dfnc, hero.spd);
                    if (hero.amftLvs is DynamicJsonArray)
                    {
                        DynamicJsonArray s = (DynamicJsonArray)hero.amftLvs;
                        heroInfo += string.Format(" : [{0},{1},{2},{3},{4}]", s.ElementAt(0), s.ElementAt(1), s.ElementAt(2), s.ElementAt(3), s.ElementAt(4));
                    }
                    info += heroInfo + "\n";
                }
            } catch (Exception ex)
            {
                info = "Fail getting hero info:\n" + ex.Message;
            }

            return info;
        }


    }

}
