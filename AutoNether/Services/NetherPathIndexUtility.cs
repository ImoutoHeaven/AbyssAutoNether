#nullable enable

using System.Collections.Generic;

namespace AutoNether.Services;

/// <summary>
/// Locates an exact node identity in the ordered visible route. It carries no route-safety or
/// native reward semantics; those remain owned by the callers and their authoritative evidence.
/// </summary>
internal static class NetherPathIndexUtility
{
    internal static int PathIndexOf(IReadOnlyList<long> path, long nodeId)
    {
        for (int index = 0; index < path.Count; index++)
        {
            if (path[index] == nodeId)
                return index;
        }
        return -1;
    }
}
