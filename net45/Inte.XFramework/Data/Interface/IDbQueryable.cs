
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 提供对未指定数据类型的特定数据源的查询进行计算的功能
    /// </summary>
    public interface IDbQueryable
    {
        /// <summary>
        /// 数据查询提供者
        /// </summary>
        IDbQueryProvider Provider { get; set; }

        /// <summary>
        /// 查询表达式
        /// </summary>
        IList<DbExpression> DbExpressions { get; set; }

        /// <summary>
        /// 转换后的查询对象
        /// </summary>
        IDbQueryableInfo DbQueryInfo { get; set; }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        BulkInfo Bulk { get; set; }
    }
}
