using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class UpdateExpressionVisitor : ExpressionVisitorBase
    {
        private DbQueryProviderBase _provider = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="UpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        public UpdateExpressionVisitor(DbQueryProviderBase provider, TableAliasCache aliases, Expression exp)
            : base(provider, aliases, exp)
        {
            _provider = provider;
            _aliases = aliases;
        }

        //{new App() {Id = p.Id}} 
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings.Count == 0) throw new XfwException("Update<T>(Expression<Func<T, T>> action, Expression<Func<T, bool>> predicate) at least update one member.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                MemberAssignment member = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember("t0", member.Member.Name);
                _builder.Append(" = ");
                base.Visit(member.Expression);
                if (index < node.Bindings.Count - 1)
                {
                    base.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }
    }
}
