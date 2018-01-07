using System;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{

    public static class DbQueryableExtensions
    {
        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        public static bool Any<TSource>(this IDbQueryable<TSource> source)
        {
            return source.Any(null);
        }

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        public static bool Any<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            IDbQueryable<bool> query = source.CreateQuery<bool>(DbExpressionType.Any, predicate);
            return query.Provider.Execute(query);
        }

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        public static int Count<TSource>(this IDbQueryable<TSource> source)
        {
            return source.Count(null);
        }

        /// <summary>
        /// 返回指定序列中满足条件的元素数量
        /// </summary>
        public static int Count<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            IDbQueryable<int> query = source.CreateQuery<int>(DbExpressionType.Count, predicate);
            return query.Provider.Execute(query);
        }

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值
        /// </summary>
        public static IDbQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<System.Linq.IGrouping<TKey, TSource>>(new DbExpression
            {
                DbExpressionType = DbExpressionType.GroupBy,
                Expressions = new Expression[] { keySelector }
            });
        }

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值
        /// </summary>
        public static IDbQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            return source.CreateQuery<System.Linq.IGrouping<TKey, TElement>>(new DbExpression
            {
                DbExpressionType = DbExpressionType.GroupBy,
                Expressions = new Expression[] { keySelector, elementSelector }
            });
        }

        /// <summary>
        ///  返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        public static IDbQueryable<TSource> DefaultIfEmpty<TSource>(this IDbQueryable<TSource> source)
        {
            return source.CreateQuery<TSource>(DbExpressionType.DefaultIfEmpty);
        }

        /// <summary>
        ///  通过使用默认的相等比较器对值进行比较返回序列中的非重复元素
        /// </summary>
        public static IDbQueryable<TSource> Distinct<TSource>(this IDbQueryable<TSource> source)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Distinct);
        }

        /// <summary>
        ///  返回序列中满足指定条件的第一个元素，如果未找到这样的元素，则返回默认值
        /// </summary>
        public static TSource FirstOrDefault<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate = null)
        {
            IDbQueryable<TSource> query = source.CreateQuery<TSource>(DbExpressionType.FirstOrDefault, predicate);
            return query.Provider.Execute(query);
        }

        /// <summary>
        ///  基于键相等对两个序列的元素进行左关联并对结果进行分组
        /// </summary>
        public static IDbQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IDbQueryable<TInner>, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression
            {
                DbExpressionType = DbExpressionType.GroupJoin,
                Expressions = new Expression[] { Expression.Constant(inner), outerKeySelector, innerKeySelector, resultSelector }
            });
        }

        /// <summary>
        ///  指示查询应该包含外键
        /// </summary>
        public static IDbQueryable<TResult> Include<TResult, TProperty>(this IDbQueryable<TResult> source, Expression<Func<TResult, TProperty>> path)
        {
            throw new NotSupportedException();
            //return source.CreateQuery<TResult>(DbExpressionType.Include, path);
        }

        /// <summary>
        ///  基于匹配键对两个序列的元素进行关联。使用默认的相等比较器对键进行比较
        /// </summary>
        public static IDbQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IDbQueryable<TOuter> outer, IDbQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            return outer.CreateQuery<TResult>(new DbExpression
            {
                DbExpressionType = DbExpressionType.Join,
                Expressions = new Expression[] { Expression.Constant(inner), outerKeySelector, innerKeySelector, resultSelector }
            });
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的最大值
        /// </summary>
        public static TResult Max<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Max, selector);
            return query.Provider.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的最小值
        /// </summary>
        public static TResult Min<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Min, selector);
            return query.Provider.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的平均值
        /// </summary>
        public static TResult Average<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Average, selector);
            return query.Provider.Execute(query);
        }

        /// <summary>
        /// 返回泛型 IDbQueryable&lt;TResult&gt; 中的所有值之和
        /// </summary>
        public static TResult Sum<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            IDbQueryable<TResult> query = source.CreateQuery<TResult>(DbExpressionType.Sum, selector);
            return query.Provider.Execute(query);
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.OrderBy, keySelector);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.OrderBy, keySelector));
            //return source;
        }

        /// <summary>
        ///  根据键按升序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderBy<TSource>(this IDbQueryable<TSource> source, string ordering)
        {
            if (string.IsNullOrEmpty(ordering)) return source;

            // a.BuyDate ASC
            string[] syntaxes = ordering.Split(' ');
            string[] segs = syntaxes[0].Split('.');

            ParameterExpression parameter = Expression.Parameter(typeof(TSource), segs[0]);
            MemberExpression member = Expression.Property(parameter, segs[1]);
            LambdaExpression lambda = Expression.Lambda(member, parameter);

            DbExpressionType d = DbExpressionType.OrderBy;
            if (syntaxes.Length > 1 && (syntaxes[1] ?? string.Empty).ToUpper() == "DESC") d = DbExpressionType.OrderByDescending;

            return source.CreateQuery<TSource>(d, lambda);
            //source.DbExpressions.Add(new DbExpression(d, lambda));
            //return source;
        }

        /// <summary>
        ///  根据键按降序对序列的元素排序
        /// </summary>
        public static IDbQueryable<TSource> OrderByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.OrderByDescending, keySelector);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.OrderByDescending, keySelector));
            //return source;
        }

        /// <summary>
        ///  通过合并元素的索引将序列的每个元素投影到新表中
        /// </summary>
        public static IDbQueryable<TResult> Select<TSource, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return source.CreateQuery<TResult>(DbExpressionType.Select, selector);
        }

        /// <summary>
        ///  将序列的每个元素投影并将结果序列组合为一个序列
        /// </summary>
        public static IDbQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IDbQueryable<TSource> source, Expression<Func<TSource, IDbQueryable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            return source.CreateQuery<TResult>(new DbExpression
            {
                DbExpressionType = DbExpressionType.SelectMany,
                Expressions = new Expression[]  { collectionSelector, resultSelector }
            });
        }

        /// <summary>
        ///  跳过序列中指定数量的元素，然后返回剩余的元素
        /// </summary>
        public static IDbQueryable<TSource> Skip<TSource>(this IDbQueryable<TSource> source, int count)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Skip, Expression.Constant(count));
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.Skip, Expression.Constant(count)));
            //return source;
        }

        /// <summary>
        ///  从序列的开头返回指定数量的连续元素
        /// </summary>
        public static IDbQueryable<TSource> Take<TSource>(this IDbQueryable<TSource> source, int count)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Take, Expression.Constant(count));
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.Take, Expression.Constant(count)));
            //return source;
        }

        /// <summary>
        ///  通过使用默认的相等比较器生成两个序列的并集。
        /// </summary>
        public static IDbQueryable<TSource> Union<TSource>(this IDbQueryable<TSource> source, IDbQueryable<TSource> u)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Union, Expression.Constant(u));
        }

        /// <summary>
        ///  根据某个键按升序对序列中的元素执行后续排序
        /// </summary>
        public static IDbQueryable<TSource> ThenBy<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.ThenBy, keySelector);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.ThenBy, keySelector));
            //return source;
        }

        /// <summary>
        ///  根据某个键按降序对序列中的元素执行后续排序
        /// </summary>
        public static IDbQueryable<TSource> ThenByDescending<TSource, TKey>(this IDbQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return source.CreateQuery<TSource>(DbExpressionType.ThenByDescending, keySelector);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.ThenByDescending, keySelector));
            //return source;
        }

        /// <summary>
        ///  基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引
        /// </summary>
        public static IDbQueryable<TSource> Where<TSource>(this IDbQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return source.CreateQuery<TSource>(DbExpressionType.Where, predicate);
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.Where, predicate));
            //return source;
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TSource&gt;"/> 创建一个数组
        /// </summary>
        public static TSource[] ToArray<TSource>(this IDbQueryable<TSource> source)
        {
            return source.ToList<TSource>().ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建一个数组
        /// </summary>
        public static TElement[] ToArray<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            return source.Skip((index - 1) * pageSize).Take(pageSize).ToArray();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static List<TElement> ToList<TElement>(this IDbQueryable<TElement> source)
        {
            return source.Provider.ExecuteList(source);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="List&lt;TElement&gt;"/>
        /// </summary>
        public static List<TElement> ToList<TElement>(this IDbQueryable<TElement> source, int index, int pageSize)
        {
            if (index < 1) index = 1;
            return source.Skip((index - 1) * pageSize).Take(pageSize).ToList();
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataTable"/>
        /// </summary>
        public static DataTable ToDataTable<TElement>(this IDbQueryable<TElement> source)
        {
            string cmd = source.ToString();
            return source.Provider.ExecuteDataTable(cmd);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="DataSet"/>
        /// </summary>
        public static DataSet ToDataSet<TElement>(this IDbQueryable<TElement> source)
        {
            string cmd = source.ToString();
            return source.Provider.ExecuteDataSet(cmd);
        }

        /// <summary>
        ///  从 <see cref="IDbQueryable&lt;TElement&gt;"/> 创建 <see cref="PagedList&lt;TElement&gt;"/>
        ///  pageSize = 1024 表示取所有
        /// </summary>
        public static PagedList<TElement> ToPagedList<TElement>(this IDbQueryable<TElement> source, int pageIndex, int pageSize = 10)
        {
            IList<TElement> result = null;
            int totalCount = 0;
            int totalPages = 0;

            if (pageSize == 1024)
            {
                result = source.ToList();
                totalCount = result.Count;
                pageIndex = 1;
                totalPages = 1;
            }
            else
            {
                if (pageSize == 0) pageSize = 10;
                totalCount = source.Count();
                totalPages = totalCount / pageSize;
                if (totalCount % pageSize > 0) ++totalPages;
                if (pageIndex > totalPages) pageIndex = totalPages;
                if (pageIndex < 1) pageIndex = 1;
                result = source.ToList(pageIndex, pageSize);
            }

            return new PagedList<TElement>(result, pageIndex, pageSize, totalCount);
        }
    }
}
