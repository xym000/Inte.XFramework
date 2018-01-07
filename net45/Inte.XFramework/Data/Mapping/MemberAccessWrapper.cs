using System.Reflection;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 类属性访问包装器
    /// </summary>
    /// <remarks>适用Data命名空间下的类属性和数据库字段的映射</remarks>
    public class MemberAccessWrapper : Inte.XFramework.Reflection.MemberAccessWrapper
    {
        private ColumnAttribute _column = null;
        private bool _columnReaded = false;

        private ForeignKeyAttribute _foreignKey = null;
        private bool _foreignkeyReaded = false;

        /// <summary>
        /// 列特性
        /// </summary>
        public ColumnAttribute Column
        {
            get
            {
                if (!_columnReaded)
                {
                    _column = base.GetCustomAttribute<ColumnAttribute>();
                    _columnReaded = true;
                }
                return _column;
            }
        }

        /// <summary>
        /// 列特性
        /// </summary>
        public ForeignKeyAttribute ForeignKey
        {
            get
            {
                if (!_foreignkeyReaded)
                {
                    _foreignKey = base.GetCustomAttribute<ForeignKeyAttribute>();
                    _foreignkeyReaded = true;
                }
                return _foreignKey;
            }
        }

        /// <summary>
        /// 属性对应到数据库的字段名，如果没有指定Column特性，则使用属性名称做为字段名
        /// </summary>
        //public string ColumnName
        //{
        //    get
        //    {
        //        return this.Column != null && !string.IsNullOrEmpty(this.Column.Name) ? this.Column.Name : this.Member.Name;
        //    }
        //}

        /// <summary>
        /// 初始化 <see cref="MemberAccessWrapper"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public MemberAccessWrapper(MemberInfo member)
            : base(member)
        {
        }
    }
}
