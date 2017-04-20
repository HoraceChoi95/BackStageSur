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
        static Socket server;
        private ServiceHost host;
        bool read = true;
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
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4770));//绑定端口号和IP
            Thread t = new Thread(ReciveMsg);//开启接收消息线程
            t.Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {

            host.Closed += delegate { MessageBox.Show("服务已经停止！"); };
            this.host.Close();
            this.bt_Stop.Enabled = false;
            this.bt_Ini.Enabled = true;
            read = false;

        }

        private void ReciveMsg()
        {
            while (read)
            {
                EndPoint point = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号
                byte[] buffer = new byte[1024];
                int length = FrmMain.server.ReceiveFrom(buffer, ref point);//接收数据报
                string message = Encoding.UTF8.GetString(buffer, 0, length);
                message += "\r\n";
                this.SetText(message);

            }


        }

        delegate void SetTextCallback(string text);
        /// <summary>
        /// 更新文本框内容的方法
        /// </summary>
        /// <param name="text"></param>
        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.textBox1.InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {
                while (!this.textBox1.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (this.textBox1.Disposing || this.textBox1.IsDisposed)
                        return;
                }
                SetTextCallback d = new SetTextCallback(SetText);
                this.textBox1.Invoke(d, new object[] { text });
            }
            else if (this.textBox1.TextLength >= 2147450880)
            {
                this.textBox1.Text = this.textBox1.Text.Substring(32767);
                this.textBox1.AppendText(text);
                this.textBox1.SelectionStart = textBox1.TextLength;
                this.textBox1.ScrollToCaret();
            }
            else
            {
                this.textBox1.AppendText(text);
                this.textBox1.SelectionStart = textBox1.TextLength;
                this.textBox1.ScrollToCaret();
            }
        }



        [ServiceBehavior(IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]//返回详细错误信息开启,开启多线程


        public class cl : Icl

        {
            #region UDP Socket发送信息服务端
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            EndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4770);
            #endregion
            public const string connstr = "Server=124.161.78.133;Port=9620;Database=BackStageSur;Uid=postgres;Pwd=swjtu;";
            /// <summary>
            /// 用于客户端账户登录
            /// </summary>
            public int Login(string p, string pswd)//登录方法
            {
                try
                {
                    string sqlstrlgn = "select passwd from sur.tb_login where clientid='" + p + "'";//选择clientid相对应的MD5
                    Npgsql.NpgsqlConnection myconnlgn = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommlgn = new Npgsql.NpgsqlCommand(sqlstrlgn, myconnlgn);
                    myconnlgn.Open();
                    string comp = mycommlgn.ExecuteScalar().ToString().Trim();    //MD5赋值给临时变量
                    myconnlgn.Close();
                    myconnlgn.Dispose();
                    if (comp == "" || comp == null)//判断是否有对应MD5
                    {
                        server.SendTo(Encoding.UTF8.GetBytes("" + p + "试图登录，用户名密码不存在"), point);
                        return 2;
                    }
                    else if (comp == pswd)//判断相等
                    {
                        server.SendTo(Encoding.UTF8.GetBytes("" + p + "登陆成功"), point);
                        return 0;
                    }
                    else
                    {
                        server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户密码校验失败"), point);
                        return 1;
                    }

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

            /// <summary>
            /// 用于客户端登录后一次性获取所有服务器、网卡和服务的方法，p为clientid
            /// </summary>
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
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户初始化数据返回成功"), point);
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

            /// <summary>
            /// 将网卡表中url字段从inet型转为string型的方法
            /// </summary>
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
            int ntserverid = 0;

            /// <summary>
            /// 网卡监测的线程函数
            /// </summary>
            public int ping(int netboardid, ref long RtT, string p)
            {
                #region 从数据库中读取数据
                string dat = "select url,avgrtt,stddevrtt,serverid from tb_netboard where tb_netboard.netboardid=" + netboardid + "";
                Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
                Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
                DataTable dt = new DataTable();
                myda.Fill(dt);
                string url = dt.Rows[0][0].ToString().Trim();
                double avgrtt = Convert.ToDouble(dt.Rows[0][1]);
                double stddevrtt = Convert.ToDouble(dt.Rows[0][2]);
                ntserverid = Convert.ToInt16(dt.Rows[0][3]);
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

                            mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
                            mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                            mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                            mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                            mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.ExecuteNonQuery();
                            server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户监测" + netboardid + "网卡，数据正常"), point);
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
                        string ErrData = "INSERT INTO sur.tb_error(netboardid,success, rtt, ttl, df, bfl, \"time\",handled,clientid,serverid)VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time, @handled,@clientid,@serverid); ";
                        string MetData = "INSERT INTO sur.tb_ntbdata(netboardid,success, rtt, ttl, df, bfl, \"time\")VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time); ";
                        Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                        Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(ErrData, myconnping);
                        myconnping.Open();
                        try
                        {

                            mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
                            mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                            mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                            mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                            mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                            mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = p;
                            mycommping.Parameters.Add("@serverid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = ntserverid;
                            mycommping.ExecuteNonQuery();

                            mycommping.CommandText = MetData;
                            mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = true;
                            mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                            mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                            mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                            mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.ExecuteNonQuery();
                            server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户监测" + netboardid + "网卡，往返时长过大"), point);
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




            /// <summary>
            /// 网卡监测方法
            /// </summary>
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
                        string ErrData = "INSERT INTO sur.tb_error(netboardid,success, rtt, ttl, df, bfl, \"time\",handled,clientid,serverid)VALUES(@netboardid, @success, @rtt, @ttl, @df, @bfl, @time, @handled,@clientid,@serverid); ";
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
                            mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                            mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                            mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                            mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                            mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.Parameters.Add("@handled", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                            mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = p;
                            mycommping.Parameters.Add("@serverid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = ntserverid;
                            mycommping.ExecuteNonQuery();

                            mycommping.CommandText = MetData;
                            mycommping.Parameters.Add("@netboardid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = netboardid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                            mycommping.Parameters.Add("@rtt", NpgsqlTypes.NpgsqlDbType.Bigint).Value = RtT;
                            mycommping.Parameters.Add("@ttl", NpgsqlTypes.NpgsqlDbType.Integer).Value = Ttl;
                            mycommping.Parameters.Add("@df", NpgsqlTypes.NpgsqlDbType.Boolean).Value = DF;
                            mycommping.Parameters.Add("@bfl", NpgsqlTypes.NpgsqlDbType.Integer).Value = BfL;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.ExecuteNonQuery();
                            server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户监测" + netboardid + "网卡，Ping失败"), point);
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
            int svserverid;

            /// <summary>
            /// 服务监测线程函数
            /// </summary>
            public int Service(int serviceid, string p)
            {
                #region 从数据库中读取数据
                string dat = "select tb_service.netboardid,tb_netboard.url,tb_service.port,tb_netboard.serverid from tb_netboard inner join tb_service on tb_netboard.netboardid=tb_service.netboardid where tb_service.serviceid=" + serviceid + "";
                Npgsql.NpgsqlConnection myconndat = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommdat = new Npgsql.NpgsqlCommand(dat, myconndat);
                Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(dat, myconndat);
                DataTable dt = new DataTable();
                myda.Fill(dt);
                svnetboardid = Convert.ToInt16(dt.Rows[0][0]);
                string url = dt.Rows[0][1].ToString().Trim();
                Int32 port = Convert.ToInt32(dt.Rows[0][2].ToString().Trim());
                svserverid = Convert.ToInt16(dt.Rows[0][3]);
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
                            server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户监测" + serviceid + "服务，成功"), point);
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
            /// <summary>
            /// 服务监测方法
            /// </summary>
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
                        string ErrData = "INSERT INTO sur.tb_error(netboardid,serviceid,success,\"time\",handled,clientid,serverid)VALUES(@netboardid,@serviceid,@success, @time, @handled,@clientid,@serverid); ";
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
                            mycommping.Parameters.Add("@serverid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = svserverid;
                            mycommping.ExecuteNonQuery();

                            mycommping.CommandText = MetData;
                            mycommping.Parameters.Add("@serviceid", NpgsqlTypes.NpgsqlDbType.Numeric).Value = serviceid;
                            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = false;
                            mycommping.Parameters.Add("@time", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = DateTime.Now.ToLongTimeString();
                            mycommping.ExecuteNonQuery();
                            server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户监测" + serviceid + "服务，失败"), point);
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
            /// <summary>
            /// 已经报错的网卡切换到此方法监测，不向数据库写入信息
            /// </summary>
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

            /// <summary>
            /// 已经报错的服务切换到此方法监测，不向数据库写入信息
            /// </summary>
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

            /// <summary>
            /// 读取特定服务器上的网卡和服务的详细信息，p为clientid
            /// </summary>
            public DataSet SvrDetl(int serverid, string p)
            {
                try
                {
                    string s = p;
                    string sqlstrGtSrvr = "select * from sur.tb_server where tb_server.clientid='" + s + "' and tb_server.serverid=" + serverid + "";
                    string sqlstrGtNetbd = "select netboardid,tb_netboard.serverid,url from tb_netboard inner join tb_server on tb_netboard.serverid=tb_server.serverid where tb_server.clientid='" + s + "' and tb_server.serverid=" + serverid + "";
                    string sqlstrGtSrvis = "select serviceid,tb_service.serverid,servicetype,servicename,netboardid,port from tb_service inner join tb_server on tb_service.serverid=tb_server.serverid where tb_server.clientid='" + s + "' and tb_server.serverid=" + serverid + "";
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
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询" + serverid + "服务器详细信息，成功"), point);
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

            /// <summary>
            /// 读取某用户的员工详细信息
            /// </summary>
            public DataSet ClientDetail(string p)
            {
                try
                {
                    string s = p;
                    string sqlstrGtCD = "select * from sur.tb_client where tb_client.clientid='" + s + "'";
                    Npgsql.NpgsqlConnection myconnGtCD = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrGtCD, myconnGtCD);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrGtCD, myconnGtCD);
                    myconnGtCD.Open();
                    DataTable dtGtCD = new DataTable("员工");
                    DataSet dsGtCD = new DataSet("ClientDetail");
                    myda.Fill(dtGtCD);
                    dsGtCD.Tables.Add(dtGtCD);
                    myconnGtCD.Close();
                    myconnGtCD.Dispose();
                    mycommGtSer.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询员工详细信息，成功"), point);
                    return dsGtCD;
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

            /// <summary>
            /// 读取某个服务器所有已处理和未处理的错误
            /// </summary>
            public DataSet SelSrvErr(int serverid, string p)
            {
                try
                {
                    string s = p;
                    string sqlstrSLER = "select * from sur.tb_error where tb_error.serverid=" + serverid + " and tb_error.clientid='" + p + "'";
                    string sqlstrSLER1 = "select * from sur.tb_error where tb_error.serverid=" + serverid + " and tb_error.clientid='" + p + "' and handled=false";
                    Npgsql.NpgsqlConnection myconnSLER = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrSLER, myconnSLER);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrSLER, myconnSLER);
                    myconnSLER.Open();
                    DataTable dtSLER = new DataTable("所有错误");
                    DataTable dtSLER1 = new DataTable("未处理错误");
                    DataSet dsSLER = new DataSet("Errors");
                    myda.Fill(dtSLER);

                    dsSLER.Tables.Add(dtSLER);
                    mycommGtSer.CommandText = sqlstrSLER1;
                    myda.SelectCommand.CommandText = sqlstrSLER1;
                    myda.Fill(dtSLER1);
                    dsSLER.Tables.Add(dtSLER1);
                    myconnSLER.Close();
                    myconnSLER.Dispose();
                    mycommGtSer.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询" + serverid + "服务器错误详细信息，成功"), point);
                    return dsSLER;
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

            /// <summary>
            /// 读取某个用户所有未处理的错误
            /// </summary>
            public DataSet SelUhdErr(string p)
            {
                try
                {
                    string s = p;

                    string sqlstrSLER1 = "select * from sur.tb_error where tb_error.clientid='" + p + "' and handled=false";
                    Npgsql.NpgsqlConnection myconnSLER = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrSLER1, myconnSLER);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrSLER1, myconnSLER);
                    myconnSLER.Open();

                    DataTable dtSLER1 = new DataTable("未处理错误");
                    DataSet dsSLER = new DataSet("Errors");


                    mycommGtSer.CommandText = sqlstrSLER1;
                    myda.SelectCommand.CommandText = sqlstrSLER1;
                    myda.Fill(dtSLER1);
                    dsSLER.Tables.Add(dtSLER1);
                    myconnSLER.Close();
                    myconnSLER.Dispose();
                    mycommGtSer.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询未处理错误详细信息，成功"), point);
                    return dsSLER;
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

            /// <summary>
            /// 读取用户指定网卡的指定条数的最近错误
            /// </summary>
            public DataSet SelNtbRctErr(int netboardid, int count, string p)
            {
                try
                {
                    string s = p;
                    int ntbid = netboardid;
                    int ct = count;
                    string sqlstrSNRE = "select * from tb_error where tb_error.netboardid=" + ntbid + " and tb_error.clientid='" + s + "' and tb_error.serviceid is null order by time desc limit " + ct + "";
                    Npgsql.NpgsqlConnection myconnSNRE = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrSNRE, myconnSNRE);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrSNRE, myconnSNRE);
                    myconnSNRE.Open();

                    DataTable dtSNRE = new DataTable("网卡最近错误");
                    DataSet dsSNRE = new DataSet("NtbRctErrors");


                    mycommGtSer.CommandText = sqlstrSNRE;
                    myda.SelectCommand.CommandText = sqlstrSNRE;
                    myda.Fill(dtSNRE);
                    dsSNRE.Tables.Add(dtSNRE);
                    myconnSNRE.Close();
                    myconnSNRE.Dispose();
                    mycommGtSer.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询" + netboardid + "号网卡未处理最近" + count + "条错误详细信息，成功"), point);
                    return dsSNRE;
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

            /// <summary>
            /// 用于在新增服务器之前选择服务器的紧急联系人（读取数据库中所有员工信息）
            /// </summary>
            public DataSet SelEmerEmp(string clientid)
            {
                try
                {
                    string s = clientid;
                    string sqlstrSEE = "select employid,name from tb_client where tb_client.clientid='"+s+"'";
                    Npgsql.NpgsqlConnection myconnSEE = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrSEE, myconnSEE);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrSEE, myconnSEE);
                    myconnSEE.Open();

                    DataTable dtSEE = new DataTable("员工");
                    DataSet dsSEE = new DataSet("emlpoyee");


                    mycommGtSer.CommandText = sqlstrSEE;
                    myda.SelectCommand.CommandText = sqlstrSEE;
                    myda.Fill(dtSEE);
                    dsSEE.Tables.Add(dtSEE);
                    myconnSEE.Close();
                    myconnSEE.Dispose();
                    mycommGtSer.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + s + "用户选择员工中紧急联系人信息，成功"), point);
                    return dsSEE;
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
            /// <summary>
            /// 新增一个服务器
            /// </summary>
            public int InsSvr(string clientid,string servername,int commyear,string empolyid)
            {
                string cid = clientid;
                string svrname = servername;
                int cyear = commyear;
                string eid = empolyid;
                string InsSvr = "INSERT INTO sur.tb_server(clientid,name,commissionyear,emergency)VALUES(@clientid,@name,@commissionyear,@emergency); ";
                Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(InsSvr, myconnping);
                myconnping.Open();
                try
                {
                    mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char,10).Value = cid;
                    mycommping.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Char, 40).Value =svrname;
                    mycommping.Parameters.Add("@commissionyear", NpgsqlTypes.NpgsqlDbType.Bigint).Value =cyear;
                    mycommping.Parameters.Add("@emergency", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = eid;
                    mycommping.ExecuteNonQuery();
                    server.SendTo(Encoding.UTF8.GetBytes("" + cid+ "用户新增" + svrname + "服务器，成功"), point);
                    myconnping.Close();
                    return 0;
                }
                catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
                {
                    myconnping.Close();
                    server.SendTo(Encoding.UTF8.GetBytes("" + cid + "用户新增" + svrname + "服务器，数据库写入失败"), point);
                    var error = new WCFError("Insert", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                    throw new FaultException<WCFError>(error, error.Message);//抛出错误
                }
                
            }
            /// <summary>
            /// 新增一个雇员
            /// </summary>
            public int InsEmp(string name,int age,string sex,string tel,string email,string clientid)
            {
                string cid = clientid;
                string nme = name;
                int ag = age;
                string gender= sex;
                string at = email;
                string InsEmp = "INSERT INTO sur.tb_client(clientid,name,age,tel,email,sex)VALUES(@clientid,@name,@age,@tel,@email,@sex); ";
                Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(InsEmp, myconnping);
                myconnping.Open();
                try
                {
                    mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = cid;
                    mycommping.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Char, 10).Value = nme;
                    mycommping.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Smallint).Value = ag;
                    mycommping.Parameters.Add("@tel", NpgsqlTypes.NpgsqlDbType.Char, 15).Value = tel;
                    mycommping.Parameters.Add("@email", NpgsqlTypes.NpgsqlDbType.Char, 20).Value = at;
                    mycommping.Parameters.Add("@sex", NpgsqlTypes.NpgsqlDbType.Char, 5).Value = gender;
                    mycommping.ExecuteNonQuery();
                    server.SendTo(Encoding.UTF8.GetBytes("" + cid + "用户新增" + nme+ "雇员，成功"), point);
                    myconnping.Close();
                    return 0;
                }
                catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
                {
                    myconnping.Close();
                    server.SendTo(Encoding.UTF8.GetBytes("" + cid + "用户新增" + nme + "雇员，数据库写入失败"), point);
                    var error = new WCFError("Insert", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                    throw new FaultException<WCFError>(error, error.Message);//抛出错误
                }
                
            }

            /// <summary>
            /// 读取网卡最近的指定条数据
            /// </summary>
            public DataSet SelNtbRctData(int netboardid, int count, string p)
            {
                try
                {
                    string s = p;
                    int ntbid = netboardid;
                    int ct = count;
                    string sqlstrSNRD = "select * from tb_ntbdata where tb_ntbdata.netboardid=" + ntbid + " order by time desc limit " + ct + "";
                    Npgsql.NpgsqlConnection myconnSNRD = new Npgsql.NpgsqlConnection(connstr);
                    Npgsql.NpgsqlCommand mycommSNRD = new Npgsql.NpgsqlCommand(sqlstrSNRD, myconnSNRD);
                    Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrSNRD, myconnSNRD);
                    myconnSNRD.Open();

                    DataTable dtSNRD = new DataTable("网卡最近数据");
                    DataSet dsSNRD = new DataSet("NtbRctData");


                    mycommSNRD.CommandText = sqlstrSNRD;
                    myda.SelectCommand.CommandText = sqlstrSNRD;
                    myda.Fill(dtSNRD);
                    dsSNRD.Tables.Add(dtSNRD);
                    myconnSNRD.Close();
                    myconnSNRD.Dispose();
                    mycommSNRD.Dispose();
                    server.SendTo(Encoding.UTF8.GetBytes("" + p + "用户查询" + netboardid + "号网卡最近" + count + "条数据详细信息，成功"), point);
                    return dsSNRD;
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
            [OperationContract]
            [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
            DataSet SvrDetl(int serverid, string p);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet ClientDetail(string p);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet SelSrvErr(int serverid, string p);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet SelUhdErr(string p);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet SelNtbRctErr(int netboardid, int count, string p);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet SelEmerEmp(string clientid);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            int InsSvr(string clientid, string servername, int commyear, string empolyid);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            int InsEmp(string name, int age, string sex, string tel, string email, string clientid);
            [OperationContract]
            [FaultContract(typeof(WCFError))]
            DataSet SelNtbRctData(int netboardid, int count, string p);
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
}