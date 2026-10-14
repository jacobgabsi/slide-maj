Option Strict On
Option Explicit On

Imports System.Data.OleDb
Imports System.IO
Imports System.Text.RegularExpressions

''' <summary>
''' Reparation des lignes de la minuterie dont le jour ou le type a ete enregistre VIDE (06/11/2026, base d'un autre PC : tous les jours, types et volumes etaient
''' vides dans slide.accdb, les lignes s'affichaient sans jour ni type ; le defaut d'enregistrement est corrige depuis le 31/10, mais les bases deja touchees restent vides).
'''
''' Au chargement de la minuterie, une ligne sans jour retrouve son jour par sa cle de tri (« 04HHmm » = vendredi, 000000 = BOOT, 99 = SHUTDOWN, 07/08/09 = Lun-Ven, Sam-Dim, TLJ) ;
''' une ligne sans type retrouve son type par son action (« Chaine 01 :TF1 » = TV, un chemin de fichier = Film, « $Groupe... » = Sonoff, « ... ON / OFF » = Tuya, « Volume ON » = Volume).
''' Une valeur vide qu'on ne peut pas deviner reste vide (le volume, par exemple, est a ressaisir). Seuls les champs VIDES sont remplis, rien d'autre n'est change ;
''' la base est copiee d'abord (slide.accdb.avant-reparation-champs, une seule fois).
''' </summary>
Friend Module ReparationChampsVides

    Private ReadOnly _faites As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    Private Function JourDeLaCle(tri As String) As String
        If tri.Length < 6 Then Return ""
        If tri = "000000" Then Return "BOOT"
        Dim code As Integer = CInt(Val(Left(tri, 2)))
        Select Case code
            Case 99 : Return "SHUTDOWN"
            Case 7 : Return "Lun-Ven"
            Case 8 : Return "Sam-Dim"
            Case 9 : Return "TLJ"
            Case 0 To 6 : Return jourDeLaSemaine(code)
            Case Else : Return ""
        End Select
    End Function

    Private Function TypeDeLAction(action As String, argument As String) As String
        Dim a As String = action.Trim()
        If a = "" Then Return ""
        If a.StartsWith("$"c) Then Return "Sonoff"
        If Regex.IsMatch(a, "^Volume (ON|OFF)$", RegexOptions.IgnoreCase) Then Return "Volume"
        If Regex.IsMatch(a, "^Chaine \d+\s*:", RegexOptions.IgnoreCase) Then Return "TV"
        If argument.Contains("blink://") Then Return "Caméra Blink"
        If Regex.IsMatch(a, "^(\\\\|[A-Za-z]:\\|https?://)") Then Return "Film"
        If Regex.IsMatch(a, "\s(ON|OFF)$", RegexOptions.IgnoreCase) Then Return "Tuya"
        Return ""
    End Function

    ''' <summary>Remplit les jours et les types vides de la base de la minuterie ; rend le nombre de lignes reparees.</summary>
    Friend Function ReparerLesChampsVides(chemin As String) As Integer
        Try
            If String.IsNullOrEmpty(chemin) OrElse Not File.Exists(chemin) Then Return 0
            SyncLock _faites
                If Not _faites.Add(chemin) Then Return 0          ' (une fois par lancement : la base reparee n'a plus rien a reparer)
            End SyncLock

            Dim lignes As New List(Of Object())
            Dim reparees As Integer = 0
            Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                cn.Open()
                Using cmd As New OleDbCommand("SELECT Actif, Jour, Heure, [Type], [Action], Play, [Durée], Volume, tri, Argument FROM MinuterieActive", cn)
                    Using lecteur As OleDbDataReader = cmd.ExecuteReader()
                        While lecteur.Read()
                            Dim v(9) As Object
                            lecteur.GetValues(v)
                            Dim jour As String = Convert.ToString(If(IsDBNull(v(1)), "", v(1)))
                            Dim type As String = Convert.ToString(If(IsDBNull(v(3)), "", v(3)))
                            Dim action As String = Convert.ToString(If(IsDBNull(v(4)), "", v(4)))
                            Dim tri As String = Convert.ToString(If(IsDBNull(v(8)), "", v(8)))
                            Dim argument As String = Convert.ToString(If(IsDBNull(v(9)), "", v(9)))
                            Dim change As Boolean = False
                            If jour.Trim() = "" Then
                                Dim j As String = JourDeLaCle(tri)
                                If j <> "" Then v(1) = j : change = True
                            End If
                            If type.Trim() = "" Then
                                Dim t As String = TypeDeLAction(action, argument)
                                If t <> "" Then v(3) = t : change = True
                            End If
                            If change Then reparees += 1
                            lignes.Add(v)
                        End While
                    End Using
                End Using
            End Using
            If reparees = 0 Then Return 0

            Dim copie As String = chemin & ".avant-reparation-champs"
            If Not File.Exists(copie) Then File.Copy(chemin, copie)

            Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                cn.Open()
                Using transaction As OleDbTransaction = cn.BeginTransaction()
                    Using effacer As New OleDbCommand("DELETE FROM MinuterieActive", cn, transaction)
                        effacer.ExecuteNonQuery()
                    End Using
                    For Each v As Object() In lignes
                        Using ajout As New OleDbCommand("INSERT INTO MinuterieActive VALUES (?,?,?,?,?,?,?,?,?,?)", cn, transaction)
                            For i As Integer = 0 To 9
                                ajout.Parameters.AddWithValue("p" & i, If(IsDBNull(v(i)), CObj(DBNull.Value), v(i)))
                            Next
                            ajout.ExecuteNonQuery()
                        End Using
                    Next
                    transaction.Commit()
                End Using
            End Using
            DiagLog("Base de la minuterie : " & reparees & " ligne(s) sans jour ou sans type reparee(s) (sauvegarde : " & copie & ")")
            Return reparees
        Catch ex As Exception
            DiagLog("Base de la minuterie : reparation des jours et types vides impossible (" & ex.Message & ")")
            Return 0
        End Try
    End Function

End Module
