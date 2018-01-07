using System;
using System.Data;

namespace Inte.XFramework.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 对应到数据库中的列名
        /// </summary>
        ///public string Name { get; set; }

        /// <summary>
        /// 是否为自增列
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// 是否是主键列
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// 标志该属性不是主表字段
        /// 用途：
        /// 1. 生成 INSERT/UPDATE 语句时忽略此字段
        /// 2. 生成不指定具体字段的 SELECT 语句时忽略此字段
        /// </summary>
        public bool NoMapped { get; set; }

        /// <summary>
        /// 数据库字段的数据类型
        /// </summary>
        public DbType DbType { get; set; }
    }
}
