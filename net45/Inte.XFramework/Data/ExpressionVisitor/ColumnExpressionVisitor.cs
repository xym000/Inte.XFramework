using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using TypeUtils = Inte.XFramework.Reflection.TypeUtils;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    public class ColumnExpressionVisitor : ExpressionVisitorBase
    {
        private DbQueryProviderBase _provider = null;
        private TableAliasCache _aliases = null;
        private DbExpression _groupBy = null;
        private static IDictionary<DbExpressionType, string> _statisMethods = null;

        private IDictionary<string, Column> _columns = null;
        private ColumnNavDescriptorCollection _navDescriptors = null;
        private List<string> _navDescriptorKeys = null;
        private bool _mOnly = false;

        static ColumnExpressionVisitor()
        {
            _statisMethods = new Dictionary<DbExpressionType, string>
            {
                { DbExpressionType.Count,"COUNT" },
                { DbExpressionType.Max,"MAX" },
                { DbExpressionType.Min,"MIN" },
                { DbExpressionType.Average,"AVG" },
                { DbExpressionType.Sum,"SUM" }
            };
        }

        /// <summary>
        /// 初始化 <see cref="ColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        public ColumnExpressionVisitor(DbQueryProviderBase provider, TableAliasCache aliases, DbExpression exp, DbExpression groupBy = null, bool mOnly = false)
            : base(provider, aliases, exp.Expressions != null ? exp.Expressions[0] : null)
        {
            _provider = provider;
            _aliases = aliases;
            _groupBy = groupBy;
            _mOnly = mOnly;

            _columns = new Dictionary<string, Column>();
            _navDescriptors = new ColumnNavDescriptorCollection();
            _navDescriptorKeys = new List<string>(10);
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(SqlBuilder builder)
        {
            if (base.Expression != null)
            {
                base._builder = builder;
                _builder.AppendNewLine();

                if (base.Expression.NodeType == ExpressionType.Constant)
                {
                    // if have no select syntax
                    Type type = (base.Expression as ConstantExpression).Value as Type;
                    this.VisitAllMember(type, "t0");
                }
                else
                {
                    base.Write(builder);
                }

                // 去掉最后的空格和回车
                if (_builder[_builder.Length - 1] != _provider.EscCharRight)
                {
                    int space = Environment.NewLine.Length + 1;
                    int index = _builder.Length - 1;
                    while (_builder[index] == ' ')
                    {
                        space++;
                        index--;
                    }
                    _builder.Length -= space;
                }
            }
        }

        /// <summary>
        /// SELECT 字段
        /// Column 对应实体的原始属性
        /// </summary>
        public IDictionary<string, Column> Columns
        {
            get { return _columns; }
        }

        /// <summary>
        /// 导航属性描述信息
        /// 从 DataReader 到实体的映射需要使用这些信息来给导航属性赋值
        /// </summary>
        public ColumnNavDescriptorCollection NavDescriptors
        {
            get { return _navDescriptors; }
        }

        //p=>p
        //p=>p.t
        //p=>p.Id
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            LambdaExpression lambda = node as LambdaExpression;
            if (lambda.Body.NodeType == ExpressionType.Parameter)
            {
                // expression like a=> a
                Type type = lambda.Body.Type;
                string alias = _aliases.GetTableAlias(lambda);
                this.VisitAllMember(type, alias);
                return node;
            }

            if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                // expression like t=> t.a
                // => SELECT a.ClientId
                Type type = lambda.Body.Type;
                return TypeUtils.IsPrimitive(type)
                    ? base.VisitLambda(node)
                    : this.VisitAllMember(type, _aliases.GetTableAlias(lambda.Body), node);
            }

            if (_mOnly) _mOnly = false;
            return base.VisitLambda(node);
        }

        //{new App() {Id = p.Id}} 
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.NewExpression != null) this.VisitAllArguments(node.NewExpression);

            if (_navDescriptorKeys.Count == 0) _navDescriptorKeys.Add(node.Type.Name);

            //todo #对 Bindings 进行排序，保证导航属性的赋值一定要最后面#
            // 未实现，在书写表达式时人工保证 ##

            for (int i = 0; i < node.Bindings.Count; i++)
            {
                MemberAssignment binding = node.Bindings[i] as MemberAssignment;
                if (binding == null) throw new XfwException("Only 'MemberAssignment' binding supported.");

                Type pType = (node.Bindings[i].Member as System.Reflection.PropertyInfo).PropertyType;
                bool visitNav = !TypeUtils.IsPrimitive(pType);

                #region 一般属性

                // 非导航属性
                if (!visitNav)
                {
                    base.VisitMemberBinding(node.Bindings[i]);

                    // 选择字段
                    string newName = ColumnExpressionVisitor.AddColumn(_columns, binding.Member.Name);
                    // 添加字段别名
                    _builder.AppendAs(newName);
                    _builder.Append(',');
                    _builder.AppendNewLine();

                    continue;
                }

                #endregion

                #region 导航属性

                int n = _navDescriptorKeys.Count;

                string keyName = _navDescriptorKeys.Count > 0 ? _navDescriptorKeys[_navDescriptorKeys.Count - 1] : string.Empty;
                keyName = !string.IsNullOrEmpty(keyName) ? keyName + "." + binding.Member.Name : binding.Member.Name;
                ColumnNavDescriptor descriptor = new ColumnNavDescriptor(keyName, binding.Member);

                if (!_navDescriptors.ContainsKey(keyName))
                {
                    descriptor.Start = _columns.Count;
                    descriptor.Count = CountField(binding.Expression);
                    _navDescriptors.Add(keyName, descriptor);
                    _navDescriptorKeys.Add(keyName);
                }

                if (binding.Expression.NodeType == ExpressionType.MemberAccess) this.VisitMember_Navigation(binding.Expression as MemberExpression);
                else if (binding.Expression.NodeType == ExpressionType.New) this.VisitAllArguments(binding.Expression as NewExpression);
                else if (binding.Expression.NodeType == ExpressionType.MemberInit) this.VisitMemberInit(binding.Expression as MemberInitExpression);

                if (_navDescriptorKeys.Count != n) _navDescriptorKeys.RemoveAt(_navDescriptorKeys.Count - 1);

                #endregion
            }

            return node;
        }

        private Expression VisitMember_Navigation(MemberExpression node)
        {
            string alias = string.Empty;
            Type type = node.Type;

            if (node.IsVisitable())
            {
                int index = 0;
                this.VisitNavigation(node);
                foreach (var kvp in Navigations)
                {
                    index += 1;
                    if (index < Navigations.Count) continue;

                    alias = _aliases.GetNavigationTableAlias(kvp.Key);
                    type = kvp.Value.Type;
                }
            }
            else
            {
                alias = _aliases.GetTableAlias(node);
                type = node.Type;
            }

            //未实现 1对多 映射
            //if (type.IsGenericType) type = type.GetGenericArguments()[0];
            this.VisitAllMember(type, alias);
            return node;
        }

        //{new  {Id = p.Id}} 
        protected override Expression VisitNew(NewExpression node)
        {
            if (node != null)
            {
                if (node.Arguments.Count == 0) throw new XfwException("'NewExpression' do not have any arguments.");
                this.VisitAllArguments(node);
            }

            return node;
        }

        //遍历New表达式的参数集
        private Expression VisitAllArguments(NewExpression node)
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Expression exp = node.Arguments[i];
                Type pType = exp.Type;

                if (exp.NodeType == ExpressionType.Parameter)
                {
                    // new Client(a)
                    Type type = exp.Type;
                    string alias = _aliases.GetTableAlias(exp);
                    this.VisitAllMember(type, alias);
                    continue;
                }

                if (exp.NodeType == ExpressionType.MemberAccess || exp.NodeType == ExpressionType.Call)
                {
                    if (TypeUtils.IsPrimitive(pType))
                    {
                        // new Client(a.ClientId)
                        this.Visit(exp);
                        // 选择字段
                        string newName = ColumnExpressionVisitor.AddColumn(_columns, node.Members != null ? node.Members[i].Name : (exp as MemberExpression).Member.Name);
                        // 添加字段别名
                        _builder.AppendAs(newName);
                        _builder.Append(',');
                        _builder.AppendNewLine();
                    }
                    else
                    {
                        this.VisitMember_Navigation(exp as MemberExpression);
                    }

                    continue;
                }

                throw new XfwException("NodeType '{0}' not supported.", exp.NodeType);
            }

            return node;
        }

        // g.Key.CompanyName & g.Max(a)
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

            // Group By 解析
            if (_groupBy != null && node.IsGrouping())
            {
                // CompanyName = g.Key.Name
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                Expression exp = null;
                Expression body = keySelector.Body;


                if (body.NodeType == ExpressionType.MemberAccess)
                {
                    // group xx by a.CompanyName
                    exp = body;

                    //
                    //
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // group xx by new { Name = a.CompanyName  }

                    string memberName = node.Member.Name;
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
                }

                return this.Visit(exp);
            }

            // 分组后再分页，子查询不能有OrderBy子句，此时要将OrderBy字段添加到选择字段中，抛给外层查询使用
            var newNode = base.VisitMember(node);
            if (_mOnly)
            {
                ColumnExpressionVisitor.AddColumn(_columns, node.Member.Name);
                _builder.AppendAs(node.Member.Name);
            }
            return newNode;
        }

        // g.Max(a=>a.Level)
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_groupBy != null && node.IsGrouping())
            {
                DbExpressionType dbExpressionType = DbExpressionType.None;
                Enum.TryParse(node.Method.Name, out dbExpressionType);
                Expression exp = dbExpressionType == DbExpressionType.Count
                    ? Expression.Constant(1)
                    : (node.Arguments.Count == 1 ? null : node.Arguments[1]);
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                // 如果是 a=> a 这种表达式，那么一定会指定 elementSelector
                if (exp.NodeType == ExpressionType.Parameter) exp = _groupBy.Expressions[1];

                _builder.Append(_statisMethods[dbExpressionType]);
                _builder.Append("(");
                this.Visit(exp);
                _builder.Append(")");

                return node;
            }

            return base.VisitMethodCall(node);
        }

        // 选择所有的字段
        private Expression VisitAllMember(Type type, string alias, Expression node = null)
        {
            TypeRuntimeInfo runtimeInfo = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            Dictionary<string, Inte.XFramework.Reflection.MemberAccessWrapper> wrappers = runtimeInfo.Wrappers;

            //Fixed issue# 匿名类的字段不能Set
            //runtimeInfo.IsAnonymousType
            //? type.GetProperties().ToDictionary(p => p.Name, p => new Inte.XFramework.Reflection.MemberAccessWrapper(p))
            //: runtimeInfo.Wrappers;

            foreach (var w in wrappers)
            {
                var wrapper = w.Value;// as Inte.XFramework.Reflection.MemberAccessWrapper;
                var mapper = wrapper as MemberAccessWrapper;
                if (mapper != null && mapper.Column != null && mapper.Column.NoMapped) continue;
                if (mapper != null && mapper.ForeignKey != null) continue; // 不加载导航属性

                _builder.AppendMember(alias, wrapper.Member.Name);

                // 选择字段
                string newName = ColumnExpressionVisitor.AddColumn(_columns, wrapper.Member.Name);
                // 添加字段别名
                _builder.AppendAs(newName);
                _builder.Append(",");
                _builder.AppendNewLine();
            }

            return node;
        }

        // 选择字段
        public static string AddColumn(IDictionary<string, Column> columns, string name)
        {
            // ATTENTION：此方法不能在 VisitMember 方法里调用
            // 因为 VisitMember 方法不一定是最后SELECT的字段
            // 返回最终确定的唯一的列名

            string newName = name;
            int dup = 0;
            if (columns.ContainsKey(newName))
            {
                var column = columns[newName];
                column.Duplicate += 1;

                newName = newName + column.Duplicate.ToString(); //string.Format("{0}{1}", newName, column.Dup.ToString());
                dup = column.Duplicate;
            }

            columns.Add(newName, new Column { Name = name, Duplicate = dup });
            return newName;
        }

        // 计算数据库字段数量 
        private static int CountField(Expression node)
        {
            int num = 0;

            switch (node.NodeType)
            {
                case ExpressionType.MemberInit:
                    MemberInitExpression m = node as MemberInitExpression;
                    foreach (var exp in m.NewExpression.Arguments) num += _typeFieldAggregator(exp);
                    foreach (MemberAssignment ma in m.Bindings) num += _primitiveAggregator((ma.Member as System.Reflection.PropertyInfo).PropertyType);

                    break;

                case ExpressionType.MemberAccess:
                    MemberExpression m1 = node as MemberExpression;
                    num += _typeFieldAggregator(m1);

                    break;

                case ExpressionType.New:
                    NewExpression m2 = node as NewExpression;
                    foreach (var exp in m2.Arguments) num += _typeFieldAggregator(exp);
                    if (m2.Members != null) foreach (var member in m2.Members) num += _primitiveAggregator((member as System.Reflection.PropertyInfo).PropertyType);

                    break;
            }

            return num;
        }

        static Func<Expression, int> _typeFieldAggregator = exp =>
            exp.NodeType == ExpressionType.MemberAccess && TypeUtils.IsPrimitive(exp.Type) ? 1 : TypeRuntimeInfoCache.GetRuntimeInfo(exp.Type).FieldCount;
        static Func<Type, int> _primitiveAggregator = type => TypeUtils.IsPrimitive(type) ? 1 : 0;
    }
}
