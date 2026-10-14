Option Strict On
Option Explicit On

''' <summary>
''' Les morceaux entendus depuis le lancement, toutes sources confondues (26/11/2026) : « Musique.Prev » rend celui entendu juste avant, meme si
''' l'alternance a change de source entre-temps (un MP3 fini, Deezer prend le relais : « precedent » doit ramener le MP3, pas un morceau de Deezer).
''' Cle d'un morceau : le chemin du fichier (mp3) ou « Artiste - Titre » (deezer). La radio n'est pas notee. Utilise depuis le fil de l'interface.
''' </summary>
Friend Module HistoriqueMusique

    Private Const Gardes As Integer = 200
    Private ReadOnly _entendus As New List(Of (Source As String, Cle As String))

    ''' <summary>Un morceau se fait entendre (sans effet si c'est deja le dernier note).</summary>
    Friend Sub Entendu(source As String, cle As String)
        If cle = "" OrElse EstLeDernier(source, cle) Then Exit Sub
        _entendus.Add((source, cle))
        If _entendus.Count > Gardes Then _entendus.RemoveAt(0)
    End Sub

    ''' <summary>Oublie le morceau en cours (s'il est deja note) et rend celui d'avant. Source = "" s'il n'y en a pas.</summary>
    Friend Function Reculer(sourceEnCours As String, cleEnCours As String) As (Source As String, Cle As String)
        If EstLeDernier(sourceEnCours, cleEnCours) Then
            If _entendus.Count = 1 Then Return ("", "")
            _entendus.RemoveAt(_entendus.Count - 1)
        End If
        If _entendus.Count = 0 Then Return ("", "")
        Return _entendus(_entendus.Count - 1)
    End Function

    Private Function EstLeDernier(source As String, cle As String) As Boolean
        If _entendus.Count = 0 Then Return False
        Dim dernier As (Source As String, Cle As String) = _entendus(_entendus.Count - 1)
        Return dernier.Source = source AndAlso dernier.Cle = cle
    End Function

End Module
