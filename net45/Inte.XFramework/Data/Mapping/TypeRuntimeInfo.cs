
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 类型运行时元数据
    /// </summary>
    /// <remarks>适用Data命名空间，仅包含类型公开的属性元数据</remarks>
    public class TypeRuntimeInfo : Inte.XFramework.Reflection.TypeRuntimeInfo
    {
        private TableAttribute _table = null;
        private bool _attReaded = false;
        private Type _type = null;
        private IDictionary<string, MemberAccessWrapper> _navWrappers = null;
        private int _fieldCount = 0;
        //private Func<System.Data.IDataRecord, IDictionary<string, Column>, int, int, object> _deserializer;

        /// <summary>
        /// 类型对应的数据表
        /// </summary>
        public TableAttribute Table
        {
            get
            {
                if (!_attReaded)
                {
                    _table = base.GetCustomAttribute<TableAttribute>();
                    _attReaded = true;
                }
                return _table;
            }
        }

        /// <summary>
        /// 类型对应的数据表名，如果没有指定Table特性，则使用类型名称做为表名
        /// </summary>
        public string TableName
        {
            get
            {
                return this.Table != null && !string.IsNullOrEmpty(Table.Name) ? Table.Name : this._type.Name;
            }
        }

        /// <summary>
        /// 获取类型对应数据库的列数。
        /// </summary>
        public int FieldCount
        {
            get
            {
                var w = this.Wrappers;
                return _fieldCount;
            }
        }

        /// <summary>
        /// 导航属性成员
        /// </summary>
        public IDictionary<string, MemberAccessWrapper> NavWrappers
        {
            get
            {
                if (_navWrappers == null)
                {
                    _navWrappers = new Dictionary<string, MemberAccessWrapper>();
                    foreach (var kvp in this.Wrappers)
                    {
                        MemberAccessWrapper m = kvp.Value as MemberAccessWrapper;
                        if (m.ForeignKey != null) _navWrappers.Add(kvp.Key, m);
                    }
                }

                return _navWrappers;
            }
        }

        /// <summary>
        /// 初始化 <see cref="TypeRuntimeInfo"/> 类的新实例
        /// </summary>
        /// <param name="type">类型声明</param>
        public TypeRuntimeInfo(Type type)
            : base(type)
        {
            _type = type;
        }

        /// <summary>
        /// 初始化成员包装器集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override Dictionary<string, Reflection.MemberAccessWrapper> InitializeWrapper(Type type)
        {
            Dictionary<string, Reflection.MemberAccessWrapper> data = new Dictionary<string, Reflection.MemberAccessWrapper>();
            IEnumerable<MemberAccessWrapper> members =
                type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                //Fixed issue#匿名类的属性不可写
                //匿名类：new{ClientId=a.ClientId}
                .Where(p => p.CanRead && (this.IsAnonymousType ? true : p.CanWrite))
                .Select(p => new MemberAccessWrapper(p));

            foreach (MemberAccessWrapper m in members)
            {
                data.Add(m.Member.Name, m);
                if (!(m.Column != null && m.Column.NoMapped || m.ForeignKey != null)) _fieldCount += 1;
            }
            
            return data;
        }
    }
}
