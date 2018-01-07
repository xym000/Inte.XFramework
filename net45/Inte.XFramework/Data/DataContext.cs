
using System;
using System.Data;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public class DataContext : IDisposable
    {
        #region 私有字段

        private readonly List<object> _dbQueryables = new List<object>();
        private readonly object oLock = new object();
        private IDbQueryProvider _provider;

        #endregion

        #region 公开属性

        /// <summary>
        /// <see cref="IDbQueryable"/> 的解析执行提供程序
        /// </summary>
        public IDbQueryProvider Provider
        {
            get { return _provider; }
            set { _provider = value; }
        }

        /// <summary>
        /// 执行命令超时时间
        /// </summary>
        public int CommandTimeout
        {
            get { return _provider.CommandTimeout ?? 0; }
            set { _provider.CommandTimeout = value; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="DataContext"/> 类的新实例
        /// </summary>
        public DataContext()
            : this(XfwContainer.Default.Resolve<IDbQueryProvider>())
        {
        }

        /// <summary>
        /// 使用提供程序初始化 <see cref="DataContext"/> 类的新实例
        /// </summary>
        public DataContext(IDbQueryProvider provider)
        {
            _provider = provider;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 新增记录
        /// </summary>
        public virtual void Insert<T>(T TEntity)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            table.DbExpressions.Add(new DbExpression 
            {
                DbExpressionType = DbExpressionType.Insert,
                Expressions = new [] { Expression.Constant(TEntity) }
            });            
            
            lock (this.oLock)
                _dbQueryables.Add(table);
        }

        /// <summary>
        /// 批量新增记录
        /// </summary>
        public virtual void Insert<T>(IEnumerable<T> collection)
        {
            List<IDbQueryable> bulkList = new List<IDbQueryable>();
            foreach (T value in collection)
            {
                IDbQueryable<T> table = this.GetTable<T>();
                table.DbExpressions.Add(new DbExpression
                {
                    DbExpressionType = DbExpressionType.Insert,
                    Expressions = new[] { Expression.Constant(value) }
                });

                bulkList.Add(table);
            }

            lock (this.oLock)
                _dbQueryables.Add(bulkList);
        }

        /// <summary>
        /// 批量新增记录
        /// </summary>
        public void Insert<T>(IDbQueryable<T> query)
        {
            //IDbQueryable<T> source = query;
            //source.DbExpressions.Add(new DbExpression(DbExpressionType.Insert));
            query = query.CreateQuery<T>(new DbExpression(DbExpressionType.Insert));
            lock (this.oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(T TEntity)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            table.DbExpressions.Add(new DbExpression 
            {
                 DbExpressionType = DbExpressionType.Delete,
                 Expressions = new[] { Expression.Constant(TEntity) }
            });
            lock (this.oLock)
                _dbQueryables.Add(table);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(Expression<Func<T, bool>> predicate)
        {
            this.Delete<T>(p => p.Where(predicate));
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression(DbExpressionType.Delete));
            lock (this.oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T>(T TEntity)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            table.DbExpressions.Add(new DbExpression 
            {
                DbExpressionType = DbExpressionType.Update, 
                Expressions = new[] { Expression.Constant(TEntity) }
            });
            lock (this.oLock)
                _dbQueryables.Add(table);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T>(Expression<Func<T, T>> action, Expression<Func<T, bool>> predicate)
        {
            this.Update<T>(action, p => p.Where(predicate));
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T>(Expression<Func<T, T>> action, IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression 
            {
                DbExpressionType = DbExpressionType.Update,
                Expressions = new[] { action }
            });
            lock (this.oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public void Update<T, TFrom>(Expression<Func<T, TFrom, T>> action, IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression
            {
                DbExpressionType = DbExpressionType.Update,
                Expressions = new[] { action }
            });
            lock (this.oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public async Task<int> SubmitChangesAsync()
        {
            int count = _dbQueryables.Count;
            if (count == 0) return 0;

            IDbConnection conn = null;
            IDbTransaction trans = null;

            try
            {

                List<string> sqlList = this.Resolve(false);
                conn = await _provider.CreateConnectionAsync(true);
                if (count > 1) trans = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                var result = await _provider.ExecuteAsync(sqlList, trans);
                this.SetIdentity(result);

                if (trans != null) trans.Commit();
            }
            catch
            {
                if (trans != null) trans.Rollback();
                throw;
            }
            finally
            {
                if (trans != null) trans.Dispose();
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();
                this.Dispose();
            }

            return count;
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public int SubmitChanges()
        {
            int count = _dbQueryables.Count;
            if (count == 0) return 0;

            IDbConnection conn = null;
            IDbTransaction trans = null;

            try
            {
                List<string> sqlList = this.Resolve(false);
                conn = _provider.CreateConnection(true);
                if (count > 1) trans = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                var result = _provider.Execute(sqlList, trans);
                this.SetIdentity(result);

                if (trans != null) trans.Commit();
            }
            catch
            {
                if (trans != null) trans.Rollback();
                throw;
            }
            finally
            {
                if (trans != null) trans.Dispose();
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();
                this.Dispose();
            }

            return count;
        }

        /// <summary>
        /// 返回影响的行数
        /// </summary>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbTransaction trans = null)
        {
            if (_dbQueryables.Count != 1) throw new XfwException("Only one 'IDbQueryable' accept.");

            try
            {
                List<string> sqlList = this.Resolve(false);
                return _provider.ExecuteNonQuery(sqlList[0], trans);
            }
            catch
            {
                if (trans != null) trans.Rollback();
                throw;
            }
            finally
            {
                this.Dispose();
            }
        }

        /// <summary>
        /// 释放所有的更改
        /// </summary>
        public void DiscardChanges()
        {
            lock (this.oLock)
                this._dbQueryables.Clear();
        }

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// </summary>
        public IDbQueryable<T> GetTable<T>()
        {
            DbQueryable<T> queryable = new DbQueryable<T> { Provider = this.Provider };
            queryable.DbExpressions = new List<DbExpression> { new DbExpression 
            {
                DbExpressionType = DbExpressionType.GetTable, 
                Expressions = new[] { Expression.Constant(typeof(T)) }
            } };
            return queryable;
        }

        /// <summary>
        /// 释放由 <see cref="DataContext"/> 类的当前实例占用的所有资源
        /// </summary>
        public void Dispose()
        {
            this.DiscardChanges();
        }

        /// <summary>
        /// 将 IDbQueryable&lt;T&gt;对象解析成 SQL 脚本
        /// </summary>
        /// <returns></returns>
        public List<string> Resolve(bool clear = true)
        {
            List<string> sqlList = new List<string>();
            int count = _dbQueryables.Count;

            try
            {
                if (count != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        object obj = _dbQueryables[i];
                        if (obj is IDbQueryable)
                        {
                            string cmd = _dbQueryables[i].ToString();
                            sqlList.Add(cmd);
                        }
                        else
                        {
                            // 解析批量插入操作
                            List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                            this.ResolveBulk(sqlList, bulkList);
                        }
                    }
                }
            }
            finally
            {
               if(clear) this.Dispose();
            }

            return sqlList;
        }

        #endregion

        #region 私有函数

        //更新记录
        private void Update<T>(Expression<Func<T, T>> action, Func<IDbQueryable<T>, IDbQueryable<T>> predicate)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            IDbQueryable<T> query = predicate(table);
            this.Update<T>(action, query);
        }

        // 删除记录
        private void Delete<T>(Func<IDbQueryable<T>, IDbQueryable<T>> predicate)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            this.Delete<T>(predicate(table));
        }

        // 更新自增列
        private void SetIdentity(List<int> identitys)
        {
            if (identitys == null || identitys.Count == 0) return;

            int index = -1;
            foreach (var obj in _dbQueryables)
            {
                IDbQueryable query = obj as IDbQueryable;
                if (query == null) continue;

                var info = query.DbQueryInfo as IDbQueryableInfo_Insert;
                if (info != null && info.Entity != null && info.AutoIncrement != null)
                {
                    index += 1;
                    int identity = identitys[index];
                    info.AutoIncrement.Set(info.Entity, identity);
                }
            }
        }

        private void ResolveBulk(List<string> sqlList, List<IDbQueryable> bulkList)
        {
            // SQL 只能接收1000个
            int pageSize = 1000;
            int page = bulkList.Count % pageSize == 0 ? bulkList.Count / pageSize : (bulkList.Count / pageSize + 1);
            for (int pageIndex = 1; pageIndex <= page; pageIndex++)
            {
                var dbQueryables = bulkList.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                int i = 0;
                int t = dbQueryables.Count();
                var builder = new System.Text.StringBuilder(128);

                foreach (IDbQueryable q in dbQueryables)
                {
                    i += 1;
                    q.Bulk = new BulkInfo { OnlyValue = i != 1, IsOver = i == t };

                    string cmd = q.ToString();
                    builder.Append(cmd);
                }

                if (builder.Length > 0) sqlList.Add(builder.ToString());
            }
        }

        #endregion
    }
}
