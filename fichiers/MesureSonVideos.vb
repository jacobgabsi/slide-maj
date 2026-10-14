Option Strict On
Option Explicit On

Imports System.IO.Compression
Imports System.Net.Http
Imports System.Security.Cryptography

''' <summary>
''' Mesure A L'AVANCE du niveau sonore des videos des sources (6 400 videos chez Jacob), pour le
''' lissage du son (NiveauSonore) : une video deja mesuree demarre directement au bon volume, sans
''' l'ajustement des premieres secondes. Les fichiers ne sont JAMAIS modifies.
'''
''' Meme mesure que pendant la lecture : NiveauSonore lit le vumetre de Slide toutes les 50 ms
''' (crete), de 0,5 s a 15 s, et garde 10 log10(moyenne des cretes au carre). Ici ffmpeg decode
''' le son de la video sur la meme duree, en tranches de 50 ms (2 400 echantillons a 48 kHz), et
''' donne la crete de chaque tranche ; meme calcul, meme seuil de silence.
'''
''' ffmpeg (data\ffmpeg.exe) est installe en silence s'il manque : compilation "essentials" de
''' gyan.dev (celle que propose ffmpeg.org pour Windows), verifiee par son empreinte SHA-256.
''' </summary>
Public Module MesureSonVideos

    Public ReadOnly ExtensionsVideo As String() = {".mp4", ".mov", ".m4v", ".3gp", ".avi", ".mkv", ".mpg", ".mpeg", ".wmv"}

    Private Const UrlFfmpeg As String = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"

    Public Enum ResultatMesure
        Mesure
        SansSon
        Echec
        Interrompu
    End Enum

    Public Function CheminFfmpeg() As String
        Return IO.Path.Combine(AppPath, "data", "ffmpeg.exe")
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Videos a mesurer
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Dossiers de videos des sources (dossiers actifs, videos activees : "@1x1x").</summary>
    Public Function DossiersVideo() As List(Of String)
        Dim dossiers As New List(Of String)
        Try
            If My.Settings.CheminPhotoVideo Is Nothing Then Return dossiers
            For Each ligne As String In My.Settings.CheminPhotoVideo
                Dim chemin As String = ligne, opt As String = "1111"
                Dim p As Integer = ligne.IndexOf("@"c)
                If p >= 0 Then chemin = ligne.Substring(0, p) : opt = ligne.Substring(p + 1)
                If chemin.Trim() = "" OrElse opt.Length < 4 OrElse opt(0) <> "1"c OrElse opt(2) <> "1"c Then Continue For
                dossiers.Add(chemin.Trim())
            Next
        Catch ex As Exception
            DiagLog("Mesure des videos : sources illisibles (" & ex.Message & ")")
        End Try
        Return dossiers.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    End Function

    ''' <summary>Les videos des dossiers qui ne sont ni deja mesurees, ni connues sans son.
    ''' Tache de fond.</summary>
    Public Function VideosAMesurer(dossiers As List(Of String), connus As HashSet(Of String)) As List(Of String)
        Dim sansSon As HashSet(Of String) = VideosSansSon()
        Dim liste As New List(Of String)
        Dim vus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each dossier As String In dossiers
            If Not IO.Directory.Exists(dossier) Then Continue For
            Try
                For Each chemin As String In IO.Directory.EnumerateFiles(dossier, "*.*", IO.SearchOption.AllDirectories)
                    If Not ExtensionsVideo.Contains(IO.Path.GetExtension(chemin).ToLowerInvariant()) Then Continue For
                    Dim cle As String = NiveauSonore.CleNormalisee(chemin)
                    If connus.Contains(cle) OrElse sansSon.Contains(cle) OrElse Not vus.Add(cle) Then Continue For
                    liste.Add(chemin)
                Next
            Catch ex As Exception
                DiagLog("Mesure des videos : dossier illisible " & dossier & " (" & ex.Message & ")")
            End Try
        Next
        Return liste
    End Function

    ' Videos sans piste son (ou silencieuses) : notees pour ne pas les remesurer a chaque fois.
    Private Function FichierSansSon() As String
        Return IO.Path.Combine(DossierReglages, "VideosSansSon.txt")
    End Function

    Private Function VideosSansSon() As HashSet(Of String)
        Dim s As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Try
            If IO.File.Exists(FichierSansSon()) Then
                For Each l As String In IO.File.ReadAllLines(FichierSansSon(), Text.Encoding.UTF8)
                    If l.Trim() <> "" Then s.Add(l.Trim())
                Next
            End If
        Catch ex As Exception
        End Try
        Return s
    End Function

    Public Sub NoterSansSon(chemin As String)
        Try
            IO.File.AppendAllText(FichierSansSon(), NiveauSonore.CleNormalisee(chemin) & vbCrLf, New Text.UTF8Encoding(False))
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Installation silencieuse de ffmpeg
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Installe data\ffmpeg.exe s'il manque (tache de fond). <paramref name="progression"/>
    ''' recoit le pourcentage du telechargement. Renvoie le chemin de ffmpeg, ou "" en cas d'echec
    ''' (pas d'internet...) : la mesure attendra le prochain demarrage.</summary>
    Public Function InstallerFfmpeg(progression As Action(Of Integer), annule As Func(Of Boolean)) As String
        Dim cible As String = CheminFfmpeg()
        If IO.File.Exists(cible) Then Return cible
        Dim zip As String = IO.Path.Combine(IO.Path.GetTempPath(), "slide_ffmpeg_" & Guid.NewGuid().ToString("N") & ".zip")
        Try
            DiagLog("Mesure des videos : ffmpeg absent, telechargement de " & UrlFfmpeg)
            Using client As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(30)}
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Slide")
                Dim attendu As String = client.GetStringAsync(UrlFfmpeg & ".sha256").Result.Trim().Split({" "c, ControlChars.Tab}, StringSplitOptions.RemoveEmptyEntries)(0).ToLowerInvariant()
                Using reponse As HttpResponseMessage = client.GetAsync(UrlFfmpeg, HttpCompletionOption.ResponseHeadersRead).Result
                    reponse.EnsureSuccessStatusCode()
                    Dim taille As Long = If(reponse.Content.Headers.ContentLength, 0L)
                    Using source As IO.Stream = reponse.Content.ReadAsStreamAsync().Result,
                          dest As New IO.FileStream(zip, IO.FileMode.Create, IO.FileAccess.Write)
                        Dim tampon(81919) As Byte
                        Dim lus As Long = 0, dernier As Integer = -1
                        Dim n As Integer = source.Read(tampon, 0, tampon.Length)
                        Do While n > 0
                            If annule() Then Return ""
                            dest.Write(tampon, 0, n)
                            lus += n
                            If taille > 0 Then
                                Dim pc As Integer = CInt(lus * 100 \ taille)
                                If pc <> dernier Then dernier = pc : progression(pc)
                            End If
                            n = source.Read(tampon, 0, tampon.Length)
                        Loop
                    End Using
                End Using
                ' Fichier intact et authentique : meme empreinte que celle publiee par le site.
                Dim obtenu As String
                Using f As IO.FileStream = IO.File.OpenRead(zip), sha As SHA256 = SHA256.Create()
                    obtenu = Convert.ToHexString(sha.ComputeHash(f)).ToLowerInvariant()
                End Using
                If obtenu <> attendu Then
                    DiagLog("Mesure des videos : ffmpeg telecharge refuse (empreinte " & obtenu & " au lieu de " & attendu & ")")
                    Return ""
                End If
            End Using
            ' Seul ffmpeg.exe est garde (le reste de l'archive ne sert pas).
            Using archive As ZipArchive = ZipFile.OpenRead(zip)
                Dim entree As ZipArchiveEntry = archive.Entries.FirstOrDefault(
                    Function(e) e.FullName.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                If entree Is Nothing Then
                    DiagLog("Mesure des videos : ffmpeg.exe absent de l'archive")
                    Return ""
                End If
                Dim provisoire As String = cible & ".tmp"
                entree.ExtractToFile(provisoire, True)
                IO.File.Move(provisoire, cible, True)
            End Using
            DiagLog("Mesure des videos : ffmpeg installe (" & cible & ")")
            Return cible
        Catch ex As Exception
            Dim e As Exception = If(ex.InnerException, ex)
            DiagLog("Mesure des videos : installation de ffmpeg impossible (" & e.Message & ")")
            Return ""
        Finally
            Try
                If IO.File.Exists(zip) Then IO.File.Delete(zip)
            Catch ex As Exception
            End Try
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Mesure d'une video : SONIE (volume ressenti, norme EBU R128, en LUFS), a plusieurs endroits.
    '
    ' 02/10, "les vieilles videos VHS sont plus fortes malgre le lissage" : la premiere mesure
    ' (pointes du son sur les 15 premieres secondes) ne convenait pas a ces videos de 4 a 35 min
    ' dont le diaporama joue des extraits pris au hasard : "1991 - Anniversaire" vaut -19 LUFS au
    ' debut, -15 au milieu, -9,7 vers la fin. Et a pointes egales, le son dense d'une VHS parait
    ' plus fort. On mesure donc le volume ressenti, tout au long du fichier (une fenetre de 20 s
    ' tous les ~30 s) ; le lissage prend la valeur de l'extrait joue (NiveauSonore).
    ' ------------------------------------------------------------------------------------------

    Private Const FenetreSecondes As Integer = 20
    Private Const SilenceLufs As Double = -60

    ''' <summary>Sonie de la video : liste (position en secondes, LUFS). Tache de fond.</summary>
    Public Function MesurerVideo(ffmpeg As String, chemin As String, mesures As List(Of KeyValuePair(Of Double, Double)),
                                 annule As Func(Of Boolean)) As ResultatMesure
        mesures.Clear()
        Dim duree As Double = 0
        Dim sansSon As Boolean = False
        ' Premiere fenetre : le debut (40 s : une video courte est ainsi mesuree en entier).
        Dim premiere As Double? = MesurerFenetre(ffmpeg, chemin, -1, 40, duree, sansSon, annule)
        If annule() Then Return ResultatMesure.Interrompu
        If sansSon Then Return ResultatMesure.SansSon
        If Not premiere.HasValue AndAlso duree <= 0 Then Return ResultatMesure.Echec
        If premiere.HasValue AndAlso premiere.Value > SilenceLufs Then mesures.Add(New KeyValuePair(Of Double, Double)(0, premiere.Value))
        ' Video longue : une fenetre de 20 s tous les 30 s (40 fenetres au plus).
        If duree > 45 Then
            Dim pas As Double = Math.Max(30, duree / 40)
            Dim position As Double = 40
            Do While position < duree - 5
                If annule() Then Return ResultatMesure.Interrompu
                Dim d As Double = 0, s As Boolean = False
                Dim v As Double? = MesurerFenetre(ffmpeg, chemin, position, FenetreSecondes, d, s, annule)
                If v.HasValue AndAlso v.Value > SilenceLufs Then mesures.Add(New KeyValuePair(Of Double, Double)(position, v.Value))
                position += pas
            Loop
        End If
        Return If(mesures.Count > 0, ResultatMesure.Mesure, ResultatMesure.SansSon)
    End Function

    ''' <summary>Sonie (LUFS) d'une fenetre de la video, Nothing si ffmpeg ne la donne pas.
    ''' <paramref name="debut"/> negatif : depuis le debut du fichier.</summary>
    Private Function MesurerFenetre(ffmpeg As String, chemin As String, debut As Double, secondes As Integer,
                                    ByRef duree As Double, ByRef sansSon As Boolean, annule As Func(Of Boolean)) As Double?
        Dim infos As New ProcessStartInfo(ffmpeg) With {
            .UseShellExecute = False, .CreateNoWindow = True,
            .RedirectStandardOutput = True, .RedirectStandardError = True}
        Dim arguments As New List(Of String) From {"-hide_banner", "-nostats", "-nostdin"}
        If debut >= 0 Then arguments.AddRange({"-ss", debut.ToString("0.###", Globalization.CultureInfo.InvariantCulture)})
        arguments.AddRange({"-t", secondes.ToString(), "-i", chemin, "-vn", "-sn", "-map", "0:a:0",
                            "-af", "ebur128=framelog=quiet", "-f", "null", "-"})
        For Each a As String In arguments
            infos.ArgumentList.Add(a)
        Next
        Try
            Using p As Process = Process.Start(infos)
                Try
                    p.PriorityClass = ProcessPriorityClass.BelowNormal   ' le diaporama passe d'abord
                Catch ex As Exception
                End Try
                Dim sortie As Threading.Tasks.Task(Of String) = p.StandardOutput.ReadToEndAsync()
                Dim erreurs As Threading.Tasks.Task(Of String) = p.StandardError.ReadToEndAsync()
                ' ffmpeg ne fait que lire : on peut l'arreter a tout moment.
                Do Until p.WaitForExit(100)
                    If annule() Then
                        Try
                            p.Kill()
                        Catch ex As Exception
                        End Try
                        Return Nothing
                    End If
                Loop
                p.WaitForExit()
                Dim texte As String = erreurs.Result
                Dim md As System.Text.RegularExpressions.Match =
                    System.Text.RegularExpressions.Regex.Match(texte, "Duration: (\d+):(\d+):(\d+(?:\.\d+)?)")
                If md.Success Then
                    duree = Integer.Parse(md.Groups(1).Value) * 3600 + Integer.Parse(md.Groups(2).Value) * 60 +
                            Double.Parse(md.Groups(3).Value, Globalization.CultureInfo.InvariantCulture)
                End If
                ' Pas de piste son (message selon la version de ffmpeg ; 9.0 : "Failed to set value
                ' '0:a:0' for option 'map'").
                If texte.Contains("matches no streams") OrElse texte.Contains("does not contain any stream") OrElse
                   texte.Contains("for option 'map'") Then
                    sansSon = True
                    Return Nothing
                End If
                ' Resume d'ebur128 : "    I:         -19.0 LUFS" (le dernier est le bilan).
                Dim mi As System.Text.RegularExpressions.MatchCollection =
                    System.Text.RegularExpressions.Regex.Matches(texte, "\bI:\s+(-?\d+(?:\.\d+)?) LUFS")
                If mi.Count = 0 Then
                    If p.ExitCode <> 0 Then
                        DiagLog("Mesure des videos : echec sur " & chemin & " (code " & p.ExitCode & ") " &
                                texte.Trim().Split({ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
                    End If
                    Return Nothing
                End If
                Return Double.Parse(mi(mi.Count - 1).Groups(1).Value, Globalization.CultureInfo.InvariantCulture)
            End Using
        Catch ex As Exception
            DiagLog("Mesure des videos : " & chemin & " (" & ex.Message & ")")
            Return Nothing
        End Try
    End Function
End Module
