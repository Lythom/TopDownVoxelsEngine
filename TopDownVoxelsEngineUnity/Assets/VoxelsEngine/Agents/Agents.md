# Agents

Agents are visual representations of the entities of the state.

## AgentsDirector
The AgentsDirector is responsible of instanciating / destroying entities to reflect the state.

## Entities

Each entity can have an agent.

Agents are output only, the don't provid information.

To get informations about a specific entity, you must query the state and not the agent, unless you want informations
about the current visual reprensentation of the entity and not about it's underlying data.