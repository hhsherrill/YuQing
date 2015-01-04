using OLDZHANG.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;

namespace ClientZhongGuoSuZhou
{
    public class Client
    {
        private Thread m_thread;
        private static string base_url = "http://www.suzhou.gov.cn";

        HttpHelper http = new HttpHelper();

        public Client()
        {
            m_thread = new Thread(DoWork);
            http.Encoding = Encoding.GetEncoding("gb2312");
            http.TimeOut = 20000;
            http.Accept = "text/html, application/xhtml+xml, */*";
            http.AutoRedirect = true;
        }

        public void Start()
        {
            m_thread.Start(this);
        }

        public void Abort()
        {
            m_thread.Abort();
        }

        public void DoWork(object data)
        {
            while (true)
            {
                try
                {
                    //获取信息列表
                    List<string> topiclist = getTopics();
                    foreach (string topic in topiclist)
                    {
                        try
                        {
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    Thread.Sleep(3 * 60 * 60 * 1000);//每隔3小时执行一次
                }
                catch (InvalidOperationException)
                {
                    Thread.Sleep(5 * 1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        //获取主题列表
        private List<string> getTopics()
        {
            List<string> topiclist = new List<string>();
            String[] webcontent = new String[2];
            string postData1 = string.Format("keystr={0}&select=1", HttpUtility.UrlEncode("苏州图书馆", Encoding.GetEncoding("gbk")));
            webcontent[0] = http.Post(base_url + "/asite/search.asp", postData1);
            string postData2 = string.Format("keystr={0}&select=1", HttpUtility.UrlEncode("苏图", Encoding.GetEncoding("gbk")));
            webcontent[1] = webcontent[0] = http.Post(base_url + "/asite/search.asp", postData2);

            foreach (string content in webcontent)
            {

                topiclist.Add(content);
            }
            return topiclist;
        }
    }
}
