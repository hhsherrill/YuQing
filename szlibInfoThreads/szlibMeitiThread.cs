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

namespace szlibInfoThreads
{
    public class szlibMeitiThread
    {
        private Thread m_thread;
        private static string m_url = "http://www.szlib.com/stzx/dynamicInformation.aspx?id=9";
        private static string base_url = "http://www.szlib.com/stzx/";

        public szlibMeitiThread()
        {
            m_thread = new Thread(szlibMeitiThread.DoWork);
        }

        public void Start()
        {
            m_thread.Start(this);
        }

        public void Abort()
        {
            m_thread.Abort();
        }

        public static void DoWork(object data)
        {
            while (true)
            {
                try
                {
                    //获取页面
                    string webcontent = baiduThread.Fetch(m_url);
                    //获取新闻列表
                    List<string> newslist = getNews(webcontent);
                    foreach (string news in newslist)
                    {
                        try
                        {
                            string newsurl = base_url + news.Substring(news.IndexOf('\'') + 1, news.LastIndexOf('\'') - news.IndexOf('\'') - 1);
                            if (newsurl != null)
                            {
                                string newsid = Utility.Hash(newsurl);
                                //如果库中已有该链接，表示已抓取过，后面的不用再抓取
                                if (SQLServerUtil.existNewsId(newsid)) break;
                                //没有就保存
                                string newstitle = news.Substring(news.LastIndexOf('\'') + 2, news.IndexOf("</a>") - news.LastIndexOf('\'') - 2);
                                string source = newstitle.Substring(newstitle.IndexOf('【') + 1, newstitle.IndexOf('】') - newstitle.IndexOf('【') - 1);
                                string contentHTML = baiduThread.Fetch(newsurl);
                                string time = null;
                                string timepat = @"<span id=""lblTime"">[^<>]+</span>";
                                Match match = Regex.Match(contentHTML, timepat);
                                if (match.Success)
                                {
                                    time = match.Value.Substring(match.Value.IndexOf('>') + 1, match.Value.LastIndexOf('<') - match.Value.IndexOf('>') - 1);
                                    time = Regex.Replace(time, "\\s{2,}", " ");
                                }
                                string content = null;
                                string contentpat = @"<span id=""lblcontent"">[\s\S]+?</span>";
                                Match match2 = Regex.Match(contentHTML, contentpat);
                                if (match2.Success) content = match2.Value.Replace("<br>", "\n");
                                content = content.Substring(content.IndexOf('>') + 1, content.LastIndexOf('<') - content.IndexOf('>') - 1);
                                SQLServerUtil.addNews(newsid, newstitle, Utility.Encode(content), time, source, newsurl, "本馆网站",null);
                            }
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

        //获取新闻列表
        private static List<string> getNews(string html)
        {
            List<string> newslist = new List<string>();
            string s = html;
            s = Regex.Replace(s, "\\s{3,}", "");
            s = s.Replace("\r", "");
            s = s.Replace("\n", "");
            string pat = @"<div id=""blogs"">[\s\S]+</div>[\s\S]*<!-- blogs -->";
            Match match = Regex.Match(s, pat);
            if (match.Success) s = match.Value;
            string pat2 = @"<h3><a href[ ]*=[ ]*[""']([^#<])+</a></h3>";
            MatchCollection mc = Regex.Matches(s, pat2);
            foreach (Match m in mc)
            {
                newslist.Add(m.Value);
            }
            return newslist;
        }
    }
}
