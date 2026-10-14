Imports System
Imports System.Data.OleDb
Imports System.Drawing.Drawing2D
'pour exif
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Math
Imports System.Net
Imports System.Net.Http
Imports System.Net.Mail
Imports System.Text
Imports System.Threading
Imports System.Xml
Imports ADOX
Imports MetadataEtractor
Imports MetadataExtractor.Formats.Exif
Imports MetadataExtractor.Formats.QuickTime
Imports Two10.CountryLookup
Imports Vlc.DotNet.Core
Imports Vlc.DotNet.Forms
Imports Vlc.DotNet.Wpf

Public Structure Minuterie

    Dim Actif As Boolean
    Dim Jour As String
    Dim Heure As String
    Dim Type As String
    Dim Action As String
    Dim Play As String
    Dim Durée As String
    Dim Volume As String
    Dim Tri As String
    Dim Argument As String

End Structure

Public Structure Shabbat_Time

    Dim Ville As String
    Dim Debut As String
    Dim Fin As String
    Dim PreDebut As String
    Dim ToutLesXminutes As Integer
    Dim DurréeAffichage As Integer
    Dim counter1 As Double
    Dim counter2 As Integer
    Dim position As Integer

End Structure

Public Structure Diffu

    Dim Mode As String
    Dim Chaine As Integer
    Dim Vol As Integer
    Dim Arg As String
    Dim Source As String

End Structure
Module Module1

    ' Valeur generee par bump_version.ps1 (VersionAuto.vb), incrementee de 0,001 a chaque
    ' compilation reelle. Reste une variable et non une constante : l'ancien code pouvait
    ' l'affecter, et rien ne doit casser si c'etait le cas quelque part.
    Public SlideVersion As String = SlideVersionAuto
    Public NombreDePhoto As Double
    Public NombreDeVideo As Double
    Public NombreDeMusic As Double
    Public FullScreen As Boolean = False
    Public Music As Boolean = False
    Public Music_Mp3 As Boolean = False
    Public Music_Radio As Boolean = False
    Public Music_Deezer As Boolean = False
    Public DeezerInterne As Boolean = True        ' Deezer joue DANS Slide (MusiqueDeezer, FormDeezer) ; False : application Deezer externe, comme avant
    Public DeezerLogin As String = ""             ' compte Deezer (reglages, onglet Audio) : Slide remplit la page de connexion du Deezer integre
    Public DeezerMotDePasse As String = ""        ' ... son mot de passe, CHIFFRE par Windows pour cet utilisateur (MusiqueDeezer.Proteger), jamais en clair
    Public VideoWithAudio As Boolean = False
    Public FlagExit As Boolean = False
    Public cheminPhoto As String
    Public cheminVideo As String
    Public cheminPhotoVideo As String
    Public FlagStop As Boolean = False
    Public DurréePhoto As Integer = 180
    Public DurréeVideo As Integer = 180
    Public NumPhoto As Integer = -1
    Public NumVideo As Integer = -1
    Public ProchainePhotoForcée = 0
    Public SeizeNeuvieme As Double = (16 / 9)
    Public FormatPhoto169 As Boolean = True
    Public PhotoAlbum As Boolean = False
    Public PhotoAleatoire As Integer = 0           '0=non 1=aleatoire 2=aleatoiresequentiel 2=aleatoiresequentielDate
    Public VideoAleatoire As Boolean = True
    Public ErrorReadVideo As Boolean = False
    Public OptionChange As Boolean = False
    Public reprendreLeSlide As Boolean = False
    Public DurréeAffichageDuneImage As Integer = 5
    Public DossierDivxParDefault As String = ""
    Public DossierDreamboxParDefault As String = ""
    Public DossierFreeboxParDefault As String = ""
    Public ChannelID(50, 1) As String
    Public ChannelIDsat(500, 1) As String
    Public ChannelIDIPTV(25000, 1) As String
    Public ChannelIDsatTotal As Integer = 0
    Public ChannelIDIPTVTotal As Integer = 0
    Public jourDeLaSemaine() As String = {"Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche", "Lun-Ven", "Sam-Dim", "TLJ", "BOOT"}
    Public NouveauCycle As Boolean = True
    Public NouvelleMinute As String = "9999"
    Public PremierLancementDeLaMinuterie As Boolean = True
    Public LigneMinuterie As New ArrayList
    ''' <summary>Change a chaque rechargement de la minuterie (reglages fermes par OK, import...) : une passe qui lance les taches de la minute en voit
    ''' la modification et s'arrete (16/10/2026 : "j'ai supprime les PIP, les 2 du haut sont encore la" : ils partaient apres le OK).</summary>
    Public VersionDeLaMinuterie As Integer
    Public VolumeGeneral As Integer = 100
    Public VolumeMusicSlide As Integer = 100
    Public VolumeDeezer As Integer = 50           ' volume du Deezer integre (0 a 100), a la place de VolumeMusicSlide quand la source est Deezer (onglet Audio, cadre Deezer)
    Public VolumeVideoSlide As Integer = 100
    Public VolumeTVParDefault As Integer = 100
    Public VolumeDreamboxParDefault As Integer = 100
    Public VolumeNetflixPrimeParDefault As Integer = 100
    Public Diffusion As Diffu
    Public BarProgress As Double = 0
    Public BarProgressTotal As Double = 0
    Public Borderless As Boolean = False
    Public Dreambox_IP As String = "192.168.0.200"
    Public FluxReseaux_IP As String = "192.168.0.168"
    Public WindowsList As New ArrayList
    Public tailleBordureFenetre As Integer = SystemInformation.ToolWindowCaptionHeight
    Public AfficheDate As Boolean = False
    Public RadioUrl As String = "http//listen.radionomy.com/love-radio.m3u"
    Public clickedCell
    Public bitmap As Bitmap
    Public AfficheHeureDeShabbat As Boolean = True
    Public Shabbat As Shabbat_Time
    Public City(50, 3) As String
    Public Radio(500, 1) As String
    Public RetourAuDebutNetflix As Boolean = False
    Public recepteurSat As String = "\\VUDUO2\"
    'Public recepteurSat As String = "\\DM8000\"

    ' .NET Core/.NET 5+ : Application.StartupPath se termine desormais par un separateur,
    ' contrairement au .NET Framework. Tout le code concatene un chemin commencant par un
    ' separateur derriere : on conserve donc la forme sans separateur final.
    Public ReadOnly Property AppPath As String
        Get
            Return System.Windows.Forms.Application.StartupPath.TrimEnd(System.IO.Path.DirectorySeparatorChar)
        End Get
    End Property
    Public VLCpath As String = AppPath & "\data\VLC\"

    Private _reglagesPrets As Boolean
    Private ReadOnly _verrouReglages As New Object()

    ''' <summary>
    ''' data\settings\ : tout ce qui est PROPRE A CE POSTE (base slide.accdb, listes enregistrees, niveaux sonores, listes
    ''' IPTV, gmail.txt, journaux...). Ce dossier n'est jamais publie ni ecrase par les mises a jour (15/10/2026). A la
    ''' premiere utilisation les fichiers qui etaient directement dans data\ y sont deplaces.
    ''' </summary>
    Public ReadOnly Property DossierReglages As String
        Get
            Dim d As String = AppPath & "\data\settings"
            If Not _reglagesPrets Then
                SyncLock _verrouReglages
                    If Not _reglagesPrets Then
                        _reglagesPrets = True
                        MigrerLesReglages(d)
                    End If
                End SyncLock
            End If
            Return d
        End Get
    End Property

    ''' <summary>Un fichier de data\ qui est propre au poste (meme liste que MiseAJour.Exclu).</summary>
    Private Function EstUnReglageDuPoste(nom As String) As Boolean
        nom = nom.ToLowerInvariant()
        If nom = "savelistphoto.txt" OrElse nom = "savelistvideo.txt" OrElse nom = "savecomputername.txt" Then Return True
        If nom = "niveauxsonores.txt" OrElse nom = "niveauxsonores.sauvegarde.txt" OrElse nom = "soniedesvideos.txt" Then Return True
        If nom = "videossansson.txt" OrElse nom = "artiste.png" OrElse nom = "gmail.txt" OrElse nom = "tailscale_cle.txt" Then Return True
        If nom = "telechargements.txt" OrElse nom = "relancedenuit.txt" Then Return True
        If nom.StartsWith("normalisationmusique") OrElse nom.StartsWith("iptv") OrElse nom.StartsWith("slide.accdb") Then Return True
        If nom.EndsWith(".log") Then Return True
        Return False
    End Function

    Private Sub MigrerLesReglages(dossier As String)
        Try
            IO.Directory.CreateDirectory(dossier)
            Dim data As String = AppPath & "\data"
            If Not IO.Directory.Exists(data) Then Exit Sub
            For Each f As String In IO.Directory.GetFiles(data)
                Try
                    Dim nom As String = IO.Path.GetFileName(f)
                    If Not EstUnReglageDuPoste(nom) Then Continue For
                    Dim cible As String = IO.Path.Combine(dossier, nom)
                    If IO.File.Exists(cible) Then Continue For         ' deja migre : on ne touche a rien
                    IO.File.Move(f, cible)
                Catch
                    ' fichier verrouille : il sera repris au prochain demarrage
                End Try
            Next
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Options communes a tous les lecteurs VLC de Slide, posees juste avant leur creation (appele
    ''' par chaque VlcLibDirectoryNeeded). Sortie son DirectSound : chaque lecteur a alors SON
    ''' volume et SON "muet". Avec la sortie par defaut de VLC 3.0.24 (session audio Windows,
    ''' commune a tout le programme), couper le son du PIP coupait aussi la musique (01/10, apres
    ''' la mise a jour de VLC 3.0.7 -> 3.0.24).
    ''' </summary>
    ''' <param name="pipTv">Vrai pour le lecteur d'un PIP TV : la reduction de l'image est alors
    ''' faite par logiciel, avec un filtre de qualite. Mesure du 02/10 (LCI, PIP de 216 x 121) :
    ''' l'image TNT de 1920 x 1080 est divisee par 9 ; la puce graphique le fait avec un filtre
    ''' basique (lettres crenelees), le filtre bicubique rend des contours lisses, pour 2,6 % du
    ''' processeur au lieu de 2,2 % et aucune image en retard. Desentrelacement "bob" : 50 images
    ''' par seconde. (Options du LECTEUR : passees a la chaine, VLC les ignore.) Seulement pour le
    ''' PIP TV : en plein ecran ou pour un film 4K, ce chemin logiciel serait trop lourd.</param>
    Public Sub OptionsDuLecteurVlc(sender As Object, Optional pipTv As Boolean = False)
        Dim lecteur As Vlc.DotNet.Forms.VlcControl = TryCast(sender, Vlc.DotNet.Forms.VlcControl)
        If lecteur IsNot Nothing AndAlso lecteur.VlcMediaplayerOptions Is Nothing Then
            If pipTv Then
                lecteur.VlcMediaplayerOptions = {"--aout=directsound", "--vout=wingdi", "--swscale-mode=2",
                                                 "--deinterlace=1", "--deinterlace-mode=bob"}
                DiagLog("PIP TV : reduction de l'image par filtre bicubique (qualite)")
            Else
                lecteur.VlcMediaplayerOptions = {"--aout=directsound"}
            End If
        End If
    End Sub
    'Public OLEDBPROVIDER As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source="
    ' Le moteur Access porte deux noms selon la facon dont il est arrive sur le PC :
    '   Microsoft.ACE.OLEDB.16.0 = Office / Access 2016, 2019 ou 365 (clic-et-a-jour) ;
    '   Microsoft.ACE.OLEDB.12.0 = le redistribuable range dans Runtime\ (et Access 2007 a 2013).
    ' C'est le meme moteur, mais certains PC n'ont que l'un des deux inscrits : on prend donc
    ' celui qui est reellement la, au lieu d'exiger le 12.0 comme avant.
    Public OLEDBPROVIDER As String = "Provider=" & FournisseurAccess() & ";Data Source="
    Public PhotoModeCroissant As Boolean = False
    Public VideoModeCroissant As Boolean = False
    Public AfficheClock As Boolean = True
    Public ClockTaillePourcent As Integer = 100   ' taille de la montre en % de sa taille normale/BIG
    Public LegendeTaillePourcent As Integer = 100 ' taille de la legende du bas (lieu [date]) en % de la normale
    Public LegendeCoin As String = "Bgauche"     ' coin de l'etiquette lieu · date : Bgauche, Bdroite, Hgauche, Hdroite (Form4)
    Public BandesRelief As Boolean = True        ' relief argente du lieu et de la date (sinon texte plat) : Form4
    Public BandesPolice As String = "Arial Black"   ' police du lieu et de la date dans les bandes noires (Form4)
    Public BandesPosition As String = "Milieu"   ' lieu et date dans la bande noire : Haut, Milieu, Bas (Form4)
    Public LegendeCentreeSousPip As Boolean = False   ' etiquette lieu · date centree sous un PIP du meme coin (Form4)
    Public BandesActives As Boolean = True   ' lieu et date ecrits dans les bandes noires laterales (Form4) ; sinon tout reste en bas
    Public LisserLeSon As Boolean = True     ' egalise le niveau des musiques et des videos (NiveauSonore, Form4 onglet Audio)
    Public RelanceNuitActive As Boolean = True    ' relance automatique de Slide la nuit (RelanceDeNuit, Form4 onglet Divers)
    Public RelanceNuitHeure As Integer = 3        ' heure (0 a 23) a partir de laquelle elle est tentee
    Public RelanceNuitFrequence As Integer = 7    ' en jours : 7 (une fois par semaine) ou 30 (une fois par mois)
    Public AnimerLesPhotos As Boolean = False     ' photos en mouvement lent, fondu entre deux photos (AnimationPhoto, Form4 onglet Photos)
    Public AnimationArrivee As Integer = 1        ' facon dont une photo animee arrive (AnimationPhoto.Arrivee : 0 au hasard, 1 fondu...)
    Public AnimationMouvement As Integer = 0      ' son mouvement ensuite (AnimationPhoto.Mouvement : 0 au hasard, 1 avancer...)
    Public AnimerLesVideos As Boolean = False     ' transitions animees des videos (FormTransitionVideo, Form4 onglet Videos)
    Public AnimationVideo As Integer = 1          ' laquelle (FormTransitionVideo.Sortie : 0 au hasard, 1 fondu, 2 poussee, 3 volet, 4 cercle)
    Public AnimationVideoMouvement As Integer = 0  ' mouvement de la video pendant qu elle joue (VideoVivante.Mouvement : 0 au hasard, 1 avancer, 2 reculer, 3 glisser, 4 aucun)
    Public AnimationPhotoForce As Integer = 0     ' force du mouvement des photos animees : 0 normal, 1 fort, 2 tres fort (AnimationPhoto)
    Public AnimationVideoForce As Integer = 0     ' de meme pour les videos (VideoVivante)
    Public AnimationPhotoCouleurs As Integer = 0  ' couleurs des photos animees : 0 normales, 1 noir et blanc, 2 au hasard (CouleursDuDiaporama)
    Public AnimationVideoCouleurs As Integer = 0  ' de meme pour les videos du diaporama
    Public AnimationVideoBandes As Integer = 0    ' bandes noires d une video animee : 0 les laisser, 2 agrandir la video (VideoVivante)
    ' Animations tirees quand une liste est sur "Au hasard" : un bit par numero d animation (bit 0 : aucune). Voir TirageAuHasard.
    Public HasardPhotoArrivee As Integer = TirageAuHasard.PhotoArriveeParDefaut
    Public HasardPhotoMouvement As Integer = TirageAuHasard.PhotoMouvementParDefaut
    Public HasardVideoTransition As Integer = TirageAuHasard.VideoTransitionParDefaut
    Public HasardVideoMouvement As Integer = TirageAuHasard.VideoMouvementParDefaut
    Public HasardPhotoCouleurs As Integer = 3     ' couleurs tirees quand "Couleurs" est sur "Au hasard" : bit 0 normales, bit 1 noir et blanc
    Public HasardVideoCouleurs As Integer = 3
    Public FondFlouVideo As Boolean = False      ' fond flou anime dans les bandes noires des videos (VideoVivante), independant de AnimerLesVideos
    Public BandesTaillePourcent As Integer = 100  ' taille du lieu / de la date dans les bandes noires, en %
    Public CleUsbNom As String = "JACOB_1TO"      ' cle USB proposee par "Synchroniser sur la cle USB"
    Public LieuEnLigne As Boolean = False         ' lieu des photos/videos cherche sur Internet (OpenStreetMap)
    Public AfficheLieu As Boolean = True          ' localite affichee avec la date (Form4, onglet Photos)
    Public DejaClock As Boolean = False
    Public DurationLastVideo As Integer = False
    ''' <summary>Secondes de video deja jouees dans l'enchainement en cours (Form1) ; lu aussi par
    ''' PrechargementVideo pour savoir si une autre video va suivre.</summary>
    Public CumSecondeVideoPlay As Double
    Public DatabaseNetPath As String = ""
    Public DatabaseLocalPath As String = DossierReglages & "\slide.accdb"
    Public DatabasePath As String
    Public MinuterieEnReseaux As Boolean = False
    Public TempsDattenteAcceptable As Integer = 10
    Public TempsDattenteMaximum As Integer = 60
    Public onlyphotos As Boolean = False
    Public onlyvideos As Boolean = False
    Public ModeRdv As Boolean = False
    Public ModeRdvDateDebut As String = ""
    Public ModeRdvDateFin As String = ""
    Public ModeRdvType As String = ""
    Public ModeRdvLastNumPhoto As Integer = 0
    Public ModeRdvLastDatePhoto As Date = "01/01/1900"
    Public ModeRdvLastNumVideo As Integer = 0
    Public ModeRdvLastDateVideo As Date = "01/01/1900"
    Public ModeRdvLimitVideo As Integer = 0
    Public ModeRdvEnBoucle As Boolean = True
    Public RDV_AucunePhotoTrouvé As Boolean = False
    Public RDV_AucuneVideoTrouvé As Boolean = False
    Public ModeRdvFirstNumPhoto As Integer = 0
    Public ModeRdvFirstNumVideo As Integer = 0
    Public RDV_ProchainNumPhotoForcée As Integer = -1
    Public RDV_ProchainNumVideoForcée As Integer = -1
    Public NombreDeCapteur As Integer = 50
    Public Sonoff(NombreDeCapteur, 1) As String
    Public Croix As Boolean = False
    ''' <summary>Bouton "Réglages" de l'ecran de demarrage (ou Ctrl au lancement) : les reglages sont a
    ''' ouvrir des que le demarrage le permet. Voir Form1.DemanderLesReglages.</summary>
    Public ReglagesDemandesAuDemarrage As Boolean = False
    Public dejaPasséParLa As Integer = 0
    Public CompteurDeReconnexion As Integer = 0
    Public SansInternet As Boolean = True
    Public MinimumDurréeVideoAcceptable As Integer = 1
    Public VLCBoost As Boolean = True
    Public VolumeVLCBoost As Integer = 30
    Public VolumeActuel As Integer = 0
    Public Comment As String = ""
    Public PIPvolume As Integer = 0
    Public PipSizeDefault As Integer = 15
    Public Task As Task
    Public TaskIsPlaying As Boolean = False
    Public TaskIsPlayingState As Boolean = False
    Public AfficheVideoChabbatShalom As Boolean = False
    Public AfficheVideoChavouaTov As Boolean = True
    Public StopWindowsUpdate As Boolean = False
    Public User As String = ""
    Public DossierNflixParDefault As String = AppPath & "\data\iptv" & ".m3u"
    Public IPTV_Login As String = ""
    Public IPTV_Password As String = ""
    Public IPTV_url As String = ""
    Public IPTV_OnlyFavo = False
    Public GroupePardefault As String = ""
    Public PlanB = False
    Public Header As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, Like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063"
    Public ComputerName As String = Environment.MachineName
    Public GridPhoto As String = DossierReglages & "\SaveListPhoto.txt"
    Public GridVideo As String = DossierReglages & "\SaveListVideo.txt"
    Public GridMusic As String = AppPath & "\data\SaveListMusic.m3u"
    Public GridName As String = DossierReglages & "\SaveComputerName.txt"
    Public Form14DontClose As Boolean = True
    Public VolumeOFF As Boolean = False
    ''' <summary>Plus de 0 tant que la minuterie n'a pas rendu la main : une tache bloquante (film,
    ''' TV, ecran noir...) est en cours. Compte dans Form3.Timer3_Tick, lu par RelanceDeNuit.</summary>
    Public MinuterieEnCours As Integer
    Public VolumeMusicSlideParMinuterie As Integer = 0
    Public VolumeDeezerParMinuterie As Integer = -1  ' volume du Deezer integre impose par la minuterie (0 a 100), -1 : celui des reglages (VolumeDeezer)
    Public DeezerPlaylistParMinuterie As String = ""  ' playlist Deezer imposee par la minuterie (adresse), "" : celle des reglages (DeezerPlaylist)
    Public DeezerPlaylist As String = ""              ' playlist (ou album) que le Deezer integre lance : adresse Deezer, "" : ce que Deezer reprend de lui-meme
    Public DeezerPlaylistAuOK As Boolean = True       ' le bouton OK des reglages fait jouer la playlist du champ Playlist (case "La lancer quand je clique sur OK") ; False : OK ne change pas la musique en cours
    Public DeezerCarte As Boolean = True              ' pochette et titre du morceau de Deezer sur l'image, comme un PIP (Form4, onglet Audio ; voir CarteDeezer)
    Public DeezerCarteCoin As String = "Hgauche"      ' son coin : Bgauche, Bdroite, Hgauche, Hdroite
    Public DeezerCartePourcent As Integer = 100       ' sa taille en % de la taille normale (50 a 500)
    Public DeezerCarteSecondes As Integer = 0         ' 0 : toujours affichee ; sinon, affichee ce nombre de secondes a chaque nouveau morceau
    Public VolumeVideoSlideParMinuterie As Integer = 0
    Public LancerLePremierLancementDeLaMinuterie As Boolean = True
    Public DatePhotoMaximum As Date = "1/1/1900"
    Public DatePhotoMinimum As Date = "1/1/2999"
    Public DateVideoMaximum As Date = "1/1/1900"
    Public DateVideoMinimum As Date = "1/1/2999"
    Public Generator As New Random()
    Public HoraireDeShabbatDansCadran As Boolean = False
    Public LatenceNetflix As Integer = 0
    Public MinuterieFromSettings As Boolean = False
    Public VolumeOFFegal1 As Boolean = False
    Public ScreenSizeW As Integer = My.Computer.Screen.Bounds.Size.Width
    Public ScreenSizeH As Integer = My.Computer.Screen.Bounds.Size.Height
    Public mRow As Integer = 0
    Public ControlAdistance As Boolean = False
    Public yyMMdd As String = Format(Now, "yyMMdd")
    Public exited As Boolean
    Public wwDeezer As Integer = 5
    Public fsw As New FileSystemWatcher()
    Public FswDeja As Boolean = False
    Public ModeModifExif As Boolean = False
    Public Pourcentage As Double = 0.15
    Public Pipsize As Double = 0.15






    Sub InitSource()



        Call frequenceTV()
        ChronoEtape("  InitSource: apres frequenceTV")

        'Form2.Show()
        'city  0,0:Nom  0,1:latitude 0,2:longitude 0,3:TimeZone
        City(1, 0) = "Paris" : City(1, 1) = 48.85341 : City(1, 2) = 2.3488 : City(1, 3) = "Europe/Paris"
        City(2, 0) = "Cannes" : City(2, 1) = 43.55135 : City(2, 2) = 7.01275 : City(2, 3) = "Europe/Paris"
        City(3, 0) = "Netanya" : City(3, 1) = 32.33291 : City(3, 2) = 34.85992 : City(3, 3) = "Asia/Tel_Aviv"
        City(4, 0) = "Netanya2" : City(3, 1) = 32.33291 : City(3, 2) = 34.85992 : City(3, 3) = "Asia/Tel_Aviv"
        City(5, 0) = "Tel Aviv" : City(4, 1) = 32.08088 : City(4, 2) = 34.78057 : City(4, 3) = "Asia/Tel_Aviv"
        City(6, 0) = "Los Angeles" : City(5, 1) = 34.05223 : City(5, 2) = -118.24368 : City(5, 3) = "US/Pacific"
        City(7, 0) = "Miami" : City(6, 1) = 25.77427 : City(6, 2) = -80.19366 : City(6, 3) = "US/Eastern"
        City(8, 0) = "New York" : City(7, 1) = 40.71427 : City(7, 2) = -74.00597 : City(7, 3) = "US/Eastern"
        City(9, 0) = "Maldives Malé" : City(8, 1) = 4.1748 : City(8, 2) = 73.50888 : City(8, 3) = "Indian/Maldives"
        City(10, 0) = "Marrackech" : City(9, 1) = 31.63416 : City(9, 2) = -7.99994 : City(9, 3) = "Etc/GMT+0"
        City(11, 0) = "Eilat" : City(10, 1) = 29.55805 : City(10, 2) = 34.94821 : City(10, 3) = "Asia/Tel_Aviv"
        City(12, 0) = "Rio de Janeiro" : City(11, 1) = -22.90278 : City(11, 2) = -43.2075 : City(11, 3) = "America/Fortaleza"

        'Sonoff
        'Sonoff(0, 0) = "$Groupe VideoProjecteur et SonoSousSol" : Sonoff(0, 1) = "192.168.169.153/153"
        Sonoff(0, 0) = "$Groupe VideoProjecteur et SonoSousSol" : Sonoff(0, 1) = "192.168.0.150/151"
        Sonoff(1, 0) = "VideoProjecteur(150)" : Sonoff(1, 1) = "192.168.0.150"
        Sonoff(2, 0) = "SonoSousSol(151)" : Sonoff(2, 1) = "192.168.0.151"
        Sonoff(3, 0) = "Sonoff TV Salon(156)" : Sonoff(3, 1) = "192.168.0.156"
        Sonoff(4, 0) = "Sonoff153" : Sonoff(4, 1) = "192.168.0.153"
        Sonoff(5, 0) = "Sonoff154" : Sonoff(5, 1) = "192.168.0.154"
        Sonoff(6, 0) = "Chauffage soufflant(155)" : Sonoff(6, 1) = "192.168.0.155"

        NombreDeCapteur = 7

        My.Application.DoEvents()
        'chaine sat
        Call ListChaineSAT()
        ChronoEtape("  InitSource: apres ListChaineSAT")


        My.Application.DoEvents()

        ChargeRadioList()
        MusiqueVLC.MusiqueVider()
        ChronoEtape("  InitSource: apres ChargeRadioList + newPlaylist")

        'repertoire du fichier user.config C:\Users\jacob\AppData\Local\slide\...
        v = 0 : p = 0 : m = 0
        Form1.ProgressBar1.Value = 20

        If Croix = True Then Exit Sub

        ' Relance de nuit : le diaporama reprend ou il en etait, meme sans l'option "reprendre".
        If My.Settings.reprendreLeSlide = True OrElse RelanceDeNuit.Reprise Then
            If My.Settings.reprendreLeSlide = True Then reprendreLeSlide = True
            result = SaveRestoreLeSlide(2)

            If result = True Then Exit Sub

        End If


        If My.Settings.CheminPhotoVideo Is Nothing Then Exit Sub


        Try

            For Each item In My.Settings.CheminPhotoVideo
                If Croix = True Then Exit Sub
                My.Application.DoEvents()
                p = p + NBPhotosInDir(item) : Form1.Label1.Text = "Photos  " & Format(p)
                v = v + NBVideosInDir(item) : Form1.Label2.Text = "Videos  " & Format(v)
                m = m + NBMusicInDir(item) : Form1.Label3.Text = "Musics  " & Format(m)
                My.Application.DoEvents()
            Next
            ChronoEtape("  InitSource: apres boucle COMPTAGE (NBPhotos/NBVideos/NBMusic)")
            BarProgressTotal = 40
            Form1.ProgressBar1.Value = BarProgressTotal

            BarProgress = (100 - BarProgressTotal) / (My.Settings.CheminPhotoVideo.Count) / 2

            ' (13/10/2026 : une attente fixe de 2 s ici, des qu'il y avait plus de 2 000 photos et videos, est supprimee.)
            ChronoEtape("  InitSource: apres wait(2) conditionnel")

            For Each item In My.Settings.CheminPhotoVideo
                If Croix = True Then Exit Sub
                My.Application.DoEvents()
                BarProgressTotal = BarProgressTotal + BarProgress  'Form1.ProgressBar1.Value = BarProgressTotal

                Call PhotoVideoInMemory(item)

            Next
            ChronoEtape("  InitSource: apres boucle CHARGEMENT (PhotoVideoInMemory)")

            DatePhotoMaximum = (From T In Form2.DataGridView1.Rows.Cast(Of DataGridViewRow)()
                                Select T.Cells(2).Value).Max

            DatePhotoMinimum = (From T In Form2.DataGridView1.Rows.Cast(Of DataGridViewRow)()
                                Select T.Cells(2).Value).Min

            DateVideoMaximum = (From T In Form2.DataGridView2.Rows.Cast(Of DataGridViewRow)()
                                Select T.Cells(2).Value).Max

            DateVideoMinimum = (From T In Form2.DataGridView2.Rows.Cast(Of DataGridViewRow)()
                                Select T.Cells(2).Value).Min



            Form1.ProgressBar1.Value = 100

            NombreDeMusic = m

            Do Until Form2.DataGridView1.ColumnCount = 4
                Form2.DataGridView1.Columns.RemoveAt(4)
            Loop

            Do Until Form2.DataGridView2.ColumnCount = 4
                Form2.DataGridView2.Columns.RemoveAt(4)
            Loop

            Form2.DataGridView1.Columns.Add("Tag", "ExifDate")
            Form2.DataGridView2.Columns.Add("Tag", "ExifDate")
            Form2.DataGridView1.Update()
            Form2.DataGridView2.Update()
            ChronoEtape("  InitSource: fin (colonnes + min/max dates)")




        Catch ex As Exception
            Form4.TabControl1.SelectedIndex = 0
            'MsgBox(ex.ToString)
            'Form4.ShowDialog()

        End Try


    End Sub

    Sub LoadUserValue()
        StopWindowsUpdate = My.Settings.StopWindowsUpdate
        SansInternet = My.Settings.SansInternet
        RetourAuDebutNetflix = My.Settings.RetourAuDebutNetflix
        VolumeOFFegal1 = My.Settings.VolumeOFFegal1
        VLCBoost = My.Settings.VLCBoost
        VolumeVLCBoost = My.Settings.VolumeVLCBoost
        Comment = My.Settings.Comment
        PipSizeDefault = My.Settings.PipSizeDefault
        Borderless = My.Settings.BorderLess
        MinuterieEnReseaux = My.Settings.MinuterieEnReseaux
        FullScreen = My.Settings.FullScreen
        RadioUrl = My.Settings.RadioURL
        Music_Radio = My.Settings.Music_Radio
        Music_Deezer = My.Settings.Music_Deezer
        Music_Mp3 = My.Settings.Music_Mp3
        VideoWithAudio = My.Settings.VideoWithAudio
        Music = My.Settings.Music
        PhotoAleatoire = My.Settings.PhotoAleatoire
        VideoAleatoire = My.Settings.VideoAleatoire
        FormatPhoto169 = My.Settings.FormatPhoto169
        PhotoAlbum = My.Settings.PhotoAlbum
        DurréePhoto = My.Settings.durréePhoto
        DurréeVideo = My.Settings.DurréeVideo
        NumPhoto = 0 'My.Settings.ProchainePhoto
        NumVideo = 0 ' My.Settings.ProchaineVideo
        DossierDivxParDefault = My.Settings.DossierDivxParDefault
        DossierDreamboxParDefault = My.Settings.DossierDreamboxParDefault
        DossierFreeboxParDefault = My.Settings.DossierFreeboxParDefault
        Dreambox_IP = My.Settings.Dreambox_IP
        FluxReseaux_IP = My.Settings.FluxReseaux_IP
        onlyphotos = My.Settings.OnlyPhotos
        onlyvideos = My.Settings.OnlyVideos
        ModeRdv = My.Settings.ModeRDV
        ModeRdvDateDebut = My.Settings.ModeRdvDateDebut
        ModeRdvDateFin = My.Settings.ModeRdvDateFin
        VolumeGeneral = My.Settings.VolumeGeneral
        VolumeMusicSlide = My.Settings.VolumeMusicSlide
        VolumeVideoSlide = My.Settings.VolumeVideoSlide
        VolumeTVParDefault = My.Settings.VolumeTVParDefault
        VolumeDreamboxParDefault = My.Settings.VolumeDreamboxParDefault
        VolumeNetflixPrimeParDefault = My.Settings.VolumeNetflixPrimeParDefault
        AfficheDate = My.Settings.AfficheDate
        AfficheHeureDeShabbat = My.Settings.AfficheHeureDeShabbat
        PhotoModeCroissant = My.Settings.PhotoModeCroissant
        VideoModeCroissant = My.Settings.VideoModeCroissant
        AfficheVideoChavouaTov = My.Settings.AfficheVideoChavouaTov
        AfficheVideoChabbatShalom = My.Settings.AfficheVideoChabbatShalom
        Shabbat.Ville = My.Settings.Shabbat_Ville
        Shabbat.ToutLesXminutes = My.Settings.Shabbat_ToutLesXminutes
        AfficheClock = My.Settings.AfficheClock
        ClockTaillePourcent = My.Settings.ClockTaillePourcent
        LegendeTaillePourcent = My.Settings.LegendeTaillePourcent
        LegendeCoin = If(String.IsNullOrEmpty(My.Settings.LegendeCoin), "Bgauche", My.Settings.LegendeCoin)
        BandesPosition = If(String.IsNullOrEmpty(My.Settings.BandesPosition), "Milieu", My.Settings.BandesPosition)
        BandesPolice = If(String.IsNullOrEmpty(My.Settings.BandesPolice), "Arial Black", My.Settings.BandesPolice)
        BandesRelief = My.Settings.BandesRelief
        FormLieuBande.DefinirRelief(BandesRelief)
        FormLieuBande.DefinirPolice(BandesPolice)
        LegendeCentreeSousPip = My.Settings.LegendeCentreeSousPip
        BandesActives = My.Settings.BandesActives
        LisserLeSon = My.Settings.LisserLeSon
        RelanceNuitActive = My.Settings.RelanceNuitActive
        RelanceNuitHeure = Math.Max(0, Math.Min(23, My.Settings.RelanceNuitHeure))
        RelanceNuitFrequence = My.Settings.RelanceNuitFrequence
        AnimerLesPhotos = My.Settings.AnimerLesPhotos
        AnimationArrivee = My.Settings.AnimationArrivee
        AnimationMouvement = My.Settings.AnimationMouvement
        AnimerLesVideos = My.Settings.AnimerLesVideos
        AnimationVideo = My.Settings.AnimationVideo
        AnimationVideoMouvement = My.Settings.AnimationVideoMouvement
        AnimationPhotoForce = My.Settings.AnimationPhotoForce
        AnimationVideoForce = My.Settings.AnimationVideoForce
        AnimationPhotoCouleurs = My.Settings.AnimationPhotoCouleurs
        AnimationVideoCouleurs = My.Settings.AnimationVideoCouleurs
        HasardPhotoArrivee = My.Settings.HasardPhotoArrivee
        HasardPhotoMouvement = My.Settings.HasardPhotoMouvement
        HasardVideoTransition = My.Settings.HasardVideoTransition
        HasardVideoMouvement = My.Settings.HasardVideoMouvement
        HasardPhotoCouleurs = My.Settings.HasardPhotoCouleurs
        HasardVideoCouleurs = My.Settings.HasardVideoCouleurs
        DeezerInterne = My.Settings.DeezerInterne
        VolumeDeezer = My.Settings.VolumeDeezer
        DeezerPlaylist = If(My.Settings.DeezerPlaylist, "")
        DeezerPlaylistAuOK = My.Settings.DeezerPlaylistAuOK
        DeezerCarte = My.Settings.DeezerCarte
        DeezerCarteCoin = If(String.IsNullOrEmpty(My.Settings.DeezerCarteCoin), "Hgauche", My.Settings.DeezerCarteCoin)
        DeezerCartePourcent = If(My.Settings.DeezerCartePourcent <= 0, 100, My.Settings.DeezerCartePourcent)
        DeezerCarteSecondes = Math.Max(0, My.Settings.DeezerCarteSecondes)
        DeezerLogin = If(My.Settings.DeezerLogin, "")
        DeezerMotDePasse = If(My.Settings.DeezerMotDePasse, "")
        AnimationVideoBandes = My.Settings.AnimationVideoBandes
        FondFlouVideo = My.Settings.FondFlouVideo
        ' (Avant la 8.432, le fond flou etait la valeur 1 de la liste "Bandes noires".)
        If AnimationVideoBandes = 1 Then
            AnimationVideoBandes = 0 : My.Settings.AnimationVideoBandes = 0
            FondFlouVideo = True : My.Settings.FondFlouVideo = True
        End If
        BandesTaillePourcent = My.Settings.BandesTaillePourcent
        CleUsbNom = My.Settings.CleUsbNom
        LieuEnLigne = My.Settings.LieuEnLigne
        AfficheLieu = My.Settings.AfficheLieu
        ConvertirAncienneOptionClockBIG()
        User = My.Settings.User
        IPTV_Login = My.Settings.IPTV_login
        IPTV_Password = My.Settings.IPTV_password
        IptvSources.Initialiser()                 ' adresse de la source IPTV active (voir IptvSources)
        GroupePardefault = My.Settings.GroupePardefault
        IPTV_OnlyFavo = My.Settings.IPTV_OnlyFavo
        PlanB = My.Settings.PlanB
        HoraireDeShabbatDansCadran = My.Settings.HoraireDeShabbatDansCadran
        LatenceNetflix = My.Settings.LatenceNetflix
        ControlAdistance = My.Settings.ControlAdistance

    End Sub


    Sub init()

        ChronoEtape("init : debut")
        RelanceDeNuit.LireEtat()                     ' relance de nuit : etat laisse par l'ancien programme


        ' Heure du PC verifiee (NTP, sinon google.com) et corrigee si besoin : voir HeureDuPC.vb
        ' (remplace le programme externe data\SyncTime.exe).
        VerifierHeureAuLancement()
        ChronoEtape("apres VerifierHeureAuLancement (reseau)")


        'On Error Resume Next
        Form2.Hide()
        ChronoEtape("apres Form2.Hide (creation de Form2)")

        Diffusion.Mode = "VIDEO"   'diffusion actuellement  cad que ce passe t il actuellement film tv photo video

        DatabaseNetPath = My.Settings.DatabaseNetPath
        ChronoEtape("apres 1er acces My.Settings")

        If My.Settings.MinuterieEnReseaux = True Then
            MinuterieEnReseaux = My.Settings.MinuterieEnReseaux
            DatabasePath = DatabaseNetPath & "\slide.accdb"
            If My.Computer.FileSystem.FileExists(DatabasePath) = False Then DatabasePath = DatabaseLocalPath : MinuterieEnReseaux = False : My.Settings.MinuterieEnReseaux = MinuterieEnReseaux
        Else
            DatabasePath = DatabaseLocalPath
        End If
        ChronoEtape("apres FileExists sur le chemin RESEAU de la base")


        SansInternet = My.Settings.SansInternet
        StopWindowsUpdate = My.Settings.StopWindowsUpdate
        ControlAdistance = My.Settings.ControlAdistance

        If ControlAdistance = True Then
            Try
                FileSystem.Kill(DatabaseNetPath & "\ControleAdistance\*.*")
            Catch ex As Exception
                'MsgBox(ex.Message)
            End Try
        End If
        ChronoEtape("apres bloc ControlAdistance (Kill sur chemin reseau)")

        RetourAuDebutNetflix = My.Settings.RetourAuDebutNetflix
        VolumeOFFegal1 = My.Settings.VolumeOFFegal1
        VLCBoost = My.Settings.VLCBoost
        VolumeVLCBoost = My.Settings.VolumeVLCBoost
        Comment = My.Settings.Comment
        PipSizeDefault = My.Settings.PipSizeDefault

        ' (Le repli sur Microsoft.Jet.OLEDB.4.0 quand le programme tournait en 32 bits a ete retire :
        '  Jet ne sait pas lire un fichier .accdb, et Slide est desormais compile en 64 bits.)

        ' Villes pour la legende "lieu [date]" des photos/videos : chargees une fois, en arriere-plan.
        PrechargerVilles()
        ChronoEtape("avant InitSource")
        Call InitSource()
        ChronoEtape("apres InitSource (scan photos/videos)")

        Form2.Label4.Text = "Photo de " & Format(DatePhotoMinimum, "MMMyyyy") & " à " & Format(DatePhotoMaximum, "MMMyyyy") & " et Video de " & Format(DateVideoMinimum, "MMMyyyy") & " à " & Format(DateVideoMaximum, "MMMyyyy")


        My.Application.DoEvents()

        If YaVraimentDesPhotosAffichable() = False Then NombreDePhoto = 0
        ChronoEtape("apres YaVraimentDesPhotosAffichable")

        Borderless = My.Settings.BorderLess
        Try
            If Borderless = True Then Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None Else Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
        Catch ex As Exception

        End Try

        FullScreen = My.Settings.FullScreen : FullScreen_SWITCH_ON(FullScreen)
        PleinEcranChange(FullScreen)          ' demarrage deja en plein ecran : programmes gardes a partir d'ici

        If My.Settings.RadioURL <> "" Then RadioUrl = My.Settings.RadioURL
        Music_Mp3 = My.Settings.Music_Mp3
        Music_Radio = My.Settings.Music_Radio
        Music_Deezer = My.Settings.Music_Deezer


        If My.Settings.VideoWithAudio = True Then VideoWithAudio = My.Settings.VideoWithAudio
        Music = My.Settings.Music
        RelanceDeNuit.RemettreLaMusique()            ' relance de nuit : musique comme avant

        If Music = True Then
            If Music_Radio = True Then MusiqueVLC.MusiqueJouerRadio(RadioUrl)
            If Music_Deezer = True Then
                ' Deezer dans Slide (MusiqueDeezer), ou l'application Deezer comme avant.
                If MusiqueDeezer.Actif Then MusiqueDeezer.Demarrer() Else LanceDeezer()
            Else
                FermerLesLecteursLances("Deezer")      ' (au demarrage : aucun, le Deezer de l'utilisateur reste)
            End If
            If Diffusion.Mode <> "VIDEO" And VideoWithAudio = False Then Music_SWITCH_ON(Music)
            If NombreDePhoto = 0 And Croix = False Then MusiqueVLC.MusiqueJouer() : wait(10)
        End If
        ChronoEtape("apres bloc Music (Deezer/radio)")


        If String.IsNullOrEmpty(My.Settings.PhotoAleatoire) = False Then PhotoAleatoire = My.Settings.PhotoAleatoire
        If String.IsNullOrEmpty(My.Settings.VideoAleatoire) = False Then VideoAleatoire = My.Settings.VideoAleatoire
        If String.IsNullOrEmpty(My.Settings.FormatPhoto169) = False Then FormatPhoto169 = My.Settings.FormatPhoto169
        If String.IsNullOrEmpty(My.Settings.PhotoAlbum) = False Then PhotoAlbum = My.Settings.PhotoAlbum


        If My.Settings.durréePhoto > 0 Then DurréePhoto = My.Settings.durréePhoto
        If My.Settings.DurréeVideo > 0 Then DurréeVideo = My.Settings.DurréeVideo

        If My.Settings.DurréeAffichageDuneImage > 0 Then DurréeAffichageDuneImage = My.Settings.DurréeAffichageDuneImage
        If My.Settings.LatenceNetflix > 0 Then LatenceNetflix = My.Settings.LatenceNetflix
        'If My.Settings.reprendreLeSlide = True Then
        'reprendreLeSlide = True
        'NumPhoto = My.Settings.ProchainePhoto
        'NumVideo = My.Settings.ProchaineVideo
        'NumPhoto = 0
        'ideo = 0
        'End If


        w = My.Settings.ScreenSizeW
        h = My.Settings.ScreenSizeH
        'ScreenSizeW = w
        'ScreenSizeH = h


        If w = 0 Or h = 0 Then
            Form3.Width = 800
            Form3.Height = 450 + tailleBordureFenetre
        Else
            Form3.Width = My.Settings.ScreenSizeW
            Form3.Height = My.Settings.ScreenSizeH
        End If

        ' La fenetre reprend la place qu'elle avait au dernier lancement (demande de Jacob, 07/10) ;
        ' jamais retenue, ou hors de tout ecran : centree, comme avant. Lancee en plein ecran, elle
        ' y reviendra en quittant le plein ecran.
        Dim placeRetenue As Rectangle? = PlaceEnregistree()
        If placeRetenue.HasValue Then
            If FullScreen = True Then
                _fenetreAvantPleinEcran = placeRetenue.Value
            Else
                Form3.StartPosition = FormStartPosition.Manual
                Form3.Location = placeRetenue.Value.Location
            End If
            DiagLog("Fenetre : place du dernier lancement " & placeRetenue.Value.ToString())
        End If

        If FullScreen = True Then FullScreen_SWITCH_ON(FullScreen)

        Form2.HScrollBar1.Minimum = 0
        Form2.HScrollBar1.Maximum = NombreDePhoto
        Form2.HScrollBar2.Minimum = 0
        Form2.HScrollBar2.Maximum = NombreDeVideo
        If NombreDePhoto < 20 Then Form2.HScrollBar1.LargeChange = 1
        If NombreDeVideo < 20 Then Form2.HScrollBar2.LargeChange = 1

        DossierDivxParDefault = My.Settings.DossierDivxParDefault
        DossierDreamboxParDefault = My.Settings.DossierDreamboxParDefault
        DossierFreeboxParDefault = My.Settings.DossierFreeboxParDefault
        Dreambox_IP = My.Settings.Dreambox_IP
        FluxReseaux_IP = My.Settings.FluxReseaux_IP
        onlyphotos = My.Settings.OnlyPhotos
        onlyvideos = My.Settings.OnlyVideos

        ModeRdv = My.Settings.ModeRDV
        ModeRdvDateDebut = My.Settings.ModeRdvDateDebut
        ModeRdvDateFin = My.Settings.ModeRdvDateFin
        ModeRdvLimitVideo = My.Settings.ModeRdvLimitVideo
        ModeRdvEnBoucle = My.Settings.ModeRDVenBoucle

        LatenceNetflix = My.Settings.LatenceNetflix


        MusiqueVLC.MusiqueDefinirVolume(100)
        If MusiqueDeezer.Actif Then MusiqueDeezer.DefinirVolume(VolumeDeezer)       ' Deezer integre : son propre volume, des le depart (pas 100)

        VolumeGeneral = My.Settings.VolumeGeneral
        ' Relance de nuit : un "Volume OFF" de la minuterie reste coupe (voir RelanceDeNuit).
        RelanceDeNuit.RemettreLeVolumeGeneral()
        If VolumeOFF = True Then SetMasterVol(If(VolumeOFFegal1 = True, 1, 0)) Else SetMasterVol(VolumeGeneral)
        VolumeMusicSlide = My.Settings.VolumeMusicSlide ': SetMusicVolSlide(VolumeMusicSlide)
        VolumeVideoSlide = My.Settings.VolumeVideoSlide ': SetVideoVolSlide(VolumeVideoSlide)
        VolumeTVParDefault = My.Settings.VolumeTVParDefault
        VolumeDreamboxParDefault = My.Settings.VolumeDreamboxParDefault
        VolumeNetflixPrimeParDefault = My.Settings.VolumeNetflixPrimeParDefault
        AfficheDate = My.Settings.AfficheDate

        AfficheHeureDeShabbat = My.Settings.AfficheHeureDeShabbat

        PhotoModeCroissant = My.Settings.PhotoModeCroissant
        VideoModeCroissant = My.Settings.VideoModeCroissant
        RelanceDeNuit.RemettreLesReglages()          ' relance de nuit : volumes et modes d'avant


        Try

            If PhotoModeCroissant = True Then
                Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
            Else
                Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Descending)
            End If

            If VideoModeCroissant = True Then
                Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
            Else
                Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Descending)
            End If

        Catch ex As Exception

        End Try

        AfficheVideoChavouaTov = My.Settings.AfficheVideoChavouaTov
        AfficheVideoChabbatShalom = My.Settings.AfficheVideoChabbatShalom
        Shabbat.Ville = My.Settings.Shabbat_Ville
        Shabbat.ToutLesXminutes = My.Settings.Shabbat_ToutLesXminutes
        Shabbat.counter1 = 0
        Shabbat.counter2 = 0
        If AfficheHeureDeShabbat = True Or AfficheVideoChavouaTov = True Or AfficheVideoChabbatShalom = True Then
            Call HeureDeShabbat(Shabbat.Ville)
            ChronoEtape("apres HeureDeShabbat (reseau)")
            'Form3.Timer1.Enabled = True
        End If
        My.Application.DoEvents()

        AfficheClock = My.Settings.AfficheClock
        ClockTaillePourcent = My.Settings.ClockTaillePourcent
        LegendeTaillePourcent = My.Settings.LegendeTaillePourcent
        LegendeCoin = If(String.IsNullOrEmpty(My.Settings.LegendeCoin), "Bgauche", My.Settings.LegendeCoin)
        BandesPosition = If(String.IsNullOrEmpty(My.Settings.BandesPosition), "Milieu", My.Settings.BandesPosition)
        BandesPolice = If(String.IsNullOrEmpty(My.Settings.BandesPolice), "Arial Black", My.Settings.BandesPolice)
        BandesRelief = My.Settings.BandesRelief
        FormLieuBande.DefinirRelief(BandesRelief)
        FormLieuBande.DefinirPolice(BandesPolice)
        LegendeCentreeSousPip = My.Settings.LegendeCentreeSousPip
        BandesActives = My.Settings.BandesActives
        LisserLeSon = My.Settings.LisserLeSon
        RelanceNuitActive = My.Settings.RelanceNuitActive
        RelanceNuitHeure = Math.Max(0, Math.Min(23, My.Settings.RelanceNuitHeure))
        RelanceNuitFrequence = My.Settings.RelanceNuitFrequence
        AnimerLesPhotos = My.Settings.AnimerLesPhotos
        AnimationArrivee = My.Settings.AnimationArrivee
        AnimationMouvement = My.Settings.AnimationMouvement
        AnimerLesVideos = My.Settings.AnimerLesVideos
        AnimationVideo = My.Settings.AnimationVideo
        AnimationVideoMouvement = My.Settings.AnimationVideoMouvement
        AnimationPhotoForce = My.Settings.AnimationPhotoForce
        AnimationVideoForce = My.Settings.AnimationVideoForce
        AnimationPhotoCouleurs = My.Settings.AnimationPhotoCouleurs
        AnimationVideoCouleurs = My.Settings.AnimationVideoCouleurs
        HasardPhotoArrivee = My.Settings.HasardPhotoArrivee
        HasardPhotoMouvement = My.Settings.HasardPhotoMouvement
        HasardVideoTransition = My.Settings.HasardVideoTransition
        HasardVideoMouvement = My.Settings.HasardVideoMouvement
        HasardPhotoCouleurs = My.Settings.HasardPhotoCouleurs
        HasardVideoCouleurs = My.Settings.HasardVideoCouleurs
        DeezerInterne = My.Settings.DeezerInterne
        VolumeDeezer = My.Settings.VolumeDeezer
        DeezerPlaylist = If(My.Settings.DeezerPlaylist, "")
        DeezerPlaylistAuOK = My.Settings.DeezerPlaylistAuOK
        DeezerCarte = My.Settings.DeezerCarte
        DeezerCarteCoin = If(String.IsNullOrEmpty(My.Settings.DeezerCarteCoin), "Hgauche", My.Settings.DeezerCarteCoin)
        DeezerCartePourcent = If(My.Settings.DeezerCartePourcent <= 0, 100, My.Settings.DeezerCartePourcent)
        DeezerCarteSecondes = Math.Max(0, My.Settings.DeezerCarteSecondes)
        DeezerLogin = If(My.Settings.DeezerLogin, "")
        DeezerMotDePasse = If(My.Settings.DeezerMotDePasse, "")
        AnimationVideoBandes = My.Settings.AnimationVideoBandes
        FondFlouVideo = My.Settings.FondFlouVideo
        ' (Avant la 8.432, le fond flou etait la valeur 1 de la liste "Bandes noires".)
        If AnimationVideoBandes = 1 Then
            AnimationVideoBandes = 0 : My.Settings.AnimationVideoBandes = 0
            FondFlouVideo = True : My.Settings.FondFlouVideo = True
        End If
        BandesTaillePourcent = My.Settings.BandesTaillePourcent
        CleUsbNom = My.Settings.CleUsbNom
        LieuEnLigne = My.Settings.LieuEnLigne
        AfficheLieu = My.Settings.AfficheLieu
        ConvertirAncienneOptionClockBIG()
        User = My.Settings.User
        IPTV_Login = My.Settings.IPTV_login
        IPTV_Password = My.Settings.IPTV_password
        IptvSources.Initialiser()                 ' adresse de la source IPTV active (voir IptvSources)
        GroupePardefault = My.Settings.GroupePardefault
        IPTV_OnlyFavo = My.Settings.IPTV_OnlyFavo
        DossierNflixParDefault = DossierReglages & "\iptv" & ".m3u"
        PlanB = My.Settings.PlanB
        HoraireDeShabbatDansCadran = My.Settings.HoraireDeShabbatDansCadran

        'ChargeRadioList()
        My.Application.DoEvents()
        ChargeDataMinuterieActive()
        ChronoEtape("apres ChargeDataMinuterieActive (base Access)")
        My.Application.DoEvents()
        SaveAllWindows()
        ChronoEtape("apres SaveAllWindows")
        My.Application.DoEvents()

        ' Bouton "Réglages" de l'ecran de demarrage. Il ne marchait que si le clic arrivait AVANT le
        ' chargement des sources (Croix le fait sauter : plus de photos, donc reglages ouverts). Le
        ' lancement ne dure plus que 2 secondes : le clic arrivait presque toujours apres, et il ne se
        ' passait rien (09/10/2026). Les reglages s'ouvrent maintenant dans tous les cas, plus bas.
        My.Application.DoEvents()                              ' un clic encore en attente
        Dim chargementInterrompu As Boolean = Croix            ' le clic a fait sauter (ou a coupe) le chargement

        If Croix = False Then

            Form3.Show()
            Form3.BringToFront()

        End If

        Croix = False

        'If ProcessExist("TheAeroClock") Then DejaClock = True


        If AfficheClock = True And DejaClock = False Then Call AfficheMontre()
        ChronoEtape("apres AfficheMontre")


        My.Application.DoEvents()                              ' un clic sur "Réglages" encore en attente
        Dim reglagesDemandes As Boolean = ReglagesDemandesAuDemarrage
        ReglagesDemandesAuDemarrage = False
        If reglagesDemandes AndAlso Not (NombreDePhoto = 0 And NombreDeVideo = 0) Then
            ' Reglages demandes alors que les sources sont (au moins en partie) chargees.
            DiagLog("Demarrage : reglages demandes sur l'ecran de demarrage -> ouverts" & If(chargementInterrompu, " (chargement des sources interrompu : repris ensuite)", ""))
            Croix = False
            Form4.StartPosition = FormStartPosition.CenterScreen
            Form4.ShowDialog()
            ' (Valides par OK, Slide se relance. On ne revient ici qu'apres "Annuler".)
            OptionChange = False
            Croix = False
            If chargementInterrompu Then Call InitSource()
        ElseIf NombreDePhoto = 0 And NombreDeVideo = 0 Then
            Form3.Hide()
            Form4.StartPosition = FormStartPosition.CenterScreen
            Form4.ShowDialog()
            Call InitSource()
            'Form3.Show()

        End If


        ChronoEtape("init : fin")
        Form3.Timer3.Start()
        ' Lissage du son entre musiques et videos (ecoute le niveau de Slide 20 fois par seconde).
        NiveauSonore.Demarrer()
    End Sub

    ''' <summary>Pas des boucles d'attente. Avant, elles tournaient a vide (DoEvents en rafale,
    ''' des millions de tours par seconde) : un coeur du processeur etait occupe a 100 % pendant
    ''' toute la duree d'affichage de chaque photo. Rendre la main 10 ms par tour ne change rien
    ''' a la reactivite (100 tours par seconde, l'oeil n'y voit rien) et libere le processeur.</summary>
    Public Const PasDattenteMs As Integer = 10

    Public Sub PauseCourte()
        Thread.Sleep(PasDattenteMs)
    End Sub

    ''' <summary>Coeur commun des trois attentes : rend la main a l'interface (DoEvents) et au
    ''' processeur (PauseCourte) jusqu'a la duree voulue, ou jusqu'a FlagExit. Les trois ne
    ''' different que par leur unite et par ce qu'elles font ensuite ; la meme boucle etait
    ''' recopiee dans chacune.</summary>
    Friend Sub AttendreMillisecondes(duree As Double)
        Dim Chrono As Stopwatch = Stopwatch.StartNew()
        My.Application.DoEvents()
        trouve = 0
        a = 0

        Do Until trouve = 1
            My.Application.DoEvents()  ' Faire «contrôle à d'autres processus
            PauseCourte()
            a = a + 1
            If FlagExit = True Then Exit Do
            If Chrono.ElapsedMilliseconds > duree Then trouve = 1
        Loop
    End Sub

    Sub wait(secondes As Double)

        AttendreMillisecondes(secondes * 1000)

        If SansInternet = False And Music = True Then

            CompteurDeReconnexion = CompteurDeReconnexion + 1

            If CompteurDeReconnexion > 30 Then
                If Music_Radio = True Then
                    If MusiqueVLC.MusiqueRadioSansDonnees() Then
                        If ConnectionInternet() = True Then MusiqueVLC.MusiqueJouerRadio(RadioUrl)
                    End If
                End If
                CompteurDeReconnexion = 0
            End If


        End If





        'Call Minuterie()


    End Sub


    ''' <summary>Attente simple, sans la reconnexion de la radio que fait wait().</summary>
    Sub waitExclusif(secondes As Double)
        AttendreMillisecondes(secondes * 1000)
    End Sub

    ''' <summary>Idem, en dixiemes de seconde.</summary>
    Sub waitExclusifEnDixiemeDeSeconde(secondes As Double)
        AttendreMillisecondes(secondes * 100)
    End Sub


    ''' <summary>Photo a l'ecran (redessinee telle quelle apres un passage plein ecran / fenetre).</summary>
    Private _photoAffichee As String = ""

    Sub AffichePhoto(Nom As String)
        _photoAffichee = Nom
        ' Une video est a l'ecran et les videos sont animees : son image reste posee par-dessus
        ' pendant que la photo s'installe, puis se retire (voir FormTransitionVideo).
        If Form3.VlcControl1.Visible Then FormTransitionVideo.Couvrir(Form3)
        PrechargementVideo.Abandonner()      ' retour aux photos : plus de video suivante en attente
        'Form3.PictureBox1.Image = Nothing
        'If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False And Form5.Visible = False Then CacherSouris

        If AfficheDate = True Or AfficheHeureDeShabbat = True Then
            AfficheDateDeLaPhoto(Nom)
        Else
            MasqueLegende()
        End If

        If PhotoAlbum = False Then
            If FormatPhoto169 = True Then Call AffichePhoto169(Nom) Else Call AffichePhoto413(Nom)
        Else
            Call AffichePhotoAlbum(Nom)
        End If


        ' La photo est a l'ecran : les lecteurs video du diaporama rendent leur memoire (jusqu'a
        ' 1,7 Go par video 4K restee en pause, voir Form3.LibererLeLecteur).
        Form3.LibererLesLecteursVideo()

        Call SaveLog(Nom, "PHOTO")

        If NumPhoto > 0 Then
            Form2.DataGridView1.ClearSelection()
            Form2.DataGridView1.Rows(NumPhoto).Selected = True
            If Form2.Visible = True Then
                If Form2.CheckBox1.Checked = False Then
                    If NumPhoto > 6 Then Form2.DataGridView1.FirstDisplayedScrollingRowIndex = NumPhoto - 5
                End If
            End If


        End If




        ' La photo est en place : l'image de la video qu'elle remplace se retire (videos animees).
        FormTransitionVideo.Retirer()
    End Sub

    Sub AffichePhotoAlbum(Nom As String)

        AnimationPhoto.Masquer()             ' (l'album n'est pas anime)


        Form3.PictureBox1.Visible = True
        Form3.VlcControl1.Visible = False

        Form3.PictureBox1.Top = 0
        Form3.PictureBox1.Left = 0
        Form3.PictureBox1.Height = Form3.ClientSize.Height
        Form3.PictureBox1.Width = Form3.ClientSize.Width

        My.Application.DoEvents()

        'On Error GoTo er
        ScreenSizeW = Form3.Width
        ScreenSizeH = Form3.Height
        Dim w As Integer = Int(ScreenSizeW / 2) 'x
        Dim H As Integer = Int(ScreenSizeH / 2) 'y

        'PositionDeLaPhoto(1, y) 0=x 1=y 2=pris en compte 3=Numphoto
        Dim PositionDeLaPhoto(4, 3)
        PositionDeLaPhoto(1, 0) = 0 : PositionDeLaPhoto(1, 1) = 0
        PositionDeLaPhoto(3, 0) = w : PositionDeLaPhoto(3, 1) = 0
        PositionDeLaPhoto(2, 0) = 0 : PositionDeLaPhoto(2, 1) = H + 25
        PositionDeLaPhoto(4, 0) = w : PositionDeLaPhoto(4, 1) = H + 25

        Dim myGraphics As Graphics = Form3.PictureBox1.CreateGraphics
        myGraphics.Clear(Color.Black)

        Dim Portrait As Boolean

        For i = 1 To 4

            If i > 1 Then

                NumPhoto = prochainePhoto()

                If NumPhoto = -1 Then
                    'NumPhoto = 1
                    'ModeRdv = False
                    'If prochain = 1 Then ProchainePhotoForcée = PositionDeLaPhoto(1, 3)
                    Exit Sub
                End If

                Nom = Form2.DataGridView1.Rows(NumPhoto).Cells(3).Value
                'If InStr(Nom, "39381") > 0 Then Stop
                'Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 1
            End If
            Dim myBitmap As New System.Drawing.Bitmap(filename:=Nom)

            ALendroit = ImageOrientation(myBitmap)

            'GoTo suivant
            Select Case (ALendroit)
                Case 3 '180° a l'envers
                    myBitmap.RotateFlip(RotateFlipType.Rotate180FlipNone)
                Case 4 '180° a l'envers
                    myBitmap.RotateFlip(RotateFlipType.Rotate180FlipX)
                Case 5 '90°
                    myBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone)
                Case 6 '90°
                    myBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone)
                Case 8 '270°
                    myBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone)
            End Select

            w2 = myBitmap.Width
            H2 = myBitmap.Height

            Portrait = False : If H2 > w2 Then Portrait = True

            Dispo = 0 : prochain = 0
            For ii = 4 To 1 Step -1
                If PositionDeLaPhoto(ii, 2) = Nothing Then Dispo = Dispo + 1 : prochain = ii
            Next

            If Dispo = 0 Then
                ProchainePhotoForcée = NumPhoto
                Exit For
            End If

            If Portrait = True Then

                If prochain = 4 And PositionDeLaPhoto(3, 2) = 1 Then
                    ProchainePhotoForcée = PositionDeLaPhoto(3, 3)
                    PositionDeLaPhoto(3, 2) = Nothing
                    prochain = 3
                    Form2.DataGridView1.Rows(PositionDeLaPhoto(3, 3)).Cells(0).Value = False
                End If

                If prochain = 2 And PositionDeLaPhoto(3, 2) = 0 Then
                    prochain = 3
                End If

                If prochain = 2 And PositionDeLaPhoto(1, 2) = 1 Then
                    ProchainePhotoForcée = PositionDeLaPhoto(1, 3)
                    PositionDeLaPhoto(1, 2) = Nothing
                    prochain = 1
                    Form2.DataGridView1.Rows(PositionDeLaPhoto(1, 3)).Cells(0).Value = False

                End If

            End If

            trouve = prochain
            PositionDeLaPhoto(trouve, 2) = 1
            PositionDeLaPhoto(trouve, 3) = NumPhoto

            If Portrait = True Then
                If trouve = 1 Or trouve = 3 Then
                    PositionDeLaPhoto(trouve + 1, 2) = 1
                End If
            End If

            ratio = w2 / H2
            ratio2 = w / H

            If ratio < ratio2 Then
                W3 = w
                H3 = w * (1 / ratio)
            Else
                W3 = H * ratio
                H3 = H
            End If

            'MyBitMap2 = ResizeImage(myBitmap, W3 - 5, H3 - 25)
            'myGraphics.DrawImage(MyBitMap2, New RectangleF(PositionDeLaPhoto(trouve, 0), PositionDeLaPhoto(trouve, 1), 500, 500))




            'pour mieux voir les tetes (plus haut) 1/3 2/3
            'myGraphics.DrawImage(image:=myBitmap,
            'destRect:=New Rectangle(x:=PositionDeLaPhoto(trouve, 0), y:=PositionDeLaPhoto(trouve, 0), width:=W3, height:=H3),
            'srcRect:=New Rectangle(x:=0, y:=myBitmap.Height * 0.1, width:=myBitmap.Width * ratio, height:=myBitmap.Height * 0.8),
            'srcUnit:=GraphicsUnit.Pixel)


            'left,top,W,H
            myGraphics.DrawImage(myBitmap, New RectangleF(PositionDeLaPhoto(trouve, 0), PositionDeLaPhoto(trouve, 1), W3 - 5, H3 - 25))
            myBitmap.Dispose()          ' photo dessinee : memoire rendue tout de suite

            Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 1

        Next

fin:
        myGraphics.Dispose()            ' ressource Windows de CreateGraphics

        For y = 1 To Int(DurréeAffichageDuneImage / 3)
            wait(1)
            If FlagExit = True Then Exit For

        Next

    End Sub


    Function ResizeImage(bmSource As Drawing.Bitmap, TargetWidth As Int32, TargetHeight As Int32) As Drawing.Bitmap
        Dim bmDest As New Drawing.Bitmap(TargetWidth, TargetHeight, Drawing.Imaging.PixelFormat.Format32bppArgb)

        Dim nSourceAspectRatio = bmSource.Width / bmSource.Height
        Dim nDestAspectRatio = bmDest.Width / bmDest.Height

        Dim NewX = 0
        Dim NewY = 0
        Dim NewWidth = bmDest.Width
        Dim NewHeight = bmDest.Height

        If nDestAspectRatio = nSourceAspectRatio Then
            'same ratio
        ElseIf nDestAspectRatio > nSourceAspectRatio Then
            'Source is taller
            NewWidth = Convert.ToInt32(Math.Floor(nSourceAspectRatio * NewHeight))
            NewX = Convert.ToInt32(Math.Floor((bmDest.Width - NewWidth) / 2))
        Else
            'Source is wider
            NewHeight = Convert.ToInt32(Math.Floor((1 / nSourceAspectRatio) * NewWidth))
            NewY = Convert.ToInt32(Math.Floor((bmDest.Height - NewHeight) / 2))
        End If

        Using grDest = Drawing.Graphics.FromImage(bmDest)
            With grDest
                .CompositingQuality = Drawing.Drawing2D.CompositingQuality.HighQuality
                .InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                .PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality
                .SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias
                .CompositingMode = Drawing.Drawing2D.CompositingMode.SourceOver

                .DrawImage(bmSource, NewX, NewY, NewWidth, NewHeight)
            End With
        End Using

        Return bmDest
    End Function




    Sub AffichePhoto169(Nom As String)

        'If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False And Form5.Visible = False Then CacherSouris

        Form3.PictureBox1.Visible = True
        Form3.VlcControl1.Visible = False
        'Form3.PictureBox1.Image = Nothing


        Form3.PictureBox1.Top = 0
        Form3.PictureBox1.Left = 0

        Form3.PictureBox1.Height = Form3.ClientSize.Height

        Form3.PictureBox1.Width = Form3.ClientSize.Width

        My.Application.DoEvents()

        ' Photos animees (reglages, onglet Photos) : la photo bouge lentement dans son cadre, la
        ' carte graphique fait le travail (voir AnimationPhoto). Impossible : photo immobile.
        If AnimerLesPhotos AndAlso AnimationPhoto.Afficher(Nom, Form3.PictureBox1, True, DurréeAffichageDuneImage) Then
            OublierLImageImmobile()
            Exit Sub
        End If
        AnimationPhoto.Masquer()

        On Error GoTo er
        'On Error Resume Next

        Dim myBitmap As New System.Drawing.Bitmap(filename:=Nom)
        Dim myGraphics As Graphics = Form3.PictureBox1.CreateGraphics
        myGraphics.Clear(Color.Black)

        'myGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic
        'myGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality

        myGraphics.CompositingQuality = CompositingQuality.HighQuality


        ALendroit = ImageOrientation(myBitmap)

        Select Case (ALendroit)
            Case 3 '180° a l'envers
                myBitmap.RotateFlip(RotateFlipType.Rotate180FlipNone)
            Case 4 '180° a l'envers
                myBitmap.RotateFlip(RotateFlipType.Rotate180FlipX)
            Case 5 '90° '''
                myBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone)
            Case 6 '90°
                myBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone)
            Case 8 '270°
                myBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone)
        End Select

        ' --- Zone d'affichage et bande decoupee dans la photo ---------------------------------
        ' On ne recadre JAMAIS plus que le 16:9 d'origine. Sur un ecran plus large que 16:9
        ' (ultrawide 5120x2160 = 2,37:1), la bande est centree avec des bandes noires laterales
        ' au lieu d'etre etiree : le ratio de la photo est conserve.
        Dim largeurDest As Integer = Form3.PictureBox1.ClientSize.Width
        Dim hauteurDest As Integer = Form3.PictureBox1.ClientSize.Height
        Dim ratioDest As Double = largeurDest / hauteurDest
        Dim ratioBande As Double = Math.Min(SeizeNeuvieme, ratioDest)

        ' Hauteur de la bande : jamais plus haute que la photo (cas d'un panorama deja tres large,
        ' qui n'est alors pas recadre du tout).
        Dim hauteurBande As Integer = CInt(Math.Max(1, Math.Min(myBitmap.Height, myBitmap.Width / ratioBande)))
        ' Decalage vers le haut (1/6 au lieu de la moitie) pour mieux cadrer les visages.
        Dim hautBande As Integer = CInt((myBitmap.Height - hauteurBande) / 6)
        ' Ratio reellement obtenu apres l'eventuel plafonnement ci-dessus.
        Dim ratioBandeReel As Double = myBitmap.Width / hauteurBande

        ratio = myBitmap.Width / myBitmap.Height
        If ratio < 1.1 Then
            'If ratio < 0 Then
            'Form3.PictureBox1.Image = Image.FromFile(Nom)
            DefinirImageAffichee(myBitmap)


        Else
            ' Destination : la bande est mise a l'echelle sans deformation puis centree.
            ' Sur un ecran 16:9 on retombe exactement sur le plein ecran d'avant (dx = dy = 0).
            Dim dw As Integer = largeurDest
            Dim dh As Integer = CInt(largeurDest / ratioBandeReel)
            If dh > hauteurDest Then
                dh = hauteurDest
                dw = CInt(hauteurDest * ratioBandeReel)
            End If
            Dim dx As Integer = (largeurDest - dw) \ 2
            Dim dy As Integer = (hauteurDest - dh) \ 2

            'pour mieux voir les tetes (plus haut) 1/3 2/3
            myGraphics.DrawImage(image:=myBitmap,
            destRect:=New Rectangle(x:=dx, y:=dy, width:=dw, height:=dh),
            srcRect:=New Rectangle(x:=0, y:=hautBande, width:=myBitmap.Width, height:=hauteurBande),
            srcUnit:=GraphicsUnit.Pixel)

            ' Photo dessinee (et non confiee au PictureBox) : on rend la memoire tout de suite.
            myBitmap.Dispose()
        End If

        ' CreateGraphics reserve une ressource Windows a chaque photo : la rendre aussi.
        myGraphics.Dispose()




er:
    End Sub


    ''' <summary>Change la photo affichee et LIBERE la precedente. Sans cela, chaque photo laissait
    ''' son image en memoire jusqu'au prochain passage du ramasse-miettes : sur un diaporama qui
    ''' tourne des heures avec des photos de 10 a 20 Mo, la memoire montait sans raison.</summary>
    Sub DefinirImageAffichee(nouvelle As Image)
        AnimationPhoto.Masquer()             ' autre chose dans la zone de la photo : plus d'animation
        Dim ancienne As Image = Form3.PictureBox1.Image
        Form3.PictureBox1.Image = nouvelle
        If ancienne IsNot Nothing AndAlso Not ReferenceEquals(ancienne, nouvelle) Then
            Try
                ancienne.Dispose()
            Catch ex As Exception
            End Try
        End If
    End Sub

    ''' <summary>La photo animee est a l'ecran : l'image immobile restee dessous (photo precedente)
    ''' est rendue, sans toucher a l'animation.</summary>
    Sub OublierLImageImmobile()
        Dim ancienne As Image = Form3.PictureBox1.Image
        If ancienne Is Nothing Then Exit Sub
        Form3.PictureBox1.Image = Nothing
        Try
            ancienne.Dispose()
        Catch ex As Exception
        End Try
    End Sub

    Sub AffichePhoto413(Nom As String)
        'Form3.PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        'If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False Then CacherSouris
        Form3.PictureBox1.Visible = True


        Form3.VlcControl1.Visible = False

        Form3.PictureBox1.Top = 0
        Form3.PictureBox1.Left = 0
        Form3.PictureBox1.Height = Form3.ClientSize.Height
        Form3.PictureBox1.Width = Form3.ClientSize.Width
        If AnimerLesPhotos AndAlso AnimationPhoto.Afficher(Nom, Form3.PictureBox1, False, DurréeAffichageDuneImage) Then
            OublierLImageImmobile()
            Exit Sub
        End If
        On Error GoTo ER
        'Form3.PictureBox1.Dispose()
        'GC.Collect()
        'Form3.BackColor = Color.Black : Form3.Refresh()
        DefinirImageAffichee(Image.FromFile(Nom))

        Form3.PictureBox1.Refresh()
ER:
    End Sub

    Public Function ZoomImage(ByVal img As System.Drawing.Image, ByVal ZoomValue As Double) As System.Drawing.Image
        Dim width As Int32 = Convert.ToInt32(img.Width * ZoomValue) / 100
        Dim height As Int32 = Convert.ToInt32(img.Height * ZoomValue) / 100

        Dim zoomImage1 As New System.Drawing.Bitmap(img, width, height)
        Dim converted As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(zoomImage1)
        converted.InterpolationMode = InterpolationMode.HighQualityBicubic
        Return zoomImage1
    End Function

    Sub orientation()
        'orientaion du image
        'Me.PictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone)
        'Me.PictureBox1.Refresh()
    End Sub



    Function PhotoVideoInMemory(cheminPhotoVideo)

        ' Chaque etape est chronometree dans le journal ("Chargement de ...") : elle dit ou part le temps d'un
        ' lancement sans "reprendre le diaporama" (13/10/2026 : 19 s avant la premiere photo).
        Dim chronoCharge As Stopwatch = Stopwatch.StartNew()
        Dim etapesCharge As New List(Of String)
        Dim noterCharge As Action(Of String) = Sub(quoi As String)
                                                   etapesCharge.Add(quoi & " " & chronoCharge.ElapsedMilliseconds & " ms")
                                                   chronoCharge.Restart()
                                               End Sub

        Opt = "1111"   ' actif/photo/video/music
        op = InStr(cheminPhotoVideo, "@")
        If op > 0 Then
            Opt = Mid(cheminPhotoVideo, op + 1)
            cheminPhotoVideo = Mid(cheminPhotoVideo, 1, op - 1)
        End If
        OptActif = Mid(Opt, 1, 1)

        If OptActif <> "1" Then
            Return 0
            Exit Function
        End If

        OptPhoto = Mid(Opt, 2, 1)
        OptVideo = Mid(Opt, 3, 1)
        OptMusic = Mid(Opt, 4, 1)


        'Suppression des repertoires Goodsync
        Dim DirToRemove As String() = {"_gsdata_"}  'Dim names As String() = {"Dir_B", "Dir_C"}

        Dim dir2 As New System.IO.DirectoryInfo(cheminPhotoVideo)
        Dim root = cheminPhotoVideo
        Try
            My.Computer.FileSystem.DeleteDirectory(root & "\_gsdata_", FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
        Catch ex As Exception

        End Try

        For Each d In New DirectoryInfo(root).EnumerateDirectories()
            For Each n In DirToRemove
                Dim f = New DirectoryInfo(Path.Combine(d.FullName, n))
                Try
                    f.Delete(recursive:=True)
                Catch exc As DirectoryNotFoundException
                    ' ignore
                End Try
            Next
        Next



        ' Des dossiers viennent d'etre supprimes juste au-dessus : on rescanne.
        OublierScan()
        noterCharge("_gsdata_")
        Dim photoVideo = FichiersDuDossier(CStr(cheminPhotoVideo))
        noterCharge("scan")
        'If Croix = True Then Exit Function
        'My.Application.DoEvents()

        If OptPhoto = "1" Then

            Dim ListePhoto = From file In photoVideo
                             Where file.Extension.ToLower = ".jpg" Or file.Extension.ToLower = ".jpeg" Or file.Extension.ToLower = ".png"
                             Order By file.LastWriteTime Descending
                             Select file.Name, file.LastWriteTime, file.FullName

            My.Application.DoEvents()
            ' Chargement rapide (13/10/2026 : "quand reprendre le diaporama n'est pas coche, Slide met beaucoup
            ' de temps a se lancer" ; les 54 000 photos prenaient 11,9 s) : les colonnes viennent d'une liste
            ' VIDE du meme type, les lignes sont creees puis ajoutees d'un coup. Avant : liste liee au tableau,
            ' puis une ligne ajoutee et un DoEvents par photo.
            Dim photosListe = ListePhoto.ToList()
            Form2.DataGridView3.DataSource = photosListe.Take(0).ToList()

            My.Application.DoEvents()
            CaseAcocherVide(3)
            If Croix = True Then Exit Function
            My.Application.DoEvents()
            For Each Col As DataGridViewColumn In Form2.DataGridView3.Columns
                Form2.DataGridView1.Columns.Add(DirectCast(Col.Clone, DataGridViewColumn))
            Next

            Dim lignesPhoto As New List(Of DataGridViewRow)(photosListe.Count)
            For Each unePhoto In photosListe
                Dim majuscules As String = UCase(unePhoto.Name)
                If InStr(majuscules, "ALBUMART") > 0 OrElse InStr(majuscules, "FOLDER") > 0 Then Continue For
                Dim ligne As New DataGridViewRow()
                ligne.CreateCells(Form2.DataGridView1, False, unePhoto.Name, unePhoto.LastWriteTime, unePhoto.FullName)
                lignesPhoto.Add(ligne)
            Next
            If Croix = True Then Exit Function
            Form2.DataGridView1.Rows.AddRange(lignesPhoto.ToArray())





            NombreDePhoto = Form2.DataGridView1.RowCount
        End If
        noterCharge("photos")

        My.Application.DoEvents()

        If OptVideo = "1" Then

            Form2.DataGridView3.Columns.Clear()
            Try
                Form2.DataGridView3.DataSource.clear
            Catch ex As Exception

            End Try


            'PhotoVideoInMemory = 0
            BarProgressTotal = BarProgressTotal + BarProgress : Form1.ProgressBar1.Value = BarProgressTotal
            ''''''

            Dim ListeVideo = From file In photoVideo
                             Where file.Extension.ToLower = ".mkv" Or file.Extension.ToLower = ".avi" Or file.Extension.ToLower = ".mpg" _
                            Or file.Extension.ToLower = ".mpeg" Or file.Extension.ToLower = ".mpe" Or file.Extension.ToLower = ".wmv" _
                            Or file.Extension.ToLower = ".asf" Or file.Extension.ToLower.ToLower = ".mov" _
                            Or file.Extension.ToLower = ".qt" Or file.Extension.ToLower = ".movie" Or file.Extension.ToLower = ".mp4" _
                            Or file.Extension.ToLower = ".divx" Or file.Extension.ToLower = ".xvid" Or file.Extension.ToLower = ".ts" _
                            Or file.Extension.ToLower = ".3gp"
                             Order By file.LastWriteTime Descending
                             Select file.Name, file.LastWriteTime, file.FullName

            My.Application.DoEvents()
            Form2.DataGridView3.DataSource = ListeVideo.ToList

            My.Application.DoEvents()
            CaseAcocherVide(3)


            For Each Col As DataGridViewColumn In Form2.DataGridView3.Columns
                If Croix = True Then Exit Function
                My.Application.DoEvents()
                Form2.DataGridView2.Columns.Add(DirectCast(Col.Clone, DataGridViewColumn))
            Next
            'Form2.DataGridView2.Columns(0).ValueType = GetType(Boolean)
            'If Croix = True Then Exit Function
            'My.Application.DoEvents()

            For rowIndex As Integer = 0 To (Form2.DataGridView3.Rows.Count - 1)
                Form2.DataGridView2.Rows.Add(Form2.DataGridView3.Rows(rowIndex).Cells.Cast(Of DataGridViewCell).Select(Function(c) c.Value).ToArray)
            Next
        End If

        noterCharge("videos")
        My.Application.DoEvents()

        If OptMusic = "1" Then

            Dim ListeMP3 = From file In photoVideo
                           Where file.Extension.ToLower = ".mp3" Or file.Extension.ToLower = ".m4a" Or file.Extension.ToLower = ".wav" _
                                Or file.Extension.ToLower = ".flac" Or file.Extension.ToLower = ".wma" _
                                Or file.Extension.ToLower = ".wmv" Or file.Extension.ToLower = ".asf" Or file.Extension.ToLower.ToLower = ".aac" _
                                Or file.Extension.ToLower = ".aa3"
                           Select file.FullName



            For Each Chanson As String In ListeMP3.ToList
                If Croix = True Then Exit Function
                My.Application.DoEvents()
                MusiqueVLC.MusiqueAjouter(Chanson)
            Next

            MusiqueVLC.MusiqueMelanger()


        End If

        Form2.DataGridView3.Columns.Clear()
        'Form2.DataGridView3.DataSource.clear




        My.Application.DoEvents()
        NombreDeVideo = Form2.DataGridView2.RowCount
        If NombreDeVideo <= 0 Then NombreDeVideo = 0
        Form2.Label1.Text = (NombreDePhoto).ToString + " Photos"
        Form2.Label2.Text = (NombreDeVideo).ToString + " Videos"
        Form2.Label3.Text = (MusiqueVLC.MusiqueNombre()).ToString + " Musics"
        noterCharge("musique")
        DiagLog("Chargement de " & cheminPhotoVideo & " : " & String.Join(" | ", etapesCharge) & " (" & NombreDePhoto & " photos, " & NombreDeVideo & " videos)")

    End Function





    Sub CaseAcocherVide(x As Integer)
        'Form2.Show()
        'Form2.Refresh()
        Application.DoEvents()
        Dim DataGrid As DataGridView

        DataGrid = Form2.DataGridView1

        Select Case x
            Case 1
                DataGrid = Form2.DataGridView1
                DataGrid.AutoGenerateColumns = False
                Call SaveLog("*** RAZ DECOCHAGE ", "PHOTO")
            Case 2
                DataGrid = Form2.DataGridView2
                DataGrid.AutoGenerateColumns = False
                Call SaveLog("*** RAZ DECOCHAGE ", "VIDEO")
            Case 3
                DataGrid = Form2.DataGridView3

        End Select


        If DataGrid.Columns(0).GetType.Name = "DataGridViewCheckBoxColumn" Then
            DataGrid.Columns.Remove(DataGrid.Columns(0).Name)
        End If


        Dim chk1 As New DataGridViewCheckBoxColumn()
        DataGrid.Columns.Insert(0, chk1)
        chk1.HeaderText = "Check"
        chk1.Name = "chk"
        chk1.ReadOnly = True
        DataGrid.Columns(0).Width = 30
        DataGrid.ClearSelection()
        DataGrid.Refresh()



    End Sub



    ' ------------------------------------------------------------------------------------------
    ' Scan des dossiers : InitSource interrogeait QUATRE fois de suite le meme dossier (comptage
    ' des photos, des videos, des musiques, puis chargement), chaque fois avec un parcours
    ' recursif complet. Le dernier parcours est donc garde en memoire et reservi aux appels
    ' suivants sur le meme dossier.
    ' ------------------------------------------------------------------------------------------

    Private _scanChemin As String = ""
    Private _scanFichiers As IO.FileInfo() = Nothing

    Public Function FichiersDuDossier(chemin As String) As IO.FileInfo()
        If _scanFichiers IsNot Nothing AndAlso String.Equals(chemin, _scanChemin, StringComparison.OrdinalIgnoreCase) Then
            Return _scanFichiers
        End If
        _scanFichiers = New IO.DirectoryInfo(chemin).GetFiles("*.*", IO.SearchOption.AllDirectories)
        _scanChemin = chemin
        Return _scanFichiers
    End Function

    ''' <summary>A appeler quand le contenu du dossier a change (suppression des _gsdata_).</summary>
    Public Sub OublierScan()
        _scanFichiers = Nothing
        _scanChemin = ""
    End Sub

    ''' <summary>Nombre de fichiers ayant une de ces extensions (comparaison sans casse).</summary>
    Private Function CompteExtensions(fichiers As IO.FileInfo(), extensions As String()) As Integer
        Return fichiers.Count(Function(f) extensions.Contains(f.Extension.ToLowerInvariant()))
    End Function

    Friend ReadOnly ExtensionsPhoto As String() = {".jpg", ".jpeg", ".png"}
    Friend ReadOnly ExtensionsVideo As String() = {".mkv", ".avi", ".mpg", ".mpeg", ".mpe", ".wmv", ".asf", ".mov",
                                                   ".qt", ".movie", ".mp4", ".divx", ".xvid", ".ts", ".3gp"}
    Friend ReadOnly ExtensionsMusique As String() = {".mp3", ".m4a", ".wav", ".flac", ".wma", ".wmv", ".asf", ".aac", ".aa3"}

    Function NBPhotosInDir(cheminPhotoVideo)

        opt = "1111"
        op = InStr(cheminPhotoVideo, "@")
        If op > 0 Then
            opt = Mid(cheminPhotoVideo, op + 1)
            cheminPhotoVideo = Mid(cheminPhotoVideo, 1, op - 1)
        End If
        OptActif = Mid(opt, 1, 1)
        OptPhoto = Mid(opt, 2, 1)
        If OptActif <> 1 Or OptPhoto <> 1 Then
            Return 0
            Exit Function
        End If



        Try
            ' Un simple comptage : pas besoin de trier ni de construire la liste.
            NBPhotosInDir = CompteExtensions(FichiersDuDossier(CStr(cheminPhotoVideo)), ExtensionsPhoto)
        Catch ex As Exception
            NBPhotosInDir = 0
        End Try



    End Function

    Function NBVideosInDir(cheminPhotoVideo)

        opt = "1111"
        op = InStr(cheminPhotoVideo, "@")
        If op > 0 Then
            opt = Mid(cheminPhotoVideo, op + 1)
            cheminPhotoVideo = Mid(cheminPhotoVideo, 1, op - 1)
        End If
        OptActif = Mid(opt, 1, 1)
        OptVideo = Mid(opt, 3, 1)
        If OptActif <> 1 Or OptVideo <> 1 Then
            Return 0
            Exit Function
        End If


        Try
            NBVideosInDir = CompteExtensions(FichiersDuDossier(CStr(cheminPhotoVideo)), ExtensionsVideo)
        Catch ex As Exception
            NBVideosInDir = 0
        End Try

    End Function

    Function NBMusicInDir(cheminPhotoVideo)

        opt = "1111"
        op = InStr(cheminPhotoVideo, "@")
        If op > 0 Then
            opt = Mid(cheminPhotoVideo, op + 1)
            cheminPhotoVideo = Mid(cheminPhotoVideo, 1, op - 1)
        End If
        OptActif = Mid(opt, 1, 1)
        OptMusic = Mid(opt, 4, 1)
        If OptActif <> 1 Or OptMusic <> 1 Then
            Return 0
            Exit Function
        End If



        Try
            NBMusicInDir = CompteExtensions(FichiersDuDossier(CStr(cheminPhotoVideo)), ExtensionsMusique)
        Catch ex As Exception
            NBMusicInDir = 0
        End Try



    End Function

    Function prochainePhoto()

        If RDV_ProchainNumPhotoForcée <> -1 Then
            Num = NumPhoto
            Exit Function
        End If


        Randomize()

        If ProchainePhotoForcée <> 0 Then
            prochainePhoto = ProchainePhotoForcée
            ProchainePhotoForcée = 0
            Exit Function
        End If
        If ModeRdv Then
            Num = prochainePhoto_ou_Video()
            If IsNothing(Num) Then GoTo suite

            If ModeRdvType = "PHOTO" Then
                prochainePhoto = Num
                Form2.DataGridView1.Rows(prochainePhoto).Cells(0).Value = 1
                Exit Function
            End If

            If ModeRdvType = "VIDEO" Then
                prochainePhoto = -1
                Exit Function
            End If
suite:
        End If

        n = 0
        If PhotoAleatoire = 0 Then n = 0
        If PhotoAleatoire = 1 Then n = 1
        If PhotoAleatoire = 2 And NouveauCycle = True Then n = 1 : NouveauCycle = False
        If PhotoAleatoire = 3 And NouveauCycle = True Then n = 2 : NouveauCycle = False

        Select Case n
            Case 0
                NumPhoto = NumPhoto + 1
                If NumPhoto <= NombreDePhoto - 1 Then
                    If Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 0 Then
                        Exit Select
                    End If
                End If

                trouve = 0
                For j = 0 To NombreDePhoto - 1
                    If Form2.DataGridView1.Rows(j).Cells(0).Value = 0 Then trouve = 1 : NumPhoto = j : Exit For
                Next
                If trouve = 0 Then CaseAcocherVide(1) : NumPhoto = 0


            Case 1

                trouve = 0 : i = 0
                Do Until trouve = 1
                    NumPhoto = Generator.Next(0, NombreDePhoto - 1)
                    If Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 0 Then trouve = 1 : Exit Do
                    i = i + 1 : If i > 1000 Then Exit Do
                Loop

                If trouve = 0 Then
                    i = 0
                    For i = 0 To NombreDePhoto - 1
                        If Form2.DataGridView1.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumPhoto = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(1) : NumPhoto = 0
                End If

            Case 2

                trouve = 0 : i = 0
                iDiff = DateDiff("d", DatePhotoMinimum, DatePhotoMaximum, vbMonday)
                Dim RndDate As Date
                'Randomize()
                RndDate = DateAdd("d", Int(Generator.Next(0, iDiff)), DatePhotoMinimum)
                For i = 0 To Form2.DataGridView1.Rows.Count - 1
                    If RndDate >= Form2.DataGridView1.Rows(i).Cells(2).Value Then
                        If Form2.DataGridView1.Rows(i).Cells(0).Value = 0 Then NumPhoto = i : trouve = 1 : Exit For
                    End If
                Next
                If trouve = 0 Then
                    i = 0
                    For i = 0 To NombreDePhoto - 1
                        If Form2.DataGridView1.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumPhoto = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(1) : NumPhoto = 0
                End If

        End Select

        prochainePhoto = NumPhoto


    End Function

    Function prochainePhoto_ou_Video()
        ' Mode RDV : element suivant de la periode (photo ou video). Voir ModeRendezVous : la liste
        ' de la periode est dressee une fois, triee par date, puis simplement parcourue.
        Return ModeRendezVous.RdvSuivant()
    End Function



    Function prochainePhoto_OK()

        n = 0
        If PhotoAleatoire = 0 Then n = 0
        If PhotoAleatoire = 1 Then n = 1
        If PhotoAleatoire = 2 And NouveauCycle = True Then n = 1 : NouveauCycle = False

        Select Case n
            Case 0
                NumPhoto = NumPhoto + 1
                If NumPhoto <= NombreDePhoto Then
                    If Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 0 Then
                        Exit Select
                    End If
                End If

                trouve = 0
                For j = 0 To NombreDePhoto
                    If Form2.DataGridView1.Rows(j).Cells(0).Value = 0 Then trouve = 1 : NumPhoto = j : Exit For
                Next
                If trouve = 0 Then CaseAcocherVide(1) : NumPhoto = 0


            Case 1

                trouve = 0 : i = 0
                Do Until trouve = 1
                    NumPhoto = Generator.Next(0, NombreDePhoto)
                    If Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 0 Then trouve = 1 : Exit Do
                    i = i + 1 : If i > 1000 Then Exit Do
                Loop

                If trouve = 0 Then
                    i = 0
                    For i = 0 To NombreDePhoto
                        If Form2.DataGridView1.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumPhoto = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(1) : NumPhoto = 0
                End If
        End Select

        prochainePhoto_OK = NumPhoto


    End Function


    Function ProchaineVideo()

        Randomize()

        If ModeRdv Then
            NUM = prochainePhoto_ou_Video()
            ' RDV termine ou impossible (ModeRdv vient d'etre remis a False) : choix normal ci-dessous.
            ' Avant, on continuait avec l'ancien ModeRdvType et la video n. 0 etait jouee.
            If IsNothing(NUM) OrElse Not ModeRdv Then GoTo choixNormal
            If ModeRdvType = "VIDEO" Then
                ProchaineVideo = NUM
                Form2.DataGridView2.Rows(ProchaineVideo).Cells(0).Value = 1
                Exit Function
            End If

            If ModeRdvType = "PHOTO" Then
                ProchaineVideo = -1
                Exit Function
            End If

        End If
choixNormal:


        If VideoAleatoire = True Then

            If PhotoAleatoire = 3 Then


                trouve = 0 : i = 0
                iDiff = DateDiff("d", DateVideoMinimum, DateVideoMaximum, vbMonday)
                Dim RndDate As Date
                'Randomize()
                RndDate = DateAdd("d", Int(Generator.Next(0, iDiff)), DateVideoMinimum)
                For i = 0 To Form2.DataGridView2.Rows.Count - 1
                    If RndDate >= Form2.DataGridView2.Rows(i).Cells(2).Value Then
                        If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then NumVideo = i : trouve = 1 : Exit For
                    End If
                Next

                If trouve = 0 Then
                    i = 0
                    For i = 0 To NombreDeVideo - 1
                        If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideo = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(2) : NumVideo = 0
                End If



            Else

                trouve = 0 : i = 0
                Do Until trouve = 1
                    NumVideo = Generator.Next(0, NombreDeVideo)
                    If Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 0 Then trouve = 1 : Exit Do
                    i = i + 1 : If i > 1000 Then Exit Do
                Loop

                If trouve = 0 Then
                    i = 0
                    For i = 0 To NombreDeVideo - 1
                        If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideo = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(2) : NumVideo = 0
                End If
            End If


        Else

            NumVideo = NumVideo + 1
            If NumVideo < 0 Then NumVideo = 0
            If NumVideo > NombreDeVideo - 1 Then
                i = 0 : trouve = 0
                For i = 0 To NombreDeVideo - 1
                    If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideo = i : Exit For
                Next
                If trouve = 0 Then CaseAcocherVide(2) : NumVideo = 0 : ProchaineVideo = NumVideo : Exit Function

            Else
                If Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1 Then
                    i = 0 : trouve = 0
                    For i = 0 To NombreDeVideo - 1
                        If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideo = i : Exit For
                    Next
                    If trouve = 0 Then CaseAcocherVide(2) : NumVideo = 0 : ProchaineVideo = NumVideo : Exit Function

                End If

            End If
        End If

        ProchaineVideo = NumVideo


    End Function

    Function ProchaineVideoSuivante()



        NumVideoProv = NumVideo + 1

        If NumVideoProv > NombreDeVideo - 1 Then
            i = 0 : trouve = 0
            For i = 0 To NombreDeVideo - 1
                If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideoProv = i : Exit For
            Next
            If trouve = 0 Then NumVideoProv = 0 : ProchaineVideoSuivante = NumVideoProv : Exit Function

        Else
            If Form2.DataGridView2.Rows(NumVideoProv).Cells(0).Value = 1 Then
                trouve = 0
                For i = NumVideoProv To NombreDeVideo - 1
                    If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideo = i : Exit For
                Next
                If trouve = 0 Then
                    For i = 0 To NumVideoProv - 1
                        If Form2.DataGridView2.Rows(i).Cells(0).Value = 0 Then trouve = 1 : NumVideoProv = i : Exit For
                    Next
                End If

                If trouve = 0 Then NumVideoProv = 0 : ProchaineVideoSuivante = NumVideoProv : Exit Function
            End If

        End If


        ProchaineVideoSuivante = NumVideoProv


    End Function



    ''' <summary>Trace de migration .NET 10 : ecrit dans data\MigrationDiag.log.
    ''' A retirer quand l'application sera stabilisee.</summary>
    ''' <summary>
    ''' Nom du fournisseur OLE DB a utiliser pour ouvrir slide.accdb sur ce PC.
    '''
    ''' On interroge la base de registre, comme le fait Slide.bat avant de lancer le programme.
    ''' Le 16.0 passe en premier : c'est celui des Office recents, et sur ces machines le 12.0
    ''' est souvent absent. Si aucun des deux n'est inscrit, on renvoie quand meme le 12.0 :
    ''' le message d'erreur d'OleDb nommera alors ce qui manque et ce que Runtime\ installe.
    ''' </summary>
    Public Function FournisseurAccess() As String
        For Each nom As String In {"Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0"}
            Try
                Using cle As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(nom)
                    If cle IsNot Nothing Then
                        DiagLog("Base Access : fournisseur " & nom)
                        Return nom
                    End If
                End Using
            Catch ex As Exception
                ' registre illisible : on essaie le nom suivant
            End Try
        Next
        DiagLog("Base Access : aucun fournisseur ACE inscrit sur ce PC (Runtime\accessdatabaseengine_X64.exe l'installe)")
        Return "Microsoft.ACE.OLEDB.12.0"
    End Function

    Private ReadOnly _verrouDuJournal As New Object

    Public Sub DiagLog(message As String)
        Try
            ' Une ecriture a la fois : deux fils qui ecrivaient au meme instant se genaient, et l'une des
            ' deux lignes etait perdue (13/10/2026 : la ligne "apres Form2.Hide" du demarrage a disparu,
            ' la relecture des listes en tache de fond ecrivait la sienne au meme moment).
            Dim ligne As String = Date.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") & " ; " & message & vbCrLf
            SyncLock _verrouDuJournal
                IO.File.AppendAllText(DossierReglages & "\MigrationDiag.log", ligne)
            End SyncLock
        Catch
            ' le diagnostic ne doit jamais faire tomber l'application
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Cache de plugins de VLC
    '
    ' Sans le fichier plugins\plugins.dat, libvlc ouvre et inspecte ses ~360 DLL de plugins une
    ' par une a chaque demarrage. Le dossier etant sur un lecteur reseau, cela coutait ~16 s a la
    ' creation du premier VlcControl (Form2), soit l'essentiel du temps de lancement.
    ' On regenere donc le cache au demarrage quand il manque ou qu'il est perime.
    ' ------------------------------------------------------------------------------------------

    Private Function DossierVLC() As String
        Return AppPath & "\data\VLC"
    End Function

    ''' <summary>Vrai si le cache de plugins de VLC est absent, invalide ou perime.</summary>
    Public Function CacheVLCaRegenerer() As Boolean
        Try
            Dim dossierPlugins As New IO.DirectoryInfo(IO.Path.Combine(DossierVLC(), "plugins"))
            If Not dossierPlugins.Exists Then Return False
            If Not IO.File.Exists(IO.Path.Combine(DossierVLC(), "vlc-cache-gen.exe")) Then Return False

            Dim cache As New IO.FileInfo(IO.Path.Combine(dossierPlugins.FullName, "plugins.dat"))
            If Not cache.Exists Then Return True

            ' Un cache genere avec un chemin RELATIF fait 25 octets et ne sert a rien :
            ' vlc-cache-gen exige un chemin absolu, sans le signaler.
            If cache.Length < 1000 Then Return True

            ' Perime si VLC a ete mis a jour. On ne compare PAS avec la date du dossier plugins :
            ' y ecrire plugins.dat met justement cette date a jour, ce qui declencherait une
            ' regeneration a chaque lancement.
            Dim coeur As New IO.FileInfo(IO.Path.Combine(DossierVLC(), "libvlc.dll"))
            If coeur.Exists AndAlso coeur.LastWriteTimeUtc > cache.LastWriteTimeUtc Then Return True

            Return False
        Catch
            Return False   ' dans le doute on ne regenere pas : au pire le demarrage reste lent
        End Try
    End Function

    ''' <summary>Genere le cache de plugins de VLC. Bloquant : ~20 s sur un lecteur reseau a froid,
    ''' mais une seule fois, et c'est le temps que libvlc passerait de toute facon a chaque
    ''' demarrage sans cache. A appeler AVANT la creation du premier VlcControl (Form2).</summary>
    Public Sub GenereCacheVLC()
        Try
            Dim dossierPlugins As String = IO.Path.Combine(DossierVLC(), "plugins")
            Dim chrono As Stopwatch = Stopwatch.StartNew()

            Dim demarrage As New ProcessStartInfo(IO.Path.Combine(DossierVLC(), "vlc-cache-gen.exe"),
                                                  """" & dossierPlugins & """") With {
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .WorkingDirectory = DossierVLC()
            }

            Using generateur As Process = Process.Start(demarrage)
                If Not generateur.WaitForExit(180000) Then
                    generateur.Kill()
                    DiagLog("Cache VLC : abandon apres 3 minutes")
                    Exit Sub
                End If
            End Using

            Dim taille As Long = 0
            Dim cache As New IO.FileInfo(IO.Path.Combine(dossierPlugins, "plugins.dat"))
            If cache.Exists Then taille = cache.Length
            DiagLog("Cache VLC genere en " & chrono.ElapsedMilliseconds & " ms (" & taille & " octets)")

        Catch ex As Exception
            ' Jamais bloquant : sans cache l'application demarre seulement plus lentement.
            DiagLog("Cache VLC : echec (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Chronometrage du demarrage : temps cumule depuis le premier appel.
    ''' Temporaire, a retirer avec DiagLog.</summary>
    Private ChronoDemarrage As Stopwatch
    Private DerniereEtapeMs As Long
    Public Sub ChronoEtape(etape As String)
        If ChronoDemarrage Is Nothing Then ChronoDemarrage = Stopwatch.StartNew()
        Dim total As Long = ChronoDemarrage.ElapsedMilliseconds
        DiagLog("CHRONO total=" & total.ToString().PadLeft(6) & " ms  (+" &
                (total - DerniereEtapeMs).ToString().PadLeft(6) & " ms)  " & etape)
        DerniereEtapeMs = total
    End Sub

    Sub SaveLogError(Nom, typeErreur)

        Dim path As String = DossierReglages & "\Error.log"

        If File.Exists(path) = False Then
            File.WriteAllText(path, Now + " ; " + Nom + " ; " + typeErreur + Environment.NewLine)
        Else
            File.AppendAllText(path, Now + " ; " + Nom + " ; " + typeErreur + Environment.NewLine)
        End If



    End Sub
    Sub SaveLog(Nom, TP)

        Dim path As String = DossierReglages & "\Log.log"

        If File.Exists(path) = False Then
            File.WriteAllText(path, Now + " ; " + Nom + " ; " + TP + Environment.NewLine)
        Else
            File.AppendAllText(path, Now + " ; " + Nom + " ; " + TP + Environment.NewLine)
        End If



    End Sub
    Sub SaveVersion(NomAppli, ver)
        NomAppli = UCase(Left(NomAppli, 1)) & Mid(NomAppli, 2)
        Chemin = AppPath
        p = InStr(Chemin, "\" & NomAppli & "\bin")
        If p = 0 Then Exit Sub

        Try
            ' Le dossier marqueur est cree DANS le dossier du projet (demande de Jacob) :
            ' ...\Slide\Slide Version 8.122, et non plus a cote, dans le dossier parent.
            ' p pointe le "\" qui precede le nom : on garde donc p + la longueur du nom.
            Pathh = Mid(Chemin, 1, p + Len(NomAppli))
            Dim dirs As String() = System.IO.Directory.GetDirectories(Pathh, NomAppli & " Version *")
            If dirs.Length <> 1 Then
                My.Computer.FileSystem.CreateDirectory(Pathh & "\" & NomAppli & " Version " & ver)

                Exit Sub
            End If
            name = dirs(0)
            My.Computer.FileSystem.RenameDirectory(name, NomAppli & " Version " & ver)

        Catch ex As Exception

        End Try


    End Sub




    ''' <summary>Taille et position de la fenetre juste avant le passage en plein ecran, rendues a
    ''' la sortie. Avant : la position n'etait pas gardee (fenetre recentree) et la taille etait
    ''' forcee a 800 x 450 au minimum (une fenetre de 506 x 322 revenait plus grande).</summary>
    Private _fenetreAvantPleinEcran As Rectangle = Rectangle.Empty

    Private Function EstEnPleinEcran() As Boolean
        Return Form3.WindowState = FormWindowState.Maximized AndAlso Form3.FormBorderStyle = FormBorderStyle.None
    End Function

    ''' <summary>Place de la fenetre retenue au dernier lancement (Form3.RetenirLaPlace), ou Nothing
    ''' si elle n'a jamais ete retenue ou si elle ne tombe plus sur aucun ecran (ecran debranche,
    ''' resolution changee) : la fenetre est alors centree comme avant.</summary>
    Function PlaceEnregistree() As Rectangle?
        Return PlaceSiVisible(My.Settings.ScreenPosX, My.Settings.ScreenPosY, My.Settings.ScreenSizeW, My.Settings.ScreenSizeH)
    End Function

    Private Const PlaceJamaisRetenue As Integer = -100000

    Function PlaceSiVisible(x As Integer, y As Integer, largeur As Integer, hauteur As Integer) As Rectangle?
        If x = PlaceJamaisRetenue OrElse y = PlaceJamaisRetenue Then Return Nothing
        If largeur <= 0 OrElse hauteur <= 0 Then                 ' taille jamais retenue : celle par defaut (voir init)
            largeur = 800
            hauteur = 450 + tailleBordureFenetre
        End If
        Dim place As New Rectangle(x, y, largeur, hauteur)
        ' Assez de fenetre dans un ecran pour la voir et l'attraper (un quart au moins).
        For Each ecran As Screen In Screen.AllScreens
            Dim visible As Rectangle = Rectangle.Intersect(ecran.WorkingArea, place)
            If visible.Width >= largeur \ 4 AndAlso visible.Height >= hauteur \ 4 Then Return place
        Next
        Return Nothing
    End Function

    Sub FullScreen_SWITCH_ON(x As Boolean)


        If x = True Then
            ' Appelee aussi a chaque video en plein ecran : on ne retient la fenetre qu'a l'ENTREE.
            If Not EstEnPleinEcran() AndAlso Form3.WindowState = FormWindowState.Normal Then
                _fenetreAvantPleinEcran = Form3.Bounds
            End If
            Form3.Top = 0 : Form3.Left = 0
            Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            Form3.WindowState = FormWindowState.Maximized
            Form3.BackColor = Color.Black

            Form3.VlcControl1.Top = 0
            Form3.VlcControl1.Left = 0
            Form3.VlcControl1.Height = Form3.ClientSize.Height
            Form3.VlcControl1.Width = Form3.ClientSize.Width
            Form3.FullScreenToolStripMenuItem.Checked = True



        Else

            ' Appelee aussi a chaque video en mode fenetre : on ne touche a la taille et a la
            ' position qu'en SORTANT du plein ecran.
            Dim sortDuPleinEcran As Boolean = Form3.WindowState = FormWindowState.Maximized

            If Borderless = True Then Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None Else Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
            Form3.WindowState = FormWindowState.Normal

            If sortDuPleinEcran Then
                Dim avant As Rectangle = _fenetreAvantPleinEcran
                If Not avant.IsEmpty AndAlso Screen.AllScreens.Any(Function(s) s.WorkingArea.IntersectsWith(avant)) Then
                    Form3.Bounds = avant                                  ' taille ET position d'avant
                Else
                    ' Lance directement en plein ecran (ou ecran debranche) : taille enregistree,
                    ' centree sur l'ecran de la fenetre.
                    w = My.Settings.ScreenSizeW
                    h = My.Settings.ScreenSizeH
                    If w < 320 Or h < 180 Then
                        w = 800
                        h = 450 + tailleBordureFenetre
                    End If
                    Dim zone As Rectangle = Screen.FromControl(Form3).WorkingArea
                    Form3.Bounds = New Rectangle(zone.Left + (zone.Width - w) \ 2, zone.Top + (zone.Height - h) \ 2, w, h)
                End If
            End If

            Form3.BackColor = Color.Black

            Form3.VlcControl1.Top = 0
            Form3.VlcControl1.Left = 0
            Form3.VlcControl1.Height = Form3.ClientSize.Height
            Form3.VlcControl1.Width = Form3.ClientSize.Width
            Form3.FullScreenToolStripMenuItem.Checked = False

        End If



    End Sub

    ''' <summary>
    ''' Execute une tache du type "Deezer" de la minuterie. Avec le Deezer integre a Slide, tout
    ''' passe par MusiqueDeezer ; avec l'application Deezer externe, par les anciens moyens
    ''' (lancement de l'application, clics et touches de GestionControlAdistanceSub2).
    ''' </summary>
    ''' <param name="volume">Colonne Volume de la ligne ("Lance Deezer" et "Volume Deezer").</param>
    ''' <param name="argument">Colonne Argument : "!/Playlist:adresse" pour "Lance Deezer".</param>
    Sub TacheDeezer(action As String, volume As Object, argument As String)
        Select Case UCase(If(action, "").Trim())
            Case "LANCE DEEZER", "DEEZER"               ' ("Deezer" : ancienne action de Music Source)
                ' La musique devient Deezer, au volume de la ligne, avec la playlist de la ligne.
                If MusiqueDeezer.Interne Then DeezerParLaMinuterie(volume, CStr(Arg("Playlist", argument))) Else LanceDeezer()
            Case "DEEZER FAVORIS"
                ' La musique devient Deezer, sur la playlist "Coups de coeur" du compte (08/10).
                If MusiqueDeezer.Interne Then
                    DeezerParLaMinuterie(volume, MusiqueDeezer.Favoris)
                Else
                    DiagLog("Minuterie : Deezer Favoris demande le Deezer integre (l'application externe est seulement lancee)")
                    LanceDeezer()
                End If
            Case "DEEZER PLAY/PAUSE"
                If MusiqueDeezer.Interne Then MusiqueDeezer.Basculer() Else GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Pause")
            Case "DEEZER PLAY"
                If MusiqueDeezer.Interne Then MusiqueDeezer.LectureSurDemande() Else GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Play")
            Case "DEEZER PAUSE"
                If MusiqueDeezer.Interne Then MusiqueDeezer.PauseSurDemande() Else GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Stop")
            Case "DEEZER NEXT SONG"
                If MusiqueDeezer.Interne Then MusiqueDeezer.Suivant() Else GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Next")
            Case "VOLUME DEEZER"
                ' Volume du Deezer integre a partir de cette heure (sans le lancer ni changer de source).
                If VolumeDeLaLigne(volume) >= 0 Then
                    VolumeDeezerParMinuterie = VolumeDeLaLigne(volume)
                    If MusiqueDeezer.Actif Then MusiqueDeezer.DefinirVolume(VolumeDeezerParMinuterie)
                End If
            Case "KILL DEEZER"
                ' Deezer integre : ferme, et il le reste jusqu'a une relance (MusiqueDeezer.Quitter).
                If MusiqueDeezer.Interne Then MusiqueDeezer.Quitter() Else KillProcess("Deezer")
            Case Else
                DiagLog("Minuterie : action Deezer inconnue (" & action & ")")
        End Select
    End Sub

    ''' <summary>Volume ecrit dans la colonne Volume d'une ligne de la minuterie (0 a 100), ou -1
    ''' si la case est vide.</summary>
    Function VolumeDeLaLigne(volume As Object) As Integer
        Dim v As String = If(volume Is Nothing, "", CStr(volume)).Trim()
        If v = "" OrElse Not IsNumeric(v) Then Return -1
        Return Math.Max(0, Math.Min(100, CInt(Val(v))))
    End Function

    ''' <summary>
    ''' Taches de la minuterie "Music Source / Deezer" et "Divers / Lance Deezer" (demande de
    ''' Jacob, 07/10 : "programmer Deezer a une heure precise avec le volume") : a partir de cette
    ''' heure, la musique du diaporama est Deezer, au volume de la ligne (case vide : celui des
    ''' reglages). Comme les autres taches "Music Source", le changement vaut jusqu'a la tache
    ''' suivante ou au prochain lancement de Slide (les reglages ne sont pas modifies).
    ''' Pour revenir aux MP3 ou a la radio : "Radio to Mp3" / "Mp3 to Radio" ; pour couper : "Music OFF".
    ''' </summary>
    ''' <param name="playlist">Adresse Deezer de la playlist de cette tache ("" : celle des
    ''' reglages, onglet Audio ; s'il n'y en a pas non plus, la file d'attente de Deezer).</param>
    ''' <param name="garderLeVolume">Vrai pour une commande a distance ("Deezer.Favo") : le volume
    ''' en cours n'est pas touche.</param>
    Sub DeezerParLaMinuterie(volume As Object, Optional playlist As String = "", Optional garderLeVolume As Boolean = False)
        If Not garderLeVolume Then VolumeDeezerParMinuterie = VolumeDeLaLigne(volume)
        DeezerPlaylistParMinuterie = If(playlist, "").Trim()
        Music = True : Music_Deezer = True : Music_Mp3 = False : Music_Radio = False
        DiagLog("Minuterie : musique = Deezer" & If(MusiqueDeezer.Actif, " (integre a Slide)", " (application externe)") &
                ", volume " & If(VolumeDeezerParMinuterie >= 0, VolumeDeezerParMinuterie.ToString(), "des reglages (" & VolumeDeezer & ")") &
                ", playlist " & If(MusiqueDeezer.PlaylistVoulue() = "", "aucune (file d'attente de Deezer)", MusiqueDeezer.PlaylistVoulue()))
        If MusiqueDeezer.Actif Then
            MusiqueDeezer.DefinirVolume(If(VolumeDeezerParMinuterie >= 0, VolumeDeezerParMinuterie, VolumeDeezer))
            MusiqueDeezer.RevenirALaPlaylist()
        End If
        Music_SWITCH_ON(True)
        ' (Music_SWITCH_ON ne redemande pas la musique pendant une video du diaporama : la playlist
        '  voulue ne partait alors jamais - voir MusiqueDeezer.JouerSiLaMusiqueJoue.)
        If MusiqueDeezer.Actif Then
            MusiqueDeezer.JouerSiLaMusiqueJoue()
            ' La tache demande Deezer EXPRESSEMENT : si Slide ne tient pas la musique en pause, elle part tout de
            ' suite. Avant, seule une musique deja en cours (ou deja voulue) etait relancee : pendant une longue
            ' video muette, Deezer, ouvert par la tache, attendait la photo ou la video suivante (journal de
            ' l'autre poste, 14/10/2026 : tache a 09:20:01, premiere note a 09:31 - 11 minutes de silence).
            If LaMusiquePeutJouerMaintenant() Then MusiqueDeezer.Jouer()
        End If
    End Sub

    ''' <summary>
    ''' Vrai si Slide ne tient PAS la musique en pause en ce moment : diaporama en photos, ou video du diaporama
    ''' que l'on n'entend pas (muette par le reglage, volume 0, sans piste son). Faux pendant une video qu'on entend,
    ''' un film, la television, une annonce, un PIP sonore, ou tant que les reglages sont ouverts.
    ''' </summary>
    Function LaMusiquePeutJouerMaintenant() As Boolean
        If Music = False Then Return False
        If PIPvolume > 0 Then Return False
        If SilenceDesReglages.Actif Then Return False
        Select Case If(Diffusion.Mode, "")
            Case "PHOTO"
                Return True
            Case "VIDEO"
                Return VideoDuDiaporamaMuette()
        End Select
        Return False
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Tache "Volume / Volume OFF" de la minuterie (15/10/2026) : en plus de couper le son de Windows, elle FERME Deezer et
    ' arrete le MP3 et la radio ; "Volume ON" reprend la musique dans l'etat d'avant (meme source, MP3 reprenant le meme
    ' morceau la ou il s'etait arrete ; Deezer est rouvert).
    ' ------------------------------------------------------------------------------------------

    Private Class EtatMusiqueAvantVolumeOff
        Public Music As Boolean
        Public Mp3 As Boolean
        Public Radio As Boolean
        Public Deezer As Boolean
    End Class

    Private _musiqueAvantVolumeOff As EtatMusiqueAvantVolumeOff = Nothing      ' Nothing : aucune coupure en cours

    Sub CouperLaMusiquePourVolumeOff()
        Try
            If _musiqueAvantVolumeOff IsNot Nothing Then Exit Sub                ' deja coupee : l'etat d'origine est garde
            _musiqueAvantVolumeOff = New EtatMusiqueAvantVolumeOff With {.Music = Music, .Mp3 = Music_Mp3, .Radio = Music_Radio, .Deezer = Music_Deezer}
            DiagLog("Volume OFF : musique arretee (avant : " & If(Music, "musique ON", "musique OFF") & ", source " &
                    If(Music_Deezer, "Deezer", If(Music_Radio, "radio", "MP3")) & ")")
            Music = False
            Music_SWITCH_ON(False)                                               ' MP3 et radio en pause, Deezer externe ferme
            If MusiqueDeezer.Interne Then MusiqueDeezer.Fermer()                 ' Deezer integre : lecteur ferme
        Catch ex As Exception
            DiagLog("Volume OFF : arret de la musique impossible (" & ex.Message & ")")
        End Try
    End Sub

    Sub ReprendreLaMusiqueApresVolumeOff()
        Try
            Dim e As EtatMusiqueAvantVolumeOff = _musiqueAvantVolumeOff
            If e Is Nothing Then Exit Sub
            _musiqueAvantVolumeOff = Nothing
            Music_Mp3 = e.Mp3 : Music_Radio = e.Radio : Music_Deezer = e.Deezer
            Music = e.Music
            DiagLog("Volume ON : musique remise dans l'etat d'avant (" & If(e.Music, "musique ON", "musique OFF") & ", source " &
                    If(e.Deezer, "Deezer", If(e.Radio, "radio", "MP3")) & ")")
            If Not e.Music Then Exit Sub
            If e.Deezer Then
                Music_SWITCH_ON(True)                                            ' Deezer est rouvert
            Else
                Form3.MusicToolStripMenuItem.Checked = True
                MusiqueVLC.MusiqueJouer()                                        ' le morceau en pause reprend ou il etait (radio : le flux repart)
            End If
        Catch ex As Exception
            DiagLog("Volume ON : reprise de la musique impossible (" & ex.Message & ")")
        End Try
    End Sub

    Sub Music_SWITCH_ON(x As Boolean)
        If ConfierAuFilPrincipal(Sub() Music_SWITCH_ON(x), "Music_SWITCH_ON") Then Exit Sub

        If x = False Then
            MusiqueVLC.MusiquePause()
            Form3.MusicToolStripMenuItem.Checked = False
            FermerLesLecteursLances("Deezer")          ' seulement le Deezer ouvert par Slide
            Exit Sub
        End If

        ' Avant : KillProcess("edge") et ("Deezer") a chaque reprise de la musique (donc a chaque
        ' N / P...) : Edge et tout ce qui utilise msedgewebview2 etaient tues. On ne ferme plus que
        ' ce que Slide a ouvert.
        FermerLesFenetresNetflixPrime()
        FermerLesLecteursLances("Deezer")
        ' La source n'est plus Deezer (ou plus le Deezer integre) : son lecteur est ferme.
        If Not MusiqueDeezer.Actif Then MusiqueDeezer.MettreEnVeille()      ' (en pause, pas ferme : retour rapide sur Deezer)

        If Music_Radio = True Then

            If MusiqueVLC.MusiqueUrlActuelle() <> RadioUrl Then
                MusiqueVLC.MusiqueJouerRadio(RadioUrl)
            End If
        End If


        If Music_Deezer = True Then
            'check si deezer est lancé
            If MusiqueDeezer.Actif Then
                MusiqueDeezer.Relancer()               ' (s'il avait ete quitte par "Kill Deezer" : il peut revenir)
                MusiqueDeezer.Demarrer()               ' Deezer dans Slide : MusiqueJouer, plus bas, le fait jouer
            ElseIf WindowExist("Deezer") = False Then
                LanceDeezer()
            End If

        End If


        If Music_Mp3 = True Then
            MusiqueVLC.MusiqueMelanger()
        End If



        LancerMusic = True
        ' Pendant une video qu'on entend : pas de musique. Pendant une video MUETTE, elle peut jouer (comme
        ' MusiquePendantLaVideo et LaMusiquePeutJouerMaintenant) : sinon, apres l'alternance Deezer -> mp3 pendant
        ' une video muette, Deezer etait ferme et la musique mp3 ne demarrait pas avant la fin de la video (15/10/2026 :
        ' "l'album s'affiche mais pas de son").
        If Diffusion.Mode = "VIDEO" AndAlso Not VideoDuDiaporamaMuette() Then LancerMusic = False
        ' (StartsWith et non Substring(0, 4) : avec le mode "TV" ou "SAT", plus court, Substring levait une
        '  erreur et la fin de cette procedure n'etait pas executee.)
        If If(Diffusion.Mode, "").StartsWith("FILM") Then LancerMusic = False
        If Music_Deezer = True AndAlso Not MusiqueDeezer.Actif Then LancerMusic = False      ' (application Deezer externe : Slide ne la commande pas)
        If LancerMusic = True Then MusiqueVLC.MusiqueJouer()
        Form3.MusicToolStripMenuItem.Checked = True

    End Sub

    Sub VideoWithAudio_SWITCH_ON(x As Boolean)
        If PIPvolume > 0 Then Exit Sub

        If x = True Then
            Form3.VideoAvecAudioToolStripMenuItem.Checked = True
            ' Muette malgre tout (volume des videos a 0, video sans piste son) : on la laisse
            ' muette et la musique continue (voir VideoDuDiaporamaMuette).
            Dim muette As Boolean = VideoDuDiaporamaMuette()
            Form3.VlcControl1.Audio.IsMute = muette
            If Music = True And Diffusion.Mode = "VIDEO" And Not muette Then MusiqueVLC.MusiquePause()

        Else
            Form3.VideoAvecAudioToolStripMenuItem.Checked = False
            Form3.VlcControl1.Audio.IsMute = True

            ' Video muette : la musique continue, pendant les photos comme pendant les videos
            ' (avant, seulement pendant les photos).
            If Music = True And (Diffusion.Mode = "PHOTO" Or Diffusion.Mode = "VIDEO") Then MusiqueVLC.MusiqueJouer()
        End If
    End Sub

    ''' <summary>
    ''' Vrai si la video du diaporama ne fait AUCUN son : "VideoAvecAudio" decoche, volume des
    ''' videos a 0, ou video sans piste son. Dans tous ces cas la musique continue (demande de
    ''' Jacob) ; elle n'est mise en pause que pour une video qu'on entend.
    ''' </summary>
    Function VideoDuDiaporamaMuette() As Boolean
        If VideoWithAudio = False Then Return True
        Dim volume As Integer = If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide)
        If volume <= 0 Then Return True
        ' Pistes connues et aucune n'est du son (video de camera de surveillance, timelapse...).
        Try
            Dim media As Vlc.DotNet.Core.VlcMedia = Form3.VlcControl1.GetCurrentMedia()
            If media IsNot Nothing Then
                Dim pistes As Vlc.DotNet.Core.Interops.MediaTrack() = media.Tracks
                If pistes IsNot Nothing AndAlso pistes.Length > 0 AndAlso
                   Not pistes.Any(Function(p) p.Type = Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Audio) Then
                    Return True
                End If
            End If
        Catch ex As Exception
            ' pistes illisibles : on considere que la video a du son, comme avant
        End Try
        Return False
    End Function

    ''' <summary>Pendant une video du diaporama : musique en pause si on entend la video, sinon
    ''' musique qui continue (ou reprend).</summary>
    Sub MusiquePendantLaVideo()
        If Music = False Then
            MusiqueVLC.MusiquePause()
        ElseIf VideoDuDiaporamaMuette() Then
            MusiqueVLC.MusiqueJouer()
            DiagLog("Musique : continue pendant la video muette")
        Else
            MusiqueVLC.MusiquePause()
        End If
    End Sub

    Private Function Font() As Font
        Throw New NotImplementedException
    End Function


    ''' <param name="argument">Colonne Argument de la minuterie : "!/Aquarium:True" = au lieu du
    ''' noir, une video d'aquarium (dossier data\Aquarium) en boucle et sans le son.</param>
    Sub EcranNoir(EcranNoirFin As Date, timer As String, Optional argument As String = "")

        ClosePlayer()

        If Music = True Then Music_SWITCH_ON(False)


        Form3.VlcControl1.Audio.IsMute = True
        etatVideo = Form3.VlcControl1.Visible
        Form3.VlcControl1.Visible = False

        MusiqueVLC.MusiquePause()
        Form3.PictureBox1.Visible = True



        Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Form3.WindowState = FormWindowState.Maximized
        Form3.BackColor = Color.Black

        DefinirImageAffichee(Nothing)

        ' Rien de la photo ou de la video precedente (lieu, date) sur l'ecran noir.
        MasqueLegende()

        Application.DoEvents()
        Form3.Refresh()
        CacherSouris()

        Dim aquarium As String = ""
        If UCase(CStr(Arg("Aquarium", If(argument, "")))) = "TRUE" Then aquarium = VideoAquarium()
        DiagLog("EcranNoir jusqu'a " & EcranNoirFin.ToString() & " | argument='" & argument & "' | aquarium='" & aquarium & "'")
        If aquarium <> "" Then
            ' Plein ecran, en boucle (input-repeat), toujours sans le son.
            Form3.PictureBox1.Visible = False
            Form3.VlcControl1.Visible = True
            Form3.VlcControl1.Top = 0
            Form3.VlcControl1.Left = 0
            Form3.VlcControl1.Height = Form3.ClientSize.Height
            Form3.VlcControl1.Width = Form3.ClientSize.Width
            Form3.VlcControl1.SetMedia(New Uri("file:///" & aquarium), ":input-repeat=65535")
            CouperLeSonAvantDemarrage(Form3.VlcControl1)
            Form3.VlcControl1.Play()
            Form3.VlcControl1.Audio.IsMute = True
            Call SaveLog(aquarium, "AQUARIUM")
        End If

        Do Until Now > EcranNoirFin
            If FlagExit = True Then Exit Do

            If timer = "Timer ON" Then
                wait(1)
            Else
                waitExclusif(1)

            End If
        Loop

        If aquarium <> "" Then
            Form3.VlcControl1.Stop()
            Form3.VlcControl1.Visible = False
            Form3.PictureBox1.Visible = True
        End If

        'Music_SWITCH_ON(Music)
        If FullScreen = True Then FullScreen_SWITCH_ON(True) Else FullScreen_SWITCH_ON(False)

        FlagExit = True
    End Sub

    ''' <summary>Vrai si la cellule contient bien une liste de positions "n; p1; p2; ..." dont le
    ''' nombre annonce correspond aux positions presentes (voir AfficheVideo).</summary>
    Function EstUneListeDePositions(valeur As Object) As Boolean
        If valeur Is Nothing OrElse TypeOf valeur Is DBNull Then Return False
        Dim texte As String = CStr(valeur.ToString())
        If texte.Trim() = "" Then Return False
        Dim morceaux As String() = Split(texte, ";")
        If morceaux.Length < 2 Then Return False
        For Each m As String In morceaux
            If m.Trim() <> "" AndAlso Not IsNumeric(m) Then Return False
        Next
        Dim annonce As Integer = CInt(Val(morceaux(0)))
        If annonce < 1 Then Return False
        ' Autant de positions que le nombre annonce (le dernier ";" laisse un element vide).
        Dim positions As Integer = 0
        For k As Integer = 1 To morceaux.Length - 1
            If morceaux(k).Trim() <> "" Then positions += 1
        Next
        Return positions >= annonce
    End Function

    ''' <summary>Une video du dossier data\Aquarium, tiree au hasard s'il y en a plusieurs ;
    ''' "" si le dossier est absent ou vide (l'ecran reste alors simplement noir).</summary>
    Function VideoAquarium() As String
        Try
            Dim dossier As String = AppPath & "\data\Aquarium"
            If Not System.IO.Directory.Exists(dossier) Then Return ""
            Dim extensions As String() = {".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".mpg", ".mpeg", ".ts", ".webm"}
            Dim videos As String() = System.IO.Directory.GetFiles(dossier).
                Where(Function(f) extensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())).ToArray()
            If videos.Length = 0 Then Return ""
            Return videos(New Random().Next(videos.Length))
        Catch ex As Exception
            DiagLog("Aquarium : " & ex.Message)
            Return ""
        End Try
    End Function


    ''' <summary>
    ''' Fin d'un evenement hors PIP (film, TV, IPTV, satellite, ecran noir) : Slide repart comme a
    ''' un redemarrage (demande de Jacob). La minuterie est reevaluee TOUT DE SUITE (avant, il
    ''' fallait attendre la minute suivante), avec les taches BOOT, sauf s'il y a une autre tache a
    ''' la minute en cours. Celle qui vient de finir n'est pas rejouee (MinuteDejaJouee).
    ''' </summary>
    ''' <summary>Minute evaluee par la minuterie (jour + HHmm). Avant, RelancerCommeAuDemarrage
    ''' lisait "cherche", qui n'existe que dans Minuterie (Option Explicit Off : variable locale
    ''' implicite, donc toujours vide ici) : la tache qui venait de finir pouvait etre rejouee.</summary>
    Public MinuteEnCours As String = ""

    ''' <summary>Vrai si la tache ne rend la main qu'a sa fin : lecteur plein ecran (TV, film,
    ''' IPTV, satellite sans PIP), ecran noir, programme lance avec "Attend la fin". Les arrets
    ''' ("OFF") sont rapides.</summary>
    Function TacheBloquante(l As Minuterie) As Boolean
        Dim t As String = UCase(CStr(l.Type))
        If t = "ECRAN NOIR" Then Return True
        If t = "LANCER" Then Return UCase(CStr(Arg("Wait", CStr(l.Argument)))) = "TRUE"
        If t = "TV" OrElse t = "FILM" OrElse t = "IPTV" OrElse t.StartsWith("SAT") Then
            If UCase(CStr(l.Action)).Contains("OFF") Then Return False
            Return UCase(CStr(Arg("Pip", CStr(l.Argument)))) <> "TRUE"
        End If
        Return False
    End Function

    ''' <summary>
    ''' Tache "Lancer" : ouvre le programme (ou document, raccourci, adresse web) choisi dans
    ''' Form17, avec ses arguments tels quels (ils etaient passes en MAJUSCULES). Avec "Attend la
    ''' fin avant de continuer", la minuterie attend qu'il se ferme, en laissant Slide vivre (la
    ''' minuterie continue de tourner) ; un arret demande (FlagExit) cesse l'attente sans fermer le
    ''' programme. Avant : Stop puis boucle infinie, Slide restait bloque pour de bon.
    ''' Le programme lance est garde par la fermeture des nouveaux programmes en plein ecran.
    ''' </summary>
    Sub LancerUnProgramme(argument As String)
        Dim prog As String = CStr(Arg("Prg", argument)).Trim()
        Dim args As String = CStr(Arg("Argu", argument))
        Dim attendre As Boolean = UCase(CStr(Arg("Wait", argument))) = "TRUE"
        If prog = "" Then
            SaveLogError("Lancer", "aucun programme indique")
            Exit Sub
        End If

        ' Processus deja la avant le lancement : pour reconnaitre ensuite le "vrai" programme
        ' (voir plus bas : lanceurs qui passent la main).
        Dim avant As New HashSet(Of Integer)
        For Each p As Process In Process.GetProcesses()
            avant.Add(p.Id) : p.Dispose()
        Next
        Dim heureLancement As Date = Now

        Dim lance As Process = Nothing
        Try
            Dim infos As New ProcessStartInfo(prog) With {.Arguments = args, .UseShellExecute = True}
            Try
                Dim dossier As String = IO.Path.GetDirectoryName(prog)
                If dossier <> "" AndAlso IO.Directory.Exists(dossier) Then infos.WorkingDirectory = dossier
            Catch ex As Exception
                ' adresse web ou nom de programme sans chemin : pas de dossier de travail
            End Try
            lance = Process.Start(infos)
        Catch ex As Exception
            SaveLogError(prog, "Lancer : impossible de demarrer (" & ex.Message & ")")
            Exit Sub
        End Try

        ' Programme ouvert par un autre deja en cours (document, adresse web) : pas de processus.
        If lance Is Nothing Then Exit Sub
        Try
            ProcessusLancesParSlide.Add(lance.Id)
        Catch ex As Exception
        End Try

        ' Nom du programme attendu ("notepad" pour ...\notepad.exe) : sert a retrouver le vrai
        ' processus quand le lance n'est qu'un lanceur.
        Dim nomAttendu As String = ""
        Try
            nomAttendu = IO.Path.GetFileNameWithoutExtension(prog)
        Catch ex As Exception
        End Try

        ' Les nouveaux processus du meme nom (hors celui lance) sont aussi retenus par
        ' ProcessusLancesParSlide : en plein ecran, le vrai Bloc-notes n'est pas ferme.
        Dim relais As List(Of Process) = TrouverLesRelais(nomAttendu, avant, lance.Id)

        If Not attendre Then Exit Sub

        ' Attente : le processus lance, puis, s'il s'est ferme dans les 5 premieres secondes, les
        ' nouveaux processus du meme nom. Sous Windows 11, notepad.exe n'est qu'un lanceur qui
        ' demarre le Bloc-notes du Store dans un AUTRE processus puis se ferme : on arretait
        ' d'attendre au bout d'une seconde, Bloc-notes encore ouvert.
        Dim attendus As New List(Of Process) From {lance}
        Dim relaisCherches As Boolean = False
        Do
            If FlagExit Then Exit Do
            attendus.RemoveAll(Function(p)
                                   Try
                                       Return p.HasExited
                                   Catch ex As Exception
                                       Return True             ' plus d'acces : on ne l'attend plus
                                   End Try
                               End Function)
            If attendus.Count = 0 Then
                If relaisCherches OrElse (Now - heureLancement).TotalSeconds > 5 Then Exit Do
                relaisCherches = True
                Threading.Thread.Sleep(500)                         ' le temps que le relais demarre
                relais = TrouverLesRelais(nomAttendu, avant, lance.Id)
                attendus.AddRange(relais)
                If attendus.Count = 0 Then Exit Do
                SaveLog("Minuterie", "Lancer " & prog & " : attente du programme demarre par le lanceur (" &
                        String.Join(", ", attendus.Select(Function(p) p.Id.ToString())) & ")")
            End If
            Application.DoEvents()
            Threading.Thread.Sleep(200)
        Loop
        SaveLog("Minuterie", "Fin Lancer " & prog)
    End Sub

    ''' <summary>Processus apparus depuis le lancement, du meme nom que le programme lance (hors
    ''' lui-meme) : le vrai programme quand le lance n'est qu'un lanceur. Ils sont proteges de la
    ''' fermeture des nouveaux programmes en plein ecran.</summary>
    Private Function TrouverLesRelais(nom As String, avant As HashSet(Of Integer), idLance As Integer) As List(Of Process)
        Dim trouves As New List(Of Process)
        If String.IsNullOrEmpty(nom) Then Return trouves
        For Each p As Process In Process.GetProcessesByName(nom)
            If p.Id <> idLance AndAlso Not avant.Contains(p.Id) Then
                trouves.Add(p)
                ProcessusLancesParSlide.Add(p.Id)
            Else
                p.Dispose()
            End If
        Next
        Return trouves
    End Function

    Sub RelancerCommeAuDemarrage()
        MinuteDejaJouee = MinuteEnCours : MinuteDejaJoueeLe = Now
        PremierLancementDeLaMinuterie = True
        NouvelleMinute = "ZZZZ"
        DiagLog("Minuterie : fin d'evenement -> relance comme au demarrage (taches BOOT)")
    End Sub

    Public MinuteDejaJouee As String = ""
    Public MinuteDejaJoueeLe As Date = Date.MinValue   ' la semaine suivante, meme jour et meme heure, la tache doit rejouer

    Sub Minuterie()
        ' Simulation "pendant une video" (data\TestAnnonce.txt = "HH:MM;x;video") : verifiee a chaque
        ' passage (toutes les 2 s), elle part des qu'une video du diaporama est a l'ecran.
        Dim xVideo As Integer
        If TestAnnoncePendantVideo(xVideo) Then AnnonceShabbat(xVideo) : Exit Sub


        HHMM = Format(Now, "HHmm")
        If NouvelleMinute = HHMM Then Exit Sub

        If ControlAdistance = True And fsw.EnableRaisingEvents = False Then
            'fsw.EnableRaisingEvents = True
            Call GestionControlAdistance(ControlAdistance)
        End If

        'FswDeja = False

        NouvelleMinute = HHMM

        Shabbat.counter2 = 0 ' uniquement dans le cas ou fin de shabbat est égale à un evenement dans la minuterie

        Dim dt As Date
        dt = Now
        JDLS = Val(dt.DayOfWeek) - 1
        If JDLS = -1 Then JDLS = 6  ' lundi=0 ... dimanche= 6

        cherche = "" : cherche2 = "" : cherche3 = "" : cherche4 = ""
        cherche = Format(JDLS, "00") + HHMM


        If PremierLancementDeLaMinuterie = True Then
            PremierLancementDeLaMinuterie = False
            TrouveHHMM = 0
            ' Seule une tache de la minute EN COURS passe avant les taches BOOT. Avant, une tache
            ' de la minute SUIVANTE suffisait a sauter le BOOT : lance a 11:19 avec un film prevu a
            ' 11:20, le PIP du lancement ne partait pas (il n'etait relance qu'apres le film).
            ' Le film programme demarre de toute facon a sa minute.
            ' RTConvert : meme conversion que la boucle plus bas (jours "TLJ", "LUN-VEN"...).
            For Each row In LigneMinuterie
                If cherche = RTConvert(row.tri, JDLS) Then
                    TrouveHHMM = 1
                End If
            Next
            ' Relance juste apres un evenement (RelancerCommeAuDemarrage) : si c'est encore sa
            ' minute, ne pas le rejouer ; on repart sur les taches BOOT.
            If cherche = MinuteDejaJouee AndAlso (Now - MinuteDejaJoueeLe).TotalMinutes < 2 Then TrouveHHMM = 0

            ' Relance de nuit (RelanceDeNuit) : l'etat d'avant est remis tel quel. Pas de taches
            ' BOOT (un PIP arrete pour la nuit reviendrait) : les PIP qui etaient affiches sont
            ' relances ; les taches de la minute deja jouee par l'ancien programme ne sont pas rejouees.
            If RelanceDeNuit.PremierPassageApresRelance() Then
                If cherche = RelanceDeNuit.MinuteJouee Then TrouveHHMM = 0
                If TrouveHHMM = 0 Then cherche = "999999"       ' rien a jouer a ce passage
                RelanceDeNuit.RemettreLesPip()
            ElseIf TrouveHHMM = 0 Then
                cherche = "000000"
            End If
            ' Trace : le 01/10 a 20:00, un lancement n'a pas joue la tache BOOT (PIP TV absent, "RAS")
            ' sans explication ; de quoi comprendre si cela se reproduit.
            Try
                DiagLog("Minuterie : premier passage, minute " & Format(JDLS, "00") & HHMM & " -> recherche " & cherche &
                        " (" & LigneMinuterie.Count & " lignes, " &
                        LigneMinuterie.Cast(Of Minuterie)().Where(Function(l) RTConvert(l.Tri, JDLS) = cherche).Count() & " a jouer)")
            Catch ex As Exception
            End Try

        End If

        '* pour test
        'Shabbat.PreDebut = "17:05"
        'Shabbat.Fin = "11:53"
        'If AfficheVideoChabbatShalom = True And cherche = "03" + Replace(Shabbat.PreDebut, ":", "") Then AnnonceShabbat(0) : Exit Sub
        'If AfficheVideoChavouaTov = True And cherche = "04" + Replace(Shabbat.Fin, ":", "") Then AnnonceShabbat(1) : Exit Sub
        '
        'AnnonceShabbat(0)

        ' Simulation (data\TestAnnonce.txt = "HH:MM;0" entree ou "HH:MM;1" sortie) : une seule fois.
        Dim xTest As Integer
        If TestAnnonceAMinute(CStr(cherche), xTest) Then AnnonceShabbat(xTest) : Exit Sub
        If AfficheVideoChabbatShalom = True And cherche = "04" + Replace(Shabbat.PreDebut, ":", "") Then AnnonceShabbat(0) : Exit Sub
        If AfficheVideoChavouaTov = True And cherche = "05" + Replace(Shabbat.Fin, ":", "") Then AnnonceShabbat(1) : Exit Sub


        quoi = ""

        ' Taches de cette minute : les RAPIDES d'abord (volume, musique, modes, Sonoff, Divers,
        ' lecteurs en PIP, arrets "OFF"), puis les BLOQUANTES (TV/film/IPTV/SAT sans PIP, ecran
        ' noir, Lancer), qui ne rendent la main qu'a leur fin. Avant, dans l'ordre de la liste,
        ' une TV placee en premier retenait les autres taches de la meme minute jusqu'a sa fin
        ' (parfois des heures) : on croyait qu'une seule avait ete prise en compte.
        Dim tachesDeLaMinute As New List(Of Minuterie)
        For Each l As Minuterie In LigneMinuterie
            If cherche = RTConvert(l.Tri, JDLS) Then tachesDeLaMinute.Add(l)
        Next
        Dim ordre As New List(Of Minuterie)(tachesDeLaMinute.Where(Function(l) Not TacheBloquante(l)))
        ordre.AddRange(tachesDeLaMinute.Where(Function(l) TacheBloquante(l)))
        MinuteEnCours = cherche
        Dim minuteDeDepart As String = HHMM
        Dim bloquanteDejaJouee As Boolean = False

        Dim versionDeLaPasse As Integer = VersionDeLaMinuterie
        Try
            For Each row In ordre
                ' La minuterie a ete modifiee pendant la passe (reglages ouverts puis valides alors que les PIP se lancent) : les taches qui
                ' restent viennent de l'ancienne minuterie, elles ne sont plus lancees.
                If VersionDeLaMinuterie <> versionDeLaPasse Then
                    DiagLog("Minuterie : passe interrompue, la minuterie a ete modifiee (" & row.Type & " " & row.Action & " non lance)")
                    Exit For
                End If

                ' Une tache bloquante a tenu jusqu'apres cette minute : les suivantes (bloquantes
                ' elles aussi) ne sont plus lancees a contretemps.
                If TacheBloquante(row) Then
                    If bloquanteDejaJouee AndAlso Format(Now, "HHmm") <> minuteDeDepart Then
                        SaveLog("Minuterie", "Ignoree (sa minute est passee) : " & row.Type & " " & row.Action)
                        Continue For
                    End If
                    bloquanteDejaJouee = True
                End If

                ROWTRI = row.Tri
                ROWTRI = RTConvert(ROWTRI, JDLS)

                ' Film, TV, IPTV, SAT ou programme lance alors que le son est coupe ("Volume OFF") :
                ' le son est remis (demande de Jacob), sauf arret ("... OFF") ou volume 0 (PIP muet).
                If cherche = ROWTRI AndAlso VolumeOFF = True Then
                    Dim typeTache As String = UCase(CStr(row.Type))
                    If (typeTache = "TV" OrElse typeTache = "FILM" OrElse typeTache = "IPTV" OrElse typeTache.StartsWith("SAT") OrElse typeTache = "LANCER") AndAlso
                       Not UCase(CStr(row.Action)).Contains("OFF") AndAlso (typeTache = "LANCER" OrElse Val(row.Volume) > 0) Then
                        VolumeOFF = False
                        SetMasterVol(VolumeGeneral)
                        ReprendreLaMusiqueApresVolumeOff()
                        DiagLog("Minuterie : son remis (Volume ON) pour " & typeTache & " " & CStr(row.Action))
                    End If
                End If

                If cherche = ROWTRI And UCase(row.Type) = "ECRAN NOIR" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    p = InStr(row.Action, "-")
                    Dim EcranNoirFin As Date = Mid(row.Action, p + 1)
                    Call SaveLog("Minuterie", "Debut " & quoi)
                    CouperLeSonPourLaTache("ECRAN NOIR")
                    LibererLeSonDeLaTache()                 ' (rien ne joue pendant l'ecran noir : musique libre a sa fin)
                    EcranNoir(EcranNoirFin, row.Play, If(TypeOf row.Argument Is String, CStr(row.Argument), ""))
                    quoi &= "Fin " & quoi
                    RelancerCommeAuDemarrage()
                    'Exit Sub
                End If

                If cherche = ROWTRI And UCase(row.Type) = "LANCER" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    Call SaveLog("Minuterie", "Debut " & quoi)
                    LancerUnProgramme(CStr(row.Argument))
                End If



                If cherche = ROWTRI And UCase(row.Type) = "MODE RDV" Then

                    ' Reglages de la tache. La fenetre de la tache les ecrivait a l'ancienne ("/ModeRdv:...")
                    ' et ils n'etaient convertis qu'au redemarrage de Slide : jusque-la rien n'etait lu,
                    ' et le RDV programme ne demarrait pas (erreur de conversion). On convertit ici aussi,
                    ' et une valeur absente ne fait plus d'erreur.
                    argu = CStr(Modif_Old_Argument_Format(row.Argument))
                    ModeRdv = (UCase(CStr(Arg("ModeRDV", argu))) = "TRUE")
                    If ModeRdv = True Then
                        ModeRdvDateDebut = Replace(CStr(Arg("ModeRdvDateDebut", argu)), "-", "/")
                        ModeRdvDateFin = Replace(CStr(Arg("ModeRdvDateFin", argu)), "-", "/")
                        ModeRdvLimitVideo = CInt(Val(CStr(Arg("ModeRdvLimitVideo", argu))))
                        ModeRdvEnBoucle = (UCase(CStr(Arg("ModeRdvEnBoucle", argu))) = "TRUE")
                        ' Un RDV programme part toujours du debut de sa periode.
                        ModeRendezVous.RdvReinitialiser()
                    End If
                    quoi &= "*" & UCase(row.Type) & " " & ModeRdv
                    Call SaveLog("Minuterie", "Debut " & quoi)

                    quoi &= "Fin " & quoi
                    'Exit Sub
                End If

                ' Taches Deezer (demande de Jacob, 08/10 : "sors les commandes Deezer de Divers et mets
                ' tout dans une nouvelle section Deezer") : type "Deezer". Les lignes d'avant, ou ces
                ' actions etaient rangees dans Divers, Volume ou Music Source, sont converties au
                ' chargement (Module2.TypeDeezerPour) ; celles qui ne le seraient pas marchent aussi.
                If cherche = ROWTRI AndAlso UCase(TypeDeezerPour(CStr(row.Type), CStr(row.Action))) = "DEEZER" Then
                    If UCase(CStr(row.Type)) = "DEEZER" Then quoi &= "*DEEZER " & UCase(CStr(row.Action))
                    TacheDeezer(CStr(row.Action), row.Volume, If(TypeOf row.Argument Is String, CStr(row.Argument), ""))
                End If

                If cherche = ROWTRI And UCase(row.Type) = "DIVERS" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)

                    ' Ce qui va s'arreter se tait tout de suite, sans attendre sa fermeture.
                    Select Case UCase(CStr(row.Action))
                        Case "TV OFF", "IPTV OFF", "CLOSE ALL PLAYER", "END & CONTINUE" : CouperLeSonDeCeQuiSArrete()
                    End Select
                    If UCase(row.Action) = "TV OFF" Then ArreterLePipPersistant("TV") : FlagExit = True : EndAndContinue() : retablieTousLesVolumes() : PremierLancementDeLaMinuterie = True
                    If UCase(row.Action) = "IPTV OFF" Then ArreterLePipPersistant("IPTV") : FlagExit = True : wait(2) : PremierLancementDeLaMinuterie = True
                    If UCase(row.Action) = "CLOSE ALL PLAYER" Then ArreterTousLesPip() : ClosePlayer() : PremierLancementDeLaMinuterie = True
                    If UCase(row.Action) = "END & CONTINUE" Then ArreterTousLesPip() : EndAndContinue() : PremierLancementDeLaMinuterie = True

                End If

                If cherche = ROWTRI And UCase(row.Type) = "VOLUME" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If UCase(row.Action) = "VOLUME ON" Then VolumeOFF = False : SetMasterVol(VolumeGeneral) : ReprendreLaMusiqueApresVolumeOff()
                    If UCase(row.Action) = "VOLUME OFF" Then
                        zero = 0
                        If VolumeOFFegal1 = True Then zero = 1
                        SetMasterVol(zero)
                        VolumeOFF = True
                        CouperLaMusiquePourVolumeOff()
                    End If
                    If UCase(row.Action) = "VOLUME MUSIC SLIDE" Then VolumeMusicSlideParMinuterie = row.Volume : SetMusicVolSlide(VolumeMusicSlideParMinuterie)
                    If UCase(row.Action) = "VOLUME VIDEO SLIDE" Then VolumeVideoSlideParMinuterie = row.Volume : SetVideoVolSlide(VolumeVideoSlideParMinuterie)
                End If

                If cherche = ROWTRI And UCase(row.Type) = "MUSIC SOURCE" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    ' (Music_Deezer = False : sinon, apres une tache "Deezer", on ne revenait ni aux MP3 ni a la radio.)
                    If UCase(row.Action) = "RADIO TO MP3" Then Music = True : Music_Mp3 = True : Music_Radio = False : Music_Deezer = False : Music_SWITCH_ON(Music)
                    If UCase(row.Action) = "MP3 TO RADIO" Then Music = True : Music_Mp3 = False : Music_Radio = True : Music_Deezer = False : Music_SWITCH_ON(Music)
                    If UCase(row.Action) = "MUSIC OFF" Then Music = False : Music_SWITCH_ON(Music)
                    If UCase(row.Action) = "MUSIC ON" Then Music = True : Music_SWITCH_ON(Music)

                End If



                If cherche = ROWTRI And UCase(row.Type) = "PHOTO MODE" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "Décroissant" Then
                        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                        PhotoModeCroissant = False
                    End If
                    If row.Action = "Croissant" Then
                        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                        PhotoModeCroissant = True
                    End If
                    If row.Action = "Aléatoire Classic" Then
                        PhotoAleatoire = 1
                    End If
                    If row.Action = "Aléatoire Séquentiel" Then
                        PhotoAleatoire = 2
                    End If
                    If row.Action = "Aléatoire OFF" Then
                        PhotoAleatoire = 0
                    End If
                    If row.Action = "Reprendre les Photos depuis le debut" Then
                        NumPhoto = -1
                        CaseAcocherVide(1)
                    End If
                    NumPhoto = 0
                    'SetMusicVolSlide(Val(row.volume))

                End If

                If cherche = ROWTRI And UCase(row.Type) = "VIDEO MODE" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "Décroissant" Then
                        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                        VideoModeCroissant = False
                    End If
                    If row.Action = "Croissant" Then
                        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                        VideoModeCroissant = True
                    End If
                    If row.Action = "Aléatoire ON" Then
                        VideoAleatoire = True
                    End If
                    If row.Action = "Aléatoire OFF" Then
                        VideoAleatoire = False
                    End If
                    If row.Action = "Reprendre les Videos depuis le debut" Then
                        NumVideo = -1
                        CaseAcocherVide(2)
                    End If
                    If row.Action = "Video avec audio ON" Then
                        VideoWithAudio = True
                    End If
                    If row.Action = "Video avec audio OFF" Then
                        VideoWithAudio = False
                    End If
                    NumVideo = 0
                    'SetVideoVolSlide(Val(row.volume))
                End If

                If cherche = ROWTRI And UCase(row.Type) = "PHOTOVIDEO MODE" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "Décroissant" Then
                        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                        PhotoModeCroissant = False
                        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                        VideoModeCroissant = False
                    End If
                    If row.Action = "Croissant" Then
                        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                        PhotoModeCroissant = True
                        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                        VideoModeCroissant = True
                    End If

                    If row.Action = "Aléatoire ON" Then
                        PhotoAleatoire = 2
                        VideoAleatoire = True
                    End If

                    If row.Action = "Aléatoire OFF" Then
                        PhotoAleatoire = 0
                        VideoAleatoire = False
                    End If
                    If row.Action = "Reprendre Photos/Videos depuis le debut avec RAZ" Then
                        'NumPhoto = -1
                        CaseAcocherVide(1)
                        'NumVideo = -1
                        CaseAcocherVide(2)
                    End If
                    If row.Action = "Reprendre Photos/Videos depuis le debut sans RAZ" Then
                        NumPhoto = 0
                        NumVideo = 0
                    End If

                    If row.Action = "Reprendre Photos/Videos debut RAZ Aléatoite OFF" Then
                        'NumPhoto = -1
                        CaseAcocherVide(1)
                        'NumVideo = -1
                        CaseAcocherVide(2)
                        PhotoAleatoire = 0
                        VideoAleatoire = False
                    End If
                    If row.Action = "Mode Normal Photos/Videos Aléatoite Séquenciel" Then
                        PhotoAleatoire = 2
                        VideoAleatoire = True
                    End If


                    NumPhoto = 0
                    NumVideo = 0

                End If


                If cherche = ROWTRI And UCase(row.Type) = "SONOFF" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    Call Sonoff_Send(row.Action)
                End If


                If cherche = ROWTRI And UCase(row.Type) = "TV" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "Chaine OFF" Then
                        CouperLeSonDeCeQuiSArrete()
                        ArreterLePipPersistant("TV")
                        EndAndContinue()
                        retablieTousLesVolumes()

                    ElseIf Not CleTNTPrete() Then
                        ' Cle TNT USB absente ou en erreur : chaine non ouverte (message a l'ecran et
                        ' dans les journaux, voir CleTNT). Sans relance "comme au demarrage" : la
                        ' tache BOOT (PIP TV) echouerait de la meme facon.
                        quoi &= " (cle TNT indisponible)"
                    Else

                        NumChaine = Val(Mid(row.Action, 8, 2))
                        CouperLeSonPourLaTache("TV " & CStr(row.Action), row.Volume)
                        PlayTV_VLC(NumChaine, Val(row.Volume), row.Argument)
                        ' PIP : son propre fil, la procedure rend la main tout de suite : pas 2 s d'attente par PIP (lancement : les 4 PIP
                        ' arrivaient les uns apres les autres, 16/10/2026 : "au lancement les PIP ne reprennent leur place que 30 s apres").
                        wait(If(UCase(Arg("Pip", row.Argument)) = "TRUE", 0.4, 2))
                        Application.DoEvents()
                        LibererLeSonDeLaTache()
                        pip = If(UCase(Arg("Pip", row.Argument)) = "TRUE", True, False)
                        If pip <> True Then RelancerCommeAuDemarrage()
                    End If

                End If

                If cherche = ROWTRI And UCase(row.Type) = "IPTV" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "IPTV OFF" Then
                        CouperLeSonDeCeQuiSArrete()
                        ArreterLePipPersistant("IPTV")
                        ClosePlayer()
                        EndAndContinue()
                        retablieTousLesVolumes()

                    Else
                        'NumChaine = Val(Mid(row.action, 7, 4))
                        CouperLeSonPourLaTache("IPTV " & CStr(row.Action), row.Volume)
                        PlayIPTV_VLC(row.Action, Val(row.Volume), row.Argument)
                        LibererLeSonDeLaTache()
                        wait(If(UCase(Arg("Pip", row.Argument)) = "TRUE", 0.4, 2))
                        'Exit Sub
                        If UCase(Arg("Pip", row.Argument)) <> "TRUE" Then RelancerCommeAuDemarrage()
                    End If
                    pip = If(UCase(Arg("Pip", row.Argument)) = "TRUE", True, False)
                    If pip <> True Then
                        PremierLancementDeLaMinuterie = True
                    End If

                End If


                If cherche = ROWTRI And UCase(row.Type) = "FILM" AndAlso FilmDejaDansLePip(row.Action, row.Argument, CInt(Val(row.Volume))) Then
                    ' Deja en cours dans le PIP : EndAndContinue l'arreterait et PlayFILM_VLC le
                    ' reprendrait au debut. On le laisse continuer.
                    quoi &= "*" & UCase(row.Type) & " deja en cours dans le PIP"
                ElseIf cherche = ROWTRI And UCase(row.Type) = "FILM" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    Call SaveLog("Minuterie", "Debut " & quoi)
                    ' Film dont le fichier n'existe pas (demande de Jacob, 08/10/2026 : "il faut gerer si le film
                    ' est introuvable") : la tache est ignoree. Rien n'est arrete - le diaporama, la musique et
                    ' les PIP continuent - un message le dit quelques secondes, et la minuterie n'est pas
                    ' relancee "comme au demarrage" : elle retombait sur la meme tache, en boucle.
                    Dim filmManque As Boolean = FilmManquant(row.Action, row.Argument)
                    FilmIntrouvable = False
                    If filmManque Then
                        quoi &= " (film introuvable, tache ignoree)"
                        DiagLog("Minuterie : film introuvable, tache ignoree : " & row.Action)
                        Dim nomDuFilm As String = row.Action
                        Try
                            nomDuFilm = IO.Path.GetFileName(row.Action.ToString().Trim())
                        Catch ex As Exception
                        End Try
                        MessageEcran("Film introuvable : " & nomDuFilm & vbCrLf & "La tâche est ignorée, le diaporama continue.", 6, "Minuterie")
                    End If
                    ' Film plein ecran : il prend l'ecran, on arrete ce qui joue. Film EN PIP : il
                    ' s'ajoute, sans arreter les autres PIP (avant, le 2e PIP du BOOT arretait le 1er).
                    If Not filmManque Then CouperLeSonPourLaTache("FILM " & CStr(row.Action), row.Volume)
                    If Not filmManque AndAlso UCase(Arg("Pip", row.Argument)) <> "TRUE" Then EndAndContinue()
                    If Not filmManque Then PlayFILM_VLC(row.Action, row.Play, row.Durée, Val(row.Volume), row.Argument)
                    LibererLeSonDeLaTache()
                    wait(If(UCase(Arg("Pip", row.Argument)) = "TRUE", 0.4, 2))
                    quoi = "Fin " & quoi
                    Application.DoEvents()
                    ' Comme pour la TV et l'IPTV : en PIP, le film tourne dans son propre thread et
                    ' PlayFILM_VLC rend la main tout de suite. Remettre ici la minuterie en "premier
                    ' lancement" lui faisait relancer la tache a la minute suivante (la tache BOOT,
                    ' retrouvee faute d'autre) : le film du PIP repartait du debut chaque minute.
                    pip = If(UCase(Arg("Pip", row.Argument)) = "TRUE", True, False)
                    If pip <> True AndAlso Not filmManque AndAlso Not FilmIntrouvable Then RelancerCommeAuDemarrage()

                    'Exit Sub
                End If

                If cherche = ROWTRI And UCase(Left(row.Type, 3)) = "SAT" Then
                    quoi &= "*" & UCase(row.Type) & " " & UCase(row.Action)
                    If row.Action = "SAT OFF" Then
                        CouperLeSonDeCeQuiSArrete()
                        ArreterLePipPersistant("SAT")
                        ClosePlayer()
                        EndAndContinue()
                        retablieTousLesVolumes()

                        'Exit Sub
                    Else
                        NumChaine = Val(Mid(row.Action, 8, 3))
                        CouperLeSonPourLaTache("SAT " & CStr(row.Action), row.Volume)
                        PlaySAT_VLC(NumChaine, Val(row.Volume), row.Argument)
                        LibererLeSonDeLaTache()
                        If UCase(Arg("Pip", row.Argument)) <> "TRUE" Then RelancerCommeAuDemarrage()

                        'Exit Sub
                    End If
                End If


            Next


            ' PleinEcranChange : filet de securite si une entree/sortie du plein ecran n'a pas
            ' ete signalee (l'instantane n'est pris qu'une fois par periode de plein ecran).
            PleinEcranChange(FullScreen)
            If FullScreen = True Then CloseAllNewWindows()


        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try
        LibererLeSonDeLaTache()                 ' (tache en erreur : la musique n'est pas retenue pour autant)
        If quoi = "" Then quoi = "RAS"
        Call SaveLog("Minuterie", quoi)

        If StopWindowsUpdate = True Then Call WindowsUpdate("OFF")

        'If ControlAdistance = True Then Call GestionControlAdistance(ControlAdistance)

        LancerLePremierLancementDeLaMinuterie = True
        '        NouvelleMinute = HHMM


        If yyMMdd <> Format(Now, "yyMMdd") Then
            Call HeureDeShabbat(Shabbat.Ville)
            yyMMdd = Format(Now, "yyMMdd")
            'If fsw.EnableRaisingEvents = False Then Call GestionControlAdistance(ControlAdistance)
        End If


    End Sub


    Function RTConvert(Rt As String, JDLS As Integer)
        Tri = Left(Rt, 2)
        HM = Mid(Rt, 3, 4)
        Dim result As String


        result = Tri & HM


        Select Case Val(Tri)
            Case 9
                result = Format(JDLS, "00") + HM
            Case 8
                If JDLS = 5 Or JDLS = 6 Then result = Format(JDLS, "00") + HM
            Case 7
                If JDLS >= 0 And JDLS <= 4 Then result = Format(JDLS, "00") + HM

        End Select

        Return result

    End Function

    Sub AfficheVideoForTEST()

        Form9.Show()

        nv = NumVideo : NumVideo = -1
        dv = DurréeVideo : DurréeVideo = "55"
        va = VideoAleatoire : VideoAleatoire = False
        ov = onlyvideos : onlyvideos = True

debut:
        NumVideo = NumVideo + 1
        If NumVideo > (NombreDeVideo - 1) Then GoTo fin

        Nom = Form2.DataGridView2.Rows(NumVideo).Cells(3).Value

        Form9.TextBox1.AppendText("NumVideo:" & Str(NumVideo) & "/" & Str(NombreDeVideo) & " - " & Format(NumVideo / NombreDeVideo, "0%") & vbCrLf)
        Form9.TextBox1.AppendText("Nom:" & Nom & " ")


        Form2.DataGridView2.ClearSelection()
        Form2.DataGridView2.Rows(NumVideo).Selected = True

        Try
            pos = 0
            '''''
            MyDuration = 0
            Dim DuréeDeCeFilm As String = GetDuration(Nom)

            If DuréeDeCeFilm = "" Then
                SaveLogError(Nom, "Erreur lecture impossible (Durée Inconnue)")
                Form9.TextBox1.AppendText("Erreur lecture impossible (Durée Inconnue):" & vbCrLf)
                DurationLastVideo = 0
                GoTo debut
            End If

            MyDuration = TimeSpan.ParseExact(DuréeDeCeFilm, "c", Nothing).TotalSeconds
            If MyDuration < 5 Then Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1 : Form9.TextBox1.AppendText("Non testé v ideo < à 5 seconde:" & vbCrLf) : GoTo debut

            If (MyDuration) > (DurréeVideo + 1) Then
                pos = DurréeVideo
            End If
            '''''
            Application.DoEvents()





            ext = "file:///" : If Left(UCase(Nom), 4) = "HTTP" Then ext = ""
            ' VLC demarre le media suivant avec le volume qu'il a maintenant : on le met des
            ' a present au niveau du diaporama, sinon la video partirait au volume du film ou
            ' de la chaine precedente (c'est le coup de son du debut).
            PreparerLeVolumeAvantChangement(Form3.VlcControl1, If(VideoWithAudio = True, If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide), 0))
            Form3.VlcControl1.SetMedia(New Uri(ext & Nom))

            ' Muet avant de demarrer, comme dans AfficheVideo : evite le coup de son au debut.
            ' (Video prechargee et deja lancee en avance : elle joue deja, au bon volume ; on ne
            '  la coupe pas et on ne la relance pas.)
            Dim dejaEnLecture As Boolean = prechargee AndAlso Form3.VlcControl1.IsPlaying
            If Not dejaEnLecture Then
                CouperLeSonAvantDemarrage(Form3.VlcControl1)
                Form3.VlcControl1.Play()
            End If

            ' Silence tenu jusqu'a l'ouverture de la sortie audio, puis volume voulu.
            If VideoWithAudio = True Then
                ReglerLeSonApresDemarrage(Form3.VlcControl1, If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide))
            Else
                ReglerLeSonApresDemarrage(Form3.VlcControl1, 0)
            End If

            If Form3.ToolStripMenuItem2.Checked = True Then
                FullScreen_SWITCH_ON(True)
            Else
                FullScreen_SWITCH_ON(False)
            End If


            SetVideoVolSlide(VolumeVideoSlide)
            VideoWithAudio_SWITCH_ON(VideoWithAudio)

            Call SaveLog(Nom, "VIDEO ; " & Format(pos))


            i = 0
            'attend le debut de lecture


            Do Until Form3.VlcControl1.IsPlaying = True
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > TempsDattenteAcceptable Then
                    SaveLogError(Nom, "Erreur video ne demarre pas")
                    ErrorReadVideo = 1
                    Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                    Form9.TextBox1.AppendText("Erreur video ne demarre pas" & vbCrLf)
                    GoTo debut
                    Exit Do  ' echec lecture apres 2 minutes sortir
                End If
            Loop

            i = 0


            If pos > 0 Then Form3.VlcControl1.Time = pos * 1000              'en milliseconds   1 sec = 1000 milliseconds
            Do Until Form3.VlcControl1.Time > (pos * 1000) + 1
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > TempsDattenteAcceptable Then
                    SaveLogError(Nom, "Erreur positionement impossible")
                    ErrorReadVideo = 1
                    Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                    Form9.TextBox1.AppendText("Erreur positionement impossible" & vbCrLf)
                    GoTo debut
                    Exit Do  ' echec lecture apres 2 minutes sortir
                End If
            Loop

            'MsgBox(Form3.AxWindowsMediaPlayer1.Ctlcontrols.currentPosition)

            Form9.TextBox1.AppendText(" OK" & vbCrLf)

            Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1

            Form3.PictureBox1.Visible = False


            Form3.VlcControl1.Visible = True
            Form3.VlcControl1.Top = 0
            Form3.VlcControl1.Left = 0
            Form3.VlcControl1.Height = Form3.ClientSize.Height
            Form3.VlcControl1.Width = Form3.ClientSize.Width


            If AfficheDate = True Or AfficheHeureDeShabbat = True Then
                AfficheDateDeLaVideo(Nom)
            Else
                MasqueLegende()
            End If


        Catch ex As Exception
            SaveLogError(Nom, "Erreur de programation dans module AfficheVideoForTest")
        End Try

        GoTo debut
fin:

        'retablissement des pârametre

        NumVideo = nv
        DurréeVideo = dv
        VideoAleatoire = va
        onlyvideos = ov

    End Sub


    Sub AfficheVideo(NumVideo)
        PrechargementVideo.Etape("AfficheVideo")
        DurationLastVideo = 0

        Nom = Form2.DataGridView2.Rows(NumVideo).Cells(3).Value

        Form2.DataGridView2.ClearSelection()
        Form2.DataGridView2.Rows(NumVideo).Selected = True

        If Form2.Visible = True Then
            If Form2.CheckBox1.Checked = False Then
                If NumVideo > 6 Then Form2.DataGridView2.FirstDisplayedScrollingRowIndex = NumVideo - 5
            End If

        End If

        Try
            pos = 0
            '''''
            MyDuration = 0
            Dim DuréeDeCeFilm As String = GetDuration(Nom)

            If DuréeDeCeFilm = "" Then
                SaveLogError(Nom, "Erreur lecture impossible ( Durée ? )")
                DurationLastVideo = 0
                ErrorReadVideo = True
                Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                GoTo fin
            End If

            MyDuration = TimeSpan.ParseExact(DuréeDeCeFilm, "c", Nothing).TotalSeconds
            If MyDuration < MinimumDurréeVideoAcceptable Then Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1 : ErrorReadVideo = True : GoTo fin
            DurationLastVideo = MyDuration

            mdulo = 0
            If MyDuration Mod DurréeVideo = 0 Then mdulo = 1 ' si myduration/DurréeVideo = 0 c'est à dire que le prochain mini morceau serai égal =0 donc mini morceau -1

            If MyDuration > DurréeVideo And ModeRdv = False Then
                DurationLastVideo = DurréeVideo

                ' La cellule 4 sert a deux choses dans ce programme (liste des positions restantes
                ' "n; p1; p2; ..." et, ailleurs, la date Exif). Quand elle contient autre chose
                ' qu'une liste de positions - "Faux"/"Vrai" venus de la reprise du diaporama, une
                ' date, une liste tronquee - le tirage plus bas sortait du tableau et la video
                ' n'etait pas affichee ("Erreur de programation dans module AfficheVideo").
                If Not EstUneListeDePositions(Form2.DataGridView2.Rows(NumVideo).Cells(4).Value) Then
                    Form2.DataGridView2.Rows(NumVideo).Cells(4).Value = ""
                End If

                If Form2.DataGridView2.Rows(NumVideo).Cells(4).Value = "" Then

                    NextPos = 0
                    NombreDePosition = Int(MyDuration / DurréeVideo) + 1 - mdulo
                    eclatement = Str(NombreDePosition) + ";"
                    For j = 0 To MyDuration - 1 Step DurréeVideo
                        NextPos = j

                        eclatement = eclatement + Str(NextPos) + ";"
                    Next
                    LastPos = (MyDuration - NextPos)
                    If LastPos < 5 Then ' si reste fin video - de 5 seconde alors l'integré dans la derniere position
                        If DurréeVideo > LastPos And NombreDePosition > 2 Then ' a condition que la durrée defini soit superieur à 5 secondes
                            eclat2 = Split(eclatement, ";")
                            eclat2(0) = Str(NombreDePosition - 1)
                            eclatement = ""
                            For j = 0 To (NombreDePosition - 1)
                                eclatement = eclatement + Str(eclat2(j)) + ";"
                            Next
                        End If
                    End If

                    Form2.DataGridView2.Rows(NumVideo).Cells(4).Value = eclatement
                End If

                eclatement = Form2.DataGridView2.Rows(NumVideo).Cells(4).Value
                'Debug.Print("avant:" & eclatement)
                eclat = Split(eclatement, ";")
                nbeclat = Val(eclat(0))
                Dim random As New Random(), p As Integer
                p = random.Next(1, Val(nbeclat) + 1)
                'p = Int(rnd() * nbeclat + 0.9)
                pos = Val(eclat(p))
                eclatement = Str(nbeclat - 1) + ";"
                For k = 1 To Val(nbeclat)
                    If p <> k Then eclatement = eclatement + Str(eclat(k)) + ";"
                Next
                Form2.DataGridView2.Rows(NumVideo).Cells(4).Value = eclatement
                Form2.DataGridView2.Rows(NumVideo).DefaultCellStyle.ForeColor = Color.LightSkyBlue

                'MsgBox("Eclatement:" & eclatement & " pos:" & pos)

                'Form3.AxWindowsMediaPlayer1.Ctlcontrols.currentPosition = pos

                Application.DoEvents()
                If Val(eclat(0)) = 1 Then
                    Form2.DataGridView2.Rows(NumVideo).Cells(4).Value = ""
                    Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                    Form2.DataGridView2.Rows(NumVideo).DefaultCellStyle.ForeColor = Color.Empty      ' (la couleur du tableau : noir sur fond clair, clair sur fond sombre)
                End If

                'Debug.Print("apres:" & eclatement)

            Else

                Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
            End If
            '''''


            ext = "file:///" : If Left(UCase(Nom), 4) = "HTTP" Then ext = ""

            Form3.VlcControl1.SetPause(True)   ' (et non Pause(), qui bascule : deja figee par Form1, elle serait relancee)

            'Normalization du volume
            'Form3.VlcControl1.SetMedia(New Uri(ext & Nom), "--audio-filter=normvol", "--norm-max-level=1.0", "--norm-buff-size=10")
            'Form3.VlcControl1.SetMedia(New Uri(ext & Nom), "--audio-filter=compressor", "--compressor-rms-threshold=-20.0", "--compressor-ratio=3.0", "--compressor-attack=1.5", "--compressor-release=300.0", "--compressor-makeup-gain=15.0")

            ' VLC demarre le media suivant avec le volume qu'il a maintenant : on le met des
            ' a present au niveau du diaporama, sinon la video partirait au volume du film ou
            ' de la chaine precedente (c'est le coup de son du debut).
            ' Video deja ouverte sur le lecteur cache (PrechargementVideo) : on bascule dessus, sans
            ' ouverture ni attente. Sinon ouverture normale.
            ' Extrait qui va etre joue : le lissage du son prend la sonie mesuree a cet endroit.
            NiveauSonore.ExtraitDebut = CDbl(pos)
            NiveauSonore.ExtraitDuree = Math.Min(CDbl(MyDuration), CDbl(DurréeVideo))
            Dim prechargee As Boolean = PrechargementVideo.Prendre(CStr(Nom), pos)
            PreparerLeVolumeAvantChangement(Form3.VlcControl1, If(VideoWithAudio = True, If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide), 0))
            If Not prechargee Then
                ' Videos animees, meme lecteur pour la video suivante : l'image de celle qui finit
                ' reste posee par-dessus pendant l'ouverture (voir FormTransitionVideo).
                If Form3.VlcControl1.Visible Then FormTransitionVideo.Couvrir(Form3)
                Form3.AttendreLeLecteur(Form3.VlcControl1)     ' fin de son arret en cours, s'il y en a un
                CouleursDuDiaporama.PourLaVideo(Form3.VlcControl1)     ' noir et blanc ou couleurs, selon les reglages
                Form3.VlcControl1.SetMedia(New Uri(ext & Nom))
                Application.DoEvents()
            End If

            ' Muet AVANT de demarrer. Sinon VLC joue les premieres secondes au volume qu'il avait
            ' (celui d'un film, ou 100 %), et le volume du diaporama n'etait applique qu'apres :
            ' d'ou le coup de son au debut de chaque video. Le son est retabli plus bas, une fois
            ' le volume regle et la video positionnee.
            ' (Video prechargee et deja lancee en avance : elle joue deja, au bon volume ; on ne
            '  la coupe pas et on ne la relance pas.)
            Dim dejaEnLecture As Boolean = prechargee AndAlso Form3.VlcControl1.IsPlaying
            If Not dejaEnLecture Then
                CouperLeSonAvantDemarrage(Form3.VlcControl1)
                Form3.VlcControl1.Play()
            End If
            'Form3.VlcControl1.Size = New System.Drawing.Size(Form3.Width, Form3.Height = 450 + tailleBordureFenetre)
            'MsgBox(Form3.VlcControl1.Audio.Volume)

            ' Video prechargee : elle passe devant l'ancienne des qu'elle affiche des images (elle
            ' avance), SANS attendre le reglage du son ci-dessous. Avant, elle jouait cachee
            ' derriere l'image figee pendant cette attente : arret sur image visible, puis saut.
            If prechargee Then
                Dim departVideo As Stopwatch = Stopwatch.StartNew()
                Do Until (Form3.VlcControl1.IsPlaying AndAlso Form3.VlcControl1.Time > 30) OrElse departVideo.ElapsedMilliseconds > 400
                    Application.DoEvents()
                    Threading.Thread.Sleep(5)
                Loop
                ' Videos animees : l'image de la video qui finit est posee par-dessus avant que la
                ' nouvelle passe devant ; elle se retirera a la fin (FormTransitionVideo.Retirer).
                FormTransitionVideo.Couvrir(Form3)
                Form3.FinirLaBascule()
            End If

            ' On tient le silence tant que VLC n'a pas ouvert sa sortie audio, puis volume voulu.
            If dejaEnLecture Then
                ' deja reglee par PrechargementVideo.LancerEnAvance (et surveillee 2 s)
            ElseIf VideoWithAudio = True Then
                ReglerLeSonApresDemarrage(Form3.VlcControl1, If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide))
            Else
                ReglerLeSonApresDemarrage(Form3.VlcControl1, 0)
            End If

            Call SaveLog(Nom, "VIDEO ; " & Format(pos))


            i = 0
            Dim Chrono As Stopwatch = New Stopwatch
            Chrono.Start()
            'attend le debut de lecture




            Do Until Form3.VlcControl1.IsPlaying = True
                Application.DoEvents()

                If Diffusion.Mode = "FILM" Or Diffusion.Mode = "SAT" Then GoTo fin : Exit Do

                If (Chrono.ElapsedMilliseconds / 1000) > TempsDattenteAcceptable Then
                    SaveLogError(Nom, "Erreur video ne demarre pas") : ErrorReadVideo = 1
                    Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                    GoTo fin
                    Exit Do
                    ' echec lecture apres  15 second sortir
                End If

                'wait(1)
                'i = i + 1 : If i > TempsDattenteAcceptable Then SaveLogError(Nom, "Erreur video ne demarre pas") : ErrorReadVideo = 1 : Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1 : GoTo fin : Exit Do  ' echec lecture apres 2 minutes sortir
            Loop


            Chrono.Reset()
            Chrono.Start()

            If Diffusion.Mode = "FILM" Or Diffusion.Mode = "SAT" Then GoTo fin

            i = 0
            'jj = Form3.AxWindowsMediaPlayer1.Ctlcontrols.currentPosition

            If pos > 0 Then Form3.VlcControl1.Time = pos * 1000
            'en milliseconds   1 sec = 1000 milliseconds

            Do Until Form3.VlcControl1.Time > (pos * 1000) + 1

                If (Chrono.ElapsedMilliseconds / 1000) > TempsDattenteAcceptable Then
                    SaveLogError(Nom, "Erreur positionement impossible") : ErrorReadVideo = 1
                    Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1
                    Form3.VlcControl1.Pause()
                    GoTo fin
                    Exit Do
                    ' echec lecture apres 15 second sortir
                End If


                Application.DoEvents()
                'wait(1)
                'i = i + 1 : If i > TempsDattenteAcceptable Then SaveLogError(Nom, "Erreur positionement impossible") : ErrorReadVideo = 1 : Form2.DataGridView2.Rows(NumVideo).Cells(0).Value = 1 : GoTo fin : Exit Do  ' echec lecture apres 2 minutes sortir
                If FlagExit = True Then Application.DoEvents() : Exit Do
            Loop



            ' Videos animees, apres une photo : la photo reste posee par-dessus, puis se retire.
            If Form3.PictureBox1.Visible Then FormTransitionVideo.Couvrir(Form3)
            Form3.PictureBox1.Visible = False


            Form3.VlcControl1.Visible = True
            Form3.VlcControl1.Top = 0
            Form3.VlcControl1.Left = 0
            Form3.VlcControl1.Height = Form3.ClientSize.Height
            Form3.VlcControl1.Width = Form3.ClientSize.Width
            ' Video prechargee : elle joue, elle passe devant l'ancienne (restee affichee jusque-la).
            Form3.FinirLaBascule()

            ' La video est lancee et positionnee : on met le volume du diaporama, PUIS on retablit
            ' le son (coupe juste avant le demarrage, voir plus haut).
            SetVideoVolSlide(VolumeVideoSlide)
            VideoWithAudio_SWITCH_ON(VideoWithAudio)
            ' Video muette (quelle qu'en soit la raison) : la musique continue.
            MusiquePendantLaVideo()

            If AfficheDate = True Or AfficheHeureDeShabbat = True Then
                AfficheDateDeLaVideo(Nom)
            Else
                MasqueLegende()
            End If

            ' Pendant qu'elle joue, la video suivante s'ouvre sur l'autre lecteur (une seconde plus
            ' tard, pour ne pas provoquer d'a-coup au debut de celle-ci).
            PrechargementVideo.PreparerLaSuivante(CInt(NumVideo))

            ' Tout est en place (son, legende, video suivante) : l'ancienne image se retire, sans
            ' a-coup puisque plus rien n'occupe le programme.
            ' Videos animees : la video se met a bouger (avancer, reculer, glisser, voir
            ' VideoVivante), sous l'ancienne image qui la couvre encore.
            VideoVivante.Demarrer(Form3, FormatDeLaVideo(Form3.VlcControl1, 200), CDbl(DurationLastVideo),
                                  Function() Diffusion.Mode = "VIDEO" AndAlso Form3.VlcControl1.Visible AndAlso Not Form3.PictureBox1.Visible)
            FormTransitionVideo.Retirer()


        Catch ex As Exception
            SaveLogError(Nom, "Erreur de programation dans module AfficheVideo")
            ' Detail (type, message, ligne) : le message ci-dessus ne dit pas ce qui a echoue.
            DiagLog("AfficheVideo " & CStr(Nom) & " : " & ex.GetType().Name & " : " & ex.Message &
                    " | " & Replace(If(ex.StackTrace, ""), vbCrLf, " <- "))
        End Try
fin:
        ' Sortie anticipee (erreur, changement de mode) : l'ancien lecteur ne doit pas rester devant,
        ' ni le cache de transition.
        Form3.FinirLaBascule()
        FormTransitionVideo.Masquer()

    End Sub


    Sub AfficheTitre(n As String, p As String, Optional type As String = "")
        If ConfierAuFilPrincipal(Sub() AfficheTitre(n, p, type), "AfficheTitre") Then Exit Sub


        Form3.VlcControl1.Visible = False
        Form3.PictureBox1.Top = 0
        Form3.PictureBox1.Left = 0
        Form3.PictureBox1.Height = Form3.ClientSize.Height
        Form3.PictureBox1.Width = Form3.ClientSize.Width


        Form3.PictureBox1.Visible = True

        Application.DoEvents()
        trouve = 0
        For i = Len(n) To 1 Step -1
            If Mid(n, i, 1) = "\" Then trouve = i : Exit For
        Next
        If trouve > 0 Then n = Mid(n, trouve + 1)


        ' Le texte recu peut tenir sur plusieurs lignes ("chaine" + "en preparation ...") : la
        ' premiere ligne devient le titre, les suivantes la mention en dessous. Avec une duree,
        ' c'est elle qui passe en dessous.
        Dim morceaux As String() = Split(n, vbCrLf)
        Dim titre As String = morceaux(0).Trim()
        Dim mention As String = String.Join(" ", morceaux.Skip(1).Select(Function(s) s.Trim())).Trim()
        If p <> "*" Then mention = "Durée : " & p.Trim()

        AfficheCarteTitre(titre, mention, type.Trim())
        wait(2)
    End Sub

    ''' <summary>La "carte" affichee avant un film ou une chaine : fond sombre, titre en grandes
    ''' lettres en relief argente (le meme style que la montre et les bandes noires), mention en
    ''' dessous, et le genre ("FILM", "IPTV") en petit au-dessus. Remplace l'ancien texte blanc
    ''' colle en haut a gauche de l'ecran.</summary>
    Sub AfficheCarteTitre(titre As String, mention As String, genre As String)
        Dim largeur As Integer = Math.Max(320, Form3.PictureBox1.ClientSize.Width)
        Dim hauteur As Integer = Math.Max(240, Form3.PictureBox1.ClientSize.Height)

        Dim image As New Bitmap(largeur, hauteur, Imaging.PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(image)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

            ' Fond : noir, legerement eclairci vers le haut (evite l'aplat brut).
            Using fond As New Drawing2D.LinearGradientBrush(New Rectangle(0, 0, largeur, hauteur),
                                                            Color.FromArgb(26, 26, 32), Color.Black,
                                                            Drawing2D.LinearGradientMode.Vertical)
                g.FillRectangle(fond, 0, 0, largeur, hauteur)
            End Using

            ' Titre (et mention en dessous) en relief argente, centres.
            Dim marge As Integer
            Dim bloc As Bitmap = FormLieuBande.RendreLieu(titre, mention,
                                                          0.82F * largeur, 0.62F * hauteur, 0.13F * hauteur, marge)
            Dim hautDuBloc As Integer = hauteur \ 2
            If bloc IsNot Nothing Then
                Using bloc
                    hautDuBloc = (hauteur - bloc.Height) \ 2
                    g.DrawImage(bloc, (largeur - bloc.Width) \ 2, hautDuBloc)
                End Using
            End If

            ' Genre en petit au-dessus du titre, gris, espace, avec un filet de chaque cote.
            If genre <> "" Then
                Dim taille As Single = Math.Max(10.0F, 0.022F * hauteur)
                Using familleGenre As FontFamily = FormHorloge.PoliceChoisieOu("Arial"),
                      police As New Font(familleGenre, taille, If(familleGenre.IsStyleAvailable(FontStyle.Bold), FontStyle.Bold, FontStyle.Regular)),
                      encre As New SolidBrush(Color.FromArgb(150, 150, 160)),
                      filet As New Pen(Color.FromArgb(70, 150, 150, 160), Math.Max(1.0F, taille / 12.0F))
                    Dim texte As String = String.Join(" "c, genre.ToUpper().ToCharArray())
                    Dim mesure As SizeF = g.MeasureString(texte, police)
                    Dim x As Single = (largeur - mesure.Width) / 2.0F
                    Dim y As Single = Math.Max(taille, hautDuBloc - mesure.Height * 2.0F)
                    g.DrawString(texte, police, encre, x, y)
                    Dim milieu As Single = y + mesure.Height / 2.0F
                    Dim longueur As Single = mesure.Width * 0.8F
                    g.DrawLine(filet, x - 20 - longueur, milieu, x - 20, milieu)
                    g.DrawLine(filet, x + mesure.Width + 20, milieu, x + mesure.Width + 20 + longueur, milieu)
                End Using
            End If
        End Using

        DefinirImageAffichee(image)
        Form3.PictureBox1.Refresh()
        Application.DoEvents()
    End Sub

    Function arrondi(x As Date, i As Integer) As DateTime

        Dim Heure As Integer = x.Hour
        Dim Minute As Integer = x.Minute

        a = Math.Ceiling(Minute / i) * i
        If Minute Mod i = 0 Then a = a + i

        'y = 0u
        'If a = 60 Then a = 0 : y = 1
        'Dim RoundedTime = New DateTime(2000, 1, 1, Heure + y, a, 0)

        Dim RoundedTime As DateTime = DateSerial(Now.Year, Now.Month, Now.Day) + " " + TimeSerial(Heure, a, 0)
        Return RoundedTime
    End Function

    Sub retablieTousLesVolumes()

        ' Reglages ouverts : leur silence tient jusqu'a leur fermeture (apercu ferme, tache terminee...).
        If SilenceDesReglages.Actif Then Exit Sub

        'If PIPvolume > 0 Then Exit Sub
        If Music = False Then Exit Sub

        If Form3.VlcControl1.IsPlaying = True Then
            If VideoWithAudio = True Then
                SetVideoVolSlide(VolumeVideoSlide)
                VideoWithAudio_SWITCH_ON(VideoWithAudio)
            Else
                SetMusicVolSlide(VolumeMusicSlide)
            End If
        Else
            Form3.VlcControl1.Audio.IsMute = True
            SetMusicVolSlide(VolumeMusicSlide)

        End If


    End Sub

    Function FormDeSlide(x, h)
        trouve = False

        Try
            For Each form In My.Application.OpenForms
                If form.text = x Then trouve = True : Exit For
                If form.handle = h Then Stop
            Next
        Catch ex As Exception

        End Try

        Return trouve

    End Function

    Sub CloseAllNewWindows()

        EnumWindows2(Callback2, IntPtr.Zero)

        Exit Sub




        Dim myProcesses() As Process = Process.GetProcesses

        For Each p As Process In myProcesses
            nom = p.MainWindowTitle
            If nom <> "" Then

                If FormDeSlide(nom, h) Then nom = ""
                If UCase(nom).contains("SLIDE") Then nom = ""
                If UCase(nom).contains("PRIME") Then nom = ""
                If UCase(nom).contains("VOLET TEAMVIEWER") Then nom = ""
                q = "vb.net vlc.dotnet"
                'If nom.contains(q) Then Stop
                If nom <> "" Then
                    If WindowsList.Contains(nom) = False Then CloseWindow(nom) ': Debug.Print("à Fermer " & nom)
                End If
            End If
        Next

    End Sub
    Sub SaveAllWindows()

        EnumWindows(Callback, IntPtr.Zero)

    End Sub


    Sub AfficheDateDeLaPhoto(nom)
        'trouve = 0
        'If AfficheHeureDeShabbat = True Then
        'If FullScreen = True And HoraireDeShabbatDansCadran = True Then

        trouve = AfficheHeureDeChabbatDansCadran()

        '        trouve = 1
        'Else
        'Form3.Label4.Visible = False
        'Form3.TextBox4.Visible = False
        'End If
        'End If

        result = ""
        espace = ""
        ChavouaTov = ""
        OublierDernierLieu()
        If AfficheDate = True And AfficheHeureDeShabbat = True Then espace = " "
        If AfficheDate = True Then result = LegendeLieuDate(nom)
        If AfficheHeureDeShabbat = True And trouve = 0 Then
            ChavouaTov = espace & "ChavouaTov " & Shabbat.Fin
        End If
        resultOK = result & ChavouaTov
        ' Legende sur fond transparent, au-dessus des photos comme des videos (voir AfficheLegende).
        AfficheLegende(CStr(resultOK))


    End Sub
    ''' <summary>La fenetre principale (Form3) telle qu'elle est affichee, trouvee SANS passer par
    ''' "Form3" : en VB, l'instance par defaut d'une fenetre est propre a chaque thread, et la
    ''' nommer depuis le thread d'un lecteur PIP en fabrique une nouvelle, invisible.</summary>
    ''' <summary>Banc d'essai seulement : une fenetre qui tient lieu d'image de Slide (Nothing dans Slide).</summary>
    Friend FenetreDeBanc As Form

    Function FenetrePrincipale() As Form
        If FenetreDeBanc IsNot Nothing Then Return FenetreDeBanc
        Try
            For Each f As Form In Application.OpenForms
                If TypeOf f Is Form3 Then Return f
            Next
        Catch ex As Exception
            ' liste des fenetres modifiee pendant la lecture : tant pis pour cette fois
        End Try
        Return Nothing
    End Function

    Function ConfierAuFilPrincipal(action As Action, quoi As String) As Boolean
        ' Les lecteurs PIP tournent dans leur propre thread (MTA). La, "Form3" designerait une
        ' fenetre principale NEUVE et invisible propre a ce thread, dont le navigateur web exige
        ' un thread STA : erreur dans une procedure de fenetre -> le programme se ferme tout seul.
        ' Renvoie True si l'action a ete confiee au thread de l'interface (l'appelant s'arrete la).
        If Threading.Thread.CurrentThread.GetApartmentState() = Threading.ApartmentState.STA Then Return False
        Dim principale As Form = FenetrePrincipale()
        Try
            If principale IsNot Nothing AndAlso principale.IsHandleCreated Then
                principale.BeginInvoke(action)
            Else
                DiagLog(quoi & " : fenetre principale absente, appel ignore (thread du PIP)")
            End If
        Catch ex As Exception
            DiagLog(quoi & " : renvoi au thread de l'interface impossible : " & ex.Message)
        End Try
        Return True
    End Function

    Function AfficheHeureDeChabbatDansCadran()
        ' Appelee aussi par les lecteurs PIP, qui tournent dans leur propre thread. La, "Form3" et
        ' "FormHorloge" designeraient des fenetres neuves propres a ce thread (d'ou l'erreur
        ' "Application-defined or object-defined error" et les horaires qui disparaissaient de la
        ' montre) : on confie donc le travail au thread de l'interface, sans l'attendre.
        Dim principale As Form = FenetrePrincipale()
        If principale IsNot Nothing AndAlso principale.InvokeRequired Then
            Try
                principale.BeginInvoke(New Action(Sub() AfficheHeureDeChabbatDansCadran()))
            Catch ex As Exception
                DiagLog("Chabbat dans le cadran : renvoi au thread de l'interface impossible : " & ex.Message)
            End Try
            Return 0   ' les lecteurs n'utilisent pas le resultat
        End If

        Try

            trouve = 0

            ' Les anciennes zones de texte de Form3 ne servent plus : une TextBox ne peut pas avoir
            ' de fond transparent, et sa hauteur de ligne laisse un vide sous les chiffres qui
            ' fausse le centrage. Les horaires sont dessines dans la montre elle-meme.
            Form3.TextBox4.Visible = False
            Form3.TextBox6.Visible = False

            ' "Dans le cadran" = dans la montre : on suit donc la montre (affichee en plein ecran comme
            ' en fenetre) et non plus le plein ecran. Sans montre, trouve reste a 0 et l'appelant
            ' affiche les horaires en bas avec la date, comme avant.
            If AfficheHeureDeShabbat = True And HoraireDeShabbatDansCadran = True And AfficheClock = True Then
                ' Debut centre dans la moitie haute de la montre, fin dans la moitie basse.
                FormHorloge.DefinirHoraires(CStr(Shabbat.Debut), CStr(Shabbat.Fin))
                trouve = 1
            Else
                FormHorloge.DefinirHoraires("", "")
                Try
                    Form3.Label4.Visible = False
                Catch ex As Exception
                End Try
            End If

        Catch ex As Exception
            DiagLog("Chabbat dans le cadran : ERREUR " & ex.GetType().Name & " : " & ex.Message)
        End Try

        ' Trace de diagnostic (temporaire), ecrite seulement quand l'etat change : cette fonction
        ' est appelee a chaque photo et a chaque video.
        Static dernierEtat As String = ""
        Dim etat As String = "Chabbat dans le cadran : affiche=" & (trouve = 1) &
            " | AfficheHeureDeShabbat=" & AfficheHeureDeShabbat & " HoraireDansCadran=" & HoraireDeShabbatDansCadran &
            " AfficheClock=" & AfficheClock & " | debut='" & Shabbat.Debut & "' fin='" & Shabbat.Fin & "'"
        If etat <> dernierEtat Then DiagLog(etat) : dernierEtat = etat

        AfficheHeureDeChabbatDansCadran = trouve

    End Function

    ''' <summary>Legende "lieu [date]" en bas a gauche, sur fond transparent (FormLegende), au-dessus
    ''' des photos comme des videos. Form3.TextBox1 reste masquee mais garde texte et police : la
    ''' banniere Chabbat (Form3.TextBox2) se cale au-dessus d'elle d'apres sa hauteur.
    ''' Quand l'image laisse des bandes noires sur les cotes, le lieu (a gauche) et la date (a
    ''' droite) y sont ecrits en grand et ne sont plus repetes en bas.</summary>
    ''' <summary>Taille normale (100 %) de la legende du bas, en points : proportionnelle a la
    ''' hauteur de la fenetre, comme dans la version d'origine.</summary>
    Function TaillePointsLegende(Optional fenetre As Form = Nothing) As Single
        Dim f As Form = If(fenetre, Form3)
        Return CSng(f.ClientSize.Height / 70 + 1)
    End Function

    ''' <summary>
    ''' Bande du bas (ou du haut, selon le coin choisi dans Form4) reservee a la legende (lieu · date) a la taille reglee, MEME quand elle n'est
    ''' pas affichee : derriere le PIP les photos continuent de tourner et la suivante en aura une.
    ''' Toute la largeur est prise, le texte pouvant etre long.
    '''
    ''' Calculee ici, a chaque photo, et JAMAIS depuis la fenetre du PIP : celui-ci vit dans un
    ''' autre thread, ou "Form3" designerait une deuxieme instance creee par VB (les instances par
    ''' defaut sont propres a chaque thread) au lieu de la fenetre affichee.
    ''' </summary>
    Public ZoneLegendeReservee As Rectangle = Rectangle.Empty

    Sub CalculeLaZoneDeLaLegende()
        Try
            Dim principale As Form = FenetrePrincipale()      ' sans passer par l'instance par defaut
            If principale Is Nothing OrElse principale.IsDisposed Then Exit Sub
            Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
            Dim hauteur As Integer = FormLegende.HauteurReservee(principale, TaillePointsLegende(principale), LegendeTaillePourcent)
            If hauteur > 0 AndAlso hauteur < zone.Height Then
                ZoneLegendeReservee = New Rectangle(zone.Left, If(LegendeCoin.StartsWith("H"), zone.Top, zone.Bottom - hauteur), zone.Width, hauteur)
            End If
        Catch ex As Exception
            ' un calcul de mise en page ne doit jamais interrompre le diaporama
        End Try
    End Sub

    ''' <summary>Derniere legende affichee (pour la recaler aussitot apres un passage plein ecran /
    ''' fenetre, sans attendre la photo suivante).</summary>
    Private _derniereLegende As String = ""
    Private _legendeAffichee As Boolean

    ''' <summary>Apres un passage plein ecran <-> fenetre : PIP, legende, bandes, montre et pastille
    ''' de la normalisation se recalent TOUT DE SUITE sur la nouvelle taille (avant : les PIP
    ''' attendaient la fin du clic, la legende et les bandes la photo suivante).</summary>
    Sub RecalerApresChangementDeMode()
        Try
            Form3.Update()
            Form12.RecalerMaintenant(Form3)
            ' La photo en cours est redessinee (et recadree en 16:9) a la nouvelle taille : sinon elle
            ' gardait l'ancien cadrage jusqu'a la photo suivante, et la localite, calculee pour la
            ' nouvelle taille, ne tombait plus dans la bande noire.
            If Form3.PictureBox1.Visible AndAlso Not DernierEstVideo AndAlso _photoAffichee <> "" AndAlso
               IO.File.Exists(_photoAffichee) Then
                If PhotoAlbum Then
                    AffichePhotoAlbum(_photoAffichee)
                ElseIf FormatPhoto169 Then
                    AffichePhoto169(_photoAffichee)
                Else
                    AffichePhoto413(_photoAffichee)
                End If
            End If
            If _legendeAffichee AndAlso _derniereLegende <> "" Then AfficheLegende(_derniereLegende)
            AfficheMontre()
            NormalisationMusique.RecalerLaPastille()
        Catch ex As Exception
            DiagLog("Recalage apres plein ecran : " & ex.Message)
        End Try
    End Sub

    Sub AfficheLegende(texte As String)
        _derniereLegende = texte : _legendeAffichee = True
        Dim taillePoints As Single = TaillePointsLegende()   ' meme taille qu'avant
        ' Place a garder libre en bas, et PIP remonte s'il mord dessus.
        CalculeLaZoneDeLaLegende()
        Form12.EcarterDeLaLegende()
        Form3.TextBox1.Visible = False
        ' Police a la taille reellement affichee : la banniere Chabbat se cale au-dessus.
        Form3.TextBox1.Font = New Font("Arial", taillePoints * LegendeTaillePourcent / 100.0F)
        Form3.TextBox1.Text = texte
        texte = AfficheDansLesBandes(texte)
        If texte = "" Then
            FormLegende.Masquer()
        Else
            ' Taille changee a la souris sur la legende (molette / glisser) : on l'enregistre.
            RemoveHandler FormLegende.TailleChoisie, AddressOf LegendeRedimensionneeALaSouris
            AddHandler FormLegende.TailleChoisie, AddressOf LegendeRedimensionneeALaSouris
            FormLegende.AfficherSur(Form3, texte, taillePoints, premierPlan:=FullScreen, pourcent:=LegendeTaillePourcent, coin:=LegendeCoin,
                                    centreeSousPip:=LegendeCentreeSousPip)
        End If
    End Sub

    ''' <summary>Enregistre la taille du texte des bandes noires choisie a la souris.</summary>
    Sub BandesRedimensionneesALaSouris(pourcent As Integer)
        BandesTaillePourcent = pourcent
        My.Settings.BandesTaillePourcent = pourcent
        My.Settings.Save()
    End Sub

    ''' <summary>Enregistre la taille de la legende choisie a la souris (comme pour la montre).</summary>
    Sub LegendeRedimensionneeALaSouris(pourcent As Integer)
        LegendeTaillePourcent = pourcent
        My.Settings.LegendeTaillePourcent = pourcent
        My.Settings.Save()
        Form3.TextBox1.Font = New Font("Arial", TaillePointsLegende() * pourcent / 100.0F)
    End Sub

    Sub MasqueLegende()
        _legendeAffichee = False
        Form3.TextBox1.Visible = False
        FormLegende.Masquer()
        FormLieuBande.Masquer()
    End Sub

    ' La seconde instance de FormLieuBande (la date dans la bande de DROITE) a ete supprimee :
    ' le lieu, le pays et la date tiennent maintenant dans la seule bande de gauche.

    ''' <summary>Rapport largeur/hauteur de l'image telle qu'elle sera a l'ecran, selon le mode
    ''' d'affichage (0 = pas de bandes ou inconnu). D'apres le dernier LegendeLieuDate (ExifLieu).</summary>
    Function RatioDeLImageAffichee() As Double
        If DernierEstVideo Then Return DernierRatio          ' VLC : image entiere, rotation appliquee
        If PhotoAlbum = True Then Return 0                   ' mosaique : pas de bandes
        If FormatPhoto169 = True Then
            If DernierRatio < 1.1 Then Return DernierRatio   ' photo en hauteur : Zoom
            ' Photo en largeur : recadree en bande 16:9 au plus (voir AffichePhoto169).
            Dim ecran As Double = Form3.ClientSize.Width / Math.Max(1, Form3.ClientSize.Height)
            Return Math.Min(ecran, Math.Max(DernierRatio, Math.Min(SeizeNeuvieme, ecran)))
        End If
        Return DernierRatioBrut                              ' mode 4:3 : Zoom, sans rotation EXIF
    End Function

    ''' <summary>Lieu en grand dans la bande noire de gauche, date dans celle de droite, quand
    ''' l'image affichee en laisse d'assez larges. Renvoie la legende du bas debarrassee de ce
    ''' qui est deja affiche dans les bandes.</summary>
    Function AfficheDansLesBandes(texte As String) As String
        Dim ratioAffiche As Double = RatioDeLImageAffichee()

        ' Taille du texte de la bande, changee a la souris dessus : on l'enregistre.
        FormLieuBande.DefinirPourcent(BandesTaillePourcent)
        FormLieuBande.DefinirPositionVerticale(BandesPosition)
        BandesRelief = My.Settings.BandesRelief
        FormLieuBande.DefinirRelief(BandesRelief)
        FormLieuBande.DefinirPolice(BandesPolice)
        RemoveHandler FormLieuBande.TailleChoisie, AddressOf BandesRedimensionneesALaSouris
        AddHandler FormLieuBande.TailleChoisie, AddressOf BandesRedimensionneesALaSouris
        ' BandesActives (Form4) : sans cette option, rien dans les bandes, tout reste en bas comme avant.
        Dim bandes As Boolean = BandesActives AndAlso AfficheDate = True AndAlso ratioAffiche > 0 AndAlso
                                FormLieuBande.BandeUtilisable(Form3, ratioAffiche)

        ' Trace de diagnostic, ecrite seulement quand l'etat change (appelee a chaque photo).
        Static dernierEtatBandes As String = ""
        Dim etatBandes As String = "Bandes : affichees=" & bandes &
            " | ratio=" & Math.Round(ratioAffiche, 3) & " (photo=" & Math.Round(DernierRatio, 3) &
            " brut=" & Math.Round(DernierRatioBrut, 3) & " video=" & DernierEstVideo & ")" &
            " | fenetre=" & Form3.ClientSize.Width & "x" & Form3.ClientSize.Height & " ppp=" & Form3.DeviceDpi &
            " bande=" & FormLieuBande.LargeurBande(Form3.ClientSize.Width, Form3.ClientSize.Height, ratioAffiche) & "px" &
            " | AfficheDate=" & AfficheDate & " AfficheLieu=" & AfficheLieu & " 169=" & FormatPhoto169 &
            " album=" & PhotoAlbum & " date=" & DerniereDate.HasValue & " lieu='" & DernierLieu.Localite & "'"
        If etatBandes <> dernierEtatBandes Then DiagLog(etatBandes) : dernierEtatBandes = etatBandes

        ' Tout va dans la bande de GAUCHE (demande de Jacob) : la ville en grand, le pays puis la
        ' date en dessous en plus petit. La bande de droite reste vide.
        Dim quand As String = ""
        If bandes AndAlso DerniereDate.HasValue Then
            quand = Format(DerniereDate.Value, "MMMM yyyy")                        ' "mars 2023"
            quand = Char.ToUpper(quand(0)) & quand.Substring(1)
        End If
        Dim lieuAffichable As Boolean = bandes AndAlso AfficheLieu = True AndAlso
                                        Not String.IsNullOrEmpty(DernierLieu.Localite)

        If lieuAffichable Then
            FormLieuBande.AfficherSur(Form3, DernierLieu, DernierLieuDeduit, ratioAffiche,
                                      premierPlan:=FullScreen, quand:=quand)
        ElseIf quand <> "" Then
            ' Pas de lieu : la date prend sa place, en grand.
            FormLieuBande.AfficherTexteSur(Form3, quand, "", ratioAffiche, premierPlan:=FullScreen)
        Else
            FormLieuBande.Masquer()
        End If

        ' Ce qui est ecrit dans la bande n'est plus repete dans la legende du bas.
        If lieuAffichable Then
            ' Retire "lieu · " (avec le separateur), sinon le lieu seul.
            If DerniereLegendeLieu <> "" AndAlso texte.StartsWith(DerniereLegendeLieu & SeparateurLieuDate) Then
                texte = texte.Substring((DerniereLegendeLieu & SeparateurLieuDate).Length)
            ElseIf DerniereLegendeLieu <> "" AndAlso texte.StartsWith(DerniereLegendeLieu) Then
                texte = texte.Substring(DerniereLegendeLieu.Length)
            End If
        End If
        If quand <> "" AndAlso DerniereLegendeDate <> "" Then
            ' Retire " · date" (avec le separateur), sinon la date seule.
            texte = texte.Replace(SeparateurLieuDate & DerniereLegendeDate, "").Replace(DerniereLegendeDate, "")
        End If

        Do While texte.Contains("  ")
            texte = texte.Replace("  ", " ")
        Loop
        Return texte.Trim()
    End Function

    Sub AfficheDateDeLaVideo(nom)

        OublierDernierLieu()


        Form3.TextBox1.Text = ""
        espace = ""
        If AfficheDate = True And AfficheHeureDeShabbat = True Then espace = " "
        If AfficheDate = True Then result = LegendeLieuDate(nom)
        'If AfficheDate = True And result = "" Then
        ' Dim dt As Date
        ' Dim infoReader As System.IO.FileInfo
        ' infoReader = My.Computer.FileSystem.GetFileInfo(nom)
        ' dt = infoReader.LastWriteTime
        ' result = "[" & Format(dt, "MMM yy") & "]"
        'End If

        If AfficheDate = True Then

            Form3.TextBox1.Font = New Font("Arial", (Form3.ClientSize.Height / 70) + 1)
            Form3.TextBox1.Text = result
        End If


        'If AfficheHeureDeShabbat = True Then
        'Form3.TextBox1.Text = Form3.TextBox1.Text & espace & "ChavouaTov " & Shabbat.Fin
        'End If

        ''''''''''''''''''''
        'trouve = 0
        'If AfficheHeureDeShabbat = True Then
        'If FullScreen = True And HoraireDeShabbatDansCadran = True Then

        trouve = AfficheHeureDeChabbatDansCadran()

        'trouve = 1
        ' Else
        'Form3.Label4.Visible = False
        'Form3.TextBox4.Visible = False
        'End If
        'End If
        ''''''''''''''''''''''''''''

        ' Legende sur fond transparent, au-dessus des photos comme des videos (voir AfficheLegende).
        AfficheLegende(Form3.TextBox1.Text)


    End Sub


    ''' <summary>L'ancienne case "Gadget Clock BIG" (montre de 480 au lieu de 128) est remplacee par
    ''' la taille en %. Si elle etait cochee, on la convertit une seule fois (x 480/128 = 375 %) pour
    ''' que la montre garde la meme taille a l'ecran, puis on la remet a False. Le reglage
    ''' My.Settings.ClockBIG n'existe plus que pour cette lecture : ne pas le supprimer.</summary>
    Sub ConvertirAncienneOptionClockBIG()
        If My.Settings.ClockBIG = False Then Exit Sub
        ClockTaillePourcent = CInt(Math.Min(400, Math.Round(ClockTaillePourcent * 480.0 / 128.0)))
        My.Settings.ClockTaillePourcent = ClockTaillePourcent
        My.Settings.ClockBIG = False
        My.Settings.Save()
    End Sub

    ''' <summary>Montre analogique en haut a droite de Form3, en plein ecran comme en mode fenetre,
    ''' des que l'option "Afficher Gadget Clock" (Form4, onglet Divers) est cochee, a la taille
    ''' choisie en %. Utilise la montre integree FormHorloge, reproduction du widget externe
    ''' TheAeroClock (skin 18) qui n'est plus lance.</summary>
    Sub AfficheMontre()

        ' Une ancienne instance du widget externe ne doit pas faire doublon.
        KillProcess("TheAeroClock")

        ' Contrairement a l'ancien code (qui sortait sans rien faire), decocher l'option masque
        ' bien la montre.
        If AfficheClock = False Then
            FormHorloge.Masquer()
            Exit Sub
        End If

        ' Taille changee a la souris sur la montre (molette / glisser) : on l'enregistre.
        ' RemoveHandler puis AddHandler : un seul abonnement, meme si AfficheMontre est rappelee.
        RemoveHandler FormHorloge.TailleChoisie, AddressOf MontreRedimensionneeALaSouris
        AddHandler FormHorloge.TailleChoisie, AddressOf MontreRedimensionneeALaSouris

        ' En plein ecran : widget independant au premier plan. En fenetre : rattachee a Form3.
        FormHorloge.AfficherSur(Form3, premierPlan:=FullScreen, pourcent:=ClockTaillePourcent)

    End Sub

    ''' <summary>Enregistre la taille choisie a la souris sur la montre (comme la glissiere de
    ''' Form4) et recale aussitot les horaires de Chabbat dans le nouveau cadran.</summary>
    Sub MontreRedimensionneeALaSouris(pourcent As Integer)
        ClockTaillePourcent = pourcent
        My.Settings.ClockTaillePourcent = pourcent
        My.Settings.Save()
        AfficheHeureDeChabbatDansCadran()
    End Sub

    Sub CacherSouris()

        Cursor.Position = New Point(10000, -1)
        'Cursor.Position = New Point(10000, 10000)

    End Sub

    Public Function ConnectionInternet() As Boolean
        If SansInternet = True Then Return False : Exit Function


        ''''''
        'Exit Function

        ''''''

        Dim objUrl As New System.Uri("http://www.google.fr")
        Dim objWebReq As System.Net.WebRequest
        objWebReq = System.Net.WebRequest.Create(objUrl)
        Dim objresp As System.Net.WebResponse

        Try
            objresp = objWebReq.GetResponse
            objresp.Close()
            objresp = Nothing
            Return True

        Catch ex As Exception
            objresp = Nothing
            objWebReq = Nothing
            Return False
        End Try
    End Function



    Sub frequenceTV()
        ChronoEtape("    frequenceTV : entree")
        Dim j As Integer

        For j = 0 To 50
            ChannelID(j, 0) = ""
            ChannelID(j, 1) = ""
        Next

        ' Liste des chaines de la TNT de la ville (fichier tnt-<ville>.m3u), pour VLC.
        ville = My.Settings.Shabbat_Ville

        Dim Nom As String = AppPath & "\Data\tnt-" & ville & ".m3u"

        If File.Exists(Nom) = False Then
            Exit Sub
        End If

        Dim Chanel(50) As String
        For j = 0 To 50
            Chanel(j) = ""
        Next

        Dim reader1 As New StreamReader(Nom)
        Dim line As String = Nothing
        i = 0 : k = 0
        While (reader1.Peek() <> -1)
            line = reader1.ReadLine()

            If Left(line, 10) = "#EXTINF:0," Then
                i = i + 1
                ChannelID(i, 1) = Trim(Mid(line, 11))
                k = k + 1
            End If

            If Left(line, 25) = "#EXTVLCOPT:dvb-frequency=" Then
                ChannelID(i, 0) = Mid(line, 26)
                k = k + 1
            End If

            If Left(line, 19) = "#EXTVLCOPT:program=" Then
                ChannelID(i, 0) = ChannelID(i, 0) & " :program=" & Mid(line, 20)
                k = k + 1
            End If
            If j = 3 Then
                Chanel(i) = "Chaine " + Format(i, "00") + " :" + ChannelID(i, 1)
                k = 0
            End If

        End While
        reader1.Close()

    End Sub




    Sub PlanB_onError()
        If ConfierAuFilPrincipal(Sub() PlanB_onError(), "PlanB") Then Exit Sub

        If PlanB = True Then
            AfficheTitre("Mise en place du PlanB ...", "*")
            wait(3)
            PlayTV_VLC(1, VolumeTVParDefault, "")

        End If


    End Sub

    Function Propre(Nom)
        Nom = Replace(Nom, "file:///", "")
        Nom = Replace(Nom, "file://", "\\")
        Nom = Replace(Nom, "%20", " ")

        Propre = Nom
    End Function

    ''' <summary>Le film du meme dossier dont VolumeEtArgumentAdapté a repris les reglages ("" : aucun).</summary>
    Public VoisinAdapté As String = ""

    Sub VolumeEtArgumentAdapté(fichier, ByRef VolumeAdapté, ByRef ArgAdapté)
        VoisinAdapté = ""
        Dim BonVolume As String = "100"

        Try

            rep = ""
            trouve = 0
            p = InStrRev(fichier, "\")
            If p > 0 Then
                For i = p - 1 To 1 Step -1
                    If Mid(fichier, i, 1) = "\" Then trouve = i : Exit For
                Next
                If trouve <> 0 Then
                    rep = Mid(fichier, trouve, p - trouve + 1)
                End If
            End If

            er = ""
            If rep <> "" Then
                trouve = 0
                For i = 0 To Form4.DataGridView1.Rows.Count - 1

                    Nom = Form4.DataGridView1.Rows(i).Cells("Action").Value
                    If Nom <> fichier Then
                        If InStr(Nom, rep) > 0 Then
                            trouve = trouve + 1
                            Volume = Form4.DataGridView1.Rows(i).Cells("Volume").Value
                            Arg2 = Form4.DataGridView1.Rows(i).Cells("Argument").Value
                            If trouve = 1 Then
                                BonVolume = Volume
                                ' Les reglages d'un autre film du meme dossier, SANS son titre (il se retrouvait dans
                                ' "Changer Titre" de l'apercu du nouveau film). Ils ne sont pas appliques d'office : le
                                ' film choisi les PROPOSE (Module2.ReprendreLesReglagesDuDossier).
                                ArgAdapté = ReglagesSansTitre(Arg2)
                                VoisinAdapté = Convert.ToString(Nom)
                            End If
                            If trouve > 1 And Volume <> BonVolume Then er = rep : Exit For

                        End If
                    End If
                Next


            End If

            If er <> "" Then MsgBox("Attention vous avez des volumes differents dans le meme repertoire " & rep)

        Catch ex As Exception

        End Try


        VolumeAdapté = BonVolume

    End Sub


    Function YaVraimentDesPhotosAffichable()
        result = False
        total = 0
        For i = 0 To NombreDePhoto - 1
            PhotoName = Form2.DataGridView1.Rows(i).Cells(3).Value
            result = True
            Exit For
        Next


        Return result
    End Function


    ' Logos de la fenetre Nflix : charges EN TACHE DE FOND et gardes (demande de Jacob, 13/10/2026 :
    ' "optimise la recherche dans nflix"). Avant, chaque ligne choisie (et chaque fin de recherche)
    ' attendait sur le fil de l'interface un test de l'adresse (jusqu'a 0,5 s) puis le telechargement.
    Private _logoDemande As String = ""
    Private ReadOnly _logosGardes As New Dictionary(Of String, Image)
    Private _clientLogos As System.Net.Http.HttpClient

    Sub AfficheLogo(ChannelLogo, RowIndex, Nom, Title)

        Dim fenetre As Form14 = Form14
        fenetre.VlcControl1.Visible = False
        fenetre.GroupBox2.Visible = False
        fenetre.PictureBox1.Visible = True

        fenetre.PictureBox1.Image = Nothing
        fenetre.PictureBox1.Tag = RowIndex
        fenetre.TextBox1.Text = Nom
        fenetre.TextBox2.Text = Replace(Title, ":", ";")

        Dim adresse As String = If(ChannelLogo Is Nothing, "", Convert.ToString(ChannelLogo)).Trim()
        _logoDemande = adresse
        If adresse = "" Then Exit Sub

        Dim garde As Image = Nothing
        If _logosGardes.TryGetValue(adresse, garde) Then
            fenetre.PictureBox1.Image = garde
            Exit Sub
        End If

        If _clientLogos Is Nothing Then
            _clientLogos = New System.Net.Http.HttpClient With {.Timeout = TimeSpan.FromSeconds(8)}
            _clientLogos.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Header)
        End If
        Dim client As System.Net.Http.HttpClient = _clientLogos
        System.Threading.Tasks.Task.Run(
            Sub()
                Try
                    Dim octets As Byte() = client.GetByteArrayAsync(adresse).GetAwaiter().GetResult()
                    If octets.Length = 0 OrElse fenetre.IsDisposed Then Exit Sub
                    fenetre.BeginInvoke(
                        Sub()
                            Try
                                If fenetre.IsDisposed OrElse _logoDemande <> adresse Then Exit Sub    ' une autre ligne a ete choisie depuis
                                Dim image As Image = Image.FromStream(New MemoryStream(octets))
                                If _logosGardes.Count > 60 Then _logosGardes.Clear()
                                _logosGardes(adresse) = image
                                fenetre.PictureBox1.Image = image
                            Catch ex As Exception
                            End Try
                        End Sub)
                Catch ex As Exception
                    ' pas de logo : l'image reste vide
                End Try
            End Sub)
    End Sub

    Function URLExists(ByVal url As String) As Boolean
        Dim FoundStatus As Boolean = False

        er = 0
        If String.IsNullOrEmpty(url) Then Return False
        If url.Equals("about:blank") Then Return False
        If Not url.StartsWith("http://") And
            Not url.StartsWith("https://") Then
            url = "http://" & url
        End If

        Dim request As HttpWebRequest
        Dim response As HttpWebResponse

        Try
            request = WebRequest.Create(url)
            request.Timeout = 500
            er = 1
        Catch ex As Exception

        End Try


        Try
            response = request.GetResponse()
        Catch ex As Exception

            If er = 1 Then request.Abort()
            If InStr(ex.Message, "403") Then
                Return True
            Else
                Return False
            End If
        End Try

        request.Abort()
        Return True
    End Function

    Sub playNFlix(url)
        Form14.GroupBox2.Visible = False

        'ext = "file:///" : If Left(UCase(nom), 4) = "HTTP" Then ext = ""

        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        vPlayer = Form14.VlcControl1
        'vPlayer.VlcMediaPlayer.SetUserAgent("User-Agent", Header)
        'vPlayer.SetMedia(nom, ":http-user-agent=" & Header)
        vPlayer.SetMedia(url)
        CouperLeSonAvantDemarrage(vPlayer)
        vPlayer.Play()
        Form14.Timer2.Start()
        SetVolume(Form14.TrackBar2.Value, vPlayer)          ' volume voulu, et son retabli





    End Sub


    ''' <summary>Case a cocher relue depuis les fichiers de reprise (SaveListPhoto/Video.txt). Le
    ''' presse-papiers de la grille y ecrit "Vrai"/"Faux" (langue de Windows), que .NET moderne ne
    ''' convertit plus en Booleen ("String 'Faux' was not recognized as a valid Boolean") : la
    ''' reprise echouait des la premiere ligne. On accepte les deux langues et 1/0.</summary>
    Function TexteEnBooleen(texte) As Boolean
        Select Case Trim(CStr(texte)).ToLowerInvariant()
            Case "vrai", "true", "1", "-1" : Return True
            Case Else : Return False
        End Select
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Reprise du diaporama : relecture des listes de photos et de videos enregistrees a la fermeture.
    '
    ' Acceleration du 13/10/2026 ("accelere aussi l'ouverture de Slide") : ces listes (54 000 photos,
    ' 6 400 videos) sont relues en TACHE DE FOND des le lancement, pendant que la fenetre des lecteurs
    ' se cree (0,6 s sur le fil de l'interface) ; quand la reprise en a besoin, elles sont pretes.
    ' Le code de lecture est le meme dans les deux cas (TableDeLaReprise).
    ' ------------------------------------------------------------------------------------------

    Private _reprisePreparee As System.Threading.Tasks.Task(Of DataTable())

    ''' <summary>Lit une liste enregistree (SaveListPhoto.txt, SaveListVideo.txt) dans une table :
    ''' case "deja vu", nom, date, chemin, et une 5e colonne - date Exif pour les photos, positions
    ''' restantes pour les videos (tout autre contenu de cette colonne est ignore).
    ''' Le fichier des photos doit exister (sinon erreur : la reprise est abandonnee, comme avant) ;
    ''' sans fichier des videos, la table est vide.</summary>
    Function TableDeLaReprise(fichier As String, photos As Boolean, culture As Globalization.CultureInfo,
                              Optional progres As Action(Of Integer) = Nothing, Optional pas As Integer = 5000) As DataTable
        Dim dt As New DataTable With {.Locale = culture}
        dt.Columns.Add("Check", GetType(System.Boolean))
        dt.Columns.Add("Name", GetType(String))
        dt.Columns.Add("DateTime", GetType(DateTime))
        dt.Columns.Add("Source", GetType(String))
        dt.Columns.Add(If(photos, "Exif", "Partial"), GetType(String))
        If Not photos AndAlso Not IO.File.Exists(fichier) Then Return dt
        ' Sources sans AUCUNE photo (videos seulement) : la liste des photos n'est pas ecrite (vide = fichier supprime).
        ' Journal de l'autre poste, 14/10/2026 : "Reprendre le Slide n'a pas pu etre active" a chaque lancement.
        If photos AndAlso Not IO.File.Exists(fichier) AndAlso IO.File.Exists(GridVideo) Then Return dt
        Dim i As Integer = 0
        For Each ligne As String In My.Computer.FileSystem.ReadAllText(fichier).Split(Environment.NewLine)
            Dim cases As String() = Split(ligne, vbTab)
            If cases.Length = 4 Then cases = Split(ligne & vbTab, vbTab)
            Dim cinquieme As String = If(If(photos, IsDate(cases(4)), EstUneListeDePositions(cases(4))), cases(4), "")
            dt.Rows.Add(TexteEnBooleen(cases(0)), cases(1), cases(2), cases(3), cinquieme)
            i += 1
            If progres IsNot Nothing AndAlso i Mod pas = 0 Then progres(i)
        Next
        Return dt
    End Function

    ''' <summary>Au lancement de Slide : commence a relire les listes en tache de fond (sans effet si
    ''' elles n'existent pas). La reprise s'en sert par TablePourLaReprise.</summary>
    Sub PreparerLaReprise()
        Try
            If _reprisePreparee IsNot Nothing OrElse Not IO.File.Exists(GridPhoto) Then Exit Sub
            Dim culture As Globalization.CultureInfo = Globalization.CultureInfo.CurrentCulture
            Dim fichierPhotos As String = GridPhoto, fichierVideos As String = GridVideo
            _reprisePreparee = System.Threading.Tasks.Task.Run(
                Function() As DataTable()
                    Dim ancienne As Globalization.CultureInfo = Globalization.CultureInfo.CurrentCulture
                    Try
                        Globalization.CultureInfo.CurrentCulture = culture      ' memes dates lues que sur le fil de l'interface
                        Dim chrono As Stopwatch = Stopwatch.StartNew()
                        Dim tables As DataTable() = {TableDeLaReprise(fichierPhotos, True, culture), TableDeLaReprise(fichierVideos, False, culture)}
                        DiagLog("Reprise : listes relues en tache de fond en " & chrono.ElapsedMilliseconds & " ms (" &
                                tables(0).Rows.Count & " photos, " & tables(1).Rows.Count & " videos)")
                        Return tables
                    Finally
                        Globalization.CultureInfo.CurrentCulture = ancienne
                    End Try
                End Function)
        Catch ex As Exception
            _reprisePreparee = Nothing
        End Try
    End Sub

    ''' <summary>La table des photos (0) ou des videos (1) pour la reprise : celle preparee en tache de
    ''' fond si elle existe (on attend sa fin au besoin ; une erreur de lecture ressort ici, comme si la
    ''' lecture etait faite sur place), sinon lue maintenant, avec le compteur de l'ecran de demarrage.</summary>
    Private Function TablePourLaReprise(rang As Integer) As DataTable
        Dim preparee As System.Threading.Tasks.Task(Of DataTable()) = _reprisePreparee
        If preparee IsNot Nothing Then
            Dim attente As Stopwatch = Stopwatch.StartNew()
            Dim tables As DataTable() = preparee.GetAwaiter().GetResult()
            If rang = 0 Then DiagLog("Reprise : listes preparees en tache de fond, attendues " & attente.ElapsedMilliseconds & " ms")
            Return tables(rang)
        End If
        Dim culture As Globalization.CultureInfo = Globalization.CultureInfo.CurrentCulture
        If rang = 0 Then
            Return TableDeLaReprise(GridPhoto, True, culture,
                                    Sub(n As Integer)
                                        Form1.Label1.Text = "Photos  " & Format(n)
                                        Application.DoEvents()
                                    End Sub, 5000)
        End If
        Return TableDeLaReprise(GridVideo, False, culture,
                                Sub(n As Integer)
                                    Form1.Label2.Text = "Videos  " & Format(n)
                                    Application.DoEvents()
                                End Sub, 500)
    End Function

    ''' <summary>Les tables preparees ne servent qu'une fois : la memoire est rendue.</summary>
    Sub OublierLaReprisePreparee()
        _reprisePreparee = Nothing
    End Sub

    ''' <summary>
    ''' Ecrit un fichier texte d'UN SEUL BLOC (meme contenu que File.WriteAllText : UTF-8 sans marque).
    ''' File.WriteAllText envoie le texte par morceaux de 4 Ko ; sur le lecteur reseau d'ou tourne
    ''' Slide, la liste des photos (5,5 Mo) demandait ainsi 1 400 envois : 280 ms, contre 60 ms d'un
    ''' bloc (mesure du 13/10/2026).
    ''' </summary>
    Sub EcrireDUnBloc(fichier As String, texte As String)
        IO.File.WriteAllBytes(fichier, New System.Text.UTF8Encoding(False).GetBytes(If(texte, "")))
    End Sub

    ''' <summary>
    ''' Le contenu d'un tableau en texte : une ligne par ligne du tableau, les cases separees par une
    ''' tabulation - le MEME texte que celui de "tout selectionner puis copier", dont se servait la
    ''' sauvegarde de la position du diaporama (SaveRestoreLeSlide) et que sa lecture attend.
    '''
    ''' Pourquoi (13/10/2026, "peux-tu accelerer la fermeture du programme") : selectionner les 54 000
    ''' photos puis demander leur copie fabriquait quatre versions du texte (simple, Unicode, CSV et
    ''' HTML) ; c'etait l'essentiel du temps de fermeture de Slide. Ici le texte est ecrit une fois.
    ''' Comme la copie : colonnes visibles dans l'ordre d'affichage, lignes visibles, case a cocher en
    ''' "Vrai" / "Faux", date sans les secondes (et sans heure a minuit).
    ''' </summary>
    Function TexteDuTableau(grille As DataGridView) As String
        If grille Is Nothing OrElse grille.RowCount = 0 Then Return ""
        Dim colonnes As DataGridViewColumn() = grille.Columns.Cast(Of DataGridViewColumn)().
                                               Where(Function(c) c.Visible).OrderBy(Function(c) c.DisplayIndex).ToArray()
        If colonnes.Length = 0 Then Return ""
        Dim cases As Boolean() = colonnes.Select(Function(c) TypeOf c Is DataGridViewCheckBoxColumn).ToArray()
        Dim dates As System.ComponentModel.TypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(GetType(Date))
        Dim culture As Globalization.CultureInfo = Globalization.CultureInfo.CurrentCulture
        Dim texte As New System.Text.StringBuilder(grille.RowCount * 120)
        Dim ecrire As Action(Of Integer, Object) =
            Sub(k As Integer, valeur As Object)
                If k > 0 Then texte.Append(vbTab)
                If cases(k) Then
                    texte.Append(If(TypeOf valeur Is Boolean, If(CBool(valeur), "Vrai", "Faux"), If(TexteEnBooleen(Convert.ToString(valeur)), "Vrai", "Faux")))
                ElseIf valeur Is Nothing OrElse valeur Is DBNull.Value Then
                    ' case vide
                ElseIf TypeOf valeur Is Date Then
                    texte.Append(dates.ConvertToString(Nothing, culture, valeur))
                ElseIf TypeOf valeur Is String Then
                    texte.Append(DirectCast(valeur, String))
                Else
                    texte.Append(Convert.ToString(valeur, culture))
                End If
            End Sub

        ' Tableau lie a une table de donnees (apres une reprise du diaporama) : les valeurs sont lues
        ' dans la table, dans l'ordre de sa vue - celui des lignes du tableau, tri compris. Bien plus
        ' rapide que de les demander case par case au tableau.
        Dim vue As DataView = Nothing
        If TypeOf grille.DataSource Is DataTable Then
            vue = DirectCast(grille.DataSource, DataTable).DefaultView
        ElseIf TypeOf grille.DataSource Is DataView Then
            vue = DirectCast(grille.DataSource, DataView)
        End If
        If vue IsNot Nothing AndAlso vue.Count = grille.RowCount AndAlso Not grille.AllowUserToAddRows AndAlso
           grille.Rows.GetRowCount(DataGridViewElementStates.Visible) = grille.RowCount Then
            Dim rangs As Integer() = colonnes.Select(Function(c) vue.Table.Columns.IndexOf(c.DataPropertyName)).ToArray()
            If Not rangs.Contains(-1) Then
                For i As Integer = 0 To vue.Count - 1
                    If i > 0 Then texte.Append(vbCrLf)
                    Dim ligne As DataRow = vue(i).Row
                    For k As Integer = 0 To rangs.Length - 1
                        ecrire(k, ligne(rangs(k)))
                    Next
                Next
                Return texte.ToString()
            End If
        End If

        Dim premiere As Boolean = True
        For i As Integer = 0 To grille.RowCount - 1
            If (grille.Rows.GetRowState(i) And DataGridViewElementStates.Visible) = 0 Then Continue For
            If grille.AllowUserToAddRows AndAlso i = grille.NewRowIndex Then Continue For
            If Not premiere Then texte.Append(vbCrLf)
            premiere = False
            For k As Integer = 0 To colonnes.Length - 1
                ecrire(k, grille(colonnes(k).Index, i).Value)
            Next
        Next
        Return texte.ToString()
    End Function

    Function SaveRestoreLeSlide(x)
        'x=1 save
        'x=2 restore
        result = True
        Application.DoEvents()
        Try

            Select Case x
                Case 1

                    ' Les listes sont ecrites directement (voir TexteDuTableau) : avant, "tout selectionner
                    ' puis copier" sur les 54 000 photos prenait 1 a 1,6 s a chaque fermeture de Slide.
                    Dim chronoListes As Stopwatch = Stopwatch.StartNew()
                    Dim textePhotos As String = TexteDuTableau(Form2.DataGridView1).TrimEnd()
                    If textePhotos <> "" Then
                        EcrireDUnBloc(GridPhoto, textePhotos)
                    Else
                        IO.File.Delete(GridPhoto)
                    End If
                    Form2.DataGridView1.ClearSelection()
                    Dim texteVideos As String = TexteDuTableau(Form2.DataGridView2).TrimEnd()
                    If texteVideos <> "" Then
                        EcrireDUnBloc(GridVideo, texteVideos)
                    Else
                        IO.File.Delete(GridVideo)
                    End If
                    Form2.DataGridView2.ClearSelection()
                    Dim texteMusique As New System.Text.StringBuilder()
                    For j = 0 To MusiqueVLC.MusiqueNombre() - 1
                        texteMusique.Append(MusiqueVLC.MusiqueFichier(j)).Append(Environment.NewLine)
                    Next
                    If texteMusique.Length > 0 Then
                        EcrireDUnBloc(GridMusic, texteMusique.ToString())
                    Else
                        IO.File.Delete(GridMusic)
                    End If
                    DiagLog("Position du diaporama enregistree en " & chronoListes.ElapsedMilliseconds & " ms (" & Form2.DataGridView1.RowCount & " photos, " & Form2.DataGridView2.RowCount & " videos)")
                    If ComputerName IsNot Nothing Then
                        EcrireDUnBloc(GridName, ComputerName.TrimEnd)
                    Else
                        IO.File.Delete(GridName)
                    End If

                Case 2

                    Form1.Copyright.Text = "Reprise du Slide..."
                    Application.DoEvents()

                    ComputerNameINIT = IO.File.ReadAllText(GridName)
                    If ComputerNameINIT <> ComputerName Then Throw New System.Exception("Erreur: Ordinateur different")

                    Form2.DataGridView1.ReadOnly = False
                    Form2.DataGridView2.ReadOnly = False
                    'Form2.DataGridView1.Sort = SortOrder.None

                    Form2.DataGridView1.Rows.Clear()

                    ' La liste est relue par TableDeLaReprise : en tache de fond depuis le lancement si
                    ' PreparerLaReprise est passe (elle est alors deja prete, ou presque), sinon ici meme.
                    Dim dt As DataTable = TablePourLaReprise(0)
                    i = dt.Rows.Count


                    Form2.DataGridView1.DataSource = dt
                    Form2.DataGridView1.Columns(0).Width = 30
                    Form2.DataGridView1.RowHeadersVisible = False

                    Form2.DataGridView1.RowHeadersVisible = False

                    If i > 0 Then
                        NombreDePhoto = i - 1
                    Else
                        NombreDePhoto = 0
                    End If

                    Form1.Label1.Text = "Photos  " & Format(NombreDePhoto)
                    Form2.Label1.Text = (NombreDePhoto).ToString + " Photos"
                    Form1.ProgressBar1.Value = 60
                    Application.DoEvents()
                    If dt.Rows.Count > 0 Then              ' (sans photo : pas de date, comme pour les videos)
                        DatePhotoMaximum = dt.Compute("MAX(DateTime)", "")
                        DatePhotoMinimum = dt.Compute("MIN(DateTime)", "")
                    End If


                    Form2.DataGridView2.Rows.Clear()
                    Dim dt2 As DataTable = TablePourLaReprise(1)
                    i = dt2.Rows.Count
                    OublierLaReprisePreparee()
                    Form2.DataGridView2.DataSource = dt2
                    Form2.DataGridView2.Columns(0).Width = 30
                    Form2.DataGridView1.ReadOnly = True
                    Form2.DataGridView2.ReadOnly = True
                    Form2.DataGridView2.RowHeadersVisible = False
                    Form2.DataGridView2.RowHeadersVisible = False

                    If i > 0 Then
                        NombreDeVideo = i - 1
                    Else
                        NombreDeVideo = 0
                    End If

                    Form1.Label2.Text = "Videos  " & Format(NombreDeVideo)
                    Form2.Label2.Text = (NombreDeVideo).ToString + " Videos"
                    Form1.ProgressBar1.Value = 90
                    Application.DoEvents()
                    If NombreDeVideo > 0 Then
                        DateVideoMaximum = dt2.Compute("MAX(DateTime)", "")
                        DateVideoMinimum = dt2.Compute("MIN(DateTime)", "")

                    End If


                    i = 0
                    MusiqueVLC.MusiqueVider()
                    If IO.File.Exists(GridMusic) Then
                        For Each Chanson In My.Computer.FileSystem.ReadAllText(GridMusic).Split(Environment.NewLine)
                            Try
                                i = i + 1
                                MusiqueVLC.MusiqueAjouter(Chanson)
                                If i Mod 100 = 0 Then Application.DoEvents()
                            Catch ex As Exception

                            End Try
                        Next

                        MusiqueVLC.MusiqueMelanger()
                        Form1.Label3.Text = "Musics  " & Format(i)
                        Form2.Label3.Text = (MusiqueVLC.MusiqueNombre()).ToString + " Musics"
                    End If

                    Form1.ProgressBar1.Value = 100
                    If NombreDePhoto = 0 And NombreDeVideo = 0 Then Throw New System.Exception("Erreur: Aucune photo ou video à afficher")

            End Select



        Catch ex As Exception
            ' MsgBox(ex.Message)
            DiagLog("Reprendre le Slide : echec (" & ex.GetType().Name & " : " & ex.Message & ")")
            Form1.Copyright.Text = "'Reprendre le Slide' n'a pas pu étre activé"
            result = False

        End Try

        NumPhoto = -1
        NumVideo = -1
        Return result
    End Function

    Sub LanceDeezer()
        Application.DoEvents()
        Dim startInfo As New ProcessStartInfo()
        Dim myprocess As New Process()
        'startInfo.FileName = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe "
        'startInfo.Arguments = "https://www.deezer.com/fr/" & " -new-window "
        startInfo.FileName = "deezer:\\ "
        'startInfo.CreateNoWindow = True
        ' Adresse "deezer:" : ouverte par Windows (en .NET moderne, UseShellExecute vaut False par
        ' defaut et le lancement echouait).
        startInfo.UseShellExecute = True
        myprocess.StartInfo = startInfo

        ' Processus Deezer deja la : on ne retiendra que ceux ouverts par ce lancement.
        Dim deezerAvant As New HashSet(Of Integer)
        For Each p As Process In Process.GetProcessesByName("Deezer")
            deezerAvant.Add(p.Id) : p.Dispose()
        Next

        Try
            myprocess.Start()
        Catch ex As Exception
            SaveLogError("Deezer", "lancement impossible (" & ex.Message & ")")
            Exit Sub
        End Try
        wait(1)

        j = 0
        Do Until WindowExist("Deezer")
            wait(1)
            j = j + 1
            If j > 20 Then Exit Do
        Loop

        ' Deezer lance par Slide : lui seul sera ferme par ClosePlayer / Music_SWITCH_ON.
        For Each p As Process In Process.GetProcessesByName("Deezer")
            If Not deezerAvant.Contains(p.Id) Then RetenirLecteur(p.Id)
            p.Dispose()
        Next


        wait(16)


        Dim rrect As RECT

        Dim Foc = FocusWindow("Deezer", Nothing)
        rrect = Foc.W_RECT

        waitExclusif(2)
        Application.DoEvents()
        Cursor.Position = New Point(rrect.Left + 500, rrect.Top + 100)
        mouse_event(&H2, 0, 0, 0, 0)
        mouse_event(&H4, 0, 0, 0, 0)


        My.Computer.Keyboard.SendKeys(" ")
        waitExclusif(2)
        UnFocusWindow("Deezer", Nothing)
        Foc = FocusWindow("Slide", Nothing)


        'wait(1)
        'ApplicationHandle = myprocess.MainWindowHandle
        'MoveWindow(ApplicationHandle, ScreenSizeW - 20, 100, 1000, 1000, True) 'left,top,largeur,hauteur

        'wait(3)
        'My.Computer.Keyboard.SendKeys("+{TAB 12}")
        'wait(1)
        'My.Computer.Keyboard.SendKeys("{ENTER}")


        'My.Computer.Keyboard.SendKeys(" ")
        'wait(1)


        'My.Computer.Keyboard.SendKeys("% ")
        'wait(1)
        'My.Computer.Keyboard.SendKeys("u")
        'wait(1)
    End Sub


    ''' <summary>Affiche un message quelques secondes par-dessus le diaporama (carte sombre, voir
    ''' FormMessage), au centre de l'image de Slide, puis le retire.</summary>
    Sub MessageEcran(Mess As Object, s As Object, Optional titre As String = "Slide")
        Try
            FormMessage.Preparer(titre, CStr(Mess), CDbl(Val(s)), FenetrePrincipale())
            FormMessage.Show()
            wait(s)
            FormMessage.Close()
        Catch ex As Exception
            DiagLog("Message a l'ecran : " & ex.Message)
        End Try


    End Sub


End Module





