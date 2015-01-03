using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace szlibInfoUtil
{
    public class getWebContent
    {
        //获取页面内容
        public static String Fetch(string url)
        {
            string result = null;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 20000;
            req.UserAgent = "szlibInfo";
            req.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse res = null;
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

        public static byte[] ReadInstreamIntoMemory(Stream stream)
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
    }
}
