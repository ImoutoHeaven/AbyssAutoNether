#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoNether.Services;

/// <summary>
/// Applies the optional, same-popup native UI coverage capture to an authoritative code
/// portfolio without changing the snapshot itself. Missing coverage remains unknown; zero is a
/// valid native target count and is therefore distinct from an absent dictionary entry.
/// </summary>
internal static class NetherCodePartyCoverageProjection
{
    public static IReadOnlyList<NetherCodeState> Apply(
        IReadOnlyList<NetherCodeState> codes,
        IReadOnlyDictionary<long, int> coverageByCodeId
    )
    {
        if (codes == null)
            throw new ArgumentNullException(nameof(codes));
        if (coverageByCodeId == null)
            throw new ArgumentNullException(nameof(coverageByCodeId));

        return codes.Select(code =>
            coverageByCodeId.TryGetValue(code.CodeId, out int coverage) && coverage >= 0
                ? code with
                {
                    PartyCoverageKnown = true,
                    PartyCoverage = coverage,
                }
                : code
        ).ToArray();
    }
}
