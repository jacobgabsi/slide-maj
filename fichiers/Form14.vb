Imports System.IO
Imports Vlc.DotNet.Forms

Public Class Form14
    Implements IFenetreNotes


    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form14DontClose = False
        VlcControl1.Stop()
        retablieTousLesVolumes()
        Form4.BringToFront()
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Form14_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TrackBar2.Value = 100
        VlcControl1.Audio.IsMute = False
        _infobulleSaut.SetToolTip(Button7, "Reculer de 5 minutes")
        _infobulleSaut.SetToolTip(Button8, "Avancer de 5 minutes")

    End Sub



    Private Sub DataGridView2_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentClick

    End Sub



    Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click
        ' Le OK : le film selectionne va dans la ligne de la minuterie donnee a l'ouverture (voir ChoixNflix).

        If ChoixNflix.LigneCible IsNot Nothing AndAlso Not ChoixNflix.Valider(DataGridView2) Then
            MsgBox("Sélectionnez d'abord un film dans la liste.", MsgBoxStyle.Information, "Nflix")
            Exit Sub                                  ' (la fenetre reste ouverte : rien n'a ete ecrit)
        End If
        Form14DontClose = False
        VlcControl1.Stop()
        VlcControl1.Audio.IsMute = True
        Me.Close()
    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs) Handles VlcControl1.Click

    End Sub

    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl1.VlcLibDirectoryNeeded

        e.VlcLibDirectory = New DirectoryInfo(VLCpath)

        OptionsDuLecteurVlc(sender)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll

        Label8.Text = TimeSpan.FromSeconds(CDbl(TrackBar1.Value)).ToString("hh\:mm\:ss")
        nom = VlcControl1.GetCurrentMedia.Title

        Dim extension As String = UCase(Path.GetExtension(nom))

        If extension = ".TS" Then
            VlcControl1.Position = TrackBar1.Value * 1000 / VlcControl1.Length
        Else
            VlcControl1.Time = TrackBar1.Value * 1000
        End If

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll

        'VlcControl1.Audio.Volume = TrackBar2.Value
        Label10.Text = TrackBar2.Value
        VlcControl1.Audio.IsMute = False
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If VlcControl1.IsPlaying Then
            VlcControl1.Pause()
            Button6.Text = ">"
        Else
            VlcControl1.Play()
            Button6.Text = "II"
        End If
    End Sub

    ' Recul et avance rapides (21/10/2026) : 5 minutes de part et d'autre de l'endroit ou l'on est.
    Private Const SautSecondes As Integer = 300
    Private ReadOnly _infobulleSaut As New ToolTip()

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Sauter(-SautSecondes)
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        Sauter(SautSecondes)
    End Sub

    ''' <summary>Se deplace de <paramref name="secondes"/> (negatif : recule) dans le film en cours, sans sortir de ses bornes ; la barre et l'heure suivent tout de suite.</summary>
    Private Sub Sauter(secondes As Integer)
        Try
            Dim longueur As Long = VlcControl1.Length                      ' (millisecondes ; 0 ou moins : direct, pas de position)
            If longueur <= 0 Then Exit Sub
            Dim nouveau As Long = Math.Max(0L, Math.Min(longueur - 1000L, VlcControl1.Time + CLng(secondes) * 1000L))
            Dim extension As String = ""
            Try
                extension = UCase(Path.GetExtension(VlcControl1.GetCurrentMedia.Title))
            Catch ex As Exception
            End Try
            If extension = ".TS" Then
                VlcControl1.Position = CSng(nouveau / CDbl(longueur))
            Else
                VlcControl1.Time = nouveau
            End If
            Dim s As Integer = CInt(nouveau \ 1000L)
            TrackBar1.Value = Math.Max(TrackBar1.Minimum, Math.Min(TrackBar1.Maximum, s))
            Label8.Text = TimeSpan.FromSeconds(s).ToString("hh\:mm\:ss")
        Catch ex As Exception
            DiagLog("Nflix : saut de " & secondes & " s impossible : " & ex.Message)
        End Try
    End Sub
    Private Sub DataGridView2_CellEnter(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellEnter
        'Exit Sub
        VlcControl1.Stop()
        VlcControl1.Audio.IsMute = True
        EffacerLesPistes()

        rw = e.RowIndex
        Try
            url = DataGridView2.Rows(e.RowIndex).Cells(1).Value
            Title = DataGridView2.Rows(e.RowIndex).Cells(0).Value
            ChannelLogo = DataGridView2.Rows(e.RowIndex).Cells(2).Value
            Try
                ChannelLogo = ChannelLogo.replace("_small", "_big")
            Catch ex As Exception
            End Try

            AfficheLogo(ChannelLogo, e.RowIndex, url, Title)
            If EstUneSerie(e.RowIndex) Then TextBox1.Text = ""        ' (pas d'adresse : ses episodes viennent du serveur)
            DemanderLePitch(e.RowIndex)
            ' Le groupe d'ou vient le film est selectionne dans la liste des groupes (sans en recharger le contenu : seul un clic dans cette liste le fait).
            Dim groupeDuFilm As String = RechercheNflix.GroupeDeLAdresse(Convert.ToString(url))
            If groupeDuFilm <> "" Then
                Dim rangGroupe As Integer = ListBox1.Items.IndexOf(groupeDuFilm)
                If rangGroupe >= 0 AndAlso ListBox1.SelectedIndex <> rangGroupe Then ListBox1.SelectedIndex = rangGroupe
            End If

        Catch ex As Exception

        End Try

    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

        Try
            RowIndex = PictureBox1.Tag
            If RowIndex IsNot Nothing Then

            End If

            If EstUneSerie(CInt(RowIndex)) Then                    ' une serie : on choisit son episode
                ChoisirUnEpisode(CInt(RowIndex))
                Exit Sub
            End If

            Nom = DataGridView2.Rows(RowIndex).Cells(1).Value

            PictureBox1.Visible = False
            VlcControl1.Visible = True
            Application.DoEvents()

            playNFlix(Nom)
        Catch ex As Exception

        End Try



    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        If VlcControl1.IsPlaying Then

            Try
                pos1 = VlcControl1.Time / 1000
                Label8.Text = TimeSpan.FromSeconds(CDbl(pos1)).ToString("hh\:mm\:ss")
                pos1 = VlcControl1.Time / 1000
                TrackBar1.Value = pos1
            Catch ex As Exception
            End Try

        End If


    End Sub

    Private Sub Form14_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        e.Cancel = Form14DontClose
    End Sub



    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Try

            If VlcControl1.IsPlaying Then
                pos1 = VlcControl1.Time / 1000
                pos2 = VlcControl1.Length / 1000
                TrackBar1.Maximum = pos2 '/ 60
                TrackBar1.Value = pos1 '/ 60
                GroupBox2.BringToFront()
                Label9.Text = TimeSpan.FromSeconds(CDbl(pos2)).ToString("hh\:mm\:ss")
                Label8.Text = "00:00:00"
                Label10.Text = TrackBar2.Value
                GroupBox2.Visible = True
                PictureBox1.Visible = False
                VlcControl1.Visible = True
                Timer2.Stop()
                Timer1.Start()
            End If
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Nflix ouvert : le son de Slide est coupe et le diaporama mis en pause (20/10/2026), pour qu'un film lance dans Nflix ne se
    ' superpose pas a la musique ou au son du diaporama. Meme silence que l'ouverture des reglages (SilenceDesReglages : musique en
    ' pause, lecteurs video coupes, rendus a la fermeture) ; la pause est celle du menu "Pause" du clic droit. Si les reglages sont deja
    ' ouverts (apercu depuis Form4) leur silence est deja en place : seul ce qui a ete fait ici est defait ici.
    ' ------------------------------------------------------------------------------------------

    ' (Voir PauseExterne : pas d'arret explicite du lecteur video, la boucle video du diaporama l'arrete elle-meme.)
    Private _pauseExterne As PauseExterne

    Private Sub Form14_SilenceEtPause(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        Try
            If Me.Visible Then
                If _pauseExterne Is Nothing Then _pauseExterne = New PauseExterne("Nflix ouvert")
            Else
                RestaurerLeDiaporama()
            End If
        Catch ex As Exception
            DiagLog("Nflix (silence et pause) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Nflix est cache OU REELLEMENT FERME : son rendu, pause levee. Appelee des deux endroits (fermer la fenetre ne declenche pas
    ''' toujours VisibleChanged : le diaporama restait alors en pause) ; sans effet la deuxieme fois.</summary>
    Private Sub RestaurerLeDiaporama()
        If _pauseExterne Is Nothing Then Exit Sub
        Dim p As PauseExterne = _pauseExterne
        _pauseExterne = Nothing
        p.Rendre()
    End Sub

    Private Sub Form14_Ferme(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        RestaurerLeDiaporama()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Fenetre agrandie, contenu agrandi (20/10/2026) : quand la fenetre change de taille, tous les controles gardent leur place
    ' et leurs proportions (position, taille, police, colonnes du tableau) au lieu de seulement s'etirer. La mesure de reference
    ' est prise au chargement ; la police suit le plus petit des deux rapports (largeur, hauteur). Le menu garde sa taille.
    ' En plein ecran (voir BasculerPleinEcran) rien n'est touche ; a la sortie, la mise a l'echelle est refaite.
    ' ------------------------------------------------------------------------------------------

    Private Class Gabarit
        Friend Limites As Rectangle
        Friend TaillePolice As Single
        Friend LargeursColonnes As Integer()
        Friend PoliceCreee As Font
    End Class

    Private ReadOnly _gabarits As New Dictionary(Of Control, Gabarit)
    Private _tailleDeBase As Size
    Private _echelleEnCours As Boolean

    Private Sub PrendreLaMesure(sender As Object, e As EventArgs) Handles MyBase.Load
        _tailleDeBase = Me.ClientSize
        EnregistrerLesGabarits(Me)
    End Sub

    Private Sub EnregistrerLesGabarits(parent As Control)
        For Each c As Control In parent.Controls
            If TypeOf c Is MenuStrip Then Continue For
            Dim g As New Gabarit With {.Limites = c.Bounds, .TaillePolice = c.Font.SizeInPoints}
            Dim grille As DataGridView = TryCast(c, DataGridView)
            If grille IsNot Nothing Then
                g.LargeursColonnes = grille.Columns.Cast(Of DataGridViewColumn)().Select(Function(col) col.Width).ToArray()
            End If
            c.Anchor = AnchorStyles.Top Or AnchorStyles.Left          ' (la mise a l'echelle replace tout : plus d'ancrage)
            _gabarits(c) = g
            If TypeOf c Is GroupBox OrElse TypeOf c Is Panel Then EnregistrerLesGabarits(c)
        Next
    End Sub

    Private Sub Form14_Redimensionnee(sender As Object, e As EventArgs) Handles Me.Resize
        AppliquerLEchelle()
    End Sub

    Private Sub AppliquerLEchelle()
        If _gabarits.Count = 0 OrElse _pleinEcran OrElse _echelleEnCours OrElse Me.WindowState = FormWindowState.Minimized Then Exit Sub
        If _tailleDeBase.Width <= 0 OrElse _tailleDeBase.Height <= 0 OrElse Me.ClientSize.Width <= 0 OrElse Me.ClientSize.Height <= 0 Then Exit Sub
        _echelleEnCours = True
        Me.SuspendLayout()
        Try
            Dim fx As Double = Me.ClientSize.Width / _tailleDeBase.Width
            Dim fy As Double = Me.ClientSize.Height / _tailleDeBase.Height
            Dim k As Double = Math.Min(fx, fy)
            For Each kv As KeyValuePair(Of Control, Gabarit) In _gabarits
                Dim c As Control = kv.Key
                Dim g As Gabarit = kv.Value
                c.Bounds = New Rectangle(CInt(Math.Round(g.Limites.X * fx)), CInt(Math.Round(g.Limites.Y * fy)),
                                         Math.Max(1, CInt(Math.Round(g.Limites.Width * fx))), Math.Max(1, CInt(Math.Round(g.Limites.Height * fy))))
                If Not (TypeOf c Is PictureBox OrElse TypeOf c Is Vlc.DotNet.Forms.VlcControl) Then
                    Dim voulue As Single = CSng(g.TaillePolice * k)
                    If Math.Abs(c.Font.SizeInPoints - voulue) > 0.15F Then
                        Dim nouvelle As New Font(c.Font.FontFamily, voulue, c.Font.Style)
                        c.Font = nouvelle
                        If g.PoliceCreee IsNot Nothing Then g.PoliceCreee.Dispose()
                        g.PoliceCreee = nouvelle
                    End If
                End If
                Dim grille As DataGridView = TryCast(c, DataGridView)
                If grille IsNot Nothing AndAlso g.LargeursColonnes IsNot Nothing AndAlso g.LargeursColonnes.Length = grille.Columns.Count Then
                    For i As Integer = 0 To g.LargeursColonnes.Length - 1
                        grille.Columns(i).Width = Math.Max(10, CInt(Math.Round(g.LargeursColonnes(i) * fx)))
                    Next
                End If
            Next
        Finally
            Me.ResumeLayout(True)
            _echelleEnCours = False
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Piste audio et sous-titres du film en cours (19/10/2026) : deux listes sous le lecteur, remplies avec les pistes que VLC
    ' trouve dans le flux (elles n'arrivent qu'apres quelques secondes de lecture). Comme Form6 : on ne change que si la piste
    ' choisie est differente (un changement redemarre le decodeur et fait sauter l'image).
    ' ------------------------------------------------------------------------------------------

    Private _remplissagePistes As Boolean
    Private _essaisPistes As Integer
    Private Const EssaisPistesMax As Integer = 20

    ''' <summary>Une lecture demarre : les listes sont videes et attendent les pistes du nouveau flux.</summary>
    Friend Sub PreparerLesPistes()
        EffacerLesPistes()
        _essaisPistes = 0
        TimerPistes.Start()
    End Sub

    ''' <summary>Plus de lecture (autre ligne choisie...) : listes vides et grisees.</summary>
    Friend Sub EffacerLesPistes()
        TimerPistes.Stop()
        _remplissagePistes = True
        Try
            ComboBoxAudio.Items.Clear()
            ComboBoxSousTitres.Items.Clear()
            ComboBoxAudio.Enabled = False
            ComboBoxSousTitres.Enabled = False
        Finally
            _remplissagePistes = False
        End Try
    End Sub

    Private Sub TimerPistes_Tick(sender As Object, e As EventArgs) Handles TimerPistes.Tick
        _essaisPistes += 1
        Try
            If VlcControl1.IsPlaying Then
                Dim nAudio As Integer = VlcControl1.Audio.Tracks.Count
                Dim nSous As Integer = VlcControl1.SubTitles.Count
                ' Les pistes se completent pendant les premieres secondes : les listes sont refaites quand leur nombre change.
                If (nAudio > 0 AndAlso nAudio <> ComboBoxAudio.Items.Count) OrElse (nSous > 0 AndAlso nSous <> ComboBoxSousTitres.Items.Count) Then
                    RemplirLesPistes()
                End If
            End If
        Catch ex As Exception
        End Try
        If _essaisPistes >= EssaisPistesMax Then TimerPistes.Stop()
    End Sub

    Private Sub RemplirLesPistes()
        _remplissagePistes = True
        Try
            ComboBoxAudio.Items.Clear()
            For Each piste In VlcControl1.Audio.Tracks.All
                ComboBoxAudio.Items.Add(piste.Name)
            Next
            ComboBoxSousTitres.Items.Clear()
            For Each piste In VlcControl1.SubTitles.All
                ComboBoxSousTitres.Items.Add(piste.Name)
            Next
            ' La piste en cours de lecture est celle qui est selectionnee.
            Dim audioEnCours = VlcControl1.Audio.Tracks.Current
            If audioEnCours IsNot Nothing Then ComboBoxAudio.SelectedIndex = IndexDePiste(VlcControl1.Audio.Tracks.All, audioEnCours.ID)
            Dim sousTitreEnCours = VlcControl1.SubTitles.Current
            If sousTitreEnCours IsNot Nothing Then ComboBoxSousTitres.SelectedIndex = IndexDePiste(VlcControl1.SubTitles.All, sousTitreEnCours.ID)
            ComboBoxAudio.Enabled = ComboBoxAudio.Items.Count > 1
            ComboBoxSousTitres.Enabled = ComboBoxSousTitres.Items.Count > 1
        Finally
            _remplissagePistes = False
        End Try
    End Sub

    Private Shared Function IndexDePiste(pistes As IEnumerable, id As Integer) As Integer
        Dim i As Integer = 0
        For Each piste As Object In pistes
            If CInt(piste.ID) = id Then Return i
            i += 1
        Next
        Return -1
    End Function

    Private Sub ComboBoxAudio_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxAudio.SelectedIndexChanged
        If _remplissagePistes OrElse ComboBoxAudio.SelectedIndex < 0 Then Exit Sub
        Try
            Dim voulue = VlcControl1.Audio.Tracks.All.ElementAt(ComboBoxAudio.SelectedIndex)
            Dim actuelle = VlcControl1.Audio.Tracks.Current
            If actuelle IsNot Nothing AndAlso actuelle.ID = voulue.ID Then Exit Sub
            VlcControl1.Audio.Tracks.Current = voulue
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ComboBoxSousTitres_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxSousTitres.SelectedIndexChanged
        If _remplissagePistes OrElse ComboBoxSousTitres.SelectedIndex < 0 Then Exit Sub
        Try
            Dim voulu = VlcControl1.SubTitles.All.ElementAt(ComboBoxSousTitres.SelectedIndex)
            Dim actuel = VlcControl1.SubTitles.Current
            If actuel IsNot Nothing AndAlso actuel.ID = voulu.ID Then Exit Sub
            VlcControl1.SubTitles.Current = voulu
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Plein ecran au double-clic sur la video (19/10/2026) : la fenetre prend tout l'ecran (sans bordure ni barre de titre),
    ' la video en haut, en bas la barre de lecture (lecture/pause, defilement, volume) et les listes audio / sous-titres.
    ' Un nouveau double-clic, ou Echap, revient a la fenetre normale.
    '
    ' Le double-clic est detecte en surveillant le bouton gauche de la souris : la video est dessinee par une fenetre
    ' native de VLC, qui garde la souris pour elle (la fenetre ne recoit ni Click ni DoubleClick de la video).
    ' La bordure est retiree par les styles de la fenetre (SetWindowLongPtr) et non par FormBorderStyle : celui-ci
    ' recree la fenetre et coupe la video en cours.
    ' ------------------------------------------------------------------------------------------

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function
    <Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetWindowLongPtrW")>
    Private Shared Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function
    <Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="SetWindowLongPtrW")>
    Private Shared Function SetWindowLongPtr(hWnd As IntPtr, nIndex As Integer, valeur As IntPtr) As IntPtr
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, x As Integer, y As Integer, cx As Integer, cy As Integer, flags As UInteger) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetDoubleClickTime() As UInteger
    End Function

    Private Const GWL_STYLE As Integer = -16
    Private Const WS_CAPTION As Long = &HC00000L
    Private Const WS_THICKFRAME As Long = &H40000L
    Private Const SWP_FRAMECHANGED As UInteger = &H20UI
    Private ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private ReadOnly HWND_NOTOPMOST As New IntPtr(-2)

    Private _pleinEcran As Boolean
    Private _styleAvant As Long
    Private _boitesAvant As Rectangle
    Private _etatFenetreAvant As FormWindowState
    Private ReadOnly _visibilitesAvant As New Dictionary(Of Control, Boolean)
    Private ReadOnly _placesAvant As New Dictionary(Of Control, Object())          ' Location, Size, Anchor
    Private ReadOnly _veilleSouris As New System.Windows.Forms.Timer With {.Interval = 30}
    Private _boutonEnfonce As Boolean
    Private _dernierClicLe As Long
    Private _dernierClicEn As Point

    ' La barre du bas (lecture, pistes) disparait apres 10 secondes sans mouvement de la souris et la video reprend toute
    ' la hauteur ; un mouvement de la souris la fait reparaitre (19/10/2026).
    Private Const HauteurBarre As Integer = 96
    Private Const DelaiBarreMs As Long = 10000
    Private _barreVisible As Boolean
    Private _dernierMouvementLe As Long
    Private _positionSouris As Point

    Private Sub MontrerLaBarre(visible As Boolean)
        If visible = _barreVisible Then Exit Sub
        _barreVisible = visible
        VlcControl1.Size = New Size(Me.ClientSize.Width, Me.ClientSize.Height - If(visible, HauteurBarre, 0))
        GroupBox2.Visible = visible
        LabelAudio.Visible = visible : ComboBoxAudio.Visible = visible
        LabelSousTitres.Visible = visible : ComboBoxSousTitres.Visible = visible
        If visible Then GroupBox2.BringToFront()
    End Sub

    Private Sub SurveillerLaBarre()
        Dim position As Point = Cursor.Position
        Dim maintenant As Long = Environment.TickCount64
        If position <> _positionSouris Then
            _positionSouris = position
            _dernierMouvementLe = maintenant
            MontrerLaBarre(True)
        ElseIf _barreVisible AndAlso maintenant - _dernierMouvementLe >= DelaiBarreMs AndAlso
               Not ComboBoxAudio.DroppedDown AndAlso Not ComboBoxSousTitres.DroppedDown Then
            MontrerLaBarre(False)
        End If
    End Sub

    Private Sub DemarrerLaVeilleDeLaSouris(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler _veilleSouris.Tick, AddressOf VeillerLaSouris
        _veilleSouris.Start()
    End Sub

    ''' <summary>Double-clic gauche sur la video (ou Echap en plein ecran) : voir plus haut.</summary>
    Private Sub VeillerLaSouris(sender As Object, e As EventArgs)
        Try
            If Not Me.Visible OrElse Form.ActiveForm IsNot Me Then
                _boutonEnfonce = False
                Exit Sub
            End If
            If _pleinEcran AndAlso (GetAsyncKeyState(Keys.Escape) And &H8000) <> 0 Then
                BasculerPleinEcran()
                Exit Sub
            End If
            If _pleinEcran Then SurveillerLaBarre()
            Dim enfonce As Boolean = (GetAsyncKeyState(Keys.LButton) And &H8000) <> 0
            If enfonce AndAlso Not _boutonEnfonce Then
                _boutonEnfonce = True
                Dim position As Point = Cursor.Position
                If VlcControl1.Visible AndAlso VlcControl1.RectangleToScreen(VlcControl1.ClientRectangle).Contains(position) Then
                    Dim maintenant As Long = Environment.TickCount64
                    Dim ecart As Integer = SystemInformation.DoubleClickSize.Width
                    If maintenant - _dernierClicLe <= GetDoubleClickTime() AndAlso
                       Math.Abs(position.X - _dernierClicEn.X) <= ecart AndAlso Math.Abs(position.Y - _dernierClicEn.Y) <= ecart Then
                        _dernierClicLe = 0
                        BasculerPleinEcran()
                    Else
                        _dernierClicLe = maintenant
                        _dernierClicEn = position
                    End If
                End If
            ElseIf Not enfonce Then
                _boutonEnfonce = False
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>La fenetre est fermee ou cachee en plein ecran : elle retrouve sa place pour la prochaine ouverture.</summary>
    Private Sub Form14_FinDuPleinEcran(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible AndAlso _pleinEcran Then BasculerPleinEcran()
    End Sub

    Private Sub MemoriserLaPlace(c As Control)
        _placesAvant(c) = {c.Location, c.Size, c.Anchor}
    End Sub

    Private Sub RetablirLaPlace(c As Control)
        Dim p As Object() = Nothing
        If Not _placesAvant.TryGetValue(c, p) Then Exit Sub
        c.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        c.Location = CType(p(0), Point)
        c.Size = CType(p(1), Size)
        c.Anchor = CType(p(2), AnchorStyles)
    End Sub

    Friend Sub BasculerPleinEcran()
        If Not _pleinEcran Then
            If Not VlcControl1.Visible Then Exit Sub
            _pleinEcran = True
            _etatFenetreAvant = Me.WindowState
            _boitesAvant = If(Me.WindowState = FormWindowState.Normal, Me.Bounds, Me.RestoreBounds)
            _visibilitesAvant.Clear()
            _placesAvant.Clear()
            Dim gardes As Control() = {VlcControl1, GroupBox2, LabelAudio, ComboBoxAudio, LabelSousTitres, ComboBoxSousTitres}
            For Each c As Control In Me.Controls
                _visibilitesAvant(c) = c.Visible
                If Not gardes.Contains(c) Then c.Visible = False
            Next
            For Each c As Control In gardes
                MemoriserLaPlace(c)
                c.Anchor = AnchorStyles.Top Or AnchorStyles.Left
            Next

            ' Fenetre sans bordure ni barre de titre, sur tout l'ecran (taches comprise), sans recreer la fenetre.
            Me.WindowState = FormWindowState.Normal
            Dim ecran As Rectangle = Screen.FromControl(Me).Bounds
            _styleAvant = GetWindowLongPtr(Me.Handle, GWL_STYLE).ToInt64()
            SetWindowLongPtr(Me.Handle, GWL_STYLE, New IntPtr(_styleAvant And Not (WS_CAPTION Or WS_THICKFRAME)))
            SetWindowPos(Me.Handle, HWND_TOPMOST, ecran.X, ecran.Y, ecran.Width, ecran.Height, SWP_FRAMECHANGED)

            Dim largeur As Integer = Me.ClientSize.Width
            Dim hauteur As Integer = Me.ClientSize.Height
            Const hBarre As Integer = HauteurBarre
            VlcControl1.Location = New Point(0, 0)
            VlcControl1.Size = New Size(largeur, hauteur - hBarre)
            ' Ligne des pistes, centree, puis la barre de lecture au-dessous.
            Dim total As Integer = LabelAudio.Width + 6 + ComboBoxAudio.Width + 40 + LabelSousTitres.Width + 6 + ComboBoxSousTitres.Width
            Dim x As Integer = Math.Max(0, (largeur - total) \ 2)
            Dim yPistes As Integer = hauteur - hBarre + 8
            LabelAudio.Location = New Point(x, yPistes + 4) : x += LabelAudio.Width + 6
            ComboBoxAudio.Location = New Point(x, yPistes) : x += ComboBoxAudio.Width + 40
            LabelSousTitres.Location = New Point(x, yPistes + 4) : x += LabelSousTitres.Width + 6
            ComboBoxSousTitres.Location = New Point(x, yPistes)
            GroupBox2.Location = New Point(Math.Max(0, (largeur - GroupBox2.Width) \ 2), hauteur - GroupBox2.Height - 12)
            GroupBox2.Visible = True
            LabelAudio.Visible = True : ComboBoxAudio.Visible = True : LabelSousTitres.Visible = True : ComboBoxSousTitres.Visible = True
            GroupBox2.BringToFront()
            _barreVisible = True
            _dernierMouvementLe = Environment.TickCount64
            _positionSouris = Cursor.Position
            DiagLog("Nflix : plein ecran")
        Else
            _pleinEcran = False
            SetWindowLongPtr(Me.Handle, GWL_STYLE, New IntPtr(_styleAvant))
            SetWindowPos(Me.Handle, HWND_NOTOPMOST, _boitesAvant.X, _boitesAvant.Y, _boitesAvant.Width, _boitesAvant.Height, SWP_FRAMECHANGED)
            If _etatFenetreAvant = FormWindowState.Maximized Then Me.WindowState = FormWindowState.Maximized
            For Each c As Control In _placesAvant.Keys.ToList()
                RetablirLaPlace(c)
            Next
            For Each kv As KeyValuePair(Of Control, Boolean) In _visibilitesAvant
                kv.Key.Visible = kv.Value
            Next
            AppliquerLEchelle()                              ' (la fenetre a pu changer de taille pendant le plein ecran)
            DiagLog("Nflix : retour a la fenetre")
        End If
    End Sub

    ''' <summary>Bouton "Rechercher" (remplace le menu du meme nom ; demande de Jacob, 13/10/2026).</summary>
    Private Sub ButtonRechercher_Click(sender As Object, e As EventArgs) Handles ButtonRechercher.Click
        rechercherUnTitre()
    End Sub

    Private Sub Form14_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Control AndAlso e.KeyCode = Keys.F Then
            e.SuppressKeyPress = True
            rechercherUnTitre()
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_Click(sender As Object, e As EventArgs) Handles ListBox1.Click
        groupe = ListBox1.Items(ListBox1.SelectedIndex).ToString
        TextBox1.Text = ""
        Form4.Cursor = Cursors.WaitCursor
        ChercheGroupe(groupe)
        Form4.Cursor = Cursors.Default
    End Sub

    Private Sub DataGridView2_CellContentDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentDoubleClick
        If e.RowIndex < 0 Then Exit Sub
        If EstUneSerie(e.RowIndex) Then                        ' une serie : on choisit son episode (puis il est lu)
            ChoisirUnEpisode(e.RowIndex)
        Else
            LireLaLigne(e.RowIndex)
        End If
    End Sub

    ''' <summary>Lit la chaine, le film ou l'episode de la ligne : adresse et titre dans la fenetre, puis lecture.</summary>
    Private Sub LireLaLigne(ligne As Integer)
        VlcControl1.Stop()
        url = DataGridView2.Rows(ligne).Cells(1).Value
        Title = DataGridView2.Rows(ligne).Cells(0).Value
        ChannelLogo = DataGridView2.Rows(ligne).Cells(2).Value

        Try
            If Not (String.IsNullOrEmpty(url)) Then
                Clipboard.SetText(url)
            End If
        Catch ex As Exception

        End Try

        PictureBox1.Visible = False
        VlcControl1.Visible = True
        Label1.Text = Str(ligne)
        TextBox1.Text = url
        TextBox2.Text = Title
        playNFlix(url)
        VlcControl1.Audio.IsMute = False
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Films et series d'un compte Xtream (voir IptvSources) : liste "Contenu" au-dessus des chaines.
    ' Une serie n'a pas d'adresse dans le catalogue : on choisit son episode (FormEpisodes), puis la
    ' ligne devient cet episode (titre "Serie S01E02", adresse de l'episode) et il est lu.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ce que la ligne d'une serie retient pour que son choix puisse etre refait.</summary>
    Private Class SerieChoisie
        Public Numero As String
        Public NomDeLaSerie As String
    End Class

    ''' <summary>Menu Fichier > "Nouveautés Film rajoutée" > une des six periodes (cette semaine, la semaine derniere, ce mois, 2 et 3 derniers
    ''' mois, cette annee) : les films ajoutes au catalogue du serveur pendant la periode, dans le meme tableau que la recherche (voir
    ''' RechercheNflix.NouveauxFilms). 20/10/2026 : la date d'ajout vient du serveur (tvg-added) ; plus de comparaison avec une liste gardee.</summary>
    Private Sub MenuNouveautes_Click(sender As Object, e As EventArgs) Handles MenuNouvSemaine.Click, MenuNouvSemainePrec.Click, MenuNouvMois.Click,
                                                                               MenuNouv2Mois.Click, MenuNouv3Mois.Click, MenuNouvAnnee.Click
        Dim element As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
        If element Is Nothing Then Exit Sub
        AfficherLesNouveautes(CType(CInt(element.Tag), RechercheNflix.PeriodeNouveautes))
    End Sub

    ''' <summary>La case "Que les films en Francais" du menu est la meme que celle de la fenetre Rechercher (RechercheNflix.QueFrancais).</summary>
    Private Sub MenuNouveautesFilms_DropDownOpening(sender As Object, e As EventArgs) Handles MenuNouveautesFilms.DropDownOpening
        MenuNouvFrancais.Checked = RechercheNflix.QueFrancais
    End Sub

    Private Sub MenuNouvFrancais_CheckedChanged(sender As Object, e As EventArgs) Handles MenuNouvFrancais.CheckedChanged
        RechercheNflix.QueFrancais = MenuNouvFrancais.Checked
    End Sub

    Private Async Sub AfficherLesNouveautes(periode As RechercheNflix.PeriodeNouveautes)
        Dim source As Integer = IptvSources.SelectionIptv
        If Not IptvSources.EstXtream(source) Then
            MessageBox.Show(Me, "Les nouveautés demandent un compte Xtream (adresse du serveur, utilisateur et mot de passe).", "Nouveautés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        MenuFichier.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            VlcControl1.Stop()
            Me.Text = "Nflix - recherche des nouveautés..."
            Dim message As String = ""
            ' Catalogue de moins de 6 heures et portant les dates d'ajout, sinon retelecharge (quelques secondes).
            Dim ok As Boolean = Await Task.Run(Function() RechercheNflix.AssurerLesDates(source, 6.0, message))
            If Not ok Then
                Me.Text = "Nflix"
                MessageBox.Show(Me, message, "Nouveautés", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If
            Dim libelle As String = ""
            Dim nouveaux As List(Of RechercheNflix.Entree) = RechercheNflix.NouveauxFilms(source, periode, libelle)
            ' La liste de gauche montre les groupes des films (comme la recherche : seulement ceux qui ont des nouveautes).
            IptvSources.GenreAffiche = IptvSources.GenreIptv.Films
            RemplirLeContenu()
            RechercheNflix.FiltreDesGroupes = If(nouveaux.Count > 0, nouveaux.Select(Function(x) x.Groupe).Distinct().ToList(), Nothing)
            ReafficherLesGroupes()
            RechercheNflix.RemplirLaGrille(DataGridView2, nouveaux.Take(RechercheNflix.MaximumDeResultats).ToList())
            DataGridView2.Visible = True
            TextBox1.Text = ""
            TextBox2.Text = ""
            TextBoxPitch.Text = ""
            Me.Text = "Nflix - " & nouveaux.Count & If(nouveaux.Count > 1, " nouveaux films", " nouveau film") & " ajouté" & If(nouveaux.Count > 1, "s", "") & " " & libelle
            If nouveaux.Count = 0 Then
                Me.Text = "Nflix"
                MessageBox.Show(Me, "Aucun film ajouté " & libelle & "." &If(message <> "", vbCrLf & vbCrLf & message, ""), "Nouveautés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ElseIf message <> "" Then
                MessageBox.Show(Me, message, "Nouveautés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            Me.Text = "Nflix"
            MessageBox.Show(Me, "Les nouveautés n'ont pas pu être affichées : " & ex.Message, "Nouveautés", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            Cursor = DefaultCursor
            MenuFichier.Enabled = True
        End Try
    End Sub

    ''' <summary>Menu Fichier > "Maj Liste IPTV" (19/10/2026) : la liste des chaines, le catalogue des films et celui des series de
    ''' la source choisie sont retelecharges tout de suite (sinon : chaines a la demande, catalogues gardes 24 h). Une liste qui
    ''' ne peut pas etre retelechargee garde son ancienne version (voir IptvSources.Telecharger / TelechargerCatalogue).</summary>
    Private Async Sub MenuMajListeIptv_Click(sender As Object, e As EventArgs) Handles MenuMajListeIptv.Click
        Dim source As Integer = IptvSources.SelectionIptv
        If IptvSources.UrlIptv(source) = "" Then
            MessageBox.Show(Me, "Cette IPTV n'a pas d'adresse : renseignez-la dans les options (onglet Divers).", "Maj Liste IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        MenuFichier.Enabled = False
        Cursor = Cursors.WaitCursor
        Dim bilan As New List(Of String)
        Dim echecs As New List(Of String)
        Try
            VlcControl1.Stop()
            Me.Text = "Nflix - mise à jour de la liste IPTV..."
            Dim xtream As Boolean = IptvSources.EstXtream(source)
            Dim resultat As Object() = Await Task.Run(Function() As Object()
                                                          Dim erreurChaines As String = ""
                                                          Dim okChaines As Boolean = IptvSources.Telecharger(source, erreurChaines)
                                                          Dim erreurFilms As String = "", okFilms As Boolean = True
                                                          Dim erreurSeries As String = "", okSeries As Boolean = True
                                                          If xtream Then
                                                              okFilms = IptvSources.TelechargerCatalogue(source, IptvSources.GenreIptv.Films, erreurFilms)
                                                              okSeries = IptvSources.TelechargerCatalogue(source, IptvSources.GenreIptv.Series, erreurSeries)
                                                          End If
                                                          Return New Object() {okChaines, erreurChaines, okFilms, erreurFilms, okSeries, erreurSeries}
                                                      End Function)
            Dim chaines As Integer = IptvSources.NombreDEntrees(IptvSources.FichierIptv(source))
            If CBool(resultat(0)) Then bilan.Add(chaines & " chaînes") Else echecs.Add("Chaînes : " & CStr(resultat(1)))
            If xtream Then
                If CBool(resultat(2)) Then bilan.Add(IptvSources.NombreDEntrees(IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Films)) & " films") Else echecs.Add("Films : " & CStr(resultat(3)))
                If CBool(resultat(4)) Then bilan.Add(IptvSources.NombreDEntrees(IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Series)) & " séries") Else echecs.Add("Séries : " & CStr(resultat(5)))
            End If
            DiagLog("Maj Liste IPTV : source " & (source + 1) & " : " & If(bilan.Count > 0, String.Join(", ", bilan), "rien de mis à jour") & If(echecs.Count > 0, " | échecs : " & String.Join(" ; ", echecs), ""))

            ' Les listes affichees repartent des nouveaux fichiers.
            RechercheNflix.Oublier()
            RechercheNflix.FiltreDesGroupes = Nothing
            RechercheNflix.DerniereRecherche = ""
            DataGridView2.Rows.Clear()
            TextBox1.Text = ""
            TextBox2.Text = ""
            TextBoxPitch.Text = ""
            Me.Text = "Nflix"
            rechercher()                                        ' les groupes de la liste affichee
            PositionnerSurLeDernierAjout()
        Catch ex As Exception
            echecs.Add(ex.Message)
            Me.Text = "Nflix"
        Finally
            Cursor = DefaultCursor
            MenuFichier.Enabled = True
        End Try
        If echecs.Count > 0 Then
            MessageBox.Show(Me, If(bilan.Count > 0, "Mis à jour : " & String.Join(", ", bilan) & vbCrLf & vbCrLf, "") & "Non mis à jour (l'ancienne liste est gardée) :" & vbCrLf & String.Join(vbCrLf, echecs), "Maj Liste IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Else
            MessageBox.Show(Me, "Liste IPTV à jour : " & String.Join(", ", bilan) & ".", "Maj Liste IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ''' <summary>Bouton "Films très bien notés" de la fenêtre Rechercher (19/10/2026) : les films notés
    ''' <see cref="RechercheNflix.NoteTresBonne"/> ou plus, dans le même tableau que la recherche. Le texte saisi et le groupe choisi
    ''' (case "Dans ce groupe") restreignent la liste quand ils sont remplis.</summary>
    Friend Async Sub AfficherLesTresBienNotes(texte As String, groupeChoisi As String, dansLeGroupe As Boolean, Optional excellents As Boolean = False, Optional bons As Boolean = False)
        Dim source As Integer = IptvSources.SelectionIptv
        If Not IptvSources.EstXtream(source) Then
            MessageBox.Show(Me, "Les notes demandent un compte Xtream (adresse du serveur, utilisateur et mot de passe).", "Films très bien notés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Cursor = Cursors.WaitCursor
        Try
            VlcControl1.Stop()
            Me.Text = "Nflix - recherche des films très bien notés..."
            Dim message As String = ""
            Dim ok As Boolean = Await Task.Run(Function() RechercheNflix.AssurerLesNotes(source, message))
            If Not ok Then
                Me.Text = "Nflix"
                MessageBox.Show(Me, message, "Films très bien notés", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If
            Dim tronque As Boolean = False
            Dim groupe As String = If(dansLeGroupe, groupeChoisi, "")
            Dim trouves As List(Of RechercheNflix.Entree) = RechercheNflix.FilmsTresBienNotes(source, texte, groupe, RechercheNflix.MaximumDeResultats, tronque, excellents, bons)
            IptvSources.GenreAffiche = IptvSources.GenreIptv.Films
            RemplirLeContenu()
            RechercheNflix.FiltreDesGroupes = If(trouves.Count > 0, trouves.Select(Function(x) x.Groupe).Distinct().ToList(), Nothing)
            RechercheNflix.DerniereRecherche = texte
            ReafficherLesGroupes()
            _seuilRecherche = If(bons, RechercheNflix.NoteBonne, If(excellents, RechercheNflix.NoteExcellente, RechercheNflix.NoteTresBonne)) : _rechercheExcellents = excellents AndAlso Not bons          ' (la verification IMDb de ColorerSelonLesNotes s'en sert)
            RechercheNflix.RemplirLaGrille(DataGridView2, trouves)
            DataGridView2.Visible = True
            TextBox1.Text = ""
            TextBox2.Text = ""
            TextBoxPitch.Text = ""
            Dim seuil As String = If(bons, RechercheNflix.NoteBonne, If(excellents, RechercheNflix.NoteExcellente, RechercheNflix.NoteTresBonne)).ToString("0.#", Globalization.CultureInfo.CurrentCulture)
            Dim formule As String = If(excellents, "noté plus de " & seuil, "noté " & seuil & " ou plus")
            Dim formulePluriel As String = If(excellents, "notés plus de " & seuil, "notés " & seuil & " ou plus")
            If trouves.Count = 0 Then
                Me.Text = "Nflix"
                MessageBox.Show(Me, "Aucun film " & formule & If(texte <> "", " pour « " & texte & " »", "") & If(groupe <> "", " dans ce groupe", "") & "." &
                                If(message <> "", vbCrLf & vbCrLf & message, ""), "Films très bien notés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                Me.Text = "Nflix - " & If(tronque, "les " & trouves.Count & " mieux notés : ", "") & trouves.Count & " films " & formulePluriel & If(texte <> "", " pour « " & texte & " »", "")
                If message <> "" Then MessageBox.Show(Me, message, "Films très bien notés", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            Me.Text = "Nflix"
            MessageBox.Show(Me, "La recherche des films très bien notés a échoué : " & ex.Message, "Films très bien notés", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            Cursor = DefaultCursor
        End Try
    End Sub

    ''' <summary>Apres le remplissage du tableau (19/10/2026) : les notes des films affiches sont lues en direct sur le serveur
    ''' (une requete par groupe, au plus 12 groupes) et les films notes <see cref="RechercheNflix.NoteTresBonne"/> ou plus passent
    ''' au vert. Le tableau est deja rempli et utilisable pendant ce temps.</summary>
    Friend Async Sub ColorerSelonLesNotes(entrees As List(Of RechercheNflix.Entree)) Implements IFenetreNotes.ColorerSelonLesNotes
        Dim seuilRecherche As Double = _seuilRecherche : Dim excellentsRecherche As Boolean = _rechercheExcellents : _seuilRecherche = 0
        Try
            Dim source As Integer = IptvSources.SelectionIptv
            If Not IptvSources.EstXtream(source) Then Exit Sub
            Dim groupes As List(Of String) = entrees.Where(Function(x) x.Contenu() = "Film").Select(Function(x) x.Groupe).Distinct().Take(12).ToList()
            If groupes.Count = 0 Then Exit Sub
            Dim notes As New Dictionary(Of String, Double)(StringComparer.Ordinal)
            Await Task.Run(Sub()
                               For Each g As String In groupes
                                   Dim d As Dictionary(Of String, Double) = IptvSources.NotesDuGroupe(source, g)
                                   If d IsNot Nothing Then
                                       For Each kv As KeyValuePair(Of String, Double) In d
                                           notes(kv.Key) = kv.Value
                                       Next
                                   End If
                               Next
                           End Sub)
            If IsDisposed Then Exit Sub
            If notes.Count > 0 Then
            For Each ligne As DataGridViewRow In DataGridView2.Rows
                Dim numero As String = IptvSources.NumeroDuFilm(Convert.ToString(ligne.Cells(1).Value))
                Dim x As Double
                If numero <> "" AndAlso notes.TryGetValue(numero, x) Then RechercheNflix.ColorerSelonLaNote(ligne, x)
            Next
            End If
            VerifierAvecImdb(seuilRecherche, excellentsRecherche)
        Catch ex As Exception
            DiagLog("Notes en direct (couleurs) : " & ex.Message)
        End Try
    End Sub

    Private _seuilRecherche As Double
    Private _rechercheExcellents As Boolean
    Private _generationImdb As Integer
    Private Const MaxVerifImdb As Integer = 150

    ''' <summary>En tache de fond (22/10/2026 : "la note du serveur differe beaucoup de celle d'IMDb : change la couleur des lignes qui ne correspondent pas a la couleur basee sur IMDb ;
    ''' dans la recherche, base-toi sur les deux criteres"). Les films que la note du SERVEUR a colores sont verifies un a un sur OMDb (NotesOmdb.NoteImdb, gardee dans un fichier : un
    ''' film deja interroge ne coute plus de requete ; 1 000 requetes par jour au plus) et prennent la couleur de leur note IMDb (rose, vert ou rien).
    ''' Apres une recherche "Films > 8" ou "Films > 9" (<paramref name="seuilRecherche"/> non nul) les films dont la note IMDb connue n'atteint pas le seuil sont retires de la liste :
    ''' il faut que les DEUX notes soient bonnes. Un film inconnu d'OMDb garde sa couleur et reste. S'arrete des que le tableau est rempli de nouveau ou fermee.</summary>
    Private Async Sub VerifierAvecImdb(seuilRecherche As Double, excellents As Boolean)
        _generationImdb += 1
        Dim generation As Integer = _generationImdb
        Try
            Dim cle As String = NotesOmdb.CleDAcces()
            If cle = "" Then Exit Sub
            Dim debut As Integer = Math.Max(0, DataGridView2.FirstDisplayedScrollingRowIndex)
            Dim candidates As New List(Of DataGridViewRow)
            For i As Integer = 0 To DataGridView2.Rows.Count - 1
                Dim ligne As DataGridViewRow = DataGridView2.Rows((debut + i) Mod DataGridView2.Rows.Count)
                If ligne.DefaultCellStyle.BackColor.A <> 0 AndAlso Convert.ToString(ligne.Cells(1).Value).Contains("/movie/", StringComparison.OrdinalIgnoreCase) Then candidates.Add(ligne)
                If candidates.Count >= MaxVerifImdb Then Exit For
            Next
            If candidates.Count = 0 Then Exit Sub
            Dim aRetirer As New List(Of DataGridViewRow)
            Dim policeBarree As New Font(DataGridView2.Font, FontStyle.Strikeout)
            Dim recolores As Integer = 0
            For Each ligne As DataGridViewRow In candidates
                If generation <> _generationImdb OrElse IsDisposed Then Exit Sub
                Dim nom As String = Convert.ToString(ligne.Cells(0).Value)
                Dim note As Double = Await Task.Run(Function() NotesOmdb.NoteImdb(cle, nom))
                If generation <> _generationImdb OrElse IsDisposed Then Exit Sub
                If note < 0 Then
                    DiagLog("Notes IMDb (fond) : OMDb ne repond plus ou limite atteinte, verification arretee apres " & recolores & " film(s)")
                    Exit For
                End If
                If ligne.DataGridView Is Nothing Then Continue For
                If note = 0 Then
                    ' OMDb ne connait pas ce film (aucune note IMDb) : dans une recherche sur les deux notes, il ne qualifie pas (22/10/2026 : "les lignes laissees n'ont aucune note").
                    If seuilRecherche > 0 Then
                        aRetirer.Add(ligne)
                        ligne.DefaultCellStyle.BackColor = Color.Empty
                        ligne.DefaultCellStyle.ForeColor = Color.Gray
                        ligne.DefaultCellStyle.Font = policeBarree
                    End If
                    Continue For
                End If
                recolores += 1
                RechercheNflix.ColorerSelonLaNoteImdb(ligne, note)
                If seuilRecherche > 0 AndAlso If(excellents, Not (note + RechercheNflix.EcartImdb > seuilRecherche), note + RechercheNflix.EcartImdb < seuilRecherche) Then
                    aRetirer.Add(ligne)
                    ligne.DefaultCellStyle.BackColor = Color.Empty          ' barre et grise tout de suite, sans retirer la ligne (22/10/2026)
                    ligne.DefaultCellStyle.ForeColor = Color.Gray
                    ligne.DefaultCellStyle.Font = policeBarree
                End If
            Next
            If seuilRecherche > 0 AndAlso aRetirer.Count > 0 AndAlso generation = _generationImdb AndAlso Not IsDisposed Then
                Me.Text = "Nflix - " & (DataGridView2.Rows.Count - aRetirer.Count) & " films ayant les deux notes (serveur et IMDb) " & If(excellents, "au-dessus de ", "de ") & seuilRecherche.ToString("0.#", Globalization.CultureInfo.CurrentCulture) & " (" & aRetirer.Count & " barrés : note IMDb trop basse ou absente)"
            End If
        Catch ex As Exception
            DiagLog("Notes IMDb (fond) : " & ex.Message)
        End Try
    End Sub

    Private _remplissageContenu As Boolean

    ''' <summary>La liste "Contenu" : visible pour un compte Xtream seulement, sur le genre affiche.</summary>
    Private Sub RemplirLeContenu()
        _remplissageContenu = True
        Try
            ComboBoxContenu.Items.Clear()
            ComboBoxContenu.Items.Add("Chaînes en direct")
            ComboBoxContenu.Items.Add("Films")
            ComboBoxContenu.Items.Add("Séries")
            ComboBoxContenu.SelectedIndex = CInt(IptvSources.GenreAffiche)
        Finally
            _remplissageContenu = False
        End Try
        Dim xtream As Boolean = IptvSources.EstXtream(IptvSources.SelectionIptv)
        ComboBoxContenu.Visible = xtream
        LabelContenuIptv.Visible = xtream
    End Sub

    Private Sub Form14_ContenuIptv(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible Then Exit Sub
        RemplirLeContenu()
        PositionnerSurLeDernierAjout()
    End Sub

    ''' <summary>S'il existe un groupe "Le dernier ajouté" (ou "Le drenier ajoutee"...), la liste s'y positionne et
    ''' en affiche le contenu (demande de Jacob, 13/10/2026). Sans ce groupe : rien ne change.</summary>
    Private Sub PositionnerSurLeDernierAjout()
        MettreAJourLeTitreDesGroupes()
        Try
            Dim noms As New List(Of String)
            For Each element As Object In ListBox1.Items
                noms.Add(Convert.ToString(element))
            Next
            Dim rang As Integer = RechercheNflix.RangDuDernierAjout(noms)
            If rang < 0 Then Exit Sub
            ListBox1.SelectedIndex = rang                  ' (la liste defile jusqu'a lui)
            groupe = noms(rang)
            TextBox1.Text = ""
            Cursor = Cursors.WaitCursor
            Try
                ChercheGroupe(groupe)
            Finally
                Cursor = DefaultCursor
            End Try
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Titre de la liste des groupes (demande de Jacob, 13/10/2026 : "rajoute un titre au tableau de gauche
    ' pour pouvoir trier") : un clic change l'ordre - celui du fournisseur, A a Z, Z a A.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>"Groupes (717)" + la fleche de l'ordre en cours.</summary>
    Private Sub MettreAJourLeTitreDesGroupes()
        Dim fleche As String = ""
        Select Case RechercheNflix.OrdreDesGroupes
            Case 1 : fleche = "  ▲  A → Z"
            Case 2 : fleche = "  ▼  Z → A"
        End Select
        ButtonTriGroupes.Text = If(RechercheNflix.FiltreDesGroupes IsNot Nothing, "Groupes trouvés (", "Groupes (") & ListBox1.Items.Count & ")" & fleche
    End Sub

    ''' <summary>Remplit la liste des groupes : tous (ordre choisi), ou ceux que la derniere recherche a trouves.
    ''' Le groupe choisi, s'il y est encore, reste selectionne.</summary>
    Friend Sub ReafficherLesGroupes()
        Dim choisi As String = If(ListBox1.SelectedItem Is Nothing, "", Convert.ToString(ListBox1.SelectedItem))
        Dim ordre As List(Of String) = RechercheNflix.GroupesTries(RechercheNflix.Groupes(IptvSources.ListeAffichee()))
        ListBox1.BeginUpdate()
        Try
            ListBox1.Items.Clear()
            ListBox1.Items.AddRange(ordre.ToArray())
        Finally
            ListBox1.EndUpdate()
        End Try
        If choisi <> "" Then
            Dim k As Integer = ListBox1.Items.IndexOf(choisi)
            If k >= 0 Then ListBox1.SelectedIndex = k            ' (la liste defile jusqu'au groupe choisi)
        End If
        MettreAJourLeTitreDesGroupes()
    End Sub

    ''' <summary>Bouton "RAZ recherche" : la recherche est effacee, la liste des groupes revient en entier et la liste de
    ''' droite est videe (15/10/2026). Equivalent d'une recherche vide, sans passer par la saisie.</summary>
    Private Sub ButtonRazRecherche_Click(sender As Object, e As EventArgs) Handles ButtonRazRecherche.Click
        RechercheNflix.DerniereRecherche = ""
        RechercheNflix.FiltreDesGroupes = Nothing
        ReafficherLesGroupes()
        DataGridView2.Rows.Clear()
        Me.Text = "Nflix"
        TextBox1.Text = ""                              ' adresse et titre du dernier element lu
        TextBox2.Text = ""
        TextBoxPitch.Text = ""
        Try
            ListBox1.ClearSelected()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonTriGroupes_Click(sender As Object, e As EventArgs) Handles ButtonTriGroupes.Click
        RechercheNflix.OrdreDesGroupes = (RechercheNflix.OrdreDesGroupes + 1) Mod 3
        ReafficherLesGroupes()
    End Sub

    Private Sub ComboBoxContenu_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxContenu.SelectedIndexChanged
        If _remplissageContenu OrElse ComboBoxContenu.SelectedIndex < 0 Then Exit Sub
        Dim genre As IptvSources.GenreIptv = CType(ComboBoxContenu.SelectedIndex, IptvSources.GenreIptv)
        If genre = IptvSources.GenreAffiche Then Exit Sub
        Dim erreur As String = ""
        Dim ok As Boolean
        Cursor = Cursors.WaitCursor
        Try
            VlcControl1.Stop()
            ok = IptvSources.AssurerLeCatalogue(IptvSources.SelectionIptv, genre, erreur)      ' premier besoin : quelques secondes
        Finally
            Cursor = DefaultCursor
        End Try
        If Not ok Then
            MessageBox.Show(Me, "Le catalogue n'a pas pu être chargé :" & vbCrLf & erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            RemplirLeContenu()                                 ' revient a ce qui est affiche
            Exit Sub
        End If
        IptvSources.GenreAffiche = genre
        DataGridView2.Rows.Clear()
        TextBox1.Text = ""
        TextBox2.Text = ""
        Cursor = Cursors.WaitCursor
        Try
            rechercher()                                       ' les groupes du catalogue
        Finally
            Cursor = DefaultCursor
        End Try
        PositionnerSurLeDernierAjout()
    End Sub

    ''' <summary>La ligne est une serie du catalogue (adresse "slide-serie:..."), ou une serie dont un episode
    ''' a deja ete choisi.</summary>
    Private Function EstUneSerie(ligne As Integer) As Boolean
        If ligne < 0 OrElse ligne >= DataGridView2.Rows.Count Then Return False
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        If TypeOf rangee.Tag Is SerieChoisie Then Return True
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Return adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Le pitch du film ou de la serie choisi (demande de Jacob, 13/10/2026 : "quand je click sur un film
    ' rajoute le pitch") : demande au serveur du compte en tache de fond (IptvSources.FicheDuTitre),
    ' affichee sous l'apercu. Une ligne choisie vite apres une autre : seule la derniere compte.
    ' ------------------------------------------------------------------------------------------

    Private _pitchDemande As String = ""

    Private Async Sub DemanderLePitch(ligne As Integer)
        Dim cle As String = ""
        Dim titreFilm As String = ""
        Try
            If ligne >= 0 AndAlso ligne < DataGridView2.Rows.Count Then
                Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
                Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
                titreFilm = Convert.ToString(rangee.Cells(0).Value)
                Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
                If serie IsNot Nothing Then
                    cle = "serie:" & serie.Numero
                ElseIf adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
                    cle = "serie:" & adresse.Substring(IptvSources.PrefixeSerie.Length)
                ElseIf IptvSources.NumeroDuFilm(adresse) <> "" Then
                    cle = "film:" & IptvSources.NumeroDuFilm(adresse)
                End If
            End If
        Catch ex As Exception
            cle = ""
        End Try
        _pitchDemande = cle
        TextBoxPitch.Text = ""
        MontrerLaNote("", 0)
        If cle = "" Then Exit Sub                      ' une chaine en direct : pas de pitch
        TextBoxPitch.Text = "Chargement de la fiche..."
        Dim source As Integer = IptvSources.SelectionIptv
        Try
            Await Task.Delay(250)                      ' (la ligne peut encore changer : fleches tenues)
            If cle <> _pitchDemande OrElse IsDisposed Then Exit Sub
            Dim fiche As IptvSources.FicheIptv = Await Task.Run(Function() IptvSources.FicheDuTitre(source, cle))
            If cle <> _pitchDemande OrElse IsDisposed Then Exit Sub
            TextBoxPitch.Text = If(fiche Is Nothing, "(la fiche n'a pas pu être chargée)", fiche.EnTexte())
            ChargerLaNoteImdb(cle, titreFilm, fiche)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>La note IMDB du film choisi, sous le bouton "Plus d'infos" (22/10/2026 : "la note du serveur peut etre 10/10 alors que celle d'IMDB est 6/10 : c'est celle-la qui m'interesse").
    ''' Elle vient d'OMDb (voir NotesOmdb), pas du serveur IPTV, dont la note (TMDB le plus souvent, parfois fantaisiste) n'est pas celle d'IMDB. Meme code couleur que la liste : rose
    ''' au-dessus de 9, vert a partir de 8. Vide pour une chaine ; un mot dit pourquoi si la note manque (cle absente, film introuvable...).</summary>
    Private Async Sub ChargerLaNoteImdb(cleDemande As String, titre As String, fiche As IptvSources.FicheIptv)
        Try
            If fiche Is Nothing Then Exit Sub
            Dim cle As String = NotesOmdb.CleDAcces()
            If cle = "" Then
                MontrerLaNote("", 0)
                Exit Sub
            End If
            MontrerLaNote("", 0)
            Dim texte As String = Await Task.Run(Function() NotesOmdb.Chercher(cle, fiche.TitreOriginal, titre, fiche.Annee))
            If cleDemande <> _pitchDemande OrElse IsDisposed Then Exit Sub
            DiagLog("Note IMDB (libelle) : « " & titre & " » (original « " & fiche.TitreOriginal & " », annee " & fiche.Annee & ") -> " & Replace(If(texte, ""), vbCrLf, " | "))
            Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(texte, ""), "IMDb : (\d+(?:[.,]\d+)?) / 10")
            Dim note As Double
            If m.Success AndAlso Double.TryParse(m.Groups(1).Value.Replace(",", "."), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, note) AndAlso note > 0 Then
                MontrerLaNote("IMDB  " & note.ToString("0.0", Globalization.CultureInfo.CurrentCulture) & " / 10", note)
            ElseIf texte.Contains("introuvable") Then
                MontrerLaNote("", 0)
            Else
                MontrerLaNote("", 0)
            End If
        Catch ex As Exception
            DiagLog("Note IMDB (libelle) : " & ex.GetType().Name & " : " & ex.Message)
        End Try
    End Sub

    Private Sub MontrerLaNote(texte As String, note As Double)
        LabelNoteImdb.Text = texte
        If note > 0 AndAlso note + RechercheNflix.EcartImdb > RechercheNflix.NoteExcellente Then
            LabelNoteImdb.BackColor = Color.FromArgb(200, 40, 120)
            LabelNoteImdb.ForeColor = Color.White
        ElseIf note > 0 AndAlso note + RechercheNflix.EcartImdb >= RechercheNflix.NoteTresBonne Then
            LabelNoteImdb.BackColor = Color.FromArgb(0, 110, 50)
            LabelNoteImdb.ForeColor = Color.White
        ElseIf note > 0 AndAlso note + RechercheNflix.EcartImdb >= RechercheNflix.NoteBonne Then
            LabelNoteImdb.BackColor = Color.FromArgb(221, 208, 247)
            LabelNoteImdb.ForeColor = Color.FromArgb(50, 20, 100)
        Else
            LabelNoteImdb.BackColor = SystemColors.Control
            LabelNoteImdb.ForeColor = SystemColors.ControlText
        End If
    End Sub

    ''' <summary>Bouton "Plus d'infos" a cote du resume (19/10/2026) : la fiche complete du film ou de la serie choisi dans une fenetre
    ''' (voir FormInfoFilm). Une chaine en direct n'a pas de fiche.</summary>
    Private Async Sub ButtonPlusInfos_Click(sender As Object, e As EventArgs) Handles ButtonPlusInfos.Click
        Try
            Dim ligne As Integer = If(DataGridView2.CurrentCell Is Nothing, -1, DataGridView2.CurrentCell.RowIndex)
            If ligne < 0 OrElse ligne >= DataGridView2.Rows.Count Then
                MessageBox.Show(Me, "Choisissez d'abord un film ou une série dans la liste.", "Plus d'infos", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
            Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
            Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
            Dim cle As String = ""
            If serie IsNot Nothing Then
                cle = "serie:" & serie.Numero
            ElseIf adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
                cle = "serie:" & adresse.Substring(IptvSources.PrefixeSerie.Length)
            ElseIf IptvSources.NumeroDuFilm(adresse) <> "" Then
                cle = "film:" & IptvSources.NumeroDuFilm(adresse)
            End If
            If cle = "" Then
                MessageBox.Show(Me, "Pas de fiche pour une chaîne en direct.", "Plus d'infos", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim titre As String = Convert.ToString(rangee.Cells(0).Value)
            Dim source As Integer = IptvSources.SelectionIptv
            ButtonPlusInfos.Enabled = False
            Cursor = Cursors.WaitCursor
            Dim fiche As IptvSources.FicheIptv
            Try
                fiche = Await Task.Run(Function() IptvSources.FicheDuTitre(source, cle))
            Finally
                Cursor = DefaultCursor
                ButtonPlusInfos.Enabled = True
            End Try
            If fiche Is Nothing Then
                MessageBox.Show(Me, "La fiche n'a pas pu être chargée (le serveur ne répond pas).", "Plus d'infos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If
            FormInfoFilm.Montrer(Me, titre, fiche)
        Catch ex As Exception
            MessageBox.Show(Me, "Plus d'infos : " & ex.Message, "Plus d'infos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>Demande les episodes de la serie de la ligne au serveur, fait choisir l'un d'eux, et le lit.</summary>
    Private Sub ChoisirUnEpisode(ligne As Integer)
        If ChoisirUnEpisodeSansLire(ligne) Then LireLaLigne(ligne)
    End Sub

    ''' <summary>Demande les episodes de la serie de la ligne au serveur et fait choisir l'un d'eux : la ligne
    ''' devient cet episode. Rend Faux si rien n'est choisi.</summary>
    Private Function ChoisirUnEpisodeSansLire(ligne As Integer) As Boolean
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
        If adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
            serie = New SerieChoisie With {.Numero = adresse.Substring(IptvSources.PrefixeSerie.Length),
                                           .NomDeLaSerie = Convert.ToString(rangee.Cells(0).Value)}
            rangee.Tag = serie
        End If
        If serie Is Nothing Then Return False
        VlcControl1.Stop()
        Dim erreur As String = ""
        Dim episodes As List(Of IptvSources.EpisodeIptv)
        Cursor = Cursors.WaitCursor
        Try
            episodes = IptvSources.EpisodesDeLaSerie(IptvSources.SelectionIptv, serie.Numero, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If episodes.Count = 0 Then
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Using fenetre As New FormEpisodes
            fenetre.Initialiser(serie.NomDeLaSerie, episodes, adresse)
            If fenetre.ShowDialog(Me) <> DialogResult.OK Then Return False
            rangee.Cells(0).Value = serie.NomDeLaSerie & " " & fenetre.Choix.Code()
            rangee.Cells(1).Value = fenetre.Choix.AdresseEpisode
        End Using
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Bouton "Telecharger" (demande de Jacob, 13/10/2026 : "rajoute un bouton pour telecharger un film
    ' rapidement", puis "rajoute la possibilite de telecharger plusieurs film en simultane") : les films
    ' choisis dans le tableau (Ctrl ou Maj + clic pour en prendre plusieurs) sont mis dans la file des
    ' telechargements (Telechargements) : 3 en meme temps, les autres attendent. Une serie : on choisit
    ' d'abord ses episodes (plusieurs possibles). Un seul film : fenetre "Enregistrer sous" (dossier et
    ' nom) ; plusieurs : on choisit le dossier. La fenetre "Telechargements" montre l'avancement ; un
    ' telechargement interrompu est repris la fois suivante (IptvSources.TelechargerFichier).
    ' ------------------------------------------------------------------------------------------

    Private Const TexteDuTelechargement As String = "Télécharger : les films choisis (Ctrl ou Maj + clic pour plusieurs) sont copiés dans le dossier de votre choix."

    ''' <summary>Les lignes choisies dans le tableau, de haut en bas ; a defaut la ligne courante.</summary>
    Private Function LignesChoisies() As List(Of Integer)
        Dim l As New List(Of Integer)
        For Each r As DataGridViewRow In DataGridView2.SelectedRows
            l.Add(r.Index)
        Next
        If l.Count = 0 AndAlso DataGridView2.CurrentCell IsNot Nothing Then l.Add(DataGridView2.CurrentCell.RowIndex)
        l.Sort()
        Return l
    End Function

    Private Class ATelecharger
        Public Titre As String
        Public Adresse As String
    End Class

    ''' <summary>Demande les episodes d'une serie et en fait choisir un ou plusieurs (sans les lire). Liste vide si rien n'est choisi.</summary>
    Private Function ChoisirDesEpisodes(ligne As Integer, ByRef nomDeLaSerie As String) As List(Of IptvSources.EpisodeIptv)
        nomDeLaSerie = ""
        Dim vide As New List(Of IptvSources.EpisodeIptv)
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
        If adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
            serie = New SerieChoisie With {.Numero = adresse.Substring(IptvSources.PrefixeSerie.Length),
                                           .NomDeLaSerie = Convert.ToString(rangee.Cells(0).Value)}
            rangee.Tag = serie
        End If
        If serie Is Nothing Then Return vide
        nomDeLaSerie = serie.NomDeLaSerie
        VlcControl1.Stop()
        Dim erreur As String = ""
        Dim episodes As List(Of IptvSources.EpisodeIptv)
        Cursor = Cursors.WaitCursor
        Try
            episodes = IptvSources.EpisodesDeLaSerie(IptvSources.SelectionIptv, serie.Numero, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If episodes.Count = 0 Then
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return vide
        End If
        Using fenetre As New FormEpisodes
            fenetre.Initialiser(serie.NomDeLaSerie, episodes, adresse, True)
            If fenetre.ShowDialog(Me) <> DialogResult.OK Then Return vide
            Return New List(Of IptvSources.EpisodeIptv)(fenetre.Choisis)
        End Using
    End Function

    Private Sub ButtonTelecharger_Click(sender As Object, e As EventArgs) Handles ButtonTelecharger.Click
        If DataGridView2.Rows.Count = 0 Then
            MessageBox.Show(Me, "Choisissez d'abord un ou plusieurs films dans la liste.", "Télécharger", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim aTelecharger As New List(Of ATelecharger)
        Dim ignorees As Integer = 0
        For Each ligne As Integer In LignesChoisies()
            If EstUneSerie(ligne) Then
                Dim nomSerie As String = ""
                For Each ep As IptvSources.EpisodeIptv In ChoisirDesEpisodes(ligne, nomSerie)
                    aTelecharger.Add(New ATelecharger With {.Titre = nomSerie & " " & ep.Code(), .Adresse = ep.AdresseEpisode})
                Next
            Else
                Dim adresse As String = Convert.ToString(DataGridView2.Rows(ligne).Cells(1).Value)
                If IptvSources.EstTelechargeable(adresse) Then
                    aTelecharger.Add(New ATelecharger With {.Titre = Convert.ToString(DataGridView2.Rows(ligne).Cells(0).Value), .Adresse = adresse})
                Else
                    ignorees += 1
                End If
            End If
        Next
        If aTelecharger.Count = 0 Then
            If ignorees > 0 Then
                MessageBox.Show(Me, If(ignorees > 1, "Ces lignes sont des chaînes en direct : elles ne finissent jamais.", "Cette ligne est une chaîne en direct : elle ne finit jamais.") & vbCrLf &
                                "Seuls les films et les épisodes de séries se téléchargent.", "Télécharger", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
            Exit Sub
        End If

        ' Ou ? Un seul film : dossier ET nom ; plusieurs : le dossier (les noms viennent des titres).
        Dim dossier As String = My.Settings.Nflix_DossierTelechargement
        If String.IsNullOrWhiteSpace(dossier) OrElse Not Directory.Exists(dossier) Then
            dossier = IptvSources.DossierDesTelechargements()
            Try
                Directory.CreateDirectory(dossier)
            Catch ex As Exception
                dossier = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            End Try
        End If
        Dim cibleUnique As String = ""
        If aTelecharger.Count = 1 Then
            Dim extension As String = IptvSources.ExtensionDeLAdresse(aTelecharger(0).Adresse)
            If extension = "" Then extension = ".mp4"
            Using choix As New SaveFileDialog
                choix.Title = "Télécharger : choisissez le dossier et le nom du fichier"
                choix.InitialDirectory = dossier
                choix.FileName = IptvSources.NomDeFichier(aTelecharger(0).Titre) & extension
                choix.DefaultExt = extension.TrimStart("."c)
                choix.AddExtension = True
                choix.Filter = "Vidéo (*" & extension & ")|*" & extension & "|Tous les fichiers (*.*)|*.*"
                choix.OverwritePrompt = True
                If choix.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                cibleUnique = choix.FileName
            End Using
            dossier = Path.GetDirectoryName(cibleUnique)
        Else
            Using choix As New FolderBrowserDialog
                choix.Description = "Dossier des " & aTelecharger.Count & " téléchargements"
                choix.UseDescriptionForTitle = True
                choix.SelectedPath = dossier
                If choix.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                dossier = choix.SelectedPath
            End Using
        End If
        My.Settings.Nflix_DossierTelechargement = dossier
        Try
            My.Settings.Save()
        Catch ex As Exception
        End Try

        VlcControl1.Stop()                                    ' (l'apercu et les telechargements se partagent le compte)
        Dim ajoutes As Integer = 0, dejaLa As Integer = 0
        Dim pris As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each a As ATelecharger In aTelecharger
            Dim cible As String = cibleUnique
            If cible = "" Then
                Dim extension As String = IptvSources.ExtensionDeLAdresse(a.Adresse)
                If extension = "" Then extension = ".mp4"
                Dim nom As String = IptvSources.NomDeFichier(a.Titre)
                cible = Path.Combine(dossier, nom & extension)
                Dim n As Integer = 2
                Do While pris.Contains(cible)                 ' deux titres de meme nom dans la meme tournee
                    cible = Path.Combine(dossier, nom & " (" & n & ")" & extension)
                    n += 1
                Loop
                If File.Exists(cible) Then
                    dejaLa += 1                                ' deja telecharge : on ne refait pas
                    Continue For
                End If
            End If
            pris.Add(cible)
            If Telechargements.Ajouter(a.Titre, a.Adresse, cible) IsNot Nothing Then ajoutes += 1
        Next
        Dim notes As New List(Of String)
        If dejaLa > 0 Then notes.Add(dejaLa & If(dejaLa > 1, " déjà téléchargés (ignorés)", " déjà téléchargé (ignoré)"))
        If ignorees > 0 Then notes.Add(ignorees & If(ignorees > 1, " chaînes en direct ignorées", " chaîne en direct ignorée"))
        If notes.Count > 0 Then LabelTelechargement.Text = ajoutes & " ajouté(s) à la file ; " & String.Join(" ; ", notes) & "."
        If ajoutes > 0 Then FormTelechargements.Montrer(Me)
    End Sub

    Private Sub ButtonTelechargements_Click(sender As Object, e As EventArgs) Handles ButtonTelechargements.Click
        FormTelechargements.Montrer(Me)
    End Sub

    ''' <summary>La file des telechargements a change : un resume sous le pitch, et la barre d'avancement de l'ensemble.</summary>
    Private Sub Form14_TelechargementsChange()
        If IsDisposed Then Exit Sub
        Dim bilan As String = Telechargements.Bilan()
        LabelTelechargement.Text = If(bilan = "", TexteDuTelechargement, "Téléchargements : " & bilan)
        ButtonTelechargements.Text = If(Telechargements.Actifs() + Telechargements.EnAttente() > 0,
                                        "Téléchargements (" & (Telechargements.Actifs() + Telechargements.EnAttente()) & ")", "Téléchargements")
        Dim avancement As Integer = Telechargements.Avancement()
        If Telechargements.Actifs() = 0 Then
            ProgressBarTelechargement.Visible = False
        ElseIf avancement < 0 Then
            ProgressBarTelechargement.Style = ProgressBarStyle.Marquee
            ProgressBarTelechargement.Visible = True
        Else
            ProgressBarTelechargement.Style = ProgressBarStyle.Continuous
            ProgressBarTelechargement.Value = avancement
            ProgressBarTelechargement.Visible = True
        End If
    End Sub

    Private Sub Form14_SuivreLesTelechargements(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler Telechargements.Change, AddressOf Form14_TelechargementsChange
        AddHandler Me.Disposed, Sub() RemoveHandler Telechargements.Change, AddressOf Form14_TelechargementsChange
        Form14_TelechargementsChange()
    End Sub

    ''' <summary>Fenetre fermee ou cachee : la liste gardee en memoire pour la recherche est liberee (voir
    ''' RechercheNflix). Les telechargements en cours continuent.</summary>
    Private Sub Form14_Liberer(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then Exit Sub
        RechercheNflix.Oublier()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Plusieurs IPTV (voir IptvSources) : liste "Source" en haut de la fenetre, visible des qu'il y a
    ' au moins deux IPTV avec une adresse.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>La fenetre s'ouvre au centre de l'ecran (celui des reglages s'ils sont ouverts), a chaque
    ''' affichage : cette fenetre est reutilisee, et rouverte elle reprenait sa derniere place ou celle
    ''' laissee par un autre ecran (demande de Jacob, 13/10/2026 : "centre la fenetre Nflix").</summary>
    Private Sub Form14_Centrer(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible OrElse Me.WindowState <> FormWindowState.Normal Then Exit Sub
        Try
            Dim reglages As Form = My.Application.OpenForms.OfType(Of Form4)().FirstOrDefault()
            Dim ecran As Screen = If(reglages IsNot Nothing AndAlso reglages.Visible, Screen.FromControl(reglages), Screen.FromControl(Me))
            Dim zone As Rectangle = ecran.WorkingArea
            Me.Location = New Point(zone.Left + Math.Max(0, (zone.Width - Me.Width) \ 2),
                                    zone.Top + Math.Max(0, (zone.Height - Me.Height) \ 2))
        Catch ex As Exception
        End Try
    End Sub

    Private Class ElementSource
        Public Index As Integer
        Public Libelle As String
        Public Overrides Function ToString() As String
            Return Libelle
        End Function
    End Class

    Private _remplissageSources As Boolean

    Private Sub Form14_SourcesIptv(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible Then Exit Sub
        _remplissageSources = True
        Try
            ComboBoxSource.Items.Clear()
            Dim choix As Integer = -1
            For i As Integer = 0 To IptvSources.NombreIptv - 1
                If IptvSources.UrlIptv(i) <> "" OrElse i = IptvSources.SelectionIptv Then
                    ComboBoxSource.Items.Add(New ElementSource With {.Index = i, .Libelle = IptvSources.NomIptv(i)})
                    If i = IptvSources.SelectionIptv Then choix = ComboBoxSource.Items.Count - 1
                End If
            Next
            If choix >= 0 Then ComboBoxSource.SelectedIndex = choix
        Finally
            _remplissageSources = False
        End Try
        Dim plusieurs As Boolean = ComboBoxSource.Items.Count > 1
        ComboBoxSource.Visible = plusieurs
        LabelSourceIptv.Visible = plusieurs
    End Sub

    Private Sub ComboBoxSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxSource.SelectedIndexChanged
        If _remplissageSources Then Exit Sub
        Dim element As ElementSource = TryCast(ComboBoxSource.SelectedItem, ElementSource)
        If element Is Nothing OrElse element.Index = IptvSources.SelectionIptv Then Exit Sub
        Dim erreur As String = ""
        Cursor = Cursors.WaitCursor
        Dim ok As Boolean
        Try
            VlcControl1.Stop()
            ok = IptvSources.Activer(element.Index, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If ok Then
            DataGridView2.Rows.Clear()
            TextBox1.Text = ""
            IptvSources.GenreAffiche = IptvSources.GenreIptv.Chaines      ' (les films de la source quittee ne valent plus)
            RemplirLeContenu()
            rechercher()                                  ' les groupes de la nouvelle source
            PositionnerSurLeDernierAjout()
        Else
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _remplissageSources = True
            For k As Integer = 0 To ComboBoxSource.Items.Count - 1
                If DirectCast(ComboBoxSource.Items(k), ElementSource).Index = IptvSources.SelectionIptv Then ComboBoxSource.SelectedIndex = k
            Next
            _remplissageSources = False
        End If
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form14_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)          ' la molette agit sur la liste sous le pointeur
    End Sub

    ''' <summary>Apercu ouvert depuis les reglages : le son reprend (voir SilenceDesReglages.ApercuOuvert).</summary>
    Private Sub Form14_SonDeLApercu(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then SilenceDesReglages.ApercuOuvert(Me, VlcControl1, Function() TrackBar2.Value)
    End Sub


End Class
