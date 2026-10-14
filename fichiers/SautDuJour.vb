Imports System.IO

''' <summary>
''' « Sauter cet evenement aujourd'hui » (clic droit sur une ligne de la minuterie) : la ligne n'est pas
''' jouee a son PROCHAIN passage (aujourd'hui si son heure n'est pas passee, sinon le prochain jour qui
''' lui correspond), puis elle repart normalement. Texte barre en attendant.
''' Gardee dans data\settings\SautDuJour.txt (« AAAAMMJJ|ligne », AAAAMMJJ = le passage saute) :
''' un redemarrage de Slide ne l'oublie pas ; les passages deja passes sont effaces.
''' </summary>
Module SautDuJour

    ' ligne (sans date) -> jour du passage saute (AAAAMMJJ)
    Private ReadOnly _sautees As New Dictionary(Of String, String)
    Private _charge As Boolean = False

    Private Function Fichier() As String
        Return Path.Combine(DossierReglages, "SautDuJour.txt")
    End Function

    Private Function Aujourdhui() As String
        Return Date.Now.ToString("yyyyMMdd")
    End Function

    Private Sub Charger()
        If _charge Then Exit Sub
        _charge = True
        Try
            If File.Exists(Fichier()) Then
                For Each ligne As String In File.ReadAllLines(Fichier())
                    Dim p As Integer = ligne.IndexOf("|"c)
                    If p = 8 AndAlso String.CompareOrdinal(ligne.Substring(0, 8), Aujourdhui()) >= 0 Then _sautees(ligne.Substring(p + 1)) = ligne.Substring(0, 8)
                Next
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Enregistrer()
        Try
            File.WriteAllLines(Fichier(), _sautees.Select(Function(kv) kv.Value & "|" & kv.Key))
        Catch ex As Exception
            DiagLog("Saut : enregistrement impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Cle d'une ligne : celle de la grille et celle de la minuterie en memoire donnent la meme (action comprise : deux lignes a la meme heure ne sont pas confondues).</summary>
    Function Cle(jour As Object, heure As Object, type As Object, action As Object, tri As Object, play As Object, duree As Object, volume As Object) As String
        Return CStr(jour) & "|" & CStr(heure) & "|" & CStr(type) & "|" & CStr(action).Replace("''", "'").Trim() & "|" & CStr(tri) & "|" & CStr(play) & "|" & CStr(duree) & "|" & CStr(volume)
    End Function

    Function CleDeLaLigne(ligne As DataGridViewRow) As String
        Return Cle(ligne.Cells("Jour").Value, ligne.Cells("Heure").Value, ligne.Cells("Type").Value, ligne.Cells("Action").Value, ligne.Cells("tri").Value,
                   ligne.Cells("Play").Value, ligne.Cells("Durée").Value, ligne.Cells("Volume").Value)
    End Function

    ''' <summary>Le jour de la semaine (lundi = 0) correspond-il a la cle de tri (« 04 » vendredi, 7 = lun-ven, 8 = sam-dim, 9 = tous les jours, 10 = jours choisis) ?</summary>
    Private Function JourCorrespond(tri As String, jdls As Integer) As Boolean
        Dim code As Integer = CInt(Val(Left(tri, 2)))
        Select Case code
            Case 9 : Return True
            Case 8 : Return jdls = 5 OrElse jdls = 6
            Case 7 : Return jdls >= 0 AndAlso jdls <= 4
            Case 10 : Return ((CInt(Val(Mid(tri, 7, 3))) >> jdls) And 1) = 1
            Case Else : Return code = jdls
        End Select
    End Function

    ''' <summary>Le prochain jour ou cette ligne est jouee (aujourd'hui si son heure n'est pas passee).</summary>
    Private Function ProchainPassage(tri As String, jour As String) As String
        Dim aujourd As String = Aujourdhui()
        If tri Is Nothing OrElse tri.Length < 6 OrElse UCase(jour) = "BOOT" OrElse UCase(jour) = "SHUTDOWN" Then Return aujourd
        Dim hm As String = Mid(tri, 3, 4)
        For k As Integer = 0 To 7
            Dim d As Date = Date.Today.AddDays(k)
            Dim jdls As Integer = (CInt(d.DayOfWeek) + 6) Mod 7
            If JourCorrespond(tri, jdls) AndAlso (k > 0 OrElse String.CompareOrdinal(hm, Date.Now.ToString("HHmm")) >= 0) Then Return d.ToString("yyyyMMdd")
        Next
        Return aujourd
    End Function

    ''' <summary>Au moins une ligne sautee (evite tout calcul dans l'affichage des tableaux quand il n'y en a pas).</summary>
    Function ADesLignesSautees() As Boolean
        Charger()
        Return _sautees.Count > 0
    End Function

    ''' <summary>Cette ligne de la minuterie est-elle sautee AUJOURD'HUI (appel de la minuterie, a chaque minute) ?</summary>
    Function EstSautee(l As Minuterie) As Boolean
        Charger()
        Dim jour As String = Nothing
        Return _sautees.TryGetValue(Cle(l.Jour, l.Heure, l.Type, l.Action, l.Tri, l.Play, l.Durée, l.Volume), jour) AndAlso jour = Aujourdhui()
    End Function

    ''' <summary>Cette ligne du tableau a-t-elle son prochain passage saute (texte barre, menu) ?</summary>
    Function EstSauteeLaLigne(ligne As DataGridViewRow) As Boolean
        Charger()
        Dim jour As String = Nothing
        Return _sautees.TryGetValue(CleDeLaLigne(ligne), jour) AndAlso String.CompareOrdinal(jour, Aujourdhui()) >= 0
    End Function

    Sub Sauter(ligne As DataGridViewRow, oui As Boolean)
        Charger()
        Dim cle As String = CleDeLaLigne(ligne)
        If oui Then
            _sautees(cle) = ProchainPassage(CStr(ligne.Cells("tri").Value), CStr(ligne.Cells("Jour").Value))
        Else
            _sautees.Remove(cle)
        End If
        Enregistrer()
    End Sub

End Module
