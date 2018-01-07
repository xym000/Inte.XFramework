using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Inte.XFramework.Reflection.Emit
{
    /// <summary>
    /// 字段成员访问器
    /// </summary>
    public class MemberAccess_Field : MemberAccess
    {
        private Func<object, object> _getter = null;
        private Action<object, object> _setter = null;
        private FieldInfo _member = null;

        /// <summary>
        /// 初始化 <see cref="MemberAccess_Field"/> 类的新实例
        /// </summary>
        /// <param name="fi">字段元数据</param>
        public MemberAccess_Field(FieldInfo fi)
            :base(fi)
        {
            _member = fi;
        }

        /// <summary>
        /// Get 字段值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        public override object Get(object target)
        {
            _getter = _getter ?? MemberAccess_Field.InitializeGetter(_member);
            return _getter(target);
        }

        /// <summary>
        /// Set 字段值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        public override void Set(object target, object value)
        {
            _setter = _setter ?? MemberAccess_Field.InitializeSetter(_member);
            value = value ?? Helper.GetNullValue(_member.FieldType);
            _setter(target, value);
        }

        // 初始化 Get 动态方法
        private static Func<object, object> InitializeGetter(FieldInfo fi)
        {
            Type rpType = typeof(object);
            Type declaringType = fi.DeclaringType;
            DynamicMethod method = new DynamicMethod(string.Empty, rpType, new Type[] { rpType }, declaringType);
            ILGenerator g = method.GetILGenerator();

            // We need a reference to the current instance (stored in local argument index 1) 
            // so Ldfld can load from the correct instance (this one).
            if (!fi.IsStatic) g.Emit(OpCodes.Ldarg_0);
            g.Emit(fi.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fi);

            // Now, we execute the box opcode, which pops the value of field 'x',
            // returning a reference to the filed value boxed as an object.
            if (fi.FieldType.IsValueType)
                g.Emit(OpCodes.Box, fi.FieldType);
            g.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }


        // 初始化 Set 动态方法
        private static Action<object, object> InitializeSetter(FieldInfo fi)
        {
            Type declaringType = fi.DeclaringType;
            DynamicMethod method = new DynamicMethod(string.Empty, null, new Type[] { typeof(object), typeof(object) }, declaringType);
            ILGenerator g = method.GetILGenerator();
            Type fieldType = fi.FieldType;

            if (!fi.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);//Load the first argument (target object)
                g.Emit(OpCodes.Castclass, declaringType); //Cast to the source type
            }
            g.Emit(OpCodes.Ldarg_1);//Load the second argument (value object)
            if (fieldType.IsValueType)
                g.Emit(OpCodes.Unbox_Any, fieldType); //Unbox it 	
            g.Emit(fi.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fi);
            g.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }
    }
}
