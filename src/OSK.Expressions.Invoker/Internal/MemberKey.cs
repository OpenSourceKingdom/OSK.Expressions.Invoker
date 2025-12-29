using System;

namespace OSK.Expressions.Invoker.Internal;

internal struct MemberKey
{
    #region Variables

    public readonly Type InvocationTargetType { get; }
    public readonly string MemerName { get; }
    public readonly Type[] ParameterTypes { get; }
    public readonly Type ReturnType { get; }

    #endregion

    #region Constructors

    public MemberKey(Type serviceType, string name, Type[] parameterTypes, Type returnType)
    {
        InvocationTargetType = serviceType;
        MemerName = name;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    #endregion

    #region Helpers

    public MemberKey SetParameterTypes(params Type[] parameterTypes)
    {
        return new MemberKey(InvocationTargetType, MemerName, parameterTypes, ReturnType);
    }

    #endregion
}