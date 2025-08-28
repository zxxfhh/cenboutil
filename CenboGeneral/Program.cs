
using CenboGeneral;

#if DEBUG
Console.Title = "Mq控制http中转服务";

try
{
    MainServer server = new MainServer();
    server.Start();
}
catch (Exception ex)
{
    string s = ex.Message;
}

Console.ReadKey();
#else

var svc = new MainService();
svc.Main(args);

#endif