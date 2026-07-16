// functionpoints.csx - IFPUG + Capers Jones backfiring. Run: dotnet-script functionpoints.csx
// Inputs are analyst-derived counts (documented in README) and tool-measured LOC (scc).

// ---------- Method A: IFPUG ----------
var ftypes = new (string T, int Count, int W, string Basis)[] {
    ("EI  (External Input)",   11, 4, "6 state transitions + attachment add + CSV bulk import + telemetry ingest + AI chat submit + reformat"),
    ("EO  (External Output)",   5, 5, "weather forecast, detailed health report, translation, AI chat response, auto-reformat (all derived)"),
    ("EQ  (External Inquiry)",  8, 4, "WO by number, WO search/spec, WO attachments, employee all, employee by user, diagnostics x2, MCP reference"),
    ("ILF (Internal Logical File)", 4, 10, "WorkOrder(+attachment/status), Employee(+role), Telemetry, NServiceBus saga/outbox - aggregate roots, NOT per-table"),
    ("EIF (External Interface File)", 1, 7, "Azure OpenAI / LLM service read"),
};
int ufp = 0;
Console.WriteLine("### IFPUG function-type table\n");
Console.WriteLine("| Function type | Count | Complexity | Weight | FP |");
Console.WriteLine("|---|--:|:--:|--:|--:|");
foreach (var f in ftypes) { int fp = f.Count*f.W; ufp += fp;
    Console.WriteLine($"| {f.T} | {f.Count} | average | {f.W} | {fp} |"); }
Console.WriteLine($"| **UFP total** | | | | **{ufp}** |\n");

// GSC (14), conservative
var gsc = new (string C, int R, string J)[] {
    ("Data communications",4,"HTTP + WebSocket + gRPC + NServiceBus SQL transport"),
    ("Distributed processing",4,"separate Worker endpoint + standalone MCP server + Blazor WASM client"),
    ("Performance",2,"output caching + rate limiting present, not perf-critical"),
    ("Heavily-used configuration",2,"standard appsettings, no heavy config-driven behavior"),
    ("Transaction rate",2,"internal LOB volumes"),
    ("Online data entry",4,"interactive Blazor forms are the primary UX"),
    ("End-user efficiency",3,"nav rail, dark mode, search, responsive"),
    ("Online update",4,"real-time WebSocket state push to clients"),
    ("Complex processing",3,"state machine, idempotency, LLM orchestration"),
    ("Reusability",3,"onion layers + shared component library"),
    ("Installation ease",2,"DbUp migrations + container image"),
    ("Operational ease",3,"health checks, OTEL, local telemetry writer"),
    ("Multiple sites",1,"single deployment target"),
    ("Facilitate change",3,"CQRS + DI decouple change"),
};
int sgsc = 0;
Console.WriteLine("### General System Characteristics (VAF)\n");
Console.WriteLine("| # | Characteristic | Rating (0-5) | Justification |");
Console.WriteLine("|--:|---|:--:|---|");
int i=1; foreach (var g in gsc){ sgsc+=g.R; Console.WriteLine($"| {i++} | {g.C} | {g.R} | {g.J} |"); }
double vaf = 0.65 + 0.01*sgsc;
double afp = ufp*vaf;
Console.WriteLine($"| | **Sum GSC** | **{sgsc}** | VAF = 0.65 + 0.01x{sgsc} = **{vaf:0.00}** |\n");
Console.WriteLine($"**UFP = {ufp}  -  VAF = {vaf:0.00}  -  AFP = UFP x VAF = {afp:0.0} ~ {Math.Round(afp)} FP** (System, IFPUG)\n");

// ---------- Method B: backfiring ----------
// Physical (scc) -> logical factor for C#-family; Jones SPR LOC/FP are logical.
double p2l = 1.35; // documented physical->logical divisor
double Ratio(string lang) => lang switch { "C#"=>54.0, "SQL"=>18.0, _=>40.0 /*markup/script/other*/ };

(string cat, (string lang,int loc)[] langs)[] cats = {
  ("System", new[]{("C#",6635),("Razor",1407),("CSS",2556),("Other(config/proto)",923)}),
  ("DevOps/deploy", new[]{("C#",319),("Powershell",836),("SQL",90),("Other(json/msbuild/docker)",282)}),
  ("Test", new[]{("C#",12159),("MSBuild",162)}),
};
Console.WriteLine("### Backfiring (Capers Jones, physical->logical /{0})\n", p2l);
double sysBf=0, devBf=0, testBf=0;
foreach (var c in cats){
  Console.WriteLine($"**{c.cat}**\n");
  Console.WriteLine("| Language | Physical LOC | LOC/FP (logical) | equiv FP |");
  Console.WriteLine("|---|--:|--:|--:|");
  double sub=0;
  foreach (var l in c.langs){ double r=Ratio(l.lang); double fp=(l.loc/p2l)/r; sub+=fp;
    Console.WriteLine($"| {l.lang} | {l.loc} | {r} | {fp:0.0} |"); }
  Console.WriteLine($"| **Subtotal** | | | **{sub:0.0}** |\n");
  if(c.cat=="System") sysBf=sub; else if(c.cat.StartsWith("DevOps")) devBf=sub; else testBf=sub;
}
double band=0.5; // +/-50% sensitivity per Jones caution
Console.WriteLine($"System backfired = {sysBf:0.0} FP (sensitivity {sysBf*(1-band):0}..{sysBf*(1+band):0}).\n");

// ---------- Reconciliation ----------
double div = (sysBf-ufp)/(double)ufp*100;
Console.WriteLine("### System reconciliation (IFPUG vs backfiring)\n");
Console.WriteLine($"- IFPUG UFP = {ufp} ; AFP = {Math.Round(afp)}");
Console.WriteLine($"- Backfired System = {sysBf:0.0}");
Console.WriteLine($"- Divergence vs UFP = {div:+0.0;-0.0}% -> {(Math.Abs(div)<=30?"AGREE (<=30%)":"REVIEW (>30%)")}\n");

// ---------- Subtotals + total ----------
double sysFp = Math.Round(afp);
Console.WriteLine("### FP subtotals + total (System uses IFPUG AFP; DevOps/Test use backfired equiv FP)\n");
Console.WriteLine("| Category | FP | Share |");
Console.WriteLine("|---|--:|--:|");
double tot = sysFp+devBf+testBf;
Console.WriteLine($"| System (IFPUG AFP) | {sysFp} | {sysFp/tot*100:0}% |");
Console.WriteLine($"| DevOps/deploy (equiv) | {devBf:0} | {devBf/tot*100:0}% |");
Console.WriteLine($"| Test (equiv) | {testBf:0} | {testBf/tot*100:0}% |");
Console.WriteLine($"| **Total** | **{tot:0}** | 100% |\n");
Console.WriteLine($"System LOC/FP = 11521 / {sysFp} = {11521/sysFp:0.0} (vs C# Jones ~54 logical; physical inflation expected).");
