using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 数据查询
    /// </summary>
    public class DbQueryable<TElement> : IDbQueryable<TElement>, IDbQueryable
    {
        private IDbQueryProvider _provider = null;
        private IList<DbExpression> _dbExpressions = null;
        private IDbQueryableInfo _dbQueryInfo = null;
        private string cmd = null;

        /// <summary>
        /// 数据查询提供者
        /// </summary>
        public IDbQueryProvider Provider
        {
            get { return _provider; }
            set { _provider = value; }
        }

        /// <summary>
        /// 查询表达式
        /// </summary>
        public IList<DbExpression> DbExpressions
        {
            get { return _dbExpressions; }
            set { _dbExpressions = value; }
        }

        /// <summary>
        /// 转换后的查询对象
        /// </summary>
        public IDbQueryableInfo DbQueryInfo
        {
            get { return _dbQueryInfo; }
            set { _dbQueryInfo = value; }
        }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        public BulkInfo Bulk { get; set; }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpressionType dbExpressionType, System.Linq.Expressions.Expression expression = null)
        {
            return this.CreateQuery<TResult>(new DbExpression
            {
                DbExpressionType = dbExpressionType,
                Expressions = expression != null ? new[] { expression } : null
            });
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpression exp = null)
        {
            IDbQueryable<TResult> query = new DbQueryable<TResult>
            {
                Provider = _provider,
                DbExpressions = new List<DbExpression>(_dbExpressions)
            };

            if (exp != null) query.DbExpressions.Add(exp);
            return query;
        }

        public override string ToString()
        {
            //if (cmd == null) 
                cmd = _provider.Parse(this).CommandText;
            return cmd;
        }

        public Func<object[], TOut> Compily<TOut>()
        {
            throw new NotImplementedException();
            //IDbCommand cmd = this.Provider.Parse(this).CreateCommand();
            //return args =>
            //{
            //    for (int index = 0; index < cmd.Parameters.Count; ++index)
            //        ((IDataParameter)cmd.Parameters[index]).Value = args[index];
            //    return this.Provider.Database.Execute<TOut>(null, cmd);
            //};
        }
    }
}
