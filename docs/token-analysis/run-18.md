# Run 18 — CSV duplicate required header (general v4): BALLOONED
- bc-5a088dc6-ba72-5a2a-8928-69724cb9bb5f · #9970 · PR #9976. Transcript 81,926 B, 51 lines. **16 calls**. Est. **~251k** tokens (-91.3%, still above the 80% target, but 20x run 17).
- **Why:**
  1. **Context gap.** The dispatcher's `-Focus` list (FromHeader, AddUniqueHeader, FindMissingRequiredColumn) left out the caller `Parse(TextReader)`, which is where the fix had to surface a `Fail(...)`. The agent could see the private helpers but not the method that returns the result.
  2. **The "no reads" rule clashed with the gap.** The agent spent **54 KB of thinking** (vs ~5-10 KB in clean runs) weighing designs it couldn't verify: changing return types, an exception, or storing state on the index, all without seeing the caller.
  3. **Anchor misses.** 4 of 8 edits returned empty results (anchors built from the comment-stripped, outline-only text didn't match the real file).
  4. **Recovery loop.** Complete-AgentItem returned empty output (build failed silently), then a manual `dotnet test`, a grep, the one forbidden read of the parser, a fix, a second `dotnet test`, and a second Complete.
- Each turn replays the growing context, so the late recovery turns cost the most.
- **Lessons (v5 candidates):** (a) for files over 80 lines, automatically include callers of focus members in the same file. (b) Allow **one** targeted read when an edit anchor misses, rather than reasoning blind. (c) Print focus members with original whitespace so anchors match. (d) Complete-AgentItem must print the build errors when output would otherwise be empty.
