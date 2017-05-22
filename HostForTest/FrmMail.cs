using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackStageSur
{
    public partial class FrmMail : Form
    {
        public FrmMail()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Log.MailFrom = textBox2.Text;
            Log.host = textBox3.Text;
            Log.username = textBox4.Text;
            Log.password = textBox5.Text;
            Log.port= Convert.ToInt32(textBox8.Text);
            Log.ssl = checkBox1.Checked;
            this.Dispose();
        }

        private void FrmMail_Load(object sender, EventArgs e)
        {
            textBox2.Text = Log.MailFrom;
            textBox3.Text = Log.host;
            textBox4.Text = Log.username;
            textBox5.Text = Log.password;
            textBox8.Text = Log.port.ToString();
            checkBox1.Checked = Log.ssl;
        }
    }
}
