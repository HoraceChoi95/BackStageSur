# BackStageSur
服务器监控程序WCF客户端（Ping分支）
## 3.16:
### 新增单个服务器同步ping方法
`int PingSer(string serid,IPAddress Address,ref long RtT,ref int Ttl,bool DF,ref int BfL) ` 
 带有引用的参数（ref）传值前请先实例化（赋值）.
 **注意：** 方法的错误恢复未完成，数据库未建立，可能导致异常错误.
### 新增单个服务器异步ping方法
`static void PingSerAsync(string[] args, ref long RtT, ref int Ttl, bool DF, ref int BfL) ` 
 **注意：** 方法因还在修改暂时未添加到操作绑定，无法调用.
## 3.15：
### 新增登录方法
` int Login(string clientid,string pswd) `
pswd参量为客户端生成的密码MD5值，服务端只返回认证结果.
用户名无效返回2，密码错返回1，用户名密码匹配返回0.
### 新增服务器选择方法
` DataSet GetServer(string clientid) `
 返回登录用户的` serverid,name,type,url `,请在登录成功后在客户端调用
