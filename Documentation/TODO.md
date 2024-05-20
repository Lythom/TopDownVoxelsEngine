TODO:

- tester les perfs de génération et de rendu sur des plus grands chunks
  - optimiser "3 quad par voxel + half offset and flip the back"

- varier le rendu : voxels decorations / tesselation
- Clarifier la cible ?

Cas:
- les modifications (ie. pose de blocs) dont identifées par un id uniquement généré par le client. ie. B2 pour le bloc posé par B et A5 pour celui posé par A.
- A pose un Bloc et enregistre localement sa modification dans une liste ordonnée "en attente de validation"
- B pose un bloc au même endroit en même temps et enregistre localement sa modification dans une liste ordonnée "en attente de validation"
- le serveur reçoit B puis A et applique dans cet ordre. Le serveur de maintient pas de liste de validation car il applique dans l'ordre d'arrivée.
  - Le serveur envoi ok à B et broadcastà A la modification
  - Le serveur envoi nok à A
- B reçoit ok(B2)
  - il supprime B2 de sa liste
  - tant que le premier élément de sa liste est une modification d'un autre joueur, il supprime l'élément (lite vide)
- entre temps, A pose un bloc (A6) et enregistre localement cette modification dans sa liste "en attente de validation"
- A reçoit B2
  - il tente d'appliquer B2 lors de la réception du broacast mais erreur (ou pas). Il garde B2 dans sa liste "en attente de validation".
- A reçoit nok(A5)
  - il rollback dans l'ordre inverse toute sa liste d'en attente jusqu'à A5 inclus: A6, B2 (sans effet) puis A5
  - il supprime A5 de sa liste (restent B2 et A6)
  - il réapplique dans l'ordre toutes les modifications de sa liste "en attente de validation" (B2 puis A6)
  - tant que le premier élément de sa liste est une modification d'un autre joueur, il supprime l'élément (reste A6)
- A reçoit ok(A6)
  - il supprime A6 de sa liste
  - tant que le premier élément de sa liste est une modification d'un autre joueur, il supprime l'élément (lite vide)

Done:
- [Serveur] lors d'une demande de connexion
  - Si le personnage existe déjà, ne rien faire sinon associer le joueur identifié à son personnage
  - Si le personnage n'existe pas, créer un nouveau personnage pour ce joueur au spawn.
- Créer des messages
  - PlayerJoinFromClient
- Créer un tick qui résoud les changements dans le GameServer
- Clarifier la génération de chunk
  - Généré par le serveur (normalement bien en amont, prégénérer large ?)
  - Synchronisé avec le client à la connexion
  - Rendu par le client un fois les infos reçues
    - Le serveur pousse les infos et devine les besoins du client, le client ne rend rien tant qu'il n'a pas reçu d'infos

- Routine de persistance des chunks


- synchro des chunks
  - transmis uniquement une fois et automatiquement à l'approche du personnage
  - ensuite synchro simultanée block par block via application séquentielle des messages PlaceBlock
  - note: place block => Ajouter la possibilité de undo
  - garder la liste ordonnées des X dernières secondes (ou juste une rotation queue d'un nombre fixe ?)
  - SI modification antérieur et sur le même chunk indiquée par le serveur après application optimiste
    - Ce cas de figure est détecté au moment où le joueur reçoit la réponse du serveur

- Refaire le shader en URP ?
