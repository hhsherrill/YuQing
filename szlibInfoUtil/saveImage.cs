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
    public class saveImage
    {
        private static string folder;

        static saveImage()
        {
            folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"images");
        }

        public static void saveImageToFile(string imageurl)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string filename = imageurl.Substring(imageurl.LastIndexOf('/') + 1);
            WebRequest req = WebRequest.Create(imageurl);
            WebResponse res = req.GetResponse();
            Stream imgstream = res.GetResponseStream();
            Image img = Image.FromStream(imgstream);
            img.Save(Path.Combine(folder, filename));
        }
    }
}
