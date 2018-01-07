


using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Inte.XFramework.Reflection.Emit
{
    /// <summary>
    /// 构造函数访问器
    /// </summary>
    public class ConstructorInvoker : IConstructorInvoker
    {
        private Func<object[], object> _invoker = null;
        private ConstructorInfo _ctor = null;

        /// <summary>
        /// 类的第一个构造函数（无参构造函数）
        /// </summary>
        public ConstructorInfo Constructor { get { return _ctor;} }

        /// <summary>
        /// 初始化 <see cref="MemberAccess"/> 类的新实例
        /// </summary>
        /// <param name="constructorInfo">构造函数</param>
        public ConstructorInvoker(ConstructorInfo constructorInfo)
        {
            _ctor = constructorInfo;
        }

        private static Func<object[], object> InitializeInvoker(ConstructorInfo constructorInfo)
        {
            Type declaringType = constructorInfo.DeclaringType;
            DynamicMethod mi = new DynamicMethod(string.Empty, typeof(object), new Type[1]
            {
                typeof (object[])
            }, declaringType);
            ILGenerator g = mi.GetILGenerator();
            ParameterInfo[] parameters = constructorInfo.GetParameters();

            for (int index = 0; index < parameters.Length; ++index)
            {
                g.Emit(OpCodes.Ldarg_0);
                g.EmitInt(index);
                g.Emit(OpCodes.Ldelem_Ref);
                g.EmitCast(parameters[index].ParameterType);
            }
            g.Emit(OpCodes.Newobj, constructorInfo);
            g.Emit(OpCodes.Ret);

            return mi.CreateDelegate(typeof(Func<object[], object>)) as Func<object[], object>;
        }

        /// <summary>
        /// 动态调用构造函数
        /// </summary>
        /// <param name="parameters">构造函数参数</param>
        /// <returns></returns>
        public object Invoke(params object[] parameters)
        {
            _invoker = _invoker ?? ConstructorInvoker.InitializeInvoker(_ctor);
            return _invoker(parameters);
        }
    }
}
