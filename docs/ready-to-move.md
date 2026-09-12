# Ready To Move

The `Ready To Move` label advances a work item one column on the project board. It is consumed when the move happens, so it does not remain on the item after the transition completes.

AI agents signal completion through the callback API (`/complete`) rather than by applying the label themselves. The factory reads the callback response and handles board movement on behalf of the agent.

The label cannot move an item out of a column that is owned by an AI worker. AI workers control egress for those columns, so the label has no effect there.
