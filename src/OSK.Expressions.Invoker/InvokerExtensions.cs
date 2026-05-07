using OSK.Expressions.Invoker.Ports;
using System;
using System.Collections.Generic;
using System.Text;

namespace OSK.Expressions.Invoker;

public static class InvokerExtensions
{
    extension(IInvoker invoker)
    {
        /// <summary>
        /// Effeciently run the invoker on the targeted object without parameters
        /// </summary>
        /// <param name="invokeTarget">The object to invoke</param>
        /// <returns></returns>
        public object FastInvoke(object invokeTarget)
            => invoker.FastInvoke(invokeTarget, []);

        public object FastInvoke(object invokeTarget, params object[] parameters)
            => invoker.FastInvoke(invokeTarget, parameters);

        /// <summary>
        /// Effeciently run the invoker on the targeted object without parameters
        /// </summary>
        /// <param name="invokeTarget">The object to invoke</param>
        /// <returns>A typed result</returns>
        public T FastInvoke<T>(object invokeTarget)
            => (T)invoker.FastInvoke(invokeTarget, []);

        /// <summary>
        /// Effeciently run the invoker on the targeted object
        /// </summary>
        /// <param name="invokeTarget">The object to invoke</param>
        /// <param name="parameters">The parameters to pass to the invocation</param>
        /// <returns></returns>
        public T FastInvoke<T>(object invokeTarget, params object[] parameters)
            => (T)invoker.FastInvoke(invokeTarget, parameters);
    }
}
