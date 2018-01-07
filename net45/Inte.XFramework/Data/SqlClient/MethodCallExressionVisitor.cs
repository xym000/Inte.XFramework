using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Inte.XFramework.Reflection.Emit;

namespace Inte.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="MethodCallExpression"/> 表示式访问器
    /// </summary>
    public class MethodCallExressionVisitor : Inte.XFramework.Data.MethodCallExressionVisitorBase
    {
        static IDictionary<string, MemberAccess_Method> _methods = null;
        static MethodCallExressionVisitor()
        {
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
            _methods = typeof(MethodCallExressionVisitor)
                .GetMethods(bindingAttr)
                .ToDictionary(m => m.Name, m => new MemberAccess_Method(m));
        }

        #region 重写方法

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <param name="ExpressionVisitorBase">访问器</param>
        /// <returns></returns>
        public override Expression VisitEqualNull(BinaryExpression b, ExpressionVisitorBase visitor)
        {
            // a.Name == null => a.Name Is Null

            string oper = b.NodeType == ExpressionType.Equal
                ? " IS "
                : " IS NOT ";

            visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            visitor.Append(oper);
            visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);

            return b;
        }

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <param name="visitor">访问器</param>
        /// <returns></returns>
        public override Expression VisitCoalesce(BinaryExpression b, ExpressionVisitorBase visitor)
        {
            // expression like a.Name ?? "TAN" => ISNULL(a.Name,'TAN')

            visitor.Append("ISNULL(");
            visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            visitor.Append(",");
            visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);
            visitor.Append(")");


            return b;
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <param name="visitor">访问器</param>
        /// <returns></returns>
        public override Expression VisitMethodCall(MethodCallExpression node, ExpressionVisitorBase visitor)
        {
            Type type = node.Object != null ? node.Object.Type : node.Arguments[0].Type;
            string methodName = string.Empty;

            if (type == typeof(string) || node.Method.Name == "ToString")
                methodName = "VisitStrMethodCall_";
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                methodName = "VisitEnumMethodCall_";

            if (!string.IsNullOrEmpty(methodName))
            {
                MemberAccess_Method invoker = null;
                MethodCallExressionVisitor._methods.TryGetValue(methodName + node.Method.Name, out invoker);
                if (invoker != null)
                {
                    object exp = invoker.Invoke(this, new object[] { node, visitor });
                    return exp as Expression;
                }
            }

            throw new XfwException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
        }

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        /// <param name="node">字段或者属性节点</param>
        /// <param name="visitor">访问器</param>
        /// <returns></returns>
        public override Expression VisitMemberMember(MemberExpression node, ExpressionVisitorBase visitor)
        {
            Type type = node.Expression.Type;
            string methodName = string.Empty;

            if (type == typeof(string))
                methodName = "VisitStrMember_";

            if (!string.IsNullOrEmpty(methodName))
            {
                MemberAccess_Method invoker = null;
                MethodCallExressionVisitor._methods.TryGetValue(methodName + node.Member.Name, out invoker);
                if (invoker != null)
                {
                    object exp = invoker.Invoke(this, new object[] { node, visitor });
                    return exp as Expression;
                }
            }

            throw new XfwException("{0}.{1} is not supported.", node.Member.DeclaringType, node.Member.Name);
        }

        /// <summary>
        /// 获取 Length 属性对应的SQL函数
        /// </summary>
        /// <param name="node">字段或者属性节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public override string GetLenFuncName(MemberExpression node, ExpressionVisitorBase visitor)
        {
            return "LEN";
        }

        #endregion

        #region 私有函数

        private Expression VisitStrMethodCall_ToString(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            visitor.Append("CAST(");
            visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            visitor.Append(" AS VARCHAR)");

            return m;
        }

        private Expression VisitStrMethodCall_Contains(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Visit(m.Object);
                visitor.Append(" LIKE ");
                if (m.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    ConstantExpression c = m.Arguments[0] as ConstantExpression;
                    //visitor.Append(string.Format("N'%{0}%'", c.Value));
                    visitor.Append("N'%");
                    visitor.Append(c.Value != null ? c.Value.ToString() : string.Empty);
                    visitor.Append("%'");
                }
                else
                {
                    visitor.Append("('%' + ");
                    visitor.Visit(m.Arguments[0]);
                    visitor.Append(" + '%')");
                }
            }

            return m;
        }

        private Expression VisitStrMethodCall_StartsWith(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Visit(m.Object);
                visitor.Append(" LIKE ");
                if (m.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    ConstantExpression c = m.Arguments[0] as ConstantExpression;
                    visitor.Append("N'");
                    visitor.Append(c.Value != null ? c.Value.ToString() : string.Empty);
                    visitor.Append("%'");
                    //visitor.Append(string.Format("N'{0}%'", c.Value));
                }
                else
                {
                    visitor.Append("(");
                    visitor.Visit(m.Arguments[0]);
                    visitor.Append(" + '%')");
                }
            }

            return m;
        }

        private Expression VisitStrMethodCall_EndsWith(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Visit(m.Object);
                visitor.Append(" LIKE ");
                if (m.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    ConstantExpression c = m.Arguments[0] as ConstantExpression;
                    //visitor.Append(string.Format("N'%{0}'", c.Value));
                    visitor.Append("N'%");
                    visitor.Append(c.Value != null ? c.Value.ToString() : string.Empty);
                    visitor.Append("'");
                }
                else
                {
                    visitor.Append("('%' + ");
                    visitor.Visit(m.Arguments[0]);
                    visitor.Append(")");
                }
            }

            return m;
        }

        private Expression VisitStrMethodCall_TrimStart(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Append("LTRIM(");
                visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                visitor.Append(")");
            }

            return m;
        }

        private Expression VisitStrMethodCall_TrimEnd(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Append("RTRIM(");
                visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                visitor.Append(")");
            }

            return m;
        }

        private Expression VisitStrMethodCall_Trim(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                visitor.Append("RTRIM(LTRIM(");
                visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                visitor.Append("))");
            }

            return m;
        }

        private Expression VisitStrMethodCall_SubString(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m != null)
            {
                List<Expression> args = new List<Expression>(m.Arguments);
                if (m.Object != null) args.Insert(0, m.Object);

                visitor.Append("SUBSTRING(");
                visitor.Visit(args[0]);
                visitor.Append(",");
                visitor.Visit(args[1]);
                visitor.Append(" + 1,");

                if (args.Count == 3)
                {
                    visitor.Visit(args[2]);
                }
                else
                {
                    visitor.Append("LEN(");
                    visitor.Visit(args[0]);
                    visitor.Append(")");
                }
                visitor.Append(")");
            }

            return m;
        }

        private Expression VisitEnumMethodCall_Contains(MethodCallExpression m, ExpressionVisitorBase visitor)
        {
            if (m == null) return m;

            MemberExpression member = m.Arguments[m.Arguments.Count - 1] as MemberExpression;
            visitor.Visit(m.Arguments[m.Arguments.Count - 1]);
            visitor.Append(" IN(");

            //ConstantExpression constExpr = (m.Object != null ? m.Object : m.Arguments[0]) as ConstantExpression;
            //visitor.Visit(constExpr);
            Expression exp = m.Object != null ? m.Object : m.Arguments[0];
            if (exp.NodeType == ExpressionType.Constant)
            {
                visitor.Visit(exp);
            }
            else if (exp.NodeType == ExpressionType.MemberAccess)
            {
                visitor.Visit(exp.Evaluate());
            }
            else if (exp.NodeType == ExpressionType.NewArrayInit)
            {
                var expressions = (exp as NewArrayExpression).Expressions;
                for (int i = 0; i < expressions.Count; i++)
                {
                    visitor.Visit(expressions[i]);
                    if (i < expressions.Count - 1) visitor.Append(",");
                }
            }

            visitor.Append(")");


            return m;
        }

        private Expression VisitStrMember_Length(MemberExpression m, ExpressionVisitorBase visitor)
        {
            visitor.Append("LEN(");
            visitor.Visit(m.Expression);
            visitor.Append(")");

            return m;
        }

        #endregion
    }
}
