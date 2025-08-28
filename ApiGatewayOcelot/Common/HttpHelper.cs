using NewLife.Log;
using RestSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using LogLevel = NewLife.Log.LogLevel;

namespace ApiGatewayOcelot
{
    public static class HttpHelper
    {
        private static readonly ConcurrentDictionary<string, RestClient> _clientCache = new ConcurrentDictionary<string, RestClient>();
        // 获取或创建 RestClient 实例
        private static RestClient GetOrCreateClient(string baseUrl, bool isSolr = false)
        {
            return _clientCache.GetOrAdd(baseUrl, url =>
            {
                int timeout = AppSetting.GetConfig("HttpRequest:OutTime").ToInt() * 1000;
                var options = new RestClientOptions(url)
                {
                    Timeout = TimeSpan.FromSeconds(timeout), // 请求超时时间
                    ThrowOnAnyError = false, // 遇到错误不抛出异常
                    FollowRedirects = true, // 支持重定向
                    ConfigureMessageHandler = handler =>
                    {
                        if (handler is HttpClientHandler httpHandler)
                        {
                            httpHandler.AllowAutoRedirect = true; // 启用自动重定向
                        }
                        return handler;
                    }
                };
                // 配置 HTTPS 的证书验证
                if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    options.RemoteCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                }
                // 创建 RestClient 实例
                var _client = new RestClient(options);
                if (isSolr) _client.AddDefaultHeader("Connection", "Keep-Alive");
                return _client;
            });
        }
        // 通用请求方法
        private static async Task<string> ExecuteRequestAsync(string url, Method method, Dictionary<string, string> headers = null, object body = null, DataFormat dataFormat = DataFormat.Json)
        {
            Stopwatch stopwatch = null;
            if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                stopwatch = Stopwatch.StartNew();
            try
            {
                var uri = new Uri(url);
                var isSolr = url.ToLower().Contains("solr");
                var baseUrl = uri.GetLeftPart(UriPartial.Authority); // 提取基础 URL
                var client = GetOrCreateClient(baseUrl, isSolr);
                var request = new RestRequest(uri.PathAndQuery, method);
                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }
                // 设置请求体
                if (body != null)
                {
                    if (dataFormat == DataFormat.Json)
                    {
                        request.AddJsonBody(body);
                    }
                    else if (dataFormat == DataFormat.Xml)
                    {
                        request.AddXmlBody(body);
                    }
                    else
                    {
                        request.AddParameter("text/plain", body, ParameterType.RequestBody);
                    }
                }
                // 执行请求
                var response = await client.ExecuteAsync(request);
                if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        XTrace.WriteLine($"Http-url:{response.ResponseUri}，结果长度:{response.Content?.Length}字节，耗时：{stopwatch.ElapsedMilliseconds}ms");
                    }
                    if (response.ErrorException != null)
                    {
                        XTrace.WriteException(response.ErrorException);
                    }
                }
                return response.Content;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                return string.Empty;
            }
        }
        // GET 请求
        public static async Task<string> ManGetAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Get, headers);
        }
        // POST 请求
        public static async Task<string> ManPostAsync(string url, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers);
        }
        // POST 请求（JSON 数据）
        public static async Task<string> ManPostBodyJsonAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.Json);
        }
        // POST 请求（XML 数据）
        public static async Task<string> ManPostBodyXmlAsync(string url, object data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.Xml);
        }
        // POST 请求（文本数据）
        public static async Task<string> ManPostBodyTextAsync(string url, string data, Dictionary<string, string> headers = null)
        {
            return await ExecuteRequestAsync(url, Method.Post, headers, data, DataFormat.None);
        }
        // POST 请求（表单数据）
        public static async Task<string> ManPostFormDataAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            Stopwatch stopwatch = null;
            if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                stopwatch = Stopwatch.StartNew();
            try
            {
                var uri = new Uri(url);
                var isSolr = url.ToLower().Contains("solr");
                var baseUrl = uri.GetLeftPart(UriPartial.Authority); // 提取基础 URL
                var client = GetOrCreateClient(baseUrl, isSolr);
                var request = new RestRequest(uri.PathAndQuery, Method.Post);
                request.AlwaysMultipartFormData = true;
                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }
                // 添加表单数据
                foreach (var item in formData)
                {
                    request.AddParameter(item.Key, item.Value);
                }
                // 执行请求
                var response = await client.ExecuteAsync(request);
                if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        XTrace.WriteLine($"Http-url:{response.ResponseUri}，结果长度:{response.Content?.Length}字节，耗时：{stopwatch.ElapsedMilliseconds}ms");
                    }
                    if (response.ErrorException != null)
                    {
                        XTrace.WriteException(response.ErrorException);
                    }
                }
                return response.Content;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                return string.Empty;
            }
        }
        // POST 请求（WWW 表单数据）
        public static async Task<string> ManPostWWWAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null)
        {
            Stopwatch stopwatch = null;
            if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                stopwatch = Stopwatch.StartNew();
            try
            {
                var uri = new Uri(url);
                var isSolr = url.ToLower().Contains("solr");
                var baseUrl = uri.GetLeftPart(UriPartial.Authority); // 提取基础 URL
                var client = GetOrCreateClient(baseUrl, isSolr);
                var request = new RestRequest(uri.PathAndQuery, Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                // 设置请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.AddHeader(header.Key, header.Value);
                    }
                }
                // 添加表单数据
                foreach (var item in formData)
                {
                    request.AddParameter(item.Key, item.Value);
                }
                // 执行请求
                var response = await client.ExecuteAsync(request);
                if (XTrace.Log.Level >= LogLevel.Info && XTrace.Log.Level < LogLevel.Off)
                {
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        XTrace.WriteLine($"Http-url:{response.ResponseUri}，结果长度:{response.Content?.Length}字节，耗时：{stopwatch.ElapsedMilliseconds}ms");
                    }
                    if (response.ErrorException != null)
                    {
                        XTrace.WriteException(response.ErrorException);
                    }
                }
                return response.Content;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                return string.Empty;
            }
        }
    }
}
