using OLDZHANG.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace ClientHuiKe
{

    public class Client
    {
        private Thread m_thread;

        HttpHelper http = new HttpHelper();

        public Client()
        {
            m_thread = new Thread(DoWork);
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

            try
            {
                //获取信息列表
                List<string> topiclist = getTopics();
                /*
                foreach (string topic in topiclist)
                {
                    try
                    {
                        string topicurl = null;
                        string pat = @"<h3 class=""xs3""><a href[ ]*=[ ]*[""']([^""'#>])+[""']";
                        Match match = Regex.Match(topic, pat);
                        if (match.Success)
                        {
                            string temp = match.Value.Substring(match.Value.IndexOf(">") + 1);
                            topicurl = temp.Substring(temp.IndexOf('=') + 1).Trim('"', '\'', '#', ' ', '>').Replace("&amp;", "&");
                        }
                        if (topicurl != null)
                        {
                            string topicid = Utility.Hash(topicurl);
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
                                string contentHTML = http.Get(topicurl).RawText;
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
                Thread.Sleep(3 * 60 * 60 * 1000);//每隔3小时执行一次*/
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

        //获取主题列表
        private List<string> getTopics()
        {
            DateTime today = DateTime.Today;
            string year = today.ToString("yyyy");
            string month = today.ToString("MM");
            string day = today.ToString("dd");
            List<string> topiclist = new List<string>();
            String[] webcontent = new String[2];
            
            http.CookieContainer.Add(new Uri("http://wisenews.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            http.CookieContainer.Add(new Uri("http://wisesearch.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            http.CookieContainer.Add(new Uri("http://cn.wisesearch.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            
            http.GetHtml("http://wisenews.wisers.net/?gid=SZLIBCN&user=ipaccess");
            http.GetHtml("http://wisesearch.wisers.net/");
            string postData1 = string.Format("defaultTemplate=&searchQueryFormUpdated=true&type=&template-name=&curr-template-name=&content-locale=zh_CN&search-action=&sort-order-list=DESC%3Adate&from-date={1}{2}{3}&to-date={1}{2}{3}&date-range=PUB%3A{1}.{2}.{3}-{1}.{2}.{3}&included-publication-uids=&excluded-publication-uids=&included-listedcompany-uids=&excluded-listedcompany-uids=&included-adind-uids=&excluded-adind-uids=&included-adbrand-uids=&excluded-adbrand-uids=&hot-picks=&search_doc_type=news-only&search-query-string={0}&use-thesaurus=true&by-scope=headline%2Bcontent&within-scope=document&date-range-from-year={1}&date-range-from-month={2}&date-range-from-day={3}&date-range-to-year={1}&date-range-to-month={2}&date-range-to-day={3}&date-range-period=today&search_region=&search_source=&search_type=&select_pub_name=&delete_pub_name=&search_source_input=%C7%EB%CA%E4%C8%EB%C3%BD%CC%E5%C3%FB%D7%D6%A3%AC%C0%FD%C8%E7%A3%BA%C4%CF%B7%BD%B6%BC%CA%D0%B1%A8&template-name-new-radio=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+2&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+3&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+4&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+5&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+6&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+7&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+8&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+9&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+10&language=all&author_name=&column_name=&section_name=&listedcompany-include-op=or", HttpUtility.UrlEncode("苏州图书馆", Encoding.GetEncoding("gbk")),year,month,day);
            http.Post("http://cn.wisesearch.wisers.net/cnws/search.do", postData1);
            webcontent[0] = http.GetHtml("http://cn.wisesearch.wisers.net/cnws/FULL-CONTENT-DOCUMENT-BODY.do?is_expo=&adhoc-clip-folder-id=search-result&narrow-publication-scope=default");
            //response = http.Get("http://wisenews.wisers.net/wisenews/index.do?new-login=true");

            //response = http.Get("http://wisesearch.wisers.net/");
            //response = http.Get("http://cn.wisesearch.wisers.net/");

            //response = http.Get("http://cn.wisesearch.wisers.net/cnws/index.do?srp_restore=discard&new-login=true");
            //response = http.Post("http://cn.wisesearch.wisers.net/cnws/search.do", "defaultTemplate=&searchQueryFormUpdated=true&type=&template-name=&curr-template-name=&content-locale=zh_CN&search-action=&sort-order-list=DESC%3Adate&from-date=20141231&to-date=20141231&date-range=PUB%3A2014.12.31-2014.12.31&included-publication-uids=&excluded-publication-uids=&included-listedcompany-uids=&excluded-listedcompany-uids=&included-adind-uids=&excluded-adind-uids=&included-adbrand-uids=&excluded-adbrand-uids=&hot-picks=&search_doc_type=news-only&search-query-string=%CB%D5%D6%DD%CD%BC%CA%E9%B9%DD&use-thesaurus=true&by-scope=headline%2Bcontent&within-scope=document&date-range-from-year=2014&date-range-from-month=12&date-range-from-day=31&date-range-to-year=2014&date-range-to-month=12&date-range-to-day=31&date-range-period=today&search_region=&search_source=&search_type=&select_pub_name=&delete_pub_name=&search_source_input=%C7%EB%CA%E4%C8%EB%C3%BD%CC%E5%C3%FB%D7%D6%A3%AC%C0%FD%C8%E7%A3%BA%C4%CF%B7%BD%B6%BC%CA%D0%B1%A8&template-name-new-radio=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+2&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+3&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+4&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+5&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+6&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+7&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+8&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+9&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+10&language=all&author_name=&column_name=&section_name=&listedcompany-include-op=or", HttpContentTypes.ApplicationXWwwFormUrlEncoded);

            /*if (response.ContentLength < 1000)
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
            }*/
            return topiclist;
        }
        
    }
}
