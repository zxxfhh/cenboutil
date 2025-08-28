using CenboNew.ServiceLog;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGatewayOcelot
{
    /// <summary>
    /// 自定义负载均衡器(随机)
    /// </summary>
    public class CustomLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;
        private readonly object _lock = new object();

        // 用来记录上次选择的索引
        private int _last;

        public CustomLoadBalancer(Func<Task<List<Service>>> services) : base()
        {
            _services = services;
        }

        public string Type => typeof(CustomLoadBalancer).Name;

        public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
        {
            var services = await _services();
            if (services == null || services.Count == 0)
            {
                string reqpath = httpContext.Request.Path;
                string _reqpath = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{reqpath}";
                string _RemoteIp = "";
                if (httpContext.Request.HttpContext.Connection.RemoteIpAddress != null)
                {
                    _RemoteIp = httpContext.Request.HttpContext.Connection.RemoteIpAddress.ToString().Replace("::ffff:", "");
                }
                LogHelper.ErrorLogWrite("CustomLoadBalancer", $"{_reqpath}", $"找不到下游服务", $"{_RemoteIp}");
                return new ErrorResponse<ServiceHostAndPort>(new ErrorInvokingLoadBalancerCreator(new Exception("负载平衡算法错误1")));
            }
            if (services.Count > 0)
            {
                lock (_lock)
                {
                    if (services.Count == 1)
                        return new OkResponse<ServiceHostAndPort>(services[0].HostAndPort);

                    //if (_last >= services.Count)
                    //{
                    //    _last = 0;
                    //}
                    //var next = services[_last];
                    //_last++;
                    //return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
                    _last = new Random().Next(services.Count);
                    return new OkResponse<ServiceHostAndPort>(services[_last].HostAndPort);
                }
            }
            return new ErrorResponse<ServiceHostAndPort>(new ErrorInvokingLoadBalancerCreator(new Exception("负载平衡算法错误2")));
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }

}
