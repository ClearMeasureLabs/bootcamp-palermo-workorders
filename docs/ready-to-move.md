# Ready To Move

The `Ready To Move` label advances a work item one column on the project board. It is consumed when the move happens — once applied and processed, the label is removed automatically.

AI agents signal completion through the callback API (`/complete`) rather than by applying this label themselves. The factory processes the callback and handles any necessary board movement.

The label cannot move an item out of a column that is owned by an AI worker. For AI-owned columns, the AI worker controls when items leave — the label has no effect on egress from those columns.
