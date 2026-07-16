// valuation.csx — economic valuation (effort, cost, schedule). Run: dotnet-script valuation.csx
// FP inputs from functionpoints.csx; salaries looked up live 2026-07 (cited in README).

double sysFp = 155, devFp = 29, testFp = 170;   // System AFP (IFPUG); DevOps/Test equiv (backfired)
double locPerFp = 74.3;                           // measured System physical LOC/FP

// Salary blend (2026): BLS median 133080; Glassdoor avg 122562; PayScale 83201 (early-career heavy, down-weighted).
double baseSalary = 120000;                        // blended median base
double burdenLo = 1.5, burdenHi = 1.75;
double dayRateLo = baseSalary*burdenLo/260.0;
double dayRateHi = baseSalary*burdenHi/260.0;
double dayRate = (dayRateLo+dayRateHi)/2.0;        // ~ typical fully-loaded day rate
Console.WriteLine($"Blended base ${baseSalary:N0} x burden {burdenLo}-{burdenHi} / 260 = fully-loaded day rate ${dayRateLo:N0}-${dayRateHi:N0} (mid ${dayRate:N0}).\n");

var bands = new (double fpDay, string level)[]{
  (0.32,"Jones full-lifecycle baseline (recommended headline)"),
  (0.50,"below baseline (large / high-ceremony)"),
  (1.00,"coding-centric / well-run"),
  (2.00,"high-performing small team"),
  (3.00,"elite / best-in-class"),
};

Console.WriteLine("### Effort by productivity band (man-days = FP / FP-per-day; ~22 md/month)\n");
Console.WriteLine("| FP/man-day | Level | LOC/day equiv | System md | DevOps md | Test md |");
Console.WriteLine("|--:|---|--:|--:|--:|--:|");
foreach (var b in bands){
  double loc = locPerFp*b.fpDay;
  string sys=$"{sysFp/b.fpDay:0}", dev, test;
  if (b.fpDay==0.32){ dev="n/a (in System)"; test="n/a (in System)"; }
  else { dev=$"{devFp/b.fpDay:0}"; test=$"{testFp/b.fpDay:0}"; }
  Console.WriteLine($"| {b.fpDay:0.00} | {b.level} | {loc:0} | {sys} | {dev} | {test} |");
}
Console.WriteLine();

Console.WriteLine("### Cost by band (man-days x fully-loaded day rate, mid ${0:N0})\n", dayRate);
Console.WriteLine("| FP/man-day | Level | System $ | DevOps/Test $ | Total $ (coding-centric) |");
Console.WriteLine("|--:|---|--:|--:|--:|");
foreach (var b in bands){
  double sysC = sysFp/b.fpDay*dayRate;
  if (b.fpDay==0.32){
    Console.WriteLine($"| {b.fpDay:0.00} | {b.level} | ${sysC/1000:N0}k | n/a (in System) | n/a (in System) |");
  } else {
    double dt=(devFp+testFp)/b.fpDay*dayRate;
    double tot=sysC+dt;
    Console.WriteLine($"| {b.fpDay:0.00} | {b.level} | ${sysC/1000:N0}k | ${dt/1000:N0}k | ${tot/1000:N0}k |");
  }
}
Console.WriteLine();

double headline = sysFp/0.32*dayRate;
double codingBest = sysFp/3.0*dayRate;
Console.WriteLine("### Anchors\n");
Console.WriteLine($"- **Full-lifecycle replacement (headline, System only): ${headline/1000:N0}k** (Jones 0.32 FP/day; includes design->test->docs->PM).");
Console.WriteLine($"- Coding-centric best case (System, elite 3 FP/day): ${codingBest/1000:N0}k.");
Console.WriteLine($"- **Headline range across bands: ${codingBest/1000:N0}k (elite coding) .. ${headline/1000:N0}k (full-lifecycle).**\n");

// Schedule sanity (small-agile exponent 0.34 for this class)
double exp = 0.34;
double months = Math.Pow(sysFp, exp);
double smLife = sysFp/0.32/22.0;   // staff-months at full-lifecycle
double team = smLife/months;
Console.WriteLine("### Schedule sanity check\n");
Console.WriteLine($"- calendar months ~ FP^{exp} = {sysFp}^{exp} = **{months:0.0} months**");
Console.WriteLine($"- full-lifecycle effort = {smLife:0.0} staff-months -> implied team = {smLife:0.0}/{months:0.0} = **{team:0.0} people** (plausible).\n");
Console.WriteLine("Caveat: replacement/build-cost estimate, order-of-magnitude - NOT market value. Maintainability debt (see findings) makes CHANGING the code cost more per FP than a clean rebuild.");
