using OLDZHANG.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using szlibInfoUtil;

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
            http.AcceptLanguage = "zh-Hans-CN,zh-Hans;q=0.5";
            http.Accept = "text/html, application/xhtml+xml, */*";
            http.Encoding = System.Text.Encoding.GetEncoding("GBK"); 
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
                foreach (string topic in topiclist)
                {
                    try
                    {
                        string topicurl = null;
                        string urlpat = @"<td class=""greytext""><a href='javascript:OpenDocuments\(""(?<url>[^<>'""#]+?)""";
                        Match urlmatch=Regex.Match(topic,urlpat);
                        if (urlmatch.Success)
                        {
                            topicurl = "http://cn.wisesearch.wisers.net/cnws" + urlmatch.Groups["url"].Value;
                            string topicid = Utility.Hash(topicurl);
                            //如果库中已有该链接，表示已抓取过，后面的不用再抓取
                            if (SQLServerUtil.existNewsId(topicid)) break;
                            //没有就保存
                            string title = null;
                            string titlepat = @"document.write\(""(?<title>[^<>'""]+?)""";
                            Match titlematch = Regex.Match(topic, titlepat);
                            if(titlematch.Success) title=titlematch.Groups["title"].Value.Replace("&amp;","&");
                            MessageBox.Show(title);
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

        //获取主题列表
        private List<string> getTopics()
        {
            DateTime today = DateTime.Today;
            string year = today.ToString("yyyy");
            string month = today.ToString("MM");
            string day = today.ToString("dd");
            List<string> topiclist = new List<string>();
            String webcontent;
            
            http.CookieContainer.Add(new Uri("http://wisenews.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            http.CookieContainer.Add(new Uri("http://wisesearch.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            http.CookieContainer.Add(new Uri("http://cn.wisesearch.wisers.net"), new Cookie("cUSERNAME", "\"SZLIBCN@ipaccess\""));
            
            http.GetHtml("http://wisenews.wisers.net/?gid=SZLIBCN&user=ipaccess");
            http.GetHtml("http://wisesearch.wisers.net/");
            string postData1 = string.Format("defaultTemplate=&searchQueryFormUpdated=true&type=&template-name=&curr-template-name=&content-locale=zh_CN&search-action=&sort-order-list=DESC%3Adate&from-date={1}{2}{3}&to-date={1}{2}{3}&date-range=PUB%3A{1}.{2}.{3}-{1}.{2}.{3}&included-publication-uids=&excluded-publication-uids=&included-listedcompany-uids=&excluded-listedcompany-uids=&included-adind-uids=&excluded-adind-uids=&included-adbrand-uids=&excluded-adbrand-uids=&hot-picks=&search_doc_type=news-only&search-query-string={0}&use-thesaurus=true&by-scope=headline%2Bcontent&within-scope=document&date-range-from-year={1}&date-range-from-month={2}&date-range-from-day={3}&date-range-to-year={1}&date-range-to-month={2}&date-range-to-day={3}&date-range-period=today&search_region=&search_source=&search_type=&select_pub_name=&delete_pub_name=&search_source_input=%C7%EB%CA%E4%C8%EB%C3%BD%CC%E5%C3%FB%D7%D6%A3%AC%C0%FD%C8%E7%A3%BA%C4%CF%B7%BD%B6%BC%CA%D0%B1%A8&template-name-new-radio=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+1&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+2&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+3&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+4&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+5&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+6&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+7&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+8&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+9&template-name-new=%CB%D1%CB%F7%CF%EE%C4%BF+10&language=all&author_name=&column_name=&section_name=&listedcompany-include-op=or", HttpUtility.UrlEncode("苏州图书馆", Encoding.GetEncoding("gbk")),year,month,day);
            http.Post("http://cn.wisesearch.wisers.net/cnws/search.do", postData1);
            webcontent = http.GetHtml("http://cn.wisesearch.wisers.net/cnws/FULL-CONTENT-DOCUMENT-BODY.do?is_expo=&adhoc-clip-folder-id=search-result&narrow-publication-scope=default");
            
            webcontent = Regex.Replace(webcontent, "\\s{3,}", "");
            webcontent = webcontent.Replace("\r", "");
            webcontent = webcontent.Replace("\n", "");
            //MessageBox.Show(webcontent);
            Console.Write(webcontent);
            string pat = @"<tr valign='top' onMouseOut=""mouseout\(this\);"" *>(?<topic>[\s\S]+?)</tr>";
            MatchCollection mc = Regex.Matches(webcontent, pat);
            foreach (Match m in mc)
            {
                topiclist.Add(m.Groups["topic"].Value);
            }
            return topiclist;
        }
        
    }
}
