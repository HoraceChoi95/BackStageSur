# BackStageSur
服务器监控程序WCF客户端（Ping分支）
## 4.7:
### PingNetbd方法更新  
原先的Ping方法（基于ICMP）的PingService已经改名成PingNetbd,
`int PingNetbd(int serviceid, ref long RtT, string p)`  
p为clientid,现在此方法会比较RtT的时间，如果时间过长，则返回2并向错误数据表中写入数据。如果正常返回0，失败返回1.
### 新增TestService方法
`int TestService(int serviceid, string p)`  
此方法用于检测各种基于TCP协议的服务的状态，失败会自动重试，如果连接成功返回0，失败返回1.  
### 新增ErrNtbd方法
`ErrNtbd(int netboardid)`  
此方法用于已经连接失败的网卡的监控，并不向数据库中写入数据，网卡恢复返回0，仍失败返回1.  
**注意**：此方法并不会比较RtT的时间。  
### 新增ErrSvc方法  
`int ErrSvc(int serviceid);`  
此方法用于已经连接失败的服务的监控，并不向数据库中写入数据，服务恢复返回0，仍失败返回1.
## 3.31:
### GetServer方法更新
`GetServer(string clientid)`方法现在改名为`Intialize(string p)`了，会返回具有三个表的Dataset。  
p为clientid,Netboard表会在返回前将url字段转为string型。  
数据库更新了测试数据。
## 3.24:
###  PingService容错机制更新 
PingService方法更新：`int PingService(int serviceid, ref long RtT)`  
Ping服务现在会从数据库中读取服务类型，根据不同类型进行不同方式的测试.在第一次Ping之后会休眠2秒后再次Ping服务，如果仍不成功，才会向数据库写入数据并返回1.  
~~已知问题：PingService只支持IP,不支持带有端口号的Host.现在只能测试Web服务，不能测试FTP和数据库（会返回失败）.~~
### WCF服务端属性更新
现在WCF应用是多线程应用了.
## 3.16:
### 新增单个服务器同步ping方法
`int PingSer(string serid,IPAddress Address,ref long RtT,ref int Ttl,bool DF,ref int BfL) `  
带有引用的参数（ref）传值前请先实例化（赋值）.  
**注意：** 方法的错误恢复未完成，数据库未建立，可能导致异常错误.
~~### 新增单个服务器异步ping方法
`static void PingSerAsync(string[] args, ref long RtT, ref int Ttl, bool DF, ref int BfL) `   
**注意：** 方法因还在修改暂时未添加到操作绑定，无法调用.~~
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
