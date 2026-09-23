import re, collections
from catalog_data import GUILDS
agents = {r['name']:(g,r) for g in GUILDS for r in g['rows']}
dups=[n for n,c in collections.Counter(r['name'] for g in GUILDS for r in g['rows']).items() if c>1]
SPECIAL={'owner','-','any-agent'}
unknown=sorted({(r['name'],h) for g in GUILDS for r in g['rows'] for h in r['hand'] if h not in agents and h not in SPECIAL})
otrig=sorted({(r['name'],t[2:].split('[')[0]) for g in GUILDS for r in g['rows'] for t in r['trig'] if t.startswith('o:') and t[2:].split('[')[0] not in agents and t[2:].split('[')[0] not in SPECIAL})
if __name__=='__main__':
    print('agents',len(agents),'dups',dups); print('unknown handoffs',unknown); print('unknown o-triggers',otrig)
