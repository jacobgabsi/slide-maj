Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' La musique du diaporama, jouee par VLC (Form3.VlcMusique) au lieu du lecteur Windows Media.
'''
''' Pourquoi : le controle ActiveX de Windows Media Player n'est plus developpe (sa documentation
''' est archivee chez Microsoft) et il fait partie des "fonctionnalites multimedias" de Windows,
''' absentes des editions N ou desactivables : sur un PC neuf, Slide ne demarrait pas. VLC, lui,
''' est livre avec le programme (dossier data\VLC).
'''
''' Le comportement d'origine est conserve : liste de morceaux, lecture aleatoire, en boucle,
''' avec pause/reprise, plus la radio sur Internet.
''' </summary>
Module MusiqueVLC

    ' Liste des morceaux (chemins complets) et ordre de lecture tire au hasard.
    Private ReadOnly _morceaux As New List(Of String)
    Private ReadOnly _ordre As New List(Of Integer)
    Private _position As Integer = -1

    Private _radio As Boolean = False
    Private _url As String = ""
    Private _enLecture As Boolean = False          ' l'utilisateur veut que la musique joue
    Private _volume As Integer = 100
    Private _octetsLus As Long = -1                ' pour voir si la radio recoit quelque chose
    Private _battements As Integer = 0             ' pour la trace d'etat periodique

    ' Surveille la fin d'un morceau : on enchaine ici, et non dans l'evenement de VLC, qui arrive
    ' depuis un autre fil d'execution et ou il est interdit de relancer la lecture.
    Private WithEvents TimerSuivant As New Timer With {.Interval = 500}

    ' Passage de VLC (mp3, radio) a Deezer : Deezer met de 15 s a plus d'une minute a jouer (chargement de la page, du
    ' Flow). L'ancienne source CONTINUE jusqu'a ce que Deezer joue vraiment, puis elle se tait (15/10/2026 : "pas de
    ' musique, le son est revenu beaucoup plus tard" : 68 s de silence apres N).
    Private WithEvents TimerBasculeDeezer As New Timer With {.Interval = 1000}
    Private _basculeDeezerDepuis As Long

    Private Sub TimerBasculeDeezer_Tick(sender As Object, e As EventArgs) Handles TimerBasculeDeezer.Tick
        Try
            Dim abandon As Boolean = Not MusiqueDeezer.Actif                                 ' la source a encore change
            Dim joue As Boolean = MusiqueDeezer.EnLecture
            Dim trop_long As Boolean = Environment.TickCount64 - _basculeDeezerDepuis > 180000L
            If abandon Then
                TimerBasculeDeezer.Stop()
            ElseIf joue OrElse trop_long Then
                TimerBasculeDeezer.Stop()
                TimerSuivant.Stop()
                If Lecteur.IsPlaying Then Lecteur.Pause()
                DiagLog("Musique : Deezer joue, l'ancienne source se tait" & If(joue, "", " (delai de 3 mn depasse)"))
            End If
        Catch ex As Exception
            TimerBasculeDeezer.Stop()
        End Try
    End Sub

    ''' <summary>Le lecteur VLC reserve a la musique (invisible, sur Form3).</summary>
    Private ReadOnly Property Lecteur As Vlc.DotNet.Forms.VlcControl
        Get
            Return Form3.VlcMusique
        End Get
    End Property

    ' ------------------------------------------------------------------------------------------
    ' La liste de lecture
    ' ------------------------------------------------------------------------------------------

    Public Sub MusiqueVider()
        _morceaux.Clear()
        _ordre.Clear()
        _position = -1
    End Sub

    Public Sub MusiqueAjouter(fichier As String)
        If fichier Is Nothing OrElse fichier.Trim() = "" Then Exit Sub
        _morceaux.Add(fichier)
    End Sub

    Public Function MusiqueNombre() As Integer
        Return _morceaux.Count
    End Function

    ''' <summary>Le morceau nÂ° i de la liste (pour l'enregistrement de la liste).</summary>
    Public Function MusiqueFichier(i As Integer) As String
        If i < 0 OrElse i >= _morceaux.Count Then Return ""
        Return _morceaux(i)
    End Function

    ''' <summary>A appeler quand la liste est complete : tire l'ordre de lecture au hasard.</summary>
    Public Sub MusiqueMelanger()
        _ordre.Clear()
        _ordre.AddRange(Enumerable.Range(0, _morceaux.Count))
        For i As Integer = _ordre.Count - 1 To 1 Step -1
            Dim j As Integer = Generator.Next(0, i + 1)
            Dim t As Integer = _ordre(i) : _ordre(i) = _ordre(j) : _ordre(j) = t
        Next
        _position = -1
        DiagLog("Musique : liste de " & _morceaux.Count & " morceau(x), ordre tire au hasard")
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Lecture
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Joue la musique : reprend le morceau en cours, ou passe au suivant s'il n'y en a
    ''' pas. Sans effet s'il n'y a ni morceau ni radio.</summary>
    Public Sub MusiqueJouer()
        If ConfierAuFilPrincipal(Sub() MusiqueJouer(), "MusiqueJouer") Then Exit Sub
        If RetenuePourUneTache("lecture") Then Exit Sub
        ' Source Deezer, joue dans Slide (MusiqueDeezer) : c'est lui qui joue, pas VLC.
        If MusiqueDeezer.Actif Then
            _enLecture = True
            Try
                If Lecteur.IsPlaying AndAlso Not MusiqueDeezer.EnLecture Then
                    ' Deezer n'est pas encore a l'ecoute : la radio ou le morceau d'avant continue le temps qu'il demarre.
                    _basculeDeezerDepuis = Environment.TickCount64
                    TimerBasculeDeezer.Start()
                Else
                    TimerSuivant.Stop()
                    If Lecteur.IsPlaying Then Lecteur.Pause()       ' (radio ou morceau d'avant le changement de source)
                End If
            Catch ex As Exception
            End Try
            MusiqueDeezer.Jouer()
            Exit Sub
        End If
        _enLecture = True
        DiagLog("Musique : demande de lecture (radio=" & _radio & ", " & _morceaux.Count & " morceaux)")
        TimerCarte.Start()
        Try
            If _radio Then
                ' (En cours de demarrage : ne pas la relancer, elle repartirait de zero.)
                If Lecteur.IsPlaying OrElse TimerRadio.Enabled Then Exit Sub
                MusiqueJouerRadio(_url)
                Exit Sub
            End If
            If _morceaux.Count = 0 Then DiagLog("Musique : rien a jouer, la liste est vide") : Exit Sub
            If Lecteur.GetCurrentMedia() Is Nothing Then
                MusiqueSuivant()
            ElseIf Not Lecteur.IsPlaying Then
                Lecteur.Play()
                AppliquerVolume()
            End If
            TimerSuivant.Start()
        Catch ex As Exception
            DiagLog("Musique : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Une tache de la minuterie est en cours de lancement et a coupe le son (voir
    ''' CouperLeSonPourLaTache, Module2) : la musique ne repart pas avant que le lecteur de la
    ''' tache ait son volume. Elle est redemandee ensuite comme d'habitude (photo suivante, fin
    ''' de la tache).</summary>
    Private Function RetenuePourUneTache(quoi As String) As Boolean
        If Not SonRetenuPourUneTache() Then Return False
        DiagLog("Musique : " & quoi & " retenue (tache de la minuterie en cours de lancement)")
        Return True
    End Function

    Public Sub MusiquePause()
        ' Lecteur sur Form3 : appele depuis le thread d'un PIP, on le confie a l'interface.
        If ConfierAuFilPrincipal(Sub() MusiquePause(), "MusiquePause") Then Exit Sub
        MusiqueDeezer.Pause()                   ' (sans effet si le Deezer integre n'est pas ouvert)
        _enLecture = False
        CarteDeezer.Retirer()                   ' la pochette quitte l'image tout de suite
        Try
            TimerSuivant.Stop()
            If Lecteur.IsPlaying Then Lecteur.Pause()
        Catch ex As Exception
            DiagLog("Musique (pause) : " & ex.Message)
        End Try
    End Sub

    Public Sub MusiqueArreter()
        If ConfierAuFilPrincipal(Sub() MusiqueArreter(), "MusiqueArreter") Then Exit Sub
        MusiqueDeezer.Pause()
        _enLecture = False
        CarteDeezer.Retirer()
        Try
            TimerSuivant.Stop()
            TimerRadio.Stop()
            Lecteur.Stop()
        Catch ex As Exception
            DiagLog("Musique (arret) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Morceau suivant dans l'ordre tire au hasard ; on reboucle a la fin de la liste.</summary>
    Public Sub MusiqueSuivant()
        If ConfierAuFilPrincipal(Sub() MusiqueSuivant(), "MusiqueSuivant") Then Exit Sub
        If RetenuePourUneTache("morceau suivant") Then Exit Sub
        ' Deezer : la musique REPREND ou elle en etait (appele a la fin de chaque tache : passer au
        ' morceau suivant a chaque fois userait les sauts limites d'un compte gratuit). Le morceau
        ' suivant se demande par la commande "Deezer.Next" ou la tache "Deezer Next Song".
        If MusiqueDeezer.Actif Then
            MusiqueJouer()
            Exit Sub
        End If
        If _morceaux.Count = 0 Then Exit Sub
        If _ordre.Count <> _morceaux.Count Then MusiqueMelanger()

        _position += 1
        If _position >= _ordre.Count Then MusiqueMelanger() : _position = 0     ' boucle, nouvel ordre

        Dim fichier As String = _morceaux(_ordre(_position))
        _radio = False
        _url = fichier
        _enLecture = True
        TimerCarte.Start()
        Try
            PreparerLeVolumeAvantChangement(Lecteur, VolumePourVlc())
            Lecteur.SetMedia(New Uri("file:///" & fichier.Replace("\", "/")))
            CouperLeSonAvantDemarrage(Lecteur)
            Lecteur.Play()
            AttendreLeDemarrage()
            VolumeApresDemarrage()
            TimerSuivant.Start()
            DiagLog("Musique : lecture de " & IO.Path.GetFileName(fichier) & " (joue=" & Lecteur.IsPlaying &
                    ", etat=" & Lecteur.State.ToString() & ")")
        Catch ex As Exception
            DiagLog("Musique (" & fichier & ") : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Passe au morceau suivant quand le precedent est fini (remplace le mode "boucle"
    ''' du lecteur Windows Media).</summary>
    Private Sub TimerSuivant_Tick(sender As Object, e As EventArgs) Handles TimerSuivant.Tick
        ' Trace de diagnostic toutes les 10 s : la musique joue-t-elle encore, et a quel volume ?
        _battements += 1
        If _battements Mod 20 = 0 Then
            Try
                DiagLog("Musique (etat) : joue=" & Lecteur.IsPlaying & " etat=" & Lecteur.State.ToString() &
                        " volume=" & Lecteur.Audio.Volume & " muet=" & Lecteur.Audio.IsMute &
                        " position=" & CInt(Lecteur.Time / 1000) & "s enLecture=" & _enLecture)
            Catch ex As Exception
                DiagLog("Musique (etat) : " & ex.Message)
            End Try
        End If

        If Not _enLecture OrElse _radio Then Exit Sub
        Try
            If Lecteur.GetCurrentMedia() IsNot Nothing AndAlso Not Lecteur.IsPlaying AndAlso
               Lecteur.State = Vlc.DotNet.Core.Interops.Signatures.MediaStates.Ended Then
                ' L'alternance des sources attendait la fin de ce morceau : la source suivante prend le relais.
                If AlternanceMusique.ChangerALaFinDuMorceau() Then Exit Sub
                MusiqueSuivant()
            End If
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Pochette et titre sur l'image de Slide, comme pour Deezer (CarteDeezer ; demande de Jacob,
    ' 08/10/2026 : "fais le meme PIP affichage album pour les autres types de musique").
    '   - fichier : titre, artiste et pochette de son etiquette (MP3), sinon ce que VLC en sait,
    '     sinon le nom du fichier ; pochette du dossier a defaut (voir PochetteFichier) ;
    '   - radio : le morceau annonce par la station ("Artiste - Titre"), sinon son nom.
    ' Deezer, lui, nourrit la carte de son cote (MusiqueDeezer).
    ' ------------------------------------------------------------------------------------------

    Private WithEvents TimerCarte As New Timer With {.Interval = 1000}
    Private _carteFichier As String = ""           ' fichier dont le titre et l'artiste sont deja connus
    Private _carteTitre As String = ""
    Private _carteArtiste As String = ""

    Private Sub TimerCarte_Tick(sender As Object, e As EventArgs) Handles TimerCarte.Tick
        Try
            ' Deezer ne prend la carte que quand il JOUE vraiment : pendant son chargement (touche S, alternance) l'ancienne
            ' source continue de jouer et garde sa pochette (15/10/2026 : "avec S l'affichage de l'album ne marche plus").
            If MusiqueDeezer.Actif AndAlso MusiqueDeezer.EnLecture Then Exit Sub
            If Not (_enLecture AndAlso Lecteur.IsPlaying) Then
                CarteDeezer.SuivreMorceau(False, "", "", "", Nothing)
                Exit Sub
            End If
            If _radio Then
                Dim titre As String = "", artiste As String = "", annonce As String = "", nomDonne As String = ""
                Dim media As Vlc.DotNet.Core.VlcMedia = Lecteur.GetCurrentMedia()
                If media IsNot Nothing Then
                    annonce = If(media.NowPlaying, "")
                    nomDonne = If(media.Title, "")
                End If
                ' VLC ne donne pas les annonces d'une station en https : Slide les lit lui-meme (AnnonceRadio).
                If CarteDeezer.EstActive Then AnnonceRadio.SuivreLaStation(_url)
                If annonce.Trim() = "" Then annonce = AnnonceRadio.TitreAnnonce(_url)
                If AnnonceRadio.NomDeLaStation(_url) <> "" Then nomDonne = AnnonceRadio.NomDeLaStation(_url)
                DecrireLaRadio(annonce, nomDonne, _url, titre, artiste)
                CarteDeezer.DefinirProgression(-1)                      ' (une radio n'a pas de fin)
                CarteDeezer.SuivreMorceau(True, "radio" & vbLf & _url & vbLf & titre & vbLf & artiste, titre, artiste, Nothing, "radio")
            Else
                Dim fichier As String = _url
                If fichier <> _carteFichier Then
                    _carteFichier = fichier
                    MorceauDuFichier(fichier, _carteTitre, _carteArtiste)
                End If
                Try
                    Dim longueur As Long = Lecteur.Length
                    CarteDeezer.DefinirProgression(If(longueur > 5000, Lecteur.Time / CDbl(longueur), -1), Lecteur.Time / 1000.0, longueur / 1000.0)
                Catch ex As Exception
                    CarteDeezer.DefinirProgression(-1)
                End Try
                CarteDeezer.SuivreMorceau(True, "fichier" & vbLf & fichier, _carteTitre, _carteArtiste,
                                          Function() Threading.Tasks.Task.Run(Function() PochetteFichier.LirePochette(fichier)), "mp3")
            End If
        Catch ex As Exception
            ' la carte ne doit jamais gener la musique
        End Try
    End Sub

    ''' <summary>Titre et artiste d'un fichier : son etiquette, sinon ce que VLC en a lu, sinon son nom.</summary>
    Private Sub MorceauDuFichier(fichier As String, ByRef titre As String, ByRef artiste As String)
        titre = "" : artiste = ""
        Try
            If IO.Path.GetExtension(fichier).Equals(".mp3", StringComparison.OrdinalIgnoreCase) Then
                Dim etiquette As PochetteFichier.Etiquette = PochetteFichier.LireEtiquette(fichier)
                titre = etiquette.Titre : artiste = etiquette.Artiste
            End If
            If titre = "" Then
                Dim media As Vlc.DotNet.Core.VlcMedia = Lecteur.GetCurrentMedia()
                If media IsNot Nothing Then
                    Dim t As String = If(media.Title, "").Trim()
                    ' (Sans etiquette, VLC donne le nom du fichier pour titre : ce n'en est pas un.)
                    If t <> "" AndAlso Not t.Equals(IO.Path.GetFileName(fichier), StringComparison.OrdinalIgnoreCase) Then
                        titre = t
                        artiste = If(media.Artist, "").Trim()
                    End If
                End If
            End If
        Catch ex As Exception
        End Try
        If titre = "" Then PochetteFichier.DepuisLeNom(fichier, titre, artiste)
    End Sub

    ''' <summary>Titre et artiste a afficher pour la radio, d'apres ce que la station annonce
    ''' (<paramref name="annonce"/> : "Artiste - Titre") et le nom qu'elle se donne
    ''' (<paramref name="nomDonne"/>). Sans annonce : le nom de la station pour titre.</summary>
    Friend Sub DecrireLaRadio(annonce As String, nomDonne As String, adresse As String, ByRef titre As String, ByRef artiste As String)
        titre = "" : artiste = ""
        annonce = If(annonce, "").Trim()
        Dim station As String = If(nomDonne, "").Trim()
        ' (Sans nom donne par la station, VLC met l'adresse ou sa fin : ce n'est pas un nom.)
        If station = "" OrElse station.Contains("://") OrElse If(adresse, "").EndsWith(station, StringComparison.OrdinalIgnoreCase) Then station = "Radio"
        If annonce = "" Then
            titre = station
            artiste = If(station = "Radio", "", "Radio")
            Exit Sub
        End If
        Dim coupe As Integer = annonce.IndexOf(" - ", StringComparison.Ordinal)
        If coupe > 0 AndAlso coupe < annonce.Length - 3 Then
            artiste = annonce.Substring(0, coupe).Trim()
            titre = annonce.Substring(coupe + 3).Trim()
        Else
            titre = annonce
            artiste = station
        End If
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Radio
    ' ------------------------------------------------------------------------------------------

    Public Sub MusiqueJouerRadio(url As String)
        If ConfierAuFilPrincipal(Sub() MusiqueJouerRadio(url), "MusiqueJouerRadio") Then Exit Sub
        If url Is Nothing OrElse url.Trim() = "" Then Exit Sub
        If RetenuePourUneTache("radio") Then Exit Sub
        ' Cette station est deja en train de demarrer : on la laisse faire.
        If TimerRadio.Enabled AndAlso _radio AndAlso _url = url Then _enLecture = True : Exit Sub
        _radio = True
        _url = url
        _enLecture = True
        _octetsLus = -1
        TimerCarte.Start()
        Try
            PreparerLeVolumeAvantChangement(Lecteur, VolumePourVlc())
            Lecteur.SetMedia(New Uri(url), ":network-caching=3000")
            CouperLeSonAvantDemarrage(Lecteur)
            Lecteur.Play()
            _chronoRadio = Stopwatch.StartNew()
            TimerRadio.Start()
        Catch ex As Exception
            DiagLog("Radio (" & url & ") : " & ex.Message)
        End Try
    End Sub

    ' Demarrage de la radio suivi par un minuteur, sans bloquer l'interface.
    '
    ' Avant : on attendait ici, sur place, que la radio joue (5 s au plus), et elle ne joue jamais
    ' en moins de 5 s (liste .m3u a lire, connexion, puis 3 s de tampon) : journal du 09/10/2026,
    ' 24 demarrages sur 28 a 5,0 s pile. Pire, la demande suivante, ne la voyant pas encore jouer,
    ' la relancait de zero : 5 s de plus. Fermer l'apercu d'un film (Annuler) demandait la musique
    ' deux fois : la fenetre mettait 10 s a se fermer ; meme attente au lancement de Slide et a
    ' la touche N. Le volume est maintenant applique par ce minuteur, des que la radio joue.
    Private WithEvents TimerRadio As New Timer With {.Interval = 50}
    Private _chronoRadio As Stopwatch = Stopwatch.StartNew()
    Private Const DemarrageRadioMaxMs As Integer = 15000

    Private Sub TimerRadio_Tick(sender As Object, e As EventArgs) Handles TimerRadio.Tick
        Dim joue As Boolean = False
        Try
            joue = Lecteur.IsPlaying
        Catch ex As Exception
        End Try
        If Not joue AndAlso _chronoRadio.ElapsedMilliseconds < DemarrageRadioMaxMs Then Exit Sub
        TimerRadio.Stop()
        If Not _radio Then Exit Sub                             ' passee aux morceaux entre-temps
        Try
            If joue AndAlso Not _enLecture Then
                ' Pause demandee pendant le demarrage (video avec son, TV...) : elle est faite maintenant.
                Lecteur.Pause()
                DiagLog("Radio : demarree en " & _chronoRadio.ElapsedMilliseconds & " ms, mise en pause (demandee entre-temps)")
                Exit Sub
            End If
            DiagLog("Radio : " & If(joue, "demarree en ", "toujours pas demarree apres ") & _chronoRadio.ElapsedMilliseconds & " ms")
            VolumeApresDemarrage()
        Catch ex As Exception
            DiagLog("Radio (demarrage) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Station suivante de la liste des radios (data\radio.txt, la liste de l'onglet Audio) : pour
    ''' la radio, c'est la "musique suivante" de la touche N (demande de Jacob, 08/10/2026 : "touche N,
    ''' musique suivante pour tout type de musique"). Apres la derniere, on revient a la premiere.
    ''' La station des reglages n'est pas changee : elle revient au prochain lancement de Slide.
    ''' Renvoie le nom de la station lancee, "" s'il n'y en a pas d'autre.
    ''' </summary>
    Public Function MusiqueRadioSuivante() As String
        Try
            Dim stations As New List(Of Integer)
            For i As Integer = 0 To Radio.GetUpperBound(0)
                If Not String.IsNullOrWhiteSpace(Radio(i, 1)) Then stations.Add(i)
            Next
            If stations.Count < 2 Then Return ""
            Dim actuelle As String = If(_radio AndAlso _url <> "", _url, RadioUrl).Trim()
            Dim rang As Integer = stations.FindIndex(Function(i) Radio(i, 1).Trim().Equals(actuelle, StringComparison.OrdinalIgnoreCase))
            Dim suivante As Integer = stations((rang + 1) Mod stations.Count)       ' (station inconnue de la liste : la premiere)
            RadioUrl = Radio(suivante, 1).Trim()            ' pour que la reprise de la musique garde cette station
            DiagLog("Radio : station suivante -> " & Radio(suivante, 0) & " (" & RadioUrl & ")")
            MusiqueJouerRadio(RadioUrl)
            Return If(Radio(suivante, 0), "").Trim()
        Catch ex As Exception
            DiagLog("Radio (station suivante) : " & ex.Message)
            Return ""
        End Try
    End Function

    ''' <summary>Ce que joue la musique en ce moment (fichier ou adresse de la radio).</summary>
    Public Function MusiqueUrlActuelle() As String
        Return _url
    End Function

    ''' <summary>Vrai si la radio ne recoit plus rien (equivalent de receivedPackets = 0 du lecteur
    ''' Windows Media) : l'appelant peut alors relancer la connexion.</summary>
    Public Function MusiqueRadioSansDonnees() As Boolean
        If Not _radio Then Return False
        Try
            Dim media = Lecteur.GetCurrentMedia()
            If media Is Nothing Then Return True
            Dim octets As Long = media.Statistics.ReadBytes
            Dim immobile As Boolean = (octets <= 0) OrElse (octets = _octetsLus)
            _octetsLus = octets
            Return immobile
        Catch ex As Exception
            Return True
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Volume
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Volume de la musique, 0 a 100 comme avant (reglage de Form4).</summary>
    Public Sub MusiqueDefinirVolume(valeur As Integer)
        If ConfierAuFilPrincipal(Sub() MusiqueDefinirVolume(valeur), "MusiqueDefinirVolume") Then Exit Sub
        _volume = Math.Max(0, Math.Min(100, valeur))
        ' Deezer dans Slide : le meme reglage, envoye au lecteur de Deezer (0 a 100 %).
        If MusiqueDeezer.Actif Then
            MusiqueDeezer.DefinirVolume(_volume)
            Exit Sub
        End If
        AppliquerVolume()
    End Sub

    ''' <summary>Le meme volume, converti pour VLC. VLC n'applique PAS le pourcentage tel quel :
    ''' il l'eleve au cube. Mesure au banc sur un morceau : volume 100 = 38,9 % de niveau sonore,
    ''' 53 = 5,9 %, et 15 = 0,11 %, c'est-a-dire inaudible. Le reglage du programme (15 chez
    ''' Jacob), qui convenait au lecteur Windows Media, donnait donc le silence.
    ''' On prend la racine cubique pour retrouver le niveau d'avant : 15 devient 53.</summary>
    Private Function VolumePourVlc() As Integer
        If _volume <= 0 Then Return 0
        ' Correction du lissage (NiveauSonore), en decibels, sur l'amplitude.
        Dim amplitude As Double = _volume / 100.0 * Math.Pow(10, _correctionDb / 20)
        Dim v As Integer = CInt(Math.Round(100.0 * Math.Pow(amplitude, 1.0 / 3.0)))
        ' Au-dela de 100, VLC amplifie : permis seulement pour REMONTER un morceau enregistre trop
        ' faiblement (sinon, avec un volume deja haut, le lissage ne pourrait que baisser).
        ' 150 = +10,6 dB, le plafond du lissage.
        Return Math.Max(1, Math.Min(If(_correctionDb > 0, 150, 100), v))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Lissage du son (voir NiveauSonore) : correction propre au morceau en cours.
    ' ------------------------------------------------------------------------------------------

    Private _correctionDb As Double = 0

    ''' <summary>Corrige le volume du morceau en cours de <paramref name="db"/> decibels, en plus du
    ''' reglage de Form4. Applique tout de suite, sans attente (appele 20 fois par seconde pendant
    ''' une rampe).</summary>
    Public Sub MusiqueDefinirCorrection(db As Double)
        _correctionDb = db
        Try
            If Lecteur.Audio IsNot Nothing AndAlso Lecteur.IsPlaying AndAlso _volume > 0 Then Lecteur.Audio.Volume = VolumePourVlc()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Volume reglé dans Form4 (0 a 100, echelle lineaire), sans la correction.</summary>
    Public Function MusiqueVolumeDeBase() As Integer
        Return _volume
    End Function

    ''' <summary>Adresse du morceau (ou de la radio) qui joue, "" si rien ne joue.</summary>
    Public Function MusiqueMrlEnCours() As String
        Try
            If Not Lecteur.IsPlaying OrElse _volume <= 0 Then Return ""
            Dim media As Vlc.DotNet.Core.VlcMedia = Lecteur.GetCurrentMedia()
            Return If(media Is Nothing, "", If(media.Mrl, ""))
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>VLC demarre la lecture de facon differee : on lui laisse le temps, comme le fait
    ''' deja l'affichage des videos. Sans cette attente, le volume etait applique alors que rien ne
    ''' jouait encore, et il ne prenait pas.</summary>
    Private Sub AttendreLeDemarrage(Optional attenteMaxMs As Integer = 5000)
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do
            Try
                If Lecteur.IsPlaying Then Exit Do
            Catch ex As Exception
            End Try
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < attenteMaxMs
    End Sub

    ''' <summary>Comme pour les videos : VLC n'ouvre sa sortie audio qu'un instant apres Play et
    ''' repart a SON volume. On tient donc le silence jusqu'a ce qu'elle existe, puis on met le
    ''' volume de la musique. On n'appelle PAS SetVolume ici : cette fonction-la coupe les autres
    ''' lecteurs et met la musique en pause.</summary>
    Private Sub VolumeApresDemarrage()
        ' VLC met encore un instant a ouvrir sa sortie audio APRES le debut de la lecture : le
        ' volume demande trop tot est simplement ignore (mesure au banc : accepte au bout de
        ' ~110 ms). On insiste donc jusqu'a ce qu'il nous renvoie bien la valeur demandee.
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim accepte As Boolean = False
        Do
            Try
                If Lecteur.Audio IsNot Nothing Then
                    Lecteur.Audio.IsMute = True
                    Lecteur.Audio.Volume = VolumePourVlc()
                    If Lecteur.Audio.Volume = VolumePourVlc() Then accepte = True : Exit Do
                End If
            Catch ex As Exception
            End Try
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < 3000

        AppliquerVolume()       ' retablit le son (ou le coupe si le volume vaut 0)
        Try
            DiagLog("Musique : volume " & _volume & " (VLC " & VolumePourVlc() & ") accepte=" & accepte & " apres " & chrono.ElapsedMilliseconds &
                    " ms (lu " & Lecteur.Audio.Volume & ", muet=" & Lecteur.Audio.IsMute & ")")
        Catch ex As Exception
            DiagLog("Musique : pas de sortie audio (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Met le volume voulu et retablit le son (ou le coupe si le volume vaut 0). VLC
    ''' n'accepte pas toujours du premier coup : on verifie et on insiste jusqu'a une seconde.</summary>
    Private Sub AppliquerVolume()
        Dim muetVoulu As Boolean = (_volume = 0)
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim derniereErreur As String = ""
        Do
            Try
                If Lecteur.Audio IsNot Nothing Then
                    Lecteur.Audio.Volume = VolumePourVlc()
                    Lecteur.Audio.IsMute = muetVoulu
                    If Lecteur.Audio.IsMute = muetVoulu AndAlso Lecteur.Audio.Volume = VolumePourVlc() Then Exit Do
                End If
            Catch ex As Exception
                derniereErreur = ex.Message
            End Try
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < 1000

        If derniereErreur <> "" Then DiagLog("Musique (volume) : " & derniereErreur)
    End Sub

    ''' <summary>Fichier de musique ouvert par le lecteur ("" pour la radio ou a l'arret). Lisible
    ''' depuis un autre thread (la normalisation du volume en tache de fond).</summary>
    Public Function MusiqueFichierEnCours() As String
        If _radio OrElse Not _enLecture Then Return ""
        Return If(_url, "")
    End Function

    ''' <summary>Vrai si la musique joue vraiment en ce moment.</summary>
    ''' <summary>VLC joue VRAIMENT (sans tenir compte de la source choisie) : sert au garde-fou "une seule source a la fois".</summary>
    Friend Function VlcJoue() As Boolean
        Try
            Return _enLecture AndAlso Lecteur.IsPlaying
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Met VLC en pause parce que Deezer joue (mp3 et Deezer ne doivent jamais jouer ensemble).</summary>
    Friend Sub PauserCarDeezerJoue()
        If ConfierAuFilPrincipal(Sub() PauserCarDeezerJoue(), "MusiqueVLC") Then Exit Sub
        Try
            TimerSuivant.Stop()
            If Lecteur.IsPlaying Then Lecteur.Pause()
        Catch ex As Exception
        End Try
    End Sub

    Public Function MusiqueEnCours() As Boolean
        If MusiqueDeezer.Actif Then Return MusiqueDeezer.EnLecture
        Try
            Return Lecteur.IsPlaying
        Catch ex As Exception
            Return False
        End Try
    End Function

End Module




