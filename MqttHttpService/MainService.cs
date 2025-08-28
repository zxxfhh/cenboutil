using NewLife.Agent;

namespace MqttHttpService
{
    public partial class MainService : ServiceBase
    {
        public MainServer service;
        public MainService()
        {
            ServiceName = "MqttHttpService";

            DisplayName = "圣博WebApi和Mqtt通信互转服务";
            Description = "用于后端调用mqtt通信互转情况！";
        }

        protected override void StartWork(string reason)
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
            base.StartWork(reason);
        }

        protected override void StopWork(string reason)
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
            base.StopWork(reason);
        }

    }
}
