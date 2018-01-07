


using System;
using System.Linq;
using System.Reflection;
using Inte.XFramework.Reflection.Emit;
using System.Collections.Generic;

namespace Inte.XFramework.Reflection
{
    /// <summary>
    /// 类成员访问包装器 Facade
    /// </summary>
    public class MemberAccessWrapper
    {
        private MemberAccess _memberAccess = null;
        private object[] _customAttributes = null;
        private MemberInfo _member = null;
        private Type _dataType = null;
        private MethodInfo _setMethod = null;
        //private FieldInfo _fieldInfo = null;
        //private PropertyInfo _propertyInfo = null;
        //private MethodInfo _methodInfo = null;

        /// <summary>
        /// 成员元数据
        /// </summary>
        public MemberInfo Member
        {
            get
            {
                return _member;
            }
        }

        /// <summary>
        /// 字段
        /// </summary>
        public FieldInfo FieldInfo { get { return _member as FieldInfo; } }

        /// <summary>
        /// 属性
        /// </summary>
        public PropertyInfo PropertyInfo { get { return _member as PropertyInfo; } }

        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo MethodInfo { get { return _member as MethodInfo; } }

        /// <summary>
        /// 成员元数据类型
        /// </summary>
        public Type DataType
        {
            get
            {
                if (_dataType == null)
                {
                    _dataType = _member.MemberType == MemberTypes.Property
                        ? ((PropertyInfo)_member).PropertyType
                        : ((FieldInfo)_member).FieldType;
                }

                return _dataType;
            }
        }

        /// <summary>
        /// Set 访问器
        /// </summary>
        public MethodInfo SetMethod
        {
            get
            {
                if (_setMethod == null && _member.MemberType == MemberTypes.Property)
                {
                    _setMethod = (_member as PropertyInfo).GetSetMethod();
                }

                return _setMethod;
            }
        }

        /// <summary>
        /// 成员完全限定名
        /// </summary>
        public string FullName
        {
            get
            {
                return string.Concat(_member.ReflectedType, ".", _member.Name);
            }
        }

        /// <summary>
        /// 初始化 <see cref="MemberAccessWrapper"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public MemberAccessWrapper(MemberInfo member)
        {
            _member = member;
            if (_member.MemberType == MemberTypes.Property) _memberAccess = new MemberAccess_Property((PropertyInfo)_member);
            else if (_member.MemberType == MemberTypes.Field) _memberAccess = new MemberAccess_Field((FieldInfo)_member);
            else if (_member.MemberType == MemberTypes.Method) _memberAccess = new MemberAccess_Method((MethodInfo)_member);

            if (_memberAccess == null) throw new XfwException("member {0} not support", member.ToString());
        }

        /// <summary>
        /// Get 属性/字段 值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        public object Get(object target)
        {
            return _memberAccess.Get(target);
        }

        /// <summary>
        /// Set 属性/字段 值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        public void Set(object target, object value)
        {
            _memberAccess.Set(target, value);
        }

        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual object Invoke(object target, params object[] parameters)
        {
            return _memberAccess.Invoke(target, parameters);
        }

        /// <summary>
        /// 获取指定的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute
        {
            _customAttributes = _customAttributes ?? _member.GetCustomAttributes(false);
            return _customAttributes.Length == 0
                 ? null
                 : (_customAttributes.Length == 1 ? _customAttributes[0] : _customAttributes.FirstOrDefault(a => (a as TAttribute) != null)) as TAttribute;
        }

        /// <summary>
        /// 返回表示当前对象的字符串
        /// </summary>
        public override string ToString()
        {
            return this.FullName;
        }
    }
}
