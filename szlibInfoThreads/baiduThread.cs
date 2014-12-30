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
    public class baiduThread
    {
        private Thread m_thread;
        private static string m_url = "http://news.baidu.com/ns?word=%CB%D5%D6%DD%CD%BC%CA%E9%B9%DD&bs=%CB%D5%D6%DD%CD%BC%CA%E9%B9%DD&sr=0&cl=2&rn=20&tn=news&ct=0&clk=sortbytime";
        private static string base_url = "http://news.baidu.com";

        public baiduThread()
        {
            m_thread = new Thread(baiduThread.DoWork);
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
            while (true){
                try
                {
                    baiduThread baidu = (baiduThread)data;
                    //获取页面
                    string webcontent = Fetch(m_url);
                    //获取新闻列表
                    List<string> newslist = getNews(webcontent);
                    foreach (string news in newslist)
                    {
                        //MessageBox.Show(news);
                        try
                        {
                            string urlpat = @"<li class=""result"" id="".*""><h3 class=""c-title""><a href[ ]*=[ ]*[""']([^""'#>])+[""']";
                            string newsurl = null;
                            Match match = Regex.Match(news, urlpat);
                            if (match.Success) newsurl = match.Value.Substring(match.Value.LastIndexOf('=') + 1).Trim('"', '\'', '#', ' ', '>');
                            if (newsurl != null)
                            {
                                //MessageBox.Show(newsurl);
                                string newsid = Utility.Hash(newsurl);
                                //如果库中已有该链接，表示已抓取过，后面的不用再抓取
                                if (SQLServerUtil.existNewsId(newsid)) break;
                                //判断标题是否在库中，如已在，更新转载记录
                                string newsitem = news;
                                newsitem = newsitem.Replace("<em>", "").Replace("</em>", "");
                                string titlepat = @"target=""_blank""[ ]*>([^<>])+</a></h3>";
                                Match match2 = Regex.Match(newsitem, titlepat);
                                string newstitle = null;
                                if (match2.Success) newstitle = match2.Value.Substring(match2.Value.IndexOf('>') + 1, match2.Value.IndexOf('<') - match2.Value.IndexOf('>') - 1);

                                string source = null;
                                string time = null;
                                string reprintPat = @"<p class=""c-author"">([^<>])+</p>";
                                Match match3 = Regex.Match(newsitem, reprintPat);
                                string reprintData = null;
                                if (match3.Success) reprintData = match3.Value.Substring(match3.Value.IndexOf('>') + 1, match3.Value.LastIndexOf('<') - match3.Value.IndexOf('>') - 1);
                                Match match4 = Regex.Match(reprintData, @"(\d{4}-\d{2}-\d{2}[ ]*\d{2}:\d{2})");
                                if (match4.Success) time = match4.Value;
                                source = reprintData.Replace(time, "").Replace("&nbsp;", "");
                                time = Regex.Replace(time, "\\s{2,}", " ");

                                if (newstitle != null && SQLServerUtil.existNewsTitle(newstitle) != null)
                                {
                                    SQLServerUtil.updateReprint(SQLServerUtil.existNewsTitle(newstitle), source, time);
                                }
                                //标题不存在，添加到库里
                                else
                                {
                                    string content = Fetch(newsurl);
                                    //MessageBox.Show(content);
                                    SQLServerUtil.addNews(newsid, newstitle, Utility.Encode(content), time, source, newsurl, "百度新闻",null);
                                    SQLServerUtil.updateReprint(newsid, source, time);
                                }
                                //是否有相同新闻转载
                                string reprintsPat = @"<a href[ ]*=[ ]*[""']([^""'#>])+[""'][^<>]*>\d+条相同新闻";
                                Match match6 = Regex.Match(newsitem, reprintsPat);
                                if (match6.Success)
                                {
                                    string tempstr = match6.Value.Substring(match6.Value.IndexOf('"') + 1);
                                    string reprintsUrl = base_url + tempstr.Substring(0, tempstr.IndexOf('"') + 1);
                                    //MessageBox.Show(reprintsUrl);
                                    string reprintsHtml = Fetch(reprintsUrl);
                                    List<string> reprintlist = getReprints(reprintsHtml);
                                    foreach (string reprint in reprintlist)
                                    {
                                        Match match7 = Regex.Match(reprint, @"(\d{4}-\d{2}-\d{2}[ ]*\d{2}:\d{2})");
                                        string reprinttime = null;
                                        if (match7.Success) { reprinttime = match7.Value; }
                                        string reprintsource = reprint.Replace(reprinttime, "").Replace("&nbsp;", "");
                                        reprinttime = Regex.Replace(reprinttime, "\\s{2,}", " ");
                                        SQLServerUtil.updateReprint(newsid, reprintsource, reprinttime);
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

        //获取页面内容
        public static String Fetch(string url)
        {
            string result = null;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 20000;
            req.UserAgent = "szlibInfo";
            req.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse res=null;
            string contenttype = "text/html;   charset=utf-8";
            //Encoding enc = Encoding.UTF8;
            try
            {
                res = (HttpWebResponse)req.GetResponse();
                contenttype = res.Headers["Content-Type"];
                if (res != null)
                {
                    if (contenttype.Contains("gb2312") || contenttype.Contains("GB2312"))
                    {
                        StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("GB2312"));
                        result = sr.ReadToEnd();
                        sr.Close();
                    }
                    else if (contenttype.Contains("gbk") || contenttype.Contains("GBK"))
                    {
                        StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("GBK"));
                        result = sr.ReadToEnd();
                        sr.Close();
                    }
                    else if (contenttype.Contains("utf-8") || contenttype.Contains("UTF-8"))
                    {
                        StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("UTF-8"));
                        result = sr.ReadToEnd();
                        sr.Close();
                    }
                    else
                    {
                        MemoryStream ms = new MemoryStream(ReadInstreamIntoMemory(res.GetResponseStream()));
                        StreamReader sr = new StreamReader(ms, Encoding.GetEncoding("UTF-8"));
                        result = sr.ReadToEnd();
                        Match charSetMatch = Regex.Match(result, "charset=([a-zA-Z0-9\\-]+)", RegexOptions.IgnoreCase);
                        string sChartSet = charSetMatch.Value.Substring(charSetMatch.Value.IndexOf('=') + 1);
                        //MessageBox.Show(sChartSet);
                        if (!string.IsNullOrEmpty(sChartSet) && !sChartSet.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            StreamReader sr1 = new StreamReader(ms, Encoding.GetEncoding(sChartSet));
                            result = sr1.ReadToEnd();
                            sr1.Close();     
                        }
                        sr.Close();
                    }
                    res.Close();
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //MessageBox.Show(e.Message);
            }            
            return result;
        }

        private static byte[] ReadInstreamIntoMemory(Stream stream)
        {
            int bufferSize = 16384;
            byte[] buffer = new byte[bufferSize];
            MemoryStream ms = new MemoryStream();
            while (true)
            {
                int numBytesRead = stream.Read(buffer, 0, bufferSize);
                if (numBytesRead <= 0) break;
                ms.Write(buffer, 0, numBytesRead);
            }
            return ms.ToArray();
        }

        //获取新闻列表
        public static List<string> getNews(string html)
        {
            List<string> newslist = new List<string>();
            try
            {
                string s = html;
                s = Regex.Replace(s, "\\s{3,}", "");
                s = s.Replace("\r", "");
                s = s.Replace("\n", "");
                int ulbegin = s.IndexOf(@"<ul>");
                int ulend = s.IndexOf(@"</ul>");
                if (ulbegin > 0 & ulend > 0)
                {
                    s = s.Substring(ulbegin + 4, ulend - ulbegin - 4);
                    while (s.IndexOf(@"<li") != -1)
                    {
                        int libegin = s.IndexOf(@"<li");
                        int liend = s.IndexOf(@"</li>");
                        newslist.Add(s.Substring(libegin, liend - libegin));
                        s = s.Substring(liend + 5);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return newslist;
        }

        //获取转载列表
        public static List<string> getReprints(string html)
        {
            List<string> reprintlist = new List<string>();
            try
            {
                string pat = @"<p class=""c-author"">([^<>])+</p>";
                MatchCollection mc = Regex.Matches(html, pat);
                foreach (Match match in mc)
                {
                    reprintlist.Add(match.Value.Substring(match.Value.IndexOf('>') + 1, match.Value.LastIndexOf('<') - match.Value.IndexOf('>') - 1));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return reprintlist;
        }
    }
}
