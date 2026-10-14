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

    ''' <summary>Le masque des mouvements sans la ligne Noir et blanc ; si elle etait seule cochee, « aucun mouvement » (numero
    ''' <paramref name="aucun"/>) : la photo reste fixe, en noir et blanc.</summary>
    Friend Function SansNoirEtBlanc(masque As Integer, aucun As Integer) As Integer
        Dim m As Integer = masque And Not (1 << NumeroNoirEtBlanc)
        If m = 0 AndAlso masque <> 0 Then m = 1 << aucun
        Return m
    End Function

    ''' <summary>Vrai si le tirage tombe sur la ligne Noir et blanc (une chance sur le nombre de lignes cochees).</summary>
    Friend Function NoirEtBlancTire(masque As Integer, hasard As Random) As Boolean
        If (masque And (1 << NumeroNoirEtBlanc)) = 0 Then Return False
        Return Tirer(masque, 0, NumeroNoirEtBlanc, hasard) = NumeroNoirEtBlanc
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
