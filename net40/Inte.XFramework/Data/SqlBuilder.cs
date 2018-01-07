using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class SqlBuilder
    {
        private StringBuilder _builder = null;
        private char _escCharLeft;
        private char _escCharRight;

        /// <summary>
        /// TAB 制表符
        /// </summary>
        public const string TAB = "    ";

        /// <summary>
        /// 获取或设置当前 <see cref="StringBuilder"/> 对象的长度。
        /// </summary>
        public int Length
        {
            get { return _builder.Length; }
            set { _builder.Length = value; }
        }

        /// <summary>
        /// 获取或设置此实例中指定字符位置处的字符。
        /// </summary>
        public char this[int index] { get { return _builder[index]; } }

        /// <summary>
        /// 缩进
        /// </summary>
        public int Indent { get; set; }

        /// <summary>
        /// 实例化 <see cref="SqlBuilder" /> 的新实例
        /// </summary>
        /// <param name="escCharLeft">如 [</param>
        /// <param name="escCharRight">如 ]</param>
        public SqlBuilder(char escCharLeft, char escCharRight)
        {
            _escCharLeft = escCharLeft;
            _escCharRight = escCharRight;
            _builder = new StringBuilder(128);
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="aliases">表别名</param>
        /// <param name="expression">列名表达式</param>
        /// <returns>返回解析到的表别名</returns>
        public string AppendMember(TableAliasCache aliases, Expression expression)
        {
            Expression exp = expression;
            LambdaExpression lambdaExpression = exp as LambdaExpression;
            if (lambdaExpression != null) exp = lambdaExpression.Body;

            MemberExpression memExp = exp as MemberExpression;
            if (expression.NodeType == ExpressionType.Constant || memExp.Expression.NodeType == ExpressionType.Constant)
            {
                PartialVisitor visitor = new PartialVisitor();
                var eval = visitor.Eval(memExp ?? expression);
                string value = eval.NodeType == ExpressionType.Constant
                    ? ExpressionVisitorBase.GetSqlValue((eval as ConstantExpression).Value)
                    : string.Empty;
                _builder.Append(value);

                return value;
            }
            else
            {
                string alias = aliases == null ? null : aliases.GetTableAlias(memExp);
                this.AppendMember(alias, memExp.Member.Name);
                return alias;
            }
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="expression">列名表达式</param>
        /// <param name="aliases">表别名</param>
        /// <returns>返回解析到的表别名</returns>
        public Expression AppendMember(Expression expression, TableAliasCache aliases)
        {
            this.AppendMember(aliases, expression);
            return expression;
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        public SqlBuilder AppendMember(string alias, string name)
        {
            if (!string.IsNullOrEmpty(alias))
            {
                _builder.Append(alias);
                _builder.Append('.');
            }
            return this.AppendMember(name);
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        public SqlBuilder AppendMember(string name)
        {
            _builder.Append(_escCharLeft);
            _builder.Append(name);
            _builder.Append(_escCharRight);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        public SqlBuilder AppendAs(string name)
        {
            _builder.Append(" AS ");
            return this.AppendMember(name);
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public SqlBuilder Append(string value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        public SqlBuilder Insert(int index, string value)
        {
            _builder.Insert(index, value);
            return this;
        }

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        public SqlBuilder Insert(int index, object value)
        {
            _builder.Insert(index, value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public SqlBuilder Append(int value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public SqlBuilder Append(char value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public SqlBuilder Append(object value)
        {
            if (value != null) _builder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public SqlBuilder AppendNewLine()
        {
            _builder.Append(Environment.NewLine);
            if (this.Indent > 0)
            {
                for (int i = 1; i <= this.Indent; i++) this.AppendNewTab();
            }
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public SqlBuilder AppendNewLine(string value)
        {
            _builder.AppendLine(value);
            return this;
        }

        /// <summary>
        /// 将通过处理复合格式字符串（包含零个或零个以上格式项）返回的字符串追加到此实例。每个格式项都替换为形参数组中相应实参的字符串表示形式。
        /// </summary>
        public SqlBuilder AppendFormat(string value, params object[] args)
        {
            _builder.AppendFormat(value, args);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加制表符
        /// </summary>
        public SqlBuilder AppendNewTab()
        {
            _builder.Append(SqlBuilder.TAB);
            return this;
        }

        /// <summary>
        /// 将此实例中所有指定字符串的匹配项替换为其他指定字符串。
        /// </summary>
        public SqlBuilder Replace(string oldValue, string newValue)
        {
            _builder.Replace(oldValue, newValue);
            return this;
        }

        /// <summary>
        /// 将此值实例转换成 <see cref="string"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
