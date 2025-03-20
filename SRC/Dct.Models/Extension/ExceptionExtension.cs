using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models
{
    /// <summary>
    /// ExceptionExtension 的摘要说明
    /// </summary>
    public static class ExceptionExtension
    {
        public static string GetExceptionMessage(this Exception exception)
        {
            StringBuilder sb = new StringBuilder();

            // 获取最终错误
            GetErrorMessage(sb, ref exception);

            // 前端错误
            string message = sb.ToString();

            // 记录详细错误信息
            //LogTool.Error(message, exception);

            return message;
        }

        private static StringBuilder GetErrorMessage(StringBuilder sb, ref Exception exception)
        {
            if (exception != null)
            {
                if (exception.InnerException != null)
                {
                    exception = exception.InnerException;
                    GetErrorMessage(sb, ref exception).ToString();
                }
                else if (exception is ReflectionTypeLoadException rEx)
                {
                    foreach (var loadEx in rEx.LoaderExceptions)
                    {
                        sb.AppendLine($"{loadEx.Message}");
                    }
                }
                else
                {
                    sb.AppendLine($"{exception.Message}");
                }
            }

            return sb;
        }
    }
}
