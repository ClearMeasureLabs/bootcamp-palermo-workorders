# Phase 3/4 method: input = transcript bytes replayed at each tool-call turn (summed); output = thinking+text+tool args. bytes/4.
import json,sys
def m(p):
    cum=0;inp=0;out=0;calls=0
    for line in open(p,encoding='utf-8'):
        b=len(line.encode()); o=json.loads(line)
        if o.get('role')=='assistant':
            out+=len((o.get('thinking') or '').encode())+len((o.get('text') or '').encode())
            for tc in o.get('tool_calls') or []:
                out+=len(json.dumps(tc.get('tool_args',{})).encode())
            if o.get("tool_calls"): inp+=cum+b; calls+=len(o['tool_calls'])
        cum+=b
    return inp/4,out/4,calls
if __name__=='__main__':
    for p in sys.argv[1:]:
        i,o,c=m(p); print(f"{p.split('/')[-1][:11]} in={i/1000:.1f}k out={o/1000:.2f}k calls={c}")
