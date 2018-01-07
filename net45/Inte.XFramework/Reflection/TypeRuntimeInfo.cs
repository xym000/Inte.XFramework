
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Inte.XFramework.Reflection.Emit;

namespace Inte.XFramework.Reflection
{
    /// <summary>
    /// 类型运行时元数据
    /// </summary>
    public class TypeRuntimeInfo
    {
        private Type _type = null;
        private bool _isAnonymousType = false;

        private object[] _customAttributes;
        private Dictionary<string, MemberAccessWrapper> _wrappers = null;
        private ConstructorInvoker _ctorInvoker = null;
        private Type[] _gTypes = null;
        private bool _gTypesReaded = false;

        /// <summary>
        /// 类型声明
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// 泛型参数列表
        /// </summary>
        public Type[] GenericArguments
        {
            get
            {
                if (!_gTypesReaded)
                {
                    if (_type.IsGenericType && _gTypes == null)
                    {
                        _gTypes = _type.GetGenericArguments();
                        _gTypesReaded = true;
                    }
                }

                return _gTypes;
            }
        }

        /// <summary>
        /// 是否为匿名类
        /// </summary>
        public bool IsAnonymousType
        {
            get { return _isAnonymousType; }
        }

        /// <summary>
        ///  获取一个值，该值指示当前类型是否是泛型类型。
        /// </summary>
        public bool IsGenericType
        {
            get { return _type.IsGenericType; }
        }

        /// <summary>
        /// 成员包装器集合
        /// </summary>
        public Dictionary<string, MemberAccessWrapper> Wrappers
        {
            get { return (_wrappers = _wrappers ?? this.InitializeWrapper(_type)); }
        }

        /// <summary>
        /// 构造函数调用器
        /// </summary>
        public ConstructorInvoker ConstructInvoker
        {
            get
            {
                if (_ctorInvoker == null)
                {
                    var ctor = this.GetConstructor();
                    _ctorInvoker = new ConstructorInvoker(ctor);

                }
                return _ctorInvoker;
            }
        }

        /// <summary>
        /// 初始化 <see cref="TypeRuntimeInfo"/> 类的新实例
        /// </summary>
        /// <param name="type">类型声明</param>
        internal TypeRuntimeInfo(Type type)
        {
            _type = type;
            _isAnonymousType = type != null && type.Name.Length > 18 && type.Name.IndexOf("AnonymousType", 5, StringComparison.InvariantCulture) == 5;
        }

        /// <summary>
        /// 取指定的成员包装器
        /// </summary>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public MemberAccessWrapper GetWrapper(string memberName)
        {
            MemberAccessWrapper wrapper = null;
            this.Wrappers.TryGetValue(memberName, out wrapper);

            //if (wrapper == null) throw new XfwException("member [{0}.{1}] doesn't exists", _type.Name, memberName);

            return wrapper;
        }

        /// <summary>
        /// 取指定的成员包装器自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetWrapperAttribute<TAttribute>(string memberName) where TAttribute : Attribute
        {
            MemberAccessWrapper wrapper = this.GetWrapper(memberName);
            return wrapper != null ? wrapper.GetCustomAttribute<TAttribute>() : null;
        }

        /// <summary>
        /// 取指定成员的值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public object Get(object target, string memberName)
        {
            MemberAccessWrapper wrapper = this.GetWrapper(memberName);
            return wrapper.Get(target);
        }

        /// <summary>
        /// 设置指定成员的值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="value">成员值</param>
        /// <returns></returns>
        public void Set(object target, string memberName, object value)
        {
            MemberAccessWrapper wrapper = this.GetWrapper(memberName);
            wrapper.Set(target, value);
        }

        /// <summary>
        /// 获取指定的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute
        {
            _customAttributes = _customAttributes ?? _type.GetCustomAttributes(false);
            return _customAttributes.Length == 0
                 ? null
                 : (_customAttributes.Length == 1 ? _customAttributes[0] : _customAttributes.FirstOrDefault(a => (a as TAttribute) != null)) as TAttribute;
        }

        /// <summary>
        /// 初始化成员包装器集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, MemberAccessWrapper> InitializeWrapper(Type type)
        {
            // 静态/实例 私有/公有
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            IEnumerable<MemberAccessWrapper> wrappers = type.GetMembers(bindingAttr)
                .Where(p => p.MemberType == MemberTypes.Property || p.MemberType == MemberTypes.Field || p.MemberType == MemberTypes.Method)
                .Select(p => new MemberAccessWrapper(p));

            // fix issue # overide method
            Dictionary<string, MemberAccessWrapper> d = new Dictionary<string, MemberAccessWrapper>();
            foreach (var p in wrappers)
            {
                if (!d.ContainsKey(p.Member.Name)) d.Add(p.Member.Name, p);
            }

            return d;
        }

        /// <summary>
        /// 获取构造函数
        /// 优先顺序与参数数量成反比
        /// </summary>
        /// <returns></returns>
        protected virtual ConstructorInfo GetConstructor()
        {
            ConstructorInfo[] ctors = _type.GetConstructors();
            if (_isAnonymousType) return ctors[0];

            for (int i = 0; i < 10; i++)
            {
                ConstructorInfo ctor = ctors.FirstOrDefault(x => x.GetParameters().Length == i);
                if (ctor != null) return ctor;
            }

            return ctors[0];
        }
    }
}
