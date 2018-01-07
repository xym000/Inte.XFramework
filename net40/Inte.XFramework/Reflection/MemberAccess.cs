using System;
using System.Reflection;

namespace Inte.XFramework.Reflection
{
    /// <summary>
    /// 类成员访问器
    /// </summary>
    public abstract class MemberAccess
    {
        private MemberInfo _member;

        /// <summary>
        /// 成员实例
        /// </summary>
        public MemberInfo Member
        {
            get
            {
                return _member;
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
        /// 初始化 <see cref="MemberAccess"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public MemberAccess(MemberInfo member)
        {
            _member = member;
        }

        /// <summary>
        /// Get 属性/字段 值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        public virtual object Get(object target)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Set 属性/字段 值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        public virtual void Set(object target, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual object Invoke(object target, params object[] parameters)
        {
            throw new NotSupportedException();
        }
    }
}
