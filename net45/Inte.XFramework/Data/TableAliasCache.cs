using System.Linq.Expressions;
using Inte.XFramework.Caching;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 表别名缓存项
    /// </summary>
    public class TableAliasCache
    {
        //1.表别名缓存项（包含显示指定的和导航属性产生的）
        private readonly ICache<string, string> _cache = new SimpleCache<string, string>();

        //2.导航属性产生的别名列表，这些别名没有在查询表达式显式地声明 存储为：访问链（a.Company.Address）+别名
        // where a.Company.Address.AddressName == "番禺"
        // 像这种表达式，a.Company.Address 访问链会被存储在此变量内
        private readonly ICache<string, string> _navigationAliases = new SimpleCache<string, string>();
        
        //3.由表达式显式指定的 LEFT JOIN 或者 Inner Join 表达式所对应的表的别名，存储为（Bas_Company）：物理表名+别名
        // from a in context.GetTable<Bank>()
        // join b in context.GetTable<Company>() on a.CompanyId equals b.CompanyId
        // 像这种表达式，则 b对应的表 Company 会被存储在此变量内
        // 如果同一个物理表显式指定多次，则只存储最后声明的那个
        private readonly ICache<string, string> _joinAliases = new SimpleCache<string, string>();

        // FROM 和 JOIN 表达式总数
        // 在这个计数的基础上再分配导航属性关联的表别名
        private int _explicit = 0;

        /// <summary>
        /// 实例化 <see cref="TableAliasCache"/> 类的新实例
        /// </summary>
        public TableAliasCache() : this(0) { }

        /// <summary>
        /// 实例化 <see cref="TableAliasCache"/> 类的新实例
        /// </summary>
        /// <param name="num">FROM 和 JOIN 表达式所占有的总数</param>
        public TableAliasCache(int num)
        {
            _explicit = num;
        }

        /// <summary>
        /// 根据指定表达式取表别名
        /// </summary>
        /// <param name="exp">表达式</param> 
        /// <remarks>
        /// t=>t.Id
        /// t.Id
        /// </remarks>
        public string GetTableAlias(Expression exp)
        {
            // p=>p.p
            // p=>p.Id
            // p=>p.t.Id
            // p.Id
            // p.t.Id
            // p.t
            // <>h__TransparentIdentifier0.p.Id
            XfwException.Check.NotNull(exp, "exp");
            string key = TableAliasCache.GetTableAliasKey(exp);
            return this.GetTableAlias(key);
        }

        /// <summary>
        ///  根据指定键值取表别名
        /// </summary>
        /// <param name="key">键值</param>
        public string GetTableAlias(string key)
        {
            return !string.IsNullOrEmpty(key) ? this._cache.GetOrAdd(key, x => "t" + this._cache.Count.ToString()) : "Nullable";
        }

        /// <summary>
        ///  根据指定键值取导航属性对应的表别名
        /// </summary>
        /// <param name="key">键值</param>
        public string GetNavigationTableAlias(string key)
        {
            XfwException.Check.NotNull(key, "key");
            return this._navigationAliases.GetOrAdd(key, x => "t" + (this._navigationAliases.Count + _explicit).ToString());
        }

        /// <summary>
        /// 建立 表名/表别名 键值对
        /// 由查询表达式中显示指定的 左/内关联提供
        /// </summary>
        /// <param name="name">表名</param>
        /// <param name="alias">别名（t0,t1）</param>
        /// <returns></returns>
        public string AddOrUpdateJoinTableAlias(string name, string alias)
        {
            XfwException.Check.NotNull(name, "name");
            return this._joinAliases.AddOrUpdate(name, x => alias, x => alias);
        }

        /// <summary>
        ///  根据物理表名取其对应的别名
        ///  由查询表达式中显示指定的 左/内关联提供
        /// </summary>
        /// <param name="name">表名</param>
        public string GetJoinTableAlias(string name)
        {
            string alias = string.Empty;
            this._joinAliases.TryGet(name, out alias);
            return alias;
        }

        private static string GetTableAliasKey(Expression exp)
        {
            if (exp == null) return null;

            Expression expression = exp;

            //c
            if (exp.NodeType == ExpressionType.Constant) return null;

            // p
            ParameterExpression paramExp = expression as ParameterExpression;
            if (paramExp != null) return paramExp.Name;

            // a=>a.Id
            LambdaExpression lambdaExp = expression as LambdaExpression;
            if (lambdaExp != null) expression = lambdaExp.Body.ReduceUnary();

            // a.Id
            // t.a
            // t.t.a
            // t.a.Id
            MemberExpression memExp = expression as MemberExpression;
            if (memExp == null) return TableAliasCache.GetTableAliasKey(expression);

            if (memExp.IsVisitable()) return TableAliasCache.GetTableAliasKey(memExp.Expression);

            return memExp.Member.Name;
        }
    }
}
