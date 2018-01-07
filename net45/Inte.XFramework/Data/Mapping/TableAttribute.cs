﻿using System;

namespace Inte.XFramework.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// 映射到数据库的列表
        /// </summary>
        public string Name { get; set; }
    }
}
