
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;

namespace Inte.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    /// <remarks>
    /// 2、表别名根据Lambda表达式的参数取得
    /// </remarks>
    public sealed class DbQueryProvider : DbQueryProviderBase
    {
        private static readonly string NOLOCK = string.Empty;
        private MethodCallExressionVisitor _methodVisitor = null;

        /// <summary>
        /// 方法表达式访问器
        /// </summary>
        public override MethodCallExressionVisitorBase MethodVisitor
        {
            get
            {
                _methodVisitor = _methodVisitor ?? new MethodCallExressionVisitor();
                return _methodVisitor;
            }
        }

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override char EscCharLeft
        {
            get
            {
                return '[';
            }
        }

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override char EscCharRight
        {
            get
            {
                return ']';
            }
        }

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public override string ProviderName
        {
            get
            {
                return "SqlClient";
            }
        }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix
        {
            get
            {
                return "@";
            }
        }

        /// <summary>
        /// 初始化 <see cref="DbQueryProvider"/> 类的新实例
        /// </summary>
        public DbQueryProvider()
            : this(XfwCommon.ConnString)
        { }

        /// <summary>
        /// 初始化 <see cref="DbQueryProvider"/> 类的新实例
        /// </summary>
        /// <param name="connString">数据库连接字符串</param>
        public DbQueryProvider(string connString)
            : base(SqlClientFactory.Instance, connString)
        {
        }

        // 创建 SELECT 命令
        protected override CommandDefine ParseSelectCommand<T>(DbQueryableInfo_Select<T> qQuery, int indent = 0)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有统计函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询

            bool willNest = qQuery.HaveDistinct || qQuery.GroupBy != null || qQuery.Skip > 0 || qQuery.Take > 0;
            bool useStatis = qQuery.Statis != null;
            bool groupByPaging = qQuery.GroupBy != null && qQuery.Skip > 0;         // 分组分页      
            bool useOrderBy = (!useStatis || qQuery.Skip > 0) && !qQuery.HaveAny;   // 没有统计函数或者使用 'Skip' 子句，则解析OrderBy

            ExpressionVisitorBase visitor = null;
            TableAliasCache aliases = this.PrepareAlias<T>(qQuery);
            string statName = string.Empty;

            CommandDefine_Select sc = new CommandDefine_Select(this.EscCharLeft, this.EscCharRight, aliases);
            SqlBuilder jf = sc.JoinFragment;
            SqlBuilder wf = sc.WhereFragment;
            
            if (groupByPaging) indent = indent + 1;
            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && willNest)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                visitor = new StatisExpressionVisitor(this, aliases, qQuery.Statis, qQuery.GroupBy, "t0");
                visitor.Write(jf);
                statName = (visitor as StatisExpressionVisitor).ColumnName;
                sc.AddNavigation(visitor.Navigations);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
            }

            #endregion

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();

            if (qQuery.HaveAny)
            {
                jf.Append("IF EXISTS(");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
            }

            jf.Append("SELECT ");

            if (useStatis && !willNest)
            {
                // 如果有统计函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                visitor = new StatisExpressionVisitor(this, aliases, qQuery.Statis, qQuery.GroupBy);
                visitor.Write(jf);
                sc.AddNavigation(visitor.Navigations);
            }
            else
            {

                // DISTINCT 子句
                if (qQuery.HaveDistinct) jf.Append("DISTINCT ");                

                // TOP 子句
                if (qQuery.Take > 0 && qQuery.Skip == 0) jf.AppendFormat("TOP({0})", qQuery.Take);

                // Any 
                if (qQuery.HaveAny) jf.Append("TOP 1 1");

                #region 选择字段

                if (!qQuery.HaveAny)
                {
                    // SELECT 范围
                    visitor = new ColumnExpressionVisitor(this, aliases, qQuery.Expression, qQuery.GroupBy);
                    visitor.Write(jf);

                    sc.Columns = (visitor as ColumnExpressionVisitor).Columns;
                    sc.NavDescriptors = (visitor as ColumnExpressionVisitor).NavDescriptors;
                    sc.AddNavigation(visitor.Navigations);

                    // 如果有统计，选择列中还要追加统计的列
                    if (useStatis && willNest)
                    {
                        string columnName = statName;
                        if (!string.IsNullOrEmpty(columnName) && !sc.Columns.ContainsKey(columnName))
                        {
                            if (sc.Columns.Count > 0) jf.Append(",");
                            visitor = new ColumnExpressionVisitor(this, aliases, qQuery.Statis, qQuery.GroupBy, true);
                            visitor.Write(jf);

                            sc.Columns.Add(columnName, new Column { Name = columnName, Duplicate = 1 });
                            sc.AddNavigation(visitor.Navigations);
                        }
                    }

                    // 如果分组后再分页，此时需要在原先的选择字段上再加上 'OrderBy' 子句指定的字段，外层的分页时需要用到这些排序字段
                    if (qQuery.OrderBy.Count > 0 && useOrderBy && groupByPaging)
                    {
                        if (sc.Columns.Count > 0) jf.Append(",");
                        for (int i = 0; i < qQuery.OrderBy.Count; i++)
                        {
                            visitor = new ColumnExpressionVisitor(this, aliases, qQuery.OrderBy[i], qQuery.GroupBy, true);
                            visitor.Write(jf);

                            sc.AddNavigation(visitor.Navigations);
                            if (i < qQuery.OrderBy.Count - 1) jf.AppendNewLine(",");
                        }
                    }
                }

                #endregion
            }

            // FROM 子句
            jf.AppendNewLine();
            jf.Append("FROM ");
            if (qQuery.Subquery != null)
            {
                // 子查询
                jf.Append("(");
                CommandDefine define = this.ParseSelectCommand<T>(qQuery.Subquery as DbQueryableInfo_Select<T>, indent + 1);
                jf.Append(define.CommandText);
                jf.AppendNewLine();
                jf.Append(")");
            }
            else
            {
                jf.AppendMember(TypeRuntimeInfoCache.GetRuntimeInfo(qQuery.FromType).TableName);
            }
            jf.Append(" t0 ");
            if (!string.IsNullOrEmpty(DbQueryProvider.NOLOCK)) jf.Append(DbQueryProvider.NOLOCK);

            // LEFT<INNER> JOIN 子句
            visitor = new JoinExpressionVisitor(this, aliases, qQuery.Join);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(this, aliases, qQuery.Where);
            visitor.Write(wf);
            sc.AddNavigation(visitor.Navigations);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, qQuery.GroupBy);
            visitor.Write(wf);
            sc.AddNavigation(visitor.Navigations);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, qQuery.Having, qQuery.GroupBy);
            visitor.Write(wf);
            sc.AddNavigation(visitor.Navigations);

            // ORDER 子句
            if (qQuery.OrderBy.Count > 0 && useOrderBy && !groupByPaging)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, qQuery.OrderBy, qQuery.GroupBy);
                visitor.Write(wf);
                sc.AddNavigation(visitor.Navigations);
            }

            #region 分页查询

            if (qQuery.Skip > 0 && !groupByPaging)
            {
                if (qQuery.OrderBy.Count == 0) throw new XfwException("The method 'OrderBy' must be called before the method 'Skip'.");
                wf.AppendNewLine();
                wf.Append("OFFSET ");
                wf.Append(qQuery.Skip);
                wf.Append(" ROWS");

                if (qQuery.Take > 0)
                {
                    wf.Append(" FETCH NEXT ");
                    wf.Append(qQuery.Take);
                    wf.Append(" ROWS ONLY ");
                }
            }

            #endregion

            #region 嵌套查询

            if (useStatis && willNest)
            {
                string inner = sc.CommandText;
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(" ) t0");
            }

            #endregion

            #region 分组分页

            if (groupByPaging)
            {
                SqlBuilder builder = new SqlBuilder(this.EscCharLeft, this.EscCharRight);

                // SELECT
                int index = -1;
                builder.Append("SELECT ");
                foreach (var kvp in sc.Columns)
                {
                    index += 1;
                    builder.AppendNewLine();
                    builder.AppendMember("t0", kvp.Key);
                    if (index < sc.Columns.Count - 1) builder.Append(",");
                }

                builder.AppendNewLine();
                builder.Append("FROM ( ");

                string inner = sc.CommandText;
                //jf.Replace(Environment.NewLine, Environment.NewLine + SqlBuilder.TAB);
                jf.Insert(0, builder);


                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(" ) t0");

                // 排序
                if (qQuery.OrderBy.Count > 0 && useOrderBy)
                {
                    visitor = new OrderByExpressionVisitor(this, aliases, qQuery.OrderBy, null, "t0");
                    visitor.Write(jf);
                }

                // 分页
                if (qQuery.Skip > 0)
                {
                    jf.AppendNewLine();
                    jf.Append("OFFSET ");
                    jf.Append(qQuery.Skip);
                    jf.Append(" ROWS");

                    if (qQuery.Take > 0)
                    {
                        jf.Append(" FETCH NEXT ");
                        jf.Append(qQuery.Take);
                        jf.Append(" ROWS ONLY ");
                    }
                }
            }

            #endregion

            // 'Any' 子句
            if (qQuery.HaveAny)
            {
                string inner = sc.CommandText;
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") SELECT 1 ELSE SELECT 0");
            }

            // UNION 子句
            if (qQuery.Union != null && qQuery.Union.Count > 0)
            {
                string inner = sc.CommandText;
                for (int index = 0; index < qQuery.Union.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.AppendNewLine("UNION ALL");
                    CommandDefine define = this.ParseSelectCommand<T>(qQuery.Union[index] as DbQueryableInfo_Select<T>);
                    jf.Append(define.CommandText);
                }

            }

            return sc;
        }

        // 创建 INSRT 命令
        protected override CommandDefine ParseInsertCommand<T>(DbQueryableInfo_Insert<T> qInsert)
        {
            SqlBuilder builder = new SqlBuilder(this.EscCharLeft, this.EscCharRight);
            var rInfo = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            TableAliasCache aliases = new TableAliasCache();

            if (qInsert.Entity != null)
            {
                object entity = qInsert.Entity;
                SqlBuilder columns = new SqlBuilder(this.EscCharLeft, this.EscCharRight);
                SqlBuilder values = new SqlBuilder(this.EscCharLeft, this.EscCharRight);

                foreach (var kv in rInfo.Wrappers)
                {
                    var wrapper = kv.Value as MemberAccessWrapper;
                    var column = wrapper.Column;
                    if (column != null && column.NoMapped) continue;
                    if (wrapper.ForeignKey != null) continue;

                    if (wrapper != qInsert.AutoIncrement)
                    {
                        columns.AppendMember(wrapper.Member.Name);
                        columns.Append(',');

                        var value = wrapper.Get(entity);
                        string seg = ExpressionVisitorBase.GetSqlValue(value);
                        values.Append(seg);
                        values.Append(',');
                    }
                }
                columns.Length -= 1;
                values.Length -= 1;

                if (qInsert.Bulk == null || !qInsert.Bulk.OnlyValue)
                {
                    builder.Append("INSERT INTO ");
                    builder.AppendMember(rInfo.TableName);
                    builder.AppendNewLine();
                    builder.Append('(');
                    builder.Append(columns);
                    builder.Append(')');
                    builder.AppendNewLine();
                    builder.Append("VALUES");
                    builder.AppendNewLine();
                }

                builder.Append('(');
                builder.Append(values);
                builder.Append(')');
                if (qInsert.Bulk != null && !qInsert.Bulk.IsOver) builder.Append(",");

                if (qInsert.Bulk == null && qInsert.AutoIncrement != null)
                {
                    builder.AppendNewLine();
                    builder.Append("SELECT CAST(SCOPE_IDENTITY() AS INT)");
                }
            }
            else if (qInsert.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(rInfo.TableName);
                builder.Append('(');

                CommandDefine_Select sc = this.ParseSelectCommand(qInsert.SelectInfo) as CommandDefine_Select;
                //for (int i = 0; i < seg.Columns.Count; i++)
                int i = 0;
                foreach (var kvp in sc.Columns)
                {
                    builder.AppendMember(kvp.Key);
                    if (i < sc.Columns.Count - 1)
                    {
                        builder.Append(',');
                        builder.AppendNewLine();
                    }

                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(sc.CommandText);
            }

            return new CommandDefine(builder.ToString(), null, System.Data.CommandType.Text); //builder.ToString();
        }

        // 创建 DELETE 命令
        protected override CommandDefine ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> qDelete)
        {
            var rInfo = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            SqlBuilder builder = new SqlBuilder(this.EscCharLeft, this.EscCharRight);
            bool useKey = false;

            builder.Append("DELETE t0 FROM ");
            builder.AppendMember(rInfo.TableName);
            builder.Append(" t0 ");

            if (qDelete.Entity != null)
            {
                object entity = qDelete.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var kv in rInfo.Wrappers)
                {
                    var wrapper = kv.Value as MemberAccessWrapper;
                    var column = wrapper.Column;

                    if (column != null && column.IsKey)
                    {
                        useKey = true;
                        var value = wrapper.Get(entity);
                        var seg = ExpressionVisitorBase.GetSqlValue(value);
                        builder.AppendMember("t0", wrapper.Member.Name);
                        builder.Append(" = ");
                        builder.Append(seg);
                        builder.Append(" AND ");
                    };
                }
                builder.Length -= 5;

                if (!useKey) throw new XfwException("Delete<T>(T value) require T must have key column.");
            }
            else if (qDelete.SelectInfo != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(qDelete.SelectInfo);
                var sc = new CommandDefine_Select.Builder(this.EscCharLeft, this.EscCharRight, aliases);

                ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, qDelete.SelectInfo.Join);
                visitor.Write(sc.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, qDelete.SelectInfo.Where);
                visitor.Write(sc.WhereFragment);
                sc.AddNavigation(visitor.Navigations);

                builder.Append(sc.Command);
            }

            return new CommandDefine(builder.ToString(), null, System.Data.CommandType.Text); //builder.ToString();
        }

        // 创建 UPDATE 命令
        protected override CommandDefine ParseUpdateCommand<T>(DbQueryableInfo_Update<T> qUpdate)
        {
            SqlBuilder builder = new SqlBuilder(this.EscCharLeft, this.EscCharRight);
            var rInfo = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE t0 SET");
            builder.AppendNewLine();

            if (qUpdate.Entity != null)
            {
                object entity = qUpdate.Entity;
                SqlBuilder whereBuilder = new SqlBuilder(this.EscCharLeft, this.EscCharRight);
                bool useKey = false;
                int length = 0;

                foreach (var kv in rInfo.Wrappers)
                {
                    var wrapper = kv.Value as MemberAccessWrapper;
                    var column = wrapper.Column;
                    if (column != null && column.IsIdentity) continue;
                    if (column != null && column.NoMapped) continue;
                    if (wrapper.ForeignKey != null) continue;

                    builder.AppendMember("t0", wrapper.Member.Name);
                    builder.Append(" = ");

                    var value = wrapper.Get(entity);
                    var seg = ExpressionVisitorBase.GetSqlValue(value);
                    builder.Append(seg);
                    length = builder.Length;
                    builder.Append(',');
                    builder.AppendNewLine();

                    if (column != null && column.IsKey)
                    {
                        useKey = true;
                        whereBuilder.AppendMember("t0", wrapper.Member.Name);
                        whereBuilder.Append(" = ");
                        whereBuilder.Append(seg);
                        whereBuilder.Append(" AND ");
                    }
                }

                if (!useKey) throw new XfwException("Update<T>(T value) require T must have key column.");

                builder.Length = length;
                whereBuilder.Length -= 5;

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(rInfo.TableName);
                builder.Append(" t0");


                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);

            }
            else if (qUpdate.Expression != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(qUpdate.SelectInfo);
                ExpressionVisitorBase visitor = null;
                visitor = new UpdateExpressionVisitor(this, aliases, qUpdate.Expression);
                visitor.Write(builder);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(rInfo.TableName);
                builder.AppendAs("t0");

                var sc = new CommandDefine_Select.Builder(this.EscCharLeft, this.EscCharRight, aliases);

                visitor = new JoinExpressionVisitor(this, aliases, qUpdate.SelectInfo.Join);
                visitor.Write(sc.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, qUpdate.SelectInfo.Where);
                visitor.Write(sc.WhereFragment);
                sc.AddNavigation(visitor.Navigations);

                builder.Append(sc.Command);
            }

            return new CommandDefine(builder.ToString(), null, System.Data.CommandType.Text); //builder.ToString();
        }
    }
}
