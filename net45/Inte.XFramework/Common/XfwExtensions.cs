using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Inte.XFramework
{
    public static class XfwExtensions
    {
        #region 表达式树

        /// <summary>
        /// 返回真表达式
        /// </summary>
        public static Expression<Func<T, bool>> True<T>()
            where T : class
        {
            return f => true;
        }

        /// <summary>
        /// 返回假表达式
        /// </summary>
        public static Expression<Func<T, bool>> False<T>()
            where T : class
        {
            return f => false;
        }

        /// <summary>
        /// 拼接真表达式
        /// </summary>
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> TExp1,
            Expression<Func<T, bool>> TExp2) where T : class
        {
            if (TExp1 == null) return TExp2;
            if (TExp2 == null) return TExp1;

            var invokeExp = System.Linq.Expressions.Expression.Invoke(TExp2, TExp1.Parameters.Cast<System.Linq.Expressions.Expression>());
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>
                  (System.Linq.Expressions.Expression.AndAlso(TExp1.Body, invokeExp), TExp1.Parameters);
        }

        /// <summary>
        /// 拼接假表达式
        /// </summary>
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> TExp1,
            Expression<Func<T, bool>> TExp2) where T : class
        {
            if (TExp1 == null) return TExp2;
            if (TExp2 == null) return TExp1;

            var invokeExp = System.Linq.Expressions.Expression.Invoke(TExp2, TExp1.Parameters.Cast<System.Linq.Expressions.Expression>());
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>
                  (System.Linq.Expressions.Expression.OrElse(TExp1.Body, invokeExp), TExp1.Parameters);
        }

        /// <summary>
        /// reduce unaryExpression
        /// </summary>
        /// <returns></returns>
        public static Expression ReduceUnary(this Expression exp)
        {
            UnaryExpression unaryExpression = exp as UnaryExpression;
            return unaryExpression != null
                ? unaryExpression.Operand.ReduceUnary()
                : exp;
        }


        #endregion

        #region 列表扩展

        /// <summary>
        /// 取指定列表中符合条件的元素索引
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int i = -1;
            foreach (T value in collection)
            {
                i++;
                if (predicate(value)) return i;
            }

            return -1;
        }

        /// <summary>
        /// 创建一个集合
        /// </summary>
        public static List<TResult> ToList<T,TResult>(this IEnumerable<T> collection, Func<T, TResult> selector)
        {
            return collection.Select(selector).ToList();
        }

        #endregion
    }
}
