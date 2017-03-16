# BackStageSur
服务器监控程序WCF客户端（Ping分支）
## 3.16:
### 新增单个服务器同步ping方法
`int PingSer(string serid,IPAddress Address,ref long RtT,ref int Ttl,bool DF,ref int BfL)`
带有引用的参数（ref）传值前请先实例化（赋值）.
**注意：** 方法的错误恢复未完成，数据库未建立，可能导致异常错误.
### 新增单个服务器异步ping方法
`static void PingSerAsync(string[] args, ref long RtT, ref int Ttl, bool DF, ref int BfL)`
**注意：** 方法因还在修改暂时未添加到操作绑定，无法调用.
