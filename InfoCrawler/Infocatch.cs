using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using szlibInfoThreads;

namespace InfoCrawler
{
    public partial class Infocatch : Form
    {
        private baiduThread m_baiduThread;
        private szlibMeitiThread m_szlibMeitiThread;
        //private hswzThread m_hswzThread;
        private sinablogThread m_sinablogThread;

        public Infocatch()
        {
            InitializeComponent();
            timer1.Enabled = true;

            m_baiduThread = new baiduThread();
            m_baiduThread.Start();

            m_szlibMeitiThread = new szlibMeitiThread();
            m_szlibMeitiThread.Start();

            //m_hswzThread =new hswzThread();
            //m_hswzThread.Start();

            m_sinablogThread = new sinablogThread();
            m_sinablogThread.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //this.Visible = false;
            timer1.Enabled = false;
        }

        private void Infocatch_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_baiduThread.Abort();
            m_szlibMeitiThread.Abort();
            //m_hswzThread.Abort();
            m_sinablogThread.Abort();
        }
    }
}
