using OSK.Expressions.Invoker.Models;
using OSK.Expressions.Invoker.Ports;
using System;

namespace OSK.Expressions.Invoker.Internal;

internal sealed class FastInvoker(Func<object, object[], object>? accessorCallback, Func<object, object>? getterCallback, MemberKey memberKey, InvocationType invocationType,
    Type invokeTargetType) : IInvoker
{
    #region IInvoker

    public InvocationType InvocationType => invocationType;

    public Type InvokeTargetType => invokeTargetType;

    public Type ReturnType => memberKey.ReturnType;

    public Type[] ParameterTypes => memberKey.ParameterTypes ?? [];

    public object FastInvoke(object target, params object[] args)
    {
        if (invocationType is InvocationType.Method || args is { Length: > 0 })
        {
            if (accessorCallback is null)
            {
                throw new InvalidOperationException($"No accessor/setter callback exists for the invocation of target type {target.GetType().FullName}.");
            }

            return accessorCallback(target, args);
        }

        if (getterCallback is null)
        {
            throw new InvalidOperationException("No getter callback exists for getter invocation.");
        }

        return getterCallback(target);
    }

    #endregion
}