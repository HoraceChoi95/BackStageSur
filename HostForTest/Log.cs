using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;



namespace BackStageSur
{
    class Log
    {
        public Mutex mux = new Mutex();//创建进程同步单元

        /// <summary>
        /// 写入日志文件
        /// </summary>
        public async void WriteLogFile(string input, string p)
        {
            string sFilePath = "D:\\" + DateTime.Now.ToString("yyyyMM") + "\\" + p + ""; //指定日志文件的目录
            string filename = "" + DateTime.Now.ToString("dd") + "" + p + "用户日志.log";//定义文件信息对象
            string fpath = sFilePath + "\\" + filename; //文件的绝对路径
            FileInfo finfo = new FileInfo(fpath);
            if (!Directory.Exists(sFilePath))//验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath); //不存在则创建              
            }
            if (!finfo.Exists)
            {
                FileStream fs;
                fs = File.Create(fpath);
                fs.Close();
                finfo = new FileInfo(fpath);
            }
            if (finfo.Length > 1024 * 1024 * 10)//判断文件是否存在以及是否大于2K
            {
                File.Move(fpath, filename + DateTime.Now.TimeOfDay + "\\LogFile.txt");//文件超过10MB则重命名
            }
            if (Used(fpath) == 0)//确认文件是否占用
            {
                FileStream fs = new FileStream(fpath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);//创建写流，读写共享模式
                StreamWriter w = new StreamWriter(fs);//根据上面创建的文件流创建写数据流
                w.BaseStream.Seek(0, SeekOrigin.End);//设置写数据流的起始位置为文件流的末尾
                await w.WriteAsync(input + "\r\n");//异步写入并换行
                await w.FlushAsync();//清空缓冲区内容，并把缓冲区内容写入基础流
                w.Close();//关闭写数据流
            }
            else//如果占用
            {
                mux.WaitOne();//同步基元等待释放
                using (FileStream fs = finfo.OpenWrite())//创建只写流
                {
                    StreamWriter w = new StreamWriter(fs);//根据上面创建的文件流创建写数据流
                    w.BaseStream.Seek(0, SeekOrigin.End);//设置写数据流的起始位置为文件流的末尾
                    await w.WriteAsync(input + "\r\n");//异步写入并换行
                    await w.FlushAsync();//清空缓冲区内容，并把缓冲区内容写入基础流
                    w.Close();//关闭写数据流
                }
                mux.ReleaseMutex();//释放同步基元
            }
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        public const int OF_READWRITE = 2;
        public const int OF_SHARE_DENY_NONE = 0x40;
        public readonly IntPtr HFILE_ERROR = new IntPtr(-1);
        private int Used(string fpath)
        {
            IntPtr vHandle = _lopen(fpath, OF_READWRITE | OF_SHARE_DENY_NONE);
            if (vHandle == HFILE_ERROR)
            {
                CloseHandle(vHandle);
                return 1;
            }
            else
                CloseHandle(vHandle);
            return 0;
        }
        public async void logDB(string clientid, string action, bool success)
        {
            string connstr = "Server=124.161.78.133;Port=9620;Database=BackStageSur;Uid=postgres;Pwd=swjtu;";
            string InsNtbd = "INSERT INTO sur.tb_log(clientid,action,success)VALUES(@clientid,@action,@success); ";
            Npgsql.NpgsqlConnection myconnping = new Npgsql.NpgsqlConnection(connstr);
            Npgsql.NpgsqlCommand mycommping = new Npgsql.NpgsqlCommand(InsNtbd, myconnping);
            myconnping.Open();
            mycommping.Parameters.Add("@clientid", NpgsqlTypes.NpgsqlDbType.Char, 4).Value = clientid;
            mycommping.Parameters.Add("@action", NpgsqlTypes.NpgsqlDbType.Varchar, 100).Value = action;
            mycommping.Parameters.Add("@success", NpgsqlTypes.NpgsqlDbType.Boolean).Value = success;
            int x=await mycommping.ExecuteNonQueryAsync();
            myconnping.Close();
        }
        
        public static string MailFrom = "horacechoi@outlook.com";
        public static string host = "smtp-mail.outlook.com";
        public static string username = "horacechoi@outlook.com";
        public static string password = "BOYSboys4770";
        public static int port = 587;
        public static bool ssl = true;
    }
}
