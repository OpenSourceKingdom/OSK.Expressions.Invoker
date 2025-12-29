using System;
using System.Collections.Generic;

namespace OSK.Expressions.Invoker.Internal;

internal class MemberKeyComparer : IEqualityComparer<MemberKey>
{
    public static readonly MemberKeyComparer Instance = new MemberKeyComparer();

    public bool Equals(MemberKey x, MemberKey y)
    {
        return x.InvocationTargetType == y.InvocationTargetType && StringComparer.Ordinal.Equals(x.MemerName, y.MemerName);
    }

    public int GetHashCode(MemberKey key)
    {
        var typeCode = key.InvocationTargetType.GetHashCode();
        var methodCode = key.MemerName.GetHashCode();
        return CombineHashCodes(typeCode, methodCode);
    }

    private static int CombineHashCodes(int h1, int h2)
    {
        return (h1 << 5) + h1 ^ h2;
    }
}