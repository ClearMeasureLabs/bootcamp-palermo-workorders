### IFPUG function-type table

| Function type | Count | Complexity | Weight | FP |
|---|--:|:--:|--:|--:|
| EI  (External Input) | 11 | average | 4 | 44 |
| EO  (External Output) | 5 | average | 5 | 25 |
| EQ  (External Inquiry) | 8 | average | 4 | 32 |
| ILF (Internal Logical File) | 4 | average | 10 | 40 |
| EIF (External Interface File) | 1 | average | 7 | 7 |
| **UFP total** | | | | **148** |

### General System Characteristics (VAF)

| # | Characteristic | Rating (0-5) | Justification |
|--:|---|:--:|---|
| 1 | Data communications | 4 | HTTP + WebSocket + gRPC + NServiceBus SQL transport |
| 2 | Distributed processing | 4 | separate Worker endpoint + standalone MCP server + Blazor WASM client |
| 3 | Performance | 2 | output caching + rate limiting present, not perf-critical |
| 4 | Heavily-used configuration | 2 | standard appsettings, no heavy config-driven behavior |
| 5 | Transaction rate | 2 | internal LOB volumes |
| 6 | Online data entry | 4 | interactive Blazor forms are the primary UX |
| 7 | End-user efficiency | 3 | nav rail, dark mode, search, responsive |
| 8 | Online update | 4 | real-time WebSocket state push to clients |
| 9 | Complex processing | 3 | state machine, idempotency, LLM orchestration |
| 10 | Reusability | 3 | onion layers + shared component library |
| 11 | Installation ease | 2 | DbUp migrations + container image |
| 12 | Operational ease | 3 | health checks, OTEL, local telemetry writer |
| 13 | Multiple sites | 1 | single deployment target |
| 14 | Facilitate change | 3 | CQRS + DI decouple change |
| | **Sum GSC** | **40** | VAF = 0.65 + 0.01x40 = **1.05** |

**UFP = 148  -  VAF = 1.05  -  AFP = UFP x VAF = 155.4 ~ 155 FP** (System, IFPUG)

### Backfiring (Capers Jones, physical->logical /1.35)

**System**

| Language | Physical LOC | LOC/FP (logical) | equiv FP |
|---|--:|--:|--:|
| C# | 6635 | 54 | 91.0 |
| Razor | 1407 | 40 | 26.1 |
| CSS | 2556 | 40 | 47.3 |
| Other(config/proto) | 923 | 40 | 17.1 |
| **Subtotal** | | | **181.5** |

**DevOps/deploy**

| Language | Physical LOC | LOC/FP (logical) | equiv FP |
|---|--:|--:|--:|
| C# | 319 | 54 | 4.4 |
| Powershell | 836 | 40 | 15.5 |
| SQL | 90 | 18 | 3.7 |
| Other(json/msbuild/docker) | 282 | 40 | 5.2 |
| **Subtotal** | | | **28.8** |

**Test**

| Language | Physical LOC | LOC/FP (logical) | equiv FP |
|---|--:|--:|--:|
| C# | 12159 | 54 | 166.8 |
| MSBuild | 162 | 40 | 3.0 |
| **Subtotal** | | | **169.8** |

System backfired = 181.5 FP (sensitivity 91..272).

### System reconciliation (IFPUG vs backfiring)

- IFPUG UFP = 148 ; AFP = 155
- Backfired System = 181.5
- Divergence vs UFP = +22.6% -> AGREE (<=30%)

### FP subtotals + total (System uses IFPUG AFP; DevOps/Test use backfired equiv FP)

| Category | FP | Share |
|---|--:|--:|
| System (IFPUG AFP) | 155 | 44% |
| DevOps/deploy (equiv) | 29 | 8% |
| Test (equiv) | 170 | 48% |
| **Total** | **354** | 100% |

System LOC/FP = 11521 / 155 = 74.3 (vs C# Jones ~54 logical; physical inflation expected).
