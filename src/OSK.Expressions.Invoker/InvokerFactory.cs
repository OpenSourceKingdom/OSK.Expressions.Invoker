using OSK.Expressions.Invoker.Internal;
using OSK.Expressions.Invoker.Models;
using OSK.Expressions.Invoker.Ports;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OSK.Expressions.Invoker;

public static class InvokerFactory
{
    #region Variables

    private static readonly ConcurrentDictionary<MemberKey, FastInvoker> MemberInvokerMapLookup = new(MemberKeyComparer.Instance);

    #endregion

    #region Api

    /// <summary>
    /// Create a <see cref="IInvoker"/> using a strongly typed reference and an expression to retrieve the member data.
    /// </summary>
    /// <typeparam name="T">The object target type</typeparam>
    /// <param name="memberSelector">An expression to retrieve the member</param>
    /// <returns><see cref="IInvoker"/></returns>
    public static IInvoker CreateInvoker<T>(Expression<Action<T>> memberSelector)
        => CreateInvoker(typeof(T), GetMemberInfo(memberSelector));

    /// <summary>
    /// Create a <see cref="IInvoker"/> using a strongly typed reference and an expression to retrieve the member data.
    /// </summary>
    /// <typeparam name="T">The object target type</typeparam>
    /// <param name="memberSelector">An expression to retrieve the member</param>
    /// <returns><see cref="IInvoker"/></returns>
    public static IInvoker CreateInvoker<T>(Expression<Func<T, object>> memberSelector)
        => CreateInvoker(typeof(T), GetMemberInfo(memberSelector));

    /// <summary>
    /// Create a <see cref="IInvoker"/> using a strongly typed reference and a member info
    /// </summary>
    /// <typeparam name="T">The object target type</typeparam>
    /// <param name="memberInfo">The member info to create an invoker for</param>
    /// <returns><see cref="IInvoker"/></returns>
    public static IInvoker CreateInvoker<T>(MemberInfo memberInfo)
        => CreateInvoker(typeof(T), memberInfo);

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
                [.. methodInfo.GetParameters().Select(p => p.ParameterType)], GetReturnType(memberInfo));
            return MemberInvokerMapLookup.GetOrAdd(methodKey, memberKey =>
            {
                var methodAccessorExpression = CreateMethodCompiledExpression(memberKey.InvocationTargetType, methodInfo);
                return new FastInvoker(methodAccessorExpression, null, memberKey, InvocationType.Method, invocationTargetType);
            });
        }

        var memberKey = new MemberKey(invocationTargetType, memberInfo.Name, [], GetReturnType(memberInfo));
        return MemberInvokerMapLookup.GetOrAdd(memberKey, k =>
        {
            var accessorExpressions = CreateAccessorExpressions(invocationTargetType, memberInfo);
            return new FastInvoker(accessorExpressions.SetterCallback, accessorExpressions.GetterCallback, k.SetParameterTypes(accessorExpressions.MemberType), 
                memberInfo is PropertyInfo _ ? InvocationType.Property : InvocationType.Field,
                invocationTargetType);
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

    private static Func<object, object[], object> CreateMethodCompiledExpression(Type type, MethodInfo method)
    {
        CreateParamsExpressions(method, out ParameterExpression argsExp, out Expression[] paramsExps);

        var targetExp = Expression.Parameter(typeof(object), "target");
        var castTargetExp = Expression.Convert(targetExp, type);

        var invokeExp = Expression.Call(castTargetExp, method, paramsExps);
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

    private static (Func<object, object> GetterCallback, Func<object, object[], object>? SetterCallback, Type MemberType) CreateAccessorExpressions(Type type, MemberInfo memberInfo)
    {
        var targetExp = Expression.Parameter(typeof(object), "target");
        var argsExp = Expression.Parameter(typeof(object[]), "args");
        var castArgExp = Expression.Convert(targetExp, type);

        var expressionData = memberInfo switch
        {
            PropertyInfo propertyInfo => new
            {
                propertyInfo.Name,
                MemberType = propertyInfo.PropertyType,
                IsProperty = true,
                HasSetter = propertyInfo.CanWrite,
                IsInitOnly = propertyInfo.SetMethod?.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)) ?? false
            },
            FieldInfo fieldInfo => new
            {
                fieldInfo.Name,
                MemberType = fieldInfo.FieldType,
                IsProperty = false,
                HasSetter = true,
                fieldInfo.IsInitOnly
            },
            _ => throw new InvalidOperationException($"Unable to create accessor expressions for member info of type {memberInfo.GetType().FullName}")
        };

        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var memberExp = expressionData.IsProperty
            ? Expression.Property(castArgExp, type.GetProperty(expressionData.Name, bindingFlags))
            : Expression.Field(castArgExp, type.GetField(expressionData.Name, bindingFlags));

        var valueObjExp = Expression.ArrayIndex(argsExp, Expression.Constant(0));
        var castValueExp = Expression.Convert(valueObjExp, memberExp.Type);

        var getterBody = Expression.Convert(memberExp, typeof(object));
        var getterExpression = Expression.Lambda<Func<object, object>>(getterBody, targetExp);
        var getterCallback = getterExpression.Compile();

        Func<object, object[], object>? setterCallback = null;
        if (expressionData.HasSetter)
        {
            var assignExp = Expression.Assign(memberExp, castValueExp);
            Expression setterBody = memberExp.Type.IsValueType
                ? Expression.Convert(assignExp, typeof(object))
                : assignExp;
            var setterExpression = Expression.Lambda(setterBody, targetExp, argsExp);

            setterCallback = (Func<object, object[], object>)setterExpression.Compile();
        }

        return new(getterCallback, setterCallback, memberExp.Type);
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
