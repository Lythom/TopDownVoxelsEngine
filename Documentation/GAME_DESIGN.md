# Dream Builder

## L'univers

Dans Dream Builder, les personnages joueurs ont un pouvoir particulier : ils peuvent matérialiser les rêves. Les thèmes des rêves et cauchemars sont au cœur du jeu, offrant une toile narrative et mécanique riche. Le principe de progression est le suivant : on construit son monde en explorant les rêves et en y puisant les ressources dont on a besoin.

## Construction : La Pose

La construction est un élément central du gameplay, offrant plusieurs méthodes intuitives pour placer les blocs :

-   **Bloc par bloc** : La méthode de base où chaque clic place un seul bloc.
-   **En panneau** : Lorsque le bloc est posé et le clic maintenu, les autres blocs sont posés à côté en maintenant la collision du curseur projeté sur le plan de la face initialement cliquée, permettant de construire rapidement des surfaces planes.
    ![](Images/image1.png)
-   **En ligne (inspiré de Dragon Quest Builder)** : Lorsque le clic est maintenu, les blocs sont superposés sur le même axe que celui formé par le bloc support et le bloc posé, idéal pour créer des murs ou des tours élevés de manière efficace.
    ![](Images/imgage2.png)

Note : Les blocs existants ne sont pas remplacés par défaut lors de la pose.

## Construction : Le Remplacement

Cette fonctionnalité reprend les mêmes mécanismes que la "Pose" (bloc/panneau/ligne) mais avec une différence cruciale : le bloc support et tous les blocs non-aériens (c'est-à-dire les blocs solides existants) sont remplacés par le nouveau bloc. Cela permet des modifications rapides et radicales des structures existantes.

## Craft

![](Images/image3.png)

Le craft se fait "in-world" et nécessite une interaction physique avec l'environnement.

### Procédure :
1.  **Mise en place du cercle magique** : Le joueur doit d'abord élaborer et poser un cercle gravé, qui est reformable et peut être réutilisé.
2.  **Placement des blocs de recette** : Les blocs spécifiques requis par la recette sont ensuite placés à l'intérieur du cercle.
3.  **Fermeture du cercle** : Une fois tous les éléments en place, le joueur doit "fermer" le cercle, ce qui déclenche l'opération de craft et matérialise l'objet.

### Notes sur le Craft :
-   **Flexibilité des recettes** : Les recettes ne nécessitent pas toujours de remplir tout le cercle ; certaines peuvent ne contenir que 1 ou 2 blocs.
-   **Outil de craft direct** : Un outil spécifique sera disponible pour réaliser directement les objets "communs" sans nécessiter l'utilisation d'un cercle magique. Cet outil peut être amélioré via la progression.
-   **Démontage et récupération des ressources** :
  -   Par défaut, démonter un objet donne l'objet complet si l'outil de démontage est amélioré à un niveau supérieur ou égal à celui de l'objet.
  -   Un "upgrade" permet, lors du démontage d'un objet, de récupérer uniquement les matières premières ayant servi à son craft, favorisant le recyclage et la gestion des ressources.
-   **Découverte de recettes** : Les recettes peuvent être découvertes en analysant un objet existant (via un outil d'analyse, lui-même un "upgrade"). Cependant, même après avoir été découverte, la recette doit être réalisée au moins une fois de manière explicite (avec le cercle magique) avant que l'objet puisse être posé ou crafté plus facilement dans le monde.

**Mini-jeu de matérialisation**
Le cercle de miniaturisation fonctionne avec un mini-jeu : si on attend peu, l'objet est "grand" et si on attend trop longtemps, l'objet est "petit".

## Aventure

Le personnage du joueur/joueuse commence sur une île flottante dans le ciel.
![](img.png)

Au départ, le joueur ne dispose d'aucun outil, et une seule action est possible : *rêver*, ou plutôt, **matérialiser un rêve**.

Le rêve matérialisé prend forme sous une arche dans laquelle le joueur peut se téléporter.

### Gestion des rêves et arches :
1.  **Arche unique** : Initialement, une seule arche peut être matérialisée à la fois. Une fois un rêve lancé, il faut le mener à terme avant d'en initier un autre.
2.  **Arches supplémentaires** : Il est possible d'obtenir des arches supplémentaires et exceptionnelles par des moyens spéciaux, permettant de lancer et de gérer plusieurs rêves simultanément.
3.  **Interdiction du rêve dans le rêve** : Il n'est pas possible de matérialiser un rêve depuis l'intérieur d'un autre rêve.

### Progression de l'aventure :
Lorsqu'un rêve est exploré et "réussi" (c'est-à-dire que son objectif est atteint), une nouvelle tuile se débloque et s'ajoute au monde principal. Ces nouvelles tuiles contiennent généralement de nouveaux êtres (PNJ ou créatures) dont il est possible de matérialiser un nouveau rêve, créant un cycle d'expansion du monde.

**Variété des rêves** : La même arche peut générer des rêves différents. Par exemple, il pourrait s'agir d'un PNJ qui offre de l'argent, d'un ingénieur, ou d'un joueur qui part en quête.

**Rêves des personnages principaux** : Les rêves des personnages principaux sont des aventures clés, souvent liées à la narration épisodique. Note importante : l'ouverture du rêve d'un personnage principal sera la dernière à matérialiser pour un autre personnage clé, indiquant une progression ou une résolution finale pour ce personnage.

**Sécurité du joueur** : Le joueur ne peut pas tomber de l'île principale. Le bloc le plus profond du monde est indestructible, et une barrière invisible empêche les joueurs de construire ou de se déplacer au-delà des bordures de l'île existante.

## Multijoueur

L'objectif du jeu est de cibler un public de petites communautés, favorisant la collaboration et l'expansion collective.

**Démarrage du serveur** : Au début, tous les joueurs partagent une seule tuile commune. À mesure que les joueurs parcourent des rêves et agrandissent leur base, de nouveaux PNJ rejoignent l'île principale, expriment leurs besoins et participent activement à l'évolution de l'île collective.

**Accessibilité des serveurs** : Le serveur est envisagé comme étant open-source et disponible gratuitement, permettant à n'importe qui d'héberger sa propre communauté de Dream Builder.

**Protection et permissions des constructions** :
-   Dans un premier temps, il n'y aura pas de système de protection des constructions.
-   À terme, des systèmes de permissions et de "claim" de chunks seront implémentés. Une idée est de limiter le nombre de chunks par joueur (ex: max 5 chunks/joueur), distribués progressivement.
-   Le système sera conçu pour être intuitif et favoriser des interactions fluides au sein des équipes ou des groupes de joueurs.

## PNJ (Personnages Non-Joueurs)

Les PNJ sont ajoutés au fur et à mesure que leurs rêves sont réalisés. Une fois le rêve terminé, le PNJ peut souhaiter intégrer le monde principal du joueur. Le joueur a plusieurs options :
-   **Refuser** un PNJ.
-   **Accepter** un PNJ.
-   **Inviter fortement** un PNJ qui serait réticent à rester.

**Logement des PNJ** :
-   Le nombre de PNJ est limité à 1 par *chambre* construite.
-   Chaque PNJ est associé au joueur qui l'a "libéré" (c'est-à-dire, dont le rêve a été matérialisé).
-   Le nombre total de PNJ que chaque joueur peut accueillir augmente avec le temps, dans une limite de 25 PNJ par joueur.
-   Les PNJ de joueurs différents peuvent vivre au même endroit sur l'île principale mais ne peuvent pas aider d'autres joueurs à gérer leurs propres PNJ.

**Horaires des PNJ** : Les PNJ ont d'autres rêves de repos et de sieste à utiliser pour entrer dans leurs rêves. Tant que les joueurs sont dans le rêve, le reste est "ouvert". Tant que le rêve est ouvert, le PNJ prolonge son sommeil et est indisponible pour d'autres interactions.

### Métiers des PNJ :
Chaque PNJ possède un métier qui offre des avantages spécifiques à la ville :
-   **Batisseuse / Batisseur** (Minéraux, carrelage, bois, métal, sève magique, textile)
-   **Cuisinière / Cuisinier** (Gestion de la nourriture)
-   **Administratrice / Administrateur** (Aides diverses)
-   **Fabricante d'armes** (Bois, fer, magiques)
-   **Fabricante d'armures** (Bois, fer, magiques)
-   **Soigneuse / Soigneur**
-   **Guide spirituels** (Philosophie)
-   **Fabricante de vetement**
-   **Créatrice/Créateur de sorts**
-   **Explorêveuse** ("Fabrique" et vend des rêves à explorer)
-   **Peintre** (Modification des couleurs : voir section Matériaux, pigmentation)
-   **Teinturier**

**Progression des PNJ** :
-   Chaque PNJ a des spécialités dans son métier.
-   On peut recruter directement des PNJ plus expérimentés, mais cela est plus coûteux et difficile.
-   Au début, on obtiendra les libellés "Appels" avec des recettes simples. Ils pourront progresser.
-   On peut leur apprendre des recettes, mais cela nécessite un niveau requis de l'artisan local pour être apprises.

### Recrutement de PNJ et lieux requis :
-   **0 PNJ** : Dortoir
-   **1 PNJ** : Atelier
-   **2 PNJ** : Maison + Atelier
-   **3 PNJ et plus** : Boutique

## Animaux et Crit

Les animaux et certaines créatures peuvent être ramassés comme des blocs, mais ne peuvent pas être frappés, indiquant une mécanique de capture ou d'interaction non-violente pour les intégrer au monde du joueur.

## Créatures Hostiles

Le jeu ne comprend pas de magie offensive directe. Pour contrer les créatures hostiles, les joueurs doivent répliquer et s'adapter à chaque adversaire. Chaque créature possède une faille spécifique dans ses attaques ou déplacements que le joueur peut exploiter pour la contrer ou l'esquiver efficacement.

### Idée de créature : Sanglier dangereux
Ce sanglier charge. La stratégie consiste à l'esquiver au dernier moment pour qu'il s'assomme contre un mur. Une fois assommé, le joueur peut monter sur son dos pour le désarçonner. Le sanglier charge en permanence, et le but est de le manœuvrer pour qu'il percute un mur et soit assommé indéfiniment.

## Combat

Le système de vie dans Dream Builder est inversé : la barre de PV est inversée. Les joueurs accumulent des "stacks" d'immobilisation (par exemple, par des entraves, des pièges environnementaux, des sols mouvants, des actions immobilisantes ou l'assommoir) et doivent réussir une action de "capture" une fois leur barre pleine d'immobilisation.

Pour un boss, la capture nécessite une canalisation un peu plus longue, où défendre le personnage qui canalise est un enjeu de réussite.

### Attaques coopératives (Idées) :
-   **Piège coopératif** : Un joueur donne un bout de filet à un autre joueur. Les deux joueurs doivent alors s'écarter de manière coordonnée pour que la cible se prenne dans le filet étendu entre eux.
-   **Propulsion synergique** : Un joueur (J1) s'apprête à "prendre appui" sur l'attaque d'un autre joueur (J2). J2 envoie un coup dans J1, qui est propulsé avec une puissance proportionnelle à l'attaque de J2, permettant des positionnements stratégiques ou des attaques combo.

## Idée : Release plan

1.  **Démo** : L'objectif de la démo est de recueillir des retours sans constats négatifs majeurs.
2.  **Steam Early Access (EA)** : Le jeu entrera en accès anticipé une fois que les points de frustration importants auront été lissés et que les commentaires initiaux seront majoritairement positifs.

## Idée : Bibliothèque magique

Les joueurs collectent des livres (contenant des faits, des recettes, etc.) qui, par défaut, occupent de l'espace dans l'inventaire. Une fois la "bibliothèque magique" créée et posée dans le monde, le joueur peut y déposer ses livres pour les consulter à distance, libérant ainsi son inventaire.

## Idée : Vermine ?
(Ce point est noté comme "Vermine ?", suggérant une idée potentielle pour des créatures mineures ou des nuisibles, mais sans détails supplémentaires).

## Idée : Nourriture + collection de gouttes à tout ?
(Cette note est lapidaire et semble indiquer une interaction entre la nourriture et le système de butin ("gouttes"), potentiellement que la nourriture pourrait influencer la qualité ou la quantité des butins, ou qu'elle est elle-même une forme de "drop" générique).

## Idée : Métaux et bois
Cette section semble faire référence à des processus de traitement des matériaux, plutôt qu'aux matériaux eux-mêmes :
1.  **Feu (air chaud par séchage)** : Processus potentiellement lié à la transformation du bois, ou à la cuisson de certains minerais/terres.
2.  **Séchage vent (vent froid efficace)** : Une méthode de séchage alternative, probablement pour des matériaux spécifiques.
3.  **Critique (rotation lente et efficace)** : Cela pourrait faire référence à un processus de polissage, de meulage, ou de raffinage impliquant une rotation contrôlée pour obtenir des matériaux de haute qualité.

## Idée : Cinématiques RP

Certains moments ou situations spécifiques déclencheront des cinématiques immersives mettant en scène le personnage du joueur ou un événement clé. Exemples :
-   Sauter depuis une grande hauteur (avec animation d'amorti de la chute).
-   S'asseoir.
-   Un "saut de foi".
-   La première construction d'une maison.
-   La première entrée dans la maison d'un autre joueur.
-   "Applaudissements" : Dans une nouvelle pièce (la première fois que le joueur construit cette pièce).

## Roleplay

Les personnages disposent d'un "mode RP" dédié. En activant ce mode :
-   La caméra bascule en vue à la 3ème personne.
-   Les compétences habituelles sont remplacées par des "emotes", qui sont de petites animations du modèle du personnage.

**Progression RP** : Lorsqu'un personnage interagit de manière contextuellement adaptée avec une emote (par exemple, "saluer" un nouveau joueur, "s'émerveiller" devant une belle construction), il gagne des **points de RP**. Ces points permettent de débloquer de nouvelles emotes et de nouvelles cinématiques RP.

**Bonus multijoueur RP** : Si plusieurs joueurs participent à la même emote cinématique (en réponse à un contexte adapté commun), un multiplicateur de points est appliqué pour chaque joueur participant, encourageant l'interaction et le jeu de rôle en groupe.

### Emotes (Exemples et déclencheurs) :
-   **Salut chaleureux** :
  -   Première apparition dans le jeu.
  -   Longtemps sans voir un autre joueur.
-   **Lecture mystique** :
  -   Après l'acquisition d'une nouvelle pièce.
  -   Emote de quête (recherche d'information).
-   **Danse des nuages** :
  -   Au sommet d'une montagne.
  -   Après la constatation d'une réponse.
  -   Au milieu des nilcaens (anomalie de lecture, à confirmer).
-   **Méditation flottante** :
  -   Dans un "spectacle" (Scène, arbre géant, devant un lac).
  -   Au lever du soleil.
  -   Dans un temple ou un lieu spirituel.
-   **Sabbat Rive Céleste** : (Anomalie de lecture, pourrait être "Sursaut de Vie Céleste" ou similaire)
  -   Après la découverte d'un trésor.
  -   Après un affrontement difficile.
  -   Après avoir évité un piège.
-   **Émerveillement** :
  -   Première entrée dans une pièce bien décorée.
-   **Cuisson magnifique** :
  -   Dans une cuisine.
  -   Devant un feu de camp.
-   **Moue d'approbation** :
  -   Un joueur rate sa figure d'emote contextuelle (Anomalie de lecture, pourrait être "Un joueur rate son geste").
-   **Taint d'indécision** : (Anomalie de lecture, pourrait être "Temps d'indécision" ou "Teinte d'indécision")
-   **Roulement de tambour** :
  -   Avant les résultats.
  -   Devant une construction.
  -   Avant l'ouverture d'un coffre.
  -   Avant la présentation d'un personnage.

### Situations déclenchantes d'emotes et RP :
-   Première connexion au jeu.
-   Le matin (si on a dormi et qu'on fait face à un joueur qui a dormi).
-   Croiser un joueur non-cotoyé depuis +d'1 semaine (IRL).
-   Construction d'une pièce pour la première fois.

## Construction de pièce

Les constructions peuvent suivre des motifs protégés et précis appelés "pièces". Le jeu reconnaît et valide la construction de ces pièces spécifiques.

Par exemple :
-   Un sol plat clôturé de murs de 4 blocs de haut et d'une porte forme une "**pièce vide**".
-   Une pièce vide contenant un buffet, une table et 4 chaises minimum est reconnue comme une "**salle à manger**".

**Règles de validation** : Cette vérification et reconnaissance des pièces ne fonctionne que sur l'île principale, un concept similaire à celui de Dragon Quest Builder 2.

**Types de pièces** : Des structures particulières comme des "way stones" (pierres de chemin) peuvent également être considérées comme des pièces, ayant potentiellement des fonctions spécifiques (téléportation, repère).

## L'eau

Le bloc d'eau est un ajout dynamique au monde, qui apporte plusieurs éléments de gameplay et d'esthétique :
1.  **Esthétique apaisante et animée** : Similaire à Worldbox, l'eau offre un élément visuel agréable et fluide.
2.  **Mécanique d'écoulement/barrage/inondation** : Tout comme dans Minecraft, l'eau interagit avec l'environnement, permettant la création de rivières, barrages ou même des inondations contrôlées ou accidentelles.
3.  **Magie élémentaire** : L'eau complète le "vent" et le "feu" comme un élément de magie, suggérant des interactions magiques ou des ressources spécifiques liées à l'eau.

### Détail de l'écoulement :
L'écoulement de l'eau suit une logique de "cellule/automata" similaire à Minecraft : une source d'eau se déverse en blocs de niveaux de plus en plus bas, créant un système d'écoulement réaliste.

## Nourriture

Se nourrir n'est pas une nécessité vitale pour les personnages joueurs, mais la nourriture offre des avantages significatifs :
-   **Échange avec les PNJ** : La nourriture peut être utilisée pour interagir et améliorer la "dette" (ou relation) avec les PNJ.
-   **Bonus divers pour le joueur** :
  -   Vitesse de déplacement accrue.
  -   Hauteur de saut augmentée.
  -   Puissance magique améliorée.
  -   Vitesse de fabrication accélérée.
  -   Gain d'expérience (limité à une fois par jour).
  -   Augmentation des points de vie maximum ou de la concentration maximum.

### Obtention de la nourriture :
-   **Échange avec les PNJ** : Contre de la "dette" (ou équivalent monétaire/relationnel).
-   **Butin d'aventure** : Trouvée comme récompense ou "drop" dans les rêves.
-   **Agriculture** : En faisant pousser des plantes consommables et des arbres fruitiers, puis en les récoltant sur l'île principale.
-   **Ramassage** : Ramassage d'œufs de poule (spécifié entre crochets, donc potentiellement une idée non encore finalisée).

## Matériaux de construction

Les principaux matériaux de construction disponibles pour le joueur sont : la terre, la pierre, le bois, le tissu, l'eau, différents métaux, et la sève magique. Chaque matériau peut être travaillé de diverses manières pour obtenir des matériaux raffinés et des colorants.

| Matériaux   | Brut                  | Cuit                   | Taillé               | Infusé (Magie)   | Chromite          |
| :---------- | :-------------------- | :--------------------- | :------------------- | :--------------- | :---------------- |
| **Terre**   | Blocs de terre        | Céramiques (mob., car.)| Tuiles, Pigment jaune|                  |                   |
| **Pierre**  | Blocs de pierre       |                        | Parquets, Revêtements| Pigment brun     | Minerais          |
| **Bois**    | Rondins               | Charbon                | Planches, Mob., Char.| Creuset          |                   |
| **Tissu**   | Fibres végétales      |                        | Vêtements, Mobiliers | Valenor (tissage)|                   |
| **Eau**     | Eau                   |                        |                      |                  |                   |
| **Métal**   | Minerais (fer, cobalt)| Lingots                |                      |                  | Chromite, Titanex |
| **Magie**   | Sève de sang          |                        |                      | Engrais          |                   |

**Table des couleurs et matériaux de teinture** :
-   **Jaune** : Fer (Terre)
-   **Rouge** : Fer (Minerais)
-   **Brun** : Fer (Pierre)
-   **Vert** : Chrome (Chromite)
-   **Bleu** : Cobalt (Bleu) (Minerais)
-   **Rose** : Cobalt (Rose) (Minerais)
-   **Blanc** : Titane (Minerais)
-   **Noir** : Fer (Minerais)

### Détail par Matériau :
-   **Terre** : Collectée en brisant des blocs en aventure ou sur l'île principale. Transformable en céramiques (mobiliers, carrelages) en étant cuite, ou en pigments jaunes pour la teinture.
-   **Pierre** : Collectée en aventure. Transformable en parquets, revêtements de sols et murs, ou en pièces/éléments d'architecture (colonnes, ponts, arches). Peut également produire des pigments bruns.
-   **Bois** : Collecté en abattant des arbres en aventure ou sur l'île principale. Transformable en bois taillé (planches, parquets, mobiliers, charpente). Peut être cuit en charbon.
-   **Tissu** : Fabriqué par le PNJ "Tisserand" qui collecte des fibres végétales en aventure ou les cultive sur l'île principale. Utilisé pour les vêtements et mobiliers (rideaux, tapis, moquettes).
-   **Métaux** : Collectés sous forme de minerais dans les aventures. Transformables en métaux purs (lingots) par le PNJ "Fondeur" ou "Fondeuse". Les métaux sont également la source de nombreux pigments de couleur.

## Gestion d'inventaire

Le personnage joueur dispose d'un inventaire structuré :
-   **Outils fixes** : Chaque outil débloqué possède un emplacement dédié et persistentuel. Les outils non débloqués sont invisibles.
-   **Récolte libre** : Une petite section d'inventaire temporaire, permettant de récolter 5 à 10 matériaux (selon la progression du joueur) avec une quantité maximale réduite par stack (~100 unités).
-   **Stockage entrepôt à distance** : Un système d'inventaire à distance pour le stockage à grande échelle.

**Interface d'accès rapide** : La sélection principale sur l'interface du joueur est l'outil actif (ex: placer un bloc, supprimer un bloc, remplacer). La sélection secondaire est un slot d'inventaire (bloc, ressource, mobilier) et dépend de l'outil sélectionné, permettant un accès rapide et contextuel aux objets.

**Système d'entrepôt physique** : Le stockage est représenté physiquement par une "pièce entrepôt" que le joueur doit construire et aménager. Dans cette pièce, le joueur place librement des barils (similaire aux mods de stockage de Minecraft). Chaque baril peut contenir jusqu'à 500 unités d'une ressource spécifique. L'interface utilisateur (UI) de l'entrepôt est construite dynamiquement pour représenter le plan de la pièce, et les barils empilés partagent un même slot, optimisant l'espace visuel.

**Le coffre est une pièce** : Un coffre en tant qu'objet de stockage est également considéré comme une "pièce" dans le système de reconnaissance de pièces, avec son interface utilisateur reflétant le plan de cette pièce.

---