using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IdentityServer.Controllers
{
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        [Route("Api/IdsServer/UserLogin")]
        public MetaData UserLogin(LoginParam model)
        {
            MetaData meta = new MetaData()
            {
                Status = false,
                Total = 1,
                Message = "登录失败"
            };

            Dictionary<string, string> headers = new Dictionary<string, string>();
            int clientCount = AppSetting.GetConfig("IdentityServer:ClientCount").ToInt2();
            for (int i = 1; i <= clientCount; i++)
            {
                string name = "IdentityServer:Client" + i;
                if (AppSetting.GetConfig(name + ":ClientCode") == model.ClientCode)
                {
                    headers.Add("client_id", AppSetting.GetConfig(name + ":ClientId"));
                    headers.Add("client_secret", AppSetting.GetConfig(name + ":ClientSecrets"));
                    //headers.Add("grant_type", "client_credentials");
                    headers.Add("grant_type", "password");
                    headers.Add("username", $"{model.UserCode}|{model.SourceType}");
                    headers.Add("password", model.Password);
                }
            }

            string tokenurl = AppSetting.GetConfig("IdentityServer:Authority");

            using (HttpClient http = new HttpClient())
            using (var content = new FormUrlEncodedContent(headers))
            {
                var msg = http.PostAsync(tokenurl, content).Result;
                if (msg.IsSuccessStatusCode)
                {
                    string result = msg.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(result))
                    {
                        JObject jo = JObject.Parse(result);
                        if (jo != null && jo.Property("access_token") != null)
                        {
                            meta.Status = true;
                            meta.Result = jo["access_token"].ToString();
                        }
                    }
                }
            }
            if (OperatorCommon.LoginMessage.ContainsKey(model.UserCode))
            {
                meta.Message = OperatorCommon.LoginMessage[model.UserCode];
            }

            return meta;
        }

    }
}
