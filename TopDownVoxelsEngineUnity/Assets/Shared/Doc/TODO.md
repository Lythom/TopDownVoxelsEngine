TODO:
- Workflow de démarrage d'une partie
  - local
    1. Génération du level ("world")
    2. Création d'un personnage joueur par défaut (0)
    3. Instanciation d'un CharacterAgent pour ce perso (automatique via subscription au state ? Synchronized prefabs ?)
  - Réseau
    - client
      1. Connexion au serveur (identification)
      2. Demande de personnage
      3. Demande de chunks
      4. Rendu
      5. Affichage
    - serveur
      1. Génération de map autour du spawn
      2. Attente de connexion client
- [Serveur] lors d'une demande de connexion
  - Si le personnage existe déjà, ne rien faire sinon associer le joueur identifié à son personnage
  - Si le personnage n'existe pas, créer un nouveau personnage pour ce joueur au spawn.
- Clarifier la génération de chunk
    - Généré par le serveur (normalement bien en amont, prégénérer large ?)
    - Synchronisé avec le client à la connexion
    - Rendu par le client un fois les infos reçues
      - Le serveur pousse les infos et devine les besoins du client, le client ne rend rien tant qu'il n'a pas reçu d'infos
- Créer des messages
  - PlayerJoinFromClient => ack
    - PlayerJoinAckFromServer
  - CharacterMove (position, velocity, angle) => No ack
  - CharacterChangeTool (ToolId) => Ack
    - CharacterChangeToolAck
  - CharacterChangeBlock (BlockId) => Ack
    - CharacterChangeBlockAck
  - PlaceBlock (include replace and delete (place air)) => Ack
    - PlaceBlockAck[Shared.csproj](Shared.csproj)
- Créer un tick qui résoud les changements dans le GameServer
- 