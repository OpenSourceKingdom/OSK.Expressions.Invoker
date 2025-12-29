using OSK.Expressions.Invoker.Internal;
using OSK.Expressions.Invoker.Models;
using OSK.Expressions.Invoker.Ports;
using OSK.Hexagonal.MetaData;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OSK.Expressions.Invoker
{
    public static class InvokerFactory
    {
        #region Variables

        private static readonly ConcurrentDictionary<MemberKey, FastInvoker> MemberToWrapperMap
            = new ConcurrentDictionary<MemberKey, FastInvoker>(MemberKeyComparer.Instance);

        #endregion

        #region Api

        /// <summary>
        /// Create a <see cref="IInvoker"/> using a strongly typed reference.
        /// </summary>
        /// <typeparam name="T">The object target type</typeparam>
        /// <param name="memberSelector">An expression to retrieve the member</param>
        /// <returns><see cref="IInvoker"/></returns>
        public static IInvoker CreateInvoker<T>(Expression<Action<T>> memberSelector)
            => CreateInvoker(typeof(T), GetMemberInfo(memberSelector));

        /// <summary>
        /// Create a <see cref="IInvoker"/> using a strongly typed reference.
        /// </summary>
        /// <typeparam name="T">The object target type</typeparam>
        /// <param name="memberSelector">An expression to retrieve the member</param>
        /// <returns><see cref="IInvoker>"/></returns>
        public static IInvoker CreateInvoker<T>(Expression<Func<T, object>> memberSelector)
            => CreateInvoker(typeof(T), GetMemberInfo(memberSelector));

        /// <summary>
        /// Creates a <see cref="IInvoker"/> using a <see cref="MemberInfo"/>, provided a target object type
        /// </summary>
        /// <param name="invocationTargetType">The target object type that contians the member</param>
        /// <param name="memberInfo">The member info to build the invoker for</param>
        /// <returns><see cref="IInvoker"/></returns>
        /// <exception cref="ArgumentNullException">Member Info can not be null</exception>
        public static IInvoker CreateInvoker(Type invocationTargetType, MemberInfo memberInfo)
        {
            if (invocationTargetType is null)
            {
                throw new ArgumentNullException(nameof(invocationTargetType));
            }
            if (memberInfo == null)
            {
                throw new ArgumentNullException(nameof(memberInfo));
            }

            if (memberInfo is MethodInfo methodInfo)
            {
                var methodKey = new MemberKey(invocationTargetType, methodInfo.Name, 
                    methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(), GetReturnType(memberInfo));
                return MemberToWrapperMap.GetOrAdd(methodKey, memberKey =>
                {
                    var compiledExpression = CreateMethodCompiledExpression(memberKey.InvocationTargetType, methodInfo, false);
                    return new FastInvoker(compiledExpression, memberKey, InvocationType.Method);
                });
            }

            var memberKey = new MemberKey(invocationTargetType, memberInfo.Name, [], GetReturnType(memberInfo));
            return MemberToWrapperMap.GetOrAdd(memberKey, k =>
            {
                var (compiledExpression, memberType) = CreateMemberCompiledExpression(invocationTargetType, memberInfo);
                return new FastInvoker(compiledExpression, k.SetParameterTypes(memberType), 
                    memberInfo is PropertyInfo _ ? InvocationType.Property : InvocationType.Field);
            });
        }

        #endregion

        #region Helpers

        private static Type GetReturnType(MemberInfo memberInfo)
            => memberInfo switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                MethodInfo m => m.ReturnType,
                _ => throw new ArgumentException($"Member must be a Property, Field, or Method but was of type {memberInfo.GetType().FullName}")
            };

        private static MemberInfo GetMemberInfo(LambdaExpression expression)
        {
            Expression body = expression.Body;

            // If the property returns a value type (int, bool), 
            // the body will be wrapped in a 'Convert' (UnaryExpression)
            if (body is UnaryExpression unary)
            {
                body = unary.Operand;
            }

            if (body is MethodCallExpression methodCallExpression)
            {
                return methodCallExpression.Method;
            }
            if (body is MemberExpression memberExpression)
            {
                return memberExpression.Member;
            }

            throw new ArgumentException("Expression is not a supported member access (Property, Field, or Method).");
        }

        private static Func<object, object[], object> CreateMethodCompiledExpression(Type type, MethodInfo method, bool isDelegate)
        {
            CreateParamsExpressions(method, out ParameterExpression argsExp, out Expression[] paramsExps);

            var targetExp = Expression.Parameter(typeof(object), "target");
            var castTargetExp = Expression.Convert(targetExp, type);
            var invokeExp = isDelegate
                ? (Expression)Expression.Invoke(castTargetExp, paramsExps)
                : Expression.Call(castTargetExp, method, paramsExps);

            LambdaExpression lambdaExp;

            if (method.ReturnType != typeof(void))
            {
                var resultExp = Expression.Convert(invokeExp, typeof(object));
                lambdaExp = Expression.Lambda(resultExp, targetExp, argsExp);
            }
            else
            {
                var constExp = Expression.Constant(null, typeof(object));
                var blockExp = Expression.Block(invokeExp, constExp);
                lambdaExp = Expression.Lambda(blockExp, targetExp, argsExp);
            }

            var lambda = lambdaExp.Compile();
            return (Func<object, object[], object>)lambda;
        }

        private static Tuple<Func<object, object[], object>, Type> CreateMemberCompiledExpression(Type type, MemberInfo memberInfo)
        {
            var targetExp = Expression.Parameter(typeof(object), "target");
            var argsExp = Expression.Parameter(typeof(object[]), "args");
            var valueObjExp = Expression.ArrayIndex(argsExp, Expression.Constant(0));

            var castArgExp = Expression.Convert(targetExp, type);

            var memberExp = memberInfo is PropertyInfo
                ? Expression.Property(castArgExp, type.GetRuntimeProperty(memberInfo.Name))
                : Expression.Field(castArgExp, type.GetField(memberInfo.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            var castValueExp = Expression.Convert(valueObjExp, memberExp.Type);

            var assignExp = Expression.Assign(memberExp, castValueExp);
            Expression finalBody = memberExp.Type.IsValueType
            ? Expression.Convert(assignExp, typeof(object))
            : assignExp;
            var lambdaExp = Expression.Lambda(finalBody, targetExp, argsExp);

            var lambda = lambdaExp.Compile();
            return new Tuple<Func<object, object[], object>, Type>((Func<object, object[], object>)lambda, memberExp.Type);
        }

        private static void CreateParamsExpressions(MethodBase method, out ParameterExpression argsExp, out Expression[] paramsExps)
        {
            var parameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();

            argsExp = Expression.Parameter(typeof(object[]), "args");
            paramsExps = new Expression[parameters.Count];

            for (var i = 0; i < parameters.Count; i++)
            {
                var constExp = Expression.Constant(i, typeof(int));
                var argExp = Expression.ArrayIndex(argsExp, constExp);
                paramsExps[i] = Expression.Convert(argExp, parameters[i]);
            }
        }

        #endregion
    }
}
