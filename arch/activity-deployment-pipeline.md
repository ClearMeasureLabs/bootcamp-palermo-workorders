# Deployment Pipeline Activity Diagram

Shows which environment each pipeline step executes in.

```mermaid
flowchart TB
    subgraph eng1["Engineer Workstation"]
        direction LR
        A["1. Private Build"]
    end
    subgraph build1["Build Server"]
        direction LR
        B["2. CI Build"]
    end
    subgraph deploy1["Deployed Environment"]
        direction LR
        C["3. Acceptance Tests"]
    end
    subgraph eng2["Engineer Workstation"]
        direction LR
        D["4. Verify Acceptance Tests"]
        E["5. Correct Problems"]
    end
    subgraph build2["Build Server"]
        direction LR
        F["6. Build Again"]
    end
    subgraph deploy2["Deployed Environment"]
        direction LR
        G["7. Deploy Again"]
    end
    A --> B --> C --> D --> E --> F --> G
```
