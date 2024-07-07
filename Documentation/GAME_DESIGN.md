# Voxel Engine - Le jeu derrière Dream Builder

## Construction : La Pose

- **Bloc par bloc**
- **En panneau** : lorsque le bloc est posé et le clic maintenu, les autres blocs sont posés à côté en maintenant la collision du curseur projeté sur le plan de la face initialement cliquée.

![](Images/image1.png)

- **En ligne (ie. Dragon Quest Builder)** : Lorsque le clic est maintenu, les blocs sont superposés sur de même axe que celui formé par le bloc support et le bloc posé.

![](Images/imgage2.png)

Note : Les blocs existants ne sont pas remplacés.

## Construction : Le Remplacement

- Idem (bloc/panneau/ligne) mais le bloc support et les blocs non-air sont remplacés.

## Craft

![](Images/image3.png)

Le craft se fait in-world et nécessite de mettre en place un cercle magique adapté.

### Procédure :
1. Mettre en place un cercle gravé reformable.
2. Placer les blocs de la recette.
3. Fermer le cercle pour déclencher l'opération.

### Note :
- Les recettes peuvent ne contenir que 1 ou 2 blocs. Pas besoin de remplir.
- Un outil permet de réaliser directement les objets communs sans cercle (upgrade).
- Démonter un objet permet de récupérer uniquement les matières premières du craft (upgrade).

- Par défaut, démonter un objet donne l'objet complet. Le démontage est possible uniquement si l'outil de démontage est amélioré à un niveau supérieur ou égal à celui de l'objet.
- Les recettes peuvent être découvertes en analysant un objet (outil upgrade) mais la recette doit quand même être réalisée au moins une fois avant de pouvoir être posée dans le monde.

## Aventure

Le personnage du joueur/joueuse commence sur une île dans le ciel.

![](img.png)

Aucun outil, une seule action : rêver, ou plutôt matérialiser un rêve.

Le rêve se matérialise sous forme d'arche dans laquelle on peut ensuite se téléporter.

### Notes :
1. Une seule arche à la fois. Une fois le rêve matérialisé, il faut terminer son aventure avant de lancer un autre rêve.
2. On peut obtenir de manière spéciale des arches supplémentaires et exceptionnelles en posant alors plusieurs arches simultanément.
3. On ne peut pas matérialiser un rêve dans un rêve.

Lorsque l'ouverture d'un rêve est réussie, on débloque alors une nouvelle tuile dans le monde principal. Cette nouvelle tuile contiendra généralement des nouveaux êtres dont on pourra matérialiser un nouveau rêve.

Note : La même arche peut générer des rêves différents. Ex : un rêve de papillon jaune donnera des items différents. PNJ, fournir argent, ingé ou joueur, partir en quête.

Les rêves des personnages principaux seront des aventures clés. L'ouverture du personnage principal sera la dernière à matérialiser pour un autre perso.

Le joueur ne peut pas tomber : le bloc le plus profond est indestructible et une barrière empêche les joueurs de construire ou se déplacer au-delà des bordures.

## Multijoueur

L'objectif est de viser un public de petites communautés.

Au début : une seule tuile pour tout le monde. C'est bis repetita ! Au fur et à mesure que les joueurs et joueuses parcourent des rêves, ils agrandissent leur base et leur univers. De nouveaux PNJ rejoignent l'île et expriment leur besoin et participent à l'évolution de l'île.

Le serveur est open-source ? Disponible gratuitement pour permettre à tous d'héberger leur propre serveur.

Dans un premier temps, pas de système de protection. À terme : permissions et claim de chunks. Ex : max 5 chunks/joueur, distribution progressive. Teams/joueurs/plus/intuitif/more.

## PNJ

Les PNJs sont ajoutés au fur et à mesure lorsque leur rêve est réalisé et que le PNJ souhaite intégrer le monde. Le joueur peut refuser un PNJ, l'accepter ou l'inviter fortement (il ne veut pas rester).

Le nombre de PNJ est limité à 1/chambre. Chaque PNJ est associé à celui qui l'a libéré. Le nombre de PNJ que chaque joueur peut accueillir augmente aussi avec le temps dans une limite de 25 PNJ par joueur.

Les PNJs de joueurs différents peuvent vivre au même endroit mais ne peuvent pas aider d'autres joueurs à gérer leurs PNJs.

## Animaux et Crit

On peut les ramasser comme des blocs mais pas les frapper.

## Créatures Hostiles

Pas de magie offensive. Il faut répliquer et s'adapter à chaque adversaire. Chaque créature a une faille dans ses attaques/déplacements qu'on peut contrer ou au moins esquiver.

## Idée : Sanglier dangereux

Il charge et il faut esquiver au dernier moment pour qu'il s'assomme contre un mur. On peut monter sur son dos pour le désarçonner. Il charge en permanence et il faut l'emmener contre un mur pour l'assommer pour de bon.

## Idée : Release plan

1. Démo : feedbacks sans constats négatifs importants.
2. Steam EA : quand les points de frustration importants sont lissés et que les commentaires sont plutôt positifs.

## Idée : Bibliothèque magique

Le joueur collecte des livres (faits, recettes, etc.) qui par défaut prennent de la place dans l'inventaire. Une fois la bibliothèque magique créée et posée, le joueur peut y poser ses livres et les consulter à distance.

## Idée : Vermine ?

## Idée : Nourriture + collection de gouttes à tout ?

## Idée : Métaux et bois

1. Feu (air chaud par séchage)
2. Séchage vent (vent froid efficace)
3. Critique (rotation lente et efficace)

## Idée : Cinématiques RP

Certains moments ou situations déclenchent des cinématiques mettant en scène le personnage ou l'événement. Par exemple, sauter depuis une hauteur (et amortir la chute), s'asseoir, saut de foi, première construction de maison ou entrée pour la 1ère fois dans la maison d'un autre joueur.

## Roleplay

Les personnages disposent d'un "mode RP". Le mode RP change la caméra pour une vue à la 3ème personne. Les compétences sont remplacées par des emotes, des petites animations du modèle du personnage.

Lorsqu'un personnage interagit de manière contextuellement adaptée à une emote, il gagne des points de RP qui lui permettent de débloquer des nouvelles emotes et de nouvelles cinématiques RP.

Si plusieurs joueurs participent à la même emote cinématique (en réponse au contexte adapté), un multiplicateur de points est appliqué pour chaque joueur participant.

## Construction de pièce

Les constructions suivent un motif protégé et précis appelé des "pièces".

Par exemple, un sol plat clôturé de murs de 4 blocs de haut et d'une porte forme une "pièce vide". Une pièce vide contenant un buffet, une table et 4 chaises minimum est une "salle à manger".

Cette vérification ne fonctionne que sur l'île principale. Le concept est le même que celui de Dragon Quest Builder 2.

Des structures particulières comme des way stones peuvent aussi être considérées comme des pièces.

## L'eau

Le bloc d'eau apporte plusieurs éléments :
1. Une esthétique apaisante et animée (cf. Worldbox)
2. Une mécanique d'écoulement/barrage/inondation (cf. Minecraft)
3. Une magie élémentaire qui complète vent et feu.

### Écoulement en suivant une logique à la Minecraft :
Une source se déverse en blocs de plus petits niveaux ie. cellule, automata.

## Nourriture

Se nourrir n'est pas nécessaire pour les personnages joueurs. La nourriture peut apporter les avantages suivants au joueur :
- Échange avec les PNJ (améliore la dette)
- Bonus divers :
    - Vitesse de déplacement
    - Hauteur de saut
    - Puissance magique
    - Vitesse de fabrication
    - Gain d'expérience (1 fois par jour)
    - Points de vie max / concentration max

### Obtention : la nourriture peut s'obtenir des manières suivantes :
- Échange avec les PNJ (contre de la dette)
- Butin d'aventure
- En faisant pousser les plantes consommables et les arbres fruitiers, puis en les récoltant
- [En ramassant des œufs de poule]

## Matériaux de construction

Les principaux matériaux de construction disponibles pour le joueur sont : terre, pierre, bois, tissu, eaux, différents métaux, sève magique. Chaque matériau peut être travaillé d'une manière ou d'une autre pour obtenir des matériaux raffinés.

### Terre
- La terre est collectée en collectant les blocs en aventure ou sur l'île principale.
- Elle peut être transformée en céramiques (mobiliers, carrelages) ou en pigments jaunes.

### Pierre
- La pierre est collectée en aventure.
- Elle peut être transformée en parquets et revêtements de sols et murs, en pièces/éléments d'architecture (colonnes, ponts, arches) ou en pigments bruns.

### Bois
- Le bois est collecté en abattant des arbres en aventure ou sur l'île principale.
- Il peut être transformé en bois taillé (planches, parquets), en mobilier, en charpente.
- Il peut être cuit en charbon.

### Tissu
- Le tissu est fabriqué par le PNJ "Tisserand". La recette implique la collecte de fibres végétales en aventure ou leur culture sur l'île principale.
- Il peut être utilisé pour fabriquer des vêtements ou du mobilier (rideaux, tapis, moquettes).

### Métaux
- Les métaux sont collectés sous forme de minerais dans les aventures.
- Ils peuvent être transformés en métaux purs par le PNJ "fondeur" ou "fondeuse".

## Gestion d'inventaire

Le personnage joueur dispose d'un inventaire :
- D'outils fixes (chaque outil débloqué a un emplacement dédié, les outils non débloqués sont invisibles).
- De récolte libre : très petit, il permet de récolter 5 à 10 matériaux (selon progression) avec une quantité max réduite (~100).
- De stockage entrepôt à distance.

La sélection principale est l'outil : placer un bloc, supprimer un bloc, remplacer, etc. La sélection secondaire est un slot d'inventaire (bloc, ressource, mobilier) et dépend de l'outil sélectionné.

Le stockage est représenté par une pièce "entrepôt" où le joueur place librement des barils (cf. mod Minecraft). Chaque baril peut contenir jusqu'à 500 unités d'une ressource. La UI est ensuite construite dynamiquement de sorte que les slots représentent le plan de la pièce. Les barils empilés partagent un même slot.

- Le coffre est une pièce.
- La UI est le plan de la pièce.

# Combat

## Coop

Idée attaque: un joueur donne un bout de filet à un autre joueur, les 2 joueurs doivent s'écarte pour que la cible se prenne dedans
Idée attaque: un joueur J1 s'apprête à "prendre appui" sur l'attaque d'un autre. 
  J2 envoi un coup dans J1 qui est propulsé avec une puissance proportionnelle à l'attaque de J2.