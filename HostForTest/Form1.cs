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

        //private void bt_httpGet_Click(object sender, EventArgs e)
        //{
        //    // [ServiceContract] 

        //    WebClient wc = new WebClient();

        //}
    }


    
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]//返回详细错误信息开启
    public class cl : Icl

    {

        public const string connstr = "Server=124.161.78.133;Port=9620;Database=BackStageSur;Uid=postgres;Pwd=swjtu;";
        public int Login(string p,string pswd)//登录方法
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
                var error = new WCFError("Select", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(error,error.Message);//抛出错误

            }

        }
        public int PingSer(string serid,IPAddress Address,ref long RtT,ref int Ttl,bool DF,ref int BfL)//同步Ping方法
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
            PingReply reply = pingSender.Send(Address, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Address: {0}", reply.Address.ToString());

                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                RtT = reply.RoundtripTime;
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Ttl = reply.Options.Ttl;
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                DF = reply.Options.DontFragment;
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                BfL = reply.Buffer.Length;
#region  向数据库写入数据
                string MetData = "INSERT INTO ser.data(serverid, rtt, ttl, df, bfl, \"time\", )VALUES(@serverid, @rtt, @ttl, @df, @bfl, @time ) ; ";
                Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(MetData, myconnping);
                myconnping.Open();
                try
                {
                    mycommping.Parameters.Add("@serverid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serid;
                    mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                    mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                    mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                    mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                    mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                    int stat = mycommping.ExecuteNonQuery();
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
                return 1;
        }


        public static void PingSerAsync(string[] args, ref long RtT, ref int Ttl, bool DF, ref int BfL)
        {
            if (args.Length == 0)
                throw new ArgumentException("Ping needs a host or IP Address.");

            string who = args[0];
            AutoResetEvent waiter = new AutoResetEvent(false);

            Ping pingSender = new Ping();

            // When the PingCompleted event is raised,
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply.
            int timeout = 12000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            Console.WriteLine("Time to live: {0}", options.Ttl);
            Console.WriteLine("Don't fragment: {0}", options.DontFragment);

            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            pingSender.SendAsync(who, timeout, buffer, options, waiter);

            // Prevent this example application from ending.
            // A real application should do something useful
            // when possible.
            waiter.WaitOne();
            Console.WriteLine("Ping example completed.");
        }

        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set();
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;

            DisplayReply(reply);

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }

        public static void DisplayReply(PingReply reply)
        {
            if (reply == null)
                return;

            Console.WriteLine("ping status: {0}", reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Address: {0}", reply.Address.ToString());
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }
        }
        //public DataSet SeqPingSer (DataSet s)
        //{

        //}
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

        int PingSer(string serid, IPAddress Address, ref long RtT, ref int Ttl, bool DF, ref int BfL);
    }
}
namespace HoraceOriginal
{
    [DataContractAttribute(Namespace ="Horace")]
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