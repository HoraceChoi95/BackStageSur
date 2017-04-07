using System;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Windows.Forms;






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



    [ServiceBehavior(IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]//返回详细错误信息开启,开启多线程


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


        public DataSet Intialize(string p)//服务器列表方法
        {
            try
            {
                string s = p;
                string sqlstrGtSrvr = "select * from sur.tb_server where tb_server.clientid='" + s + "'";
                string sqlstrGtNetbd = "select netboardid,tb_netboard.serverid,url from tb_netboard inner join tb_server on tb_netboard.serverid=tb_server.serverid where tb_server.clientid='" + s + "'";
                string sqlstrGtSrvis = "select serviceid,tb_service.serverid,servicetype,servicename,netboardid,port from tb_service inner join tb_server on tb_service.serverid=tb_server.serverid where tb_server.clientid='" + s + "'";
                Npgsql.NpgsqlConnection myconnInit = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrGtSrvr, myconnInit);
                Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrGtSrvr, myconnInit);
                myconnInit.Open();
                DataTable dtGtSer = new DataTable("Server");
                DataSet dsInit = new DataSet("Intialize");
                myda.Fill(dtGtSer);
                mycommGtSer.CommandText = sqlstrGtNetbd;
                myda.SelectCommand.CommandText = sqlstrGtNetbd;
                DataTable dtGtNetbd = new DataTable("Netboard");
                myda.Fill(dtGtNetbd);
                DataTable dtGtNetbd2 = new DataTable("Netboard");
                dtGtNetbd2 = ChangeColumnType(dtGtNetbd);
                mycommGtSer.CommandText = sqlstrGtSrvis;
                myda.SelectCommand.CommandText = sqlstrGtSrvis;
                DataTable dtGtSrvis = new DataTable("Service");
                myda.Fill(dtGtSrvis);
                dsInit.Tables.Add(dtGtSer);
                dsInit.Tables.Add(dtGtNetbd2);
                dsInit.Tables.Add(dtGtSrvis);
                myconnInit.Close();
                myconnInit.Dispose();
                mycommGtSer.Dispose();
                return dsInit;
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
        public DataTable ChangeColumnType(DataTable dt)
        {

            DataTable tempdt = new DataTable();
            for (int i = 0; i < 3; i++)
            {
                if (i != 2)
                {
                    DataColumn tempdc = new DataColumn();
                    tempdc.ColumnName = dt.Columns[i].ColumnName;
                    tempdc.DataType = dt.Columns[i].DataType;
                    tempdt.Columns.Add(tempdc);
                }
                else
                {
                    DataColumn tempdc = new DataColumn();
                    tempdc.ColumnName = dt.Columns[i].ColumnName;
                    tempdc.DataType = typeof(String);
                    tempdt.Columns.Add(tempdc);
                }

            }

            DataRow newrow;
            foreach (DataRow dr in dt.Rows)
            {
                newrow = tempdt.NewRow();
                newrow.ItemArray = dr.ItemArray;
                tempdt.Rows.Add(newrow);
            }
            return tempdt;

        }
        public delegate int ScndPing(int netboardid, ref long RtT, string p);//定义委托
        ScndPing SPing;//声明委托
        int netboardid = 0;
        public int ping(int netboardid, ref long RtT, string p)
        {
            #region 从数据库中读取数据
            string dat = "select url,avgrtt,stddevrtt from tb_netboard where tb_netboard.netboardid=" + netboardid + "";
            Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
            DataTable dt = new DataTable();
            myda.Fill(dt);
            string url = dt.Rows[0][0].ToString().Trim();
            double avgrtt = Convert.ToDouble(dt.Rows[0][1]);
            double stddevrtt = Convert.ToDouble(dt.Rows[0][2]);
            #endregion

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // 使用默认TTL值128,
            // but change the fragmentation behavior.
            options.DontFragment = true;
            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            IPAddress Address = IPAddress.Parse(url);
            PingReply reply = pingSender.Send(Address, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {

                RtT = reply.RoundtripTime;

                int Ttl = reply.Options.Ttl;

                bool DF = reply.Options.DontFragment;

                int BfL = reply.Buffer.Length;
                if (RtT <= (avgrtt + (3 * stddevrtt)))//如果往返时长正常
                {
                    #region  向数据库写入成功数据
                    string MetData = "INSERT INTO sur.tb_ntbdata(netboardid,success, rtt, ttl, df, bfl, \"time\" )VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(MetData, myconnping);
                    myconnping.Open();
                    try
                    {

                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = this.netboardid;
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
                else//如果往返时长过大
                {
                    #region  向数据库写入报警数据
                    string ErrData = "INSERT INTO sur.tb_error(netboardid,success, rtt, ttl, df, bfl, \"time\"，handled,clientid )VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time, @handled,@clientid); ";
                    string MetData = "INSERT INTO sur.tb_ntbdata(netboardid,success, rtt, ttl, df, bfl, \"time\")VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(ErrData, myconnping);
                    myconnping.Open();
                    try
                    {

                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = this.netboardid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
                        mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                        mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                        mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                        mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = p;
                        mycommping.ExecuteNonQuery();

                        mycommping.CommandText = MetData;
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = this.netboardid;
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
                    return 2;
                }
            }
            else
            {

                return 1;
            }

            //if (servicetype == "数据库服务器")
            //{

            //}
        }
        int i = 1;
        long tRtT = 0;





        public int PingNetbd(int netboardid, ref long RtT, string p)//同步Ping网卡方法
        {
            SPing = new ScndPing(ping);//把函数指定给委托
            int s = 0;//循环计数器初始化
            while (s != 2)
            {
                i = SPing.Invoke(netboardid, ref tRtT, p);//同步执行委托

                s += 1;//计数器加一

                if (i == 1 && s == 1)
                {

                    Thread.Sleep(2000);//第一次不成功，睡眠2秒
                }
                else if (i == 1 && s == 2)//第二次不成功，写入数据并报错
                {
                    #region  向数据库写入失败数据
                    string ErrData = "INSERT INTO sur.tb_error(netboardid,success, rtt, ttl, df, bfl, \"time\"，handled,clientid )VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time, @handled,@clientid); ";
                    string MetData = "INSERT INTO sur.tb_ntbdata(netboardid,success, rtt, ttl, df, bfl, \"time\")VALUES(@netboardid,@success, @rtt, @ttl, @df, @bfl, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(ErrData, myconnping);
                    myconnping.Open();
                    RtT = 12000;
                    int Ttl = 0;
                    bool DF = false;
                    int BfL = 32;
                    try
                    {
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = this.netboardid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                        mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                        mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                        mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = p;
                        mycommping.ExecuteNonQuery();

                        mycommping.CommandText = MetData;
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = this.netboardid;
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
                }
                else break;//如果成功，跳出循环



            }

            RtT = tRtT;//读取临时变量的值
            return i;//返回状态

        }

        public delegate int ScndTest(int serviceid, string p);//定义委托
        ScndTest STest;//声明委托
        int svnetboardid;
        public int Service(int serviceid, string p)
        {
            #region 从数据库中读取数据
            string dat = "select tb_service.netboardid,tb_netboard.url,tb_service.port from tb_netboard inner join tb_service on tb_netboard.netboardid=tb_service.netboardid where tb_service.serviceid=" + serviceid + "";
            Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
            DataTable dt = new DataTable();
            myda.Fill(dt);
            svnetboardid = Convert.ToInt16(dt.Rows[0][0]);
            string url = dt.Rows[0][1].ToString().Trim();
            Int32 port = Convert.ToInt32(dt.Rows[0][2].ToString().Trim());
            #endregion
            #region 测试连接
            TcpClient tcpClient = new TcpClient();//实例化TCPClient
            try
            {
                tcpClient.Connect(IPAddress.Parse(url), port);//建立连接
                if (tcpClient.Connected)//如果成功
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    #region  向数据库写入成功数据
                    string MetData = "INSERT INTO sur.tb_svcdata(serviceid,success,\"time\" )VALUES(@serviceid, @success, @time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(MetData, myconnping);
                    myconnping.Open();
                    try
                    {
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
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
                else//如果不成功
                {
                    return 1;//返回错误
                }
            }
            catch (Exception ex)//如果不成功
            {
                tcpClient.Close();
                tcpClient.Dispose();
                return 1;//返回错误
            }
            #endregion
        }

        public int TestService(int serviceid, string p)
        {
            STest = new ScndTest(Service);//把函数指定给委托
            int s = 0;//循环计数器初始化
            while (s != 2)
            {
                i = STest.Invoke(serviceid, p);//同步执行委托

                s += 1;//计数器加一

                if (i == 1 && s == 1)
                {

                    Thread.Sleep(2000);//第一次不成功，睡眠2秒
                }
                else if (i == 1 && s == 2)//第二次不成功，写入数据并报错
                {
                    #region  向数据库写入失败数据
                    string ErrData = "INSERT INTO sur.tb_error(netboardid,serviceid,success,\"time\"，handled,clientid)VALUES(@netboardid,@serviceid,@success, @time, @handled,@clientid); ";
                    string MetData = "INSERT INTO sur.tb_svcdata(serviceid,success,\"time\")VALUES(@serviceid,@success,@time); ";
                    Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(ErrData, myconnping);
                    myconnping.Open();
                    try
                    {
                        mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = svnetboardid;
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                        mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                        mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = p;
                        mycommping.ExecuteNonQuery();

                        mycommping.CommandText = MetData; 
                        mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                        mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
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
                }
                else break;//如果成功，跳出循环



            }


            return i;//返回状态
        }

        public int ErrNtbd(int netboardid)
        {
            #region 从数据库中读取数据
            string dat = "select url from tb_netboard where tb_netboard.netboardid=" + netboardid + "";
            Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
            DataTable dt = new DataTable();
            myda.Fill(dt);
            string url = dt.Rows[0][0].ToString().Trim();
            #endregion

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // 使用默认TTL值128,
            // but change the fragmentation behavior.
            options.DontFragment = true;
            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            IPAddress Address = IPAddress.Parse(url);
            PingReply reply = pingSender.Send(Address, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public int ErrSvc(int serviceid)
        {
            #region 从数据库中读取数据
            string dat = "select tb_netboard.url,tb_service.port from tb_netboard inner join tb_service on tb_netboard.netboardid=tb_service.netboardid where tb_service.serviceid=" + serviceid + "";
            Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
            Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
            DataTable dt = new DataTable();
            myda.Fill(dt);

            string url = dt.Rows[0][0].ToString().Trim();
            Int32 port = Convert.ToInt32(dt.Rows[0][1].ToString().Trim());
            #endregion
            #region 测试连接
            TcpClient tcpClient = new TcpClient();//实例化TCPClient
            try
            {
                tcpClient.Connect(IPAddress.Parse(url), port);//建立连接
                if (tcpClient.Connected)//如果成功
                {
                    tcpClient.Close();
                    tcpClient.Dispose();
                    return 0;
                }
                else//如果不成功
                {
                    return 1;//返回错误
                }
            }
            catch (Exception ex)//如果不成功
            {
                tcpClient.Close();
                tcpClient.Dispose();
                return 1;//返回错误
            }
            #endregion
        }

    }

    [ServiceContract(Namespace = "Horace")]

    public interface Icl
    {

        [OperationContract]

        int Login(string p, string pswd);
        [OperationContract]
        [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
        DataSet Intialize(string p);
        [OperationContract]
        [FaultContract(typeof(WCFError))]
        int PingNetbd(int serviceid, ref long RtT, string p);
        [OperationContract]
        [FaultContract(typeof(WCFError))]
        int TestService(int serviceid, string p);
        [OperationContract]
        [FaultContract(typeof(WCFError))]
        int ErrNtbd(int netboardid);
        [OperationContract]
        [FaultContract(typeof(WCFError))]
        int ErrSvc(int serviceid);
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
        static void Send(string[] args)
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
