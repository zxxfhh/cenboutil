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

if (MainSetting.Current.IsDocker)
{
    IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();
    await host.RunAsync();
}
else
{
    var svc = new MainService();
    svc.Main(args);
}

#endif