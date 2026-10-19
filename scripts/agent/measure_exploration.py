# Control-round metric: tokens (bytes/4, same replay method as measure_transcript.py) spent before the first edit.
# First edit = first assistant turn whose tool call is an edit tool, or a shell command that writes a repo file.
import json, re, sys
EDIT_TOOLS = {'search_replace', 'write', 'edit_file', 'MultiEdit', 'apply_patch', 'create_file', 'delete_file', 'edit_notebook', 'str_replace_based_edit_tool'}
SHELL_WRITE = re.compile(r"sed -i|\btee\b|>\s*\S*(src/|\.cs\b|\.razor\b|\.sql\b)|cat\s*>|git apply|dotnet new|open\([^)]*['\"]w|Set-Content|Out-File|Add-Content|\bpatch\b -p")
def is_edit(tc):
    n = tc.get('tool_name'); a = tc.get('tool_args') or {}
    if n in EDIT_TOOLS: return True
    if n == 'run_terminal_cmd': return bool(SHELL_WRITE.search(a.get('command', '')))
    return False
def m(p):
    cum = inp = pre = 0; calls = pre_calls = 0; first = None
    for line in open(p, encoding='utf-8'):
        b = len(line.encode()); o = json.loads(line)
        tcs = o.get('tool_calls') or [] if o.get('role') == 'assistant' else []
        if tcs:
            inp += cum + b; calls += len(tcs)
            if first is None and any(is_edit(t) for t in tcs): first = calls
            if first is None: pre += cum + b; pre_calls += len(tcs)
        cum += b
    return inp / 4, pre / 4, pre_calls, first
if __name__ == '__main__':
    for p in sys.argv[1:]:
        i, e, c, f = m(p)
        print(f"{p.split('/')[-1][:11]} total={i/1000:.1f}k pre_edit={e/1000:.1f}k ({100*e/i if i else 0:.0f}%) pre_edit_calls={c} first_edit_call={f}")
