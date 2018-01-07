using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;查&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Select<T> : DbQueryableInfo<T>
    {
        private List<DbExpression> _join = null;
        private List<DbExpression> _orderBy = null;
        private DbExpression _groupBy = null;

        /// <summary>
        /// JOIN 表达式集合
        /// </summary>
        public List<DbExpression> Join
        {
            get { return _join; }
            set { _join = value; }
        }

        /// <summary>
        /// ORDER BY 表达式集合
        /// </summary>
        public List<DbExpression> OrderBy
        {
            get { return _orderBy; }
            set { _orderBy = value; }
        }

        /// <summary>
        /// GROUP BY 表达式集合
        /// </summary>
        public DbExpression GroupBy
        {
            get { return _groupBy; }
            set { _groupBy = value; }
        }

        /// <summary>
        /// SQL 命令是否含 DISTINCT 
        /// </summary>
        public bool HaveDistinct { get; set; }

        /// <summary>
        /// 表达式是否是 Any 表达式
        /// </summary>
        public bool HaveAny { get; set; }

        /// <summary>
        /// 跳过序列中指定数量的元素
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// 从序列的开头返回指定数量的连续元素
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// 指示 SELECT FROM 子句表对应类型
        /// </summary>
        public Type FromType { get; set; }

        /// <summary>
        /// SELECT 字段表达式，空表示选取 <see cref="FromType"/> 的所有字段
        /// </summary>
        public DbExpression Expression { get; set; }

        /// <summary>
        /// WHERE 表达式
        /// </summary>
        public DbExpression Where { get; set; }

        /// <summary>
        /// HAVING 表达式
        /// </summary>
        public DbExpression Having { get; set; }

        /// <summary>
        /// 统计函数表达式，包括如：COUNT,MAX,MIN,AVG,SUM
        /// </summary>
        public DbExpression Statis { get; set; }

        /// <summary>
        /// 子查询语义
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </summary>
        public IDbQueryableInfo<T> Subquery { get; set; }

        /// <summary>
        /// 并集
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </summary>
        public List<IDbQueryableInfo<T>> Union { get; set; }

        /// <summary>
        /// 参与表别名运算的表达式集
        /// </summary>
        public HashSet<Expression> AliasExpressions { get; set; }

        /// <summary>
        /// 初始化 <see cref="DbQueryableInfo_Select"/> 类的新实例
        /// </summary>
        public DbQueryableInfo_Select()
        {
            _join = new List<DbExpression>();
            _orderBy = new List<DbExpression>();
        }
    }
}