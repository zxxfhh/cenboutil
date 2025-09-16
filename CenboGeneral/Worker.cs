using Microsoft.Extensions.Hosting;

namespace CenboGeneral
{
    public class Worker : BackgroundService
    {
        public MainServer service;
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                service = new MainServer();
                service.Start();
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            // 调用基类启动方法
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (service != null)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (service != null)
                {
                    service.Stop();
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            // 先调用基类停止方法
            await base.StopAsync(cancellationToken);
        }
    }
}
