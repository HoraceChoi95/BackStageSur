# BackStageSur
服务器监控程序WCF客户端（Ping分支）
## 3.24:
###  PingService容错机制更新 
PingService方法更新：`int PingService(int serviceid, ref long RtT)`
Ping服务现在会从数据库中读取服务类型，根据不同类型进行不同方式的测试.在第一次Ping之后会休眠2秒后再次Ping服务，如果仍不成功，才会向数据库写入数据并返回1.
已知问题：PingService只支持IP,不支持带有端口号的Host.现在只能测试Web服务，不能测试FTP和数据库（会返回失败）.
### WCF服务端属性更新
现在WCF应用是多线程应用了.
## 3.16:
### 新增单个服务器同步ping方法
`int PingSer(string serid,IPAddress Address,ref long RtT,ref int Ttl,bool DF,ref int BfL) ` 
 带有引用的参数（ref）传值前请先实例化（赋值）.
 **注意：** 方法的错误恢复未完成，数据库未建立，可能导致异常错误.
### 新增单个服务器异步ping方法
`static void PingSerAsync(string[] args, ref long RtT, ref int Ttl, bool DF, ref int BfL) ` 
 **注意：** 方法因还在修改暂时未添加到操作绑定，无法调用.
### 新增报错机制
为服务器选择方法添加报错机制，现在在客户端捕获封装好的FaultException错误类型即可获知服务端错误梗概,详细说明在代码的注释中，使用方法请看WCF-Error中的示例代码.

## 3.15：
### 新增登录方法
` int Login(string clientid,string pswd) `
pswd参量为客户端生成的密码MD5值，服务端只返回认证结果.
用户名无效返回2，密码错返回1，用户名密码匹配返回0.
### 新增服务器选择方法
` DataSet GetServer(string clientid) `
 返回登录用户的` serverid,name,type,url `,请在登录成功后在客户端调用
