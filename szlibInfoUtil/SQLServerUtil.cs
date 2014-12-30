using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace szlibInfoUtil
{
    public class SQLServerUtil
    {
        private static string strconn = "user id=sa; password=1q2w3esqlserver; data source=192.168.0.207; Integrated security=False; initial catalog=szlibInfo";

        //是否存在记录
        public static Boolean existNewsId(string newsid)
        {
            Boolean result = false;
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "select * from newsInfo where infoID='" + newsid + "'";
                SqlCommand cmd = new SqlCommand(s, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read()) result = true;
                dr.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        //查重，如果存在相同标题的新闻，返回此前存储的新闻id
        public static string existNewsTitle(string newstitle)
        {
            string result = null;
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "select * from newsInfo where title='" + newstitle + "'";
                SqlCommand cmd = new SqlCommand(s, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read()) result = dr.GetValue(0).ToString();
                dr.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        //更新转载记录
        public static void updateReprint(string newsid, string source, string time)
        {
            try
            {
                if (!exsitReprint(newsid, source))
                {
                    SqlConnection conn = new SqlConnection(strconn);
                    conn.Open();
                    string s = "insert into newsReprint (reprintSource,infoID,reprintTime) values('" + source + "','" + newsid + "','" + time + "')";
                    SqlCommand cmd = new SqlCommand(s, conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //是否存在转载记录
        public static Boolean exsitReprint(string newsid, string source)
        {
            Boolean result = false;
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "select * from newsReprint where infoID='" + newsid + "' and reprintSource='" + source + "'";
                SqlCommand cmd = new SqlCommand(s, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read()) result = true;
                dr.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        //添加主题
        public static void addNews(string newsid, string newstitle, string content, string time, string source,string url,string category,string status)
        {
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "insert into newsInfo (infoID,title,context,time,source,url,category,status) values('" + newsid + "','" + newstitle + "','" + content + "','" + time + "','" + source + "','"+url+"','"+category+"','"+status+"')";
                SqlCommand cmd = new SqlCommand(s, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //寒山闻钟获取状态（已关注、已处理）
        public static string getStatus(string topicid)
        {
            string status = null;
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "select status from newsInfo where infoID='" + topicid + "'";
                SqlCommand cmd = new SqlCommand(s, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read()) status = dr.GetValue(0).ToString();
                dr.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return status;
        }

        //寒山闻钟更新状态
        public static void updateStatus(string topicid, string status)
        {
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s="update newsInfo set status='"+status+"' where infoID='"+topicid+"'";
                SqlCommand cmd = new SqlCommand(s, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //寒山闻钟保存回复
        public static void addReply(string replycontent, string replytime,string replydepart, string topicid)
        {
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "insert into reply (replycontent,replytime,replydepart,infoID) values('" + replycontent + "','" + replytime + "','"+replydepart+"','" + topicid + "')";
                SqlCommand cmd = new SqlCommand(s, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //添加图片
        public static void addImage(string imgname, string infoid)
        {
            try
            {
                SqlConnection conn = new SqlConnection(strconn);
                conn.Open();
                string s = "insert into newsPicture (name,infoID) values('" + imgname + "','" + infoid + "')";
                SqlCommand cmd = new SqlCommand(s, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
