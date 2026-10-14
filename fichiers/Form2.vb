Imports Vlc.DotNet.Forms
Imports System.IO

Public Class Form2

    Private Sub DataGridView2_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentClick
        Me.PictureBox1.Visible = False
        Me.VlcControl1.Visible = True
        Me.Button1.Visible = False
        Me.Button2.Visible = True
        L = Me.DataGridView2.CurrentRow.Index
        nom = Me.DataGridView2.Rows(L).Cells(3).Value
        Form3.VlcControl1.Stop()
        ext = "file:///"
        Me.VlcControl1.SetMedia(New Uri(ext & nom))
        CouperLeSonAvantDemarrage(Me.VlcControl1)
        Me.VlcControl1.Play()
        SetVolume(100, Me.VlcControl1)
        Me.HScrollBar2.Value = L
        'Form13.Show()
        'Form13.Top = Me.Button2.Top - Form13.ClientSize.Height
        'Form13.Left = Me.Button2.Left
        'Form13.BringToFront()
    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick
        C = e.ColumnIndex
        If Me.DataGridView1.CurrentRow IsNot Nothing Then AfficherLaPhoto(Me.DataGridView1.CurrentRow.Index)
    End Sub

    ''' <summary>Charge la photo SANS garder le fichier ouvert (Image.FromFile le verrouille tant qu'elle est affichee : « Le processus ne peut pas acceder au fichier » quand on changeait sa date EXIF, 27/10/2026)
    ''' et libere la precedente.</summary>
    Private Sub PoserLImage(chemin As String)
        Dim nouvelle As Image
        Using flux As New FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            Using lue As Image = Image.FromStream(flux)
                nouvelle = New Bitmap(lue)
            End Using
        End Using
        Dim ancienne As Image = Me.PictureBox1.Image
        Me.PictureBox1.Image = nouvelle
        If ancienne IsNot Nothing Then ancienne.Dispose()
    End Sub

    ''' <summary>Montre la photo de la ligne <paramref name="ligne"/> de la liste des photos (clic sur la ligne, ou fleches du clavier : 27/10/2026).</summary>
    Private Sub AfficherLaPhoto(ligne As Integer)
        If ligne < 0 OrElse ligne >= Me.DataGridView1.Rows.Count Then Exit Sub

        Me.Button1.Visible = True
        Me.Button2.Visible = False

        Me.PictureBox1.Visible = True
        Me.VlcControl1.Visible = False

        Me.VlcControl1.Pause()

        L = ligne

        nom = Me.DataGridView1.Rows(L).Cells(3).Value

        Try
            PoserLImage(CStr(nom))
            Me.PictureBox1.Refresh()
        Catch ex As Exception
            'MsgBox(ex.Message)

            Dim myGraphics As Graphics = Me.PictureBox1.CreateGraphics
            myGraphics.FillRectangle(Brushes.Gray, 0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height)
            myGraphics.DrawString(ex.Message, New Font("Arial", 12), Brushes.Black, 0, 0)


        End Try

        Try
            Me.HScrollBar1.Value = Math.Max(Me.HScrollBar1.Minimum, Math.Min(Me.HScrollBar1.Maximum, L))
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Changer de ligne avec les fleches (ou Page precedente / suivante, Debut, Fin) affiche la photo de la nouvelle ligne.</summary>
    Private Sub DataGridView1_KeyUp(sender As Object, e As KeyEventArgs) Handles DataGridView1.KeyUp
        Select Case e.KeyCode
            Case Keys.Up, Keys.Down, Keys.PageUp, Keys.PageDown, Keys.Home, Keys.End
                If Me.DataGridView1.CurrentRow IsNot Nothing Then AfficherLaPhoto(Me.DataGridView1.CurrentRow.Index)
        End Select
    End Sub
    Private Sub Form2_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Hide()
    End Sub
    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.KeyPreview = True
        Me.Width = DataGridView2.Left + DataGridView2.Width + 20


    End Sub
    Protected Overrides Sub OnFormClosing(ByVal e As System.Windows.Forms.FormClosingEventArgs)
        Me.Hide()
        Me.VlcControl1.Stop()
        e.Cancel = True
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub HScrollBar1_Scroll(sender As Object, e As ScrollEventArgs) Handles HScrollBar1.Scroll

        Me.PictureBox1.Visible = True
        Me.Button1.Visible = True
        Me.Button2.Visible = False
        Me.VlcControl1.Visible = False

        L = Me.HScrollBar1.Value
        Me.VlcControl1.Stop()
        nom = Me.DataGridView1.Rows(L).Cells(3).Value

        Try
            PoserLImage(CStr(nom))
        Catch ex As Exception
            Dim myGraphics As Graphics = Me.PictureBox1.CreateGraphics
            myGraphics.FillRectangle(Brushes.Gray, 0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height)
            myGraphics.DrawString(ex.Message, New Font("Arial", 12), Brushes.Black, 0, 0)
        End Try

        Me.PictureBox1.Refresh()
        Me.DataGridView1.CurrentCell = Me.DataGridView1.Item(1, Me.HScrollBar1.Value)
    End Sub

    Private Sub HScrollBar2_Scroll(sender As Object, e As ScrollEventArgs) Handles HScrollBar2.Scroll

        Me.PictureBox1.Visible = False
        Me.VlcControl1.Visible = True
        Me.Button1.Visible = False
        Me.Button2.Visible = True
        L = Me.HScrollBar2.Value
        nom = Me.DataGridView2.Rows(L).Cells(3).Value

        ext = "file:///"
        Me.VlcControl1.SetMedia(New Uri(ext & nom))
        CouperLeSonAvantDemarrage(Me.VlcControl1)
        Me.VlcControl1.Play()
        Me.DataGridView2.CurrentCell = Me.DataGridView2.Item(1, Me.HScrollBar2.Value)
        SetVolume(100, Me.VlcControl1)          ' volume voulu, et son retabli

    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs)

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        L = Me.DataGridView1.CurrentRow.Index
        NumPhoto = L
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        L = Me.DataGridView2.CurrentRow.Index
        NumVideo = L
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        NumPhoto = 0
        CaseAcocherVide(1)


    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        NumVideo = 0
        CaseAcocherVide(2)

    End Sub


    Private Sub Button5_Click(sender As Object, e As EventArgs)
        Dim colType As Type = Me.DataGridView1.Columns(0).GetType
        'MessageBox.Show(colType.ToString)
        MessageBox.Show(Me.DataGridView1.Columns(0).GetType.ToString)
        'Debug.Print(Me.DataGridView1.Columns(0).GetType.ToString)
    End Sub

    Private Sub DataGridView1_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridView1.ColumnHeaderMouseClick
        NumPhoto = 0
        CaseAcocherVide(1)
    End Sub
    Private Sub DataGridView2_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridView2.ColumnHeaderMouseClick
        NumVideo = 0
        CaseAcocherVide(2)
    End Sub

    Private Sub Button5_Click_1(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone)
        PictureBox1.Refresh()
    End Sub



    Private Sub Button6_Click(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone)
        PictureBox1.Refresh()
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone)
        PictureBox1.Refresh()
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        If NumPhoto > 1 Then Me.DataGridView1.FirstDisplayedScrollingRowIndex = NumPhoto

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        If NumVideo > 1 Then Me.DataGridView2.FirstDisplayedScrollingRowIndex = NumVideo
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs)

    End Sub


    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl1.VlcLibDirectoryNeeded
        e.VlcLibDirectory = New DirectoryInfo(VLCpath)
        OptionsDuLecteurVlc(sender)

        'Dim aP As String
        'aP = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "VideoLAN\VLC")

        'If Not File.Exists(Path.Combine(aP, "libvlc.dll")) Then
        'e.VlcLibDirectory = New DirectoryInfo(AppPath & "\data\VLC")
        'Else
        'e.VlcLibDirectory = New DirectoryInfo(aP)
        'End If

    End Sub

    Private Sub Button5_Click_2(sender As Object, e As EventArgs)
        SaveRestoreLeSlide(1)
    End Sub

    Private Sub Button6_Click_1(sender As Object, e As EventArgs)
        SaveRestoreLeSlide(2)
    End Sub

    Private Sub Button5_Click_3(sender As Object, e As EventArgs) Handles Button5.Click
        NombreDePhotoChecked = 0
        NombreDeVideoChecked = 0
        msg = "Stat non disponible"

        nb = DataGridView1.RowCount - 1
        For a = 0 To nb
            If DataGridView1.Rows(a).Cells(0).Value <> 0 Then
                NombreDePhotoChecked += 1
            End If
        Next

        If (nb) > 0 Then msg = "Photo deja diffusés = " & NombreDePhotoChecked & "/" & nb & "  ( " & Format(NombreDePhotoChecked / nb, "0 %") & " )" & vbCrLf

        nb2 = DataGridView2.RowCount - 1
        For a = 0 To nb2

            If DataGridView2.Rows(a).Cells(0).Value <> 0 Then
                NombreDeVideoChecked += 1
            End If
        Next

        If (nb2) > 0 Then msg += "video deja diffusés = " & NombreDeVideoChecked & "/" & nb2 & "  ( " & Format(NombreDeVideoChecked / nb2, "0 %") & " )"

        MsgBox(msg)
    End Sub

    Private Sub Button6_Click_2(sender As Object, e As EventArgs) Handles Button6.Click
        Call ChangeDatebyExifNow
    End Sub

    Private Sub DataGridView1_CellContentDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentDoubleClick

    End Sub





    ''' <summary>
    ''' Clic sur la colonne Exif d'une liste (27/10/2026, refait : le clic droit ne faisait rien, l'erreur etait avalee en silence) :
    '''  - clic DROIT : saisir la date de prise de vue de la photo ou de la video (la date proposee vient du dossier « aaaa mm » ; une saisie vide efface la date) ; marche toujours ;
    '''  - clic GAUCHE : pose la date proposee par le nom du dossier, seulement apres « Modifier la date par Exif date » (ModeModifExif), pour ne rien changer en parcourant la liste.
    ''' </summary>
    Private Sub ClicSurLaDate(grille As DataGridView, e As MouseEventArgs)
        Try
            Dim touche As DataGridView.HitTestInfo = grille.HitTest(e.X, e.Y)
            If touche.Type <> DataGridViewHitTestType.Cell OrElse touche.ColumnIndex <> 4 OrElse touche.RowIndex < 0 Then Exit Sub
            If e.Button <> MouseButtons.Right AndAlso Not (e.Button = MouseButtons.Left AndAlso ModeModifExif) Then Exit Sub
            Dim ligne As DataGridViewRow = grille.Rows(touche.RowIndex)

            ' Date proposee : le milieu du mois du dossier "aaaa mm" ; sinon la date deja la.
            Dim proposee As String = ""
            Dim chemin As String = Convert.ToString(ligne.Cells(3).Value)
            Try
                Dim dossier As String = IO.Path.GetFileName(IO.Path.GetDirectoryName(chemin))
                Dim annee As Integer, mois As Integer
                If dossier IsNot Nothing AndAlso dossier.Length >= 7 AndAlso Integer.TryParse(dossier.Substring(0, 4), annee) AndAlso Integer.TryParse(dossier.Substring(dossier.Length - 2), mois) AndAlso
                   annee >= 1990 AndAlso mois >= 1 AndAlso mois <= 12 Then proposee = New DateTime(annee, mois, 15, 12, 0, 0).ToString()
            Catch ex As Exception
            End Try
            ' Date proposee, de la plus fiable a la moins fiable, avec son HEURE : celle de la liste, puis celle ecrite dans la photo, puis la date du fichier, puis le milieu du mois du dossier.
            Dim dejaUneDate As Date
            If Date.TryParse(Convert.ToString(ligne.Cells(4).Value), dejaUneDate) Then
                proposee = dejaUneDate.ToString()
            Else
                Dim exifDuFichier As String = DateDePriseDeVue(chemin)
                If exifDuFichier IsNot Nothing AndAlso Date.TryParse(exifDuFichier, dejaUneDate) Then
                    proposee = dejaUneDate.ToString()
                ElseIf Date.TryParse(Convert.ToString(ligne.Cells(2).Value), dejaUneDate) AndAlso dejaUneDate.Year >= 1990 Then
                    proposee = dejaUneDate.ToString()
                End If
            End If

            If e.Button = MouseButtons.Right Then
                Dim saisie As String = InputBox("Date de prise de vue (jj/mm/aaaa hh:mm:ss) :" & vbCrLf & "« - » = effacer la date (Annuler ou vide : rien ne change)." & vbCrLf & IO.Path.GetFileName(chemin), "Changer la date", proposee)
                Dim nouvelle As Date
                If Date.TryParse(saisie, nouvelle) Then
                    ligne.Cells(4).Value = nouvelle.ToString()
                    EcrireLaDateDansLeFichier(grille, ligne, chemin, nouvelle)
                ElseIf saisie.Trim() = "" Then
                    ' Annuler (ou saisie vide) : InputBox rend "" dans les deux cas, rien ne doit changer.
                ElseIf saisie.Trim() = "-" Then
                    ligne.Cells(4).Value = ""
                Else
                    DiagLog("Form2 : date non reconnue « " & saisie & " » (" & chemin & ")")
                End If
            ElseIf proposee <> "" Then
                ligne.Cells(4).Value = proposee
            End If
            grille.InvalidateRow(touche.RowIndex)
        Catch ex As Exception
            DiagLog("Form2 : changement de date impossible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La date choisie remplace la mauvaise dans le fichier lui-meme (balises EXIF d'un JPEG, sur place, sans recompression) ; le fichier, la liste et son tri prennent aussi cette date.
    ''' L'ancienne date est gardee dans le journal. Les fichiers qui ne sont pas des JPEG (PNG, HEIC, videos) gardent leurs metadonnees : seules la liste et la date du fichier changent.</summary>
    Private Sub EcrireLaDateDansLeFichier(grille As DataGridView, ligne As DataGridViewRow, chemin As String, nouvelle As Date)
        Dim compte As String
        Try
            If Not IO.File.Exists(chemin) Then
                MessageBox.Show(Me, "Fichier introuvable :" & vbCrLf & chemin, "Changer la date", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If
            Dim ancienne As String = DateDePriseDeVue(chemin)
            Dim balises As Integer = 0
            If ExifFichier.EstUnJpeg(chemin) Then balises = ExifFichier.EcrireLaDate(chemin, nouvelle)
            Try
                IO.File.SetLastWriteTime(chemin, nouvelle)
                IO.File.SetCreationTime(chemin, nouvelle)
            Catch ex As Exception
                DiagLog("Form2 : date du fichier non changee (" & ex.Message & ")")
            End Try
            Try
                ligne.Cells(2).Value = nouvelle          ' (colonne « date » de la liste : l'ordre du diaporama par date suit)
            Catch ex As Exception
            End Try
            DiagLog("Form2 : date de " & chemin & " : " & If(ancienne, "(aucune)") & " -> " & nouvelle.ToString() & " ; " & balises & " balise(s) EXIF remplacee(s)")
            RangerDansLeBonDossier(ligne, chemin, nouvelle)
            If ExifFichier.EstUnJpeg(chemin) Then
                compte = If(balises > 0, "", "Ce JPEG n'a pas de balise de date EXIF : sa date de fichier et la liste ont changé, pas ses métadonnées.")
            Else
                compte = "Ce fichier n'est pas un JPEG : sa date de fichier et la liste ont changé, pas ses métadonnées."
            End If
        Catch ex As Exception
            DiagLog("Form2 : date non ecrite dans " & chemin & " : " & ex.Message)
            compte = "La date n'a pas pu être écrite dans le fichier :" & vbCrLf & ex.Message
        End Try
        If compte <> "" Then MessageBox.Show(Me, compte, "Changer la date", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>Apres un changement de date (27/10/2026, Jacob : le fichier doit aller dans le bon dossier, sans cible en dur) : le rangement est deduit du dossier actuel de la photo.
    '''  - dossier « AAAA MM » (separateur espace, _ ou -) : la photo va dans « AAAA' MM' » a cote, sous le meme dossier d'annee « AAAA' » s'il existe (sinon directement a cote du dossier du mois) ;
    '''  - dossier « AAAA » : la photo va dans le dossier de la nouvelle annee, a cote ;
    '''  - autre rangement : question a l'utilisateur (choisir le dossier de destination, ou laisser la photo ou elle est).
    ''' Les dossiers manquants sont crees ; un fichier du meme nom deja present n'est jamais ecrase. La liste (colonne du chemin) suit le fichier.</summary>
    Private Sub RangerDansLeBonDossier(ligne As DataGridViewRow, chemin As String, nouvelle As Date)
        Try
            Dim dossier As String = IO.Path.GetDirectoryName(chemin)
            Dim nomDossier As String = IO.Path.GetFileName(dossier)
            Dim cible As String = Nothing
            Dim mois As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(nomDossier, "^(\d{4})([ _\-])(\d{2})$")
            If mois.Success Then
                Dim parent As String = IO.Path.GetDirectoryName(dossier)
                Dim nomMois As String = nouvelle.Year.ToString("0000") & mois.Groups(2).Value & nouvelle.Month.ToString("00")
                If System.Text.RegularExpressions.Regex.IsMatch(IO.Path.GetFileName(parent), "^\d{4}$") Then
                    cible = IO.Path.Combine(IO.Path.GetDirectoryName(parent), nouvelle.Year.ToString("0000"), nomMois)
                Else
                    cible = IO.Path.Combine(parent, nomMois)
                End If
            ElseIf System.Text.RegularExpressions.Regex.IsMatch(nomDossier, "^\d{4}$") Then
                cible = IO.Path.Combine(IO.Path.GetDirectoryName(dossier), nouvelle.Year.ToString("0000"))
            End If
            If cible Is Nothing Then
                If MessageBox.Show(Me, "Je ne reconnais pas le rangement du dossier :" & vbCrLf & dossier & vbCrLf & vbCrLf & "(attendu : « AAAA MM » ou « AAAA »)." & vbCrLf & vbCrLf &
                                   "Voulez-vous choisir le dossier où ranger cette photo (date " & nouvelle.ToString("dd/MM/yyyy") & ") ?", "Changer la date", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
                Using choix As New FolderBrowserDialog With {.Description = "Dossier de destination de " & IO.Path.GetFileName(chemin), .SelectedPath = dossier}
                    If choix.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                    cible = choix.SelectedPath
                End Using
            End If
            If String.Equals(IO.Path.GetFullPath(cible).TrimEnd("\"c), IO.Path.GetFullPath(dossier).TrimEnd("\"c), StringComparison.OrdinalIgnoreCase) Then Exit Sub          ' (deja au bon endroit)
            Dim destination As String = IO.Path.Combine(cible, IO.Path.GetFileName(chemin))
            If IO.File.Exists(destination) Then
                MessageBox.Show(Me, "Un fichier de même nom existe déjà dans :" & vbCrLf & cible & vbCrLf & vbCrLf & "La photo reste dans " & dossier, "Changer la date", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If
            If Not IO.Directory.Exists(cible) Then IO.Directory.CreateDirectory(cible)
            IO.File.Move(chemin, destination)
            ligne.Cells(3).Value = destination
            DiagLog("Form2 : photo deplacee " & chemin & " -> " & destination)
        Catch ex As Exception
            DiagLog("Form2 : deplacement impossible de " & chemin & " : " & ex.Message)
            MessageBox.Show(Me, "La photo n'a pas pu être déplacée dans le bon dossier :" & vbCrLf & ex.Message, "Changer la date", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub DataGridView1_MouseDown(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseDown
        ClicSurLaDate(DataGridView1, e)
    End Sub

    Private Sub DataGridView2_MouseDown(sender As Object, e As MouseEventArgs) Handles DataGridView2.MouseDown
        ClicSurLaDate(DataGridView2, e)
    End Sub

    Private Sub HScrollBar1_StyleChanged(sender As Object, e As EventArgs) Handles HScrollBar1.StyleChanged

    End Sub


    Private Sub Button7_Click_1(sender As Object, e As EventArgs)

    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form2_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
