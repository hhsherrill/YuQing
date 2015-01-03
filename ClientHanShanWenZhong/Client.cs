using EasyHttp.Http;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using szlibInfoUtil;
using System.Windows.Forms;

namespace ClientHanShanWenZhong
{
    public class Client
    {
        private Thread m_thread;
        private string base_url = "http://www.12345.suzhou.gov.cn/bbs/";

        HttpClient http = new HttpClient("http://www.12345.suzhou.gov.cn/bbs/");
        object searchquery1 = new { srchtxt = "图书馆", mod = "forum", orderby = "dateline", ascdesc = "desc", searchsubmit = "yes" };
        object searchquery2 = new { srchtxt = "苏图", mod = "forum", orderby = "dateline", ascdesc = "desc", searchsubmit = "yes" };

        public Client()
        {
            m_thread = new Thread(DoWork);
            http.Request.PersistCookies = true;
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
                            //MessageBox.Show(topic);
                            string topicurl = null;
                            string pat = @"<h3 class=""xs3""><a href[ ]*=[ ]*[""']([^""'#>])+[""']";
                            Match match = Regex.Match(topic, pat);
                            if (match.Success)
                            {
                                string temp = match.Value.Substring(match.Value.IndexOf(">") + 1);
                                topicurl =temp.Substring(temp.IndexOf('=') + 1).Trim('"', '\'', '#', ' ', '>').Replace("&amp;", "&");
                            }
                            if (topicurl != null)
                            {
                                string topicid = Utility.Hash(base_url+topicurl);
                                //如果库中已有该链接，表示已抓取过，看存储状态，已处理则跳过，未处理则读取处理情况进行更新
                                if (SQLServerUtil.existNewsId(topicid))
                                {
                                    string oldStatus = SQLServerUtil.getStatus(topicid);
                                    if (oldStatus == "已处理") continue;
                                    else
                                    {
                                        //读取主题页面
                                        string contentHTML = http.Get(topicurl).RawText;
                                        contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                        contentHTML = contentHTML.Replace("\r", "");
                                        contentHTML = contentHTML.Replace("\n", "");
                                        string status = null;  //状态（已关注、已处理）
                                        string statusPat = @"本主题由[ ]*\S+[ ]*于[ ]*[\s\S]+[ ]*添加图标[ ]*已\S{2}";
                                        Match match1 = Regex.Match(contentHTML, statusPat);
                                        if (match1.Success) status = match1.Value.Substring(match1.Value.Length - 3);
                                        if (status != oldStatus)
                                        {
                                            SQLServerUtil.updateStatus(topicid, status, DateTime.Now.ToString());
                                        }
                                        //已处理，保存处理结果
                                        if (status == "已处理")
                                        {                                            
                                            getReply(contentHTML, topicid);
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
                                    Match match2 = Regex.Match(topic, @"<span>(?<time>\d{4}-\d{1,2}-\d{1,2}[ ]*\d{2}:\d{2})</span>\s*-");
                                    if (match2.Success)
                                    {
                                        time = match2.Groups["time"].Value;
                                        time = Regex.Replace(time, "\\s{2,}", " ");
                                    }
                                    string source = null;//发帖者
                                    Match sourceMatch = Regex.Match(topic.Replace("&amp;","&"), @"<a href=""home.php\?mod=space&uid=\d+"" target=""_blank"">(?<source>[^<>]+?)</a>");
                                    if (sourceMatch.Success) source = sourceMatch.Groups["source"].Value;                                   
                                    //读取主题页面
                                    string contentHTML = http.Get(topicurl).RawText;
                                    contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                    contentHTML = contentHTML.Replace("\r", "");
                                    contentHTML = contentHTML.Replace("\n", "");
                                    string content = null;  //内容
                                    string contentPat = @"<div id=""JIATHIS_CODE_HTML4""><div class=""\S+""><table cellspacing=""0"" cellpadding=""0""><tr><td class=""t_f"" id=""postmessage_\d+"">[\s\S]+?</td></tr></table>";
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
                                    //MessageBox.Show(topicid + "\n" + topictitle + "\n" + time + "\n" + base_url + topicurl);
                                    SQLServerUtil.addNews(topicid, topictitle, content, time, source, base_url+topicurl, "寒山闻钟", status, DateTime.Now.ToString(), DateTime.Now.ToString());
                                    //如果已处理，保存处理结果
                                    if (status == "已处理")
                                    {                                       
                                        getReply(contentHTML,topicid);
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
        private List<string> getTopics()
        {
            List<string> topiclist = new List<string>();
            String[] webcontent = new String[2];
            var response = http.Get("search.php", searchquery1);
            if (response.ContentLength < 1000)
            {
                response = http.Get("search.php", searchquery1);
            }
            webcontent[0] = HttpUtility.HtmlDecode(response.RawText);

            response = http.Get("search.php", searchquery2);
            if (response.ContentLength < 1000)
            {
                response = http.Get("search.php", searchquery2);
            }
            webcontent[1] = HttpUtility.HtmlDecode(response.RawText);

            foreach (string content in webcontent)
            {
                string html = Regex.Replace(content, "\\s{3,}", "");
                html = html.Replace("\r", "");
                html = html.Replace("\n", "");
                string pat = @"<li class=""pbw""[ ]*id=""\d+"">[\s\S]+?</li>";
                MatchCollection mc = Regex.Matches(html, pat);
                foreach (Match m in mc)
                {
                    topiclist.Add(m.Value);
                }
            }
            return topiclist;
        }

        //获取回复并保存
        private void getReply(string html,string topicid)
        {
            string replyPat = @"<div class=""pstl xs1"">[\s\S]+?</div>\s*</div>\s*</div>";
            MatchCollection mc = Regex.Matches(html, replyPat);
            if (mc != null && mc.Count > 0)
            {
                foreach (Match m in mc)
                {
                    string replycontent=m.Value;
                    replycontent = replycontent.Replace("&amp;", "&");
                    //回复部门
                    string replypart = null;
                    string replypartPat=@"<a href=""home.php\?mod=space&uid=\d+"" class=""xi2 xw1"">(?<depart>[\s\S]+?)</a>";
                    Match match = Regex.Match(replycontent, replypartPat);
                    if (match.Success) replypart = match.Groups["depart"].Value;
                    //回复时间
                    string replytime = null;
                    string replyTimePat = @"<script language=""javascript""> *document.write\(getLocalTime\((?<secs>\d+)\)\); *</script>";
                    Match match4 = Regex.Match(replycontent, replyTimePat);                   
                    DateTime dt = new DateTime(1970, 1, 1);
                    //MessageBox.Show(match4.Groups["secs"].Value);
                    replytime = dt.AddSeconds(Convert.ToInt32(match4.Groups["secs"].Value)).ToString();
                    //查看楼层
                    string replyurl = null;
                    string replyurlPat=@"<a href=""javascript:;""\s*onclick=""window.open\('(?<url>[\s\S]+?)'\)"" class=""xi2"">查看楼层</a>";
                    Match match2 = Regex.Match(replycontent, replyurlPat);
                    if (match2.Success) replyurl = match2.Groups["url"].Value;
                    if (replyurl != null)
                    {
                        string replyid = replyurl.Substring(replyurl.LastIndexOf('=')+1);
                        string contentHTML = http.Get(replyurl).RawText;
                        contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                        contentHTML = contentHTML.Replace("\r", "");
                        contentHTML = contentHTML.Replace("\n", "");
                        string replyContentPat="<table cellspacing=\"0\" cellpadding=\"0\"><tr><td class=\"t_f\" id=\"postmessage_"+replyid+"\">(?<reply>[\\s\\S]+?)</td></tr></table>";
                        Match match3 = Regex.Match(contentHTML, replyContentPat);
                        if (match3.Success)
                            SQLServerUtil.addReply(match3.Groups["reply"].Value, replytime, replypart, topicid);
                    }
                }
            }
        }
    }
}
