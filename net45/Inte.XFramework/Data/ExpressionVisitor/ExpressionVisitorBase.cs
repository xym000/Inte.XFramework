using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 表达式解析器基类，提供公共的表达式处理方式
    /// </summary>
    public class ExpressionVisitorBase : ExpressionVisitor
    {
        #region 私有字段

        private DbQueryProviderBase _provider = null;
        private TableAliasCache _aliases = null;
        private Expression _expression = null;
        private IDictionary<string, MemberExpression> _navigations = null;

        /// <summary>
        /// SQL 构造器
        /// </summary>
        protected SqlBuilder _builder = null;

        //防SQL注入字符
        //private static readonly Regex RegSystemThreats = 
        //new Regex(@"\s?or\s*|\s?;\s?|\s?drop\s|\s?grant\s|^'|\s?--|\s?union\s|\s?delete\s|\s?truncate\s|" +
        //    @"\s?sysobjects\s?|\s?xp_.*?|\s?syslogins\s?|\s?sysremote\s?|\s?sysusers\s?|\s?sysxlogins\s?|\s?sysdatabases\s?|\s?aspnet_.*?|\s?exec\s?",
        //    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region 公开属性

        /// <summary>
        /// 即将解析的表达式
        /// </summary>
        internal Expression Expression
        {
            get { return _expression; }
        }

        /// <summary>
        /// 导航属性
        /// </summary>
        public IDictionary<string, MemberExpression> Navigations
        {
            get { return _navigations; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="ExpressionVisitorBase"/> 类的新实例
        /// </summary>
        public ExpressionVisitorBase(DbQueryProviderBase provider, TableAliasCache aliases, Expression exp, bool useNominate = true)
        {
            _provider = provider;
            _aliases = aliases;
            _navigations = new Dictionary<string, MemberExpression>();

            _expression = exp;
            //var visitor = new PartialVisitor();
            //_expression = useNominate ? visitor.Eval(exp) : exp;
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public virtual void Write(SqlBuilder builder)
        {
            this._builder = builder;
            this.Visit(_expression);
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b == null) return b;

            Expression left = b.Left;
            Expression right = b.Right;
            if (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse)
            {
                // expression maybe a.Name == "TAN" && a.Allowused
                left = TryMakeBinary(b.Left);
                right = TryMakeBinary(b.Right);

                if (left != b.Left || right != b.Right)
                {
                    b = Expression.MakeBinary(b.NodeType, left, right);
                    return this.Visit(b);
                }
            }

            // expression like a.Name ?? "TAN"
            if (b.NodeType == ExpressionType.Coalesce) return this.VisitBinary_Coalesce(b);

            // expression like a.Name == null
            ConstantExpression constExpr = b.Left as ConstantExpression ?? b.Right as ConstantExpression;
            if (constExpr != null && constExpr.Value == null) return this.VisitBinary_EqualNull(b);

            // expression like a.Name == a.FullName  or like a.Name == "TAN"
            return this.VisitBinary_Condition(b);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            // expression like a.Name == null ? "TAN" : a.Name => CASE WHEN a.Name IS NULL THEN 'TAN' ELSE a.Name End

            Expression test = this.TryMakeBinary(node.Test, true);
            Expression ifTrue = this.TryMakeBinary(node.IfTrue, true);
            Expression ifFalse = this.TryMakeBinary(node.IfFalse, true);

            this.Append("(CASE WHEN ");
            this.Visit(test);
            this.Append(" THEN ");
            this.Visit(ifTrue);
            this.Append(" ELSE ");
            this.Visit(ifFalse);
            this.Append(" END)");

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            string sql = GetSqlValue(c.Value);
            this.Append(sql);

            return c;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // 1.<>h__TransparentIdentifier3.b.Client.ClientName
            // 2.<>h__TransparentIdentifier3.b.Client.ClientName.Length
            // 3.<>h__TransparentIdentifier3.b.Client.Address.AddressName
            // 4.<>h__TransparentIdentifier3.b.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.Client.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.Client.Address.AddressName
            // 5.b.ClientName

            if (node == null) return node;
            // 表达式 => a.ActiveDate == DateTime.Now
            if (node.Expression == null) return this.VisitConstant(node.Evaluate());
            // 表达式 => a.State == (byte)state
            if (node.IsConstant()) return this.VisitConstant(node.Evaluate());
            // 表达式 => a.Name.Length
            if (Reflection.TypeUtils.IsPrimitive(node.Expression.Type)) return _provider.MethodVisitor.VisitMemberMember(node, this);
            // 表达式 => <>h__3.b.ClientName
            if (!node.Expression.IsVisitable()) return _builder.AppendMember(node, _aliases);
            // 表达式 => b.Client.Address.AddressName            
            this.VisitNavigation(node.Expression, node.Member.Name);

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return _provider.MethodVisitor.VisitMethodCall(node, this);
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 追加指定字符串
        /// </summary>
        public void Append(string s)
        {
            _builder.Append(s);
        }

        /// <summary>
        /// 取值对应的sql片断
        /// </summary>
        public static string GetSqlValue(object value, MemberExpression node = null)
        {
            if (value == null) return "NULL";

            Type type = value.GetType();

            if (type == typeof(string))
                return string.Format("N'{0}'", value.ToString().Replace("'", "''"));
            //return this.GetSqlValue(value as string, node);

            if (type == typeof(Guid))
                return string.Format("'{0}'", value);

            if (type == typeof(DateTime))
                return string.Format("'{0}'", value);

            if (type == typeof(bool))
                return Convert.ToBoolean(value) ? "1" : "0";

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ExpressionVisitorBase.GetSqlValue(value as IEnumerable, node);

            return value.ToString();
        }

        #endregion

        #region 私有函数

        private Expression VisitBinary_Coalesce(BinaryExpression b)
        {
            return _provider.MethodVisitor.VisitCoalesce(b, this);
        }

        private Expression VisitBinary_EqualNull(BinaryExpression b)
        {
            return _provider.MethodVisitor.VisitEqualNull(b, this);
        }

        protected virtual Expression VisitBinary_Condition(BinaryExpression b)
        {
            // expression like a.Name == a.FullName 
            // or like a.Name == "TAN"

            if (b != null)
            {
                string oper = this.GetOperator(b);
                Expression left = b.Left is ConstantExpression ? b.Right : b.Left;
                Expression right = b.Left is ConstantExpression ? b.Left : b.Right;

                bool u_l = this.TakeBracket(b, b.Left);//left.NodeType != ExpressionType.Constant; 
                if (u_l) this.Append("(");
                this.Visit(left);
                if (u_l) this.Append(")");

                this.Append(oper);

                bool u_r = this.TakeBracket(b, b.Right);//right.NodeType != ExpressionType.Constant;  
                if (u_r) this.Append("(");
                this.Visit(right);
                if (u_r) this.Append(")");
            }

            return b;
        }

        protected Expression TryMakeBinary(Expression exp, bool cstSkp = false)
        {
            if (exp.Type != typeof(bool)) return exp;

            Expression left = null;
            Expression right = null;

            switch (exp.NodeType)
            {
                //True should be translate to 1==1 and Flase should be 1==2
                case ExpressionType.Constant:
                    if (!cstSkp)
                    {
                        ConstantExpression constExp = exp as ConstantExpression;
                        bool value = Convert.ToBoolean(constExp.Value);
                        left = Expression.Constant(1);
                        right = Expression.Constant(value ? 1 : 2);
                    }

                    break;

                //x.MemberName(Boolean)
                case ExpressionType.MemberAccess:
                    left = exp;
                    right = Expression.Constant(true);

                    break;

                //!x.MemberName(Boolean)
                case ExpressionType.Not:
                    UnaryExpression unaryExp = exp as UnaryExpression;
                    if (unaryExp.Operand.NodeType == ExpressionType.MemberAccess)
                    {
                        left = unaryExp.Operand;
                        right = Expression.Constant(false);
                    }

                    break;
            }

            if (left != null) exp = Expression.MakeBinary(ExpressionType.Equal, left, right);

            return exp;
        }

        protected virtual string GetOperator(BinaryExpression b)
        {
            string opr = string.Empty;
            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    opr = " = ";
                    break;
                case ExpressionType.NotEqual:
                    opr = " <> ";
                    break;
                case ExpressionType.GreaterThan:
                    opr = " > ";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    opr = " >= ";
                    break;
                case ExpressionType.LessThan:
                    opr = " < ";
                    break;
                case ExpressionType.LessThanOrEqual:
                    opr = " <= ";
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    opr = b.Type == typeof(bool) ? " AND " : " & ";
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    opr = b.Type == typeof(bool) ? " OR " : " | ";
                    break;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    opr = " + ";
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    opr = " - ";
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    opr = " * ";
                    break;
                case ExpressionType.Divide:
                    opr = " / ";
                    break;
                case ExpressionType.Modulo:
                    opr = " % ";
                    break;
                case ExpressionType.Coalesce:
                    opr = "ISNULL";
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} is not supported.", b.NodeType));
            }

            return opr;
        }

        protected bool TakeBracket(Expression expression, Expression subExp = null)
        {
            if (subExp != null)
            {
                UnaryExpression unaryExpression = subExp as UnaryExpression;
                if (unaryExpression != null) return TakeBracket(expression, unaryExpression.Operand);

                InvocationExpression invokeExpression = subExp as InvocationExpression;
                if (invokeExpression != null) return TakeBracket(expression, invokeExpression.Expression);

                LambdaExpression lambdaExpression = subExp as LambdaExpression;
                if (lambdaExpression != null) return TakeBracket(expression, lambdaExpression.Body);

                BinaryExpression b = subExp as BinaryExpression;
                if (b != null)
                {
                    if (expression.NodeType == ExpressionType.OrElse)
                        return true;
                }
            }

            return this.GetPriority(expression) < this.GetPriority(subExp);
        }

        protected virtual void VisitNavigation(Expression node, string memberName = null)
        { 
            // 表达式 => b.Client.Address.AddressName
            Expression f = node;
            Stack<KeyValuePair<string, MemberExpression>> stack = null;
            while (f != null && f.IsVisitable())
            {
                if (f.NodeType != ExpressionType.MemberAccess) break;

                if (stack == null) stack = new Stack<KeyValuePair<string, MemberExpression>>();
                MemberExpression m = f as MemberExpression;
                var runtime = TypeRuntimeInfoCache.GetRuntimeInfo(m.Expression.Type);
                ForeignKeyAttribute attribute = runtime.GetWrapperAttribute<ForeignKeyAttribute>(m.Member.Name);
                if (attribute == null) break;

                string key = m.GetKeyWidthoutAnonymous();
                stack.Push(new KeyValuePair<string, MemberExpression>(key, m));
                f = m.Expression;
            }

            if (stack != null && stack.Count > 0)
            {
                while (stack != null && stack.Count > 0)
                {
                    KeyValuePair<string, MemberExpression> kvp = stack.Pop();
                    string key = kvp.Key;
                    MemberExpression m = kvp.Value;

                    var runtime = TypeRuntimeInfoCache.GetRuntimeInfo(m.Type);
                    // 检查查询表达式是否显示指定该表关联
                    string alias = _aliases.GetJoinTableAlias(runtime.TableName);
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，则使用导航属性别名
                        alias = _aliases.GetNavigationTableAlias(key);
                        if (!_navigations.ContainsKey(kvp.Key)) _navigations.Add(kvp);
                    }

                    if (stack.Count == 0 && !string.IsNullOrEmpty(memberName)) _builder.AppendMember(alias, memberName);
                }
            }
            else
            {
                // => SelectMany 也会产生类似 'b.Client.Address.AddressName' 这样的表达式
                string alias = _aliases.GetTableAlias(node);
                _builder.AppendMember(alias, memberName);
            }

            //return f;
        }

        protected int GetPriority(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return 3;
                case ExpressionType.And:
                    return expression.Type == typeof(bool) ? 6 : 3;
                case ExpressionType.AndAlso:
                    return 6;
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return 2;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return 4;
                case ExpressionType.Not:
                    return expression.Type == typeof(bool) ? 5 : 1;
                case ExpressionType.Or:
                    return expression.Type == typeof(bool) ? 7 : 3;
                case ExpressionType.OrElse:
                    return 7;
                default:
                    return 0;
            }
        }

        //private string GetSqlValue(string value, MemberExpression node = null)
        //{
        //    if (value == null) return "NULL";

        //    MemberExpression memberExp = node;

        //    // make sure always visit ConstantExpression Last *******
        //    //if (memberExp == null)
        //    //{
        //    //    while (_stcMember.Count > 0)
        //    //    {
        //    //        var temp = _stcMember.Pop();
        //    //        if (memberExp == null) memberExp = temp;
        //    //    }
        //    //}

        //    //bool isNv = true;
        //    //if (memberExp != null)
        //    //{
        //    //    //DbType from memberInfo
        //    //    var rInfo = TypeRuntimeInfoCache.GetRuntimeInfo(memberExp.Member.DeclaringType);
        //    //    var wrapper = rInfo.GetWrapper(memberExp.Member.Name) as MemberAccessWrapper;
        //    //    if (wrapper != null && wrapper.Column != null)
        //    //    {
        //    //        isNv = wrapper.Column.DbType == DbType.String || wrapper.Column.DbType == DbType.StringFixedLength;
        //    //    }
        //    //}

        //    //// N'信息科技' 或者 'Google'
        //    //return string.Format("{0}'{1}'", isNv ? "N" : string.Empty, value.ToString().Replace("'", "''"));
        //}

        private static string GetSqlValue(IEnumerable value, MemberExpression node = null)
        {
            if (value == null) return "NULL";

            MemberExpression memberExp = node;

            var iterator = value.GetEnumerator();
            List<string> stack = new List<string>();
            while (iterator.MoveNext())
            {
                stack.Add(ExpressionVisitorBase.GetSqlValue(iterator.Current, memberExp));
            }

            // =>a,b,c
            string sql = string.Join(",", stack);
            return sql;
        }

        #endregion
    }
}