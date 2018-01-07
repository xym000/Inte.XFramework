using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    internal sealed class DbQueryParser
    {
        public static IDbQueryableInfo<TElement> Parse<TElement>(IDbQueryable<TElement> query, int start = 0)
        {
            // 目的：将query 转换成增/删/改/查

            // 1、from a in context.GetTable<T>() select a 此时query里面可能没有SELECT 表达式
            // 2、Take 视为一个查询的结束位，如有更多查询，应使用嵌套查询

            Type type = null;
            bool isDistinct = false;
            bool isAny = false;
            int? skip = null;
            int? take = null;
            int? outer = null;
            List<Expression> where = new List<Expression>();                  // WHERE
            List<Expression> having = new List<Expression>();                 // HAVING
            List<DbExpression> join = new List<DbExpression>();               // JOIN
            List<DbExpression> orderBy = new List<DbExpression>();            // ORDER BY
            List<IDbQueryableInfo<TElement>> union = new List<IDbQueryableInfo<TElement>>();

            Expression select = null;       // SELECT #
            DbExpression insert = null;     // INSERT #
            DbExpression update = null;     // UPDATE #
            DbExpression delete = null;     // DELETE #
            DbExpression groupBy = null;    // GROUP BY #
            DbExpression statis = null;     // MAX/MIN... #

            //HashSet<Expression> aliasExpressions = new HashSet<Expression>();

            for (int index = start; index < query.DbExpressions.Count; ++index)
            {
                DbExpression curExp = query.DbExpressions[index];

                // Take(n)
                if (take != null)
                {
                    outer = index;
                    break;
                }
                //if (skip != null && (index == query.DbExpressions.Count - 1 || query.DbExpressions[index + 1].DbExpressionType != DbExpressionType.Take))
                if (skip != null && curExp.DbExpressionType != DbExpressionType.Take)
                {
                    outer = index;
                    break;
                }

                switch (curExp.DbExpressionType)
                {
                    case DbExpressionType.None:
                    case DbExpressionType.All:
                        continue;

                    case DbExpressionType.Any:
                        isAny = true;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        break;

                    case DbExpressionType.Union:
                        var uQuery = (curExp.Expressions[0] as ConstantExpression).Value as IDbQueryable<TElement>;
                        var u = DbQueryParser.Parse(uQuery);
                        union.Add(u);
                        continue;

                    case DbExpressionType.GroupBy:
                        groupBy = curExp;
                        continue;

                    case DbExpressionType.GetTable:
                        type = (curExp.Expressions[0] as ConstantExpression).Value as Type;
                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        statis = curExp;
                        continue;

                    case DbExpressionType.Count:
                        statis = curExp;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.GroupJoin:
                    case DbExpressionType.Join:
                        select = curExp.Expressions[3];
                        join.Add(curExp);
                        //aliasExpressions.Add(select);
                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        orderBy.Add(curExp);
                        continue;
                    case DbExpressionType.Select:
                        select = curExp.Expressions[0];
                        //aliasExpressions.Add(select);
                        continue;

                    case DbExpressionType.SelectMany:
                        select = curExp.Expressions[1];
                        if (!curExp.Expressions[0].IsAnonymous()) join.Add(curExp);
                        //aliasExpressions.Add(select);
                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(curExp.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(curExp.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        orderBy.Add(curExp);
                        continue;

                    case DbExpressionType.Where:
                        var predicate = groupBy == null ? where : having;
                        if (curExp.Expressions != null) predicate.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Insert:
                        insert = curExp;
                        continue;

                    case DbExpressionType.Update:
                        update = curExp;
                        continue;

                    case DbExpressionType.Delete:
                        delete = curExp;
                        continue;

                    default:
                        throw new NotSupportedException(string.Format("{0} is not support.", curExp.DbExpressionType));
                }
            }

            // 没有解析到INSERT/DELETE/UPDATE/SELECT表达式，并且没有相关统计函数，则默认选择FromType的所有字段
            bool useAllColumn = insert == null && delete == null && update == null && select == null && statis == null;
            if (useAllColumn)
            {
                select = Expression.Constant(type ?? typeof(TElement));
                //aliasExpressions.Add(select);
                //移到 GetTabl 中去 
            }

            var qQuery = new DbQueryableInfo_Select<TElement>();
            qQuery.FromType = type;
            qQuery.Expression = new DbExpression(DbExpressionType.Select, select);
            qQuery.HaveDistinct = isDistinct;
            qQuery.HaveAny = isAny;
            qQuery.Join = join;
            qQuery.OrderBy = orderBy;
            qQuery.GroupBy = groupBy;
            qQuery.Skip = skip != null ? skip.Value : 0;
            qQuery.Take = take != null ? take.Value : 0;
            qQuery.Where = new DbExpression(DbExpressionType.Where, DbQueryParser.Combine(where));
            qQuery.Having = new DbExpression(DbExpressionType.None, DbQueryParser.Combine(having));
            qQuery.Statis = statis;
            qQuery.Union = union;
            //qQuery.AliasExpressions = aliasExpressions;

            if (update != null)
            {
                var qUpdate = new DbQueryableInfo_Update<TElement>();
                ConstantExpression expression2 = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (expression2 != null)
                    qUpdate.Entity = expression2.Value;
                else
                    qUpdate.Expression = update.Expressions[0];
                qUpdate.SelectInfo = qQuery;
                return qUpdate;
            }
            if (delete != null)
            {
                var qDelete = new DbQueryableInfo_Delete<TElement>();
                ConstantExpression expression2 = delete.Expressions != null ? delete.Expressions[0] as ConstantExpression : null;
                if (expression2 != null)
                    qDelete.Entity = expression2.Value;
                qDelete.SelectInfo = qQuery;
                return qDelete;
            }

            if (insert != null)
            {
                var qInsert = new DbQueryableInfo_Insert<TElement>();
                if (insert.Expressions != null) qInsert.Entity = (insert.Expressions[0] as ConstantExpression).Value;
                qInsert.SelectInfo = qQuery;
                query.DbQueryInfo = qInsert;
                qInsert.Bulk = query.Bulk;
                return qInsert;
            }

            if (outer != null)
            {
                // 解析嵌套查询
                var qOuter = (DbQueryableInfo_Select<TElement>)DbQueryParser.Parse<TElement>(query, outer.Value);
                qOuter.Subquery = qQuery;
                return qOuter;
            }

            // 查询表达式
            return qQuery;
        }

        private static Expression Combine(IList<Expression> predicates)
        {
            if (predicates.Count == 0) return null;

            Expression body = ((LambdaExpression)predicates[0].ReduceUnary()).Body;
            for (int i = 1; i < predicates.Count; i++)
            {
                Expression expression = predicates[i];
                if (expression != null) body = Expression.And(body, ((LambdaExpression)expression.ReduceUnary()).Body);
            }
            return body;

        }
    }
}
