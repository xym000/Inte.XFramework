using System;

namespace Inte.XFramework
{
    /// <summary>
    /// XFramework 类库异常
    /// </summary>
    [Serializable]
    public class XfwException : Exception
    {
        public XfwException()
        {
        }

        public XfwException(string message)
            : base(message)
        {
        }

        public XfwException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public XfwException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public XfwException(string message, Exception innerException, params object[] args)
            : base(string.Format(message, args), innerException)
        {
        }

        public static void Throw(string message)
        {
            throw new XfwException(message);
        }

        public static void Throw(string message, params object[] args)
        {
            throw new XfwException(message, args);
        }

        public class Check
        {
            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static T NotNull<T>(T value, string parameterName) where T : class
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static T? NotNull<T>(T? value, string parameterName) where T : struct
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static string NotNull(string value, string parameterName)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(parameterName);
                }

                return value;
            }
        }
    }
}
