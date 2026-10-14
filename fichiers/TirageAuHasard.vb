Option Strict On
Option Explicit On

''' <summary>
''' Animations tirees au sort (demande de Jacob, 07/10 : "dans hasard, laisse-moi selectionner les
''' animations, et rajoute l'option aucune animation dans hasard").
'''
''' Quand une liste des reglages est sur "Au hasard" (arrivee et mouvement des photos, transition et
''' mouvement des videos), le tirage se fait parmi les animations cochees dans la fenetre
''' "Choix du hasard..." (FormChoixHasard, ouverte depuis les onglets Photos et Videos).
'''
''' Chaque choix est un nombre dont chaque bit est une animation : le bit N est l'animation numero N
''' de la liste (memes numeros que AnimationPhoto.Arrivee, AnimationPhoto.Mouvement,
''' FormTransitionVideo.Sortie et VideoVivante.Mouvement). Le bit 0, qui serait "Au hasard", veut
''' dire AUCUNE animation pour les arrivees et les transitions (les mouvements ont deja leur
''' "Aucun" dans la liste).
''' </summary>
Friend Module TirageAuHasard

    ' Choix d'origine : ce qui etait tire avant que le choix existe (plus les nouvelles arrivees des photos).
    Friend Const PhotoArriveeParDefaut As Integer = &H7FFE      ' les 14 arrivees, sans "aucune"
    Friend Const PhotoMouvementParDefaut As Integer = &H1E      ' avancer, reculer, glisser, balancer
    Friend Const VideoTransitionParDefaut As Integer = &H66     ' fondu, poussee, zoom, fondu au noir
    Friend Const VideoMouvementParDefaut As Integer = &HE       ' avancer, reculer, glisser

    ''' <summary>Numero de la ligne « Noir et blanc » du tableau des mouvements (15/10/2026) : ce n'est pas un mouvement
    ''' mais un effet de couleur, tire comme les autres lignes puis retire du tirage du mouvement.</summary>
    Friend Const NumeroNoirEtBlanc As Integer = 20

    ''' <summary>Numero de la ligne « Album photo » du tableau des mouvements des PHOTOS (21/10/2026) : ce n'est pas un mouvement mais une mise en page ;
    ''' cochee, un ecran sur n (n = nombre de lignes cochees) montre plusieurs photos en album, quel que soit le reglage « Photo au format album ».</summary>
    Friend Const NumeroAlbum As Integer = 21

    ''' <summary>Numero de la ligne « Sépia » du tableau des mouvements des PHOTOS (26/10/2026) : un effet de couleur, comme Noir et blanc.</summary>
    Friend Const NumeroSepia As Integer = 22

    ''' <summary>Part maximale des photos (ou des videos) montrees en noir et blanc ou en sepia avec « Au hasard » (28/10/2026 : « maximum 10 % des affichages »),
    ''' partagee entre les deux effets quand les deux sont coches.</summary>
    Private Const PartMaxCouleurs As Double = 0.1

    ''' <summary>Le tirage est tombe sur une ligne de couleur (<paramref name="masque"/> = lignes cochees) : la garde avec la probabilite qui ramene
    ''' la part de cet effet a PartMaxCouleurs (divisee par le nombre d'effets de couleur coches), sans jamais depasser la chance du tirage lui-meme.</summary>
    Private Function GarderLaCouleur(masque As Integer, hasard As Random) As Boolean
        Dim lignes As Integer = 0
        For numero As Integer = 0 To NumeroSepia
            If (masque And (1 << numero)) <> 0 Then lignes += 1
        Next
        Dim effets As Integer = 0
        If (masque And (1 << NumeroNoirEtBlanc)) <> 0 Then effets += 1
        If (masque And (1 << NumeroSepia)) <> 0 Then effets += 1
        If lignes = 0 OrElse effets = 0 Then Return False
        Dim partVoulue As Double = PartMaxCouleurs / effets
        Dim partDuTirage As Double = 1.0 / lignes
        Return partDuTirage <= partVoulue OrElse hasard.NextDouble() < partVoulue / partDuTirage
    End Function

    ''' <summary>Vrai si le tirage tombe sur la ligne Sépia (une chance sur le nombre de lignes cochees, ramenee a 10 % au plus avec Noir et blanc).</summary>
    Friend Function SepiaTire(masque As Integer, hasard As Random) As Boolean
        If (masque And (1 << NumeroSepia)) = 0 Then Return False
        Return Tirer(masque, 0, NumeroSepia, hasard) = NumeroSepia AndAlso GarderLaCouleur(masque, hasard)
    End Function

    ''' <summary>Le masque des mouvements sans les lignes Noir et blanc, Sépia et Album ; si elles etaient seules cochees, « aucun mouvement » (numero
    ''' <paramref name="aucun"/>) : la photo reste fixe (en noir et blanc, en sepia ou en album).</summary>
    Friend Function SansNoirEtBlanc(masque As Integer, aucun As Integer) As Integer
        Dim m As Integer = masque And Not (1 << NumeroNoirEtBlanc) And Not (1 << NumeroAlbum) And Not (1 << NumeroSepia)
        If m = 0 AndAlso masque <> 0 Then m = 1 << aucun
        Return m
    End Function

    ''' <summary>Vrai si le tirage tombe sur la ligne Album photo (une chance sur le nombre de lignes cochees).</summary>
    Friend Function AlbumTire(masque As Integer, hasard As Random) As Boolean
        If (masque And (1 << NumeroAlbum)) = 0 Then Return False
        Return Tirer(masque, 0, NumeroSepia, hasard) = NumeroAlbum          ' (le tirage compte aussi la ligne Sepia : une chance sur le nombre de lignes cochees)
    End Function

    ''' <summary>Vrai si le tirage tombe sur la ligne Noir et blanc (une chance sur le nombre de lignes cochees).</summary>
    Friend Function NoirEtBlancTire(masque As Integer, hasard As Random) As Boolean
        If (masque And (1 << NumeroNoirEtBlanc)) = 0 Then Return False
        Return Tirer(masque, 0, NumeroSepia, hasard) = NumeroNoirEtBlanc AndAlso GarderLaCouleur(masque, hasard)          ' (idem : les lignes Album et Sepia comptent dans le tirage)
    End Function

    ''' <summary>Tire un numero parmi ceux coches dans <paramref name="masque"/> (de 0 a
    ''' <paramref name="dernier"/>). Rien de coche, ou plus rien une fois <paramref name="sauf"/>
    ''' ecarte : parmi ceux de <paramref name="parDefaut"/>.</summary>
    ''' <param name="sauf">Numero a ecarter cette fois-ci (-1 : aucun).</param>
    Friend Function Tirer(masque As Integer, parDefaut As Integer, dernier As Integer, hasard As Random, Optional sauf As Integer = -1) As Integer
        Dim candidats As New List(Of Integer)
        For Each choix As Integer In {masque, parDefaut}
            For numero As Integer = 0 To Math.Min(30, dernier)
                If (choix And (1 << numero)) <> 0 AndAlso numero <> sauf Then candidats.Add(numero)
            Next
            If candidats.Count > 0 Then Exit For
        Next
        If candidats.Count = 0 Then Return 1
        Return candidats(hasard.Next(candidats.Count))
    End Function

End Module
