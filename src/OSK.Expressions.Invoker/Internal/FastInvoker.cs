using OSK.Expressions.Invoker.Models;
using OSK.Expressions.Invoker.Ports;
using System;

namespace OSK.Expressions.Invoker.Internal;

internal sealed class FastInvoker(Func<object, object[], object> func, MemberKey memberKey, InvocationType invocationType) : IInvoker
{
    #region IInvoker

    public InvocationType InvocationType => invocationType;

    public Type ReturnType => memberKey.ReturnType;

    public Type[] ParameterTypes => memberKey.ParameterTypes ?? [];

    public object FastInvoke(object target, params object[] args)
        => func(target, args);

    #endregion
}