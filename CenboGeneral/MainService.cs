using NewLife.Agent;

namespace CenboGeneral
{
    public partial class MainService : ServiceBase
    {
        public MainServer service;
        public MainService()
        {
            ServiceName = "CenboGeneral";

            DisplayName = "圣博通用和守护服务";
            Description = "用于API控制、短信通知、邮件通知、看门狗等功能";
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
