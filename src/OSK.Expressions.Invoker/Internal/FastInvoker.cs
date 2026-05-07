using OSK.Expressions.Invoker.Models;
using OSK.Expressions.Invoker.Ports;
using System;

namespace OSK.Expressions.Invoker.Internal;

internal sealed class FastInvoker(Func<object, object[], object> accessorCallback, Func<object, object>? getterCallback, MemberKey memberKey, InvocationType invocationType,
    Type invokeTargetType) : IInvoker
{
    #region IInvoker

    public InvocationType InvocationType => invocationType;

    public Type InvokeTargetType => invokeTargetType;

    public Type ReturnType => memberKey.ReturnType;

    public Type[] ParameterTypes => memberKey.ParameterTypes ?? [];

    public object FastInvoke(object target, params object[] args)
        => invocationType is InvocationType.Method || getterCallback is null || args is { Length: >0 }
            ? accessorCallback(target, args)
            : getterCallback(target);

    #endregion
}