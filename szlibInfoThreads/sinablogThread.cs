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
    public class sinablogThread
    {
        private Thread m_thread;
        private static string m_url = "http://search.sina.com.cn/?by=all&q=%CB%D5%D6%DD%CD%BC%CA%E9%B9%DD&c=blog&range=article";

        public sinablogThread()
        {
            m_thread = new Thread(sinablogThread.DoWork);
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
                    //获取博客列表
                    List<string> blogList = getBlogs(webcontent);
                    foreach (string blog in blogList)
                    {
                        try
                        {
                            string blogurl = null;
                            string urlpat = @"<div class=""box-result clearfix"" data-sudaclick=""blk_blog_\d+_index"" *>\s*<h2 class=[""']r-info-blog-tit[""']>\s*<a href=""(?<url>[^""'#>]+)"" target=""_blank"">";
                            Match match = Regex.Match(blog,urlpat);
                            if (match.Success) blogurl = match.Groups["url"].Value;
                            //MessageBox.Show(blogurl);
                            if (blogurl != null)
                            {
                                string blogid = Utility.Hash(blogurl);
                                //如果库中已有该链接，表示已抓取过，后面的不用再抓取
                                if (SQLServerUtil.existNewsId(blogid)) break;
                                //没有就保存
                                string contentHTML = baiduThread.Fetch(blogurl);
                                contentHTML = Regex.Replace(contentHTML, "\\s{3,}", "");
                                contentHTML = contentHTML.Replace("\r", "");
                                contentHTML = contentHTML.Replace("\n", "");
                                string title=null;
                                string source=null;
                                string titleAndSource = @"<title>(?<str>[\s\S]+?)</title>";
                                Match match1 = Regex.Match(contentHTML, titleAndSource);
                                if(match1.Success)
                                {
                                    string temp = match1.Groups["str"].Value;
                                    temp = temp.Substring(0, temp.LastIndexOf('_')-1);
                                    source = temp.Substring(temp.LastIndexOf('_') + 1);
                                    title = temp.Substring(0, temp.LastIndexOf('_') - 1);
                                }
                                string time=null;
                                string timepat = @"<span class=""time SG_txtc"">(?<time>[\s\S]+?)</span>";
                                Match match2 = Regex.Match(contentHTML, timepat);
                                if (match2.Success)
                                {
                                    time = match2.Groups["time"].Value.Trim('(', ')');
                                    time = Regex.Replace(time, "\\s{2,}", " ");
                                }
                                string content = null;
                                string contentPat = @"<!-- *正文开始 *-->(?<content>[\s\S]+?)<!-- *正文结束 *-->";
                                Match match3 = Regex.Match(contentHTML, contentPat);
                                if (match3.Success)
                                {
                                    content = match3.Groups["content"].Value;
                                    string imgPat = @"<img[\s\S]*?real_src[ ]*=[""'](?<img>[^""'>#]+?)[""'][\s\S]*?/>";
                                    MatchCollection mc = Regex.Matches(content, imgPat);
                                    if (mc != null && mc.Count > 0)
                                    {
                                        foreach (Match m in mc)
                                        {
                                            string imgurl = m.Groups["img"].Value;
                                            string imgname = imgurl.Substring(imgurl.LastIndexOf('/')+1);
                                            saveImage.saveImageToFile(imgurl);
                                            SQLServerUtil.addImage(imgname,blogid);
                                        }
                                    }
                                    content = Regex.Replace(content,@"<br>|<br */>","\n");
                                    content = Regex.Replace(content, @"</p>|</P>", "\n");
                                    content = Regex.Replace(content, @"<[^<>]+?>", "");
                                }
                                SQLServerUtil.addNews(blogid, title, Utility.Encode(content), time, source, blogurl, "新浪博客", null);
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

        //获取博客列表
        private static List<string> getBlogs(string html)
        {
            List<string> bloglist=new List<string>();
            string s = html;
            s = Regex.Replace(s, "\\s{3,}", "");
            s = s.Replace("\r", "");
            s = s.Replace("\n", "");
            string pat = @"<!-- *0422 *博主新模板 *-->[\s\S]+?<!-- *0422 *博主新模板 *-->";
            MatchCollection mc = Regex.Matches(s, pat);
            foreach (Match m in mc)
            {
                bloglist.Add(m.Value);
            }
            return bloglist;
        }
    }
}
