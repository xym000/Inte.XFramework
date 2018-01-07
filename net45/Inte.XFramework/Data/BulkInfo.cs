using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 批量插入信息
    /// </summary>
    public class BulkInfo
    {
        /// <summary>
        /// 标志在解析SQL时，是否仅解析 VALUE
        /// 此属性主要用于批量插入数据
        /// </summary>
        public bool OnlyValue { get; set; }

        /// <summary>
        /// 标志在解析批量插入时是否是结束位
        /// </summary>
        public bool IsOver { get; set; }
    }
}
