using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BackStageSur
{
    class Log
    {
        /**//// <summary>
            /// 写入日志文件
            /// </summary>
            /// <param name="input"></param>
        public async void WriteLogFile(string input,string p)
        {
            /**/
            ///指定日志文件的目录
            
            /**/
            ///定义文件信息对象

            

            string sFilePath = "d:\\" + DateTime.Now.ToString("yyyyMM");
            string filename = "" + DateTime.Now.ToString("dd")+""+p+"用户日志.log";
            string fpath = sFilePath + "\\" + filename; //文件的绝对路径
            FileInfo finfo = new FileInfo(fpath);
            if (!Directory.Exists(sFilePath))//验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }

            /**/
            ///判断文件是否存在以及是否大于2K
            if (finfo.Length > 1024 * 1024 * 10)
            {
               
                //文件超过10MB则重命名
                File.Move(fpath, filename + DateTime.Now.TimeOfDay + "\\LogFile.txt");
                
            }
            
            ///创建只写文件流

            using (FileStream fs = finfo.OpenWrite())
            {
                /**/
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs);

                /**/
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);

                

                /**/
                ///写入并换行
                await w.WriteAsync(input+ "\n\r");

                /**/
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                await w.FlushAsync();

                /**/
                ///关闭写数据流
                w.Close();
            }

        }
    }
}
