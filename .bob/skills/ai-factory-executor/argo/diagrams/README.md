# Architecture diagrams — AI Dev Factory on Argo

Rendered locally with PlantUML (`plantuml -tpng c4/*.puml views/*.puml`).
Source `.puml` next to each `.png`.

## C4 model (the four levels)
| Level | File | Shows |
|-------|------|-------|
| C1 — Context   | `c4/c1-context.puml`   | The factory, its users, GitHub, and the LLM backend |
| C2 — Container | `c4/c2-container.puml` | The k3s node: Argo + up to 3 concurrent isolated pods |
| C3 — Component | `c4/c3-component.puml` | Inside one AI Dev pod: clone init, entrypoint, agent, gates, PR publisher, SQL sidecar |
| C4 — Code      | `c4/c4-code.puml`      | `implement-issue.ps1` submission path → Secret / WorkflowTemplate / semaphore |

## 4+1 architecture views (Kruchten)
| View | File | Shows |
|------|------|-------|
| Logical      | `views/logical.puml`     | Functional building blocks: Intake, Isolation, Concurrency, Execution |
| Process      | `views/process.puml`     | Runtime concurrency, the 3-slot gate, per-pod process groups |
| Development   | `views/development.puml` | Source module layout + reuse of the existing executor files |
| Physical     | `views/physical.puml`    | Deployment onto the one k3s machine |
| Scenarios (+1)| `views/scenarios.puml`  | "implement issue N" end-to-end sequence tying the four views together |

## Re-render
```bash
plantuml -tpng c4/*.puml views/*.puml
```
> Rendering is local — the sandbox blocks uploading internal diagrams to the public
> PlantUML server. This PlantUML build (1.2020.02) lacks `C4_Deployment`, so the
> physical view uses native UML deployment nodes.
