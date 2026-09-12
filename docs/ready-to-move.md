# Ready To Move

The `Ready To Move` label advances a work item one column on the project board and is consumed when the move happens — it is removed automatically after the item moves.

AI agents signal completion through the callback API (`/complete`) rather than by applying the label themselves. The factory applies the label on behalf of the agent once the completion call is received and validated.

The label cannot move an item out of a column that is owned by an AI worker. For those columns, the AI worker controls egress, so applying `Ready To Move` has no effect — only the worker's own completion signal can advance the item.
