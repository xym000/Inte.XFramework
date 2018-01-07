using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// JOIN 表达式解析器
    /// </summary>
    public class JoinExpressionVisitor : ExpressionVisitorBase
    {
        private List<DbExpression> _qJoin = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="JoinExpressionVisitor"/> 类的新实例
        /// </summary>
        public JoinExpressionVisitor(DbQueryProviderBase provider, TableAliasCache aliases, List<DbExpression> qJoin)
            : base(provider, aliases, null, false)
        {
            _qJoin = qJoin;
            _aliases = aliases;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(SqlBuilder builder)
        {
            foreach(DbExpression qj in _qJoin)
            {
                builder.AppendNewLine();

                // [INNER/LEFT JOIN]
                if (qj.DbExpressionType == DbExpressionType.GroupJoin || qj.DbExpressionType == DbExpressionType.Join)
                {
                    JoinType joinType = qj.DbExpressionType == DbExpressionType.Join
                        ? JoinType.InnerJoin
                        : JoinType.LeftJoin;
                    this.AppendJoinType(builder, joinType);
                    this.AppendLfInJoin(builder, qj, _aliases);
                }
                else if (qj.DbExpressionType == DbExpressionType.SelectMany)
                {
                    this.AppendJoinType(builder, JoinType.CrossJoin);
                    this.AppendCrossJoin(builder, qj, _aliases);
                } 
            }
        }

        private void AppendJoinType(SqlBuilder builder, JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    builder.Append("INNER JOIN");
                    break;
                case JoinType.LeftJoin:
                    builder.Append("LEFT JOIN");
                    break;
                case JoinType.CrossJoin:
                    builder.Append("CROSS JOIN");
                    break;
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(SqlBuilder builder, DbExpression exp, TableAliasCache aliases)
        {
            // [TableName]
            //var constExp = exp.Expressions[0] as ConstantExpression;
            //constExp.Type.GetGenericArguments()[0]
            //var queryable = constExp.Value as IDbQueryable;
            //var innerExp = queryable.DbExpressions[0];
            //if (innerExp.DbExpressionType != DbExpressionType.GetTable) throw new XfwException("inner expression must be GetTable<T> expression");
            //Type type = (innerExp.Expression as ConstantExpression).Value as Type;

            Type type = exp.Expressions[0].Type.GetGenericArguments()[0];
            builder.Append(' ');
            builder.AppendMember(TypeRuntimeInfoCache.GetRuntimeInfo(type).TableName);

            LambdaExpression left = exp.Expressions[1] as LambdaExpression;
            LambdaExpression right = exp.Expressions[2] as LambdaExpression;
            NewExpression body1 = left.Body as NewExpression;
            NewExpression body2 = right.Body as NewExpression;

            // t0(t1)
            string alias = body1 == null
                ? aliases.GetTableAlias(exp.Expressions[2])
                : aliases.GetTableAlias(body2.Arguments[0]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            // ON a.Name = b.Name AND a.Id = b.Id
            builder.Append("ON ");

            if (body1 == null)
            {
                builder.AppendMember(aliases, left.Body.ReduceUnary());
                builder.Append(" = ");
                builder.AppendMember(aliases, right.Body.ReduceUnary());
            }
            else
            {
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    builder.AppendMember(aliases, body1.Arguments[index]);
                    builder.Append(" = ");
                    builder.AppendMember(aliases, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                }
            }
        }

        // Cross Join
        private void AppendCrossJoin(SqlBuilder builder, DbExpression exp, TableAliasCache aliases)
        {
            LambdaExpression lambdaExp = exp.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            builder.Append(' ');
            builder.AppendMember(TypeRuntimeInfoCache.GetRuntimeInfo(type).TableName);

            string alias = aliases.GetTableAlias(lambdaExp.Parameters[1]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');
        }


        /// <summary>
        /// 关联类型
        /// </summary>
        enum JoinType
        {
            /// <summary>
            /// 内关联
            /// </summary>
            InnerJoin,

            /// <summary>
            /// 左关联
            /// </summary>
            LeftJoin,

            /// <summary>
            /// 全关联
            /// </summary>
            CrossJoin,
        }
    }
}
