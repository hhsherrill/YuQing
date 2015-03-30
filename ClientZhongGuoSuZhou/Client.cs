using OLDZHANG.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;
using System.Text.RegularExpressions;
using szlibInfoUtil;
using System.Windows.Forms;

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
                            string topicurl = topic.Substring(topic.IndexOf('=') + 1, topic.IndexOf("target=_blank") - topic.IndexOf('=') - 1);
                            topicurl = topicurl.Replace(" ", "");
                            if (topicurl != null)
                            {
                                string topicid = Utility.Hash(topicurl);
                                //如果库中已有该链接，表示已抓取过，跳过
                                if (SQLServerUtil.existNewsId(topicid)) continue;
                                string topictitle = topic.Substring(topic.IndexOf('>') + 1, topic.IndexOf("</a>") - topic.IndexOf('>') - 1);
                                //搜索结果存在相同文章，只保存一次
                                if (topictitle != null && SQLServerUtil.existNewsTitle(topictitle, "中国苏州网") != null) continue;
                                string time = topic.Substring(topic.IndexOf("</a>")+4).Replace(" ","").Trim('[', ']');
                                string contentHTML = http.GetHtml(topicurl);
                                //MessageBox.Show(contentHTML);                               
                                if (contentHTML != null)
                                {
                                    contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                    contentHTML = contentHTML.Replace("\r", "");
                                    contentHTML = contentHTML.Replace("\n", "");
                                    string source = null;//来源
                                    string sourcePat = @"<div class=""Page-content-right-item"" id=""text-content""><dl><dt>[\s\S]+?</dt><dd>(?<source>[\s\S]+?)</dd></dl>";
                                    Match match = Regex.Match(contentHTML, sourcePat);
                                    if (match.Success) source = match.Groups["source"].Value.Substring(match.Groups["source"].Value.LastIndexOf('：') + 1);
                                    //另一种格式
                                    else
                                    {
                                        sourcePat = @"<div class=""con2 clearfix""><h1>[\s\S]+?</h1><h4>(?<source>[\s\S]+?)</h4>";
                                        match = Regex.Match(contentHTML, sourcePat);
                                        if (match.Success) source = match.Groups["source"].Value.Substring(match.Groups["source"].Value.LastIndexOf('：') + 1);
                                    }
                                    string content = null; //内容
                                    string contentPat = @"<div class=""wen""><div class=TRS_Editor>(?<content>[\s\S]+?)</div><br/></div>";
                                    Match match2 = Regex.Match(contentHTML, contentPat);
                                    if (match2.Success)
                                    {
                                        content = match2.Groups["content"].Value;
                                        MatchCollection mc = Regex.Matches(content, @"<[IMG|img][\s\S]+?src=""(?<img>[^""'<>#]+)""[\s\S]+?/>");
                                        if (mc != null && mc.Count > 0)
                                        {
                                            foreach (Match m in mc)
                                            {
                                                string imgurl = topicurl.Substring(0, topicurl.LastIndexOf('/')) + m.Groups["img"].Value.Substring(1);
                                                content = content.Replace(m.Groups["img"].Value,imgurl);
                                                /*string imgname = Utility.Hash(imgurl) + ".jpg";
                                                saveToFile.saveImageToFile(imgurl);
                                                SQLServerUtil.addImage(imgname, topicurl);*/
                                            }
                                        }
                                        /*content = Regex.Replace(content, @"<[br|BR][\s\S]*?>", "\n");
                                        content = Regex.Replace(content, @"</p>|</P>", "\n");
                                        content = Regex.Replace(content, @"<[^<>]+?>", "");*/
                                    }
                                    //另一种格式
                                    else
                                    {
                                        contentPat = @"<div class=""cbank clearfix"">(?<content>[\s\S]+?)</div><div class=""bbank"">";
                                        match2 = Regex.Match(contentHTML, contentPat);
                                        if (match2.Success)
                                        {
                                            content = match2.Groups["content"].Value;
                                            MatchCollection mc = Regex.Matches(content, @"<[IMG|img][\s\S]+?src=""(?<img>[^""'<>#]+)""[\s\S]+?/>");
                                            if (mc != null && mc.Count > 0)
                                            {
                                                foreach (Match m in mc)
                                                {
                                                    string imgurl = topicurl.Substring(0, topicurl.LastIndexOf('/')) + m.Groups["img"].Value.Substring(1);
                                                    content = content.Replace(m.Groups["img"].Value,imgurl);
                                                    /*string imgname = Utility.Hash(imgurl) + ".jpg";
                                                    saveToFile.saveImageToFile(imgurl);
                                                    SQLServerUtil.addImage(imgname, topicurl);*/
                                                }
                                            }
                                            /*content = Regex.Replace(content, @"</div>|</DIV>", "\n");
                                            content = Regex.Replace(content, @"<[^<>]+?>", "");*/
                                        }
                                    }
                                    //MessageBox.Show(source);
                                    //MessageBox.Show(content);
                                    SQLServerUtil.addNews(topicid, topictitle, Utility.Encode(content), time, source, topicurl, "中国苏州网", null, DateTime.Now.ToString(), DateTime.Now.ToString());
                                }                             
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(this.ToString() + e.Message);
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
                    Console.WriteLine(this.ToString() + e.Message);
                    Thread.Sleep(5 * 1000);
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
            webcontent[1] = http.Post(base_url + "/asite/search.asp", postData2);

            foreach (string content in webcontent)
            {
                string html = Regex.Replace(content, "\\s{3,}", "");
                html = html.Replace("\r", "");
                html = html.Replace("\n", "");
                Match match = Regex.Match(html, @"<div class=""Page-content-right-class"">[\s\S]+?</div>");
                if (match.Success) html = match.Value;
                string pat = @"<li>(?<topic>[\s\S]+?)</li>";
                MatchCollection mc = Regex.Matches(html, pat);
                foreach (Match m in mc)
                {
                    topiclist.Add(m.Groups["topic"].Value);
                }
            }
            return topiclist;
        }
    }
}
