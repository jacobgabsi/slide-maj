Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO

''' <summary>
''' Affichage automatique du dernier enregistrement Blink (Jacob, 27/10/2026 : « je voudrais que le dernier enregistrement, peu importe la camera, s'affiche quand il est nouveau »).
''' Toutes les N secondes (30 a 60), la liste des clips du stockage cloud est relue (BlinkApi.ListerLesClipsDepuisAsync) ; un clip plus recent que le dernier deja vu est telecharge en entier
''' puis joue comme un film (PlayFILM_VLC) : en PIP (coin, taille, volume au choix) ou en plein ecran. Un seul clip a la fois ; s'il en arrive plusieurs pendant ce temps, seul le plus recent
''' est joue ensuite. Au premier passage, le dernier clip existant sert de reference : les anciens ne sont jamais rejoues. Reglages propres au poste : data\settings\BlinkClips.ini.
''' </summary>
Friend Module BlinkClips

    Friend Class ReglagesClips
        Friend Actif As Boolean
        Friend Intervalle As Integer = 45
        Friend PleinEcran As Boolean
        Friend Coin As String = "Bdroite"
        Friend Taille As Integer = 30
        Friend Volume As Integer
        ''' <summary>Place libre choisie a la souris (proportions de l'image de Slide) ; negatif = le PIP reste dans son coin.</summary>
        Friend PosX As Double = -1
        Friend PosY As Double = -1
        ''' <summary>Noms des cameras a afficher (seules les cochees). Tant que <see cref="CamerasChoisies"/> est faux (jamais enregistre depuis la liste cochable), liste vide = toutes.</summary>
        Friend Cameras As New List(Of String)
        Friend CamerasChoisies As Boolean
    End Class

    Private _reglages As ReglagesClips
    Private _derniere As String = ""                    ' date de creation (texte ISO UTC de Blink) du dernier clip deja vu
    Private _demarre As Boolean
    Private ReadOnly _file As New Queue(Of BlinkApi.ClipBlink)          ' (clips a jouer, du plus ancien au plus recent : ceux de la meme minute s'enchainent)
    Private _reglagesDeLaFile As ReglagesClips                         ' (essai de la fenetre : ses reglages valent pour tout le lot)
    Private _essais As Integer
    Private _enCours As Boolean
    Private _enPip As Boolean
    Private _fichierEnCours As String = ""
    Private _lancementLe As Long
    Private _vuDansLePip As Boolean
    Private _fini As Boolean
    Private _titreEnCours As String = ""
    Private _legendeMontree As Boolean
    Private _lotPleinEcran As Boolean            ' (le clip precedent du lot est en plein ecran et n'a pas rendu la main)
    Private _idEnCours As String = ""
    ' Clip « plein ecran » pendant un film, une chaine TV, IPTV ou SAT en plein ecran : le clip s'affiche par-dessus (PIP agrandi a l'image) ; un film est mis en pause puis repris, une chaine continue derriere.
    Private _surcouche As Boolean
    Private _filmEnPauseParClip As Boolean
    Private _sauveDiffusion As Diffu
    Private _sauveTask As Boolean
    Private ReadOnly _telechargements As New Collections.Concurrent.ConcurrentDictionary(Of String, Task(Of Boolean))
    Private _legende As FormLegendeClip          ' (bandeau affiche pendant tout le clip, ferme a sa fin)

    Private Function FichierIni() As String
        Return DossierReglages & "\BlinkClips.ini"
    End Function

    Friend Function Reglages() As ReglagesClips
        If _reglages IsNot Nothing Then Return _reglages
        Dim r As New ReglagesClips
        Try
            If File.Exists(FichierIni()) Then
                For Each ligne As String In File.ReadAllLines(FichierIni())
                    Dim p As Integer = ligne.IndexOf("="c)
                    If p <= 0 Then Continue For
                    Dim cle As String = ligne.Substring(0, p).Trim()
                    Dim valeur As String = ligne.Substring(p + 1).Trim()
                    Dim n As Integer
                    Select Case cle
                        Case "Actif" : r.Actif = (valeur = "1")
                        Case "Intervalle" : If Integer.TryParse(valeur, n) Then r.Intervalle = Math.Max(15, Math.Min(600, n))
                        Case "PleinEcran" : r.PleinEcran = (valeur = "1")
                        Case "Coin" : If valeur <> "" Then r.Coin = valeur
                        Case "Taille" : If Integer.TryParse(valeur, n) Then r.Taille = Math.Max(5, Math.Min(100, n))
                        Case "Volume" : If Integer.TryParse(valeur, n) Then r.Volume = Math.Max(0, Math.Min(100, n))
                        Case "PosX" : Double.TryParse(valeur, NumberStyles.Float, CultureInfo.InvariantCulture, r.PosX)
                        Case "PosY" : Double.TryParse(valeur, NumberStyles.Float, CultureInfo.InvariantCulture, r.PosY)
                        Case "Cameras" : r.Cameras = valeur.Split("|"c).Where(Function(c) c <> "").ToList()
                        Case "CamerasChoisies" : r.CamerasChoisies = (valeur = "1")
                        Case "Derniere" : _derniere = valeur
                    End Select
                Next
            End If
        Catch ex As Exception
            DiagLog("Blink : lecture de BlinkClips.ini impossible : " & ex.Message)
        End Try
        _reglages = r
        Return r
    End Function

    ''' <summary>Les reglages de l'affichage automatique pour l'export des parametres (ni le repere du dernier clip vu, ni le compte Blink : propres au poste).</summary>
    Friend Function ExporterLesReglages() As Newtonsoft.Json.Linq.JObject
        Dim r As ReglagesClips = Reglages()
        Return New Newtonsoft.Json.Linq.JObject From {{"Actif", r.Actif}, {"Intervalle", r.Intervalle}, {"PleinEcran", r.PleinEcran}, {"Coin", r.Coin}, {"Taille", r.Taille}, {"Volume", r.Volume},
                                                      {"PosX", r.PosX}, {"PosY", r.PosY}, {"Cameras", New Newtonsoft.Json.Linq.JArray(r.Cameras)}, {"CamerasChoisies", r.CamerasChoisies}}
    End Function

    ''' <summary>Reprend ces reglages (import des parametres) : seuls ceux presents dans <paramref name="valeurs"/> changent. Activer l'affichage reprend une reference neuve (jamais de vieux clips rejoues).</summary>
    Friend Sub ImporterLesReglages(valeurs As Newtonsoft.Json.Linq.JObject)
        Dim r As ReglagesClips = Reglages()
        Dim jeton As Newtonsoft.Json.Linq.JToken = Nothing
        If valeurs.TryGetValue("Actif", jeton) Then
            Dim actif As Boolean = CBool(jeton)
            If actif AndAlso Not r.Actif Then _derniere = ""
            r.Actif = actif
        End If
        If valeurs.TryGetValue("Intervalle", jeton) Then r.Intervalle = Math.Max(15, Math.Min(600, CInt(jeton)))
        If valeurs.TryGetValue("PleinEcran", jeton) Then r.PleinEcran = CBool(jeton)
        If valeurs.TryGetValue("Coin", jeton) AndAlso CStr(jeton) <> "" Then r.Coin = CStr(jeton)
        If valeurs.TryGetValue("Taille", jeton) Then r.Taille = Math.Max(5, Math.Min(100, CInt(jeton)))
        If valeurs.TryGetValue("Volume", jeton) Then r.Volume = Math.Max(0, Math.Min(100, CInt(jeton)))
        If valeurs.TryGetValue("PosX", jeton) Then r.PosX = CDbl(jeton)
        If valeurs.TryGetValue("PosY", jeton) Then r.PosY = CDbl(jeton)
        If valeurs.TryGetValue("Cameras", jeton) Then r.Cameras = CType(jeton, Newtonsoft.Json.Linq.JArray).Select(Function(c) CStr(c)).Where(Function(c) c <> "").ToList()
        If valeurs.TryGetValue("CamerasChoisies", jeton) Then r.CamerasChoisies = CBool(jeton)
        Sauver()
        DiagLog("Blink : reglages de l'affichage automatique repris d'un import")
    End Sub

    Private Sub Sauver()
        Try
            Dim r As ReglagesClips = Reglages()
            File.WriteAllLines(FichierIni(), {"Actif=" & If(r.Actif, "1", "0"), "Intervalle=" & r.Intervalle, "PleinEcran=" & If(r.PleinEcran, "1", "0"),
                                              "Coin=" & r.Coin, "Taille=" & r.Taille, "Volume=" & r.Volume, "Cameras=" & String.Join("|", r.Cameras), "CamerasChoisies=" & If(r.CamerasChoisies, "1", "0"), "Derniere=" & _derniere,
                                              "PosX=" & r.PosX.ToString("0.000", CultureInfo.InvariantCulture), "PosY=" & r.PosY.ToString("0.000", CultureInfo.InvariantCulture)})
        Catch ex As Exception
            DiagLog("Blink : ecriture de BlinkClips.ini impossible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Taille du PIP changee a la souris (Form12) : elle devient celle des reglages de l'onglet Blink.</summary>
    Friend Sub RetenirLaTaille(pourcent As Integer)
        Reglages().Taille = Math.Max(5, Math.Min(100, pourcent))
        Sauver()
        DiagLog("Blink : taille du PIP des clips retenue : " & Reglages().Taille & " %")
    End Sub

    ''' <summary>Place du PIP choisie a la souris (negative : retour au coin).</summary>
    Friend Sub RetenirLaPosition(fx As Double, fy As Double)
        Dim r As ReglagesClips = Reglages()
        r.PosX = If(fx < 0 OrElse fy < 0, -1, fx)
        r.PosY = If(fx < 0 OrElse fy < 0, -1, fy)
        Sauver()
        DiagLog("Blink : place du PIP des clips retenue : " & If(r.PosX < 0, "coin", r.PosX.ToString("0.00", CultureInfo.InvariantCulture) & " / " & r.PosY.ToString("0.00", CultureInfo.InvariantCulture)))
    End Sub

    ''' <summary>Enregistre les reglages de la fenetre. Activer l'affichage reprend une reference neuve : seuls les clips qui arriveront ensuite s'afficheront.</summary>
    Friend Sub Enregistrer(nouveaux As ReglagesClips)
        Dim avant As ReglagesClips = Reglages()
        If nouveaux.Actif AndAlso Not avant.Actif Then _derniere = ""
        If Not String.Equals(nouveaux.Coin, avant.Coin, StringComparison.OrdinalIgnoreCase) Then nouveaux.PosX = -1 : nouveaux.PosY = -1          ' (un autre coin choisi : la place libre est oubliee)
        _reglages = nouveaux
        Sauver()
    End Sub

    Friend Sub Demarrer()
        If _demarre Then Exit Sub
        _demarre = True
        Try
            Dim vieux As String = Path.Combine(Path.GetTempPath(), "SlideBlink")
            ' Clips d'une session precedente : seulement les fichiers de plus d'une heure. Tout effacer coupait le clip qu'un autre Slide (ancienne version qui se ferme apres une mise a jour) etait en train de lire : « Film introuvable ».
            If Directory.Exists(vieux) Then
                For Each f As String In Directory.GetFiles(vieux)
                    Try
                        If (Date.UtcNow - File.GetLastWriteTimeUtc(f)).TotalHours > 1 Then File.Delete(f)
                    Catch ex2 As Exception
                    End Try
                Next
            End If
        Catch ex As Exception
        End Try
        Task.Run(AddressOf Boucle)
    End Sub

    Private _derniereRaison As String = Nothing

    Private Async Function Boucle() As Task
        Dim demarreLe As Long = Environment.TickCount64          ' (la recherche des nouveaux clips attend 30 s que Slide ait demarre ; la boucle, elle, suit tout de suite un clip lance a la main)
        Do
            Dim attente As Integer = 45
            Try
                Dim r As ReglagesClips = Reglages()
                attente = r.Intervalle
                ' Pourquoi rien ne se passe (27/10/2026, "ne marche pas sur l'autre PC") : une ligne au journal a chaque changement de situation.
                Dim raison As String = If(Not r.Actif, "affichage automatique non active sur ce poste (reglage par poste : bouton Affichage auto...)",
                                          If(Not BlinkApi.EstConnecte, "compte Blink non connecte sur ce poste (connexion par poste : onglet Camera, cadre Blink)",
                                             If(SilenceDesReglages.Actif, "reglages ouverts (mis en attente)", "")))
                If raison <> _derniereRaison Then
                    _derniereRaison = raison
                    DiagLog("Blink : clips automatiques : " & If(raison = "", "en veille, recherche toutes les " & r.Intervalle & " s", raison))
                End If
                If _enCours Then
                    VerifierLaFin()
                    attente = 1
                ElseIf _file.Count > 0 AndAlso BlinkApi.EstConnecte AndAlso (Not SilenceDesReglages.Actif OrElse _reglagesDeLaFile IsNot Nothing) Then          ' (lot de l'essai : il continue meme reglages ouverts)
                    Await Jouer(If(_reglagesDeLaFile, r))          ' (clip suivant du lot, ou clip pas encore telechargeable)
                ElseIf r.Actif AndAlso BlinkApi.EstConnecte AndAlso Not SilenceDesReglages.Actif AndAlso Environment.TickCount64 - demarreLe > 30000 Then
                    Await ChercherLesNouveaux(r)
                    If _file.Count > 0 Then Await Jouer(r)
                End If
            Catch ex As Exception
                DiagLog("Blink : clips automatiques : " & ex.Message)
            End Try
            ' Par tranches de 0,25 s : un clip lance a la main (essai, minuterie) est suivi sans attendre la fin de l'intervalle, et la fin d'un clip est vue presque tout de suite.
            For tranche As Integer = 1 To If(_enCours, 1, attente * 4)
                Await Task.Delay(250)
                If _enCours AndAlso attente > 1 Then Exit For
            Next
        Loop
    End Function

    Private Function UtcDe(texte As String) As Date
        Dim d As DateTimeOffset
        If DateTimeOffset.TryParse(texte, CultureInfo.InvariantCulture, DateTimeStyles.None, d) Then Return d.UtcDateTime
        Return Date.UtcNow.AddHours(-1)
    End Function

    Private Function CameraVoulue(r As ReglagesClips, nom As String) As Boolean
        If Not r.CamerasChoisies AndAlso r.Cameras.Count = 0 Then Return True          ' (jamais choisi : toutes, comme avant)
        Return r.Cameras.Any(Function(c) String.Equals(c, nom, StringComparison.OrdinalIgnoreCase))
    End Function

    Private Async Function ChercherLesNouveaux(r As ReglagesClips) As Task
        Dim depuis As Date = If(_derniere = "", Date.UtcNow.AddHours(-2), UtcDe(_derniere).AddMinutes(-1))
        Dim liste As List(Of BlinkApi.ClipBlink) = Await BlinkApi.ListerLesClipsDepuisAsync(depuis, 1, False)
        If _derniere = "" Then
            ' Premier passage : reference = dernier clip existant (ou l'instant present), rien n'est affiche.
            _derniere = If(liste.Count > 0, liste(0).CreeLe, Date.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'+00:00'", CultureInfo.InvariantCulture))
            Sauver()
            DiagLog("Blink : clips automatiques : reference " & _derniere & " (les clips anterieurs ne s'affichent pas)")
            Return
        End If
        If liste.Count = 0 Then Return
        Dim nouveaux As List(Of BlinkApi.ClipBlink) = liste.Where(Function(c) String.CompareOrdinal(c.CreeLe, _derniere) > 0 AndAlso CameraVoulue(r, c.Camera)).ToList()
        If String.CompareOrdinal(liste(0).CreeLe, _derniere) > 0 Then
            _derniere = liste(0).CreeLe          ' (meme un clip d'une camera non voulue compte comme vu)
            Sauver()
        End If
        ' Un clip trop vieux n'est plus d'actualite (27/10/2026, Jacob : au demarrage ou a la fermeture des Options, un clip de 31 minutes s'affichait) : il est ignore.
        ' Age maximum : 2 minutes (Jacob, 27/10/2026), ou deux intervalles de verification si l'intervalle est plus long que 60 s.
        Dim ageMax As Double = Math.Max(2.0, r.Intervalle * 2 / 60.0)
        Dim trop As Integer = nouveaux.RemoveAll(Function(c) (Date.UtcNow - UtcDe(c.CreeLe)).TotalMinutes > ageMax)
        If trop > 0 Then DiagLog("Blink : " & trop & " clip(s) de plus de " & Math.Round(ageMax, 1) & " minutes ignore(s) (trop anciens pour s'afficher)")
        If nouveaux.Count > 0 Then
            ' Le plus recent donne la minute ; tous les clips de cette minute (toutes cameras) s'enchainent, du plus ancien au plus recent. Les clips d'avant cette minute ne sont pas rejoues.
            Dim minute As String = nouveaux(0).CreeLe.Substring(0, Math.Min(16, nouveaux(0).CreeLe.Length))
            Dim lot As List(Of BlinkApi.ClipBlink) = nouveaux.Where(Function(c) c.CreeLe.StartsWith(minute, StringComparison.Ordinal)).Reverse().ToList()
            _file.Clear()
            For Each c As BlinkApi.ClipBlink In lot
                _file.Enqueue(c)
            Next
            _reglagesDeLaFile = Nothing
            _essais = 0
            DiagLog("Blink : " & lot.Count & " nouveau(x) clip(s) a " & minute & " : " & String.Join(", ", lot.Select(Function(c) c.Camera & " " & c.Id)) & If(nouveaux.Count > lot.Count, " ; " & (nouveaux.Count - lot.Count) & " plus ancien(s) ignore(s)", ""))
        End If
    End Function

    Private Function FichierDuClip(clip As BlinkApi.ClipBlink) As String
        Dim dossier As String = Path.Combine(Path.GetTempPath(), "SlideBlink")
        Directory.CreateDirectory(dossier)
        Return Path.Combine(dossier, "clip_" & clip.Id & ".mp4")
    End Function

    ''' <summary>Telechargement d'un clip, une seule fois meme demande deux fois (le suivant du lot se telecharge pendant que le courant joue : l'enchainement n'attend plus le reseau).</summary>
    Private Function Telecharger(clip As BlinkApi.ClipBlink) As Task(Of Boolean)
        Return _telechargements.GetOrAdd(clip.Id, Function(k As String) As Task(Of Boolean)
                                                      Return Task.Run(Async Function() As Task(Of Boolean)
                                                                          Dim fichier As String = FichierDuClip(clip)
                                                                          Dim ok As Boolean = Await BlinkApi.TelechargerLeClipAsync(clip, fichier & ".tmp")
                                                                          If ok Then
                                                                              If File.Exists(fichier) Then File.Delete(fichier)
                                                                              File.Move(fichier & ".tmp", fichier)
                                                                          Else
                                                                              Dim ignore As Task(Of Boolean) = Nothing
                                                                              _telechargements.TryRemove(clip.Id, ignore)          ' (un autre essai pourra recommencer)
                                                                          End If
                                                                          Return ok
                                                                      End Function)
                                                  End Function)
    End Function

    ''' <summary>Telecharge le clip en attente puis le lance. Rend un texte d'etat (pour l'essai de la fenetre).</summary>
    Private Async Function Jouer(r As ReglagesClips) As Task(Of String)
        If _file.Count = 0 Then Return "Aucun clip."
        Dim clip As BlinkApi.ClipBlink = _file.Peek()
        Dim dossier As String = Path.Combine(Path.GetTempPath(), "SlideBlink")
        Directory.CreateDirectory(dossier)
        Dim fichier As String = Path.Combine(dossier, "clip_" & clip.Id & ".mp4")
        Dim telecharge As Boolean = Await Telecharger(clip)
        If telecharge AndAlso Not File.Exists(fichier) Then
            ' Fichier disparu (efface par un autre Slide qui demarre, ou deja joue) : « Film introuvable » s'affichait a l'ecran (27/10/2026, au lancement). On le retelecharge.
            DiagLog("Blink : clip " & clip.Id & " : fichier temporaire disparu, nouveau telechargement")
            Dim oubli As Task(Of Boolean) = Nothing
            _telechargements.TryRemove(clip.Id, oubli)
            telecharge = Await Telecharger(clip)
            If telecharge AndAlso Not File.Exists(fichier) Then telecharge = False
        End If
        If Not telecharge Then
            _essais += 1
            If _essais >= 4 Then
                DiagLog("Blink : clip " & clip.Id & " abandonne apres 4 essais de telechargement")
                _file.Dequeue()
                If _file.Count = 0 AndAlso _lotPleinEcran Then
                    ' Le clip d'avant etait en plein ecran sans rendre la main : le diaporama repart.
                    _lotPleinEcran = False
                    Dim fp As Form = FenetrePrincipale()
                    If fp IsNot Nothing Then fp.BeginInvoke(New Action(Sub()
                                                                          EndAndContinue()
                                                                          RelancerCommeAuDemarrage()
                                                                      End Sub))
                End If
                If _file.Count = 0 Then _reglagesDeLaFile = Nothing
                _essais = 0
                Return "Téléchargement impossible (voir le journal)."
            End If
            Return "Clip pas encore disponible, nouvel essai au prochain passage."
        End If
        _idEnCours = clip.Id
        _file.Dequeue()
        _essais = 0
        Dim suite As Boolean = _file.Count > 0          ' (d'autres clips du lot suivent : le diaporama ne repart qu'apres le dernier)
        If Not suite Then _reglagesDeLaFile = Nothing
        If _derniere <> "" AndAlso String.CompareOrdinal(clip.CreeLe, _derniere) > 0 Then _derniere = clip.CreeLe : Sauver()          ' (clip montre, meme par l'essai : le suivant ne le rejoue pas)
        Dim f As Form = FenetrePrincipale()
        If f Is Nothing Then Return "Fenêtre de Slide introuvable."

        Dim titre As String = clip.Camera & " " & UtcDe(clip.CreeLe).ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)
        Dim pip As Boolean = Not r.PleinEcran
        Dim volume As Integer = r.Volume
        ' Que joue-t-il en plein ecran ? Le diaporama : le clip prend l'ecran comme avant. Un film, une chaine TV / IPTV / SAT : le clip se pose par-dessus, rien n'est arrete.
        Dim surcouche As Boolean = _surcouche
        Dim premiereSurcouche As Boolean = False
        Dim estFilm As Boolean = False
        If Not pip AndAlso Not surcouche Then
            Try
                Dim mode As String = Diffusion.Mode
                If Form3.VlcControl2.Visible AndAlso (mode = "FILM-VLC-INT" OrElse mode = "TV_VLC_INT" OrElse mode = "IPTV_VLC_INT" OrElse mode = "SAT-VLC_INT") Then
                    surcouche = True : premiereSurcouche = True : estFilm = (mode = "FILM-VLC-INT")
                End If
            Catch ex As Exception
            End Try
        End If
        If surcouche Then pip = True
        Dim argument As String = "!/Titre:" & titre.Replace("!", "") & "!/Source:BlinkClip" & If(surcouche, "!/PipPlein:TRUE", "")
        If pip Then argument &= "!/Pip:TRUE!/PipSize:" & (r.Taille / 100.0).ToString("0.##", CultureInfo.InvariantCulture) & "!/PipPosition:" & r.Coin
        If pip AndAlso r.PosX >= 0 AndAlso r.PosY >= 0 Then argument = ArgumentAvecPositionPip(argument, r.PosX, r.PosY)

        _enCours = True : _enPip = pip : _fini = False : _vuDansLePip = False
        If premiereSurcouche Then _surcouche = True : _filmEnPauseParClip = estFilm
        _titreEnCours = New String(titre.Where(Function(ch As Char) Not Char.IsSurrogate(ch)).ToArray()).Trim()          ' (le bandeau n'affiche pas les emojis)
        _legendeMontree = False
        _fichierEnCours = fichier : _lancementLe = Environment.TickCount64
        Dim nomCamera As String = New String(clip.Camera.Where(Function(ch As Char) Not Char.IsSurrogate(ch)).ToArray()).Trim()          ' (le message du bas : la camera seule, l'heure est en haut a gauche)
        ProgrammerLaLegende(nomCamera, pip, fichier,UtcDe(clip.CreeLe).ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture))
        For Each suivant As BlinkApi.ClipBlink In _file
            Telecharger(suivant)          ' (en avance, pendant que celui-ci joue)
        Next
        DiagLog("Blink : clip " & clip.Id & " (" & titre & ") lance " & If(pip, "en PIP " & r.Coin & " " & r.Taille & " %", "en plein ecran") & ", volume " & volume)
        f.BeginInvoke(New Action(Sub()
                                     Try
                                         If pip Then
                                             If premiereSurcouche Then
                                                 _sauveDiffusion = Diffusion
                                                 _sauveTask = TaskIsPlaying
                                                 If estFilm Then
                                                     FilmEnPause = True
                                                     Form3.VlcControl2.SetPause(True)          ' (le film attend la fin du clip)
                                                     Dim restantMinutes As Integer = 0
                                                     Try
                                                         Dim longueur As Long = Form3.VlcControl2.Length          ' (millisecondes ; 0 ou moins : duree inconnue)
                                                         If longueur > 0 Then restantMinutes = CInt(Math.Ceiling(Math.Max(0L, longueur - Form3.VlcControl2.Time) / 60000.0))
                                                     Catch ex As Exception
                                                     End Try
                                                     DebutDeCoupure(restantMinutes)
                                                 End If
                                             End If
                                             PlayFILM_VLC(fichier, "", "", volume, argument)
                                         Else
                                             ' Premier clip du lot : le diaporama s'efface ; les suivants prennent la place du precedent sans rendre la main (ClipBlinkEnChaine).
                                             If Not _lotPleinEcran Then
                                                 CouperLeSonPourLaTache("FILM " & fichier, volume)
                                                 EndAndContinue()
                                             End If
                                             ClipBlinkEnChaine = suite
                                             _lotPleinEcran = suite
                                             PlayFILM_VLC(fichier, "", "", volume, argument)
                                             ClipBlinkEnChaine = False
                                             If Not suite Then
                                                 LibererLeSonDeLaTache()
                                                 RelancerCommeAuDemarrage()          ' (comme la minuterie apres un film plein ecran : le diaporama repart, sinon l'ecran reste noir)
                                             End If
                                             _fini = True
                                         End If
                                     Catch ex As Exception
                                         DiagLog("Blink : lecture du clip impossible : " & ex.Message)
                                         ClipBlinkEnChaine = False : _lotPleinEcran = False
                                         _fini = True
                                     End Try
                                 End Sub))
        Return "Clip lancé : " & titre & "."
    End Function

    ''' <summary>Pose le bandeau (camera, heure) sur l'image du clip : des que son PIP est a l'ecran (30 s au plus), ou 1,5 s apres le lancement en plein ecran. Tache a part : ne depend pas de la boucle de surveillance.</summary>
    Private Sub ProgrammerLaLegende(texte As String, pip As Boolean, cible As String, heure As String)
        Task.Run(Async Function()
                     Try
                         Dim f As Form = FenetrePrincipale()
                         If f Is Nothing Then Exit Function
                         Dim zone As Rectangle = Rectangle.Empty
                         If pip Then
                             For essai As Integer = 1 To 60
                                 Await Task.Delay(500)
                                 zone = RectangleDuPipPour(cible)
                                 If Not zone.IsEmpty Then Exit For
                             Next
                             If zone.IsEmpty Then DiagLog("Blink : legende du clip : PIP introuvable") : Exit Function
                             Await Task.Delay(800)                  ' (le PIP vient d'etre place : la video s'y installe)
                         Else
                             Await Task.Delay(1500)
                         End If
                         f.BeginInvoke(New Action(Sub()
                                                      Try
                                                          If Not _enCours Then Exit Try          ' (clip deja fini)
                                                          Dim l As New FormLegendeClip()
                                                          _legende = l
                                                          l.Montrer(texte, If(pip, RectangleDuPipPour(cible), f.RectangleToScreen(f.ClientRectangle)), 900,
                                                                  Function() If(pip, RectangleDuPipPour(cible), f.RectangleToScreen(f.ClientRectangle)), heure)          ' (15 min au plus : la fin du clip le ferme avant)
                                                      Catch ex As Exception
                                                          DiagLog("Blink : legende du clip : " & ex.Message)
                                                      End Try
                                                  End Sub))
                     Catch ex As Exception
                         DiagLog("Blink : legende du clip : " & ex.Message)
                     End Try
                 End Function)
    End Sub

    ''' <summary>Le dernier clip du lot est fini : le film reprend (et les heures de la minuterie sont decalees du temps de pause), l'etat de la diffusion est remis comme avant le clip.</summary>
    Private Sub Reprendre()
        _surcouche = False
        Dim film As Boolean = _filmEnPauseParClip
        _filmEnPauseParClip = False
        Dim f As Form = FenetrePrincipale()
        If f Is Nothing Then Exit Sub
        f.BeginInvoke(New Action(Sub()
                                     Try
                                         Diffusion = _sauveDiffusion
                                         TaskIsPlaying = _sauveTask
                                         If film Then
                                             FilmEnPause = False
                                             Form3.VlcControl2.SetPause(False)
                                             FinDeCoupure()
                                         End If
                                     Catch ex As Exception
                                         DiagLog("Blink : reprise apres le clip impossible : " & ex.Message)
                                     End Try
                                 End Sub))
    End Sub

    ''' <summary>Le clip a-t-il fini d'etre joue ? Alors le fichier est supprime et le suivant peut partir.</summary>
    Private Sub VerifierLaFin()
        Dim fini As Boolean = _fini
        If _enPip Then
            Dim present As Boolean = PipEnCoursPour(_fichierEnCours)
            ' « Apparu » = la fenetre du PIP est posee a l'ecran (et non seulement sa session creee : sur l'autre PC le PIP restait « en cours » 15 min sans jamais s'afficher).
            If present AndAlso Not RectangleDuPipPour(_fichierEnCours).IsEmpty Then _vuDansLePip = True
            If _vuDansLePip AndAlso Not present Then fini = True
            If Not _vuDansLePip AndAlso Environment.TickCount64 - _lancementLe > 45000 Then
                fini = True
                DiagLog("Blink : le clip n'est jamais apparu dans un PIP (45 s) : PIP arrete, clip suivant")
                ArreterLePipPour(_fichierEnCours)
            End If
        End If
        If Environment.TickCount64 - _lancementLe > 15 * 60 * 1000 Then fini = True            ' (garde-fou : un clip dure 1 ou 2 minutes)
        If Not fini Then Exit Sub
        _enCours = False
        DiagLog("Blink : clip termine")
        If _surcouche AndAlso _file.Count = 0 Then Reprendre()
        Try
            Dim l As FormLegendeClip = _legende
            _legende = Nothing
            Dim f As Form = FenetrePrincipale()
            If l IsNot Nothing AndAlso f IsNot Nothing Then f.BeginInvoke(New Action(Sub()
                                                                                         Try
                                                                                             If Not l.IsDisposed Then l.Close()
                                                                                         Catch ex As Exception
                                                                                         End Try
                                                                                     End Sub))
        Catch ex As Exception
        End Try
        Try
            Dim oubli As Task(Of Boolean) = Nothing
            _telechargements.TryRemove(_idEnCours, oubli)          ' (rejoue plus tard : il se telechargera de nouveau)
            File.Delete(_fichierEnCours)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Minuterie (« Blink clips ON / OFF ») : met l'affichage automatique en marche ou a l'arret, comme la case de l'onglet Blink. En marche, la reference est reprise : seuls les clips a venir s'affichent.</summary>
    Friend Sub ActiverAffichageAuto(actif As Boolean)
        Dim r As ReglagesClips = Reglages()
        If r.Actif = actif Then Exit Sub
        If actif Then _derniere = ""
        r.Actif = actif
        Sauver()
        DiagLog("Blink : affichage automatique des clips " & If(actif, "mis en marche", "arrete") & " par la minuterie")
    End Sub

    ''' <summary>Minuterie (« Blink dernier clip ») : affiche tout de suite le dernier clip, avec les reglages enregistres (volume : celui de la colonne Volume s'il est donne, sinon celui des reglages).</summary>
    Friend Async Sub AfficherLeDernier(volume As Integer)
        Try
            Dim r As ReglagesClips = Reglages()
            Dim voulu As New ReglagesClips With {.Actif = r.Actif, .Intervalle = r.Intervalle, .PleinEcran = r.PleinEcran, .Coin = r.Coin, .Taille = r.Taille, .Cameras = r.Cameras, .CamerasChoisies = r.CamerasChoisies,
                                                  .Volume = If(volume >= 0, Math.Min(100, volume), r.Volume)}
            DiagLog("Blink : dernier clip demande par la minuterie : " & Await EssayerLeDernier(voulu))
        Catch ex As Exception
            DiagLog("Blink : dernier clip impossible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Essai de la fenetre de reglages : joue tout de suite le dernier clip existant (toutes cameras), avec les reglages donnes.</summary>
    Friend Async Function EssayerLeDernier(r As ReglagesClips) As Task(Of String)
        If _enCours Then Return "Un clip est déjà en cours d'affichage."
        If Not BlinkApi.EstConnecte Then Return "Pas encore connecté."
        Dim liste As List(Of BlinkApi.ClipBlink) = Await BlinkApi.ListerLesClipsDepuisAsync(Date.UtcNow.AddDays(-7), 1, True)
        If liste.Count = 0 Then Return "Aucun enregistrement dans les 7 derniers jours."
        ' Meme regle que l'automatique : les clips de la minute du plus recent, enchaines.
        Dim minute As String = liste(0).CreeLe.Substring(0, Math.Min(16, liste(0).CreeLe.Length))
        _file.Clear()
        For Each c As BlinkApi.ClipBlink In liste.Where(Function(x) x.CreeLe.StartsWith(minute, StringComparison.Ordinal)).Reverse()
            _file.Enqueue(c)
        Next
        _reglagesDeLaFile = r
        _essais = 0
        Return Await Jouer(r)
    End Function

End Module
