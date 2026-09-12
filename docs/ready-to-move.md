# Ready To Move

The `Ready To Move` label advances a work item one column on the project board. It is consumed when the move happens, so the label is automatically removed after the item is moved.

AI agents signal completion through the callback API (`/complete`) — they do not apply the `Ready To Move` label themselves. The factory responds to that callback and handles any necessary board transitions.

The label cannot move an item out of a column that is owned by an AI worker. For those columns the AI worker controls egress, so applying the label has no effect until the AI worker releases the item.
