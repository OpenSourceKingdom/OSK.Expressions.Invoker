using OSK.Expressions.Invoker.Models;
using System;

namespace OSK.Expressions.Invoker.Ports
{
    public interface IInvoker
    {
        InvocationType InvocationType { get; }

        Type[] ParameterTypes { get; }

        Type ReturnType { get; }

        object FastInvoke(object invokeTarget, object[] parameters);
    }
}
