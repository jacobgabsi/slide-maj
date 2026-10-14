Option Strict On
Option Explicit On

Imports System.Data.OleDb
Imports System.IO

''' <summary>
''' Colonne "Argument" de la minuterie en TEXTE LONG (demande de Jacob, 14/10/2026 : "rends le champ extensible").
'''
''' Les reglages d'une tache (titre, PIP, boucle... et, pour un film IPTV choisi dans Nflix, son adresse "!/Url:" et
''' sa source) sont ranges dans la colonne Argument de la table MinuterieActive. Creee en "texte" par Access, cette
''' colonne est limitee a 255 caracteres : l'adresse d'un film IPTV (serveur + utilisateur + mot de passe + numero)
''' suffit presque a la remplir.
'''
''' Au chargement de la minuterie, la colonne est passee en "Memo" (texte long) si elle ne l'est pas encore :
'''  - une copie de la base est faite avant (slide.accdb.avant-argument-long, une seule fois) ;
'''  - rien n'est perdu : les valeurs sont gardees telles quelles (verifie sur une copie de la base de Jacob) ;
'''  - si la base ne peut pas etre modifiee (ouverte ailleurs, en lecture seule), la limite de 255 reste en vigueur
'''    (ReglagesLongsPossibles = Faux) et Slide continue comme avant.
''' </summary>
Friend Module BaseMinuterie

    ''' <summary>Vrai quand la colonne Argument de la base en service accepte plus de 255 caracteres.</summary>
    Friend ReglagesLongsPossibles As Boolean = False

    ''' <summary>Vrai quand la colonne Action de la base en service accepte plus de 255 caracteres (04/11/2026 : les reglages d'option d'une tache Tuya font des textes longs).</summary>
    Friend ActionLonguePossible As Boolean = False
    Private ReadOnly _faitsAction As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>La colonne Action de la minuterie passe en texte long (comme Argument), avec une copie de la base avant (une seule fois). Sans effet si c'est deja fait ou si la base ne peut pas etre modifiee.</summary>
    Private Sub PreparerLaColonneAction(chemin As String)
        Try
            SyncLock _faitsAction
                Dim dejaFait As Boolean
                If _faitsAction.TryGetValue(chemin, dejaFait) Then
                    ActionLonguePossible = dejaFait
                    Exit Sub
                End If
            End SyncLock
            Dim longue As Boolean = False
            Try
                If ColonneEnTexteLong(chemin, "Action") Then
                    longue = True
                Else
                    Dim copie As String = chemin & ".avant-action-long"
                    If Not File.Exists(copie) Then File.Copy(chemin, copie)
                    Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                        cn.Open()
                        Using cmd As New OleDbCommand("ALTER TABLE MinuterieActive ALTER COLUMN [Action] MEMO", cn)
                            cmd.ExecuteNonQuery()
                        End Using
                    End Using
                    longue = ColonneEnTexteLong(chemin, "Action")
                    DiagLog("Base de la minuterie : colonne Action en texte long (" & If(longue, "ok", "echec") & "), sauvegarde : " & copie)
                End If
            Catch ex As Exception
                DiagLog("Base de la minuterie : colonne Action laissee a 255 caracteres (" & ex.Message & ")")
            End Try
            SyncLock _faitsAction
                _faitsAction(chemin) = longue
            End SyncLock
            ActionLonguePossible = longue
        Catch ex As Exception
            DiagLog("Base de la minuterie (Action) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Longueur maximale des reglages d'une tache une fois la colonne en texte long.</summary>
    Friend Const LongueurMaxiLongue As Integer = 2000

    Private ReadOnly _faits As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>A appeler avant de lire ou d'ecrire la minuterie. Sans effet apres le premier passage par base.</summary>
    Friend Sub PreparerLaBase(chemin As String)
        Try
            If String.IsNullOrEmpty(chemin) OrElse Not File.Exists(chemin) Then Exit Sub
            PreparerLaColonneAction(chemin)
            PreparerLaColonneVolume(chemin)
            SyncLock _faits
                Dim dejaFait As Boolean
                If _faits.TryGetValue(chemin, dejaFait) Then
                    ReglagesLongsPossibles = dejaFait
                    Exit Sub
                End If
            End SyncLock

            Dim longs As Boolean = False
            Try
                If ColonneEnTexteLong(chemin) Then
                    longs = True
                Else
                    Dim copie As String = chemin & ".avant-argument-long"
                    If Not File.Exists(copie) Then File.Copy(chemin, copie)
                    Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                        cn.Open()
                        Using cmd As New OleDbCommand("ALTER TABLE MinuterieActive ALTER COLUMN [Argument] MEMO", cn)
                            cmd.ExecuteNonQuery()
                        End Using
                    End Using
                    longs = ColonneEnTexteLong(chemin)
                    DiagLog("Base de la minuterie : colonne Argument en texte long (" & If(longs, "ok", "echec") & "), sauvegarde : " & copie)
                End If
            Catch ex As Exception
                DiagLog("Base de la minuterie : colonne Argument laissee a 255 caracteres (" & ex.Message & ")")
            End Try
            SyncLock _faits
                _faits(chemin) = longs
            End SyncLock
            ReglagesLongsPossibles = longs
        Catch ex As Exception
            DiagLog("Base de la minuterie : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Colonne Volume (12/11/2026) : creee en NVARCHAR(3) (« 100 » au plus). Le choix « Default » (volume par defaut des reglages) a besoin de 7 caracteres : sans cela
    ' l'enregistrement de la minuterie echouait (« Le champ est trop petit... »). Elle est elargie a 10 caracteres, avec une copie de la base avant (une seule fois).
    ' ------------------------------------------------------------------------------------------
    Private ReadOnly _faitsVolume As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    Private Sub PreparerLaColonneVolume(chemin As String)
        Try
            SyncLock _faitsVolume
                If _faitsVolume.Contains(chemin) Then Exit Sub
            End SyncLock
            Try
                Dim assez As Boolean
                Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                    cn.Open()
                    Dim colonnes As DataTable = cn.GetSchema("Columns", New String() {Nothing, Nothing, "MinuterieActive", "Volume"})
                    Dim v As Object = If(colonnes.Rows.Count = 0, Nothing, colonnes.Rows(0)("CHARACTER_MAXIMUM_LENGTH"))
                    assez = colonnes.Rows.Count = 0 OrElse v Is Nothing OrElse v Is DBNull.Value OrElse Convert.ToInt64(v) = 0 OrElse Convert.ToInt64(v) >= 10
                End Using
                If Not assez Then
                    Dim copie As String = chemin & ".avant-volume-long"
                    If Not File.Exists(copie) Then File.Copy(chemin, copie)
                    Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                        cn.Open()
                        Using cmd As New OleDbCommand("ALTER TABLE MinuterieActive ALTER COLUMN [Volume] TEXT(10)", cn)
                            cmd.ExecuteNonQuery()
                        End Using
                    End Using
                    DiagLog("Base de la minuterie : colonne Volume elargie a 10 caracteres (choix « Default »), sauvegarde : " & copie)
                End If
                SyncLock _faitsVolume
                    _faitsVolume.Add(chemin)
                End SyncLock
            Catch ex As Exception
                DiagLog("Base de la minuterie : colonne Volume laissee a 3 caracteres (" & ex.Message & ")")
            End Try
        Catch ex As Exception
            DiagLog("Base de la minuterie (Volume) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Vrai si Argument est deja un texte long (longueur de colonne 0 pour Access).</summary>
    Private Function ColonneEnTexteLong(chemin As String, Optional colonne As String = "Argument") As Boolean
        Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
            cn.Open()
            Dim colonnes As DataTable = cn.GetSchema("Columns", New String() {Nothing, Nothing, "MinuterieActive", colonne})
            If colonnes.Rows.Count = 0 Then Return False
            Dim v As Object = colonnes.Rows(0)("CHARACTER_MAXIMUM_LENGTH")
            If v Is Nothing OrElse v Is DBNull.Value Then Return True
            Return Convert.ToInt64(v) = 0 OrElse Convert.ToInt64(v) > 255
        End Using
    End Function

End Module
