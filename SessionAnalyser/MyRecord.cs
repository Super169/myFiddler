using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionAnalyser
{
    [Serializable]
    public class MyRecord
    {
        public string action;
        public string style;
        public string prompt;
        public string requestBody;
        public string responseBody;
    }
}
