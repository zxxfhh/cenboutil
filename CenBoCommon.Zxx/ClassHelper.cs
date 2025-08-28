using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CenBoCommon.Zxx
{
    public class ClassHelper
    {
        private static string _ClassName = string.Empty;
        public static string ClassName
        {
            get
            {
                _MethodName = "";
                GetCallerInfo();
                return _ClassName;
            }
        }

        private static string _MethodName = string.Empty;
        public static string MethodName
        {
            get
            {
                return _MethodName;
            }
        }

        private static void GetCallerInfo()
        {
            try
            {
                // 获取调用堆栈
                var stackTrace = new StackTrace(true);

                // 跳过当前方法(GetCallerInfo)和属性访问器(ClassName.get)
                for (int i = 2; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null) continue;

                    // 获取声明类型
                    var declaringType = method.DeclaringType;
                    if (declaringType == null) continue;

                    // 处理编译器生成的类型
                    if (declaringType.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    {
                        // 如果是编译器生成的类型，尝试获取原始类型
                        if (declaringType.DeclaringType != null)
                        {
                            declaringType = declaringType.DeclaringType;

                            // 获取原始方法名
                            var originalMethodName = GetOriginalMethodName(declaringType, method.Name);
                            if (!string.IsNullOrEmpty(originalMethodName))
                            {
                                _ClassName = declaringType.Name;
                                _MethodName = originalMethodName;
                                return;
                            }
                        }
                        continue;
                    }

                    // 获取方法名
                    string methodName = method.Name;

                    // 处理属性访问器
                    if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                    {
                        methodName = methodName.Substring(4);
                    }

                    // 处理异步方法
                    if (methodName.Contains("<"))
                    {
                        var match = Regex.Match(methodName, @"<(.+?)>");
                        if (match.Success && match.Groups.Count > 1)
                        {
                            string potentialMethodName = match.Groups[1].Value;

                            // 处理编译器生成的方法名
                            if (potentialMethodName.Contains(">g__"))
                            {
                                potentialMethodName = potentialMethodName.Substring(0, potentialMethodName.IndexOf(">g__"));
                            }

                            // 移除泛型参数
                            int genericIndex = potentialMethodName.IndexOf('`');
                            if (genericIndex > 0)
                            {
                                potentialMethodName = potentialMethodName.Substring(0, genericIndex);
                            }

                            methodName = potentialMethodName;
                        }
                    }

                    // 设置类名和方法名
                    _ClassName = declaringType.Name;
                    _MethodName = methodName;
                    return;
                }
            }
            catch (Exception)
            {
                _ClassName = "Unknown";
                _MethodName = "Unknown";
            }
        }

        private static string GetOriginalMethodName(Type declaringType, string methodName)
        {
            try
            {
                // 查找所有方法
                var methods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var method in methods)
                {
                    // 检查方法是否包含AsyncStateMachine特性
                    var stateMachineAttr = method.GetCustomAttribute<AsyncStateMachineAttribute>();
                    if (stateMachineAttr != null)
                    {
                        // 如果找到异步方法，返回其名称
                        return method.Name;
                    }
                }

                // 如果没有找到异步方法，返回null
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

