using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Inte.XFramework.Reflection.Emit
{
    /// <summary>
    /// 属性成员访问器
    /// </summary>
    public class MemberAccess_Property : MemberAccess
    {
        private Func<object, object> _getter = null;
        private Action<object, object> _setter = null;
        private PropertyInfo _member = null;

        /// <summary>
        /// 初始化 <see cref="MemberAccess_Property"/> 类的新实例
        /// </summary>
        /// <param name="pi">字段元数据</param>
        public MemberAccess_Property(PropertyInfo pi)
            :base(pi)
        {
            _member = pi;
        }

        /// <summary>
        /// Get 属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        public override object Get(object target)
        {
            if (!_member.CanRead) throw new XfwException("this property [{0}] is unreadable", base.FullName);

            _getter = _getter ?? MemberAccess_Property.InitializeGetter(_member);
            return _getter(target);
        }

        /// <summary>
        /// Set 属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        public override void Set(object target, object value)
        {
            if (!_member.CanWrite) throw new XfwException("this property [{0}] is unwritable", base.FullName);

            _setter = _setter ?? MemberAccess_Property.InitializeSetter(_member);
            _setter(target, value ?? Helper.GetNullValue(_member.PropertyType));
        }

        // 初始化 Get 动态方法
        private static Func<object, object> InitializeGetter(PropertyInfo pi)
        {
            MethodInfo mi = pi.GetGetMethod(true);
            DynamicMethod method = new DynamicMethod(mi.Name, typeof(object), new Type[] { typeof(object) }, mi.Module);
            ILGenerator g = method.GetILGenerator();

            if (!mi.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);  //Load the first argument,(target object)
                g.Emit(OpCodes.Castclass, pi.DeclaringType);   //Cast to the source type
            }
            g.EmitCall(mi.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, mi, null); //Get the property value
            if (mi.ReturnType.IsValueType)
                g.Emit(OpCodes.Box, mi.ReturnType); //Box if necessary
            g.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        // 初始化 Set 动态方法
        private static Action<object, object> InitializeSetter(PropertyInfo property)
        {
            MethodInfo mi = property.GetSetMethod(true);
            DynamicMethod method = new DynamicMethod(mi.Name, null, new Type[] { typeof(object), typeof(object) }, mi.Module);
            ILGenerator g = method.GetILGenerator();
            Type paramType = mi.GetParameters()[0].ParameterType;

            if (!mi.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0); //Load the first argument (target object)
                g.Emit(OpCodes.Castclass, mi.DeclaringType); //Cast to the source type
            }
            g.Emit(OpCodes.Ldarg_1); //Load the second argument (value object)
            g.EmitCast(paramType);
            g.EmitCall(mi.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, mi, null); //Set the property value
            g.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }
    }
}
