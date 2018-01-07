using System;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    public static  class ExpressionExtensions
    {
        private static readonly string _anonymousName = "<>h__TransparentIdentifier";
        private static readonly string _groupingName = "IGrouping`2";

        private static Func<string, bool> _isGrouping = g => g == ExpressionExtensions._groupingName;
        private static Func<string, bool> _isAnonymous = name => !string.IsNullOrEmpty(name) && name.StartsWith(ExpressionExtensions._anonymousName, StringComparison.Ordinal);

        /// <summary>
        /// 判断属性访问表达式是否有系统动态生成前缀
        /// <code>
        /// h__TransparentIdentifier.a.CompanyName
        /// </code>
        /// </summary>
        public static bool IsAnonymous(this Expression node)
        {
            // <>h__TransparentIdentifier => h__TransparentIdentifier.a.CompanyName
            Expression exp = node;
            ParameterExpression paramExp = exp.NodeType == ExpressionType.Lambda 
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (paramExp != null) return ExpressionExtensions._isAnonymous(paramExp.Name);

            if (exp.NodeType == ExpressionType.MemberAccess)    // <>h__TransparentIdentifier.a.CompanyName
            {
                MemberExpression memExp = exp as MemberExpression;
                if (ExpressionExtensions._isAnonymous(memExp.Member.Name)) return true;

                return ExpressionExtensions.IsAnonymous(memExp.Expression);
            }

            return false;
        }

        /// <summary>
        /// 判断是否是分组表达式
        /// </summary>
        public static bool IsGrouping(this Expression node)
        {
            //g.Key
            //g.Key.CompanyName
            //g.Max()
            //g=>g.xxx
            //g.Key.CompanyId.Length
            //g.Key.Length 

            // g | g=>g.xx
            Expression exp = node;
            ParameterExpression paramExp = exp.NodeType == ExpressionType.Lambda
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (paramExp != null) return ExpressionExtensions._isGrouping(paramExp.Type.Name);

            // g.Max
            MethodCallExpression callExp = exp as MethodCallExpression;
            if (callExp != null) return ExpressionExtensions._isGrouping(callExp.Arguments[0].Type.Name);


            MemberExpression memExp = exp as MemberExpression;
            if (memExp != null)
            {
                // g.Key
                var g1 = memExp.Member.Name == "Key" && ExpressionExtensions._isGrouping(memExp.Expression.Type.Name);
                if (g1) return g1;

                // g.Key.Length | g.Key.Company | g.Key.CompanyId.Length
                memExp = memExp.Expression as MemberExpression;
                if (memExp != null)
                {
                    g1 = memExp.Member.Name == "Key" && ExpressionExtensions._isGrouping(memExp.Expression.Type.Name) && memExp.Type.Namespace == null; //匿名类没有命令空间
                    if (g1) return g1;
                }
            }

            return false;
        }

        /// <summary>
        /// 在递归访问 MemberAccess 表达式时，判定节点是否能够被继续递归访问
        /// </summary>
        public static bool IsVisitable(this Expression node)
        {
            // a 
            // <>h__TransparentIdentifier.a
            // <>h__TransparentIdentifier0.<>h__TransparentIdentifier1.a
            
            if (node.NodeType == ExpressionType.Parameter) return false;
            if (node.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression m = node as MemberExpression;
                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                    string name = (m.Expression as ParameterExpression).Name;
                    if (ExpressionExtensions._isAnonymous(name)) return false;
                }
                if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    string name = (m.Expression as MemberExpression).Member.Name;
                    if (ExpressionExtensions._isAnonymous(name)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断表达式链是否是常量类型
        /// </summary>
        public static bool IsConstant(this MemberExpression node)
        {
            if (node == null) return false;
            if (node.Expression == null) return false;
            if (node.Expression.NodeType == ExpressionType.Constant) return true;
            return (node.Expression as MemberExpression).IsConstant();
        }

        /// <summary>
        /// 取剔除掉系统动态生成前缀后的表达式
        /// </summary>
        public static string GetKeyWidthoutAnonymous(this MemberExpression node)
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            b.Append(node.Member.Name);

            Expression f = node.Expression;
            while (f.IsVisitable())
            {
                b.Append('.');
                b.Append((f as MemberExpression).Member.Name);
                f = (f as MemberExpression).Expression;
            }

            if (f.NodeType == ExpressionType.Parameter) b.Append('.').Append((f as ParameterExpression).Name);
            if (f.NodeType == ExpressionType.MemberAccess) b.Append('.').Append((f as MemberExpression).Member.Name);


            return b.ToString();
        }

        /// <summary>
        /// 计算表达式的值
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ConstantExpression Evaluate(this Expression e)
        {
            if (e.NodeType == ExpressionType.Constant)
            {
                return e as ConstantExpression;
            }

            LambdaExpression lambda = e is LambdaExpression ? Expression.Lambda(((LambdaExpression)e).Body) : Expression.Lambda(e);
            Delegate fn = lambda.Compile();

            return Expression.Constant(fn.DynamicInvoke(null), e is LambdaExpression ? ((LambdaExpression)e).Body.Type : e.Type);
        }
    }
}
