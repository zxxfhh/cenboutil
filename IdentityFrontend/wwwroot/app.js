/// <reference path="oidc-client.js" />

function log() {
    document.getElementById('results').innerText = '';

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        }
        else if (typeof msg !== 'string') {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById('results').innerHTML += msg + '\r\n';
    });
}

document.getElementById("login").addEventListener("click", login, false);
document.getElementById("api").addEventListener("click", api, false);
document.getElementById("logout").addEventListener("click", logout, false);

var config = {
    authority: "http://localhost:5130/",
    //authority: "http://192.168.0.76:23676/",
    //authority: "http://identity.cenbo.com/",
    client_id: "ApiGatewayOcelot",
    redirect_uri: "http://localhost:5003/callback.html",
    response_type: "code",
    scope: "CenBoOcelotZhpdGsdxApi openid",
    post_logout_redirect_uri: "http://localhost:5003/index.html",
};
var mgr = new Oidc.UserManager(config);

mgr.getUser().then(function (user) {
    if (user) {
        log("用户token", user.access_token);
        log("登录成功", user.profile);
    }
    else {
        log("登录失败");
    }
});

function login() {
    mgr.signinRedirect();
}

function api() {
    mgr.getUser().then(function (user) {
        var url = "http://120.26.243.159:13603/zhpdapi/gsdx/HtkDeviceparam/GetParamTypeSelect?devtype=1";
        //var url = "http://192.168.0.76:23677/zhpdapi/gsdx/HtkDeviceparam/GetParamTypeSelect?devtype=1";

        var xhr = new XMLHttpRequest();
        xhr.open("GET", url);
        xhr.onload = function () {
            log(xhr.status, JSON.parse(xhr.responseText));
        };
        //xhr.setRequestHeader("Access-Control-Allow-Origin", "*");
        //xhr.setRequestHeader('Access-Control-Allow-Methods', 'GET,PUT,POST,DELETE');
        //xhr.setRequestHeader('Access-Control-Allow-Headers', 'Content-Type');
        xhr.setRequestHeader("sourcetype", "Web");
        xhr.setRequestHeader("authorization", "Bearer " + user.access_token);
        xhr.send();
    });
}

function logout() {
    mgr.signoutRedirect();
}