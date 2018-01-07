
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Inte.XFramework.Reflection.Emit
{
    /// <summary>
    /// 方法成员访问器
    /// </summary>
    public sealed class MemberAccess_Method : MemberAccess//, IMethodInvoker
    {
        private Func<object, object[], object> _invoker;
        private MethodInfo _member = null;

        /// <summary>
        /// 初始化 <see cref="MemberAccess_Method"/> 类的新实例
        /// </summary>
        /// <param name="mi">方法元数据</param>
        public MemberAccess_Method(MethodInfo mi)
            :base(mi)
        {
            _member = mi;
        }

        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public override object Invoke(object target, params object[] parameters)
        {
            _invoker = _invoker ?? MemberAccess_Method.InitializeInvoker(_member);
            return _invoker(target, parameters);
        }

        // 初始化方法调用器
        private static Func<object, object[], object> InitializeInvoker(MethodInfo mi)
        {
            DynamicMethod dm = mi.IsPublic
                ? new DynamicMethod(mi.Name, typeof(object), new Type[2] { typeof(object), typeof(object[]) }, mi.Module)
                : new DynamicMethod(mi.Name, typeof(object), new Type[2] { typeof(object), typeof(object[]) }, mi.DeclaringType);

            ILGenerator g = dm.GetILGenerator();
            ParameterInfo[] parameters = mi.GetParameters();
            Type[] typeArray = new Type[parameters.Length + (!mi.IsStatic ? 1 : 0)];

            for (int index = 0; index < typeArray.Length; ++index)
            {
                typeArray[index] = index == parameters.Length
                    ? mi.DeclaringType
                    : (parameters[index].ParameterType.IsByRef ? parameters[index].ParameterType.GetElementType() : parameters[index].ParameterType);
            }
            LocalBuilder[] local = new LocalBuilder[typeArray.Length];
            for (int index = 0; index < typeArray.Length; ++index)
            {
                local[index] = g.DeclareLocal(typeArray[index], true);
            }
            for (int index = 0; index < parameters.Length; ++index)
            {
                g.Emit(OpCodes.Ldarg_1);
                g.EmitInt(index);
                g.Emit(OpCodes.Ldelem_Ref);
                g.EmitCast(typeArray[index]);
                g.Emit(OpCodes.Stloc, local[index]);
            }

            if (!mi.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);
                if (mi.DeclaringType.IsValueType)
                {
                    g.Emit(OpCodes.Unbox_Any, mi.DeclaringType);
                    g.Emit(OpCodes.Stloc, local[local.Length - 1]);
                    g.Emit(OpCodes.Ldloca_S, local.Length - 1);
                }
            }

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                    g.Emit(OpCodes.Ldloca_S, local[index]);
                else
                    g.Emit(OpCodes.Ldloc, local[index]);
            }

            if (mi.IsVirtual)
                g.EmitCall(OpCodes.Callvirt, mi, null);
            else
                g.EmitCall(OpCodes.Call, mi, null);
            if (mi.ReturnType == typeof(void))
                g.Emit(OpCodes.Ldnull);
            else
                g.EmitBoxIfNeeded(mi.ReturnType);

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                {
                    g.Emit(OpCodes.Ldarg_1);
                    g.EmitInt(index);
                    g.Emit(OpCodes.Ldloc, local[index]);
                    g.EmitBoxIfNeeded(local[index].LocalType);
                    g.Emit(OpCodes.Stelem_Ref);
                }
            }
            g.Emit(OpCodes.Ret);

            return dm.CreateDelegate(typeof(Func<object, object[], object>)) as Func<object, object[], object>;
        }
    }
}
