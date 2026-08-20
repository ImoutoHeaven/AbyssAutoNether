# Treasure Cache Full Regression Evidence

Evidence ID: `forensic-keynotfound-20260820-full`

This directory records a clean full-suite Docker regression preceded in the
same ephemeral container by a fresh native Cpp2IL diffable/ISIL decompile.
The source and current game trees are mounted read-only; restore/build/test
outputs live only on tmpfs.

Outcome: 1331/1331 passed for the intermediate catch-based fix. Review then
replaced that workaround with root-cause cache-access removal so canonical
Treasure priority remains intact. Final full verification is in
`forensic-keynotfound-20260820-review-repair`.
