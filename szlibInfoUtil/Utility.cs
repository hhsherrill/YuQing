using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;
using System.Web;

namespace szlibInfoUtil
{
    public class Utility
    {
        private static string PREFIX = @"\u";

        public static string Hash(string url)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.UTF8.GetBytes(url);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }

            return s.ToString();
        }

        /// <summary>
        /// 爬虫需要两个URL是否指向相同的页面这一点可以被迅速检测出来, 这就需要URL规范化.
        /// URL规范化做的主要的事情:
        /// 转换为小写
        /// 相对URL转换成绝对URL
        /// 删除默认端口号
        /// 根目录添加斜杠
        /// 猜测的目录添加尾部斜杠
        /// 删除分块
        /// 解析路径
        /// 删除缺省名字
        /// 解码禁用字符
        /// 
        /// 更多信息参照RFC3986:
        /// http://tools.ietf.org/html/rfc3986
        /// </summary>
        /// <param name="strURL"></param>
        public static void Normalize(string baseUri, ref string strUri)
        {
            // 相对URL转换成绝对URL
            if (strUri.StartsWith("/"))
            {
                strUri = baseUri + strUri.Substring(1);
            }

            // 当查询字符串为空时去掉问号"?"
            if (strUri.EndsWith("?"))
                strUri = strUri.Substring(0, strUri.Length - 1);

            // 转换为小写
            strUri = strUri.ToLower();

            // 删除默认端口号
            // 解析路径
            // 解码转义字符
            Uri tempUri = new Uri(strUri);
            strUri = tempUri.ToString();

            // 根目录添加斜杠
            int posTailingSlash = strUri.IndexOf("/", 8);
            if (posTailingSlash == -1)
                strUri += '/';

            // 猜测的目录添加尾部斜杠
            if (posTailingSlash != -1 && !strUri.EndsWith("/") && strUri.IndexOf(".", posTailingSlash) == -1)
                strUri += '/';

            // 删除分块
            int posFragment = strUri.IndexOf("#");
            if (posFragment != -1)
            {
                strUri = strUri.Substring(0, posFragment);
            }

            // 删除缺省名字
            string[] DefaultDirectoryIndexes = 
            {
                "index.html",
                "default.asp",
                "default.aspx",
            };
            foreach (string index in DefaultDirectoryIndexes)
            {
                if (strUri.EndsWith(index))
                {
                    strUri = strUri.Substring(0, (strUri.Length - index.Length));
                    break;
                }
            }


        }

        public static void Normalize(ref string strUri)
        {
            Normalize(string.Empty, ref strUri);
        }

        public static string GetBaseUri(string strUri)
        {
            string baseUri;
            Uri uri = new Uri(strUri);
            string port = string.Empty;
            if (!uri.IsDefaultPort)
                port = ":" + uri.Port;
            baseUri = uri.Scheme + "://" + uri.Host + port + "/";

            return baseUri;

        }

        //插入数据库时替换字符
        public static string Encode(string str)
        {
            str = str.Replace("'", "''");
            str = str.Replace("\"", "&quot;");
            str = str.Replace("<", "&lt;");
            str = str.Replace(">", "&gt;");
            str = str.Replace("\n", "<br>");
            str = str.Replace("“", "&ldquo;");
            str = str.Replace("”", "&rdquo;");
            return str;
        }

        public static String ascii2Native(string str)
        {
            
            StringBuilder sb = new StringBuilder();
            int begin = 0;
            int index = str.IndexOf(PREFIX);
            while (index != -1)
            {
                sb.Append(str.Substring(begin, index-begin));
                sb.Append(ascii2Char(str.Substring(index, 6)));
                begin = index + 6;
                index = str.IndexOf(PREFIX, begin);
            }
            sb.Append(str.Substring(begin));
            return sb.ToString();
        }

        private static char ascii2Char(string str)
        {
            if (str.Length != 6)
            {
                throw new Exception("Ascii string of a native character must be 6 character.");
            }
            if (!PREFIX.Equals(str.Substring(0, 2)))
            {
                throw new Exception("Ascii string of a native character must start with \"\\u\".");
            }
            string tmp = str.Substring(2, 2);
            int code = int.Parse(tmp,NumberStyles.AllowHexSpecifier)<<8;
            tmp = str.Substring(4, 2);
            code += int.Parse(tmp, NumberStyles.AllowHexSpecifier);
            return (char)code;
        }

        
    }
}
