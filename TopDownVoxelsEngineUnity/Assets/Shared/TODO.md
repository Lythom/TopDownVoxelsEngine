TODO:
- Clarifier la génération de chunk
    - Généré par le serveur (normalement bien en amont, prégénérer large ?)
    - Synchronisé avec le client à la connexion
    - Rendu par le client un fois les infos reçues
      - Le serveur pousse les infos et devine les besoins du client, le client ne rend rien tant qu'il n'a pas reçu d'infos
- Créer des messages
  - CharacterMove (position, velocity, angle) => No ack
  - CharacterChangeTool (ToolId) => Ack
  - CharacterChangeBlock (BlockId) => Ack
  - PlaceBlock (include replace and delete (place air)) => Ack
- Créer un tick qui résoud les changements dans le GameServer
- 