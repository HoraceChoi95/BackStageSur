using System;
using System.Data;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Windows.Forms;




namespace BackStageSur
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private ServiceHost host;

        private void button1_Click(object sender, EventArgs e)
        {
           
            Uri baseAddress = new Uri("http://127.0.0.1:9999/");
            this.host = new ServiceHost(typeof(cl), baseAddress);
            host.AddServiceEndpoint(typeof(Icl), new WSHttpBinding(), "cl");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.HttpGetUrl = new Uri("http://127.0.0.1:9999/cl/metadata");
            host.Description.Behaviors.Add(smb);
            host.Opened += delegate { MessageBox.Show("服务已经启动！"); };
            host.Open();
            this.bt_Ini.Enabled = false;
            this.bt_Stop.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            host.Closed += delegate { MessageBox.Show("服务已经停止！"); };
            this.host.Close();
            this.bt_Stop.Enabled = false;
            this.bt_Ini.Enabled = true;

        }

        //private void bt_httpGet_Click(object sender, EventArgs e)
        //{
        //    // [ServiceContract] 

        //    WebClient wc = new WebClient();

        //}
    }

    
    public class cl : Icl
    {
        public const string connstr = "Server=127.0.0.1;Port=5432;Database=BackStageSur;Uid=postgres;Pwd=4770;";
        public int Login(string p,string pswd)
        {
            string sqlstrlgn = "select passwd from ser.tb_login where cleintid='" + p + "'";
            Npgsql.NpgsqlConnection myconnlgn = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommlgn = new Npgsql.NpgsqlCommand(sqlstrlgn, myconnlgn);

            //Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstr, myconn);
            myconnlgn.Open();
            //DataTable dt = new DataTable();
            //DataSet ds = new DataSet();
            //myda.Fill(dt);
            //ds.Tables.Add(dt);
            string comp = mycommlgn.ExecuteScalar().ToString();
            myconnlgn.Close();
            myconnlgn.Dispose();
            if (comp == "" || comp == null)
            {
                return 2;
            }
            else if (comp == pswd)
            {
                return 0;
            }
            else return 1;
            
        }
       

        public DataSet GetServer(string p)
        {
            string sqlstrGtSer = "select serverid,name,type,url from ser.tb_server where cleintid='" + p + "'";
            Npgsql.NpgsqlConnection myconnGtSer = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrGtSer, myconnGtSer);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrGtSer, myconnGtSer);
            myconnGtSer.Open();
            DataTable dtGtSer = new DataTable();
            DataSet dsGtSer = new DataSet();
            myda.Fill(dtGtSer);
            dsGtSer.Tables.Add(dtGtSer);
            return dsGtSer;
        }
        
    }


    [ServiceContract]
    public interface Icl
    {
        [OperationContract]
        
        int Login(string p, string pswd);
        DataSet GetServer(string p);
    }
}
