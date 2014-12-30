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
    public class hswzThread
    {
        private Thread m_thread;
        private static string tsg_url = "http://www.12345.suzhou.gov.cn/bbs/search.php?mod=forum&searchid=1844&orderby=dateline&ascdesc=desc&searchsubmit=yes&kw=图书馆";
        private static string st_url = "http://www.12345.suzhou.gov.cn/bbs/search.php?mod=forum&searchid=1878&orderby=dateline&ascdesc=desc&searchsubmit=yes&kw=苏图";
        private static string base_url = "http://www.12345.suzhou.gov.cn/bbs/";

        public hswzThread()
        {
            m_thread = new Thread(hswzThread.DoWork);
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
                    //获取信息列表
                    List<string> topiclist = getTopics();
                    foreach (string topic in topiclist)
                    {
                        try
                        {
                            string topicurl=null;
                            string pat=@"<h3 class=""xs3""><a href[ ]*=[ ]*[""']([^""'#>])+[""']";
                            Match match = Regex.Match(topic, pat);
                            if (match.Success)
                            {
                                string temp = match.Value.Substring(match.Value.IndexOf(">") + 1);
                                topicurl = base_url+temp.Substring(temp.IndexOf('=') + 1).Trim('"', '\'', '#', ' ', '>').Replace("&amp;", "&");
                            }
                            if (topicurl != null)
                            {
                                string topicid = Utility.Hash(topicurl);                                
                                //如果库中已有该链接，表示已抓取过，看存储状态，已处理则跳过，未处理则读取处理情况进行更新
                                if (SQLServerUtil.existNewsId(topicid))
                                {
                                    string oldStatus=SQLServerUtil.getStatus(topicid);
                                    if (oldStatus == "已处理") continue;
                                    else
                                    {
                                        //读取主题页面
                                        string contentHTML = baiduThread.Fetch(topicurl);
                                        contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                        contentHTML = contentHTML.Replace("\r", "");
                                        contentHTML = contentHTML.Replace("\n", "");
                                        string status = null;  //状态（已关注、已处理）
                                        string statusPat = @"本主题由[ ]*\S+[ ]*于[ ]*[\s\S]+[ ]*添加图标[ ]*已\S{2}";
                                        Match match1 = Regex.Match(contentHTML, statusPat);
                                        if (match1.Success) status = match1.Value.Substring(match1.Value.Length - 3);
                                        if (status != oldStatus)
                                        {
                                            SQLServerUtil.updateStatus(topicid, status);
                                        }
                                        //已处理，保存处理结果
                                        if (status == "已处理")
                                        {
                                            string replyPat = @"<h3 class=""xs1 psth"">部门回复</h3><div class=""pstl xs1"">[\s\S]+?<div id=""comment_\d+"" class=""cm"">";
                                            Match match2 = Regex.Match(contentHTML, replyPat);
                                            if (match2.Success)
                                            {
                                                string replyHTML = match2.Value;
                                                replyHTML = Regex.Replace(replyHTML, @"(<[\s\S]+?>[\s]*)+", "|");
                                                string[] arr = replyHTML.Split('|');
                                                for (int i = 1; i <arr.Length; i+=4)
                                                {
                                                    string replydepart = arr[i];
                                                    string replySecs = arr[i+1].Substring(arr[i+1].LastIndexOf('(') + 1, arr[i+1].IndexOf(')') - arr[i+1].LastIndexOf('(') - 1);
                                                    DateTime dt = new DateTime(1970, 1, 1);
                                                    string replytime = dt.AddMilliseconds(Convert.ToInt32(replySecs) * 1000).ToString();
                                                    string replycontent = arr[i + 3];
                                                    SQLServerUtil.addReply(replycontent, replytime,replydepart,topicid);
                                                }                                                                                                
                                            }
                                        }
                                    }
                                }
                                //否则保存主题
                                else
                                {
                                    string topictitle = null;
                                    string titlepat = @"<a href[\s\S]+>[\s\S]+?</a></h3>";
                                    Match match1 = Regex.Match(topic, titlepat);
                                    if (match1.Success) topictitle = match1.Value.Substring(match1.Value.IndexOf('>') + 1, match1.Value.IndexOf("</a>") - match1.Value.IndexOf('>') - 1);
                                    topictitle = Regex.Replace(topictitle, "<[^<>]+>", "");
                                    string time = null;  //时间
                                    Match match2 = Regex.Match(topic, @"\d{4}-\d{1,2}-\d{1,2}[ ]*\d{2}:\d{2}");
                                    if (match2.Success)
                                    {
                                        time = match2.Value;
                                        time = Regex.Replace(time, "\\s{2,}", " ");
                                    }
                                    //读取主题页面
                                    string contentHTML = baiduThread.Fetch(topicurl);
                                    contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                    contentHTML = contentHTML.Replace("\r", "");
                                    contentHTML = contentHTML.Replace("\n", "");
                                    string content = null;  //内容
                                    string contentPat = @"<div id=""JIATHIS_CODE_HTML4""><div class=""t_fsz""><table cellspacing=""0"" cellpadding=""0""><tr><td class=""t_f"" id=""postmessage_\d+"">[\s\S]+?</td></tr></table>";
                                    Match match3 = Regex.Match(contentHTML, contentPat);
                                    if (match3.Success)
                                    {
                                        content = Regex.Replace(match3.Value, @"<div id=""JIATHIS_CODE_HTML4""><div class=""t_fsz""><table cellspacing=""0"" cellpadding=""0""><tr><td class=""t_f"" id=""postmessage_\d+"">", " ");
                                        content = Regex.Replace(content, "</td></tr></table>", "");
                                    }
                                    string status = null;  //状态（已关注、已处理）
                                    string statusPat = @"本主题由[ ]*\S+[ ]*于[ ]*[\s\S]+[ ]*添加图标[ ]*已\S{2}";
                                    Match match4 = Regex.Match(contentHTML, statusPat);
                                    if (match4.Success) status = match4.Value.Substring(match4.Value.Length - 3);
                                    SQLServerUtil.addNews(topicid, topictitle, content, time, null, topicurl, "寒山闻钟", status);
                                    //如果已处理，保存处理结果
                                    if (status == "已处理")
                                    {
                                        string replyPat = @"<h3 class=""xs1 psth"">部门回复</h3><div class=""pstl xs1"">[\s\S]+?<div id=""comment_\d+"" class=""cm"">";
                                        Match match5 = Regex.Match(contentHTML, replyPat);
                                        if (match5.Success)
                                        {
                                            string replyHTML = match5.Value;
                                            replyHTML = Regex.Replace(replyHTML, @"(<[\s\S]+?>[\s]*)+", "|");
                                            string[] arr = replyHTML.Split('|');
                                            for (int i = 1; i < arr.Length; i += 4)
                                            {
                                                string replydepart = arr[i];
                                                string replySecs = arr[i + 1].Substring(arr[i + 1].LastIndexOf('(') + 1, arr[i + 1].IndexOf(')') - arr[i + 1].LastIndexOf('(') - 1);
                                                DateTime dt = new DateTime(1970, 1, 1);
                                                string replytime = dt.AddMilliseconds(Convert.ToInt32(replySecs) * 1000).ToString();
                                                string replycontent = arr[i + 3];
                                                SQLServerUtil.addReply(replycontent, replytime, replydepart, topicid);
                                            }
                                        }
                                    }
                                }                               
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

        //获取主题列表
        private static List<string> getTopics()
        {
            List<string> topiclist = new List<string>();
            String[] webcontent=new String[2];
            webcontent[0] = baiduThread.Fetch(tsg_url);
            webcontent[1] = baiduThread.Fetch(st_url);
            foreach (string content in webcontent)
            {
                string html = Regex.Replace(content, "\\s{3,}", "");
                html = html.Replace("\r", "");
                html = html.Replace("\n", "");
                string pat=@"<li class=""pbw""[ ]*id=""\d+"">[\s\S]+?</li>";
                MatchCollection mc = Regex.Matches(html, pat);
                foreach (Match m in mc)
                {
                    topiclist.Add(m.Value);
                }
            }
            return topiclist;
        }
    }
}
