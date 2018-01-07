﻿
namespace Inte.XFramework
{
    /// <summary>
    /// 分页列表接口
    /// </summary>
    public interface IPagedList
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// 页长
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// 记录总数
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// 总页数
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// 能否进行上一次查询
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// 能否进行下一页查询
        /// </summary>
        bool HasNextPage { get; }
    }
}
