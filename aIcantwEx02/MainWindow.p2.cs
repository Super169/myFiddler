using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace aIcantwEx02
{
    // Part 2: Task Handling
    public partial class MainWindow
    {
        private Queue<string> qTasks = new Queue<string>();
        private bool taskRunning = false;

        delegate void goNextTastInvoker(bool lastSuccess);


        private void goNextTask(bool lastSuccess = true)
        {
            if ((qTasks.Count > 0) && (lastSuccess))
            {
                string requestText = qTasks.Dequeue();
                if (!sendRequest(requestText)) qTasks.Clear();
            } else
            {
                qTasks.Clear();
                stopFiddler();
                taskRunning = false;
            }
        }

        private bool goTaskRetireAll()
        {
            if (oIcantwSession == null)
            {
                fillResponse("<<No session captured>>");
                return false;
            }

            if (taskRunning)
            {
                fillResponse("Task is running");
                return false;
            }

            // make sure the queue is cleared
            qTasks.Clear();
            qTasks.Enqueue(getRetireBody(1));
            qTasks.Enqueue(getRetireBody(2));
            qTasks.Enqueue(getRetireBody(5));
            qTasks.Enqueue(getRetireBody(6));
            qTasks.Enqueue(getRetireBody(7));
            qTasks.Enqueue(getRetireBody(8));
            taskRunning = true;
            goNextTask();

            return true;
        }

        private string getRetireBody(int pos)
        {
            string test;
            test = string.Format("\"{0}\"", 1);

            return string.Format("{{\"act\":\"Manor.retireAll\",\"sid\":\"{0}\",\"body\":\"{{\\\"decId\\\":{1}}}\"}}", txtSId.Text, pos);
        }

    }
}
