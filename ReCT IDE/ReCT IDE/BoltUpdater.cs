using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace ReCT_IDE
{
    public class BoltUpdater
    {
        public bool isUpdateAvailable(string version)
        {
            WebRequest request = WebRequest.Create("http://bytespace.tk/Extras/ReCTVersion");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            string responseFromServer = new StreamReader(dataStream).ReadToEnd();

            Console.WriteLine((version != responseFromServer) + " | Update response");

            dataStream.Close();
            response.Close();

            return version != responseFromServer;
        }
        public string getUpdateVersion()
        {
            WebRequest request = WebRequest.Create("http://bytespace.tk/Extras/ReCTVersion");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            return new StreamReader(dataStream).ReadToEnd();
        }
    }
}
