# Deployment Pipeline Activity Diagram

Shows which environment each pipeline step executes in.

```mermaid
flowchart TB
    subgraph eng["Engineer Workstation"]
        direction LR
        A["1. Private Build"]
    end
    subgraph build["Build Server"]
        direction LR
        B["2. CI Build"]
    end
    subgraph deploy["Deployed Environment"]
        direction LR
        C["3. Acceptance Tests"]
    end
    A --> B --> C
```
