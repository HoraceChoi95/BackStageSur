# BackStageSur
服务器监控程序WCF客户端
**注意：** *此版本为终极测试版本*
## 3.15：
### 新增登录方法
` int Login(string clientid,string pswd) `
用户名无效返回2，密码错返回1，用户名密码匹配返回0.
### 新增服务器选择方法
` DataSet GetServer(string clientid) `
 返回登录用户的` serverid,name,type,url `,请在登录成功后在客户端调用
