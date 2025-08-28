using CenboNew.ServiceLog;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ApiGatewayOcelot
{
    public class OcelotLogWare
    {
        private readonly RequestDelegate _next;
        private IConfiguration _configuration;
        private Stopwatch sw = new();

        public OcelotLogWare(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path == "/" || httpContext.Request.Path == "/favicon.ico")
            {
                return;
            }
            if (httpContext.Request.Method.ToUpper() != "OPTIONS")
            {
                sw.Restart();

                Microsoft.Extensions.Primitives.StringValues authorization = "";
                httpContext.Request.Headers.TryGetValue("authorization", out authorization);
                Microsoft.Extensions.Primitives.StringValues selftoken = "";
                httpContext.Request.Headers.TryGetValue("token", out selftoken);

                string reqpath = httpContext.Request.Path;
                UpRequestEntity up = new UpRequestEntity()
                {
                    ContentLength = httpContext.Request.ContentLength ?? 0,
                    ContentType = httpContext.Request.ContentType ?? "",
                    RequestMethod = httpContext.Request.Method,
                    RequestURL = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{reqpath}",
                    Authorization = authorization.ToString(),
                    SelfToken = selftoken.ToString(),
                };
                if (httpContext.Request.HttpContext.Connection.RemoteIpAddress != null)
                {
                    up.RemoteIp = httpContext.Request.HttpContext.Connection.RemoteIpAddress.ToString().Replace("::ffff:", "");
                    httpContext.Request.Headers["X-Real-IP"] = up.RemoteIp;
                }

                await _next(httpContext);

                DownResponseEntity down = new DownResponseEntity()
                {
                    ContentLength = httpContext.Response.ContentLength ?? 0,
                    ContentType = httpContext.Response.ContentType ?? "",
                    StatusCode = httpContext.Response.StatusCode,
                };
                // 从HttpContext获取下游请求信息
                var downstreamRequest = httpContext.Items["DownstreamRequest"] as Ocelot.Request.Middleware.DownstreamRequest;
                if (downstreamRequest != null)
                {
                    var downstreamUri = downstreamRequest.ToHttpRequestMessage().RequestUri;
                    if (downstreamUri != null)
                    {
                        down.RequestURL = downstreamUri.ToString();
                    }
                }

                sw.Stop();
                long esecond = sw.ElapsedMilliseconds;
                string result = $"上游JSON:{JsonConvert.SerializeObject(up)} 下游JSON:{JsonConvert.SerializeObject(down)}";
                _ = Task.Run(() =>
                {
                    LogHelper.SysLogWrite("OcelotLog", $"{reqpath}", $"耗时({esecond}ms):{result}", $"{up.RemoteIp}");
                });
            }
            else
            {
                await _next(httpContext);
            }
        }

    }
}
