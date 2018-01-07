
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 数据查询提供者 提供一系列方法用以执行数据库操作
    /// </summary>
    public abstract class DbQueryProviderBase : IDbQueryProvider
    {
        #region 嵌套类型

        /// <summary>
        /// 数据适配器，扩展Fill方法
        /// .NET的DataSet.Load方法，底层调用DataAdapter.Fill(DataTable[], IDataReader, int, int)
        /// Dapper想要返回DataSet，需要重写Load方法，不必传入DataTable[]，因为数组长度不确定
        /// </summary>
        class XLoadAdapter : DataAdapter
        {
            public XLoadAdapter()
            {
            }

            public int FillFromReader(DataSet ds, IDataReader dataReader, int startRecord, int maxRecords)
            {
                return this.Fill(ds, "Table", dataReader, startRecord, maxRecords);
            }
        }

        /// <summary>
        /// 扩展Load方法
        /// </summary>
        class XDataSet : DataSet
        {
            public override void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler handler, params DataTable[] tables)
            {
                XLoadAdapter adapter = new XLoadAdapter
                {
                    FillLoadOption = loadOption,
                    MissingSchemaAction = MissingSchemaAction.AddWithKey
                };
                if (handler != null)
                {
                    adapter.FillError += handler;
                }
                adapter.FillFromReader(this, reader, 0, 0);
                if (!reader.IsClosed && !reader.NextResult())
                {
                    reader.Close();
                }
            }
        }

        #endregion

        #region 私有字段

        private int _executeSize = 200; // 批量执行SQL时每次执行命令条数
        private string _connString = string.Empty;

        #endregion

        #region 公开属性

        /// <summary>
        /// 方法表达式访问器
        /// </summary>
        public abstract MethodCallExressionVisitorBase MethodVisitor { get; }

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public abstract char EscCharLeft { get; }

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public abstract char EscCharRight { get; }

        /// <summary>
        /// 数据源类提供者
        /// </summary>
        public DbProviderFactory DbProvider { get; private set; }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get { return _connString; } }

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public abstract string ProviderName { get; }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public abstract string ParameterPrefix { get; }

        /// <summary>
        /// 执行命令超时时间
        /// </summary>
        public int? CommandTimeout { get; set; }

        #endregion

        #region 公开事件

        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<ExceptionEventArgs> OnException;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="DbQueryProviderBase"/> 类的新实例
        /// </summary>
        /// <param name="providerFactory">数据源提供者</param>
        /// <param name="connectionString">数据库连接字符串</param>
        protected DbQueryProviderBase(DbProviderFactory providerFactory, string connectionString)
        {
            this.DbProvider = providerFactory;
            _connString = connectionString;
        }

        #endregion

        #region 接口实现

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="isOpen">是否打开连接</param>
        /// <returns></returns>
        public IDbConnection CreateConnection(bool isOpen = false)
        {
            DbConnection conn = this.DbProvider.CreateConnection();
            conn.ConnectionString = this.ConnectionString;
            if (isOpen) conn.Open();

            return conn;
        }

        /// <summary>
        /// 异步创建数据库连接
        /// </summary>
        /// <param name="isOpen">是否打开连接</param>
        /// <returns></returns>
        public async Task<IDbConnection> CreateConnectionAsync(bool isOpen = false)
        {
            DbConnection conn = this.DbProvider.CreateConnection();
            conn.ConnectionString = this.ConnectionString;
            if (isOpen) await conn.OpenAsync();

            return conn;
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="query">查询 语句</param>
        /// <returns></returns>
        public CommandDefine Parse<T>(IDbQueryable<T> query)
        {
            IDbQueryableInfo<T> info = DbQueryParser.Parse(query);

            DbQueryableInfo_Select<T> qQuery = info as DbQueryableInfo_Select<T>;
            if (qQuery != null) return this.ParseSelectCommand<T>(qQuery);

            DbQueryableInfo_Insert<T> qInsert = info as DbQueryableInfo_Insert<T>;
            if (qInsert != null) return this.ParseInsertCommand<T>(qInsert);

            DbQueryableInfo_Update<T> qUpdate = info as DbQueryableInfo_Update<T>;
            if (qUpdate != null) return this.ParseUpdateCommand<T>(qUpdate);

            DbQueryableInfo_Delete<T> qDelete = info as DbQueryableInfo_Delete<T>;
            if (qDelete != null) return this.ParseDeleteCommand<T>(qDelete);

            throw new NotImplementedException();
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="commandText">SQL 语句</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public IDbCommand CreateCommand(string commandText, IDbTransaction transaction = null, CommandType? commandType = null, IEnumerable<IDataParameter> parameters = null)
        {
            IDbCommand cmd = this.DbProvider.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandTimeout = this.CommandTimeout != null ? this.CommandTimeout.Value : 300; // 5分钟
            if (commandType != null) cmd.CommandType = commandType.Value;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }
            if (transaction != null)
            {
                cmd.Connection = transaction.Connection;
                cmd.Transaction = transaction;
            }

            return cmd;
        }

        /// <summary>
        /// 创建命令参数
        /// </summary>
        /// <returns></returns>
        public IDbDataParameter CreateParameter()
        {
            return this.DbProvider.CreateParameter();
        }

        /// <summary>
        /// 创建命令参数
        /// </summary>
        /// <param name="parameterName">存储过程名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">参数类型</param>
        /// <param name="size">参数大小</param>
        /// <param name="direction">参数方向</param>
        public IDbDataParameter CreateParameter(string parameterName, object value, DbType? dbType = null, int? size = null, ParameterDirection? direction = null)
        {
            IDbDataParameter param = this.CreateParameter();
            param.ParameterName = parameterName;
            if (dbType != null) param.DbType = dbType.Value;
            //当参数大小为0时，不使用该参数大小值
            if (size != null && size.Value > 0)
            {
                param.Size = size.Value;
            }
            else
            {
                if (direction != null && direction.Value == ParameterDirection.Output) param.Size = 100;
            }

            //创建输出类型的参数
            if (direction != null) param.Direction = direction.Value;
            if (!(direction != null && direction.Value == ParameterDirection.Output && value == null))
            {
                param.Value = value;
            }

            ///返回创建的参数
            return param;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IDbCommand cmd)
        {
            return this.DoExecute<int>(cmd, p => p.ExecuteNonQuery(), cmd.Transaction == null);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteNonQueryAsync(cmd);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回受影响的行数
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<int>(cmd, async p => await p.ExecuteNonQueryAsync(), cmd.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public object ExecuteScalar(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public object ExecuteScalar(IDbCommand cmd)
        {
            return this.DoExecute<object>(cmd, p => p.ExecuteScalar(), cmd.Transaction == null);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteScalarAsync(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<object>(cmd, async p => await p.ExecuteScalarAsync(), cmd.Transaction == null);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteReader(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public IDataReader ExecuteReader(IDbCommand cmd)
        {
            return this.DoExecute<IDataReader>(cmd, p => p.ExecuteReader(CommandBehavior.SequentialAccess), false);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteReaderAsync(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IDataReader"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd)
        {
            return await this.DoExecuteAsync<DbDataReader>(cmd, async p => await p.ExecuteReaderAsync(CommandBehavior.SequentialAccess), false);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public T Execute<T>(IDbQueryable<T> query, IDbTransaction transaction = null)
        {
            CommandDefine define = this.Parse(query);
            IDbCommand cmd = this.CreateCommand(define.CommandText, transaction, define.CommandType, define.Parameters);
            return this.Execute<T>(cmd, define);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public T Execute<T>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.Execute<T>(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="define">命令定义对象，用于解析实体的外键</param>
        /// <returns></returns>
        public T Execute<T>(IDbCommand cmd, CommandDefine define = null)
        {
            IDataReader reader = null;
            T TResult = default(T);
            IDbConnection conn = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                conn = cmd != null ? cmd.Connection : null;
                TypeDeserializer<T> deserializer = new TypeDeserializer<T>(reader, define as CommandDefine_Select);
                if (reader.Read()) TResult = deserializer.Deserialize();
                return TResult;
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="query">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(IDbQueryable<T> query, IDbTransaction transaction = null)
        {
            //string commandText = this.Parse(query).CommandText;
            CommandDefine define = this.Parse(query);
            IDbCommand cmd = this.CreateCommand(define.CommandText, transaction, define.CommandType, define.Parameters);
            return await this.ExecuteAsync<T>(cmd, define);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteAsync<T>(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回单个实体对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<T> ExecuteAsync<T>(IDbCommand cmd, CommandDefine define = null)
        {
            IDataReader reader = null;
            T TResult = default(T);
            IDbConnection conn = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;
                TypeDeserializer<T> deserializer = new TypeDeserializer<T>(reader, define as CommandDefine_Select);
                if (await (reader as DbDataReader).ReadAsync()) TResult = deserializer.Deserialize();
                return TResult;
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }
        }

        /// <summary>
        /// 执行SQL 语句，并返回新增记录时的自增ID列表
        /// <code> 批量 增/删/改 时用 </code>
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public List<int> Execute(List<string> sqlList, IDbTransaction transaction = null)
        {
            if (sqlList == null || sqlList.Count == 0) return new List<int>();

            IDbConnection conn = null;
            List<int> identitys = new List<int>();
            int pages = sqlList.Count % _executeSize == 0 ? sqlList.Count / _executeSize : (sqlList.Count / _executeSize + 1);

            for (int i = 1; i <= pages; i++)
            {
                var curSqls = sqlList.Skip((i - 1) * _executeSize).Take(_executeSize);
                string commandText = string.Join(Environment.NewLine, curSqls);
                IDbCommand cmd = this.CreateCommand(commandText, transaction);

                IDataReader reader = null;
                try
                {
                    reader = this.ExecuteReader(cmd);
                    conn = cmd != null ? cmd.Connection : null;
                    TypeDeserializer<int> deserializer = new TypeDeserializer<int>(reader, null);

                    do
                    {
                        if (reader.Read()) identitys.Add(deserializer.Deserialize());
                    }
                    while (reader.NextResult());
                }
                finally
                {
                    Dispose(cmd, reader, conn);
                }
            }

            return identitys;
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回新增记录时的自增ID列表
        /// <code> 批量 增/删/改 时用 </code>
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public async Task<List<int>> ExecuteAsync(List<string> sqlList, IDbTransaction transaction = null)
        {
            if (sqlList == null || sqlList.Count == 0) return new List<int>();

            IDbConnection conn = null;
            List<int> identitys = new List<int>();
            int pages = sqlList.Count % _executeSize == 0 ? sqlList.Count / _executeSize : (sqlList.Count / _executeSize + 1);

            for (int i = 1; i <= pages; i++)
            {
                var curSqls = sqlList.Skip((i - 1) * _executeSize).Take(_executeSize);
                string commandText = string.Join(Environment.NewLine, curSqls);
                IDbCommand cmd = this.CreateCommand(commandText, transaction);

                IDataReader reader = null;
                try
                {
                    reader = await this.ExecuteReaderAsync(cmd);
                    conn = cmd != null ? cmd.Connection : null;
                    TypeDeserializer<int> deserializer = new TypeDeserializer<int>(reader, null);

                    do
                    {
                        if (await (reader as DbDataReader).ReadAsync()) identitys.Add(deserializer.Deserialize());
                    }
                    while (reader.NextResult());
                }
                finally
                {
                    Dispose(cmd, reader, conn);
                }
            }

            return identitys;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public Tuple<List<T1>, List<T2>> ExecuteMultiple<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbTransaction transaction = null)
        {
            CommandDefine[] defines = new[] { this.Parse(query1), this.Parse(query2) };
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(defines[0].CommandText)) builder.AppendLine(defines[0].CommandText);
            if (!string.IsNullOrEmpty(defines[1].CommandText)) builder.AppendLine(defines[1].CommandText);
            string commandText = builder.ToString();
            IDbCommand cmd = this.CreateCommand(commandText, transaction);

            var result = this.ExecuteMultiple<T1, T2, NOOP, NOOP, NOOP, NOOP, NOOP>(cmd, defines);
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public async Task<Tuple<List<T1>, List<T2>>> ExecuteMultipleAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbTransaction transaction = null)
        {
            CommandDefine[] defines = new[] { this.Parse(query1), this.Parse(query2) };
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(defines[0].CommandText)) builder.AppendLine(defines[0].CommandText);
            if (!string.IsNullOrEmpty(defines[1].CommandText)) builder.AppendLine(defines[1].CommandText);
            string commandText = builder.ToString();
            IDbCommand cmd = this.CreateCommand(commandText, transaction);

            var result = await this.ExecuteMultipleAsync<T1, T2, NOOP, NOOP, NOOP, NOOP, NOOP>(cmd, defines);
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public Tuple<List<T1>, List<T2>, List<T3>> ExecuteMultiple<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3, IDbTransaction transaction = null)
        {
            CommandDefine[] defines = new[] { this.Parse(query1), this.Parse(query2), this.Parse(query3) };
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(defines[0].CommandText)) builder.AppendLine(defines[0].CommandText);
            if (!string.IsNullOrEmpty(defines[1].CommandText)) builder.AppendLine(defines[1].CommandText);
            if (!string.IsNullOrEmpty(defines[2].CommandText)) builder.AppendLine(defines[2].CommandText);
            string commandText = builder.ToString();
            IDbCommand cmd = this.CreateCommand(commandText, transaction);

            var result = this.ExecuteMultiple<T1, T2, T3, NOOP, NOOP, NOOP, NOOP>(cmd, defines);
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public async Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteMultipleAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3, IDbTransaction transaction = null)
        {
            CommandDefine[] defines = new[] { this.Parse(query1), this.Parse(query2), this.Parse(query3) };
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(defines[0].CommandText)) builder.AppendLine(defines[0].CommandText);
            if (!string.IsNullOrEmpty(defines[1].CommandText)) builder.AppendLine(defines[1].CommandText);
            if (!string.IsNullOrEmpty(defines[2].CommandText)) builder.AppendLine(defines[2].CommandText);
            string commandText = builder.ToString();
            IDbCommand cmd = this.CreateCommand(commandText, transaction);

            var result = await this.ExecuteMultipleAsync<T1, T2, T3, NOOP, NOOP, NOOP, NOOP>(cmd, defines);
            return new Tuple<List<T1>, List<T2>, List<T3>>(result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(cmd);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(cmd);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="defines">命令定义对象，用于解析实体的外键</param>
        public Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd, CommandDefine[] defines = null)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            List<T4> q4 = null;
            List<T5> q5 = null;
            List<T6> q6 = null;
            List<T7> q7 = null;

            TypeDeserializer<T1> deserializer1 = null;
            TypeDeserializer<T2> deserializer2 = null;
            TypeDeserializer<T3> deserializer3 = null;
            TypeDeserializer<T4> deserializer4 = null;
            TypeDeserializer<T5> deserializer5 = null;
            TypeDeserializer<T6> deserializer6 = null;
            TypeDeserializer<T7> deserializer7 = null;

            try
            {
                int i = 0;
                reader = this.ExecuteReader(cmd);
                conn = cmd != null ? cmd.Connection : null;

                do
                {
                    i += 1;

                    while (reader.Read())
                    {
                        switch (i)
                        {
                            #region 元组赋值

                            case 1:
                                if (deserializer1 == null) deserializer1 = new TypeDeserializer<T1>(reader, defines[i - 1] as CommandDefine_Select);
                                T1 TValue1 = deserializer1.Deserialize();
                                if (q1 == null) q1 = new List<T1>();
                                q1.Add(TValue1);

                                break;

                            case 2:
                                if (deserializer2 == null) deserializer2 = new TypeDeserializer<T2>(reader, defines[i - 1] as CommandDefine_Select);
                                T2 TValue2 = deserializer2.Deserialize();
                                if (q2 == null) q2 = new List<T2>();
                                q2.Add(TValue2);

                                break;

                            case 3:
                                if (deserializer3 == null) deserializer3 = new TypeDeserializer<T3>(reader, defines[i - 1] as CommandDefine_Select);
                                T3 TValue3 = deserializer3.Deserialize();
                                if (q3 == null) q3 = new List<T3>();
                                q3.Add(TValue3);

                                break;

                            case 4:
                                if (deserializer4 == null) deserializer4 = new TypeDeserializer<T4>(reader, defines[i - 1] as CommandDefine_Select);
                                T4 TValue4 = deserializer4.Deserialize();
                                if (q4 == null) q4 = new List<T4>();
                                q4.Add(TValue4);

                                break;

                            case 5:
                                if (deserializer5 == null) deserializer5 = new TypeDeserializer<T5>(reader, defines[i - 1] as CommandDefine_Select);
                                T5 TValue5 = deserializer5.Deserialize();
                                if (q5 == null) q5 = new List<T5>();
                                q5.Add(TValue5);

                                break;

                            case 6:
                                if (deserializer6 == null) deserializer6 = new TypeDeserializer<T6>(reader, defines[i - 1] as CommandDefine_Select);
                                T6 TValue6 = deserializer6.Deserialize();
                                if (q6 == null) q6 = new List<T6>();
                                q6.Add(TValue6);

                                break;

                            case 7:
                                if (deserializer7 == null) deserializer7 = new TypeDeserializer<T7>(reader, defines[i - 1] as CommandDefine_Select);
                                T7 TValue7 = deserializer7.Deserialize();
                                if (q7 == null) q7 = new List<T7>();
                                q7.Add(TValue7);

                                break;

                            #endregion
                        }
                    }
                }
                while (reader.NextResult());
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(q1, q2, q3, q4, q5, q6, q7);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="defines">命令定义对象，用于解析实体的外键</param>
        public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd, CommandDefine[] defines = null)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            List<T4> q4 = null;
            List<T5> q5 = null;
            List<T6> q6 = null;
            List<T7> q7 = null;

            TypeDeserializer<T1> deserializer1 = null;
            TypeDeserializer<T2> deserializer2 = null;
            TypeDeserializer<T3> deserializer3 = null;
            TypeDeserializer<T4> deserializer4 = null;
            TypeDeserializer<T5> deserializer5 = null;
            TypeDeserializer<T6> deserializer6 = null;
            TypeDeserializer<T7> deserializer7 = null;

            try
            {
                int i = 0;
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;

                do
                {
                    i += 1;
                    while (await (reader as DbDataReader).ReadAsync())
                    {
                        switch (i)
                        {
                            #region 元组赋值

                            case 1:
                                if (deserializer1 == null) deserializer1 = new TypeDeserializer<T1>(reader, defines[i - 1] as CommandDefine_Select);
                                T1 TValue1 = deserializer1.Deserialize();
                                if (q1 == null) q1 = new List<T1>();
                                q1.Add(TValue1);

                                break;

                            case 2:
                                if (deserializer2 == null) deserializer2 = new TypeDeserializer<T2>(reader, defines[i - 1] as CommandDefine_Select);
                                T2 TValue2 = deserializer2.Deserialize();
                                if (q2 == null) q2 = new List<T2>();
                                q2.Add(TValue2);

                                break;

                            case 3:
                                if (deserializer3 == null) deserializer3 = new TypeDeserializer<T3>(reader, defines[i - 1] as CommandDefine_Select);
                                T3 TValue3 = deserializer3.Deserialize();
                                if (q3 == null) q3 = new List<T3>();
                                q3.Add(TValue3);

                                break;

                            case 4:
                                if (deserializer4 == null) deserializer4 = new TypeDeserializer<T4>(reader, defines[i - 1] as CommandDefine_Select);
                                T4 TValue4 = deserializer4.Deserialize();
                                if (q4 == null) q4 = new List<T4>();
                                q4.Add(TValue4);

                                break;

                            case 5:
                                if (deserializer5 == null) deserializer5 = new TypeDeserializer<T5>(reader, defines[i - 1] as CommandDefine_Select);
                                T5 TValue5 = deserializer5.Deserialize();
                                if (q5 == null) q5 = new List<T5>();
                                q5.Add(TValue5);

                                break;

                            case 6:
                                if (deserializer6 == null) deserializer6 = new TypeDeserializer<T6>(reader, defines[i - 1] as CommandDefine_Select);
                                T6 TValue6 = deserializer6.Deserialize();
                                if (q6 == null) q6 = new List<T6>();
                                q6.Add(TValue6);

                                break;

                            case 7:
                                if (deserializer7 == null) deserializer7 = new TypeDeserializer<T7>(reader, defines[i - 1] as CommandDefine_Select);
                                T7 TValue7 = deserializer7.Deserialize();
                                if (q7 == null) q7 = new List<T7>();
                                q7.Add(TValue7);

                                break;

                            #endregion
                        }
                    }
                }
                while (reader.NextResult());
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(q1, q2, q3, q4, q5, q6, q7);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(IDbQueryable<T> query, IDbTransaction transaction = null)
        {
            CommandDefine define = this.Parse(query);
            IDbCommand cmd = this.CreateCommand(define.CommandText, transaction, define.CommandType, define.Parameters);
            return this.ExecuteList<T>(cmd, define);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteList<T>(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="define">命令定义对象，用于解析实体的外键</param>
        /// <returns></returns>
        public List<T> ExecuteList<T>(IDbCommand cmd, CommandDefine define = null)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            List<T> objList = new List<T>();

            try
            {
                reader = this.ExecuteReader(cmd);
                conn = cmd != null ? cmd.Connection : null;
                TypeDeserializer<T> deserializer = new TypeDeserializer<T>(reader, define as CommandDefine_Select);
                while (reader.Read())
                    objList.Add(deserializer.Deserialize());
                //yield return reader.ToEntity<T>();
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }

            return objList;
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(IDbQueryable<T> query, IDbTransaction transaction = null)
        {
            CommandDefine define = this.Parse(query);
            IDbCommand cmd = this.CreateCommand(define.CommandText, transaction, define.CommandType, define.Parameters);
            return await this.ExecuteListAsync<T>(cmd, define);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteListAsync<T>(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="IEnumerable"/> 对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="define">命令定义对象，用于解析实体的外键</param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteListAsync<T>(IDbCommand cmd, CommandDefine define = null)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            List<T> objList = new List<T>();

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;
                DbDataReader dbReader = reader as DbDataReader;
                TypeDeserializer<T> deserializer = new TypeDeserializer<T>(reader, define as CommandDefine_Select);

                while (true)
                {
                    bool flag = await dbReader.ReadAsync();
                    if (flag)
                        objList.Add(deserializer.Deserialize());
                    else
                        break;
                }
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }
            return objList;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteDataTable(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(IDbCommand cmd)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            DataTable table = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                conn = cmd != null ? cmd.Connection : null;
                table = new DataTable();
                table.Load(reader);
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }

            return table;
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteDataTableAsync(cmd);
        }

        /// <summary>
        /// 异步执行SQL 语句，并返回 <see cref="DataTable"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteDataTableAsync(IDbCommand cmd)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            DataTable table = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;
                table = new DataTable();
                table.Load(reader);
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }
            return table;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return this.ExecuteDataSet(cmd);
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(IDbCommand cmd)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            DataSet ds = null;

            try
            {
                reader = this.ExecuteReader(cmd);
                conn = cmd != null ? cmd.Connection : null;
                ds = new XDataSet();
                ds.Load(reader, LoadOption.OverwriteChanges, null, new DataTable[] { });
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }

            return ds;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbCommand cmd)
        {
            IDataReader reader = null;
            IDbConnection conn = null;
            DataSet ds = null;

            try
            {
                reader = await this.ExecuteReaderAsync(cmd);
                conn = cmd != null ? cmd.Connection : null;
                ds = new XDataSet();
                ds.Load(reader, LoadOption.OverwriteChanges, null, new DataTable[] { });
            }
            finally
            {
                Dispose(cmd, reader, conn);
            }
            return ds;
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="commandText">SQL 命令</param>
        /// <param name="transaction">事务</param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteDataSetAsync(string commandText, IDbTransaction transaction = null)
        {
            IDbCommand cmd = this.CreateCommand(commandText, transaction);
            return await this.ExecuteDataSetAsync(cmd);
        }

        #endregion

        #region 私有函数

        // 执行 SQL 命令
        protected T DoExecute<T>(IDbCommand cmd, Func<IDbCommand, T> func, bool wasClosed)
        {
            // 如果返回DataReader，则需要在关闭DataReader后再释放链接

            IDbConnection conn = cmd.Connection;
            T TResult = default(T);
            try
            {
                if (conn == null)
                {
                    conn = this.CreateConnection();
                    cmd.Connection = conn;
                }
                if (conn.State != ConnectionState.Open) conn.Open();

                TResult = func(cmd);
            }
            catch (DbException e)
            {
                if (this.OnException != null) this.OnException(this, new ExceptionEventArgs(cmd, e));
                throw;
            }
            catch (Exception e)
            {
                if (this.OnException != null) this.OnException(this, new ExceptionEventArgs(cmd, e));
                throw;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (wasClosed)
                {
                    if (conn != null) conn.Close();
                    if (conn != null) conn.Dispose();
                }
            }

            return TResult;
        }

        // 执行 SQL 命令
        protected async Task<T> DoExecuteAsync<T>(IDbCommand cmd, Func<DbCommand, Task<T>> func, bool wasClosed)
        {
            // 如果返回DataReader，则需要在关闭DataReader后再释放链接

            IDbConnection conn = cmd.Connection;
            T TResult = default(T);
            try
            {
                if (conn == null)
                {
                    conn = await this.CreateConnectionAsync();
                    cmd.Connection = conn;
                }
                if (conn.State != ConnectionState.Open) await (conn as DbConnection).OpenAsync();

                TResult = await func(cmd as DbCommand);
            }
            catch (DbException e)
            {
                if (this.OnException != null) this.OnException(this, new ExceptionEventArgs(cmd, e));
                throw;
            }
            catch (Exception e)
            {
                if (this.OnException != null) this.OnException(this, new ExceptionEventArgs(cmd, e));
                throw;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (wasClosed)
                {
                    if (conn != null) conn.Close();
                    if (conn != null) conn.Dispose();
                }
            }

            return TResult;
        }

        // 创建 SELECT 命令
        protected abstract CommandDefine ParseSelectCommand<T>(DbQueryableInfo_Select<T> qQuery, int indent = 0);

        // 创建 INSRT 命令
        protected abstract CommandDefine ParseInsertCommand<T>(DbQueryableInfo_Insert<T> qInsert);

        // 创建 DELETE 命令
        protected abstract CommandDefine ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> qDelete);

        // 创建 UPDATE 命令
        protected abstract CommandDefine ParseUpdateCommand<T>(DbQueryableInfo_Update<T> qUpdate);

        // 获取 JOIN 子句关联表的的别名
        protected TableAliasCache PrepareAlias<T>(DbQueryableInfo_Select<T> query)
        {
            TableAliasCache aliases = new TableAliasCache((query.Join != null ? query.Join.Count : 0) + 1);
            foreach (DbExpression exp in query.Join)
            {
                // [INNER/LEFT JOIN]
                if (exp.DbExpressionType == DbExpressionType.GroupJoin || exp.DbExpressionType == DbExpressionType.Join)
                    this.GetLfInJoinAlias(exp, aliases);
                else if (exp.DbExpressionType == DbExpressionType.SelectMany)
                    this.GetCrossJoinAlias(exp, aliases);
            }

            return aliases;
        }

        // 获取 LEFT JOIN / INNER JOIN 子句关联表的的别名
        private void GetLfInJoinAlias(DbExpression exp, TableAliasCache aliases)
        {
            Type type = exp.Expressions[0].Type.GetGenericArguments()[0];
            string name = TypeRuntimeInfoCache.GetRuntimeInfo(type).TableName;

            // on a.Name equals b.Name 或 on new{ Name = a.Name,Id=a.Id } equals new { Name = b.Name,Id=b.Id }
            LambdaExpression left = exp.Expressions[1] as LambdaExpression;
            LambdaExpression right = exp.Expressions[2] as LambdaExpression;
            NewExpression body1 = left.Body as NewExpression;
            if (body1 == null)
            {
                aliases.GetTableAlias(exp.Expressions[1]);
                string alias = aliases.GetTableAlias(exp.Expressions[2]);

                // 记录显示指定的LEFT JOIN 表别名
                aliases.AddOrUpdateJoinTableAlias(name, alias);
            }
            else
            {
                NewExpression body2 = right.Body as NewExpression;
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    aliases.GetTableAlias(body1.Arguments[index]);
                    string alias = aliases.GetTableAlias(body2.Arguments[index]);

                    // 记录显示指定的LEFT JOIN 表别名
                    aliases.AddOrUpdateJoinTableAlias(name, alias);
                }
            }
        }

        // 获取 CROSS JOIN 子句关联表的的别名
        private void GetCrossJoinAlias(DbExpression exp, TableAliasCache aliases)
        {
            LambdaExpression lambdaExp = exp.Expressions[1] as LambdaExpression;
            for (int index = 0; index < lambdaExp.Parameters.Count; ++index)
            {
                aliases.GetTableAlias(lambdaExp.Parameters[index]);
            }
        }

        // 尝试释放资源
        private static void Dispose(IDbCommand cmd = null, IDataReader reader = null, IDbConnection conn = null)
        {
            if (cmd != null) cmd.Dispose();
            if (reader != null) reader.Dispose();
            if (cmd != null && cmd.Transaction == null)
            {
                // 没有使用事务，则马上释放资源 ，否则在事务调用方释放
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();
            }
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 空类
        /// </summary>
        class NOOP { }

        /// <summary>
        /// 包含异常事件数据的类
        /// </summary>
        public class ExceptionEventArgs : EventArgs
        {
            /// <summary>
            /// 产生异常的SQL命令
            /// </summary>
            public IDbCommand Command { get; set; }

            /// <summary>
            /// 异常来源
            /// </summary>
            public Exception Exception { get; set; }

            public ExceptionEventArgs(IDbCommand command, Exception src)
                : base()
            {
                this.Command = command;
                this.Exception = src;
            }
        }

        #endregion
    }


}
