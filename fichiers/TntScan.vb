Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading

''' <summary>
''' Une chaine de la liste TNT d'une ville (fichier data\tnt-NOM.m3u).
''' </summary>
Friend NotInheritable Class ChaineTnt
    Public Nom As String = ""
    ''' <summary>Frequence du multiplex, en Hz.</summary>
    Public Frequence As Long
    ''' <summary>Numero de programme (service_id) dans le multiplex.</summary>
    Public Programme As Integer
    ''' <summary>Numero officiel du pays (LCN) : 0 si inconnu.</summary>
    Public Lcn As Integer
    ''' <summary>Emplacement dans la liste de Slide (1 a 50) : le numero de « Chaine NN » dans la minuterie. 0 : pas encore place.</summary>
    Public Numero As Integer
    ''' <summary>(transport_stream_id &lt;&lt; 16) + service_id, pour retrouver le numero officiel dans la table du reseau.</summary>
    Public Cle As Long
End Class

''' <summary>
''' Lecture d'un flux de transport MPEG (TS) enregistre depuis un multiplex de la TNT : tables PAT (programmes), SDT (noms des chaines) et NIT
''' (reseau : numeros officiels « LCN »). Ne depend d'aucune bibliotheque : un paquet TS fait 188 octets, les tables sont decoupees en sections.
''' </summary>
Friend NotInheritable Class AnalyseTs

    Friend NotInheritable Class ServiceTs
        Public Id As Integer
        Public Nom As String = ""
        Public Genre As Integer
        Public Crypte As Boolean
    End Class

    Public ReadOnly Services As New Dictionary(Of Integer, ServiceTs)
    Public ReadOnly ProgrammesPat As New HashSet(Of Integer)
    Public ReadOnly Lcn As New Dictionary(Of Long, Integer)
    Public TsId As Integer = -1
    ''' <summary>Nom du reseau (NIT, descripteur &H40), s'il est diffuse.</summary>
    Public NomReseau As String = ""
    Public PatVu As Boolean
    Public SdtVu As Boolean
    Public NitVu As Boolean
    Public Paquets As Long

    Private ReadOnly _tampons As New Dictionary(Of Integer, List(Of Byte))

    ''' <summary>Vrai quand la liste des chaines du multiplex est complete (PAT + SDT recus et coherents).</summary>
    Public Function ListeComplete() As Boolean
        If Not PatVu OrElse Not SdtVu OrElse ProgrammesPat.Count = 0 Then Return False
        For Each p As Integer In ProgrammesPat
            If Not Services.ContainsKey(p) Then Return False
        Next
        Return True
    End Function

    ''' <summary>Lit le fichier a partir de <paramref name="depart"/> (octets deja traites) ; rend la nouvelle position (un nombre entier de paquets).</summary>
    Public Function Lire(chemin As String, depart As Long) As Long
        Using fs As New FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            If fs.Length <= depart Then Return depart
            fs.Position = depart
            Dim n As Integer = CInt(Math.Min(fs.Length - depart, 32L * 1024 * 1024))
            Dim buf(n - 1) As Byte
            Dim lus As Integer = fs.Read(buf, 0, n)
            Dim i As Integer = 0
            Do While i + 188 <= lus
                If buf(i) <> &H47 Then
                    i += 1
                    Continue Do
                End If
                Paquet(buf, i)
                i += 188
            Loop
            Return depart + i
        End Using
    End Function

    Private Sub Paquet(b As Byte(), o As Integer)
        Dim pid As Integer = ((b(o + 1) And &H1F) << 8) Or b(o + 2)
        If pid <> 0 AndAlso pid <> &H10 AndAlso pid <> &H11 Then Exit Sub
        Paquets += 1
        Dim afc As Integer = (b(o + 3) >> 4) And 3
        If (afc And 1) = 0 Then Exit Sub                       ' pas de donnees
        Dim debut As Integer = o + 4
        If (afc And 2) <> 0 Then debut += 1 + b(o + 4)         ' champ d'adaptation
        Dim fin As Integer = o + 188
        If debut >= fin Then Exit Sub
        Dim pusi As Boolean = (b(o + 1) And &H40) <> 0
        Dim tampon As List(Of Byte) = Nothing
        _tampons.TryGetValue(pid, tampon)
        If pusi Then
            Dim pointeur As Integer = b(debut)
            debut += 1
            If tampon IsNot Nothing AndAlso pointeur > 0 Then
                For k As Integer = debut To Math.Min(debut + pointeur, fin) - 1
                    tampon.Add(b(k))
                Next
                Extraire(pid, tampon)
            End If
            tampon = New List(Of Byte)
            _tampons(pid) = tampon
            debut += pointeur
            For k As Integer = debut To fin - 1
                tampon.Add(b(k))
            Next
        Else
            If tampon Is Nothing Then Exit Sub
            For k As Integer = debut To fin - 1
                tampon.Add(b(k))
            Next
        End If
        Extraire(pid, tampon)
    End Sub

    Private Sub Extraire(pid As Integer, tampon As List(Of Byte))
        Do While tampon.Count >= 3
            If tampon(0) = &HFF Then
                tampon.Clear()
                Exit Do
            End If
            Dim lg As Integer = ((tampon(1) And &HF) << 8) Or tampon(2)
            If tampon.Count < 3 + lg Then Exit Do
            Dim section As Byte() = tampon.GetRange(0, 3 + lg).ToArray()
            tampon.RemoveRange(0, 3 + lg)
            Try
                Traiter(pid, section)
            Catch ex As Exception
                ' section mal formee (signal faible) : ignoree
            End Try
        Loop
    End Sub

    Private Sub Traiter(pid As Integer, s As Byte())
        Dim tid As Integer = s(0)
        If pid = 0 AndAlso tid = &H0 Then
            TsId = (CInt(s(3)) << 8) Or s(4)
            Dim p As Integer = 8
            Do While p + 4 <= s.Length - 4
                Dim prog As Integer = (CInt(s(p)) << 8) Or s(p + 1)
                If prog <> 0 Then ProgrammesPat.Add(prog)
                p += 4
            Loop
            PatVu = True
        ElseIf pid = &H11 AndAlso tid = &H42 Then
            Dim p As Integer = 11
            Dim limite As Integer = s.Length - 4
            Do While p + 5 <= limite
                Dim sid As Integer = (CInt(s(p)) << 8) Or s(p + 1)
                Dim crypte As Boolean = ((s(p + 3) >> 4) And 1) = 1
                Dim dl As Integer = ((s(p + 3) And &HF) << 8) Or s(p + 4)
                p += 5
                Dim finDesc As Integer = Math.Min(p + dl, limite)
                Dim svc As New ServiceTs With {.Id = sid, .Crypte = crypte}
                Do While p + 2 <= finDesc
                    Dim tag As Integer = s(p)
                    Dim len As Integer = s(p + 1)
                    If tag = &H48 AndAlso p + 2 + len <= finDesc AndAlso len >= 3 Then
                        svc.Genre = s(p + 2)
                        Dim pl As Integer = s(p + 3)
                        If p + 4 + pl < finDesc Then
                            Dim nl As Integer = s(p + 4 + pl)
                            If p + 5 + pl + nl <= finDesc Then svc.Nom = DecoderTexte(s, p + 5 + pl, nl)
                        End If
                    End If
                    p += 2 + len
                Loop
                p = finDesc
                Services(sid) = svc
            Loop
            SdtVu = True
        ElseIf pid = &H10 AndAlso (tid = &H40 OrElse tid = &H41) Then
            Dim p As Integer = 8
            Dim ndl As Integer = ((s(p) And &HF) << 8) Or s(p + 1)
            Dim q0 As Integer = p + 2
            Dim finNd As Integer = Math.Min(q0 + ndl, s.Length - 4)
            Do While q0 + 2 <= finNd
                Dim tagR As Integer = s(q0)
                Dim lenR As Integer = s(q0 + 1)
                If tagR = &H40 AndAlso q0 + 2 + lenR <= finNd AndAlso NomReseau = "" Then NomReseau = DecoderTexte(s, q0 + 2, lenR)     ' nom du reseau
                q0 += 2 + lenR
            Loop
            p += 2 + ndl
            Dim tll As Integer = ((s(p) And &HF) << 8) Or s(p + 1)
            p += 2
            Dim fin As Integer = Math.Min(p + tll, s.Length - 4)
            Do While p + 6 <= fin
                Dim tsid As Integer = (CInt(s(p)) << 8) Or s(p + 1)
                Dim dl As Integer = ((s(p + 4) And &HF) << 8) Or s(p + 5)
                p += 6
                Dim fd As Integer = Math.Min(p + dl, fin)
                Do While p + 2 <= fd
                    Dim tag As Integer = s(p)
                    Dim len As Integer = s(p + 1)
                    ' Seul le descripteur &H83 (numeros de chaines : service, 6 bits reserves + 10 bits de numero) est lu. Le &H87 (liste de chaines « v2 ») a une autre structure (identifiant, nom de liste, pays) :
                    ' le lire comme le &H83 donnait des numeros faux (23/11/2026 : LCI au numero 11, celui de TFX).
                    If tag = &H83 AndAlso p + 2 + len <= fd Then
                        Dim q As Integer = p + 2
                        Do While q + 4 <= p + 2 + len
                            Dim sid As Integer = (CInt(s(q)) << 8) Or s(q + 1)
                            Dim numero As Integer = ((s(q + 2) And &H3) << 8) Or s(q + 3)
                            Dim cle As Long = (CLng(tsid) << 16) Or CLng(sid)
                            Lcn(cle) = numero
                            q += 4
                        Loop
                    End If
                    p += 2 + len
                Loop
                p = fd
            Loop
            NitVu = True
        End If
    End Sub

    ''' <summary>Texte DVB (norme ETSI EN 300 468, annexe A) : sans octet de jeu de caracteres, c'est de l'ISO 6937 (les accents sont des octets a part, avant la lettre).</summary>
    Friend Shared Function DecoderTexte(b As Byte(), debut As Integer, longueur As Integer) As String
        If longueur <= 0 Then Return ""
        Dim i As Integer = debut
        Dim fin As Integer = debut + longueur
        Dim utf8 As Boolean = False
        Dim iso6937 As Boolean = True
        Dim premier As Integer = b(i)
        If premier < &H20 Then
            iso6937 = False
            If premier = &H15 Then
                utf8 = True
                i += 1
            ElseIf premier = &H10 Then
                i += 3
            ElseIf premier >= 1 AndAlso premier <= &HB Then
                i += 1
            Else
                i += 1
            End If
        End If
        If i >= fin Then Return ""
        If utf8 Then Return CleanerTexte(Encoding.UTF8.GetString(b, i, fin - i))
        Dim sb As New StringBuilder()
        While i < fin
            Dim c As Integer = b(i)
            i += 1
            If c >= &H80 AndAlso c <= &H9F Then Continue While   ' codes de controle (&H86, &H87 : debut/fin d'emphase, &H8A : saut de ligne)
            If iso6937 AndAlso c >= &HC1 AndAlso c <= &HCF AndAlso i < fin Then
                Dim base As Char = ChrW(b(i))
                i += 1
                Dim marque As Char = Chr(0)
                Select Case c
                    Case &HC1 : marque = ChrW(&H300)
                    Case &HC2 : marque = ChrW(&H301)
                    Case &HC3 : marque = ChrW(&H302)
                    Case &HC4 : marque = ChrW(&H303)
                    Case &HC5 : marque = ChrW(&H304)
                    Case &HC6 : marque = ChrW(&H306)
                    Case &HC7 : marque = ChrW(&H307)
                    Case &HC8 : marque = ChrW(&H308)
                    Case &HCA : marque = ChrW(&H30A)
                    Case &HCB : marque = ChrW(&H327)
                    Case &HCD : marque = ChrW(&H30B)
                    Case &HCE : marque = ChrW(&H328)
                    Case &HCF : marque = ChrW(&H30C)
                End Select
                sb.Append(base)
                If marque <> Chr(0) Then sb.Append(marque)
            Else
                sb.Append(ChrW(c))
            End If
        End While
        Return CleanerTexte(sb.ToString())
    End Function

    Private Shared Function CleanerTexte(texte As String) As String
        Try
            texte = texte.Normalize(NormalizationForm.FormC)
        Catch ex As Exception
        End Try
        Dim sb As New StringBuilder()
        For Each c As Char In texte
            If Not Char.IsControl(c) Then sb.Append(c)
        Next
        Return sb.ToString().Trim()
    End Function

End Class

''' <summary>
''' Scan de la TNT (clic droit > « Scanner la TNT… », 23/11/2026) et listes de chaines par ville (data\tnt-NOM.m3u).
'''
''' Principe : pour chaque canal UHF de la TNT francaise (21 a 48), un vlc.exe (celui de data\VLC) ouvre « dvbt:// » sur la frequence et ecrit tel quel le
''' flux recu dans un fichier temporaire ; on lit ce flux nous-memes (AnalyseTs) : noms des chaines (SDT), programmes (PAT) et numeros officiels du pays (NIT).
''' Aucun signal sur un canal : il est abandonne au bout de quelques secondes.
''' </summary>
Friend Module TntScan

    ''' <summary>Slide ne sait garder que 50 chaines (ChannelID).</summary>
    Friend Const NombreMax As Integer = 50

    ' Vrai pendant le scan : aucune chaine TV ne s'ouvre (la cle TNT n'a qu'un tuner ; voir PlayTV_VLC).
    Friend ScanEnCours As Boolean

    Private Const PremierCanal As Integer = 21
    Private Const DernierCanal As Integer = 48

    ''' <summary>Dossier des listes (data\).</summary>
    Friend Function DossierDesListes() As String
        Return Path.Combine(AppPath, "Data")
    End Function

    Friend Function FichierDeLaListe(nom As String) As String
        Return Path.Combine(DossierDesListes(), "tnt-" & nom & ".m3u")
    End Function

    ''' <summary>Vrai si <paramref name="nom"/> peut servir de nom de fichier (ni vide, ni caractere interdit).</summary>
    Friend Function NomValide(nom As String) As Boolean
        If String.IsNullOrWhiteSpace(nom) Then Return False
        Return nom.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
    End Function

    ''' <summary>La ville de Chabbat (celle des horaires, table <c>City</c>) qui correspond a <paramref name="nom"/> dans la liste des villes du cadre Chabbat (Form4), qui propose aussi les listes TNT enregistrees
    ''' (23/11/2026) : une ville est elle-meme ; « Paris II », « Paris III »… ou « Paris Est » donnent « Paris » ; sinon la ville de Chabbat actuelle (les horaires ne changent pas).</summary>
    Friend Function VilleDeChabbatDe(nom As String) As String
        nom = If(nom, "").Trim()
        If nom = "" Then Return If(My.Settings.Shabbat_Ville, "")
        For u As Integer = 1 To 50
            If String.Equals(City(u, 0), nom, StringComparison.OrdinalIgnoreCase) Then Return City(u, 0)
        Next
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(nom, "^(.*?)\s+(?:[IVXLC]+|\d+)$")
        If m.Success Then
            For u As Integer = 1 To 50
                If String.Equals(City(u, 0), m.Groups(1).Value.Trim(), StringComparison.OrdinalIgnoreCase) Then Return City(u, 0)
            Next
        End If
        For u As Integer = 1 To 50
            If City(u, 0) <> "" AndAlso nom.StartsWith(City(u, 0) & " ", StringComparison.OrdinalIgnoreCase) Then Return City(u, 0)
        Next
        Return If(My.Settings.Shabbat_Ville, "")
    End Function

    ''' <summary>Les listes existantes (« Paris », « Cannes »... d'apres les fichiers tnt-*.m3u).</summary>
    Friend Function ListesExistantes() As List(Of String)
        Dim l As New List(Of String)
        Try
            For Each f As String In Directory.GetFiles(DossierDesListes(), "tnt-*.m3u")
                Dim n As String = Path.GetFileNameWithoutExtension(f)
                n = n.Substring(4)
                If n <> "" AndAlso Not l.Contains(n, StringComparer.OrdinalIgnoreCase) Then l.Add(n)
            Next
        Catch ex As Exception
        End Try
        l.Sort(StringComparer.CurrentCultureIgnoreCase)
        Return l
    End Function

    Private Function FichierListeUtilisee() As String
        Return Path.Combine(DossierReglages, "TntListe.txt")
    End Function

    ''' <summary>La liste que la TV de Slide utilise sur ce poste : celle choisie dans la fenetre du scan, sinon celle de la ville de Chabbat (comme avant).</summary>
    Friend Function ListeUtilisee() As String
        Try
            Dim f As String = FichierListeUtilisee()
            If File.Exists(f) Then
                Dim n As String = File.ReadAllText(f, Encoding.UTF8).Trim()
                If n <> "" AndAlso File.Exists(FichierDeLaListe(n)) Then Return n
            End If
        Catch ex As Exception
        End Try
        Return My.Settings.Shabbat_Ville
    End Function

    ''' <summary>Fait de <paramref name="nom"/> la liste de la TV ("" : retour a la ville de Chabbat).</summary>
    Friend Sub ChoisirLaListe(nom As String)
        Try
            Dim f As String = FichierListeUtilisee()
            If String.IsNullOrWhiteSpace(nom) Then
                If File.Exists(f) Then File.Delete(f)
            Else
                Directory.CreateDirectory(Path.GetDirectoryName(f))
                File.WriteAllText(f, nom.Trim(), New UTF8Encoding(False))
            End If
        Catch ex As Exception
            DiagLog("TNT : liste de la TV non enregistree (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Relit le fichier d'une liste (format de Slide ou sortie de VLC). Liste vide si le fichier n'existe pas.</summary>
    Friend Function Lire(nom As String) As List(Of ChaineTnt)
        Dim liste As New List(Of ChaineTnt)
        Dim fichier As String = FichierDeLaListe(nom)
        If Not File.Exists(fichier) Then Return liste
        Dim cour As ChaineTnt = Nothing
        Using r As New StreamReader(fichier)
            Do While Not r.EndOfStream
                Dim ligne As String = If(r.ReadLine(), "").Trim()
                If ligne.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase) Then
                    cour = New ChaineTnt()
                    Dim v As Integer = ligne.IndexOf(","c)
                    If v >= 0 Then cour.Nom = ligne.Substring(v + 1).Trim()
                    liste.Add(cour)
                ElseIf cour IsNot Nothing Then
                    If ligne.StartsWith("#EXTVLCOPT:dvb-frequency=", StringComparison.OrdinalIgnoreCase) Then
                        Dim f As Long
                        Long.TryParse(ligne.Substring(25).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, f)
                        cour.Frequence = f
                    ElseIf ligne.StartsWith("#EXTVLCOPT:program=", StringComparison.OrdinalIgnoreCase) Then
                        Integer.TryParse(ligne.Substring(19).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, cour.Programme)
                    ElseIf ligne.StartsWith("#SLIDE:SLOT=", StringComparison.OrdinalIgnoreCase) Then
                        Integer.TryParse(ligne.Substring(12).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, cour.Numero)
                    ElseIf ligne.StartsWith("#SLIDE:LCN=", StringComparison.OrdinalIgnoreCase) Then
                        Integer.TryParse(ligne.Substring(11).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, cour.Lcn)
                    End If
                End If
            Loop
        End Using
        ' Fichiers sans emplacement (anciens fichiers) : l'ordre du fichier fait le numero, comme Slide le lit.
        Dim pris As New HashSet(Of Integer)
        For Each c As ChaineTnt In liste
            If c.Numero >= 1 AndAlso c.Numero <= NombreMax AndAlso Not pris.Add(c.Numero) Then c.Numero = 0
            If c.Numero < 1 OrElse c.Numero > NombreMax Then c.Numero = 0
        Next
        Dim libre As Integer = 1
        For Each c As ChaineTnt In liste
            If c.Numero <> 0 Then Continue For
            Do While pris.Contains(libre)
                libre += 1
            Loop
            If libre <= NombreMax Then
                c.Numero = libre
                pris.Add(libre)
            End If
        Next
        liste.RemoveAll(Function(c) c.Numero = 0)
        liste.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))
        Return liste
    End Function

    ''' <summary>Ecrit la liste (une copie de l'ancien fichier est gardee dans tnt-NOM.m3u.sauvegarde).</summary>
    Friend Sub Ecrire(nom As String, chaines As List(Of ChaineTnt))
        Dim fichier As String = FichierDeLaListe(nom)
        Directory.CreateDirectory(Path.GetDirectoryName(fichier))
        If File.Exists(fichier) Then
            Try
                File.Copy(fichier, fichier & ".sauvegarde", True)
            Catch ex As Exception
            End Try
        End If
        Dim sb As New StringBuilder()
        sb.AppendLine("#EXTM3U")
        For Each c As ChaineTnt In chaines.OrderBy(Function(x) x.Numero)
            sb.AppendLine("#EXTINF:0," & c.Nom)
            sb.AppendLine("#SLIDE:SLOT=" & c.Numero.ToString(CultureInfo.InvariantCulture))
            If c.Lcn > 0 Then sb.AppendLine("#SLIDE:LCN=" & c.Lcn.ToString(CultureInfo.InvariantCulture))
            sb.AppendLine("#EXTVLCOPT:dvb-adapter=0")
            sb.AppendLine("#EXTVLCOPT:dvb-frequency=" & c.Frequence.ToString(CultureInfo.InvariantCulture))
            sb.AppendLine("#EXTVLCOPT:dvb-bandwidth=8")
            sb.AppendLine("#EXTVLCOPT:program=" & c.Programme.ToString(CultureInfo.InvariantCulture))
            sb.AppendLine("dvb-t://")
        Next
        File.WriteAllText(fichier, sb.ToString(), New UTF8Encoding(True))
        DiagLog("TNT : liste « " & nom & " » enregistree (" & chaines.Count & " chaine(s))")
    End Sub

    ''' <summary>Renomme une liste (le fichier). Faux si le nouveau nom existe deja.</summary>
    Friend Function Renommer(ancien As String, nouveau As String) As Boolean
        Dim a As String = FichierDeLaListe(ancien), n As String = FichierDeLaListe(nouveau)
        If Not File.Exists(a) OrElse File.Exists(n) Then Return False
        File.Move(a, n)
        If String.Equals(ListeUtilisee(), ancien, StringComparison.OrdinalIgnoreCase) Then ChoisirLaListe(nouveau)
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Numeros du pays
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Numeros officiels francais (LCN) par nom de chaine, quand le reseau ne les donne pas (nom sans accents ni espaces, en majuscules).</summary>
    Private ReadOnly NumerosFrance As New Dictionary(Of String, Integer) From {
        {"TF1", 1}, {"FRANCE2", 2}, {"FRANCE3", 3}, {"CANAL", 4}, {"FRANCE5", 5}, {"M6", 6}, {"ARTE", 7}, {"C8", 8}, {"W9", 9}, {"TMC", 10},
        {"TFX", 11}, {"NRJ12", 12}, {"LCP", 13}, {"PUBLICSENAT", 13}, {"LCPPUBLICSENAT", 13}, {"FRANCE4", 14}, {"BFMTV", 15}, {"CNEWS", 16},
        {"CSTAR", 17}, {"GULLI", 18}, {"TF1SERIESFILMS", 19}, {"LEQUIPE", 20}, {"6TER", 21}, {"RMCSTORY", 22}, {"RMCDECOUVERTE", 23},
        {"CHERIE25", 24}, {"LCI", 26}, {"FRANCEINFO", 27}}

    Private Function NomNormalise(nom As String) As String
        Dim sb As New StringBuilder()
        For Each c As Char In nom.Normalize(NormalizationForm.FormD)
            If CharUnicodeInfo.GetUnicodeCategory(c) = UnicodeCategory.NonSpacingMark Then Continue For
            If Char.IsLetterOrDigit(c) Then sb.Append(Char.ToUpperInvariant(c))
        Next
        Dim n As String = sb.ToString()
        If n.Length > 3 AndAlso n.EndsWith("HD", StringComparison.Ordinal) Then n = n.Substring(0, n.Length - 2)
        Return n
    End Function

    ''' <summary>Numero officiel d'apres le nom (table francaise) ; 0 si inconnu.</summary>
    Friend Function NumeroOfficielParNom(nom As String) As Integer
        Dim v As Integer
        If NumerosFrance.TryGetValue(NomNormalise(nom), v) Then Return v
        Return 0
    End Function

    ''' <summary>Place chaque chaine a son numero officiel (LCN) quand il est entre 1 et 50 et libre ; les autres prennent les premiers emplacements libres, dans l'ordre des numeros puis des frequences.
    ''' Les chaines qui ne tiennent plus dans les 50 emplacements sont retirees de la liste ; rend leur nombre.</summary>
    Friend Function NumeroterSelonLePays(l As List(Of ChaineTnt)) As Integer
        Dim pris As New HashSet(Of Integer)
        For Each c As ChaineTnt In l
            c.Numero = 0
        Next
        For Each c As ChaineTnt In l.Where(Function(x) x.Lcn >= 1 AndAlso x.Lcn <= NombreMax).OrderBy(Function(x) x.Lcn)
            If pris.Add(c.Lcn) Then c.Numero = c.Lcn
        Next
        Dim libre As Integer = 1
        For Each c As ChaineTnt In l.Where(Function(x) x.Numero = 0).OrderBy(Function(x) If(x.Lcn > 0, x.Lcn, 9999)).ThenBy(Function(x) x.Frequence).ThenBy(Function(x) x.Programme).ToList()
            Do While pris.Contains(libre)
                libre += 1
            Loop
            If libre <= NombreMax Then
                c.Numero = libre
                pris.Add(libre)
            End If
        Next
        Dim hors As Integer = l.Where(Function(x) x.Numero = 0).Count()
        l.RemoveAll(Function(x) x.Numero = 0)
        l.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))
        Return hors
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Scan
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Resultat d'un scan : les chaines, d'ou viennent les numeros officiels, nombre de canaux avec signal.</summary>
    Friend NotInheritable Class ResultatDuScan
        Public ReadOnly Chaines As New List(Of ChaineTnt)
        Public CanauxAvecSignal As Integer
        Public NumerosDuReseau As Integer
        Public NumerosParNom As Integer
        Public Cryptees As Integer
        Public NomReseau As String = ""
        ''' <summary>Vrai si les chaines viennent de la 2e methode (flux reconstruit par VLC) : VLC y renumerote les programmes, les numeros de « program= » sont alors faux.</summary>
        Public Remux As Boolean
    End Class

    ''' <summary>Numero officiel de chaque chaine : celui du reseau (NIT) s'il est connu, sinon d'apres le nom. Les compteurs sont recalcules.</summary>
    Private Sub AttribuerLesNumeros(resultat As ResultatDuScan, lcnReseau As Dictionary(Of Long, Integer), Optional journal As Boolean = False)
        resultat.NumerosDuReseau = 0
        resultat.NumerosParNom = 0
        ' D'abord le numero diffuse par le RESEAU (la table de la TNT elle-meme, seule a jour : T18 et NOVO19 ont pris le 18 et le 19, ce que ma table des noms ignore) ; ensuite, pour les chaines
        ' que le reseau ne numerote pas, la table des noms francais. Jamais deux chaines au meme numero : la premiere servie le garde, l'autre reste sans numero officiel.
        Dim pris As New HashSet(Of Integer)
        For Each c As ChaineTnt In resultat.Chaines
            c.Lcn = 0
            Dim n As Integer
            If lcnReseau.TryGetValue(c.Cle, n) AndAlso n > 0 AndAlso pris.Add(n) Then
                c.Lcn = n
                resultat.NumerosDuReseau += 1
            End If
        Next
        For Each c As ChaineTnt In resultat.Chaines
            If c.Lcn > 0 Then Continue For
            Dim n As Integer = NumeroOfficielParNom(c.Nom)
            If n > 0 AndAlso pris.Add(n) Then
                c.Lcn = n
                resultat.NumerosParNom += 1
            End If
        Next
        If journal Then
            For Each c As ChaineTnt In resultat.Chaines
                Dim nr As Integer
                lcnReseau.TryGetValue(c.Cle, nr)
                DiagLog("TNT scan : numero officiel de « " & c.Nom & " » : reseau " & If(nr > 0, nr.ToString(CultureInfo.InvariantCulture), "aucun") & ", table des noms " &
                        If(NumeroOfficielParNom(c.Nom) > 0, NumeroOfficielParNom(c.Nom).ToString(CultureInfo.InvariantCulture), "aucun") & " -> retenu " & If(c.Lcn > 0, c.Lcn.ToString(CultureInfo.InvariantCulture), "aucun"))
            Next
        End If
    End Sub

    ''' <summary>Un nom de liste qui n'existe pas encore : <paramref name="base"/>, sinon « base II », « base III »…</summary>
    Friend Function NomDeListeLibre(base As String) As String
        base = If(base, "").Trim()
        If base = "" Then base = "TNT"
        For Each c As Char In Path.GetInvalidFileNameChars()
            base = base.Replace(c, " "c)
        Next
        base = base.Trim()
        If base = "" Then base = "TNT"
        If Not File.Exists(FichierDeLaListe(base)) Then Return base
        Dim romains As String() = {"", "", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"}
        For n As Integer = 2 To 99
            Dim candidat As String = base & " " & If(n < romains.Length, romains(n), n.ToString(CultureInfo.InvariantCulture))
            If Not File.Exists(FichierDeLaListe(candidat)) Then Return candidat
        Next
        Return base & " " & Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Vrai si le nom du reseau diffuse peut servir de nom de ville (pas « TNT », « R1 »…).</summary>
    Friend Function NomReseauUtilisable(nom As String) As Boolean
        Dim n As String = NomNormalise(If(nom, ""))
        If n.Length < 4 Then Return False
        Select Case n
            Case "TNT", "TNTFRANCE", "FRANCE", "TDF", "TNTHD", "RNT", "NETWORK", "DVBT", "DVB"
                Return False
        End Select
        If System.Text.RegularExpressions.Regex.IsMatch(n, "^R\d+$") Then Return False
        Return True
    End Function

    ''' <summary>Balaie les canaux 21 a 48. <paramref name="rapport"/> (fait, total, texte) est appele depuis le thread du scan ; <paramref name="annule"/> est interroge souvent.</summary>
    ''' <summary><paramref name="trouve"/> (copie de toutes les chaines trouvees jusque-la, numerotees, et nom du reseau s'il est connu) est appele a chaque canal qui en apporte : la liste se remplit au fur et a mesure.</summary>
    ''' <summary>Le lecteur VLC de Slide (celui de la fenetre TNT, integre : pas de vlc.exe a part) que le scan pilote, 23/11/2026. Les trois actions sont executees sur le fil de la fenetre par FormTnt.</summary>
    Friend NotInheritable Class PiloteDeScan
        ''' <summary>(frequence en Hz, fichier ou ecrire le flux, methode 0 ou 1) : accorde le tuner et ecrit le flux recu.</summary>
        Public Demarrer As Action(Of Long, String, Integer)
        ''' <summary>Arrete le lecteur (le tuner est libere).</summary>
        Public Arreter As Action
        ''' <summary>Messages (erreurs, avertissements) de VLC depuis le dernier Demarrer.</summary>
        Public MessagesDeVlc As Func(Of List(Of String))
    End Class

    Friend Function Scanner(rapport As Action(Of Integer, Integer, String), annule As Func(Of Boolean), trouve As Action(Of List(Of ChaineTnt), String), pilote As PiloteDeScan) As ResultatDuScan
        Dim resultat As New ResultatDuScan()
        Dim lcnReseau As New Dictionary(Of Long, Integer)
        Dim nomReseau As String = ""
        Dim total As Integer = DernierCanal - PremierCanal + 1
        Dim donneesRecues As Boolean = False
        DiagLog("TNT scan : debut (canaux " & PremierCanal & " a " & DernierCanal & ")")
        For methode As Integer = 0 To 1
            If methode = 1 Then
                If donneesRecues OrElse annule() Then Exit For
                DiagLog("TNT scan : aucune donnee avec la premiere methode, deuxieme methode (sortie par flux)")
            End If
            For canal As Integer = PremierCanal To DernierCanal
                If annule() Then Exit For
                Dim freq As Long = (474L + 8L * (canal - PremierCanal)) * 1000000L + 166000L
                rapport(canal - PremierCanal, total, "Canal " & canal & " (" & (freq / 1000000.0).ToString("0.000", CultureInfo.InvariantCulture).Replace("."c, ","c) & " MHz)" & If(methode = 1, " - 2e methode", "") &
                        " : " & resultat.Chaines.Count & " chaîne(s) trouvée(s)")
                Dim analyse As AnalyseTs = Capturer(pilote, freq, canal, methode, annule)
                If analyse Is Nothing Then Continue For
                donneesRecues = True
                For Each kv As KeyValuePair(Of Long, Integer) In analyse.Lcn
                    lcnReseau(kv.Key) = kv.Value
                Next
                Dim vus As Integer = 0
                For Each svc As AnalyseTs.ServiceTs In analyse.Services.Values
                    If Not ServiceTelevision(svc.Genre) OrElse svc.Nom = "" Then Continue For
                    If svc.Crypte Then
                        resultat.Cryptees += 1
                        Continue For
                    End If
                    If resultat.Chaines.Any(Function(c) c.Frequence = freq AndAlso c.Programme = svc.Id) Then Continue For
                    resultat.Chaines.Add(New ChaineTnt With {.Nom = svc.Nom, .Frequence = freq, .Programme = svc.Id, .Cle = (CLng(analyse.TsId) << 16) Or CLng(svc.Id)})
                    DiagLog("TNT scan : canal " & canal & " : « " & svc.Nom & " » programme " & svc.Id & " (ts " & analyse.TsId & ", genre " & svc.Genre & If(analyse.ProgrammesPat.Contains(svc.Id), ", dans la PAT", ", ABSENT de la PAT") & ")")
                    vus += 1
                Next
                If vus > 0 Then resultat.CanauxAvecSignal += 1
                If vus > 0 AndAlso methode = 1 Then
                    If Not resultat.Remux Then DiagLog("TNT scan : ATTENTION, chaines trouvees par la 2e methode (flux reconstruit par VLC) : les numeros de programme ne sont pas ceux de la TNT")
                    resultat.Remux = True
                End If
                If nomReseau = "" AndAlso analyse.NomReseau <> "" Then
                    nomReseau = analyse.NomReseau
                    DiagLog("TNT scan : nom du reseau « " & nomReseau & " »")
                End If
                If vus > 0 AndAlso trouve IsNot Nothing Then
                    AttribuerLesNumeros(resultat, lcnReseau)
                    trouve(resultat.Chaines.Select(Function(c) New ChaineTnt With {.Nom = c.Nom, .Frequence = c.Frequence, .Programme = c.Programme, .Lcn = c.Lcn, .Cle = c.Cle}).ToList(), nomReseau)
                End If
            Next
        Next
        rapport(total, total, "Scan terminé : " & resultat.Chaines.Count & " chaîne(s) sur " & resultat.CanauxAvecSignal & " canal(aux).")
        AttribuerLesNumeros(resultat, lcnReseau, True)
        resultat.NomReseau = nomReseau
        DiagLog("TNT scan : fin, " & resultat.Chaines.Count & " chaine(s), " & resultat.CanauxAvecSignal & " canal(aux) avec signal, numeros du reseau " &
                resultat.NumerosDuReseau & ", numeros par nom " & resultat.NumerosParNom & ", cryptees ignorees " & resultat.Cryptees)
        Return resultat
    End Function

    ''' <summary>Services de television : numerique SD (1), HD MPEG-2 (&H11), H.264 SD/HD (&H16, &H19), HEVC (&H1F). Hors radio et donnees.</summary>
    Private Function ServiceTelevision(genre As Integer) As Boolean
        Select Case genre
            Case 1, &H11, &H16, &H19, &H1F, &H1C, &H1D, &H20, &H21, &H22, &H23, &H24, &H25
                Return True
        End Select
        Return False
    End Function

    ''' <summary>Fait ecrire le flux d'un canal par le lecteur VLC de Slide (via <paramref name="pilote"/>) et le lit en meme temps ; rend Nothing si rien n'est recu (pas de signal).</summary>
    Private Function Capturer(pilote As PiloteDeScan, freq As Long, canal As Integer, methode As Integer, annule As Func(Of Boolean)) As AnalyseTs
        ' Un fichier PROPRE A CHAQUE CANAL (23/11/2026) : avec un nom unique, le lecteur de VLC qui finit d'ecrire le canal precedent ne peut plus verser ses paquets dans le fichier du canal suivant
        ' (des chaines auraient alors ete rangees sous la mauvaise frequence).
        Dim fichier As String = Path.Combine(Path.GetTempPath(), "slide_tnt_scan_" & canal.ToString(CultureInfo.InvariantCulture) & "_" & methode.ToString(CultureInfo.InvariantCulture) & "_" & Environment.TickCount64.ToString(CultureInfo.InvariantCulture) & ".ts")
        Dim analyse As New AnalyseTs()
        Dim octets As Long = 0
        Dim lu As Long = 0
        Dim depuisLesDonnees As Stopwatch = Nothing
        Try
            pilote.Demarrer(freq, fichier, methode)
            Dim chrono As Stopwatch = Stopwatch.StartNew()
            Do
                Thread.Sleep(400)
                If annule() Then Exit Do
                octets = If(File.Exists(fichier), New FileInfo(fichier).Length, 0L)
                If octets = 0 Then
                    If chrono.Elapsed.TotalSeconds > 12 Then Exit Do                ' pas de signal sur ce canal (le tuner met quelques secondes a s'accorder)
                    Continue Do
                End If
                If depuisLesDonnees Is Nothing Then depuisLesDonnees = Stopwatch.StartNew()
                If octets >= lu + 188L * 40L Then lu = analyse.Lire(fichier, lu)
                Dim vu As Double = depuisLesDonnees.Elapsed.TotalSeconds
                If analyse.ListeComplete() AndAlso (analyse.NitVu OrElse vu > 14) Then Exit Do
                If vu > 24 Then Exit Do
            Loop
            If octets > 0 Then lu = analyse.Lire(fichier, lu)          ' (le dernier morceau recu)
        Catch ex As Exception
            DiagLog("TNT scan : canal " & canal & " : " & ex.Message)
        End Try
        Dim messages As List(Of String) = Nothing
        Try
            messages = pilote.MessagesDeVlc()
        Catch ex As Exception
        End Try
        Try
            pilote.Arreter()
        Catch ex As Exception
            DiagLog("TNT scan : arret du lecteur (" & ex.Message & ")")
        End Try
        Thread.Sleep(800)                                                  ' (le tuner a besoin d'un instant avant le canal suivant)
        DiagLog("TNT scan : canal " & canal & " (" & freq & " Hz) : " & octets & " octets, PAT=" & analyse.PatVu & " SDT=" & analyse.SdtVu & " NIT=" & analyse.NitVu &
                ", " & analyse.Services.Count & " service(s), " & analyse.Lcn.Count & " numero(s) officiel(s)")
        ' Aucun octet : ce que VLC a dit (erreurs, avertissements) pour les trois premiers canaux de chaque methode, pour comprendre pourquoi rien n'arrive.
        If ((octets = 0 AndAlso canal = PremierCanal) OrElse (octets > 0 AndAlso Not analyse.PatVu)) AndAlso messages IsNot Nothing Then
            DiagLog("TNT scan : messages de VLC, canal " & canal & " (methode " & methode & ", " & messages.Count & " message(s)) :")
            For Each l As String In messages.Skip(Math.Max(0, messages.Count - 12))
                DiagLog("TNT scan :   vlc> " & If(l.Length > 240, l.Substring(0, 240), l))
            Next
        End If
        Try
            If File.Exists(fichier) Then File.Delete(fichier)
        Catch ex As Exception
        End Try
        If Not analyse.PatVu AndAlso Not analyse.SdtVu Then Return Nothing
        Return analyse
    End Function

End Module
