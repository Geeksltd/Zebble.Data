using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Olive;

namespace Zebble.Data
{
    internal class ExpressionRunner<T> where T : IEntity
    {
        static bool EnforceOptimizedQueries = Config.Get<bool>("Data.Access.Enforce.Optimized.Queries", defaultValue: false);
        static Dictionary<string, ExpressionRunner<T>> Cache = new Dictionary<string, ExpressionRunner<T>>();
        static List<string> CustomQueriesLog = new List<string>();

        Expression<Func<T, bool>> Criteria;
        internal List<Criterion> Conditions;
        internal Func<T, bool> DynamicCriteria;
        bool ConvertedCompletely = true;

        /// <summary>
        /// Creates a new ExpressionRunner instance.
        /// </summary>
        public ExpressionRunner(Expression<Func<T, bool>> criteria)
        {
            // First time:
            Criteria = criteria;
            Evaluate();

            if (!ConvertedCompletely)
            {
                if (EnforceOptimizedQueries) throw new Exception("The specified criteria cannot be converted to SQL.");

                DynamicCriteria = Criteria.Compile();
            }
        }

        /// <summary>
        /// Creates an instance of Expression Runner for the specified .
        /// </summary>
        internal static ExpressionRunner<T> CreateRunner(Expression<Func<T, bool>> criteria)
        {
            return new ExpressionRunner<T>(criteria);
        }

        void Evaluate()
        {
            Conditions = new List<Criterion>();

            var unitExpressions = GetUnitExpressions((LambdaExpression)Criteria);

            foreach (var ex in unitExpressions)
            {
                var condition = ProcessCriteria(ex);
                if (condition != null) Conditions.Add(condition);
                else ConvertedCompletely = false;
            }

        }

        static IEnumerable<Expression> GetUnitExpressions(LambdaExpression expression)
        {
            return GetUnitExpressions(expression.Body);
        }

        static IEnumerable<Expression> GetUnitExpressions(Expression expression)
        {
            if (expression.NodeType == ExpressionType.AndAlso)
            {
                var binary = expression as BinaryExpression;

                return GetUnitExpressions(binary.Left).Concat(binary.Right);
            }

            else return new[] { expression };

            //System.Linq.Expressions.ConstantExpression

            //System.Linq.Expressions.LambdaExpression

            //System.Linq.Expressions.MemberExpression

            //System.Linq.Expressions.UnaryExpression

            //System.Linq.Expressions.BinaryExpression

            // return null;
        }

        static bool IsSimpleParameter(Expression expression)
        {
            if (expression is ParameterExpression)
                return true;

            if (expression is UnaryExpression && (expression.NodeType == ExpressionType.Convert))
                return true;

            return false;
        }

        static string CalculatePropertyExpression(MemberExpression memberInfo)
        {
            // Handle the member:
            var property = memberInfo.Member as PropertyInfo;
            if (property == null) return null;

            // Fix for overriden properties:
            try { property = memberInfo.Expression.Type.GetProperty(property.Name) ?? property; }
            catch
            {
                // No logging is needed
            }

            if (CalculatedAttribute.IsCalculated(property)) return null;
            if (memberInfo.Expression.Type.IsNullable()) return property.Name;
            if (!property.DeclaringType.Implements<IEntity>()) return null;

            // Handle the "member owner" expression:
            if (IsSimpleParameter(memberInfo.Expression))
            {
                if (property.Name.EndsWith("Id"))
                    if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                        return property.Name.TrimEnd(2);

                return property.Name;
            }
            else if (memberInfo.Expression is MemberExpression)
            {
                // The expression is itself a member of something.

                var parentProperty = CalculatePropertyExpression(memberInfo.Expression as MemberExpression);
                if (parentProperty == null)
                    return null;
                else
                    return
                   parentProperty + "." + property.Name;
            }
            else return null;
        }

        Criterion ProcessCriteria(Expression expression)
        {
            var binary = expression as BinaryExpression;
            if (binary != null)
            {
                if (binary.NodeType == ExpressionType.OrElse)
                {
                    //var criterion = SqlExpressionCompiler<T>.Compile(binary);
                    var criterion = BinaryCriterion.From<T>(binary);

                    if (criterion?.IsConvertedCompletely == false)
                        ConvertedCompletely = false;

                    return criterion;
                }
            }
            return CreateCriterion(expression);
        }

        internal static Criterion CreateCriterion(Expression expression)
        {
            var binary = expression as BinaryExpression;

            if (binary != null)
            {
                var member = binary.Left as MemberExpression;

                if (member != null)
                {
                    var property = CalculatePropertyExpression(member);

                    if (property.IsEmpty()) return null;

                    var value = GetExpressionValue(binary.Right);
                    value = value?.ToString();
                    return new Criterion(property, ToOperator(binary.NodeType), value);
                }
                else if (binary.Left is ParameterExpression)
                {
                    var param = binary.Left as ParameterExpression;
                    return new Criterion("ID", ToOperator(binary.NodeType), GetExpressionValue(binary.Right));
                }
                else
                {
                    return null;
                }
            }

            var unary = expression as UnaryExpression;

            if (unary != null)
            {
                if (unary.NodeType != ExpressionType.Not) return null;

                var member = unary.Operand as MemberExpression;
                if (member == null) return null;

                var property = CalculatePropertyExpression(member);

                if (property.IsEmpty()) return null;

                return new Criterion(property, FilterFunction.Is, false);
            }

            // Boolean property:
            if (expression is MemberExpression && expression.NodeType == ExpressionType.MemberAccess)
            {
                var member = expression as MemberExpression;

                var property = CalculatePropertyExpression(member);

                if (property.IsEmpty()) return null;
                if (property == "HasValue")
                {
                    property = (member.Expression as MemberExpression)?.Member?.Name;
                    if (property.IsEmpty()) return null;
                    return new Criterion(property, FilterFunction.IsNot, value: null);
                }

                if (((member as MemberExpression).Member as PropertyInfo).PropertyType != typeof(bool)) return null;

                // Only one property level is supported:
                return new Criterion(property, FilterFunction.Is, true);
            }

            // Method call
            var methodCall = expression as MethodCallExpression;
            if (methodCall != null)
            {
                return Criterion.From(methodCall, throwOnError: false);
            }

            return null;
        }


        static object GetExpressionValue(ConstantExpression expression)
        {
            return expression.Value;
        }

        static object GetExpressionValue(Expression expression)
        {
            object result;

            result = ExtractExpressionValue(expression);

            var asEntity = result as Entity;
            if (asEntity != null) return asEntity.GetType().GetProperties().Last(x => x.Name == "ID").GetValue(asEntity);
            else return result;
        }

        static object ExtractExpressionValue(Expression expression)
        {
            if (expression == null) return null;

            if (expression is ConstantExpression)
                return (expression as ConstantExpression).Value;

            if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;
                var member = memberExpression.Member;

                if (member is PropertyInfo)
                {
                    return (member as PropertyInfo).GetValue(ExtractExpressionValue(memberExpression.Expression));
                }

                else if (member is FieldInfo)
                {
                    return (member as FieldInfo).GetValue(ExtractExpressionValue(memberExpression.Expression));
                }
                else
                {
                    return CompileAndInvoke(expression);
                }
            }
            else if (expression is MethodCallExpression)
            {
                var methodExpression = expression as MethodCallExpression;
                var method = (expression as MethodCallExpression).Method;

                var instance = ExtractExpressionValue(methodExpression.Object);

                return method.Invoke(instance, methodExpression.Arguments.Select(a => ExtractExpressionValue(a)).ToArray());
            }
            else
            {
                return CompileAndInvoke(expression);
            }
        }

        static object CompileAndInvoke(Expression expression)
        {
            if (EnforceOptimizedQueries)
                throw new Exception("The specified expression cannot be converted to SQL without compilation. Use simple data variables or properties in your lambda queries.");

            return Expression.Lambda(typeof(Func<>).MakeGenericType(expression.Type), expression).Compile().DynamicInvoke();
        }

        static FilterFunction ToOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return FilterFunction.Is;
                case ExpressionType.NotEqual:
                    return FilterFunction.IsNot;
                case ExpressionType.GreaterThan:
                    return FilterFunction.MoreThan;
                case ExpressionType.GreaterThanOrEqual:
                    return FilterFunction.MoreThanOrEqual;
                case ExpressionType.LessThan:
                    return FilterFunction.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return FilterFunction.LessThanOrEqual;
                default: throw new NotSupportedException(type + " is still not supported.");
            }
        }
    }
}