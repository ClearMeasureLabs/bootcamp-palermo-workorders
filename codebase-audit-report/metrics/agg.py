import json,collections
d=json.load(open('/tmp/scc.json'))
loc=collections.Counter(); cc=collections.Counter(); cnt=collections.Counter()
for lang in d:
  if lang['Name'] not in ('C#','Razor'): continue
  for f in lang['Files']:
    path=f['Location'].replace(chr(92),'/')
    p=path.split('/')
    if 'Generated' in path: key='GENERATED(excl)'
    elif len(p)>2 and p[1]=='UI': key='UI/'+p[2]
    else: key=p[1] if len(p)>1 else path
    loc[key]+=f['Code']; cc[key]+=f['Complexity']; cnt[key]+=1
for k in sorted(loc, key=lambda x:-loc[x]):
  print(f'{k:22} files={cnt[k]:4} code={loc[k]:6} cc={cc[k]:5}')
