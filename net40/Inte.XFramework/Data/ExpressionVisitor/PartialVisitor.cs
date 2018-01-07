
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// Evaluate Expression
    /// </summary>
    internal class PartialVisitor : ExpressionVisitor
    {
        private Func<Expression, bool> _canEvaluated;
        private HashSet<Expression> _candidates = null;
        private Expression _expression = null;

        public PartialVisitor()
            : this(CanEvaluated)
        { }

        public PartialVisitor(Func<Expression, bool> canEvaluated)
        {
            this._canEvaluated = canEvaluated;
        }

        public Expression Eval(Expression exp)
        {
            _expression = exp;
            NominateVisitor visitor = new NominateVisitor(_canEvaluated);
            _candidates = visitor.Nominate(exp);

            return this.Visit(exp);
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }

            if (this._candidates.Contains(node))
            {
                return this.Evaluate(node);
            }

            return base.Visit(node);
        }

        private Expression Evaluate(Expression e)
        {
            if (e.NodeType == ExpressionType.Constant)
            {
                return e;
            }

            LambdaExpression lambda = e is LambdaExpression ? Expression.Lambda(((LambdaExpression)e).Body) : Expression.Lambda(e);
            Delegate fn = lambda.Compile();

            return Expression.Constant(fn.DynamicInvoke(null), e is LambdaExpression ? ((LambdaExpression)e).Body.Type : e.Type);
        }

        private static bool CanEvaluated(Expression exp)
        {
            return exp.NodeType != ExpressionType.Parameter && exp.NodeType != ExpressionType.MemberInit && exp.NodeType != ExpressionType.New;
        }
    }

    /// <summary>
    /// Nominate Locally Expression
    /// </summary>
    internal class NominateVisitor : ExpressionVisitor
    {
        private Func<Expression, bool> _canEvaluated;
        private HashSet<Expression> _candidates;
        private bool _cannotEvaluated;

        internal NominateVisitor(Func<Expression, bool> canEvaluated)
        {
            this._canEvaluated = canEvaluated;
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            this._candidates = new HashSet<Expression>();
            this.Visit(expression);
            return this._candidates;
        }

        public override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool saveCannotBeEvaluated = _cannotEvaluated;
                _cannotEvaluated = false;

                base.Visit(expression);

                if (!_cannotEvaluated)
                {
                    if (_canEvaluated(expression))
                    {
                        _candidates.Add(expression);
                    }
                    else
                    {
                        _cannotEvaluated = true;
                    }
                }

                _cannotEvaluated |= saveCannotBeEvaluated;
            }

            return expression;
        }
    }
}
