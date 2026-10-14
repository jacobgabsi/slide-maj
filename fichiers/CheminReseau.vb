Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Slide lance par le chemin reseau (\\serveur\partage\...) au lieu du lecteur (V:\...), 27/11/2026 : « au demarrage de Slide j'ai eu ca ».
''' Windows garde les reglages de Slide selon le CHEMIN du programme : sous le chemin reseau il n'y avait aucune source, la reprise echouait (« 'Reprendre le Slide' n'a pas pu
''' etre active »), la position etait perdue et les options s'ouvraient sans sources. Quand un lecteur mene au meme programme, Slide se relance par ce lecteur (voir ApplicationEvents).
''' </summary>
Friend Module CheminReseau

    ''' <summary>Passe au Slide relance quand c'est une personne qui avait lance le premier : il reste un lancement manuel (lignes Tuya du BOOT).</summary>
    Friend Const ArgumentLancementManuel As String = "/lancement-manuel"

    <DllImport("mpr.dll", CharSet:=CharSet.Unicode, EntryPoint:="WNetGetConnectionW")>
    Private Function WNetGetConnection(lecteur As String, partage As StringBuilder, ByRef longueur As Integer) As Integer
    End Function

    ''' <summary>Le meme fichier atteint par une lettre de lecteur ; "" si le chemin n'est pas un chemin reseau ou si aucun lecteur n'y mene.</summary>
    Friend Function ParUnLecteur(chemin As String) As String
        Try
            If chemin Is Nothing OrElse Not chemin.StartsWith("\\", StringComparison.Ordinal) Then Return ""
            For Each lecteur As IO.DriveInfo In IO.DriveInfo.GetDrives()
                If lecteur.DriveType <> IO.DriveType.Network Then Continue For
                Dim lettre As String = lecteur.Name.Substring(0, 2)
                Dim partage As New StringBuilder(520)
                Dim longueur As Integer = partage.Capacity
                If WNetGetConnection(lettre, partage, longueur) <> 0 Then Continue For
                Dim racine As String = partage.ToString().TrimEnd("\"c)
                If racine = "" OrElse Not chemin.StartsWith(racine & "\", StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim parLecteur As String = lettre & chemin.Substring(racine.Length)
                If IO.File.Exists(parLecteur) Then Return parLecteur
            Next
        Catch ex As Exception
        End Try
        Return ""
    End Function

End Module
