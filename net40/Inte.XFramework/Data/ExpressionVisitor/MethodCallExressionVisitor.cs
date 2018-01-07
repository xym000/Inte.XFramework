using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 方法表达式访问器
    /// </summary>
    public abstract class MethodCallExressionVisitorBase
    {
        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public abstract Expression VisitMethodCall(MethodCallExpression node, ExpressionVisitorBase visitor);

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public abstract Expression VisitEqualNull(BinaryExpression b, ExpressionVisitorBase visitor);

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public abstract Expression VisitCoalesce(BinaryExpression b, ExpressionVisitorBase visitor);

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        /// <param name="node">字段或者属性节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public abstract Expression VisitMemberMember(MemberExpression node, ExpressionVisitorBase visitor);

        /// <summary>
        /// 获取 Length 属性对应的SQL函数
        /// </summary>
        /// <param name="node">字段或者属性节点</param>
        /// <param name="visitorBase">访问器</param>
        /// <returns></returns>
        public abstract string GetLenFuncName(MemberExpression node, ExpressionVisitorBase visitor);
    }
}
