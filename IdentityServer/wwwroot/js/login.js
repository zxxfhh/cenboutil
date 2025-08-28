var passwordInput = document.getElementById('password');
var icon = document.querySelector(".show-hidden");

const getValue = (id) => { return document.getElementById(id).value }
const setValue = (id, val) => {
    document.getElementById(id).value = val;
}
document.getElementById("login").addEventListener("click", () => {
    //login()
    saveUser();
})
const saveUser = () => {
    const isRemember = document.getElementById("isRemember").checked
    const username = getValue("username")
    const password = getValue("password")
    if (isRemember) {
        localStorage.setItem("username", username);
        localStorage.setItem("password", password);
    } else {
        localStorage.removeItem("username");
        localStorage.removeItem("password");
    }
}
const onkeypress = (key) => {
    if (key.code === "Enter") {
        login();
    }
}
const login = () => {
    const username = getValue("username")
    const password = getValue("password")
    const isRemember = document.getElementById("isRemember").checked
    const returnurl = getValue("returnurl")

    var xhr = new XMLHttpRequest();
    xhr.open("POST", "/AuthLogin/Login",true);
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            document.documentElement.innerHTML = this.responseText;
        }
    };
    //xhr.setRequestHeader("Access-Control-Allow-Origin", "*");
    //xhr.setRequestHeader('Access-Control-Allow-Methods', 'GET,PUT,POST,DELETE');
    xhr.setRequestHeader('Access-Control-Allow-Headers', 'Content-Type');
    //xhr.setRequestHeader("sourcetype", "Web");

    var requesttoken = document.querySelector('input[name="__RequestVerificationToken"]').value;
    xhr.setRequestHeader("RequestVerificationToken", requesttoken);

    // 请求头:post方式传递普通键值对，需要设置Content-type编码格式，否则后台无法正确的获取到参数
    // xhr.setRequestHeader('Content-Type','application/x-www-form-urlencoded')
    // json格式，上面的是字符串格式
    xhr.setRequestHeader('Content-Type', 'application/json')

    var data = {
        Username: username,
        Password: password,
        RememberLogin: isRemember,
        ReturnUrl: returnurl,
        Button: "login",
    };
    var datastr = JSON.stringify(data);
    xhr.send(datastr);
}
window.document.addEventListener("keypress", onkeypress);

document.getElementById('showPasswordCheckbox').addEventListener('change', function () {
    while (icon.firstChild) {
        icon.removeChild(icon.firstChild);
    }
    let html = `	<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 20 20">
						<path fill="currentColor" d="M3.26 11.602C3.942 8.327 6.793 6 10 6s6.057 2.327 6.74 5.602a.5.5 0 0 0 .98-.204C16.943 7.673 13.693 5 10 5s-6.943 2.673-7.72 6.398a.5.5 0 0 0 .98.204M9.99 8a3.5 3.5 0 1 1 0 7a3.5 3.5 0 0 1 0-7" />
					</svg>`;
    if (this.checked) {
        html = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 20 20">
					<g fill="none">
						<path
							d="M2.854 2.146a.5.5 0 1 0-.708.708l3.5 3.498a8.097 8.097 0 0 0-3.366 5.046a.5.5 0 1 0 .979.204a7.09 7.09 0 0 1 3.108-4.528L7.95 8.656a3.5 3.5 0 1 0 4.884 4.884l4.313 4.314a.5.5 0 0 0 .708-.708l-15-15z"
							fill="currentColor" />
						<path d="M10.124 8.003l3.363 3.363a3.5 3.5 0 0 0-3.363-3.363z" fill="currentColor" />
						<path
							d="M7.531 5.41l.803.803A6.632 6.632 0 0 1 10 6c3.206 0 6.057 2.327 6.74 5.602a.5.5 0 1 0 .98-.204C16.943 7.673 13.693 5 10 5c-.855 0-1.687.143-2.469.41z"
							fill="currentColor" />
					</g>
				</svg>`;
        icon.innerHTML = html
        passwordInput.setAttribute('type', 'text');
    } else {
        icon.innerHTML = html;
        passwordInput.setAttribute('type', 'password');
    }
});
if (passwordInput.value) {
    icon.style.display = "block"
} else {
    icon.style.display = "none"
}
passwordInput.addEventListener('input', function (event) {
    if (event.target.value) {
        icon.style.display = "block"
    } else {
        icon.style.display = "none"
    }
})