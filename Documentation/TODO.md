TODO:

- Blending + texture from global coordinates
  - Dans le shader, je dois pouvoir trouver:
    - L'index de la texture main de la position
    - L'index de la texture frame de la position
    - L'index de la texture main du xside
    - L'index de la texture frame du xside
    - L'index de la texture main du zside
    - L'index de la texture frame du zside
    - L'index de la texture main du diag
    - L'index de la texture frame du diag
    - Need: conversion coordinates to index
  - Au moment de générer un chunk => upload sur GPU des infos des blocks (main+frame)

- https://dee-dee-r.itch.io/dnd-sdk/devlog/939312/crafting-a-paraboloid-camera-controller


- Rendu végétation
  - un renderer dédié par chunk
  - upload des infos de décorations du chunk (positions, rotation, scale d'herbe) en ComputeBuffer
  - Shader de rendu "indirect"

- Frame change
  - Hard frame (=frame définie)
  - Soft frame (=frame constituée du main de la texture adjacente avec frameheight descendant combiné)

- tester les perfs de génération et de rendu sur des plus grands chunks

- varier le rendu : voxels decorations / tesselation
  - 1 définition des textures via un JSON. OK
```C#
        private static void RefreshTextures() {
            var mtextures = Resources.LoadAll<TextAsset>("Textures/Main");
            _allMainTextures = new ValueDropdownList<string>();
            foreach (var textAsset in mtextures) {
                _allMainTextures.Add(textAsset.name, textAsset.text);
            }

            var ftextures = Resources.LoadAll<TextAsset>("Textures/Frame");
            _allFrameTextures = new ValueDropdownList<string>();
            foreach (var textAsset in ftextures) {
                _allFrameTextures.Add(textAsset.name, textAsset.text);
            }
        }
```

- Clarifier la cible ?

Cas:
- les modifications (ie. pose de blocs) sont identifées par un id uniquement généré par le client. ie. B2 pour le bloc posé par B et A5 pour celui posé par A.
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


Certification

Application name Certbot
Application description Certbot certification

Application key
488132ca887151e6

Application secret
76da7aa3554e2b3fd87a187a9858eaec

Consumer Key
73a43fbef4312c839cf19486d7fcecf2

C:\Users\samue\AppData\Roaming\Python\Python312\Scripts\certbot certonly --dns-ovh --dns-ovh-credentials "%USERPROFILE%\ovh.ini" -d dreambuilder.sametmagda.fr.

C:\Users\samue\AppData\Roaming\Python\Python312\Scripts\certbot renew