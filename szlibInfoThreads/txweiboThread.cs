using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using szlibInfoUtil;
using System.Reflection;

namespace szlibInfoThreads
{
    public class txweiboThread
    {
        private Thread m_thread;
        private static string m_url = "http://search.t.qq.com/index.php?k=苏州图书馆&pos=239&s_dup=1";

        public txweiboThread()
        {
            m_thread = new Thread(txweiboThread.DoWork);
        }

        public void Start()
        {
            m_thread.Start(this);
        }

        public void Abort()
        {
            m_thread.Abort();
        }

        public static void DoWork()
        {
            while (true)
            {
                try
                {
                    //获取页面
                    string webcontent = getWebContent.Fetch(m_url);
                    //获取微博列表
                    List<string> weiboList = getWeibos(webcontent);
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

        //获取博客列表
        private static List<string> getWeibos(string html)
        {
            List<string> weibolist = new List<string>();
            string s = html;
            s = Regex.Replace(s, "\\s{3,}", "");
            s = s.Replace("\r", "");
            s = s.Replace("\n", "");
            string pat = @"";
            MatchCollection mc = Regex.Matches(s, pat);
            foreach (Match m in mc)
            {
                weibolist.Add(m.Value);
            }
            return weibolist;
        }
    }
}
