using System;
using System.Data;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;





namespace BackStageSur
{
    using HoraceOriginal;//添加引用WCFError错误类
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]//返回详细错误信息开启
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


    }



    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]//返回详细错误信息开启
    public class cl : Icl

    {

        public const string connstr = "Server=124.161.78.133;Port=9620;Database=BackStageSur;Uid=postgres;Pwd=swjtu;";
        public int Login(string p, string pswd)//登录方法
        {
            try
            {
                string sqlstrlgn = "select passwd from ser.tb_login where cleintid='" + p + "'";//选择clientid相对应的MD5
                Npgsql.NpgsqlConnection myconnlgn = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommlgn = new Npgsql.NpgsqlCommand(sqlstrlgn, myconnlgn);

                //Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstr, myconn);
                myconnlgn.Open();
                //DataTable dt = new DataTable();
                //DataSet ds = new DataSet();
                //myda.Fill(dt);
                //ds.Tables.Add(dt);
                string comp = mycommlgn.ExecuteScalar().ToString();  // TODO:test  //MD5赋值给临时变量
                myconnlgn.Close();
                myconnlgn.Dispose();
                if (comp == "" || comp == null)//判断是否有对应MD5
                {
                    return 2;
                }
                else if (comp == pswd)//判断相等
                {
                    return 0;
                }
                else return 1;
            }
            catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
            {
                var nerror = new WCFError("Select", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(nerror, nerror.Message);//抛出错误

            }
            catch (TimeoutException te)//如果数据库未在侦听
            {
                var terror = new WCFError("Select", te.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(terror, terror.Message);//抛出错误
            }

        }


        public DataSet GetServer(string p)//服务器列表方法
        {
            try
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

            catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
            {
                var nerror = new WCFError("Select", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(nerror, nerror.Message);//抛出错误

            }
            catch (TimeoutException te)//如果数据库未在侦听
            {
                var terror = new WCFError("Select", te.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(terror, terror.Message);//抛出错误
            }


        }
        public int PingService(int serviceid, ref long RtT)//同步Ping方法
        {
            #region 从数据库中读取数据
            string dat = "select tb_netboard.netboardid,servicetype,url,port,connstring from tb_netboard inner join tb_service on tb_netboard.netboardid=tb_service.netboardid where tb_service.serviceid=" + serviceid + "";
            Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
            DataTable dt = new DataTable();
            myda.Fill(dt);
            int netboardid = Convert.ToInt16(dt.Rows[0][0]);
            string servicetype = dt.Rows[0][1].ToString().Trim();
            string url = dt.Rows[0][2].ToString().Trim();
            string port = dt.Rows[0][3].ToString().Trim();
            string connstring = dt.Rows[0][4].ToString().Trim();
            #endregion
            if (servicetype == "网络服务器")
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                // 使用默认TTL值128,
                // but change the fragmentation behavior.
                options.DontFragment = true;
                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                IPAddress Address = IPAddress.Parse(url + ":" + port);
                PingReply reply = pingSender.Send(Address, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {

                    RtT = reply.RoundtripTime;

                    int Ttl = reply.Options.Ttl;

                    bool DF = reply.Options.DontFragment;

                    int BfL = reply.Buffer.Length;
                    #region  向数据库写入成功数据
                    string MetData = "INSERT INTO ser.tb_data(netboardid,serviceid,success, rtt, ttl, df, bfl, \"time\" )VALUES(@netboardid,@serviceid, @success, @rtt, @ttl, @df, @bfl, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(MetData, myconnping);
                    myconnping.Open();
                    try
                    {
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
                        mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                        mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                        mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                        mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.ExecuteNonQuery();
                    }
                    catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
                    {
                        var error = new WCFError("Insert", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                        throw new FaultException<WCFError>(error, error.Message);//抛出错误

                    }
                    myconnping.Close();

                    #endregion
                    return 0;
                }
                else
                {
                    #region  向数据库写入失败数据
                    string ErrData = "INSERT INTO ser.tb_error(netboardid,serviceid,success, rtt, ttl, df, bfl, \"time\"，handled )VALUES(@netboardid,@serviceid, @success, @rtt, @ttl, @df, @bfl, @time, @handled); ";
                    string MetData = "INSERT INTO ser.tb_data(netboardid,serviceid,success, rtt, ttl, df, bfl, \"time\")VALUES(@netboardid,@serviceid, @success, @rtt, @ttl, @df, @bfl, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(ErrData, myconnping);
                    myconnping.Open();
                    RtT = 12000;
                    int Ttl = 0;
                    bool DF = false;
                    int BfL = 32;
                    try
                    {
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                        mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                        mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                        mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.ExecuteNonQuery();

                        mycommping.CommandText = MetData;
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                        mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                        mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                        mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.ExecuteNonQuery();
                    }
                    catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
                    {
                        var error = new WCFError("Insert", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                        throw new FaultException<WCFError>(error, error.Message);//抛出错误
                    }
                    myconnping.Close();

                    #endregion
                    return 1;
                }
            }
            else if (servicetype == "数据库服务器")
            {

            }

        }

    }


        [ServiceContract(Namespace = "Horace")]

        public interface Icl
        {

            [OperationContract]

            int Login(string p, string pswd);
            [OperationContract]
            [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
            DataSet GetServer(string p);
            [OperationContract]

            int PingService(int serviceid, ref long RtT);
        }
    

    
}
namespace HoraceOriginal
{
    [DataContractAttribute(Namespace = "Horace")]
    public class WCFError
    {
        public WCFError(string operation, string message)
        {
            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentNullException("operation");
            }
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            Operation = operation;
            this.Message = message;
        }
        [DataMember]
        public string Operation
        { get; set; }
        [DataMember]
        public string Message
        { get; set; }
    }

}
namespace UDP_Server
{
    class Program
    {
        static Socket server;
        static void Main(string[] args)
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Console.WriteLine("服务端已经开启");

            Thread t2 = new Thread(sendMsg);//开启发送消息线程
            t2.Start();


        }
        /// <summary>
        /// 向特定ip的主机的端口发送数据报
        /// </summary>
        static void sendMsg()
        {
            EndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6000);
            while (true)
            {
                string msg = Console.ReadLine();
                server.SendTo(Encoding.UTF8.GetBytes(msg), point);
            }


        }



    }
}