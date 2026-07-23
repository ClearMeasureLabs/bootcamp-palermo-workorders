// Function Point (IFPUG) + backfiring cross-check + economic valuation.
// Run: dotnet-script fp-valuation.csx
// All inputs are evidence-derived (see README for the file:line basis of each function type).

using System;
using System.Linq;
using System.Collections.Generic;

Console.WriteLine("==================== IFPUG FUNCTION POINT COUNT ====================\n");

// ---- Transactional & data function types: (label, count, complexity, weight) ----
var rows = new (string Type, string Detail, int Count, string Cx, int Weight)[]
{
    ("EI",  "SaveDraft/create, DraftToAssigned, AssignedToInProgress, InProgressToComplete, AssignedToCancelled, AddAttachmentMetadata, bulk CSV import", 7, "Avg", 4),
    ("EO",  "WorkOrder reformat (LLM), Translation, Telemetry export, Realtime notifications", 4, "Avg", 5),
    ("EQ",  "WorkOrderByNumber, WorkOrder list/spec, EmployeeGetAll, Attachments query, Forecast, Health checks, MCP get-work-order", 7, "Low", 3),
    ("ILF-1","WorkOrder aggregate (number,status,dates,rooms,creator/assignee)", 1, "High", 15),
    ("ILF-2","Employee + Roles aggregate", 1, "Avg", 10),
    ("ILF-3","Attachment metadata", 1, "Low", 7),
    ("EIF", "Azure OpenAI interface (referenced, not maintained)", 1, "Low", 5),
};

int ufp = 0;
Console.WriteLine($"{"Type",-6}{"Complexity",-11}{"Count",6}{"Weight",8}{"FP",6}   Detail");
foreach (var r in rows)
{
    int fp = r.Count * r.Weight;
    ufp += fp;
    Console.WriteLine($"{r.Type,-6}{r.Cx,-11}{r.Count,6}{r.Weight,8}{fp,6}   {r.Detail.Substring(0, Math.Min(48, r.Detail.Length))}");
}
Console.WriteLine($"\nUnadjusted Function Points (UFP) = {ufp}\n");

// ---- 14 General System Characteristics (0..5) ----
var gsc = new (string Name, int Rating, string Why)[]
{
    ("1  Data communications",        5, "HTTP + gRPC + WebSocket + MCP transports"),
    ("2  Distributed data processing",3, "Blazor WASM client + server + Worker + MCP host"),
    ("3  Performance",                3, "rate limiting, idempotency, async throughout"),
    ("4  Heavily used configuration", 2, "standard appsettings, no tuned constraints"),
    ("5  Transaction rate",           2, "internal-scale traffic"),
    ("6  Online data entry",          5, "interactive Blazor forms, primary UX"),
    ("7  End-user efficiency",        4, "speech synth/recognition, dark mode, dictation"),
    ("8  Online update",              4, "ILFs updated interactively via commands"),
    ("9  Complex processing",         3, "LLM reformat, CSV import, state machine"),
    ("10 Reusability",                3, "UI.Shared + ServiceDefaults shared components"),
    ("11 Installation ease",          2, "container deploy, DbUp migrations"),
    ("12 Operational ease",           4, "health checks, OTel, telemetry file writer"),
    ("13 Multiple sites",             3, "Azure Container Apps + Aspire orchestration"),
    ("14 Facilitate change",          3, "CQRS/MediatR seams, smart enums"),
};
Console.WriteLine("==================== VALUE ADJUSTMENT FACTOR (GSC) ====================\n");
int tdi = 0;
foreach (var g in gsc) { tdi += g.Rating; Console.WriteLine($"{g.Name,-34}{g.Rating}   {g.Why}"); }
double vaf = 0.65 + 0.01 * tdi;
int afp = (int)Math.Round(ufp * vaf);
Console.WriteLine($"\nTotal Degree of Influence (TDI) = {tdi}");
Console.WriteLine($"VAF = 0.65 + 0.01 x {tdi} = {vaf:0.00}");
Console.WriteLine($"Adjusted Function Points (AFP) = {ufp} x {vaf:0.00} = {afp}\n");

// ---- Backfiring cross-check (Capers Jones LOC/FP) ----
Console.WriteLine("==================== BACKFIRING CROSS-CHECK (Capers Jones) ====================\n");
var back = new (string Lang, int Loc, int LocPerFp)[]
{
    ("C# (System)",   7075, 55),
    ("Razor (System)",1431, 50),
    ("SQL (System)",  1505, 40),
};
double sysBack = 0;
Console.WriteLine($"{"Language",-16}{"LOC",8}{"LOC/FP",8}{"FP",8}");
foreach (var b in back) { double fp = (double)b.Loc / b.LocPerFp; sysBack += fp; Console.WriteLine($"{b.Lang,-16}{b.Loc,8}{b.LocPerFp,8}{fp,8:0.0}"); }
Console.WriteLine($"\nSystem backfiring total = {sysBack:0} FP (LOC-based UPPER bound)");
Console.WriteLine($"IFPUG hand count (AFP)  = {afp} FP (conservative primary)");
Console.WriteLine($"Adopted System size     = {afp}-{sysBack:0} FP; midpoint ~ {(afp + sysBack) / 2:0} FP\n");

// DevOps/Test backfiring (informational only)
double testFp = 12330.0 / 55 + 800.0 / 45;
Console.WriteLine($"DevOps/Test backfiring (informational) = {testFp:0} FP-equiv (Test C# 12330 + build 800 LOC)\n");

// ---- Economic valuation ----
Console.WriteLine("==================== ECONOMIC VALUATION ====================\n");
double medianSalary = 133_080;   // BLS May 2024, software developers
double burden = 1.45;            // benefits/overhead
double loaded = medianSalary * burden;
double workingDays = 220.0;
double dayRate = loaded / workingDays;
Console.WriteLine($"BLS May-2024 median dev salary = ${medianSalary:N0}; burdened x{burden} = ${loaded:N0}; day rate = ${dayRate:N0}/day ({workingDays} days/yr)\n");

double sizeFp = afp; // value System at the conservative IFPUG number
double[] bands = { 0.32, 0.50, 1.0, 2.0, 3.0 };
Console.WriteLine($"Valuing System at {sizeFp:0} FP (IFPUG). Effort = FP / (FP-per-day).\n");
Console.WriteLine($"{"FP/day",-8}{"Band",-26}{"Man-days",10}{"Staff-mo",10}{"Cost(System)",16}");
foreach (var fpd in bands)
{
    double days = sizeFp / fpd;
    double months = days / 22.0;
    double cost = days * dayRate;
    string band = fpd == 0.32 ? "Full-lifecycle (Jones)" : fpd == 0.5 ? "Conservative" : fpd == 1.0 ? "Industry mid" : fpd == 2.0 ? "Small-team coding" : "Aggressive/AI-assisted";
    Console.WriteLine($"{fpd,-8:0.00}{band,-26}{days,10:0}{months,10:0.0}{cost,16:C0}");
}
Console.WriteLine("\nNote: the 0.32 row is FULL-lifecycle (System only). Do NOT sum System+DevOps/Test at this row.");
Console.WriteLine("Most-likely replacement investment (mid band 0.5-1.0 FP/day): $100k - $210k.");
