using NewLife.Http;

namespace CenboGeneral
{
    /// <summary>自定义控制器。包含多个服务</summary>
    public class HttpComServer : IHttpHandler
    {
        public delegate void HttpComServerDelegate(IHttpContext context);
        public event HttpComServerDelegate? HttpComServerEvent;
        public void ProcessRequest(IHttpContext context)
        {
            HttpComServerEvent?.Invoke(context);
        }
    }
}
