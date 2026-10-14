Option Strict On
Option Explicit On

''' <summary>
''' Normalisation du volume des musiques MP3 (bouton "Volume Normalizer" des reglages), puis
''' mesure du son des videos pour le lissage (MesureSonVideos), en TACHE
''' DE FOND : Slide reste utilisable pendant les longues minutes d'analyse, et la fenetre de
''' progression peut etre reduite a une pastille "%" en haut a gauche.
'''
''' Un morceau a la fois (mp3gain -r -k -c -q), a priorite basse pour ne pas saccader le
''' diaporama. Le resultat de chaque morceau s'affiche dans le journal de la fenetre.
'''
''' Morceau occupe (celui que Slide joue, ou ouvert par un autre programme) : il est REPORTE a la
''' fin, puis traite des qu'il est libere (fin de sa lecture). mp3gain ecrit dans le fichier :
''' le modifier pendant sa lecture echouait ou faisait craquer le son.
''' </summary>
Public Module NormalisationMusique

    Private _fenetre As FormProgression
    Private _enCours As Boolean

    Private ReadOnly Orange As Color = Color.FromArgb(255, 159, 10)
    Private ReadOnly Vert As Color = Color.FromArgb(48, 209, 88)
    Private ReadOnly Gris As Color = Color.FromArgb(142, 142, 147)
    Private ReadOnly Rouge As Color = Color.FromArgb(255, 69, 58)
    Private ReadOnly Bleu As Color = Color.FromArgb(100, 170, 255)

    ''' <summary>Vrai pendant une normalisation.</summary>
    Public ReadOnly Property EnCours As Boolean
        Get
            Return _enCours
        End Get
    End Property

    ''' <summary>Recale la pastille tout de suite (passage plein ecran / fenetre).</summary>
    Public Sub RecalerLaPastille()
        If _fenetre IsNot Nothing AndAlso Not _fenetre.IsDisposed Then _fenetre.RecalerLaPastille()
    End Sub

    ''' <summary>Rouvre la fenetre complete de la normalisation en cours.</summary>
    Public Sub Montrer()
        If _fenetre IsNot Nothing AndAlso Not _fenetre.IsDisposed Then _fenetre.Agrandir()
    End Sub

    ''' <summary>Fichier qui garde l'empreinte de la liste des sources normalisee.</summary>
    Public Function FichierEmpreinte() As String
        Return IO.Path.Combine(DossierReglages, "NormalisationMusique.txt")
    End Function

    Public Sub MemoriserEmpreinte(empreinte As List(Of String))
        Try
            IO.File.WriteAllLines(FichierEmpreinte(), empreinte, New Text.UTF8Encoding(False))
        Catch ex As Exception
            DiagLog("Normalisation : empreinte non enregistree (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Empreinte des sources de musique d'apres les reglages (dossiers actifs, musique
    ''' activee : options "@1xx1"), au meme format que celle de l'onglet Sources de Form4.</summary>
    Public Function EmpreinteDesReglages(Optional dossiers As List(Of String) = Nothing) As List(Of String)
        Dim empreinte As New List(Of String)
        Try
            If My.Settings.CheminPhotoVideo Is Nothing Then Return empreinte
            For Each ligne As String In My.Settings.CheminPhotoVideo
                Dim chemin As String = ligne, opt As String = "1111"
                Dim p As Integer = ligne.IndexOf("@"c)
                If p >= 0 Then chemin = ligne.Substring(0, p) : opt = ligne.Substring(p + 1)
                If chemin.Trim() = "" OrElse opt.Length < 4 OrElse opt(0) <> "1"c OrElse opt(3) <> "1"c Then Continue For
                empreinte.Add(chemin.Trim().TrimEnd("\"c).ToLowerInvariant())
                If dossiers IsNot Nothing Then dossiers.Add(chemin.Trim())
            Next
        Catch ex As Exception
            DiagLog("Normalisation : sources illisibles (" & ex.Message & ")")
        End Try
        empreinte.Sort(StringComparer.Ordinal)
        Return empreinte.Distinct().ToList()
    End Function

    ''' <summary>
    ''' Les dossiers (sous-dossiers compris) qui contiennent au moins un fichier .mp3. mp3gain ne traite QUE les MP3 :
    ''' Slide ne propose le lissage des musiques que s'il y en a (demande de Jacob, 14/10/2026 : "slide ne doit
    ''' proposer le lissage des mp3 que s'il y a des mp3 dans la source"). A appeler hors du fil de l'interface :
    ''' un dossier sans MP3 est parcouru en entier.
    ''' </summary>
    Public Function DossiersAvecMp3(dossiers As IEnumerable(Of String)) As List(Of String)
        Dim avec As New List(Of String)
        For Each d As String In dossiers
            Try
                If d <> "" AndAlso IO.Directory.Exists(d) AndAlso
                   IO.Directory.EnumerateFiles(d, "*.mp3", IO.SearchOption.AllDirectories).Any(Function(f) f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) Then
                    avec.Add(d)
                End If
            Catch ex As Exception
                ' dossier illisible : pas de MP3 connus
            End Try
        Next
        Return avec
    End Function

    ''' <summary>Vrai si la liste des sources de musique a change depuis la derniere normalisation
    ''' (ou si elle n'a jamais ete faite).</summary>
    Public Function NormalisationAFaire(empreinte As List(Of String)) As Boolean
        If empreinte.Count = 0 Then Return False
        Dim derniere As New List(Of String)
        Try
            If IO.File.Exists(FichierEmpreinte()) Then derniere = IO.File.ReadAllLines(FichierEmpreinte()).Where(Function(l) l.Trim() <> "").ToList()
        Catch ex As Exception
        End Try
        Return Not derniere.SequenceEqual(empreinte)
    End Function

    Private _minuteurDemarrage As Timer
    Private _refusee As Boolean                 ' "Non" a la question des reglages : pas de lancement automatique cette fois

    ''' <summary>
    ''' La question "Lancer la normalisation maintenant ?" (ouverture des reglages) a recu "Non" :
    ''' le lancement automatique du demarrage n'a pas lieu non plus pendant cette utilisation de
    ''' Slide (bug signale par Jacob, 08/10/2026 : "j'ai repondu non, pourtant il a quand meme
    ''' lance le lissage" - le lancement automatique, prevu vingt secondes apres le demarrage puis
    ''' retarde par la recherche des videos a mesurer, partait apres la reponse). Le bouton
    ''' "Volume Normalizer" reste utilisable ; au prochain lancement de Slide, le travail de fond
    ''' reprend normalement.
    ''' </summary>
    Public Sub NePasLancerCetteFois()
        _refusee = True
        Try
            If _minuteurDemarrage IsNot Nothing Then _minuteurDemarrage.Stop()
        Catch ex As Exception
        End Try
        DiagLog("Normalisation : refusee a l'ouverture des reglages -> pas de lancement automatique cette fois")
    End Sub


    ''' <summary>
    ''' Au demarrage, apres <paramref name="secondes"/> secondes (le temps que le diaporama et la
    ''' musique demarrent) : en tache de fond, Slide regarde s'il y a du travail - sources de
    ''' musique changees depuis la derniere normalisation, ou videos pas encore mesurees pour le
    ''' lissage. S'il y en a, la pastille "%" apparait et le travail se fait ; sinon rien ne se voit.
    ''' </summary>
    Public Sub PlanifierAuDemarrage(secondes As Integer)
        If _minuteurDemarrage IsNot Nothing Then Exit Sub
        _minuteurDemarrage = New Timer With {.Interval = Math.Max(1, secondes) * 1000}
        AddHandler _minuteurDemarrage.Tick,
            Sub()
                _minuteurDemarrage.Stop()
                If _enCours OrElse _refusee Then Exit Sub
                Dim mp3gain As String = IO.Path.Combine(AppPath, "data", "mp3gain.exe")
                Dim dossiersMusique As New List(Of String)
                Dim empreinte As List(Of String) = EmpreinteDesReglages(dossiersMusique)
                If Not NormalisationAFaire(empreinte) OrElse Not IO.File.Exists(mp3gain) Then dossiersMusique.Clear()
                Dim dossiersVideo As List(Of String) = MesureSonVideos.DossiersVideo()
                If dossiersMusique.Count = 0 AndAlso dossiersVideo.Count = 0 Then Exit Sub
                Dim connus As HashSet(Of String) = NiveauSonore.SonieConnue()     ' thread de l'interface
                Dim principale As Form = FenetrePrincipale()
                _enCours = True
                Dim tache As New Threading.Thread(
                    Sub()
                        ' Pas de MP3 dans les sources : rien a normaliser, et la liste est retenue (plus de question).
                        If dossiersMusique.Count > 0 Then
                            dossiersMusique = DossiersAvecMp3(dossiersMusique)
                            If dossiersMusique.Count = 0 Then
                                MemoriserEmpreinte(empreinte)
                                DiagLog("Normalisation : aucun MP3 dans les sources de musique, rien a normaliser")
                            End If
                        End If
                        Dim videos As List(Of String) = MesureSonVideos.VideosAMesurer(dossiersVideo, connus)
                        If dossiersMusique.Count = 0 AndAlso videos.Count = 0 Then
                            _enCours = False                    ' rien a faire : rien ne s'affiche
                            Exit Sub
                        End If
                        ' "Non" repondu entre-temps a la question des reglages : on n'affiche ni ne lance rien.
                        If _refusee Then
                            _enCours = False
                            Exit Sub
                        End If
                        DiagLog("Normalisation : lancement automatique (reduit) - musique " &
                                If(dossiersMusique.Count > 0, "a normaliser", "a jour") & ", " & videos.Count & " video(s) a mesurer")
                        Try
                            principale.Invoke(Sub() _fenetre = FormProgression.Afficher("Normalisation du volume", principale, True))
                        Catch ex As Exception
                            _enCours = False
                            Exit Sub
                        End Try
                        Traiter(mp3gain, dossiersMusique.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), empreinte, videos)
                    End Sub) With {.IsBackground = True, .Name = "Normalisation", .Priority = Threading.ThreadPriority.BelowNormal}
                tache.Start()
            End Sub
        _minuteurDemarrage.Start()
    End Sub

    ''' <summary>Lance la normalisation (bouton "Volume Normalizer", thread de l'interface) :
    ''' musiques des <paramref name="dossiers"/>, puis mesure des videos pas encore mesurees.
    ''' <paramref name="empreinte"/> : liste des sources a memoriser si tout est traite.</summary>
    Public Sub Lancer(dossiers As List(Of String), empreinte As List(Of String), devant As Form, Optional reduite As Boolean = False)
        If _enCours Then
            Montrer()
            Exit Sub
        End If
        Dim mp3gain As String = IO.Path.Combine(AppPath, "data", "mp3gain.exe")
        If Not IO.File.Exists(mp3gain) Then
            MsgBox("mp3gain.exe introuvable dans le dossier data.", vbExclamation)
            Exit Sub
        End If
        _enCours = True
        _fenetre = FormProgression.Afficher("Normalisation du volume", devant, reduite)
        _fenetre.Avancer("Recherche des morceaux…", 0, 1)
        Dim dossiersVideo As List(Of String) = MesureSonVideos.DossiersVideo()
        Dim connus As HashSet(Of String) = NiveauSonore.SonieConnue()
        Dim tache As New Threading.Thread(
            Sub() Traiter(mp3gain, dossiers, empreinte, MesureSonVideos.VideosAMesurer(dossiersVideo, connus))) With {
            .IsBackground = True, .Name = "Normalisation", .Priority = Threading.ThreadPriority.BelowNormal}
        tache.Start()
    End Sub

    ''' <summary>Execute une action sur la fenetre, depuis la tache de fond.</summary>
    Private Sub Ui(action As Action(Of FormProgression))
        Dim f As FormProgression = _fenetre
        If f Is Nothing OrElse f.IsDisposed OrElse Not f.IsHandleCreated Then Exit Sub
        Try
            f.BeginInvoke(Sub() If Not f.IsDisposed Then action(f))
        Catch ex As Exception
        End Try
    End Sub

    Private Function Annule() As Boolean
        Dim f As FormProgression = _fenetre
        Return f IsNot Nothing AndAlso f.Annule
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Tache de fond
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Bilan de la phase musique.</summary>
    Private Class BilanMusique
        Public Faite As Boolean
        Public Total As Integer
        Public Faits As Integer
        Public Erreurs As Integer
        Public Message As String = ""
    End Class

    ''' <summary>Bilan de la phase videos.</summary>
    Private Class BilanVideos
        Public Total As Integer
        Public Mesurees As Integer
        Public SansSon As Integer
        Public Erreurs As Integer
        Public FfmpegAbsent As Boolean
    End Class

    Private Sub Traiter(mp3gain As String, dossiersMusique As List(Of String), empreinte As List(Of String), videos As List(Of String))
        Dim bm As New BilanMusique()
        Dim bv As New BilanVideos With {.Total = videos.Count}
        Try
            If dossiersMusique.Count > 0 Then TraiterMusique(mp3gain, dossiersMusique, bm)
            If videos.Count > 0 AndAlso Not Annule() Then TraiterVideos(videos, bv)
        Catch ex As Exception
            DiagLog("Normalisation interrompue : " & ex.Message)
        End Try
        Terminer(bm, bv, empreinte)
    End Sub

    ''' <summary>Phase 1 : mp3gain sur les musiques MP3.</summary>
    Private Sub TraiterMusique(mp3gain As String, dossiers As List(Of String), bilanFinal As BilanMusique)
        Dim fichiers As New List(Of String)
        Dim autres As Integer = 0
        Dim formatsAudio As String() = {".m4a", ".flac", ".wma", ".wav", ".ogg", ".aac", ".opus"}
        Dim vus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each dossier As String In dossiers
            If Not IO.Directory.Exists(dossier) Then Continue For
            Try
                For Each chemin As String In IO.Directory.EnumerateFiles(dossier, "*.*", IO.SearchOption.AllDirectories)
                    Dim ext As String = IO.Path.GetExtension(chemin).ToLowerInvariant()
                    If ext = ".mp3" Then
                        If vus.Add(chemin) Then fichiers.Add(chemin)
                    ElseIf formatsAudio.Contains(ext) Then
                        autres += 1
                    End If
                Next
            Catch ex As Exception
                DiagLog("Normalisation : dossier illisible " & dossier & " (" & ex.Message & ")")
            End Try
        Next

        Dim total As Integer = fichiers.Count
        Dim faits As Integer = 0, baisses As Integer = 0, montes As Integer = 0, inchanges As Integer = 0, erreurs As Integer = 0
        Dim reportes As New List(Of String)
        Dim echoues As New List(Of String)

        ' Traite un morceau et note son resultat. Renvoie False si mp3gain n'a pas pu l'ecrire.
        Dim traiterUn As Func(Of String, Boolean, Boolean) =
            Function(chemin As String, dernierEssai As Boolean)
                Dim nom As String = IO.Path.GetFileNameWithoutExtension(chemin)
                Dim db As Double
                Dim r As Resultat = Normaliser(mp3gain, chemin, db)
                Select Case r
                    Case Resultat.Modifie
                        If db < 0 Then
                            baisses += 1 : Ui(Sub(f) f.Journal(nom, "baissé de " & (-db).ToString("0.#") & " dB", Orange))
                        Else
                            montes += 1 : Ui(Sub(f) f.Journal(nom, "monté de " & db.ToString("0.#") & " dB", Vert))
                        End If
                    Case Resultat.Inchange
                        inchanges += 1 : Ui(Sub(f) f.Journal(nom, "déjà au bon niveau", Gris))
                    Case Resultat.Interrompu
                        Return True                         ' annule : morceau intact, ni compte ni reessaye
                    Case Else
                        If Not dernierEssai Then Return False
                        erreurs += 1 : Ui(Sub(f) f.Journal(nom, "erreur", Rouge))
                End Select
                faits += 1
                Return True
            End Function

        Dim avancer As Action(Of String, String) =
            Sub(etape As String, detail As String)
                Dim b As String = baisses & " baissés  ·  " & montes & " montés  ·  " & inchanges & " déjà au bon niveau" &
                                  If(reportes.Count > 0, "  ·  " & reportes.Count & " en lecture, reportés", "")
                Dim fait As Integer = faits
                Ui(Sub(f)
                       f.Bilan(b)
                       f.Avancer(If(f.Annule, "Annulation…", etape), fait, Math.Max(1, total), detail)
                   End Sub)
            End Sub

        Try
            ' 1) Tous les morceaux libres ; ceux en cours de lecture sont reportes a la fin.
            For Each chemin As String In fichiers
                If Annule() Then Exit For
                RetirerLectureSeule(chemin)
                If EstOccupe(chemin) Then
                    reportes.Add(chemin)
                    Dim nom As String = IO.Path.GetFileNameWithoutExtension(chemin)
                    Ui(Sub(f) f.Journal(nom, "en lecture : à la fin", Bleu))
                    Continue For
                End If
                avancer("Morceau " & (faits + reportes.Count + 1) & " sur " & total, IO.Path.GetFileName(chemin))
                If Not traiterUn(chemin, False) Then echoues.Add(chemin)
            Next

            ' 2) Les morceaux en echec : un second essai (ils etaient peut-etre ouverts un instant).
            For Each chemin As String In echoues
                If Annule() Then Exit For
                If EstOccupe(chemin) Then reportes.Add(chemin) : Continue For
                avancer("Nouvel essai : morceau " & (faits + 1) & " sur " & total, IO.Path.GetFileName(chemin))
                traiterUn(chemin, True)
            Next

            ' 3) Les morceaux qui jouaient : on attend qu'ils soient liberes.
            Do While reportes.Count > 0 AndAlso Not Annule()
                Dim libre As String = reportes.FirstOrDefault(Function(c) Not EstOccupe(c))
                If libre IsNot Nothing Then
                    reportes.Remove(libre)
                    avancer("Morceau reporté " & (faits + 1) & " sur " & total, IO.Path.GetFileName(libre))
                    traiterUn(libre, True)
                Else
                    avancer("En attente de la fin de la musique en cours…", IO.Path.GetFileName(reportes(0)))
                    For i As Integer = 1 To 20                  ' 2 s, en restant attentif a Annuler
                        If Annule() Then Exit For
                        Threading.Thread.Sleep(100)
                    Next
                End If
            Loop
        Catch ex As Exception
            DiagLog("Normalisation interrompue : " & ex.Message)
            erreurs += 1
        End Try

        Dim complet As Boolean = (faits = total)
        Dim message As String
        If total = 0 Then
            message = "Aucune musique MP3 trouvée dans les dossiers de musique cochés."
        ElseIf Not complet Then
            message = "Normalisation annulée : " & faits & " musiques sur " & total & " ont été traitées." & vbCrLf &
                      "Relancez-la plus tard : les morceaux déjà traités seront passés rapidement."
        Else
            message = total & " musiques MP3 sont maintenant au même niveau." & vbCrLf &
                      baisses & " baissées, " & montes & " montées, " & inchanges & " déjà au bon niveau."
            If autres > 0 Then message &= vbCrLf & autres & " musique(s) dans un autre format (m4a, flac...) restent ajustées par le lissage du son."
        End If
        If erreurs > 0 Then message &= vbCrLf & "Attention : " & erreurs & " musique(s) n'ont pas pu être traitées."
        DiagLog("Normalisation : " & faits & " / " & total & " MP3 traites (" & baisses & " baisses, " & montes & " montes, " &
                inchanges & " inchanges, " & erreurs & " erreurs), " & autres & " autres formats")
        bilanFinal.Faite = True
        bilanFinal.Total = total
        bilanFinal.Faits = faits
        bilanFinal.Erreurs = erreurs
        bilanFinal.Message = message
    End Sub

    ''' <summary>Phase 2 : mesure du son des videos pas encore mesurees (ffmpeg, installe en
    ''' silence s'il manque). Les fichiers ne sont pas modifies.</summary>
    Private Sub TraiterVideos(videos As List(Of String), bv As BilanVideos)
        Dim total As Integer = videos.Count
        Ui(Sub(f)
               f.Bilan("")
               f.Avancer("Préparation de la mesure du son des vidéos…", 0, total)
           End Sub)
        Dim ffmpeg As String = MesureSonVideos.InstallerFfmpeg(
            Sub(pc As Integer) Ui(Sub(f) f.Avancer("Installation de l'outil de mesure (ffmpeg)… " & pc & " %", pc, 100)),
            AddressOf Annule)
        If ffmpeg = "" Then
            bv.FfmpegAbsent = True
            Exit Sub
        End If

        Dim faits As Integer = 0
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        For Each chemin As String In videos
            If Annule() Then Exit For
            Dim nom As String = IO.Path.GetFileName(chemin)
            Dim fait As Integer = faits
            Dim b As String = bv.Mesurees & " mesurées  ·  " & bv.SansSon & " sans son" &
                              If(bv.Erreurs > 0, "  ·  " & bv.Erreurs & " illisibles", "")
            Ui(Sub(f)
                   f.Bilan(b)
                   f.Avancer(If(f.Annule, "Annulation…", "Mesure du son : vidéo " & (fait + 1) & " sur " & total), fait, total, nom)
               End Sub)
            Dim mesures As New List(Of KeyValuePair(Of Double, Double))
            Select Case MesureSonVideos.MesurerVideo(ffmpeg, chemin, mesures, AddressOf Annule)
                Case MesureSonVideos.ResultatMesure.Mesure
                    bv.Mesurees += 1
                    Dim c As String = chemin, m As New List(Of KeyValuePair(Of Double, Double))(mesures)
                    Dim etiquette As String = If(m.Count = 1, m(0).Value.ToString("0.0") & " LUFS",
                                                 m.Min(Function(x) x.Value).ToString("0") & " à " & m.Max(Function(x) x.Value).ToString("0") & " LUFS (" & m.Count & " mesures)")
                    Ui(Sub(f)
                           NiveauSonore.MemoriserSonie(c, m)              ' thread de l'interface
                           f.Journal(IO.Path.GetFileNameWithoutExtension(c), etiquette, Vert)
                       End Sub)
                Case MesureSonVideos.ResultatMesure.SansSon
                    bv.SansSon += 1
                    MesureSonVideos.NoterSansSon(chemin)
                    Ui(Sub(f) f.Journal(IO.Path.GetFileNameWithoutExtension(chemin), "sans son", Gris))
                Case MesureSonVideos.ResultatMesure.Interrompu
                    Exit For
                Case Else
                    bv.Erreurs += 1
                    Ui(Sub(f) f.Journal(IO.Path.GetFileNameWithoutExtension(chemin), "illisible", Rouge))
            End Select
            faits += 1
        Next
        DiagLog("Mesure des videos : " & faits & " / " & total & " en " & CInt(chrono.Elapsed.TotalMinutes) & " min (" &
                bv.Mesurees & " mesurees, " & bv.SansSon & " sans son, " & bv.Erreurs & " illisibles)")
    End Sub

    ''' <summary>Fin, sur le thread de l'interface : niveaux des musiques oublies (normalisees),
    ''' empreinte memorisee, fenetre fermee, et bilan si la fenetre etait ouverte en grand.</summary>
    Private Sub Terminer(bm As BilanMusique, bv As BilanVideos, empreinte As List(Of String))
        ' Complete = tous traites SANS erreur : sinon (titres en hebreu avant la copie temporaire)
        ' les sources etaient notees "a jour" et les morceaux en erreur n'etaient jamais repris.
        Dim musiqueComplete As Boolean = bm.Faite AndAlso bm.Faits = bm.Total AndAlso bm.Total > 0 AndAlso bm.Erreurs = 0
        Dim videosCompletes As Boolean = bv.Mesurees + bv.SansSon + bv.Erreurs = bv.Total AndAlso Not bv.FfmpegAbsent
        Dim message As String = bm.Message
        If bv.Total > 0 Then
            If message <> "" Then message &= vbCrLf & vbCrLf
            If bv.FfmpegAbsent Then
                message &= "Mesure du son des vidéos : l'outil ffmpeg n'a pas pu être installé (internet ?). Elle se fera au prochain démarrage."
            Else
                message &= "Son des vidéos : " & bv.Mesurees & " mesurées, " & bv.SansSon & " sans son" &
                           If(bv.Erreurs > 0, ", " & bv.Erreurs & " illisibles", "") &
                           If(videosCompletes, ".", " (" & (bv.Total - bv.Mesurees - bv.SansSon - bv.Erreurs) & " restantes, au prochain démarrage).")
            End If
        End If
        Dim probleme As Boolean = (bm.Faite AndAlso (bm.Faits < bm.Total OrElse bm.Erreurs > 0)) OrElse
                                  (bv.Total > 0 AndAlso Not videosCompletes)

        Dim fen As FormProgression = _fenetre
        Dim fin As Action =
            Sub()
                If bm.Faite Then NiveauSonore.OublierLesNiveauxDesMusiques()
                If musiqueComplete Then MemoriserEmpreinte(empreinte)
                Dim reduite As Boolean = fen IsNot Nothing AndAlso Not fen.IsDisposed AndAlso fen.Reduite
                If fen IsNot Nothing AndAlso Not fen.IsDisposed Then
                    fen.Avancer("Terminé", 1, 1)
                    fen.Terminer()
                End If
                _fenetre = Nothing
                _enCours = False
                ' Fenetre reduite : pas de message qui interromprait le diaporama, la pastille
                ' "✓ 100 %" suffit (sauf annulation ou erreurs).
                If message <> "" AndAlso (Not reduite OrElse probleme) Then
                    MsgBox(message, If(probleme, vbExclamation, vbInformation), "Volume des musiques et des vidéos")
                End If
            End Sub
        Try
            If fen IsNot Nothing AndAlso Not fen.IsDisposed AndAlso fen.IsHandleCreated Then
                fen.BeginInvoke(fin)
            Else
                _enCours = False
            End If
        Catch ex As Exception
            _enCours = False
        End Try
    End Sub

    Private Enum Resultat
        Modifie
        Inchange
        Echec
        Interrompu                  ' annule pendant l'analyse : morceau intact, non compte
    End Enum

    ''' <summary>
    ''' mp3gain sur un morceau. mp3gain est un ancien programme qui ne sait pas ouvrir un fichier
    ''' dont le nom contient des caractères hors de l'alphabet occidental (titres en hebreu de
    ''' QuiSlowORIENTAL : "echec ... code 0" a chaque fois). Ces morceaux sont traites sur une
    ''' COPIE temporaire au nom simple, recopiee a la place de l'original si tout s'est bien passe
    ''' (date de modification d'origine conservee). Original intact en cas d'echec ou d'annulation.
    ''' </summary>
    Private Function Normaliser(mp3gain As String, chemin As String, ByRef db As Double) As Resultat
        db = 0
        If Not chemin.Any(Function(c) AscW(c) > 255) Then Return NormaliserFichier(mp3gain, chemin, db)

        Dim copie As String = IO.Path.Combine(IO.Path.GetTempPath(), "slide_mp3gain_" & Guid.NewGuid().ToString("N") & ".mp3")
        Try
            Dim dateOrigine As Date = IO.File.GetLastWriteTime(chemin)
            IO.File.Copy(chemin, copie, True)
            Dim r As Resultat = NormaliserFichier(mp3gain, copie, db)
            If r = Resultat.Modifie OrElse r = Resultat.Inchange Then
                ' "Inchange" aussi : mp3gain a range son analyse dans les etiquettes de la copie
                ' (le morceau sera reconnu et vite passe la prochaine fois).
                IO.File.Copy(copie, chemin, True)
                IO.File.SetLastWriteTime(chemin, dateOrigine)
            End If
            Return r
        Catch ex As Exception
            DiagLog("Normalisation : " & chemin & " (copie temporaire : " & ex.Message & ")")
            Return Resultat.Echec
        Finally
            Try
                If IO.File.Exists(copie) Then IO.File.Delete(copie)
            Catch ex As Exception
            End Try
        End Try
    End Function

    ''' <summary>mp3gain sur un fichier. Il ecrit le nom du fichier, puis "Applying mp3 gain
    ''' change of N to ..." (N pas de 1,5 dB) ou "No changes to ... are necessary".</summary>
    Private Function NormaliserFichier(mp3gain As String, chemin As String, ByRef db As Double) As Resultat
        db = 0
        Dim infos As New ProcessStartInfo(mp3gain) With {
            .Arguments = "-r -k -c -q """ & chemin & """",
            .UseShellExecute = False, .CreateNoWindow = True,
            .RedirectStandardOutput = True, .RedirectStandardError = True,
            .WorkingDirectory = IO.Path.GetDirectoryName(mp3gain)}
        Try
            Using p As Process = Process.Start(infos)
                Try
                    p.PriorityClass = ProcessPriorityClass.BelowNormal   ' le diaporama passe d'abord
                Catch ex As Exception
                End Try
                Dim erreurs As Threading.Tasks.Task(Of String) = p.StandardError.ReadToEndAsync()
                ' Sortie lue caractere par caractere : on sait des le premier octet que mp3gain a
                ' fini d'analyser.
                Dim recu As Boolean = False
                Dim lecture As Threading.Tasks.Task(Of String) = Threading.Tasks.Task.Run(
                    Function()
                        Dim texte As New Text.StringBuilder()
                        Dim tampon(255) As Char
                        Dim n As Integer = p.StandardOutput.Read(tampon, 0, tampon.Length)
                        Do While n > 0
                            recu = True
                            texte.Append(tampon, 0, n)
                            n = p.StandardOutput.Read(tampon, 0, tampon.Length)
                        Loop
                        Return texte.ToString()
                    End Function)
                ' Annuler : mp3gain n'ecrit rien pendant l'analyse (quelques secondes) et sa sortie
                ' n'arrive qu'au moment d'ecrire dans le fichier (mesure du 01/10 : 2,3 s d'analyse
                ' sans une ligne, puis nom + "Applying..." juste avant l'ecriture). Tant que rien
                ' n'est sorti, on peut donc l'arreter sans risque pour le morceau.
                Do Until p.WaitForExit(50)
                    If Annule() AndAlso Not recu Then
                        Try
                            p.Kill()
                            p.WaitForExit()
                            Return Resultat.Interrompu
                        Catch ex As Exception
                        End Try
                    End If
                Loop
                p.WaitForExit()
                Dim sortie As String = lecture.Result
                Dim texteErreur As String = erreurs.Result
                Dim m As System.Text.RegularExpressions.Match =
                    System.Text.RegularExpressions.Regex.Match(sortie, "Applying mp3 gain change of (-?\d+)")
                If m.Success Then
                    db = CInt(m.Groups(1).Value) * 1.5
                    Return If(db = 0, Resultat.Inchange, Resultat.Modifie)
                End If
                If sortie.IndexOf("No changes to", StringComparison.OrdinalIgnoreCase) >= 0 Then Return Resultat.Inchange
                DiagLog("Normalisation : echec sur " & chemin & " (code " & p.ExitCode & ") " & texteErreur.Trim())
                Return Resultat.Echec
            End Using
        Catch ex As Exception
            DiagLog("Normalisation : " & chemin & " (" & ex.Message & ")")
            Return Resultat.Echec
        End Try
    End Function

    ''' <summary>Vrai si le morceau est joue par Slide ou ouvert par un autre programme.</summary>
    Private Function EstOccupe(chemin As String) As Boolean
        If String.Equals(MusiqueFichierEnCours(), chemin, StringComparison.OrdinalIgnoreCase) Then Return True
        Try
            Using fs As New IO.FileStream(chemin, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None)
            End Using
            Return False
        Catch ex As IO.FileNotFoundException
            Return False                                ' disparu : mp3gain le signalera
        Catch ex As IO.DirectoryNotFoundException
            Return False
        Catch ex As IO.IOException
            Return True                                 ' ouvert ailleurs
        Catch ex As Exception
            Return False                                ' autre souci : mp3gain le dira
        End Try
    End Function

    ''' <summary>mp3gain ecrit dans le fichier : seuls les MP3 traites perdent leur "lecture seule".</summary>
    Private Sub RetirerLectureSeule(chemin As String)
        Try
            Dim a As IO.FileAttributes = IO.File.GetAttributes(chemin)
            If (a And IO.FileAttributes.ReadOnly) <> 0 Then IO.File.SetAttributes(chemin, a And Not IO.FileAttributes.ReadOnly)
        Catch ex As Exception
        End Try
    End Sub

End Module
