Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text

''' <summary>
''' Titre, artiste et pochette d'un fichier de musique, pour la carte affichee sur l'image de
''' Slide (CarteDeezer) quand la musique vient des fichiers et non de Deezer (demande de Jacob,
''' 08/10/2026 : "fais le meme PIP affichage album pour les autres types de musique").
'''
''' - MP3 : lus dans l'etiquette du fichier (ID3v2 : titre TIT2, artiste TPE1, album TALB, image APIC).
''' - Pochette absente du fichier : une image du dossier (cover.jpg, folder.jpg, front.jpg,
'''   AlbumArt*.jpg, ou une image du meme nom que le morceau).
''' - Rien dans le fichier : le titre et l'artiste sont deduits du nom ("Artiste - Titre.mp3").
''' Rien n'est jamais ecrit ; une lecture qui echoue rend simplement "rien".
''' </summary>
Friend Module PochetteFichier

    Friend Class Etiquette
        Public Titre As String = ""
        Public Artiste As String = ""
        Public Album As String = ""
        ''' <summary>Octets de l'image de la pochette (JPEG, PNG...), ou Nothing.</summary>
        Public Image As Byte()
    End Class

    ''' <summary>Au-dela, on ne lit pas l'etiquette (elle contient surtout la pochette).</summary>
    Private Const TailleMaxi As Integer = 24 * 1024 * 1024

    ''' <summary>L'etiquette ID3v2 placee en tete d'un MP3 ; vide si le fichier n'en a pas.</summary>
    Friend Function LireEtiquette(fichier As String) As Etiquette
        Dim e As New Etiquette()
        Try
            Using flux As New FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
                Dim tete(9) As Byte
                If flux.Read(tete, 0, 10) < 10 Then Return e
                If tete(0) <> AscW("I"c) OrElse tete(1) <> AscW("D"c) OrElse tete(2) <> AscW("3"c) Then Return e
                Dim version As Integer = tete(3)
                If version < 2 OrElse version > 4 Then Return e
                Dim drapeaux As Integer = tete(5)
                If (drapeaux And &H80) <> 0 Then Return e           ' octets "desynchronises" (rare) : on ne s'y risque pas
                Dim taille As Integer = Securise(tete, 6)
                If taille <= 0 OrElse taille > TailleMaxi Then Return e
                Dim octets(taille - 1) As Byte
                Dim lus As Integer = 0
                Do While lus < taille
                    Dim n As Integer = flux.Read(octets, lus, taille - lus)
                    If n <= 0 Then Exit Do
                    lus += n
                Loop
                Dim p As Integer = 0
                ' En-tete etendu (rare) : on le saute.
                If (drapeaux And &H40) <> 0 AndAlso version >= 3 AndAlso lus >= 4 Then
                    p = If(version = 4, Securise(octets, 0), Entier(octets, 0, 4) + 4)
                End If
                Dim enTete As Integer = If(version = 2, 6, 10)
                Do While p + enTete <= lus
                    If octets(p) = 0 Then Exit Do                    ' remplissage : plus de trames
                    Dim nom As String, longueur As Integer
                    If version = 2 Then
                        nom = Encoding.ASCII.GetString(octets, p, 3)
                        longueur = Entier(octets, p + 3, 3)
                    Else
                        nom = Encoding.ASCII.GetString(octets, p, 4)
                        longueur = If(version = 4, Securise(octets, p + 4), Entier(octets, p + 4, 4))
                    End If
                    p += enTete
                    If longueur <= 0 OrElse p + longueur > lus Then Exit Do
                    Select Case nom
                        Case "TIT2", "TT2" : e.Titre = Texte(octets, p, longueur)
                        Case "TPE1", "TP1" : e.Artiste = Texte(octets, p, longueur)
                        Case "TALB", "TAL" : e.Album = Texte(octets, p, longueur)
                        Case "APIC", "PIC" : If e.Image Is Nothing Then e.Image = Image(octets, p, longueur, version = 2)
                    End Select
                    p += longueur
                Loop
            End Using
        Catch ex As Exception
            ' fichier illisible, etiquette abimee : comme s'il n'y en avait pas
        End Try
        Return e
    End Function

    ''' <summary>Entier "securise" d'ID3 : quatre octets de sept bits.</summary>
    Private Function Securise(o As Byte(), p As Integer) As Integer
        Return ((o(p) And &H7F) << 21) Or ((o(p + 1) And &H7F) << 14) Or ((o(p + 2) And &H7F) << 7) Or (o(p + 3) And &H7F)
    End Function

    Private Function Entier(o As Byte(), p As Integer, n As Integer) As Integer
        Dim v As Long = 0
        For i As Integer = 0 To n - 1 : v = (v << 8) Or o(p + i) : Next
        Return CInt(Math.Min(v, Integer.MaxValue))
    End Function

    ''' <summary>Texte d'une trame : un octet donne le codage, puis le texte.</summary>
    Private Function Texte(o As Byte(), p As Integer, n As Integer) As String
        If n < 2 Then Return ""
        Dim codage As Encoding = CodageDe(o(p))
        Dim t As String = If(o(p) = 1, AvecMarque(o, p + 1, n - 1), codage.GetString(o, p + 1, n - 1))
        Dim fin As Integer = t.IndexOf(ChrW(0))
        If fin >= 0 Then t = t.Substring(0, fin)
        Return t.Trim()
    End Function

    Private Function CodageDe(code As Byte) As Encoding
        Select Case code
            Case 1 : Return Encoding.Unicode
            Case 2 : Return Encoding.BigEndianUnicode
            Case 3 : Return Encoding.UTF8
            Case Else : Return Encoding.Latin1
        End Select
    End Function

    ''' <summary>UTF-16 avec sa marque d'ordre des octets (FF FE ou FE FF).</summary>
    Private Function AvecMarque(o As Byte(), p As Integer, n As Integer) As String
        If n >= 2 AndAlso o(p) = &HFE AndAlso o(p + 1) = &HFF Then Return Encoding.BigEndianUnicode.GetString(o, p + 2, n - 2)
        If n >= 2 AndAlso o(p) = &HFF AndAlso o(p + 1) = &HFE Then Return Encoding.Unicode.GetString(o, p + 2, n - 2)
        Return Encoding.Unicode.GetString(o, p, n)
    End Function

    ''' <summary>Les octets de l'image d'une trame APIC (ou PIC en version 2) : codage, type de
    ''' fichier, genre d'image, description, puis l'image elle-meme.</summary>
    Private Function Image(o As Byte(), p As Integer, n As Integer, ancienne As Boolean) As Byte()
        Dim fin As Integer = p + n
        Dim code As Byte = o(p)
        Dim q As Integer = p + 1
        If ancienne Then
            q += 3                                       ' "JPG", "PNG"
        Else
            Do While q < fin AndAlso o(q) <> 0 : q += 1 : Loop      ' "image/jpeg" termine par un zero
            q += 1
        End If
        q += 1                                           ' genre d'image (3 = face avant)
        ' Description : terminee par un zero, deux en UTF-16.
        If code = 1 OrElse code = 2 Then
            Do While q + 1 < fin AndAlso Not (o(q) = 0 AndAlso o(q + 1) = 0) : q += 2 : Loop
            q += 2
        Else
            Do While q < fin AndAlso o(q) <> 0 : q += 1 : Loop
            q += 1
        End If
        If q >= fin Then Return Nothing
        Dim octets As Byte() = New Byte(fin - q - 1) {}
        Array.Copy(o, q, octets, 0, octets.Length)
        Return octets
    End Function

    ''' <summary>La pochette d'un fichier de musique : celle de son etiquette, sinon une image de
    ''' son dossier ; Nothing s'il n'y en a pas. (A appeler hors du fil de l'interface.)</summary>
    Friend Function LirePochette(fichier As String) As Drawing.Image
        Try
            If String.IsNullOrEmpty(fichier) OrElse Not File.Exists(fichier) Then Return Nothing
            If Path.GetExtension(fichier).Equals(".mp3", StringComparison.OrdinalIgnoreCase) Then
                Dim octets As Byte() = LireEtiquette(fichier).Image
                Dim image As Drawing.Image = Decoder(octets)
                If image IsNot Nothing Then Return image
            End If
            Dim dossier As String = Path.GetDirectoryName(fichier)
            ' Une image du meme nom que le morceau lui appartient toujours. Les images "du dossier" (cover.jpg, folder.jpg,
            ' AlbumArt...) ne valent que pour un dossier qui est UN album : dans un dossier de centaines de morceaux de
            ' tous les artistes (20/10/2026 : "Georgy Porgy" de Toto avec la pochette d'un autre), elles sont celles d'un
            ' autre album et ne sont pas utilisees.
            For Each extension As String In {".jpg", ".jpeg", ".png"}
                Dim propre As String = Path.Combine(dossier, Path.GetFileNameWithoutExtension(fichier) & extension)
                If File.Exists(propre) Then
                    Dim image As Drawing.Image = Decoder(File.ReadAllBytes(propre))
                    If image IsNot Nothing Then Return image
                End If
            Next
            If Directory.EnumerateFiles(dossier).Count(Function(f) EstUnMorceau(f)) > MorceauxMaxParAlbum Then Return Nothing
            Dim candidats As New List(Of String)
            For Each nom As String In {"cover", "folder", "front", "album", "pochette"}
                For Each extension As String In {".jpg", ".jpeg", ".png"}
                    candidats.Add(Path.Combine(dossier, nom & extension))
                Next
            Next
            For Each candidat As String In candidats
                If File.Exists(candidat) Then
                    Dim image As Drawing.Image = Decoder(File.ReadAllBytes(candidat))
                    If image IsNot Nothing Then Return image
                End If
            Next
            ' Windows Media Player : AlbumArt_{identifiant}_Large.jpg, un par album ; sans choix possible entre plusieurs, aucune.
            Dim grandes As List(Of String) = Directory.EnumerateFiles(dossier, "AlbumArt_*_Large.jpg").Take(2).ToList()
            If grandes.Count = 1 Then
                Dim image As Drawing.Image = Decoder(File.ReadAllBytes(grandes(0)))
                If image IsNot Nothing Then Return image
            End If
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    ''' <summary>Au-dela, le dossier n'est plus un album : ses images ne disent rien de tel morceau.</summary>
    Private Const MorceauxMaxParAlbum As Integer = 40

    Private Function EstUnMorceau(chemin As String) As Boolean
        Dim e As String = Path.GetExtension(chemin).ToLowerInvariant()
        Return e = ".mp3" OrElse e = ".m4a" OrElse e = ".flac" OrElse e = ".wma" OrElse e = ".wav" OrElse e = ".ogg"
    End Function

    Private Function Decoder(octets As Byte()) As Drawing.Image
        If octets Is Nothing OrElse octets.Length < 100 Then Return Nothing
        Try
            Using flux As New MemoryStream(octets), lue As Drawing.Image = Drawing.Image.FromStream(flux)
                Return New Drawing.Bitmap(lue)              ' (copie : l'image lue depend du flux)
            End Using
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>Titre et artiste deduits du nom du fichier quand il n'a pas d'etiquette :
    ''' "Artiste - Titre (Official Video)[xx].mp3" donne ("Titre", "Artiste").</summary>
    Friend Sub DepuisLeNom(fichier As String, ByRef titre As String, ByRef artiste As String)
        Dim nom As String = ""
        Try
            nom = Path.GetFileNameWithoutExtension(fichier)
        Catch ex As Exception
        End Try
        nom = nom.Replace("_"c, " "c)
        ' Mentions entre crochets ou parentheses qui ne font pas partie du titre.
        nom = RegularExpressions.Regex.Replace(nom, "\[[^\]]*\]", " ")
        nom = RegularExpressions.Regex.Replace(nom, "\((?:[^)]*\b(?:official|officiel|audio|video|vidéo|clip|lyrics?|paroles|hd|hq)\b[^)]*)\)", " ", RegularExpressions.RegexOptions.IgnoreCase)
        nom = RegularExpressions.Regex.Replace(nom, "^\s*\d{1,3}\s*[-.]\s*", "")          ' numero de piste en tete
        nom = RegularExpressions.Regex.Replace(nom, "\s{2,}", " ").Trim(" "c, "-"c)
        Dim coupe As Integer = nom.IndexOf(" - ", StringComparison.Ordinal)
        If coupe < 0 Then coupe = nom.IndexOf("-", StringComparison.Ordinal)
        If coupe > 0 AndAlso coupe < nom.Length - 1 Then
            artiste = nom.Substring(0, coupe).Trim(" "c, "-"c)
            titre = nom.Substring(coupe + 1).Trim(" "c, "-"c)
        Else
            titre = nom
            artiste = ""
        End If
    End Sub

End Module
