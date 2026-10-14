Option Strict On
Option Explicit On

Imports System.Text.RegularExpressions

''' <summary>
''' Jours PERSONNALISES de la minuterie (demande de Jacob, 28/10/2026 : « dans la colonne jour choisir personnalise », jours de la semaine) : en plus d'un jour, de Lun-Ven, Sam-Dim ou TLJ,
''' une tache peut avoir plusieurs jours au choix. La colonne Jour contient alors « Lun, Mer, Ven » (au moins deux jours, hors Lun-Ven / Sam-Dim / TLJ qui gardent leur nom).
'''
''' Cle de tri : « JJHHmm » comme avant (00-06 un jour, 07 Lun-Ven, 08 Sam-Dim, 09 TLJ, 99 SHUTDOWN) ; pour un jour personnalise « 10HHmm » suivi du masque des jours sur 3 chiffres
''' (bit 0 lundi... bit 6 dimanche) : les jours se lisent dans le tri, comme pour les autres groupes (voir RTConvert).
''' </summary>
Friend Module JoursPerso

    ''' <summary>La valeur de la liste de la colonne Jour qui ouvre le choix des jours.</summary>
    Friend Const Libelle As String = "Personnalisé…"

    Private ReadOnly Abreges As String() = {"Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"}
    Private Const Lun_Ven As Integer = &H1F          ' 31
    Private Const Sam_Dim As Integer = &H60          ' 96
    Private Const TousLesJours As Integer = &H7F     ' 127

    Private ReadOnly _liste As New Regex("^\s*(?:lun|mar|mer|jeu|ven|sam|dim)(?:\s*[,;+/]\s*(?:lun|mar|mer|jeu|ven|sam|dim))+\s*$", RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)
    Private ReadOnly _jour As New Regex("lun|mar|mer|jeu|ven|sam|dim", RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)

    Private Function NombreDeJours(masque As Integer) As Integer
        Dim n As Integer = 0
        For i As Integer = 0 To 6
            If (masque And (1 << i)) <> 0 Then n += 1
        Next
        Return n
    End Function

    ''' <summary>Masque des jours d'un texte « Lun, Mer, Ven » (bit 0 = lundi) ; 0 si ce n'est pas une liste de jours.</summary>
    Friend Function Masque(jour As String) As Integer
        If jour Is Nothing OrElse Not _liste.IsMatch(jour) Then Return 0
        Dim m As Integer = 0
        For Each trouve As Match In _jour.Matches(jour)
            Dim i As Integer = Array.FindIndex(Abreges, Function(a) String.Equals(a, trouve.Value, StringComparison.OrdinalIgnoreCase))
            If i >= 0 Then m = m Or (1 << i)
        Next
        Return If(NombreDeJours(m) >= 2, m, 0)
    End Function

    Friend Function EstPerso(jour As String) As Boolean
        Return Masque(jour) <> 0
    End Function

    ''' <summary>« Lun, Mer, Ven » pour un masque.</summary>
    Friend Function Texte(masque As Integer) As String
        Dim noms As New List(Of String)
        For i As Integer = 0 To 6
            If (masque And (1 << i)) <> 0 Then noms.Add(Abreges(i))
        Next
        Return String.Join(", ", noms)
    End Function

    ''' <summary>La valeur de la colonne Jour pour un masque : le nom du jour s'il n'y en a qu'un, « Lun-Ven », « Sam-Dim » ou « TLJ » pour ces groupes, sinon la liste « Lun, Mer, Ven ».</summary>
    Friend Function ValeurDuJour(masque As Integer) As String
        masque = masque And TousLesJours
        If masque = TousLesJours Then Return "TLJ"
        If masque = Lun_Ven Then Return "Lun-Ven"
        If masque = Sam_Dim Then Return "Sam-Dim"
        If NombreDeJours(masque) = 1 Then
            For i As Integer = 0 To 6
                If masque = (1 << i) Then Return jourDeLaSemaine(i)
            Next
        End If
        Return Texte(masque)
    End Function

    ''' <summary>Masque d'une valeur quelconque de la colonne Jour (un jour, Lun-Ven, Sam-Dim, TLJ ou une liste) ; 0 pour BOOT, SHUTDOWN ou une valeur inconnue.</summary>
    Friend Function MasqueDuJour(jour As String) As Integer
        Dim j As String = If(jour, "").Trim().ToUpperInvariant()
        Select Case j
            Case "LUN-VEN" : Return Lun_Ven
            Case "SAM-DIM" : Return Sam_Dim
            Case "TLJ" : Return TousLesJours
        End Select
        For i As Integer = 0 To 6
            If jourDeLaSemaine(i).ToUpperInvariant() = j Then Return 1 << i
        Next
        Return Masque(jour)
    End Function

    ''' <summary>Cle de tri d'une ligne de la minuterie (jour + heure « HH:mm »).</summary>
    Friend Function CodeTri(jour As String, heure As String) As String
        Dim h As String = If(heure, "")
        Dim hhmm As String = If(h.Length >= 5, h.Substring(0, 2) & h.Substring(3, 2), "0000")
        Dim j As String = If(jour, "").Trim().ToUpperInvariant()
        Dim m As Integer = Masque(jour)
        If m <> 0 Then Return "10" & hhmm & m.ToString("000")
        Dim t As Integer = 0
        For i As Integer = 0 To 6
            If jourDeLaSemaine(i).ToUpperInvariant() = j Then t = i
        Next
        If j = "LUN-VEN" Then t = 7
        If j = "SAM-DIM" Then t = 8
        If j = "TLJ" Then t = 9
        If j = "SHUTDOWN" Then t = 99          ' (jamais joue par une heure : seulement a la fermeture par Ctrl + Quitter)
        Return t.ToString("00") & hhmm
    End Function

End Module
