Imports System.Globalization
Imports System.Threading.Tasks
Imports Vlc.DotNet.Forms

''' <summary>
''' Clic droit > « Scanner la TNT… » (23/11/2026) : balaie les canaux de la TNT avec la clé, garde les chaînes avec leur numéro officiel (LCN) et enregistre la liste sous un nom de ville
''' (data\tnt-NOM.m3u, lu par frequenceTV). On peut aussi ouvrir une liste existante, changer l'ordre (Monter / Descendre, ou taper un autre numéro), renommer les chaînes et la liste,
''' et dire si c'est cette liste que la TV de Slide utilise. Les contrôles sont dans le Concepteur ; la logique du scan est dans TntScan.
''' </summary>
Public Class FormTnt

    Private _chaines As New List(Of ChaineTnt)
    Private _nomCharge As String = ""
    Private _enCours As Boolean
    Private _annule As Boolean

    ''' <summary>Ouvre la fenetre (modale).</summary>
    Friend Shared Sub Montrer(proprietaire As IWin32Window)
        Using f As New FormTnt()
            f.ShowDialog(proprietaire)
        End Using
    End Sub

    ''' <summary>Le lecteur du scan est celui de VLC integre a Slide (le dossier data\VLC, comme la TV) : rien a installer sur un autre PC.</summary>
    Private Sub VlcScan_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcScan.VlcLibDirectoryNeeded
        e.VlcLibDirectory = New IO.DirectoryInfo(VLCpath)
        OptionsDuLecteurVlc(sender)
    End Sub

    Private ReadOnly _messagesVlc As New List(Of String)
    Private _journalVlcBranche As Boolean

    ''' <summary>Les trois actions que le scan commande au lecteur integre ; elles s'executent sur le fil de la fenetre (comme la TV), le scan tournant sur un fil de fond.</summary>
    Private Function CreerLePilote() As TntScan.PiloteDeScan
        If Not _journalVlcBranche Then
            _journalVlcBranche = True
            AddHandler VlcScan.Log, Sub(s, ev)
                                        SyncLock _messagesVlc
                                            If _messagesVlc.Count < 400 Then _messagesVlc.Add(ev.Level.ToString() & " : " & ev.Message)
                                        End SyncLock
                                    End Sub
        End If
        Dim pilote As New TntScan.PiloteDeScan()
        pilote.Demarrer = Sub(freq As Long, fichier As String, methode As Integer)
                              SyncLock _messagesVlc
                                  _messagesVlc.Clear()
                              End SyncLock
                              Invoke(Sub()
                                         Dim options As New List(Of String)
                                         options.Add(":dvb-frequency=" & freq.ToString(CultureInfo.InvariantCulture))
                                         If methode = 0 Then
                                             ' Flux BRUT de la TNT ecrit tel quel (demux « dump ») : verifie dans le journal du 23/11/2026 (9.879), la table du reseau (NIT) et les numeros officiels y sont. Pas l'UDP « ts-out » de la 9.883 : rien n'arrivait.
                                             options.AddRange({":no-video", ":no-audio", ":demux=dump", ":demuxdump-file=" & fichier})
                                         Else
                                             options.AddRange({":sout=#std{access=file,mux=ts,dst=""" & fichier & """}", ":sout-all", ":sout-keep"})
                                         End If
                                         VlcScan.SetMedia("dvbt://", options.ToArray())
                                         VlcScan.Play()
                                     End Sub)
                          End Sub
        pilote.Arreter = Sub()
                             If IsDisposed Then Exit Sub
                             Invoke(Sub() VlcScan.Stop())
                         End Sub
        pilote.MessagesDeVlc = Function()
                                   SyncLock _messagesVlc
                                       Return New List(Of String)(_messagesVlc)
                                   End SyncLock
                               End Function
        Return pilote
    End Function

    Private Sub FormTnt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        Try
            Dim h As IntPtr = VlcScan.Handle                     ' (la fenetre interne du lecteur doit exister, meme cachee, pour que VLC ait ou se brancher)
        Catch ex As Exception
            DiagLog("TNT : lecteur du scan (" & ex.Message & ")")
        End Try
        MoletteSousLaSouris.Suivre(Me)
        ' La fenetre ne depasse jamais l'ecran (23/11/2026, « Enregistrer la liste ne marche pas » : sur un ecran plus petit ou tres agrandi, la rangee du bas -
        ' Enregistrer, Fermer - pouvait etre hors de l'ecran donc inatteignable).
        Try
            Dim zone As Rectangle = Screen.FromControl(Me).WorkingArea
            MinimumSize = New Size(Math.Min(MinimumSize.Width, zone.Width), Math.Min(MinimumSize.Height, zone.Height))
            If Height > zone.Height Then Height = zone.Height
            If Width > zone.Width Then Width = zone.Width
            Left = Math.Max(zone.Left, Math.Min(Left, zone.Right - Width))
            Top = Math.Max(zone.Top, Math.Min(Top, zone.Bottom - Height))
        Catch ex As Exception
        End Try
        RemplirLesListes()
        ' La fenetre s'ouvre VIDE (23/11/2026) : aucune chaine affichee, c'est le point de depart d'un nouveau scan. Une liste enregistree s'ouvre a la demande
        ' (la choisir dans la case du haut puis « Ouvrir »).
        ComboListe.Text = ""
        _chaines = New List(Of ChaineTnt)
        _nomCharge = ""
        Afficher()
        LabelEtat.Text = "Cliquez sur « Scanner la TNT », ou choisissez une liste enregistrée dans la case du haut puis cliquez sur « Ouvrir »."
        MettreAJourLesBoutons()
    End Sub

    Private Sub RemplirLesListes()
        Dim texte As String = ComboListe.Text
        ComboListe.Items.Clear()
        For Each n As String In TntScan.ListesExistantes()
            ComboListe.Items.Add(n)
        Next
        ComboListe.Text = texte
    End Sub

    Private Sub MettreAJourLesBoutons()
        Dim libre As Boolean = Not _enCours
        ButtonScanner.Enabled = libre
        ButtonArreter.Enabled = _enCours
        ButtonOuvrir.Enabled = libre
        ButtonRenommer.Enabled = libre AndAlso _nomCharge <> ""
        ButtonMonter.Enabled = libre
        ButtonDescendre.Enabled = libre
        ButtonSupprimer.Enabled = libre
        ButtonNumeroter.Enabled = libre
        ButtonEnregistrer.Enabled = libre
        ButtonReinitCle.Enabled = libre
        ComboListe.Enabled = libre
        GrilleChaines.Enabled = libre
    End Sub

    Private Sub Afficher(Optional selection As ChaineTnt = Nothing, Optional parOfficiel As Boolean = False)
        ' Tri : par numero officiel (colonne « N° officiel ») pendant et apres un scan et apres « Numeroter comme le pays » (23/11/2026, les chaines sans numero officiel a la fin) ;
        ' sinon par numero Slide, que Monter / Descendre modifient.
        If parOfficiel Then
            _chaines.Sort(Function(a, b) If(a.Lcn > 0, a.Lcn, 100000 + a.Numero).CompareTo(If(b.Lcn > 0, b.Lcn, 100000 + b.Numero)))
        Else
            _chaines.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))
        End If
        GrilleChaines.SuspendLayout()
        Try
            GrilleChaines.Rows.Clear()
            For Each c As ChaineTnt In _chaines
                Dim i As Integer = GrilleChaines.Rows.Add(c.Numero.ToString(CultureInfo.InvariantCulture), c.Nom,
                                                         If(c.Lcn > 0, c.Lcn.ToString(CultureInfo.InvariantCulture), ""),
                                                         (c.Frequence / 1000000.0).ToString("0.000", CultureInfo.CurrentCulture) & " MHz",
                                                         c.Programme.ToString(CultureInfo.InvariantCulture))
                GrilleChaines.Rows(i).Tag = c
            Next
            If selection IsNot Nothing Then
                For Each r As DataGridViewRow In GrilleChaines.Rows
                    If r.Tag Is selection Then
                        r.Selected = True
                        GrilleChaines.CurrentCell = r.Cells(1)
                        Exit For
                    End If
                Next
            End If
        Finally
            GrilleChaines.ResumeLayout()
        End Try
    End Sub

    Private Function ChaineChoisie() As ChaineTnt
        If GrilleChaines.CurrentRow Is Nothing Then Return Nothing
        Return TryCast(GrilleChaines.CurrentRow.Tag, ChaineTnt)
    End Function

    Private Sub Ouvrir(nom As String)
        _chaines = TntScan.Lire(nom)
        _listeRemux = False
        _nomCharge = nom
        CheckBoxUtiliser.Checked = String.Equals(TntScan.ListeUtilisee(), nom, StringComparison.OrdinalIgnoreCase)      ' (coche : c'est la liste de la TV ; une liste ouverte pour regarder ne la remplace pas sans cette case)
        Afficher()
        LabelEtat.Text = "Liste « " & nom & " » : " & _chaines.Count & " chaîne(s)."
        MettreAJourLesBoutons()
    End Sub

    Private Sub ButtonOuvrir_Click(sender As Object, e As EventArgs) Handles ButtonOuvrir.Click
        Dim nom As String = ComboListe.Text.Trim()
        If Not TntScan.NomValide(nom) OrElse Not IO.File.Exists(TntScan.FichierDeLaListe(nom)) Then
            MessageBox.Show(Me, "Aucune liste de ce nom." & vbCrLf & "Les listes existantes sont dans le menu déroulant.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Ouvrir(nom)
    End Sub

    Private Sub ComboListe_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ComboListe.SelectionChangeCommitted
        Dim nom As String = Convert.ToString(ComboListe.SelectedItem)
        If nom <> "" AndAlso IO.File.Exists(TntScan.FichierDeLaListe(nom)) Then Ouvrir(nom)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Scan
    ' ------------------------------------------------------------------------------------------

    Private Async Sub ButtonScanner_Click(sender As Object, e As EventArgs) Handles ButtonScanner.Click
        Try
            Dim nomCle As String = "", code As Integer
            If CleTNT.EtatDeLaCleTNT(nomCle, code) = CleTNT.EtatCle.Absente Then
                MessageBox.Show(Me, "Aucune clé TNT n'est branchée sur ce PC : le scan est impossible." & vbCrLf & "Branchez la clé TNT puis recommencez.", "Scanner la TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
        Catch ex As Exception
            DiagLog("TNT scan : verification de la cle (" & ex.Message & ")")
        End Try

        ' Le scan part tout de suite, SANS question (23/11/2026), sur une fenetre VIDE : c'est un nouveau processus, la liste a l'ecran est effacee d'abord (les listes enregistrees ne sont pas touchees),
        ' et le scan cree une NOUVELLE liste sous un nouveau nom de ville (celui de Chabbat, puis le nom du reseau si la TNT le donne ; « II », « III »… si le nom existe deja). Elle se remplit au fur et a mesure.
        _chaines = New List(Of ChaineTnt)
        _nomCharge = ""
        _nomReseauPris = False
        _listeRemux = False
        Afficher()
        ComboListe.Text = TntScan.NomDeListeLibre(My.Settings.Shabbat_Ville)
        _enCours = True
        _annule = False
        TntScan.ScanEnCours = True
        MettreAJourLesBoutons()
        ProgressBarScan.Value = 0
        ProgressBarScan.Maximum = 28
        LabelPourcentage.Text = "0 %"
        ' La cle ne sert qu'a une chose a la fois : toute TV de Slide est arretee AVANT le scan, sinon aucun canal ne repond.
        Try
            Await LibererLaTV()
            DiagLog("TNT scan : tuner libere, lancement")
        Catch ex As Exception
            DiagLog("TNT scan : preparation (" & ex.Message & ")")
        End Try
        If IsDisposed Then Exit Sub
        If _annule Then                                   ' (« Arreter » pendant la preparation)
            _enCours = False
            TntScan.ScanEnCours = False
            MettreAJourLesBoutons()
            LabelEtat.Text = "Scan arrêté."
            Exit Sub
        End If
        LabelEtat.Text = "Scan en cours…"
        Dim resultat As TntScan.ResultatDuScan = Nothing
        Dim erreur As String = ""
        Try
            Dim pilote As TntScan.PiloteDeScan = CreerLePilote()
            resultat = Await Task.Run(Function() TntScan.Scanner(AddressOf Rapporter, Function() _annule, AddressOf ChainesTrouvees, pilote))
            Try
                If Not IsDisposed Then VlcScan.Stop()               ' (le dernier canal en cours si le scan a ete arrete)
            Catch ex As Exception
            End Try
        Catch ex As Exception
            erreur = ex.Message
            DiagLog("TNT scan : erreur (" & ex.Message & ")")
        End Try
        _enCours = False
        TntScan.ScanEnCours = False
        If IsDisposed Then Exit Sub
        MettreAJourLesBoutons()
        ProgressBarScan.Value = 0
        LabelPourcentage.Text = ""

        If erreur <> "" Then
            LabelEtat.Text = "Scan impossible : " & erreur
            MessageBox.Show(Me, "Scan impossible : " & erreur, "Scanner la TNT", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If
        If resultat Is Nothing OrElse resultat.Chaines.Count = 0 Then
            LabelEtat.Text = If(_annule, "Scan arrêté, aucune chaîne trouvée.", "Aucune chaîne trouvée.")
            If Not _annule Then
                MessageBox.Show(Me, "Aucune chaîne trouvée." & vbCrLf & "Vérifiez que l'antenne est branchée sur la clé TNT et que la clé n'est pas utilisée par une autre application." & vbCrLf & vbCrLf &
                                "Le détail de chaque canal est dans le journal de Slide (lignes « TNT scan »).", "Scanner la TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
            Exit Sub
        End If

        _chaines = resultat.Chaines
        Dim hors As Integer = TntScan.NumeroterSelonLePays(_chaines)
        Afficher(Nothing, True)
        Dim texte As String = If(_annule, "Scan arrêté : ", "Scan terminé : ") & _chaines.Count & " chaîne(s) sur " & resultat.CanauxAvecSignal & " canal(aux). Numéros officiels : " &
                              resultat.NumerosDuReseau & " lus dans le réseau, " & resultat.NumerosParNom & " d'après le nom."
        If hors > 0 Then texte &= " " & hors & " chaîne(s) au-delà de " & TntScan.NombreMax & " non gardée(s)."
        If resultat.Cryptees > 0 Then texte &= " " & resultat.Cryptees & " cryptée(s) ignorée(s)."
        texte &= " Cliquez sur Enregistrer pour garder la liste « " & ComboListe.Text.Trim() & " »."
        LabelEtat.Text = texte
        If ComboListe.Text.Trim() = "" Then ComboListe.Text = TntScan.NomDeListeLibre(My.Settings.Shabbat_Ville)
        CheckBoxUtiliser.Checked = False                 ' (decoche par defaut, demande de Jacob 23/11/2026 : la TV garde sa liste tant qu'on ne coche pas « Utiliser cette liste »)
        _nomCharge = ""
        _listeRemux = resultat.Remux
        If _listeRemux Then
            LabelEtat.Text = "ATTENTION : numéros de programme non fiables (flux reconstruit par VLC). " & texte
            MessageBox.Show(Me, "Les chaînes ont été trouvées par la 2e méthode du scan : VLC y reconstruit le flux et renumérote les programmes." & vbCrLf &
                            "Les numéros de « program= » du fichier ne sont donc PAS ceux de la TNT, et la TV risque de ne pas jouer ces chaînes." & vbCrLf & vbCrLf &
                            "Ne vous fiez pas à cette liste : le détail est dans le journal de Slide (lignes « TNT scan »).", "Scanner la TNT", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
        MettreAJourLesBoutons()
    End Sub

    ''' <summary>Vrai si la liste affichee vient de la 2e methode du scan (numeros de programme faux).</summary>
    Private _listeRemux As Boolean

    ''' <summary>Coupe toute TV de Slide (PIP, apercu, lecteur) : la cle ne sert qu'a une chose a la fois. Sert au scan et aux boutons Activer / Reinitialiser la cle.</summary>
    Private Async Function LibererLaTV() As Task
        LabelEtat.Text = "Arrêt de la TV pour libérer la clé TNT…"
        DiagLog("TNT : arret de la TV pour liberer la cle (mode " & Diffusion.Mode & ")")
        LibererLeTunerTV()                                   ' PIP TV
        FermerLApercuTV()                                    ' apercu TV
        If Diffusion.Mode = "TV_VLC_INT" Then EndAndContinue() ' TV en cours (comme la touche N) : le lecteur est arrete, le diaporama reprend
        Try
            Dim chrono As Stopwatch = Stopwatch.StartNew()
            Do While Form3.VlcControl2.IsPlaying AndAlso chrono.ElapsedMilliseconds < 6000
                Form3.VlcControl2.Stop()
                Await Task.Delay(300)
            Loop
        Catch ex As Exception
        End Try
        Await Task.Delay(2500)                               ' (le pilote de la cle a besoin d'un instant pour se liberer)
    End Function

    ''' <summary>Bouton « Réinitialiser la clé TNT » (23/11/2026, remplace « Activer » et « Réinitialiser » : l'activation fait partie de la réinitialisation). Apres un scan la cle reste bloquee : Windows ne peut pas
    ''' la redemarrer tant que Slide la tient ouverte (« pnputil /restart-device » rendait 3010). Slide demande donc l'autorisation administrateur, lance le script qui attend sa fermeture puis active / desactive /
    ''' reactive la cle, et se ferme et se relance tout seul (meme mecanisme que la relance de nuit : PIP et diaporama retrouves). Le resultat des commandes de Windows arrive dans le journal de Slide
    ''' (lignes « Cle TNT reinit : ... ») au redemarrage.</summary>
    Private Sub ButtonReinitCle_Click(sender As Object, e As EventArgs) Handles ButtonReinitCle.Click
        Const titre As String = "Réinitialiser la clé TNT"
        Dim nom As String = "", id As String = "", code As Integer
        Try
            If Not CleTNT.TrouverLaCle(nom, id, code) Then
                MessageBox.Show(Me, "Aucune clé TNT n'est branchée sur ce PC." & vbCrLf & "Branchez la clé TNT puis recommencez.", titre, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
        Catch ex As Exception
            MessageBox.Show(Me, "Recherche de la clé impossible : " & ex.Message, titre, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End Try
        If MessageBox.Show(Me, "Clé : " & nom & If(code <> 0, " (code d'erreur Windows " & code & ")", "") & vbCrLf & vbCrLf &
                           "Pour que Windows puisse la redémarrer, Slide va se fermer, puis la clé est activée et redémarrée, et Slide se relance tout seul (environ 30 secondes ; le diaporama et les PIP reprennent)." & vbCrLf &
                           "Windows va demander l'autorisation administrateur : répondez Oui." & vbCrLf & vbCrLf & "Continuer ?",
                           titre, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub

        Dim message As String = ""
        If Not CleTNT.LancerLaReinitialisation(id, message) Then
            DiagLog("Cle TNT : reinitialisation non lancee (" & message & ")")
            MessageBox.Show(Me, message, titre, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If
        LabelEtat.Text = "Slide se ferme pour réinitialiser la clé, puis il redémarre…"
        Try
            VlcScan.Stop()
        Catch ex As Exception
        End Try
        DiagLog("Cle TNT : fermeture de Slide pour reinitialiser la cle")
        RelanceDeNuit.RelancerMaintenant(False)                  ' (ferme Slide et le relance 15 s plus tard ; ne revient ici que si la relance est impossible)
        MessageBox.Show(Me, "Slide n'a pas pu se fermer et se relancer tout seul." & vbCrLf & "Fermez Slide vous-même dans la minute qui vient : la clé sera alors réinitialisée ; relancez Slide ensuite.", titre, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private _nomReseauPris As Boolean

    ''' <summary>Appele depuis le thread du scan, a chaque canal qui apporte des chaines : la liste a l'ecran se remplit, et le nom de la liste suit le nom du reseau des qu'il est connu.</summary>
    Private Sub ChainesTrouvees(chaines As List(Of ChaineTnt), nomReseau As String)
        If IsDisposed OrElse Not IsHandleCreated Then Exit Sub
        Try
            BeginInvoke(Sub()
                            If IsDisposed OrElse Not _enCours Then Exit Sub
                            _chaines = chaines
                            TntScan.NumeroterSelonLePays(_chaines)
                            Afficher(Nothing, True)                    ' (le tableau est retrie par numero officiel a chaque nouvelle chaine trouvee)
                            If Not _nomReseauPris AndAlso nomReseau <> "" AndAlso TntScan.NomReseauUtilisable(nomReseau) Then
                                _nomReseauPris = True
                                ComboListe.Text = TntScan.NomDeListeLibre(nomReseau)
                            End If
                        End Sub)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Appele depuis le thread du scan.</summary>
    Private Sub Rapporter(fait As Integer, total As Integer, texte As String)
        If IsDisposed OrElse Not IsHandleCreated Then Exit Sub
        Try
            BeginInvoke(Sub()
                            If IsDisposed Then Exit Sub
                            ProgressBarScan.Maximum = Math.Max(1, total)
                            ProgressBarScan.Value = Math.Max(0, Math.Min(ProgressBarScan.Maximum, fait))
                            LabelPourcentage.Text = CInt(100.0 * ProgressBarScan.Value / ProgressBarScan.Maximum).ToString(CultureInfo.InvariantCulture) & " %"      ' (pourcentage des canaux essayes, a droite de la barre)
                            LabelEtat.Text = texte
                        End Sub)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonArreter_Click(sender As Object, e As EventArgs) Handles ButtonArreter.Click
        _annule = True
        LabelEtat.Text = "Arrêt en cours…"
    End Sub

    Private Sub FormTnt_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _enCours Then
            _annule = True
            e.Cancel = True                      ' le scan s'arrete, puis la fenetre peut se fermer
            LabelEtat.Text = "Arrêt du scan en cours… fermez la fenêtre dans un instant."
        End If
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Edition
    ' ------------------------------------------------------------------------------------------

    Private Sub ButtonMonter_Click(sender As Object, e As EventArgs) Handles ButtonMonter.Click
        Deplacer(-1)
    End Sub

    Private Sub ButtonDescendre_Click(sender As Object, e As EventArgs) Handles ButtonDescendre.Click
        Deplacer(1)
    End Sub

    Private Sub Deplacer(sens As Integer)
        Dim c As ChaineTnt = ChaineChoisie()
        If c Is Nothing Then Exit Sub
        Dim i As Integer = _chaines.IndexOf(c)
        Dim j As Integer = i + sens
        If i < 0 OrElse j < 0 OrElse j >= _chaines.Count Then Exit Sub
        Dim autre As ChaineTnt = _chaines(j)
        Dim n As Integer = c.Numero
        c.Numero = autre.Numero
        autre.Numero = n
        Afficher(c)
    End Sub

    Private Sub ButtonSupprimer_Click(sender As Object, e As EventArgs) Handles ButtonSupprimer.Click
        Dim c As ChaineTnt = ChaineChoisie()
        If c Is Nothing Then Exit Sub
        If MessageBox.Show(Me, "Retirer « " & c.Nom & " » de la liste ?", "TNT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        _chaines.Remove(c)
        Afficher()
    End Sub

    Private Sub ButtonNumeroter_Click(sender As Object, e As EventArgs) Handles ButtonNumeroter.Click
        If Not _chaines.Any(Function(c) c.Lcn > 0) Then
            MessageBox.Show(Me, "Aucun numéro officiel n'est connu pour ces chaînes (il est lu pendant le scan, ou déduit du nom des chaînes françaises).", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        If MessageBox.Show(Me, "Remettre les chaînes à leur numéro officiel ?" & vbCrLf & "(l'ordre que vous avez donné est remplacé ; les chaînes sans numéro officiel passent après)", "TNT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Dim hors As Integer = TntScan.NumeroterSelonLePays(_chaines)
        Afficher(Nothing, True)
        LabelEtat.Text = "Chaînes numérotées comme le pays." & If(hors > 0, " " & hors & " chaîne(s) au-delà de " & TntScan.NombreMax & " retirée(s).", "")
    End Sub

    Private Sub GrilleChaines_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles GrilleChaines.CellEndEdit
        If e.RowIndex < 0 Then Exit Sub
        Dim c As ChaineTnt = TryCast(GrilleChaines.Rows(e.RowIndex).Tag, ChaineTnt)
        If c Is Nothing Then Exit Sub
        Dim texte As String = Convert.ToString(GrilleChaines.Rows(e.RowIndex).Cells(e.ColumnIndex).Value)
        If e.ColumnIndex = ColonneNom.Index Then
            texte = texte.Trim()
            If texte <> "" Then c.Nom = texte
            Afficher(c)
        ElseIf e.ColumnIndex = ColonneNumero.Index Then
            Dim n As Integer
            If Integer.TryParse(texte.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, n) AndAlso n >= 1 AndAlso n <= TntScan.NombreMax Then
                Dim autre As ChaineTnt = _chaines.FirstOrDefault(Function(x) x IsNot c AndAlso x.Numero = n)
                If autre IsNot Nothing Then autre.Numero = c.Numero          ' (deja pris : les deux numeros s'echangent)
                c.Numero = n
            End If
            Afficher(c)
        End If
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Enregistrement, nom de la liste
    ' ------------------------------------------------------------------------------------------

    Private Sub ButtonEnregistrer_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrer.Click
        Dim nom As String = ComboListe.Text.Trim()
        DiagLog("TNT : Enregistrer la liste « " & nom & " » (" & _chaines.Count & " chaine(s), liste de la TV : " & CheckBoxUtiliser.Checked & ", dossier " & TntScan.DossierDesListes() & ")")
        If Not TntScan.NomValide(nom) Then
            MessageBox.Show(Me, "Donnez un nom à la liste (par exemple le nom de la ville : « Paris », « Paris II »…)." & vbCrLf & "Les caractères \ / : * ? "" < > | ne sont pas permis.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        If _chaines.Count = 0 Then
            MessageBox.Show(Me, "La liste est vide : lancez un scan ou ouvrez une liste.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        If _listeRemux AndAlso MessageBox.Show(Me, "Les numéros de programme de cette liste viennent d'un flux reconstruit par VLC : ils sont probablement faux et la TV risque de ne pas jouer les chaînes." & vbCrLf & "Enregistrer quand même ?", "TNT", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Exit Sub
        If Not String.Equals(nom, _nomCharge, StringComparison.OrdinalIgnoreCase) AndAlso IO.File.Exists(TntScan.FichierDeLaListe(nom)) Then
            If MessageBox.Show(Me, "Une liste « " & nom & " » existe déjà. La remplacer ?" & vbCrLf & "(l'ancienne est gardée dans tnt-" & nom & ".m3u.sauvegarde)", "TNT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        End If
        Try
            TntScan.Ecrire(nom, _chaines)
        Catch ex As Exception
            DiagLog("TNT : enregistrement impossible (" & ex.Message & ")")
            MessageBox.Show(Me, "Enregistrement impossible : " & ex.Message & vbCrLf & TntScan.FichierDeLaListe(nom), "TNT", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End Try
        _nomCharge = nom
        If CheckBoxUtiliser.Checked Then TntScan.ChoisirLaListe(nom)
        If String.Equals(TntScan.ListeUtilisee(), nom, StringComparison.OrdinalIgnoreCase) Then
            Try
                frequenceTV()                    ' la TV et les listes de la minuterie prennent tout de suite la nouvelle liste
            Catch ex As Exception
                DiagLog("TNT : rechargement de la liste (" & ex.Message & ")")
            End Try
        End If
        RemplirLesListes()
        MettreAJourLesBoutons()
        Dim texteFin As String = "Liste « " & nom & " » enregistrée (" & _chaines.Count & " chaîne(s))" & If(String.Equals(TntScan.ListeUtilisee(), nom, StringComparison.OrdinalIgnoreCase), " ; c'est celle de la TV de Slide.", ".")
        LabelEtat.Text = texteFin
        MessageBox.Show(Me, texteFin, "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)      ' (un retour visible : avant, seul le texte du bas changeait)
    End Sub

    Private Sub ButtonRenommer_Click(sender As Object, e As EventArgs) Handles ButtonRenommer.Click
        Dim nouveau As String = ComboListe.Text.Trim()
        If _nomCharge = "" Then Exit Sub
        If Not TntScan.NomValide(nouveau) Then
            MessageBox.Show(Me, "Tapez le nouveau nom de la liste dans la case du haut, puis cliquez sur Renommer." & vbCrLf & "Les caractères \ / : * ? "" < > | ne sont pas permis.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        If String.Equals(nouveau, _nomCharge, StringComparison.OrdinalIgnoreCase) AndAlso nouveau = _nomCharge Then
            MessageBox.Show(Me, "Tapez le NOUVEAU nom dans la case du haut, puis cliquez sur Renommer.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Try
            If String.Equals(nouveau, _nomCharge, StringComparison.OrdinalIgnoreCase) Then
                ' (seulement les majuscules / minuscules : Windows ne voit pas de difference, on passe par un nom provisoire)
                Dim provisoire As String = _nomCharge & "_provisoire"
                If Not TntScan.Renommer(_nomCharge, provisoire) Then Throw New Exception("renommage impossible")
                If Not TntScan.Renommer(provisoire, nouveau) Then Throw New Exception("renommage impossible")
            ElseIf Not TntScan.Renommer(_nomCharge, nouveau) Then
                MessageBox.Show(Me, "Une liste « " & nouveau & " » existe déjà.", "TNT", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
        Catch ex As Exception
            MessageBox.Show(Me, "Renommage impossible : " & ex.Message, "TNT", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End Try
        DiagLog("TNT : liste « " & _nomCharge & " » renommee « " & nouveau & " »")
        _nomCharge = nouveau
        If String.Equals(TntScan.ListeUtilisee(), nouveau, StringComparison.OrdinalIgnoreCase) Then
            Try
                frequenceTV()
            Catch ex As Exception
            End Try
        End If
        RemplirLesListes()
        MettreAJourLesBoutons()
        LabelEtat.Text = "Liste renommée « " & nouveau & " »."
    End Sub

End Class
