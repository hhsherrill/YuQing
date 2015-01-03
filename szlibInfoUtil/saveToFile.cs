using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Net;
using System.Drawing;

namespace szlibInfoUtil
{
    public class saveToFile
    {
        private static string imagefolder;
        private static string webfolder;

        static saveToFile()
        {
            imagefolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"images");
            webfolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "webs");
        }

        public static void saveImageToFile(string imageurl)
        {
            if (!Directory.Exists(imagefolder)) Directory.CreateDirectory(imagefolder);
            string filename = imageurl.Substring(imageurl.LastIndexOf('/') + 1);
            if (!File.Exists(Path.Combine(imagefolder, filename)))
            {
                WebRequest req = WebRequest.Create(imageurl);
                WebResponse res = req.GetResponse();
                Stream imgstream = res.GetResponseStream();
                Image img = Image.FromStream(imgstream);
                img.Save(Path.Combine(imagefolder, filename));
            }            
        }

        public static string saveWebToFile(string weburl)
        {
            if (!Directory.Exists(webfolder)) Directory.CreateDirectory(webfolder);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(weburl);
            req.Timeout = 20000;
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            string contentType = res.ContentType;
            if (contentType != "text/html") return null;
            byte[] buffer = getWebContent.ReadInstreamIntoMemory(res.GetResponseStream());
            res.Close();
            string extension = GetExtensionByMimeType(contentType);
            string filename = Path.Combine(webfolder, Utility.Hash(weburl) + "." + extension);
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            fs.Write(buffer, 0, buffer.Length);
            fs.Close();
            return Utility.Hash(weburl) + "." + extension;
        }

        public static string GetExtensionByMimeType(string mimeType)
        {
            int pos;
            if ((pos = mimeType.IndexOf('/')) != -1)
            {
                return mimeType.Substring(pos + 1);
            }
            return string.Empty;
        }
    }
}
