using OSK.Expressions.Invoker.Models;
using OSK.Hexagonal.MetaData;
using System;

namespace OSK.Expressions.Invoker.Ports
{
    [HexagonalIntegration(HexagonalIntegrationType.LibraryProvided)]
    public interface IInvoker
    {
        /// <summary>
        /// The type of invocation this invoker is configured to support
        /// </summary>
        InvocationType InvocationType { get; }

        /// <summary>
        /// The type of object that should be passed to the invoker
        /// </summary>
        Type InvokeTargetType { get; }

        /// <summary>
        /// The expected parameter types to be provided to the invoker to function
        /// </summary>
        Type[] ParameterTypes { get; }

        /// <summary>
        /// The return type of the member info that was used to create this invoker
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Effeciently run the invoker on the targeted object
        /// </summary>
        /// <param name="invokeTarget">The object to invoke</param>
        /// <param name="parameters">The parameters to pass to the invocation</param>
        /// <returns></returns>
        object FastInvoke(object invokeTarget, object[] parameters);
    }
}
