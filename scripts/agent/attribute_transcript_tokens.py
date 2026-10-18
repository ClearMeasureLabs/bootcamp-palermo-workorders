import json,glob,collections,sys
ids="00e570ab 5a0d68f5 384d166b 9f5504a5 bfe4d417 547adadf 42f6f715 b0a9a07c 9077fff8 2f9a5598 fecf3547 ea38b9a0 81384f13 559045c2 b45403a6 84811922".split()
tot=collections.Counter(); grand=0; turnsAll=0; waitturns=0
for i in ids:
    L=[l for l in open(glob.glob(f'bc-{i}*.jsonl')[0])]
    objs=[json.loads(l) for l in L]
    turn_idx=[k for k,o in enumerate(objs) if o.get('role')=='assistant' and o.get('tool_calls')]
    edited=False; sub=0
    for k,o in enumerate(objs):
        n=sum(1 for t in turn_idx if t>=k)  # times this line is replayed
        b=len(L[k].encode())*n
        r=o.get('role'); a=o.get('tool_args') or {}; cm=a.get('command',''); tf=a.get('target_file','')
        if r=='user': c='prompt'
        elif r=='assistant': c='assistant msgs/tool args'
        elif 'GENERAL.md' in tf: c='skill file'
        elif 'gh issue' in cm: c='issue body'
        elif 'Submit-Change' in cm: c='submit'; sub+=1
        elif 'Wait-PrChecks' in cm: c='wait results'
        elif o.get('tool_name')=='get_mcp_tools': c='get_mcp_tools'
        elif o.get('tool_name') in ('search_replace','write','edit_file'): c='edits'; edited=True
        else: c='reads/search after first edit' if edited else 'reads/search before first edit'
        tot[c]+=b; grand+=b
    # wait turn cost: replay at each wait call turn
    for t in turn_idx:
        tc=objs[t]['tool_calls'][0].get('tool_args',{})
        if 'Wait-PrChecks' in str(tc): waitturns+=sum(len(x.encode()) for x in L[:t+1])
print('total input k',grand/4000)
for c,v in tot.most_common(): print(f'{c:35s} {v/4000:8.1f}k {100*v/grand:5.1f}%')
print('replay cost of wait-call turns k',waitturns/4000, f'{100*waitturns/grand:.1f}%')
