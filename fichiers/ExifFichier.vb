Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Ecriture de la date de prise de vue dans le fichier d'une photo JPEG (27/10/2026, Jacob : « si il y a une erreur de date, que le nouvel exif remplace le mauvais dans le fichier des photos »).
''' La date EXIF est un texte de taille fixe ("2020:08:15 14:23:05" + zero final, 20 octets) : elle est remplacee sur place, octet pour octet. L'image n'est ni relue ni recompressee,
''' le reste du fichier n'est pas touche. Balises remplacees quand elles existent : DateTimeOriginal (prise de vue), DateTimeDigitized (numerisation), DateTime (IFD0, derniere modification).
''' Une photo sans ces balises (WhatsApp, capture...) ne peut pas etre changee ainsi : la fonction rend 0 et le fichier reste intact.
''' </summary>
Friend Module ExifFichier

    Private Const TagDateTime As Integer = &H132
    Private Const TagExifIfd As Integer = &H8769
    Private Const TagDateOriginal As Integer = &H9003
    Private Const TagDateNumerisee As Integer = &H9004

    ''' <summary>Vrai pour les fichiers dont la date EXIF peut etre ecrite sur place.</summary>
    Friend Function EstUnJpeg(fichier As String) As Boolean
        Dim ext As String = Path.GetExtension(fichier).ToLowerInvariant()
        Return ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".jpe"
    End Function

    ''' <summary>Remplace les dates EXIF du JPEG <paramref name="fichier"/> par <paramref name="date"/>. Rend le nombre de balises remplacees (0 : aucune balise de date, rien d'ecrit).
    ''' Lance une exception si le fichier ne peut pas etre ouvert en ecriture.</summary>
    Friend Function EcrireLaDate(fichier As String, [date] As DateTime) As Integer
        Dim texte As Byte() = Text.Encoding.ASCII.GetBytes([date].ToString("yyyy:MM:dd HH:mm:ss", Globalization.CultureInfo.InvariantCulture) & vbNullChar)         ' 20 octets
        Dim remplacees As Integer = 0
        Using flux As New FileStream(fichier, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
            If flux.ReadByte() <> &HFF OrElse flux.ReadByte() <> &HD8 Then Return 0          ' pas un JPEG
            ' Segments JPEG : cherche le premier APP1 « Exif ».
            Do While flux.Position < flux.Length - 4
                Dim ff As Integer = flux.ReadByte()
                If ff <> &HFF Then Return 0
                Dim marqueur As Integer = flux.ReadByte()
                Do While marqueur = &HFF          ' (octets de remplissage)
                    marqueur = flux.ReadByte()
                Loop
                If marqueur = &HDA OrElse marqueur = &HD9 OrElse marqueur < 0 Then Return 0          ' debut de l'image ou fin : pas d'EXIF
                Dim longueur As Integer = (flux.ReadByte() << 8) Or flux.ReadByte()
                If longueur < 2 Then Return 0
                Dim debutSegment As Long = flux.Position          ' juste apres la longueur
                If marqueur = &HE1 AndAlso longueur >= 16 Then
                    Dim segment(longueur - 3) As Byte
                    flux.Read(segment, 0, segment.Length)
                    If segment(0) = AscW("E"c) AndAlso segment(1) = AscW("x"c) AndAlso segment(2) = AscW("i"c) AndAlso segment(3) = AscW("f"c) AndAlso segment(4) = 0 AndAlso segment(5) = 0 Then
                        Dim modifies As New List(Of Integer)          ' decalages (dans le segment) des textes remplaces
                        remplacees = ChercherLesDates(segment, 6, texte, modifies)
                        For Each decalage As Integer In modifies
                            flux.Position = debutSegment + decalage
                            flux.Write(texte, 0, texte.Length)
                        Next
                        flux.Flush()
                        Return remplacees
                    End If
                    Continue Do          ' (autre APP1, XMP par exemple : deja lu)
                End If
                flux.Position = debutSegment + longueur - 2
            Loop
        End Using
        Return remplacees
    End Function

    ' --- lecture de la structure TIFF de l'EXIF ---

    Private Function Lire16(b As Byte(), p As Integer, grosBout As Boolean) As Integer
        If p < 0 OrElse p + 2 > b.Length Then Return -1
        Return If(grosBout, (CInt(b(p)) << 8) Or b(p + 1), (CInt(b(p + 1)) << 8) Or b(p))
    End Function

    Private Function Lire32(b As Byte(), p As Integer, grosBout As Boolean) As Long
        If p < 0 OrElse p + 4 > b.Length Then Return -1
        If grosBout Then Return (CLng(b(p)) << 24) Or (CLng(b(p + 1)) << 16) Or (CLng(b(p + 2)) << 8) Or b(p + 3)
        Return (CLng(b(p + 3)) << 24) Or (CLng(b(p + 2)) << 16) Or (CLng(b(p + 1)) << 8) Or b(p)
    End Function

    ''' <summary>Parcourt l'IFD0 puis le sous-IFD EXIF ; les 20 octets de chaque balise de date sont ajoutes a <paramref name="modifies"/>. Rend leur nombre.</summary>
    Private Function ChercherLesDates(segment As Byte(), debutTiff As Integer, texte As Byte(), modifies As List(Of Integer)) As Integer
        If debutTiff + 8 > segment.Length Then Return 0
        Dim grosBout As Boolean
        If segment(debutTiff) = AscW("M"c) AndAlso segment(debutTiff + 1) = AscW("M"c) Then
            grosBout = True
        ElseIf segment(debutTiff) = AscW("I"c) AndAlso segment(debutTiff + 1) = AscW("I"c) Then
            grosBout = False
        Else
            Return 0
        End If
        Dim ifd0 As Long = Lire32(segment, debutTiff + 4, grosBout)
        Dim exifIfd As Long = ParcourirIfd(segment, debutTiff, ifd0, grosBout, modifies, False)
        If exifIfd > 0 Then ParcourirIfd(segment, debutTiff, exifIfd, grosBout, modifies, True)
        Return modifies.Count
    End Function

    ''' <summary>Un IFD : repere les balises de date (texte de 20 octets, ailleurs que dans l'entree) et, dans l'IFD0, l'adresse du sous-IFD EXIF (rendue, 0 sinon).</summary>
    Private Function ParcourirIfd(segment As Byte(), debutTiff As Integer, ifd As Long, grosBout As Boolean, modifies As List(Of Integer), dansExif As Boolean) As Long
        Dim p As Long = debutTiff + ifd
        If ifd < 8 OrElse p + 2 > segment.Length Then Return 0
        Dim nombre As Integer = Lire16(segment, CInt(p), grosBout)
        Dim adresseExif As Long = 0
        For i As Integer = 0 To nombre - 1
            Dim e As Integer = CInt(p) + 2 + i * 12
            If e + 12 > segment.Length Then Exit For
            Dim tag As Integer = Lire16(segment, e, grosBout)
            Dim genre As Integer = Lire16(segment, e + 2, grosBout)
            Dim compte As Long = Lire32(segment, e + 4, grosBout)
            Dim valeur As Long = Lire32(segment, e + 8, grosBout)
            If tag = TagExifIfd AndAlso Not dansExif Then adresseExif = valeur
            Dim estUneDate As Boolean = (tag = TagDateTime AndAlso Not dansExif) OrElse (dansExif AndAlso (tag = TagDateOriginal OrElse tag = TagDateNumerisee))
            If estUneDate AndAlso genre = 2 AndAlso compte = 20 Then
                Dim place As Long = debutTiff + valeur
                If place >= 0 AndAlso place + 20 <= segment.Length Then modifies.Add(CInt(place))
            End If
        Next
        Return adresseExif
    End Function

End Module
