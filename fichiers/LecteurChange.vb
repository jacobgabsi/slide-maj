Imports System.IO

''' <summary>
''' Disques retrouves par leur NOM (et non par leur lettre) : les sources de photos/videos/musique
''' sont enregistrees « [NomDuDisque]\dossier@1111 » et la lettre actuelle est cherchee a chaque lecture.
''' Une source encore enregistree avec une lettre qui ne repond plus est cherchee une seule fois sous
''' les autres lettres, puis reenregistree avec le nom du disque.
''' </summary>
Module LecteurChange

    Private ReadOnly _appris As New Dictionary(Of Char, Char)

    Private Function Existe(chemin As String) As Boolean
        Try
            Dim t As System.Threading.Tasks.Task(Of Boolean) = System.Threading.Tasks.Task.Run(Function() File.Exists(chemin) OrElse Directory.Exists(chemin))
            Return t.Wait(3000) AndAlso t.Result
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Meme chemin sur une autre lettre de disque ; le chemin d'origine s'il n'y a rien a corriger ou rien trouve.</summary>
    Function Retrouver(chemin As String) As String
        If String.IsNullOrEmpty(chemin) OrElse chemin.Length < 3 OrElse chemin(1) <> ":"c Then Return chemin
        Dim ancienne As Char = Char.ToUpperInvariant(chemin(0))
        Dim suite As String = chemin.Substring(1)
        If _appris.ContainsKey(ancienne) Then
            Dim essai As String = _appris(ancienne) & suite
            If Existe(essai) Then Return essai
        End If
        If Existe(chemin) Then Return chemin
        For n As Integer = 67 To 90
            Dim lettre As Char = ChrW(n)
            If lettre = ancienne Then Continue For
            Dim essai As String = lettre & suite
            If Existe(essai) Then
                _appris(ancienne) = lettre
                DiagLog("Disque : " & ancienne & ": est devenu " & lettre & ": (" & chemin & ")")
                Return essai
            End If
        Next
        Return chemin
    End Function


    ' ------------------------------------------------------------------------------------------
    ' Sources enregistrees avec le NOM du disque a la place de sa lettre : « [MonDisque]\jacob\Photos@1111 ».
    ' Slide retrouve la lettre actuelle du disque a chaque lecture : un disque qui change de lettre
    ' (ou un reglage copie sur un autre PC) ne casse plus rien.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Lettre actuelle du disque de ce nom (« E: »), ou "" si absent ou ambigu.</summary>
    Private Function LettreDuDisque(nom As String) As String
        Try
            Dim t As System.Threading.Tasks.Task(Of String) = System.Threading.Tasks.Task.Run(Function()
                                                                                                  Dim trouves As New List(Of String)
                                                                                                  For Each d As DriveInfo In DriveInfo.GetDrives()
                                                                                                      If d.DriveType = DriveType.CDRom Then Continue For
                                                                                                      If d.IsReady AndAlso String.Equals(d.VolumeLabel, nom, StringComparison.OrdinalIgnoreCase) Then trouves.Add(d.Name.Substring(0, 2))
                                                                                                  Next
                                                                                                  Return If(trouves.Count >= 1, trouves(0), "")
                                                                                              End Function)
            Return If(t.Wait(4000), If(t.Result, ""), "")
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>« [Nom]\dossier » devient « E:\dossier » ; le reste est rendu tel quel.</summary>
    Function Resoudre(chemin As String) As String
        If String.IsNullOrEmpty(chemin) OrElse chemin(0) <> "["c Then Return chemin
        Dim fin As Integer = chemin.IndexOf("]"c)
        If fin < 2 Then Return chemin
        Dim lettre As String = LettreDuDisque(chemin.Substring(1, fin - 1))
        If lettre = "" Then Return chemin
        Return lettre & chemin.Substring(fin + 1)
    End Function

    ''' <summary>« E:\dossier » devient « [Nom]\dossier » si le disque est local, porte un nom et que ce nom est unique ; sinon inchange.</summary>
    Function Etiqueter(chemin As String) As String
        If String.IsNullOrEmpty(chemin) OrElse chemin.Length < 3 OrElse chemin(1) <> ":"c Then Return chemin
        Dim reseau As String = CheminReseau(chemin)
        If reseau <> "" Then Return reseau
        Try
            Dim t As System.Threading.Tasks.Task(Of String) = System.Threading.Tasks.Task.Run(Function()
                                                                                                  Dim d As New DriveInfo(chemin.Substring(0, 2))
                                                                                                  If Not d.IsReady OrElse d.DriveType = DriveType.Network OrElse d.DriveType = DriveType.CDRom Then Return ""
                                                                                                  Dim nom As String = d.VolumeLabel
                                                                                                  If nom = "" OrElse nom.Contains("]") Then Return ""
                                                                                                  Dim memes As Integer = 0
                                                                                                  For Each autre As DriveInfo In DriveInfo.GetDrives()
                                                                                                      If autre.IsReady AndAlso autre.DriveType <> DriveType.CDRom AndAlso String.Equals(autre.VolumeLabel, nom, StringComparison.OrdinalIgnoreCase) Then memes += 1
                                                                                                  Next
                                                                                                  Return If(memes = 1, nom, "")
                                                                                              End Function)
            If Not t.Wait(4000) OrElse String.IsNullOrEmpty(t.Result) Then Return chemin
            Return "[" & t.Result & "]" & chemin.Substring(2)
        Catch ex As Exception
            Return chemin
        End Try
    End Function

    ''' <summary>Les sources de photos/videos/musique avec la lettre actuelle des disques. Une source enregistree avec une lettre est
    ''' retrouvee (disque change de lettre) puis reenregistree avec le nom du disque.</summary>
    Function SourcesResolues() As List(Of String)
        Dim liste As New List(Of String)
        If My.Settings.CheminPhotoVideo Is Nothing Then Return liste
        Dim aReecrire As Boolean = False
        Dim neuves As New Specialized.StringCollection
        For Each brut As String In My.Settings.CheminPhotoVideo
            Dim opt As String = "", chemin As String = brut
            Dim p As Integer = brut.IndexOf("@"c)
            If p >= 0 Then chemin = brut.Substring(0, p) : opt = brut.Substring(p)
            Dim actuel As String = Resoudre(chemin)
            If actuel = chemin AndAlso chemin.Length >= 3 AndAlso chemin(1) = ":"c Then actuel = Retrouver(chemin)
            liste.Add(actuel & opt)
            Dim aEnregistrer As String = If(chemin.StartsWith("["), chemin, Etiqueter(actuel))
            If aEnregistrer <> chemin Then aReecrire = True
            neuves.Add(aEnregistrer & opt)
        Next
        If aReecrire Then
            Try
                My.Settings.CheminPhotoVideo = neuves
                My.Settings.Save()
                DiagLog("Sources : disques enregistres par leur nom")
            Catch ex As Exception
            End Try
        End If
        Return liste
    End Function

    ''' <summary>Le chemin sans son disque (« e:\jacob\x » ou « [Nom]\jacob\x » donnent « \jacob\x ») : pour comparer deux listes sans que la lettre ou le nom du disque compte.</summary>
    Function SansDisque(chemin As String) As String
        If String.IsNullOrEmpty(chemin) Then Return chemin
        If chemin(0) = "["c Then
            Dim fin As Integer = chemin.IndexOf("]"c)
            If fin > 0 Then Return chemin.Substring(fin + 1)
        ElseIf chemin.Length >= 2 AndAlso chemin(1) = ":"c Then
            Return chemin.Substring(2)
        End If
        Return chemin
    End Function

    <System.Runtime.InteropServices.DllImport("mpr.dll", CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
    Private Function WNetGetConnection(local As String, remote As System.Text.StringBuilder, ByRef taille As Integer) As Integer
    End Function

    ''' <summary>« X:\dossier » d'un lecteur reseau mappe devient « \serveur\partage\dossier » ; sinon "".</summary>
    Private Function CheminReseau(chemin As String) As String
        Try
            Dim t As System.Threading.Tasks.Task(Of String) = System.Threading.Tasks.Task.Run(Function()
                                                                                                  Dim sb As New System.Text.StringBuilder(1024)
                                                                                                  Dim n As Integer = sb.Capacity
                                                                                                  If WNetGetConnection(chemin.Substring(0, 2), sb, n) <> 0 Then Return ""
                                                                                                  Return sb.ToString().TrimEnd("\"c) & chemin.Substring(2)
                                                                                              End Function)
            Return If(t.Wait(4000), If(t.Result, ""), "")
        Catch ex As Exception
            Return ""
        End Try
    End Function
End Module
