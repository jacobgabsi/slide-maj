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

    ''' <summary>Longueur maximale des reglages d'une tache une fois la colonne en texte long.</summary>
    Friend Const LongueurMaxiLongue As Integer = 2000

    Private ReadOnly _faits As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>A appeler avant de lire ou d'ecrire la minuterie. Sans effet apres le premier passage par base.</summary>
    Friend Sub PreparerLaBase(chemin As String)
        Try
            If String.IsNullOrEmpty(chemin) OrElse Not File.Exists(chemin) Then Exit Sub
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

    ''' <summary>Vrai si Argument est deja un texte long (longueur de colonne 0 pour Access).</summary>
    Private Function ColonneEnTexteLong(chemin As String) As Boolean
        Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
            cn.Open()
            Dim colonnes As DataTable = cn.GetSchema("Columns", New String() {Nothing, Nothing, "MinuterieActive", "Argument"})
            If colonnes.Rows.Count = 0 Then Return False
            Dim v As Object = colonnes.Rows(0)("CHARACTER_MAXIMUM_LENGTH")
            If v Is Nothing OrElse v Is DBNull.Value Then Return True
            Return Convert.ToInt64(v) = 0 OrElse Convert.ToInt64(v) > 255
        End Using
    End Function

End Module
