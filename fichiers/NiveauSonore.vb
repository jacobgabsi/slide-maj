Option Strict On
Option Explicit On

''' <summary>
''' Lissage du son entre les musiques et les videos du diaporama (option "Lisser le son", Form4,
''' onglet Audio).
'''
''' Les fichiers n'ont pas tous le meme niveau : mesure au banc, jusqu'a 14 dB d'ecart entre un
''' morceau et une video. Les filtres de VLC (normvol, compresseur) sont ignores par la
''' bibliothèque utilisee (seul l'egaliseur passe, verifie dans le journal de VLC) : la correction
''' est donc faite ici, sur le volume des lecteurs, a partir du niveau REELLEMENT produit par Slide
''' (le vumetre de sa session audio Windows, celui du melangeur de volume).
'''
''' Pour chaque musique ou video qui commence :
'''   - 0 a 2,5 s : on ecoute ; puis correction douce (au plus PlafondDb, dans un sens ou l'autre) ;
'''   - jusqu'a 15 s : on continue d'ecouter et on affine une fois ; ensuite plus rien ne bouge
'''     (pas de "pompage" pendant le morceau).
''' Le niveau de chaque fichier est memorise (data\NiveauxSonores.txt) : la fois suivante, il part
''' directement au bon volume. La reference est la mediane des niveaux deja mesures : la
''' correction egalise les fichiers entre eux sans changer le volume general choisi.
'''
''' Le niveau mesure contient le volume du lecteur : on le retire (volume de base du reglage et
''' correction en cours) pour obtenir le niveau du FICHIER lui-meme. Musique : l'echelle de Slide
''' est lineaire (MusiqueVLC passe la racine cubique a VLC). Videos : volume VLC, applique au cube.
''' </summary>
Public Module NiveauSonore

    ''' <summary>Correction maximale, dans un sens ou dans l'autre.</summary>
    Private Const PlafondDb As Double = 10
    ''' <summary>En dessous, silence : l'echantillon n'est pas compte.</summary>
    Private Const SeuilSilence As Double = 0.003          ' -50 dB
    Private Const DureeEcouteMs As Integer = 2500
    Private Const DureeAffinageMs As Integer = 15000
    Private Const EchantillonsMini As Integer = 20
    ''' <summary>En dessous de ce gain du lecteur (-20 dB), on ne mesure pas : trop imprecis.
    ''' Mesure du 01/10 : musique a 3 % (gain 0,03) mesuree a +16 / +26 dB, impossible.</summary>
    Private Const GainMinimalMesurable As Double = 0.1

    Private WithEvents Minuteur As New Timer With {.Interval = 50}

    Private _derniereRechercheSession As Date = Date.MinValue

    ' Source en cours : "M|mrl" (musique) ou "V|mrl" (video du diaporama), "" si aucune.
    Private _source As String = ""
    Private _chrono As New Stopwatch()
    Private _phase As Integer                              ' 0 ecoute, 1 affinage, 2 fixe
    Private _somme As Double                               ' somme des carres (niveau du fichier)
    Private _nombre As Integer
    Private _correction As Double                          ' dB appliques en ce moment
    Private _cible As Double                               ' dB vises
    Private _tours As Integer                              ' ticks du minuteur (verification du volume video)
    Private _pasDb As Double = 0.5                         ' vitesse de la rampe (dB par tick)

    Private _niveaux As Dictionary(Of String, Double)      ' fichier -> niveau propre (dB)

    ''' <summary>A appeler une fois, au demarrage : lance l'ecoute (20 fois par seconde).</summary>
    Public Sub Demarrer()
        Minuteur.Start()
    End Sub

    Private Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles Minuteur.Tick
        Try
            Suivre()
        Catch ex As Exception
            DiagLog("Lissage du son : " & ex.Message)
            _derniereRechercheSession = Date.MinValue      ' la liste des sessions sera relue
        End Try
    End Sub

    Private Sub Suivre()
        If Not LisserLeSon Then
            ' Option decochee : plus aucune correction (la video reprend son volume a la suivante).
            If _source <> "" OrElse _correction <> 0 Then
                MusiqueVLC.MusiqueDefinirCorrection(0)
                _source = "" : _correction = 0 : _cible = 0
            End If
            Exit Sub
        End If
        Dim cle As String = SourceEnCours()

        If cle <> _source Then
            ' Changement de musique ou de video : on repart de zero pour la nouvelle source.
            TerminerSource()
            _source = cle
            _chrono.Restart()
            _phase = 0 : _somme = 0 : _nombre = 0
            If cle = "" Then Exit Sub
            ' Video dont la sonie a ete mesuree a l'avance : correction d'apres le volume RESSENTI
            ' de l'extrait joue, fixee pour tout l'extrait (pas de mesure pendant la lecture).
            If cle.StartsWith("V|") Then
                Dim sonie As Double? = SonieDeLExtrait(Fichier(cle))
                If sonie.HasValue Then
                    Dim reference As Double = ReferenceSonie()
                    _correction = CorrectionSonie(sonie.Value, reference)
                    _cible = _correction
                    _phase = 2
                    Appliquer(cle, _correction)
                    DiagLog("Lissage du son (sonie) : " & IO.Path.GetFileName(Fichier(cle)) & " extrait a " & CInt(ExtraitDebut) &
                            " s : " & sonie.Value.ToString("0.0") & " LUFS, reference " & reference.ToString("0.0") &
                            " -> correction " & _correction.ToString("+0.0;-0.0;0") & " dB")
                    Exit Sub
                End If
            End If
            ' Deja mesure : il part tout de suite au bon volume, puis on affine quand meme.
            Dim niveau As Double
            If Niveaux().TryGetValue(Fichier(cle), niveau) Then
                _correction = CorrectionPour(cle, niveau) : _cible = _correction
                _phase = 1
            Else
                _correction = 0 : _cible = 0
            End If
            Appliquer(cle, _correction)
            Exit Sub
        End If
        If cle = "" Then Exit Sub

        ' Rampe douce vers la correction visee.
        If Math.Abs(_cible - _correction) > 0.01 Then
            _correction += Math.Sign(_cible - _correction) * Math.Min(_pasDb, Math.Abs(_cible - _correction))
            Appliquer(cle, _correction)
        End If
        ' Video : deux fois par seconde, on verifie que le lecteur est bien au volume corrige.
        ' Le diaporama regle le volume de base juste apres le demarrage (SetVideoVolSlide) et
        ' ecrasait ainsi la correction d'une video deja mesuree, qui n'etait plus jamais remise.
        _tours += 1
        ' (Les 4 premieres secondes : a chaque tick, sinon la video jouait jusqu'a une seconde au
        '  volume non corrige - un debut deux fois trop fort pour une video forte.)
        If cle.StartsWith("V|") AndAlso (_tours Mod 10 = 0 OrElse _chrono.ElapsedMilliseconds < 4000) Then ReaffirmerLeVolumeVideo()
        If _phase = 2 Then Exit Sub

        ' Mesure : niveau produit, ramene au niveau du fichier lui-meme.
        Dim crete As Double = CreteDeSlide()
        ' La premiere demi-seconde, VLC n'a pas encore pris le volume voulu (il tient le silence
        ' puis l'applique, voir ReglerLeSonApresDemarrage) : on ne compte pas ces echantillons.
        ' Pas de mesure fiable (le vumetre est celui de TOUT Slide) :
        '  - lecteur trop bas : le niveau du fichier = crete / gain, une petite part de bruit ou de
        '    musique devient enorme (videos mesurees a +20 dB, impossible) ;
        '  - video pendant que la musique joue aussi : on mesurerait la musique.
        Dim mesurable As Boolean = GainDuLecteur(cle) >= GainMinimalMesurable AndAlso
                                   Not (cle.StartsWith("V|") AndAlso MusiqueVLC.MusiqueEnCours())
        If mesurable AndAlso crete > SeuilSilence AndAlso _chrono.ElapsedMilliseconds >= 500 Then
            Dim gain As Double = GainDuLecteur(cle) * Math.Pow(10, _correction / 20)
            If gain > 0.0000001 Then
                Dim a As Double = crete / gain
                _somme += a * a : _nombre += 1
            End If
        End If

        Dim ecoule As Long = _chrono.ElapsedMilliseconds
        If _phase = 0 AndAlso ecoule >= DureeEcouteMs AndAlso _nombre >= EchantillonsMini Then
            Conclure(cle, 0.5)                             ' ~1 s de rampe
            _phase = 1
        ElseIf _phase = 1 AndAlso ecoule >= DureeAffinageMs AndAlso _nombre >= EchantillonsMini Then
            Conclure(cle, 0.25)                            ' ~2 s de rampe, plus discret
            _phase = 2
        End If
    End Sub

    ''' <summary>Niveau du fichier mesure jusqu'ici : memorise, et correction visee.</summary>
    Private Sub Conclure(cle As String, pasDb As Double)
        Dim niveau As Double = 10 * Math.Log10(_somme / _nombre)
        If niveau > NiveauMaxPlausible Then
            ' Mesure fausse (Memoriser la rejette aussi) : on ne corrige rien sur sa base. Avant, la
            ' correction de -10 dB etait quand meme appliquee : musique baissee pour rien.
            Memoriser(Fichier(cle), niveau)
            Exit Sub
        End If
        Memoriser(Fichier(cle), niveau)
        _cible = CorrectionPour(cle, niveau)
        _pasDb = pasDb
        DiagLog("Lissage du son : " & IO.Path.GetFileName(Uri.UnescapeDataString(Fichier(cle))) &
                " niveau " & niveau.ToString("0.0") & " dB, reference " & Reference(cle, niveau).ToString("0.0") &
                " dB -> correction " & _cible.ToString("+0.0;-0.0;0") & " dB")
    End Sub

    Private Sub TerminerSource()
        ' Musique arretee au milieu de la mesure : on garde ce qu'on a, si c'est assez.
        If _source <> "" AndAlso _phase < 2 AndAlso _nombre >= EchantillonsMini * 3 Then
            Memoriser(Fichier(_source), 10 * Math.Log10(_somme / _nombre))
        End If
    End Sub

    ''' <summary>La correction pour amener un fichier de ce niveau a la reference, plafonnee.</summary>
    Private Function CorrectionPour(cle As String, niveau As Double) As Double
        Return Math.Max(-PlafondDb, Math.Min(PlafondDb, Reference(cle, niveau) - niveau))
    End Function

    ''' <summary>Le "niveau normal" des fichiers du MEME genre (musiques entre elles, videos entre
    ''' elles) : la mediane de leurs niveaux mesures. Avant, musiques et videos partageaient une
    ''' seule mediane : avec 312 videos (dont des mesures fausses a +20 dB) pour 30 musiques, la
    ''' reference etait a +18,6 dB et TOUTES les musiques etaient montees au maximum (+10 dB).
    ''' Moins de 5 fichiers du genre : le premier mesure sert de reference ; aucun : pas de
    ''' correction.</summary>
    Private Function Reference(cle As String, niveauCourant As Double) As Double
        Dim musique As Boolean = EstMusique(Fichier(cle))
        Dim memes As List(Of Double) = Niveaux().Where(Function(kv) EstMusique(kv.Key) = musique).Select(Function(kv) kv.Value).ToList()
        If memes.Count = 0 Then Return niveauCourant
        If memes.Count < 5 Then Return memes(0)
        memes.Sort()
        Return memes(memes.Count \ 2)
    End Function

    ''' <summary>Un niveau mesure au-dessus de 0 dB est impossible (le maximum numerique) : la
    ''' mesure etait fausse (video au volume presque nul pendant que la musique jouait).</summary>
    Private Const NiveauMaxPlausible As Double = 0

    Private ReadOnly _formatsAudio As String() = {".mp3", ".m4a", ".flac", ".wma", ".wav", ".ogg", ".aac", ".opus"}

    Private Function EstMusique(mrl As String) As Boolean
        Dim ext As String = ""
        Try
            ext = IO.Path.GetExtension(Uri.UnescapeDataString(mrl)).ToLowerInvariant()
        Catch ex As Exception
        End Try
        Return _formatsAudio.Contains(ext)
    End Function


    ' ------------------------------------------------------------------------------------------
    ' Qui joue, et a quel volume
    ' ------------------------------------------------------------------------------------------

    ''' <summary>La source qu'on entend : la video du diaporama si elle a du son, sinon la
    ''' musique ; rien pendant un PIP avec le son, un film, la TV (leur son n'est pas concerne).</summary>
    Private Function SourceEnCours() As String
        If PIPvolume > 0 Then Return ""
        Try
            If Diffusion.Mode = "VIDEO" AndAlso Form3.VlcControl1.IsPlaying AndAlso Not VideoDuDiaporamaMuette() Then
                Dim media As Vlc.DotNet.Core.VlcMedia = Form3.VlcControl1.GetCurrentMedia()
                If media IsNot Nothing Then Return "V|" & media.Mrl
            End If
        Catch ex As Exception
        End Try
        Dim mrl As String = MusiqueVLC.MusiqueMrlEnCours()
        Return If(mrl = "", "", "M|" & mrl)
    End Function

    Private Function Fichier(cle As String) As String
        Return CleNormalisee(cle.Substring(2))
    End Function

    ''' <summary>Cle d'un fichier dans la memoire des niveaux : chemin Windows en minuscules, quelle
    ''' que soit son ecriture (adresse VLC "file:///D:/a%20b.mp4" ou chemin "D:\a b.mp4"). Ainsi
    ''' une video mesuree a l'avance (MesureSonVideos) est reconnue quand VLC la joue.</summary>
    Public Function CleNormalisee(mrlOuChemin As String) As String
        Dim chemin As String = If(mrlOuChemin, "")
        Try
            If chemin.StartsWith("file:", StringComparison.OrdinalIgnoreCase) Then chemin = New Uri(chemin).LocalPath
        Catch ex As Exception
            chemin = Uri.UnescapeDataString(chemin)
        End Try
        If Not chemin.Contains("://") Then chemin = chemin.Replace("/"c, "\"c)
        Return chemin.ToLowerInvariant()
    End Function

    ''' <summary>Fichiers dont le niveau est deja connu (cles normalisees). Thread de l'interface.</summary>
    Public Function NiveauxConnus() As HashSet(Of String)
        Return New HashSet(Of String)(Niveaux().Keys, StringComparer.OrdinalIgnoreCase)
    End Function

    ''' <summary>Niveau d'une video mesure a l'avance par ffmpeg (meme mesure que pendant la
    ''' lecture). Thread de l'interface.</summary>
    Public Sub MemoriserMesureAvance(chemin As String, niveau As Double)
        Memoriser(CleNormalisee(chemin), niveau)
    End Sub

    ''' <summary>Gain (amplitude, 0 a 1) du lecteur, SANS la correction : volume du reglage.</summary>
    Private Function GainDuLecteur(cle As String) As Double
        If cle.StartsWith("M|") Then Return MusiqueVLC.MusiqueVolumeDeBase() / 100.0
        Return Math.Pow(VolumeVlcVideoDiaporama() / 100.0, 3)
    End Function

    Private Sub Appliquer(cle As String, db As Double)
        If cle.StartsWith("M|") Then
            MusiqueVLC.MusiqueDefinirCorrection(db)
        Else
            Try
                ' Volume VLC au cube : une correction de db decibels = volume x 10^(db/60).
                Dim v As Integer = CInt(Math.Round(VolumeVlcVideoDiaporama() * Math.Pow(10, db / 60)))
                If Form3.VlcControl1.Audio IsNot Nothing Then Form3.VlcControl1.Audio.Volume = Math.Max(0, Math.Min(VolumeVLCMaxi, v))
            Catch ex As Exception
            End Try
        End If
    End Sub

    ''' <summary>Crete actuelle du son produit par Slide (vumetre de ses sessions audio Windows,
    ''' lu par VumetreSession : sans CoreAudio.dll, qui fuyait et pouvait fermer le programme).
    ''' Mesure au banc : apres un changement de fichier, Windows peut ouvrir une NOUVELLE session
    ''' pour le programme ; la liste est donc relue toutes les 2 s, et on garde la plus forte
    ''' (musique et video ont chacune leur lecteur).</summary>
    Private Function CreteDeSlide() As Double
        If (Date.Now - _derniereRechercheSession).TotalSeconds >= 2 Then
            _derniereRechercheSession = Date.Now
            VumetreSession.Rafraichir()
        End If
        Return VumetreSession.Crete()
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Niveaux memorises : data\NiveauxSonores.txt, "niveau<TAB>fichier"
    ' ------------------------------------------------------------------------------------------

    Private Function FichierNiveaux() As String
        Return IO.Path.Combine(DossierReglages, "NiveauxSonores.txt")
    End Function

    Private Function Niveaux() As Dictionary(Of String, Double)
        If _niveaux IsNot Nothing Then Return _niveaux
        _niveaux = New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        Try
            If IO.File.Exists(FichierNiveaux()) Then
                For Each ligne As String In IO.File.ReadAllLines(FichierNiveaux(), Text.Encoding.UTF8)
                    Dim p As Integer = ligne.IndexOf(ControlChars.Tab)
                    Dim v As Double
                    If p > 0 AndAlso Double.TryParse(ligne.Substring(0, p), Globalization.NumberStyles.Float,
                                                     Globalization.CultureInfo.InvariantCulture, v) Then
                        If v <= NiveauMaxPlausible Then _niveaux(CleNormalisee(ligne.Substring(p + 1))) = v
                    End If
                Next
            End If
        Catch ex As Exception
            DiagLog("Lissage du son : niveaux illisibles (" & ex.Message & ")")
        End Try
        Return _niveaux
    End Function

    ''' <summary>Oublie les niveaux memorises des MUSIQUES (pas des videos) : apres une
    ''' normalisation des fichiers (bouton "Volume Normalizer"), ils ne valent plus rien et le
    ''' lissage appliquerait une correction fausse des la premiere seconde. Renvoie le nombre oublie.</summary>
    Public Function OublierLesNiveauxDesMusiques() As Integer
        Dim a_oublier As List(Of String) = Niveaux().Keys.Where(AddressOf EstMusique).ToList()
        For Each cle As String In a_oublier
            Niveaux().Remove(cle)
        Next
        Try
            Dim lignes As IEnumerable(Of String) = Niveaux().Select(Function(kv) kv.Value.ToString("0.00", Globalization.CultureInfo.InvariantCulture) & ControlChars.Tab & kv.Key)
            IO.File.WriteAllLines(FichierNiveaux(), lignes, New Text.UTF8Encoding(False))
        Catch ex As Exception
            DiagLog("Lissage du son : niveaux non reecrits (" & ex.Message & ")")
        End Try
        _source = ""                                       ' la musique en cours sera remesuree
        Return a_oublier.Count
    End Function

    ''' <summary>
    ''' Verifie que la video du diaporama joue bien au volume voulu (reglage + correction du
    ''' lissage) et le remet si besoin. Appele plusieurs fois juste apres le passage a une video
    ''' PRECHARGEE : VLC ne cree sa sortie son qu'a la reprise de la lecture et repart alors a son
    ''' propre volume (100 %), apres le reglage fait par AfficheVideo. Thread de l'interface.
    ''' </summary>
    Private _remisesNotees As Integer

    Public Sub ReaffirmerLeVolumeVideo()
        Try
            If PIPvolume > 0 OrElse CBool(VolumeOFF) OrElse Not CBool(VideoWithAudio) OrElse VideoDuDiaporamaMuette() Then Exit Sub
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcControl1
            If lecteur.Audio Is Nothing Then Exit Sub
            Dim cible As Integer = VolumeVlcVideoDiaporama()
            If LisserLeSon AndAlso _source.StartsWith("V|") Then cible = CInt(Math.Round(cible * Math.Pow(10, _correction / 60)))
            cible = Math.Max(0, Math.Min(CInt(VolumeVLCMaxi), cible))
            Dim lu As Integer = lecteur.Audio.Volume
            If lu >= 0 AndAlso Math.Abs(lu - cible) > 1 Then
                If _remisesNotees < 30 Then _remisesNotees += 1 : DiagLog("Son : volume de la video remis a " & cible & " (VLC etait a " & lu & ")")
                lecteur.Audio.Volume = cible
            End If
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Sonie des videos (volume ressenti, en LUFS), mesuree a l'avance par MesureSonVideos a
    ' plusieurs positions de chaque fichier : data\SonieDesVideos.txt, une ligne par video,
    ' "chemin <tab> position:LUFS;position:LUFS;...". Le lissage d'une video prend la sonie de
    ' l'extrait joue (AfficheVideo donne sa position) et l'amene a la sonie mediane des videos.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Debut (s) et duree (s) de l'extrait de video que le diaporama va jouer.</summary>
    Public ExtraitDebut As Double
    Public ExtraitDuree As Double

    ''' <summary>Une video trop forte peut etre baissee davantage qu'on ne monte une video faible
    ''' (baisser ne sature jamais).</summary>
    Private Const PlafondBaisseSonieDb As Double = 18

    ''' <summary>Correction (dB) pour amener un extrait de cette sonie a la reference.
    ''' Baisse : jusqu'a 18 dB. Hausse : 10 dB au plus, et de moins en moins pour un extrait tres
    ''' faible (sous -30 LUFS : ambiance, vent, pas de paroles ; 4 dB a -42) - le monter davantage
    ''' ne ferait ressortir que le bruit de fond (02/10 : des rushes a -40 LUFS montes de 10 dB).</summary>
    Private Function CorrectionSonie(sonieExtrait As Double, reference As Double) As Double
        Dim db As Double = reference - sonieExtrait
        If db <= 0 Then Return Math.Max(-PlafondBaisseSonieDb, db)
        ' Hausse : 10 dB jusqu'a -30 LUFS, puis de moins en moins (4 dB a -42 LUFS et en dessous).
        Dim plafondHausse As Double = If(sonieExtrait >= -30, 10.0, Math.Max(4.0, 10.0 - (-30 - sonieExtrait) * 0.5))
        Return Math.Min(db, plafondHausse)
    End Function

    Private _sonie As Dictionary(Of String, List(Of KeyValuePair(Of Double, Double)))
    Private _referenceSonie As Double?

    Private Function FichierSonie() As String
        Return IO.Path.Combine(DossierReglages, "SonieDesVideos.txt")
    End Function

    Private Function Sonie() As Dictionary(Of String, List(Of KeyValuePair(Of Double, Double)))
        If _sonie IsNot Nothing Then Return _sonie
        _sonie = New Dictionary(Of String, List(Of KeyValuePair(Of Double, Double)))(StringComparer.OrdinalIgnoreCase)
        Try
            If IO.File.Exists(FichierSonie()) Then
                Dim inv As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
                For Each ligne As String In IO.File.ReadAllLines(FichierSonie(), Text.Encoding.UTF8)
                    Dim t As Integer = ligne.IndexOf(ControlChars.Tab)
                    If t <= 0 Then Continue For
                    Dim mesures As New List(Of KeyValuePair(Of Double, Double))
                    For Each m As String In ligne.Substring(t + 1).Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                        Dim d As Integer = m.IndexOf(":"c)
                        Dim position, lufs As Double
                        If d > 0 AndAlso Double.TryParse(m.Substring(0, d), Globalization.NumberStyles.Float, inv, position) AndAlso
                           Double.TryParse(m.Substring(d + 1), Globalization.NumberStyles.Float, inv, lufs) Then
                            mesures.Add(New KeyValuePair(Of Double, Double)(position, lufs))
                        End If
                    Next
                    If mesures.Count > 0 Then _sonie(ligne.Substring(0, t)) = mesures
                Next
            End If
        Catch ex As Exception
            DiagLog("Lissage du son : sonie illisible (" & ex.Message & ")")
        End Try
        Return _sonie
    End Function

    ''' <summary>Videos dont la sonie est deja mesuree (cles normalisees). Thread de l'interface.</summary>
    Public Function SonieConnue() As HashSet(Of String)
        Return New HashSet(Of String)(Sonie().Keys, StringComparer.OrdinalIgnoreCase)
    End Function

    ''' <summary>Enregistre la sonie d'une video (positions en s, LUFS). Thread de l'interface.</summary>
    Public Sub MemoriserSonie(chemin As String, mesures As List(Of KeyValuePair(Of Double, Double)))
        If mesures Is Nothing OrElse mesures.Count = 0 Then Exit Sub
        Dim cle As String = CleNormalisee(chemin)
        Sonie()(cle) = New List(Of KeyValuePair(Of Double, Double))(mesures)
        _referenceSonie = Nothing
        Try
            Dim inv As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
            IO.File.AppendAllText(FichierSonie(),
                cle & ControlChars.Tab & String.Join(";", mesures.Select(Function(m) m.Key.ToString("0", inv) & ":" & m.Value.ToString("0.0", inv))) & vbCrLf,
                New Text.UTF8Encoding(False))
        Catch
            ' non memorisee : elle sera remesuree au prochain demarrage
        End Try
    End Sub

    ''' <summary>Moyenne de plusieurs sonies (moyenne des energies, comme le fait la norme).</summary>
    Private Function MoyenneLufs(valeurs As IEnumerable(Of Double)) As Double
        Dim liste As List(Of Double) = valeurs.ToList()
        Return 10 * Math.Log10(liste.Average(Function(v) Math.Pow(10, v / 10)))
    End Function

    ''' <summary>Sonie de l'extrait qui va etre joue : moyenne des mesures tombant dans l'extrait,
    ''' a defaut la mesure la plus proche. Nothing si la video n'a pas ete mesuree.</summary>
    Private Function SonieDeLExtrait(fichier As String) As Double?
        Dim mesures As List(Of KeyValuePair(Of Double, Double)) = Nothing
        If Not Sonie().TryGetValue(fichier, mesures) OrElse mesures.Count = 0 Then Return Nothing
        Dim debut As Double = ExtraitDebut
        Dim fin As Double = debut + Math.Max(1, ExtraitDuree)
        ' Une mesure couvre 20 s (40 s pour la premiere) a partir de sa position.
        Dim dedans As List(Of Double) = mesures.Where(Function(m) m.Key + 20 > debut AndAlso m.Key < fin).Select(Function(m) m.Value).ToList()
        If dedans.Count > 0 Then Return MoyenneLufs(dedans)
        Return mesures.OrderBy(Function(m) Math.Abs(m.Key - debut)).First().Value
    End Function

    ''' <summary>Volume VLC a donner a une video qui va demarrer a <paramref name="debut"/> s :
    ''' volume du diaporama, corrige d'apres sa sonie si elle est connue et le lissage actif. Sert
    ''' a la video lancee en avance (PrechargementVideo), pour qu'elle parte au bon volume.</summary>
    Public Function VolumeVlcPourLaVideo(chemin As String, debut As Double, duree As Double) As Integer
        Dim volume As Integer = VolumeVlcVideoDiaporama()
        Try
            If Not LisserLeSon Then Return volume
            Dim mesures As List(Of KeyValuePair(Of Double, Double)) = Nothing
            If Not Sonie().TryGetValue(CleNormalisee(chemin), mesures) OrElse mesures.Count = 0 Then Return volume
            Dim fin As Double = debut + Math.Max(1, duree)
            Dim dedans As List(Of Double) = mesures.Where(Function(m) m.Key + 20 > debut AndAlso m.Key < fin).Select(Function(m) m.Value).ToList()
            Dim sonieExtrait As Double = If(dedans.Count > 0, MoyenneLufs(dedans), mesures(0).Value)
            Dim db As Double = CorrectionSonie(sonieExtrait, ReferenceSonie())
            Return Math.Max(0, Math.Min(CInt(VolumeVLCMaxi), CInt(Math.Round(volume * Math.Pow(10, db / 60)))))
        Catch ex As Exception
            Return volume
        End Try
    End Function

    ''' <summary>Sonie "normale" des videos : la mediane des sonies moyennes de chaque video.</summary>
    Private Function ReferenceSonie() As Double
        If _referenceSonie.HasValue Then Return _referenceSonie.Value
        Dim moyennes As List(Of Double) = Sonie().Values.Select(Function(m) MoyenneLufs(m.Select(Function(x) x.Value))).ToList()
        If moyennes.Count = 0 Then Return -23
        moyennes.Sort()
        ' 3 dB SOUS la mediane : les videos faibles peuvent ainsi rejoindre le niveau commun sans
        ' hausse excessive (avant : les fortes ramenees a la mediane, les faibles montees de 6 dB au
        ' plus, restaient jusqu'a deux fois moins fortes que les autres).
        _referenceSonie = moyennes(moyennes.Count \ 2) - 3
        Return _referenceSonie.Value
    End Function

    ''' <summary>Remise a zero du lissage (bouton "RAZ" de Form4, onglet Audio) : oublie les
    ''' niveaux memorises (tous, ou seulement ceux des videos), apres une sauvegarde du fichier
    ''' (data\NiveauxSonores.sauvegarde.txt). Chaque fichier sera remesure a sa prochaine lecture,
    ''' en quelques secondes. Renvoie le nombre de niveaux oublies.</summary>
    Public Function RemettreAZero(Optional videosSeulement As Boolean = False) As Integer
        Try
            If IO.File.Exists(FichierNiveaux()) Then
                IO.File.Copy(FichierNiveaux(), IO.Path.ChangeExtension(FichierNiveaux(), ".sauvegarde.txt"), True)
            End If
        Catch ex As Exception
            DiagLog("Lissage du son : sauvegarde impossible (" & ex.Message & ")")
        End Try
        Dim a_oublier As List(Of String) = Niveaux().Keys.Where(Function(k) Not videosSeulement OrElse Not EstMusique(k)).ToList()
        For Each cle As String In a_oublier
            Niveaux().Remove(cle)
        Next
        Try
            Dim lignes As IEnumerable(Of String) = Niveaux().Select(Function(kv) kv.Value.ToString("0.00", Globalization.CultureInfo.InvariantCulture) & ControlChars.Tab & kv.Key)
            IO.File.WriteAllLines(FichierNiveaux(), lignes, New Text.UTF8Encoding(False))
        Catch ex As Exception
            DiagLog("Lissage du son : niveaux non reecrits (" & ex.Message & ")")
        End Try
        _source = ""                                       ' le morceau en cours sera remesure
        _correction = 0 : _cible = 0
        DiagLog("Lissage du son : remise a zero (" & If(videosSeulement, "videos", "tout") & "), " & a_oublier.Count & " niveaux oublies")
        Return a_oublier.Count
    End Function

    Private Sub Memoriser(fichierMrl As String, niveau As Double)
        If niveau > NiveauMaxPlausible Then
            DiagLog("Lissage du son : mesure impossible ignoree (" & niveau.ToString("0.0") & " dB) pour " & IO.Path.GetFileName(Uri.UnescapeDataString(fichierMrl)))
            Exit Sub
        End If
        Niveaux()(fichierMrl) = niveau
        Try
            IO.File.AppendAllText(FichierNiveaux(),
                niveau.ToString("0.00", Globalization.CultureInfo.InvariantCulture) & ControlChars.Tab & fichierMrl & vbCrLf,
                New Text.UTF8Encoding(False))
        Catch
            ' non memorise : il sera simplement remesure la prochaine fois
        End Try
    End Sub

End Module
