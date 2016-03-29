using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;
using System.Threading;

namespace aIcantwEx03
{
    // Part 2: Task Handling
    public partial class MainWindow
    {

        private bool goTaskRetireAll()
        {
            addRequest(getRetireBody(1));
            addRequest(getRetireBody(2));
            addRequest(getRetireBody(5));
            addRequest(getRetireBody(6));
            addRequest(getRetireBody(7));
            addRequest(getRetireBody(8));
            requestHandler.RunWorkerAsync();
            return true;
        }

        private string getRetireBody(int pos)
        {
            string test;
            test = string.Format("\"{0}\"", 1);
            return string.Format("{{\"act\":\"Manor.retireAll\",\"sid\":\"{0}\",\"body\":\"{{\\\"decId\\\":{1}}}\"}}", txtSId.Text, pos);
        }


        private bool goTaskTester()
        {
            addRequest(getGenericRequest("Login.serverInfo"));
            addRequest(getGenericRequest("Shop.shopNextRefreshTime"));
            addRequest(getGenericRequest("Patrol.getPatrolInfo"));
            addRequest(getGenericRequest("Manor.getManorInfo"));
            requestHandler.RunWorkerAsync();
            return true;
        }


        private string getGenericRequest(string act, bool addSId = true, string body = null)
        {
            string requestText = "";
            dynamic json;
            try
            {
                json = Json.Decode("{}");
                json.act = act;
                if (addSId) json.sid = txtSId.Text;
                if (body != null) json.body = body;
                requestText = Json.Encode(json);
            }
            catch (Exception)
            {
            }
            return requestText;
        }



    }
}
