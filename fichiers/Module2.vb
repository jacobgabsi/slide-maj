Option Explicit Off


Imports System.Data
Imports System.Data.OleDb
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports System.Diagnostics.Eventing
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Xml
Imports ADOX
Imports CoreAudio
Imports MediaInfoDotNet
Imports MediaInfoLib
Imports MetadataExtractor.Formats.Exif
Imports Zmanim
Imports Zmanim.TimeZone
Imports Zmanim.TzDatebase
Imports Zmanim.Utilities



Module Module2
    Public ColSelection = 4
    Public CheminFilm




    Sub Choix_Film(r)
        Argument = ""
        ancienNom = Form4.DataGridView1.Rows(r).Cells("Action").Value
        ' La ligne avait deja un film (avec ses reglages de PIP, etc.) : memorises pour etre proposes au nouveau film.
        ReglagesDuFilmRemplace = Nothing
        Try
            If UCase(CStr(If(Form4.DataGridView1.Rows(r).Cells("Type").Value, ""))) = "FILM" Then
                ReglagesDuFilmRemplace = CStr(If(Form4.DataGridView1.Rows(r).Cells("Argument").Value, ""))
                VolumeDuFilmRemplace = CStr(If(Form4.DataGridView1.Rows(r).Cells("Volume").Value, "")).Trim()
                NomDuFilmRemplace = CStr(If(ancienNom, ""))
            End If
        Catch ex As Exception
        End Try
        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewTextBoxCell
        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewTextBoxCell)

        If CheminFilm = DossierNflixParDefault Then
            'Dim Nom As String = AppPath & "\Data\IPTV.m3u"
            Dim Nom As String = DossierNflixParDefault
            Dim infoReader As System.IO.FileInfo
            infoReader = My.Computer.FileSystem.GetFileInfo(Nom)
            If Format(infoReader.LastWriteTime, "dd/MM/yyyy") <> Format(Now, "dd/MM/yyyy") Or File.Exists(Nom) = False Then
                ' Liste du jour de la source IPTV active : liste .m3u ou compte Xtream (voir IptvSources).
                Dim erreurIptv As String = ""
                Form4.Cursor = Cursors.WaitCursor
                Dim reussi As Boolean = IptvSources.Telecharger(IptvSources.SelectionIptv, erreurIptv)
                Form4.Cursor = Cursors.Default
                If Not reussi Then MsgBox("La liste IPTV n'a pas pu être mise à jour :" & vbCrLf & erreurIptv)
            End If
            LigneMinuterieEnCours.Index = CInt(r)


            Exit Sub
            'GoTo suite
        End If


        ' Plusieurs films a la fois (demande de Jacob, 06/10 : les episodes d'une serie). Le premier
        ' prend la ligne en cours ; les suivants sont ajoutes juste en dessous, chacun a l'heure ou
        ' le precedent se termine (voir Form4.AjouterLaSuiteDeLaSerie).
        Form4.OpenFileDialog1.Multiselect = True
        Form4.OpenFileDialog1.RestoreDirectory = True

        Form4.OpenFileDialog1.Filter = "Tous les Fichiers|*.*"

        If DossierDreamboxParDefault <> "" And CheminFilm = DossierDreamboxParDefault Then Form4.OpenFileDialog1.Filter = "Dreambox File|*.TS"

        Dim serie As New List(Of String)
        result = Form4.OpenFileDialog1.ShowDialog()
        If result = DialogResult.OK Then
            serie = TrierLesEpisodes(Form4.OpenFileDialog1.FileNames)
            fileName = If(serie.Count > 0, serie(0), Form4.OpenFileDialog1.FileName)
        Else
            fileName = ancienNom
        End If

        Form4.OpenFileDialog1.InitialDirectory = ""

        RemplirLaLigneFilm(r, fileName, Argument)
        ReglagesDuFilmRemplace = Nothing
        If serie.Count > 1 Then Form4.AjouterLaSuiteDeLaSerie(CInt(r), serie.Skip(1).ToList())

    End Sub

    ''' <summary>Les fichiers choisis dans l'ordre des episodes : par nom, les nombres compares comme
    ''' des nombres ("Episode 2" avant "Episode 10"), quel que soit l'ordre des clics.</summary>
    Function TrierLesEpisodes(fichiers As String()) As List(Of String)
        Dim cle As Func(Of String, String) =
            Function(f) System.Text.RegularExpressions.Regex.Replace(IO.Path.GetFileName(f).ToLowerInvariant(), "\d+",
                                                                      Function(m) m.Value.PadLeft(12, "0"c))
        Return fichiers.Where(Function(f) Not String.IsNullOrWhiteSpace(f)).
                        OrderBy(Function(f) IO.Path.GetDirectoryName(f), StringComparer.OrdinalIgnoreCase).
                        ThenBy(cle, StringComparer.Ordinal).ToList()
    End Function

    ''' <summary>Remplit une ligne "Film" de la minuterie avec <paramref name="fileName"/> : colonne
    ''' Action, duree (colonnes Play et Durée), volume et reglages propres a ce fichier.</summary>
    ''' <summary>
    ''' Un autre film du meme dossier est deja dans la minuterie, avec ses reglages : ils sont PROPOSES au
    ''' film qu'on vient de choisir, jamais appliques d'office (demande de Jacob, 13/10/2026 : "si tu
    ''' retrouves des anciens reglages, propose-moi de reprendre ou pas les parametres" - un film d'un
    ''' dossier ou un autre etait en PIP arrivait en PIP sans qu'on l'ait demande).
    ''' Rend Vrai pour les reprendre. Rien de notable dans ces reglages (volume 100, pas de PIP, de boucle,
    ''' de piste ou de sous-titres choisis) : repris sans rien demander, ils ne changent rien.
    ''' </summary>
    Function ReprendreLesReglagesDuDossier(fichier As String, voisin As String, volume As String, reglages As String, Optional filmRemplace As Boolean = False) As Boolean
        Dim lignes As New List(Of String)
        Try
            Dim v As Integer
            If Integer.TryParse(Convert.ToString(volume), v) AndAlso v <> 100 Then lignes.Add("Volume : " & v & " %")
            If UCase(Convert.ToString(Arg("Pip", reglages))) = "TRUE" Then
                Dim coin As String = CoinDuPip(reglages)
                Dim ou As String
                Select Case coin
                    Case "Bgauche" : ou = "en bas à gauche"
                    Case "Hdroite" : ou = "en haut à droite"
                    Case "Hgauche" : ou = "en haut à gauche"
                    Case "Mgauche" : ou = "au milieu à gauche"
                    Case "Mdroite" : ou = "au milieu à droite"
                    Case Else : ou = "en bas à droite"
                End Select
                Dim texte As String = "PIP : " & LireTaillePip(reglages) & " %, " & ou
                If UCase(Convert.ToString(Arg("PipPersistant", reglages))) = "TRUE" Then texte &= ", reste affiché"
                lignes.Add(texte)
            End If
            If UCase(Convert.ToString(Arg("Boucle", reglages))) = "TRUE" Then lignes.Add("Lecture en boucle")
            If Convert.ToString(Arg("AudioLanguage", reglages)).Trim() <> "" Then lignes.Add("Piste audio choisie")
            If Convert.ToString(Arg("SubTitle", reglages)).Trim() <> "" Then lignes.Add("Sous-titres choisis")
            If UCase(Convert.ToString(Arg("ModeHTTP", reglages))) = "TRUE" Then lignes.Add("Lecture par le réseau (HTTP)")
            If Convert.ToString(Arg("Pub", reglages)).Trim() <> "" Then lignes.Add("Coupure de publicité")
        Catch ex As Exception
            DiagLog("Reglages du dossier (lecture) : " & ex.Message)
            Return False
        End Try
        If lignes.Count = 0 Then Return True

        Dim nom As Func(Of String, String) = Function(chemin As String)
                                                 Try
                                                     Return IO.Path.GetFileName(chemin)
                                                 Catch ex As Exception
                                                     Return chemin
                                                 End Try
                                             End Function
        Dim question As String = If(filmRemplace, "Le film que vous remplacez dans cette ligne :", "Un autre film de ce dossier est déjà dans la minuterie :") & vbCrLf &
                                 "    " & If(String.IsNullOrEmpty(voisin), If(filmRemplace, "(film remplacé)", "(film du même dossier)"), nom(voisin)) & vbCrLf & vbCrLf &
                                 If(filmRemplace, "avait ces réglages :", "Il a ces réglages :") & vbCrLf &
                                 String.Join(vbCrLf, lignes.Select(Function(l) "    • " & l)) & vbCrLf & vbCrLf &
                                 If(filmRemplace, "Prendre les mêmes réglages que l'existant pour « ", "Les reprendre pour « ") & nom(fichier) & " » ?"
        Dim reponse As DialogResult
        Try
            reponse = If(Form4.Visible,
                         MessageBox.Show(Form4, question, "Réglages du dossier", MessageBoxButtons.YesNo, MessageBoxIcon.Question),
                         MessageBox.Show(question, "Réglages du dossier", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
        Catch ex As Exception
            DiagLog("Reglages du dossier (question) : " & ex.Message)
            Return False
        End Try
        DiagLog(If(filmRemplace, "Reglages du film remplace : proposes pour ", "Reglages du dossier : proposes pour ") & nom(fichier) & " (" & String.Join(", ", lignes) & ") -> " &
                If(reponse = DialogResult.Yes, "repris", "non repris"))
        Return reponse = DialogResult.Yes
    End Function

    ''' <summary>Reglages d'une tache Film qui suivent la LIGNE quand on en change le film (coin, taille et mode du PIP, boucle,
    ''' piste audio, sous-titres, mode HTTP, coupure de pub) : tout sauf le titre, l'adresse et la source, propres a l'ancien film.</summary>
    Function ReglagesConservesDuFilm(ancienArgument As String) As String
        Dim reste As String = ""
        For Each cle As String In {"Lancement", "Pip", "PipSize", "PipPosition", "PipPersistant", "Boucle", "AudioLanguage", "SubTitle", "ModeHTTP", "Pub"}
            Dim v As String = Convert.ToString(Arg(cle, If(ancienArgument, ""))).Trim()
            If v <> "" Then reste &= "!/" & cle & ":" & v
        Next
        Return reste
    End Function

    ' Film REMPLACE dans une ligne (Choix_Film) : ses reglages, proposes au nouveau film a la place de ceux d'un film voisin.
    Friend ReglagesDuFilmRemplace As String = Nothing
    Friend VolumeDuFilmRemplace As String = ""
    Friend NomDuFilmRemplace As String = ""

    Sub RemplirLaLigneFilm(r As Object, fileName As Object, Optional Argument As Object = "", Optional proposerLesReglages As Boolean = True)
        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewTextBoxCell
        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = fileName

        Dim MyDuration As String = GetDuration(fileName)

        If MyDuration = "" Then MyDuration = GetDuration2(fileName)
        If MyDuration = "00:00" Then MyDuration = "23:59:59"
        If MyDuration = "00:00:00" Then MyDuration = "23:59:59"
        Form4.DataGridView1.Rows(r).Cells("Play") = New DataGridViewButtonCell


        Dim cb2 As DataGridViewButtonCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Play"), DataGridViewButtonCell)
        cb2.Value = MyDuration

        If CheminFilm <> DossierNflixParDefault Then

            Form4.DataGridView1.Rows(r).Cells("Durée").Value = MyDuration
            'Form4.DataGridView1.Rows(r).Cells("Volume").Value = "100"

            If InStr(UCase(fileName), recepteurSat) > 0 Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = Format(VolumeDreamboxParDefault)
            Form4.DataGridView1.Rows(r).Cells("Type").Value = "Film"
            ' Reglages d'un autre film du meme dossier : proposes, pas appliques d'office. Les films d'une
            ' serie ne demandent rien (proposerLesReglages = Faux) : ils gardent ceux du premier episode.
            If proposerLesReglages Then
                Dim reglagesRemplaces As String = If(ReglagesDuFilmRemplace Is Nothing, "", ReglagesConservesDuFilm(ReglagesDuFilmRemplace))
                If reglagesRemplaces <> "" Then
                    ' Un film en remplace un autre dans CETTE ligne (16/10/2026 : "le PIP que je veux remplacer etait en haut a droite,
                    ' la fenetre me propose en haut a gauche") : ce sont les reglages du film remplace qui sont proposes, pas ceux
                    ' d'un voisin du dossier.
                    If ReprendreLesReglagesDuDossier(CStr(fileName), NomDuFilmRemplace, VolumeDuFilmRemplace, reglagesRemplaces, True) Then
                        If VolumeDuFilmRemplace <> "" AndAlso VolumeDuFilmRemplace <> "100" Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = VolumeDuFilmRemplace
                        Argument = reglagesRemplaces
                    End If
                Else
                VolumeEtArgumentAdapté(fileName, VolumeAdapté, ArgAdapté)
                If ReprendreLesReglagesDuDossier(CStr(fileName), CStr(VoisinAdapté), CStr(VolumeAdapté), CStr(ArgAdapté)) Then
                    If VolumeAdapté <> "100" Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = VolumeAdapté
                    If ArgAdapté <> "" Then Argument = ArgAdapté
                End If
                End If
            End If
            Form4.DataGridView1.Rows(r).Cells("Argument").Value = Argument
            ' Le titre montre dans l'infobulle de Play est celui des reglages de la ligne (vide pour un
            ' film qu'on vient de choisir) : avant, celui du film precedent y restait.
            Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = Convert.ToString(Arg("Titre", Argument))
            Form4.DataGridView1.CurrentCell = Form4.DataGridView1.Rows(r).Cells("Play")
        End If


    End Sub

    Sub Choix_Lancer(r)

        argu = Form4.DataGridView1.Rows(r).Cells("Argument").Value

        Form17.TextBox15.Text = Arg("Prg", argu)
        Form17.TextBox1.Text = Arg("Argu", argu)
        chk = Arg("Wait", argu)
        If chk = "" Then chk = False
        Form17.CheckBox1.Checked = chk

        Form17.StartPosition = FormStartPosition.CenterScreen
        Form17.Show()
        Form17.TopMost = True

    End Sub
    ''' <summary>Liste deroulante des chaines TV dans la cellule Action de la ligne
    ''' <paramref name="r"/>, positionnee sur <paramref name="chaine"/> (la 1re chaine si vide).
    ''' Une chaine enregistree absente de la liste actuelle (autre ville) y est ajoutee telle
    ''' quelle, pour ne jamais la perdre.</summary>
    Sub ListeDesChainesTV(r As Integer, chaine As String)
        Dim Chanel(50) As String
        For i = 0 To 50
            Chanel(i) = "Chaine " + Format(i, "00") + " :" + ChannelID(i, 1)
        Next
        Chanel(0) = "Chaine OFF"

        ' Poser la valeur declenche CellValueChanged (Form4), qui remet le volume TV par defaut :
        ' on garde celui de la ligne (sinon le chargement ecrasait le volume enregistre).
        Dim volume As Object = Form4.DataGridView1.Rows(r).Cells("Volume").Value

        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Chanel)
        If String.IsNullOrEmpty(chaine) Then chaine = Chanel(1)
        If Not cb.Items.Contains(chaine) Then cb.Items.Add(chaine)
        cb.Value = chaine

        If volume IsNot Nothing AndAlso CStr(volume) <> "" Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = volume
    End Sub

    Sub Choix_TV(r)

        ' Ligne deja TV (on a juste re-choisi "TV" pour changer de chaine) : on garde sa chaine,
        ' son volume, sa colonne Play et son argument (reglages du PIP). Avant, tout etait efface.
        Dim ancienneChaine As String = CStr(If(Form4.DataGridView1.Rows(r).Cells("Action").Value, ""))
        If ancienneChaine.StartsWith("Chaine ", StringComparison.OrdinalIgnoreCase) Then
            ListeDesChainesTV(r, ancienneChaine)
            Exit Sub
        End If

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        ListeDesChainesTV(r, "")

        Form4.DataGridView1.Rows(r).Cells("Volume").Value = VolumeTVParDefault.ToString
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""

    End Sub

    Sub Choix_SAT(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim ChanelSat(ChannelIDsatTotal) As String
        For i = 1 To ChannelIDsatTotal
            ChanelSat(i) = "Sat   " + Format(i, "000") + " :" + ChannelIDsat(i, 1)
        Next

        ChanelSat(0) = "SAT OFF"


        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(ChanelSat)
        cb.Value = ChanelSat(1)

        Form4.DataGridView1.Rows(r).Cells("Volume").Value = VolumeDreamboxParDefault.ToString
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""

    End Sub
    Sub Choix_IPTV(r)

        '**'
        'Form4.Button25.PerformClick()
        ''Choix_Film(r)
        'Exit Sub

        ChoixNflix.Ouvrir(Form4.DataGridView1.Rows(CInt(r)), IptvSources.GenreIptv.Chaines)          ' la fenetre s'ouvre sur les chaines


    End Sub




    Sub Choix_Volume(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim Vc(4) As String
        Vc(0) = "Volume OFF"
        Vc(1) = "Volume ON"
        Vc(2) = "Volume Music Slide"
        Vc(3) = "Volume Video Slide"
        Vc(4) = "Volume General"


        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""

    End Sub
    Sub Choix_PhotoMode(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim Vc(5) As String
        Vc(0) = "Décroissant"
        Vc(1) = "Croissant"
        Vc(2) = "Aléatoire Classic"
        Vc(3) = "Aléatoire Séquentiel"
        Vc(4) = "Aléatoire OFF"
        Vc(5) = "Reprendre les Photos depuis le debut"


        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Items.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypePhoto).ToArray())
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
    End Sub
    Sub Choix_VideoMode(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim Vc(6) As String
        Vc(0) = "Décroissant"
        Vc(1) = "Croissant"
        Vc(2) = "Aléatoire ON"
        Vc(3) = "Aléatoire OFF"
        Vc(4) = "Reprendre les Videos depuis le debut"
        Vc(5) = "Video avec audio ON"
        Vc(6) = "Video avec audio OFF"

        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Items.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypeVideo).ToArray())
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
    End Sub
    Sub Choix_PhotoVideoMode(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim Vc(7) As String
        Vc(0) = "Décroissant"
        Vc(1) = "Croissant"
        Vc(2) = "Aléatoire ON"
        Vc(3) = "Aléatoire OFF"
        Vc(4) = "Reprendre Photos/Videos depuis le debut avec RAZ"
        Vc(5) = "Reprendre Photos/Videos depuis le debut sans RAZ"
        Vc(6) = "Reprendre Photos/Videos debut RAZ Aléatoite OFF"
        Vc(7) = "Mode Normal Photos/Videos Aléatoite Séquenciel"


        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""


    End Sub
    Sub Choix_Sonoff(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""



        Dim Vc((NombreDeCapteur - 1) * 2 + 1) As String
        j = -1
        For i = 0 To NombreDeCapteur - 1
            j = j + 1 : Vc(j) = Sonoff(i, 0) & " ON"
            j = j + 1 : Vc(j) = Sonoff(i, 0) & " OFF"
        Next




        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Volume").Value = "100"
    End Sub

    ''' <summary>Le type "Tuya" vient d'etre choisi : la case Action propose les commandes de l'onglet Sources (cadre Tuya). Plusieurs comptes : la question "quel compte ?"
    ''' vient d'abord ; la reponse est gardee dans l'argument de la ligne ("!/Compte:Nom") et la liste ne propose que les appareils de ce compte (et les groupes).</summary>
    Sub Choix_Tuya(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Volume").Value = ""
        If TuyaSlide.NomsDesCommandes().Count = 0 Then
            MessageBox.Show("Aucune commande Tuya : renseignez le compte et cochez des appareils dans l'onglet Sources (cadre Tuya).", "Tuya", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim compte As String = ""
        If Not DemanderLeCompteTuya(compte, "") Then compte = TuyaSlide.ComptesActuels().Select(Function(c) c.Nom).FirstOrDefault()          ' (annuler : le premier compte)
        ListeDesActionsDuType(CInt(r), "Tuya", "")
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ArgumentDuCompteTuya(compte)
        ' La fenetre de choix multiples s'ouvre tout de suite (plusieurs appareils dans la meme ligne ; annuler : la premiere commande de la liste).
        Dim nouvelle As String = ""
        If FormReglages.Demander(Form4, "Tuya", TuyaSlide.NomsDesCommandes(compte), "", nouvelle) Then ListeDesActionsDuType(CInt(r), "Tuya", nouvelle)
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ArgumentDuCompteTuya(compte)
    End Sub

    ''' <summary>Clic droit "Modifier les options" sur une ligne Tuya : le compte (s'il y en a plusieurs), puis la fenetre de choix multiples sur la ou les commandes actuelles.</summary>
    Sub ModifierLaLigneTuya(r As Integer)
        Dim actuelle As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").FormattedValue)
        Dim argument As String = CStr(If(Form4.DataGridView1.Rows(r).Cells("Argument").Value, ""))
        Dim compte As String = ""
        If Not DemanderLeCompteTuya(compte, Convert.ToString(Arg("Compte", argument)).Trim()) Then Exit Sub
        Dim nouvelle As String = ""
        If FormReglages.Demander(Form4, "Tuya", TuyaSlide.NomsDesCommandes(compte), actuelle, nouvelle) Then ListeDesActionsDuType(r, "Tuya", nouvelle)
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ArgumentDuCompteTuya(compte)
    End Sub

    ''' <summary>L'argument d'une ligne Tuya : "!/Compte:Nom" quand il y a plusieurs comptes, sinon vide.</summary>
    Function ArgumentDuCompteTuya(compte As String) As String
        Return If(String.IsNullOrWhiteSpace(compte) OrElse TuyaSlide.ComptesActuels().Count <= 1, "", "!/Compte:" & compte.Trim())
    End Function

    ''' <summary>Plusieurs comptes Tuya : demande de quel compte sont les appareils de la ligne (<paramref name="compte"/> : son nom). Un seul compte : pas de question. Faux si on annule.</summary>
    Function DemanderLeCompteTuya(ByRef compte As String, deja As String) As Boolean
        Dim noms As List(Of String) = TuyaSlide.ComptesActuels().Select(Function(c) c.Nom).ToList()
        compte = If(noms.Count = 1, noms(0), "")
        If noms.Count <= 1 Then Return True
        Return FormChoixCompteTuya.Demander(Form4, noms, deja, compte)
    End Function

    Sub Choix_EcranNoir(r)

        Dim heure As Date = Form4.DataGridView1.Rows(r).Cells("Heure").Value
        'heure2 = Form4.DataGridView1.Rows(r).Cells("Heure").Value
        'Dim heure = TimeSerial(Mid(heure2, 1, 2), Mid(heure2, 4, 2), 0)
        j = UCase(Form4.DataGridView1.Rows(r).Cells("Jour").Value)
        db = New DateTime(ProchainJourDeLaSemaine(j).Year, ProchainJourDeLaSemaine(j).Month, ProchainJourDeLaSemaine(j).Day, heure.Hour, heure.Minute, 0)
        Form5.DateTimePicker1.Value = db
        act = Form4.DataGridView1.Rows(r).Cells("action").Value
        p = InStr(act, "-")
        Try
            If IsDate(Mid(act, p + 1)) = False Then
                Form5.DateTimePicker2.Value = db.AddMinutes(1)
            Else
                'p = InStr(act, "-")
                Dim db2 As Date = Mid(act, p + 1)
                Form5.DateTimePicker2.Value = db2
                If Form4.DataGridView1.Rows(r).Cells("Play").Value = "Timer ON" Then Form5.CheckBox1.Checked = True Else Form5.CheckBox1.Checked = False
            End If

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        ' La cellule peut etre vide (Nothing) ou venir de la base (DBNull) : CStr planterait.
        Try
            Dim argumentLigne As Object = Form4.DataGridView1.Rows(r).Cells("Argument").Value
            Dim texteArgument As String = If(TypeOf argumentLigne Is String, CStr(argumentLigne), "")
            Form5.CheckBoxAquarium.Checked = UCase(CStr(Arg("Aquarium", texteArgument))) = "TRUE"
        Catch ex As Exception
            Form5.CheckBoxAquarium.Checked = False
            DiagLog("Choix_EcranNoir aquarium : " & ex.Message)
        End Try
        Form5.StartPosition = FormStartPosition.CenterScreen
        Form5.Show()
        Form5.TopMost = True



    End Sub
    Sub Choix_ModeRdv(r)


        Form16.StartPosition = FormStartPosition.CenterScreen
        Form16.Show()
        Form16.TopMost = True



    End Sub

    Sub Choix_Divers(r)

        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        ' (Les commandes de Deezer ont leur propre type depuis le 08/10 : voir Choix_Deezer.)
        Dim Vc(3) As String
        Vc(0) = "TV OFF"
        Vc(1) = "IPTV OFF"
        Vc(2) = "Close All Player"
        Vc(3) = "End & Continue"


        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Value = Vc(0)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""

    End Sub
    ' ------------------------------------------------------------------------------------------
    ' Type "Deezer" de la minuterie (demande de Jacob, 08/10 : "sors les commandes Deezer de
    ' Divers et mets tout dans une nouvelle section Deezer"). Toutes les commandes de Deezer y
    ' sont : avant, elles etaient dans Divers (Lance Deezer, Play/Pause, Play, Pause, Next Song,
    ' Kill Deezer), Volume (Volume Deezer) et Music Source (Deezer). Execution : Module1.TacheDeezer.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Actions du type "Deezer", dans l'ordre de la liste.</summary>
    Friend ReadOnly ActionsDeezer As String() = {"Lance Deezer", "Deezer Favoris", "Deezer Play/Pause", "Deezer Play", "Deezer Pause", "Deezer Next Song", "Volume Deezer", "Kill Deezer"}

    ''' <summary>Type sous lequel une tache doit etre rangee : "Deezer" pour les taches Deezer
    ''' d'avant le 08/10 (rangees dans Divers, Volume ou Music Source), sinon son type.</summary>
    ''' <summary>Le type de tache "SatVlcExt" (chaine du recepteur satellite dans un VLC externe) a ete
    ''' retire le 13/10/2026 avec le lecteur externe : une tache enregistree sous ce type devient une
    ''' tache "Sat", jouee dans Slide. Meme chaine, meme volume ; la base suit au prochain OK des reglages.</summary>
    Function TypeSansVlcExterne(type As Object) As Object
        If String.Equals(Convert.ToString(type).Trim(), "SatVlcExt", StringComparison.OrdinalIgnoreCase) Then Return "Sat"
        Return type
    End Function

    Function TypeDeezerPour(type As String, action As String) As String
        Dim t As String = UCase(If(type, "").Trim()), a As String = UCase(If(action, "").Trim())
        If t = "DEEZER" Then Return "Deezer"
        If t = "DIVERS" AndAlso (a = "LANCE DEEZER" OrElse a = "DEEZER PLAY/PAUSE" OrElse a = "DEEZER PLAY" OrElse a = "DEEZER PAUSE" OrElse a = "DEEZER NEXT SONG" OrElse a = "KILL DEEZER") Then Return "Deezer"
        If t = "VOLUME" AndAlso a = "VOLUME DEEZER" Then Return "Deezer"
        If t = "MUSIC SOURCE" AndAlso a = "DEEZER" Then Return "Deezer"
        Return type
    End Function

    ''' <summary>Action d'une tache Deezer, telle qu'elle est ecrite dans la liste du type "Deezer"
    ''' (l'ancienne action "Deezer" de Music Source devient "Lance Deezer").</summary>
    Function ActionDeezerPour(action As String) As String
        Dim a As String = If(action, "").Trim()
        If UCase(a) = "DEEZER" Then Return ActionsDeezer(0)
        For Each connue As String In ActionsDeezer
            If String.Equals(connue, a, StringComparison.OrdinalIgnoreCase) Then Return connue
        Next
        Return a
    End Function

    ''' <summary>Pose la liste des actions Deezer dans la case Action de la ligne, sur
    ''' <paramref name="action"/>, sans toucher au reste de la ligne (chargement des taches).</summary>
    Sub ListeDesActionsDeezer(r As Integer, action As String)
        ' Poser la valeur declenche CellValueChanged (Form4), qui regle le volume selon l'action :
        ' au chargement, on garde celui de la ligne.
        Dim volume As Object = Form4.DataGridView1.Rows(r).Cells("Volume").Value
        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(ActionsDeezer)
        Dim voulue As String = ActionDeezerPour(action)
        If Not ActionsDeezer.Contains(voulue) Then voulue = ActionsDeezer(0)
        cb.Value = voulue
        If volume IsNot Nothing AndAlso CStr(volume) <> "" Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = volume
    End Sub

    ''' <summary>Liste deroulante de la case Action pour une ligne CHARGEE (ou copiee) de type <paramref name="type"/>, posee
    ''' sur <paramref name="action"/> sans toucher au reste de la ligne. Couvre tous les types a liste : TV, Sat, Volume,
    ''' Photo/Video/PhotoVideo Mode, Sonoff, Divers, Music Source, Deezer (15/10/2026 : "la petite fleche n'est plus la
    ''' quand je rouvre la minuterie"). Les autres types (Film, IPTV, Lancer, Ecran Noir, Mode RDV) n'ont pas de liste.</summary>
    Sub ListeDesActionsDuType(r As Integer, type As String, action As String)
        Try
            Dim t As String = UCase(If(type, "").Trim())
            If t = "TV" Then ListeDesChainesTV(r, action) : Exit Sub
            If t = "DEEZER" Then ListeDesActionsDeezer(r, action) : Exit Sub
            Dim Vc As New List(Of String)
            Select Case t
                Case "SAT"
                    Vc.Add("SAT OFF")
                    For i As Integer = 1 To ChannelIDsatTotal
                        Vc.Add("Sat   " + Format(i, "000") + " :" + ChannelIDsat(i, 1))
                    Next
                Case "VOLUME"
                    Vc.AddRange({"Volume OFF", "Volume ON", "Volume Music Slide", "Volume Video Slide", "Volume General"})
                Case "PHOTO MODE"
                    Vc.AddRange({"Décroissant", "Croissant", "Aléatoire Classic", "Aléatoire Séquentiel", "Aléatoire OFF", "Reprendre les Photos depuis le debut"})
                    Vc.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypePhoto))
                Case "VIDEO MODE"
                    Vc.AddRange({"Décroissant", "Croissant", "Aléatoire ON", "Aléatoire OFF", "Reprendre les Videos depuis le debut", "Video avec audio ON", "Video avec audio OFF"})
                    Vc.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypeVideo))
                Case "PHOTOVIDEO MODE"
                    Vc.AddRange({"Décroissant", "Croissant", "Aléatoire ON", "Aléatoire OFF", "Reprendre Photos/Videos depuis le debut avec RAZ", "Reprendre Photos/Videos depuis le debut sans RAZ", "Reprendre Photos/Videos debut RAZ Aléatoite OFF", "Mode Normal Photos/Videos Aléatoite Séquenciel"})
                Case "SONOFF"
                    For i As Integer = 0 To NombreDeCapteur - 1
                        Vc.Add(Sonoff(i, 0) & " ON")
                        Vc.Add(Sonoff(i, 0) & " OFF")
                    Next
                Case "TUYA"
                    Vc.AddRange(TuyaSlide.NomsDesCommandes())
                Case "DIVERS"
                    Vc.AddRange({"TV OFF", "IPTV OFF", "Close All Player", "End & Continue"})
                Case "MUSIC SOURCE"
                    Vc.AddRange(ActionsMusicSource)
                Case "RADIO"
                    Vc.AddRange(NomsDesRadios())
                Case "SON"
                    Vc.AddRange(ActionsSon)
                    Vc.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypeSon))
                Case "AUTRES"
                    Vc.AddRange(ReglagesMinuterie.Actions(ReglagesMinuterie.TypeAutres))
                Case "YOM TOV"
                    Vc.AddRange({"Entrée de fête", "Sortie de fête"})
                Case Else
                    Exit Sub
            End Select
            If Vc.Count = 0 Then Exit Sub
            ' Poser la valeur declenche CellValueChanged (Form4) : on garde le volume de la ligne.
            Dim volume As Object = Form4.DataGridView1.Rows(r).Cells("Volume").Value
            Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
            Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
            cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            cb.Items.Clear()
            cb.Items.AddRange(Vc.ToArray())
            Dim voulue As String = If(action, "")
            If voulue = "" Then voulue = Vc(0)
            If Not cb.Items.Contains(voulue) Then cb.Items.Add(voulue)     ' valeur enregistree absente de la liste actuelle : jamais perdue
            cb.Value = voulue
            If volume IsNot Nothing AndAlso CStr(volume) <> "" Then Form4.DataGridView1.Rows(r).Cells("Volume").Value = volume
        Catch ex As Exception
            DiagLog("Minuterie : liste des actions de la ligne " & r & " (" & type & ") : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Le type "Deezer" vient d'etre choisi pour la ligne : liste des actions, sur
    ''' "Lance Deezer" ; le volume et la playlist se reglent d'apres l'action (Form4).</summary>
    Sub Choix_Deezer(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Volume").Value = ""
        ListeDesActionsDeezer(CInt(r), ActionsDeezer(0))
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Type "Radio" de la minuterie (20/10/2026) : la case Action propose les radios de la liste (cadre Radio, onglet Audio) ;
    ' a l'heure de la ligne, la musique devient cette station (execution : Module1, comme "Mp3 to Radio" mais sur la station choisie).
    ' ------------------------------------------------------------------------------------------

    ' ------------------------------------------------------------------------------------------
    ' Colonne Volume de la minuterie (20/10/2026 : "souvent renseignee a 100 : qu'elle ne le soit que si c'est utile") : un volume n'a
    ' de sens que pour une video/chaine (Film, TV, Sat, IPTV), les volumes de Slide (Music/Video Slide) et le Deezer lance ou regle.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Vrai si la colonne Volume sert pour cette tache. Type ou action encore vide : vrai (on ne touche a rien).</summary>
    Function VolumeUtilePour(type As String, action As String) As Boolean
        Dim t As String = UCase(If(type, "").Trim()), a As String = UCase(If(action, "").Trim())
        If t = "" OrElse a = "" Then Return True
        Select Case t
            Case "FILM" : Return True
            Case "TV" : Return a <> "CHAINE OFF"
            Case "IPTV" : Return a <> "IPTV OFF"
            Case "VOLUME" : Return a = "VOLUME MUSIC SLIDE" OrElse a = "VOLUME VIDEO SLIDE" OrElse a = "VOLUME GENERAL" OrElse a = "VOLUME DEEZER"
            Case "DEEZER" : Return a = "LANCE DEEZER" OrElse a = "DEEZER FAVORIS" OrElse a = "VOLUME DEEZER"
            Case "MUSIC SOURCE" : Return a = "DEEZER"
            Case "DIVERS" : Return a = "LANCE DEEZER"
        End Select
        If t.StartsWith("SAT") Then Return a <> "SAT OFF"
        Return False
    End Function

    ''' <summary>Le type "Yom Tov" vient d'etre choisi : la fenetre des fetes s'ouvre ; l'entree et la sortie des fetes cochees sont inserees dans la minuterie
    ''' (la premiere annonce prend cette ligne). Annuler : la ligne reste, inactive, sans date (elle n'agit pas).</summary>
    Sub Choix_YomTov(r)
        If r < 0 OrElse r >= Form4.DataGridView1.Rows.Count Then Exit Sub
        Dim ligne As DataGridViewRow = Form4.DataGridView1.Rows(r)
        If ligne.IsNewRow Then Exit Sub                       ' (la ligne vierge du bas : rien a remplir)
        ligne.Cells("Play").Value = ""
        ligne.Cells("Play").ToolTipText = ""
        ligne.Cells(0).ToolTipText = ""
        ligne.Cells("Durée").Value = ""
        ligne.Cells("Argument").Value = ""
        ligne.Cells("Volume").Value = ""
        ListeDesActionsDuType(CInt(r), "Yom Tov", "Entrée de fête")
        ' Fenetre annulee : la ligne reste, decochee (elle n'a ni fete ni reglage). Validee (fetes ou 2 lignes a regler a la main) : la ligne est utilisee.
        If Not FormYomTov.Choisir(Form4, Nothing, CInt(r)) AndAlso Not ligne.IsNewRow Then ligne.Cells("Actif").Value = False
    End Sub

    ''' <summary>Clic droit "Modifier les options" sur une ligne Yom Tov : la fenetre des fetes (les annonces choisies sont ajoutees ; la ligne reste).</summary>
    Sub ModifierLaLigneYomTov(r As Integer)
        FormYomTov.Choisir(Form4, Nothing, -1)
    End Sub

    ''' <summary>Le type "Autres" vient d'etre choisi : reglages qui n'ont pas de type propre (mode hors ligne, controle a distance, mises a jour).</summary>
    Sub Choix_Autres(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Volume").Value = ""
        Dim actions As List(Of String) = ReglagesMinuterie.Actions(ReglagesMinuterie.TypeAutres)
        ListeDesActionsDuType(CInt(r), "Autres", actions(0))
        ' La fenetre de choix multiples s'ouvre tout de suite (annuler : le premier reglage de la liste).
        Dim nouvelle As String = ""
        If FormReglages.Demander(Form4, "Autres", Nothing, "", nouvelle) Then ListeDesActionsDuType(CInt(r), "Autres", nouvelle)
    End Sub

    ''' <summary>Actions simples deja proposees par les types Photo Mode et Video Mode (la fenetre de choix multiples les reprend).</summary>
    Friend ReadOnly ActionsPhotoModeBase As String() = {"Décroissant", "Croissant", "Aléatoire Classic", "Aléatoire Séquentiel", "Aléatoire OFF", "Reprendre les Photos depuis le debut"}
    Friend ReadOnly ActionsVideoModeBase As String() = {"Décroissant", "Croissant", "Aléatoire ON", "Aléatoire OFF", "Reprendre les Videos depuis le debut", "Video avec audio ON", "Video avec audio OFF"}

    ''' <summary>Clic droit "Modifier les options" sur une ligne Photo Mode, Video Mode ou Autres : la fenetre de choix multiples, sur l'action actuelle.</summary>
    Sub ModifierLaLigneReglages(r As Integer, type As String)
        Dim statiques As String() = If(type = "Photo Mode", ActionsPhotoModeBase, If(type = "Video Mode", ActionsVideoModeBase, Nothing))
        Dim actuelle As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").FormattedValue)
        Dim nouvelle As String = ""
        If FormReglages.Demander(Form4, type, statiques, actuelle, nouvelle) Then ListeDesActionsDuType(r, type, nouvelle)
    End Sub

    ''' <summary>Vide la case Volume de la ligne si elle ne sert pas pour sa tache.</summary>
    Sub NettoyerLeVolume(ligne As DataGridViewRow)
        Try
            If ligne Is Nothing OrElse ligne.IsNewRow Then Exit Sub
            If CStr(If(ligne.Cells("Volume").Value, "")).Trim() = "" Then Exit Sub
            If Not VolumeUtilePour(CStr(If(ligne.Cells("Type").Value, "")), CStr(If(ligne.Cells("Action").Value, ""))) Then ligne.Cells("Volume").Value = ""
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Les noms des radios de la liste, dans l'ordre de la liste.</summary>
    Function NomsDesRadios() As List(Of String)
        Dim noms As New List(Of String)
        For i As Integer = 0 To UBound(Radio, 1)
            Dim nom As String = If(Radio(i, 0), "").Trim()
            If nom <> "" AndAlso If(Radio(i, 1), "").Trim() <> "" AndAlso Not noms.Contains(nom) Then noms.Add(nom)
        Next
        Return noms
    End Function

    ''' <summary>L'adresse de la radio de ce nom dans la liste ("" si elle n'y est plus).</summary>
    Function UrlDeLaRadio(nom As String) As String
        For i As Integer = 0 To UBound(Radio, 1)
            If If(Radio(i, 0), "").Trim().Equals(If(nom, "").Trim(), StringComparison.OrdinalIgnoreCase) AndAlso If(Radio(i, 1), "").Trim() <> "" Then
                Return Radio(i, 1).Trim()
            End If
        Next
        Return ""
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Type "Son" de la minuterie (20/10/2026) : les reglages du cadre Son de l'onglet Audio (Form4) : video avec le son, musique,
    ' sources cochees (Fichiers MP3 / Radio / Deezer, plusieurs = alternance) et duree de l'alternance.
    ' ------------------------------------------------------------------------------------------

    Friend ReadOnly ActionsSon As String() = {
        "Video avec son ON", "Video avec son OFF", "Musique ON", "Musique OFF",
        "Sources : MP3", "Sources : Radio", "Sources : Deezer",
        "Sources : MP3 + Radio", "Sources : MP3 + Deezer", "Sources : Radio + Deezer", "Sources : MP3 + Radio + Deezer",
        "Alterner toutes les 1 min", "Alterner toutes les 2 min", "Alterner toutes les 5 min", "Alterner toutes les 10 min",
        "Alterner toutes les 15 min", "Alterner toutes les 30 min", "Alterner toutes les 60 min"}

    Sub Choix_Son(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        ListeDesActionsDuType(CInt(r), "Son", ActionsSon(0))
        ' La fenetre des reglages s'ouvre tout de suite : plusieurs choix dans la meme ligne (annuler : la premiere action de la liste).
        Dim nouvelle As String = ""
        If FormSon.Demander(Form4, "", nouvelle) Then ListeDesActionsDuType(CInt(r), "Son", nouvelle)
    End Sub

    ''' <summary>Clic droit "Modifier les options" sur une ligne Son : la fenetre des reglages, sur l'action actuelle de la ligne.</summary>
    Sub ModifierLaLigneSon(r As Integer)
        Dim actuelle As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").FormattedValue)
        Dim nouvelle As String = ""
        If FormSon.Demander(Form4, actuelle, nouvelle) Then ListeDesActionsDuType(r, "Son", nouvelle)
    End Sub

    ''' <summary>Execute une action du type "Son" (appelee a l'heure de la ligne). Memes effets que les cases du cadre Son suivies de OK.</summary>
    Sub TacheSon(action As String)
        Try
            ' Plusieurs reglages dans une ligne, separes par ";" : la musique est appliquee en dernier (apres le choix des sources).
            Dim morceaux As List(Of String) = If(action, "").Split(";"c).Select(Function(m) m.Trim()).Where(Function(m) m <> "").ToList()
            If morceaux.Count > 1 Then
                ' ("Radio : station" avant "Sources" : si la radio devient la source, c'est deja la bonne station qui demarre.)
                For Each m As String In morceaux.Where(Function(x) UCase(x).StartsWith("RADIO :") OrElse UCase(x).StartsWith("DEEZER :"))
                    TacheSon(m)
                Next
                For Each m As String In morceaux.Where(Function(x) Not UCase(x).StartsWith("MUSIQUE ") AndAlso Not UCase(x).StartsWith("RADIO :") AndAlso Not UCase(x).StartsWith("DEEZER :"))
                    TacheSon(m)
                Next
                For Each m As String In morceaux.Where(Function(x) UCase(x).StartsWith("MUSIQUE "))
                    TacheSon(m)
                Next
                Exit Sub
            End If
            Dim a As String = UCase(If(action, "").Trim())
            Select Case a
                Case "VIDEO AVEC SON ON": VideoWithAudio = True : My.Settings.VideoWithAudio = True
                Case "VIDEO AVEC SON OFF" : VideoWithAudio = False : My.Settings.VideoWithAudio = False
                Case "MUSIQUE ON" : Music = True : Music_SWITCH_ON(Music)
                Case "MUSIQUE OFF" : Music = False : Music_SWITCH_ON(Music)
                Case Else
                    If a.StartsWith("DEEZER :") Then
                        ' La playlist de Deezer (adresse, "flow" ou "favoris") : imposee jusqu'a la prochaine tache Deezer / Son.
                        Dim adresse As String = MusiqueDeezer.AdresseDeezer(If(action, "").Trim().Substring("Deezer :".Length).Trim())
                        If adresse = "" Then
                            DiagLog("Minuterie : playlist Deezer invalide dans la tache Son (" & action & "), ignoree")
                        ElseIf Music AndAlso Music_Deezer Then
                            DeezerParLaMinuterie(Nothing, adresse, True)             ' Deezer joue deja : il change de playlist maintenant
                        Else
                            DeezerPlaylistParMinuterie = adresse                   ' (utilisee quand Deezer deviendra la source)
                        End If
                    ElseIf a.StartsWith("RADIO :") Then
                        ' La station a jouer quand la source est la radio (celle de la liste des radios, par son nom).
                        Dim url As String = UrlDeLaRadio(If(action, "").Trim().Substring("Radio :".Length).Trim())
                        If url = "" Then
                            DiagLog("Minuterie : radio '" & action & "' absente de la liste, ignoree")
                        Else
                            RadioUrl = url
                            If Music AndAlso Music_Radio Then Music_SWITCH_ON(True)          ' (change de station si la radio joue deja)
                        End If
                    ElseIf a.StartsWith("SOURCES :") Then
                        Dim cochees As New List(Of String)
                        Dim liste As String = a.Substring("SOURCES :".Length)
                        If liste.Contains("MP3") Then cochees.Add("mp3")
                        If liste.Contains("RADIO") Then cochees.Add("radio")
                        If liste.Contains("DEEZER") Then cochees.Add("deezer")
                        AlternanceMusique.AppliquerLesSources(cochees)
                    ElseIf a.StartsWith("ALTERNER TOUTES LES ") Then
                        AlternanceMusique.AppliquerLaDuree(CInt(Val(a.Substring("ALTERNER TOUTES LES ".Length))))
                    ElseIf ReglagesMinuterie.Executer(ReglagesMinuterie.TypeSon, action) Then
                        ' (pochette de l'album : voir ReglagesMinuterie)
                    Else
                        DiagLog("Minuterie : action Son inconnue '" & action & "'")
                    End If
            End Select
        Catch ex As Exception
            DiagLog("Minuterie : tache Son '" & action & "' : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Le type "Radio" vient d'etre choisi pour la ligne : liste des radios, sur celle des reglages (cadre Radio) si elle y est.</summary>
    Sub Choix_Radio(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
        Dim noms As List(Of String) = NomsDesRadios()
        Dim voulue As String = ""
        For i As Integer = 0 To UBound(Radio, 1)
            If If(Radio(i, 1), "").Trim().Equals(If(RadioUrl, "").Trim(), StringComparison.OrdinalIgnoreCase) Then voulue = If(Radio(i, 0), "").Trim() : Exit For
        Next
        If voulue = "" AndAlso noms.Count > 0 Then voulue = noms(0)
        If noms.Count = 0 Then
            MessageBox.Show("La liste des radios est vide : ajoutez-en dans l'onglet Audio (cadre Radio).", "Radio", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        ListeDesActionsDuType(CInt(r), "Radio", voulue)
    End Sub

    Sub Choix_MusicSource(r)
        Form4.DataGridView1.Rows(r).Cells("Action").Value = ""

        Dim Vc As String() = ActionsMusicSource

        Form4.DataGridView1.Rows(r).Cells("Action") = New DataGridViewComboBoxCell
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Action"), DataGridViewComboBoxCell)
        cb.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        cb.Items.Clear()
        cb.Items.AddRange(Vc)
        cb.Value = Vc(1)
        Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Play").ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = ""
        Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = ""
    End Sub

    ''' <summary>Actions du type "Music Source" (20/10/2026 : "la fonction est incomplete") : tous les passages d'une source a l'autre
    ''' (Fichiers MP3, Radio, Deezer), la source suivante parmi celles cochees, la station suivante, et la musique en marche / arretee.</summary>
    Friend ReadOnly ActionsMusicSource As String() = {
        "Mp3 to Radio", "Radio to Mp3", "Mp3 to Deezer", "Radio to Deezer", "Deezer to Mp3", "Deezer to Radio",
        "Source suivante", "Radio suivante", "Music OFF", "Music ON"}

    Sub ListChaineSAT()

        Nom = AppPath & "\Data\ListChannelSATdefault.HTML"

        'Exit Sub

        Try
            ' (Attente plafonnee a 0,3 s au lieu de 1 s : sur le reseau de la maison, le recepteur repond
            ' en quelques millisecondes ou pas du tout ; eteint, il retardait chaque lancement de Slide.)
            If My.Computer.Network.Ping(Dreambox_IP, 300) Then

                Dim wb As New WebClient
                Dim infoReader As System.IO.FileInfo
                url = "http://" & Dreambox_IP & "/web/getservices?sRef=1:7:1:0:0:0:0:0:0:0:FROM BOUQUET " + Chr(34) + "userbouquet.favourites.tv" + Chr(34) + "ORDER BY bouquet" 'pour avoir list des chaines
                Nom = AppPath & "\Data\ListChannelSAT.HTML"
                infoReader = My.Computer.FileSystem.GetFileInfo(Nom)

                If Format(infoReader.LastWriteTime, "dd/MM/yyyy") <> Format(Now, "dd/MM/yyyy") Then
                    Dim html As String = wb.DownloadString(url)
                    File.WriteAllText(Nom, html)
                End If

            End If

        Catch ex As Exception

        End Try


        ChannelIDsatTotal = 1
        ChannelIDsat(ChannelIDsatTotal, 0) = "FLUX RESEAUX"
        ChannelIDsat(ChannelIDsatTotal, 1) = "FLUX RESEAUX"

        Dim elm As XElement = XElement.Load(Nom)

        If elm IsNot Nothing Then

            For Each page In elm.Elements("e2service")
                servicereference = page.Element("e2servicereference").Value
                servicename = page.Element("e2servicename").Value
                If Left(servicereference, 5) <> "1:64:" Then
                    ChannelIDsatTotal = ChannelIDsatTotal + 1
                    ChannelIDsat(ChannelIDsatTotal, 0) = servicereference
                    ChannelIDsat(ChannelIDsatTotal, 1) = servicename
                End If

            Next
        End If
        ChannelIDsatTotal = ChannelIDsatTotal + 1
        'For i = 1 To ChannelIDsatTotal
        ' Debug.Print(ChannelIDsat(i, 0) & " " & ChannelIDsat(i, 1))
        'Next

    End Sub




    Function ProchainJourDeLaSemaine(j) As Date
        'Dim heure As Date = Form4.DataGridView1.Rows(r).Cells("Heure").Value
        t = 0
        If j = "LUNDI" Then t = DayOfWeek.Monday
        If j = "MARDI" Then t = DayOfWeek.Tuesday
        If j = "MERCREDI" Then t = DayOfWeek.Wednesday
        If j = "JEUDI" Then t = DayOfWeek.Thursday
        If j = "VENDREDI" Then t = DayOfWeek.Friday
        If j = "SAMEDI" Then t = DayOfWeek.Saturday
        If j = "DIMANCHE" Then t = DayOfWeek.Sunday


        'For i = 0 To 6
        ' db = New DateTime(Now.Year, Now.Month, Now.Day + i, 1, 1, 0)
        ' If db.dayofweek = t Then ProchainJourDeLaSemaine = db : Exit For
        'Next

        For i = 0 To 6
            If Now().AddDays(i).DayOfWeek = t Then
                ProchainJourDeLaSemaine = Now().AddDays(i)
                Exit For
            End If
        Next

        Return ProchainJourDeLaSemaine
    End Function

    ''' <summary>Durees des videos deja lues (chemin -> "hh:mm:ss").</summary>
    Private ReadOnly _durees As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

    Function GetDurationOK(ByVal MovieFullPath As String) As String

        If File.Exists(MovieFullPath) Then
            ' Duree deja lue : tout de suite (elle etait lue deux fois par video).
            Dim connue As String = Nothing
            If _durees.TryGetValue(MovieFullPath, connue) Then Return connue
            Dim objShell As Object = CreateObject("Shell.Application")
            Dim objFolder As Object =
         objShell.Namespace(Path.GetDirectoryName(MovieFullPath))
            ' ParseName va droit au fichier. Avant : TOUT le dossier etait parcouru pour le trouver
            ' (des centaines de rushes du mariage = une attente avant chaque video).
            Dim element As Object = objFolder.ParseName(Path.GetFileName(MovieFullPath))
            If element Is Nothing Then Return ""
            result = objFolder.GetDetailsOf(element, 27).ToString
            If Len(result) > 8 Then result = ""
            If result <> "" Then _durees(MovieFullPath) = result
            Return result
        Else
            Return ""
        End If

    End Function

    ''' <summary>Duree d'un media. Passait par le lecteur Windows Media (composant COM wmp.dll,
    ''' absent des editions N de Windows et plus developpe par Microsoft) : c'est desormais VLC
    ''' qui la donne, comme partout ailleurs dans le programme.</summary>
    Function GetDuration2(ByVal MovieFullPath As String) As String
        Return GetDuration3(MovieFullPath)
    End Function
    Function GetDuration3(ByVal MovieFullPath As String) As String

        Dim Duration As String = ""


        PreparerLeVolumeAvantChangement(Form3.VlcControl1, 100)
        Form3.VlcControl1.SetMedia(New Uri("file:///" & MovieFullPath))
        CouperLeSonAvantDemarrage(Form3.VlcControl1)
        Form3.VlcControl1.Play()
        wait(1)
        NbSeconds = Form3.VlcControl1.Length / 1000

        Form3.VlcControl1.Pause()

        Dim tsSeconds = TimeSpan.FromSeconds(CDbl(NbSeconds))
        Duration = tsSeconds.ToString("hh\:mm\:ss")

        Return Duration

    End Function



    Function GetDuration(ByVal MovieFullPath As String) As String
        If MovieFullPath = "" Then
            Return "00:00:00"
        End If

        er = 0 : D1 = 0
        MyDuration = 0

        Dim Duration As String = ""
        Application.DoEvents()

        Try
            Duration = GetDurationOK(MovieFullPath)

        Catch ex As Exception
            Duration = "00:00:00"
            er = 1
        End Try



        If er = 1 Or Duration = "" Then 'si Duration="" cad impossible à calculer 

            Try
                Duration = GetDuration3(MovieFullPath)

            Catch ex As Exception
                Duration = "00:00:00"
            End Try

        End If

fin:
        Return Duration

    End Function

    Function GetDurationOK2(ByVal MovieFullPath As String) As String

        Dim Duration As String = ""
        Duration = GetDurationOK(MovieFullPath)
        MyDuration = 0

        Try
            MyDuration = TimeSpan.ParseExact(Duration, "c", Nothing).TotalSeconds
        Catch ex As Exception
        End Try

        Application.DoEvents()

        If MyDuration < MinimumDurréeVideoAcceptable Then GetDurationOK2 = "" : Exit Function

        If MyDuration = 0 Then
            Dim aviFile As MediaFile = New MediaFile(MovieFullPath)
            D = aviFile.duration / 1000

            If D > 0 Then
                Dim tsSeconds = TimeSpan.FromSeconds(CDbl(D))
                Duration = tsSeconds.ToString("hh\:mm\:ss")
            End If

        End If

        If Duration = "" Or Duration = "00:00:00" Then
            Duration = GetDuration2(MovieFullPath)
        End If


        If Duration = "" Or Duration = "00:00:00" Then SaveLogError(MovieFullPath, "Duration impossible à trouver")

        Return Duration

    End Function

    Sub PreviewFilm(r As Integer, Optional Nom As String = "", Optional Titre As String = "")
        LigneMinuterieEnCours.Index = CInt(r)
        If Nom = "" Then
            Nom = Form4.DataGridView1.Rows(r).Cells("Action").Value
            ' Film choisi dans Nflix (IPTV) : la colonne Action n'a que le TITRE, l'adresse du film est dans les reglages
            ' de la tache ("!/Url:"). Sans elle, PLAY cherchait un fichier du nom du titre : bip, et la fenetre de
            ' reglage (PIP, titre, volume...) ne s'ouvrait pas, donc le film ne pouvait plus etre edite.
            Dim adresseIptv As String = Convert.ToString(Arg("Url", Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Argument").Value)))
            If adresseIptv <> "" Then Nom = adresseIptv
        Else
            If Form4.DataGridView1.Rows(r).Cells("Argument").Value = "" Then
                Form4.DataGridView1.Rows(r).Cells("Argument").Value = "!/Titre:" & Titre
            End If

        End If


        If InStr(UCase(Nom), "WWW.PRIMEVIDEO.COM") > 0 Or InStr(UCase(Nom), "WWW.NETFLIX.COM") > 0 Then
            SetVideoVolSlide(0)
            SetMusicVolSlide(0)
            FlagExit = False
            Application.DoEvents()
            Dim startInfo As New ProcessStartInfo()
            Dim myprocess As New Process()
            startInfo.FileName = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe "
            startInfo.Arguments = Nom & " -new-window"
            myprocess.StartInfo = startInfo
            myprocess.Start()
            SetVol(VolumeTVParDefault)
            Exit Sub
        End If

        trouve = 0
        trouve2 = 0
        dejaPasséParLa = 0

        If InStr(Nom, "#") > 0 Then
            For y = Len(Nom) To 1 Step -1
                If Mid(Nom, y, 1) = "\" Then trouve = y : Exit For
            Next
            'SourceDir = Left(Nom, trouve - 1)
            SourceDir = Nom.Substring(0, trouve - 1)
            trouve2 = InStr(Nom, "*")
            'mot = Mid(Nom, trouve + 3, trouve2 - trouve - 3)
            mot = Nom.Substring(trouve + 2, trouve2 - trouve - 3)
            'ext = Mid(Nom, trouve2)
            ext = Nom.Substring(trouve2 - 1)
            Nom = ReplayCherche(SourceDir, mot, ext)
        End If


        Argument = Form4.DataGridView1.Rows(r).Cells("Argument").Value

        Try
            PIP = Arg("Pip", Argument)
            If PIP = "" Then
                PIP = False
                Pipsize = PipSizeDefault
            End If

            If PIP <> False Then
                Pipsize = LireTaillePip(Argument)
                PipPosition = Arg("PipPosition", Argument)
                If PipPosition = "" Then PipPosition = "Bdroite"
            End If
        Catch ex As Exception
            PIP = False
            Pipsize = PipSizeDefault
            PipPosition = "Bdroite"
        End Try




        Form6.CheckBox4.Checked = PIP
        Form6.TrackBar3.Value = LireTaillePip(Argument)   ' aussi quand le PIP est decoche (Pipsize n'est alors pas relu)
        If PipPosition = "Bdroite" Then Form6.RadioButton6.Checked = True
        If PipPosition = "Hdroite" Then Form6.RadioButton5.Checked = True
        If PipPosition = "Bgauche" Then Form6.RadioButton4.Checked = True
        If PipPosition = "Hgauche" Then Form6.RadioButton3.Checked = True
        If PipPosition = "Mgauche" Then Form6.RadioButton7.Checked = True
        If PipPosition = "Mdroite" Then Form6.RadioButton8.Checked = True
        Form6.CheckBoxSuite.Checked = UCase(Convert.ToString(Arg("Suite", Argument))) = "TRUE"
        Form6.CheckBoxSuite.Enabled = Not Form6.CheckBox4.Checked



        Form6.Label11.Text = Form6.TrackBar3.Value
        ' Lire en boucle : le film repart au debut quand il se termine normalement (PlayFILM_VLC_P2).
        ' En standard (02/10, demande de Jacob) : cochee pour un film en PIP, decochee sinon. Une
        ' tache deja enregistree garde son choix ; cocher ou decocher "PIP" dans la fenetre regle
        ' la case en consequence (Form6.CheckBox4_CheckedChanged).
        Dim boucleEnregistree As String = UCase(CStr(Arg("Boucle", Argument)))
        Form6.CheckBoxBoucle.Checked = If(boucleEnregistree = "", Form6.CheckBox4.Checked, boucleEnregistree = "TRUE")
        ' "Rester affiche" n'a de sens que pour un PIP : decochee et grisee si PIP est decoche.
        Form6.CheckBoxPipPersistant.Checked = Form6.CheckBox4.Checked AndAlso (UCase(Arg("PipPersistant", Argument)) = "TRUE")
        Form6.CheckBoxPipPersistant.Enabled = Form6.CheckBox4.Checked



        Form6.VlcControl1.Visible = True
        Form6.GroupBox2.Top = Form6.VlcControl1.Height
        Form6.GroupBox2.Left = 0
        Form3.VlcControl1.Audio.IsMute = True
        Application.DoEvents()


        FlagStop = True

        MusiqueVLC.MusiquePause()
        ClosePlayer()


        If Left(UCase(Nom), 4) <> "HTTP" Then
            ' Dernier recours : l'info-bulle de la case Action garde l'adresse d'un film IPTV (posee par Nflix).
            Dim bulle As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").ToolTipText)
            If Left(UCase(bulle), 4) = "HTTP" Then Nom = bulle
        End If
        If Left(UCase(Nom), 4) <> "HTTP" Then
            If System.IO.File.Exists(Nom) = False Then
                DiagLog("Apercu film : ni fichier ni adresse web pour la ligne " & (r + 1) & " : « " & Nom & " » ; reglages=" & Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Argument").Value))
                Console.Beep()
                Exit Sub
            End If
        End If


        Form6.StartPosition = FormStartPosition.CenterScreen
        Form6.Show()
        Form6.TopMost = True

        Titre = Arg("Titre", Argument)
        ' Pas de titre enregistre : la case "Changer Titre" propose celui du film, c'est-a-dire le
        ' nom du fichier sans son dossier ni son extension (demande de Jacob, 06/10). Il n'est
        ' enregistre dans la tache que si l'on valide par OK. Adresse Internet : case laissee vide.
        ' La case reste modifiable : on peut y taper un autre titre avant de valider.
        ' ("Titre" garde le titre ENREGISTRE : la suite le compare a la colonne Play.)
        Dim titrePropose As String = Titre
        ' Titre recopie d'un AUTRE film (voir TitreDUnAutreFilm) : on propose celui de ce film-ci.
        If TitreDUnAutreFilm(Form4.DataGridView1, CInt(r), Convert.ToString(Titre)) Then
            DiagLog("Preview : le titre enregistre """ & Convert.ToString(Titre) & """ est celui d'un autre film de la minuterie -> titre du fichier propose")
            titrePropose = ""
        End If
        If Trim(titrePropose) = "" AndAlso InStr(Nom, "://") = 0 Then
            Try
                titrePropose = IO.Path.GetFileNameWithoutExtension(CStr(Nom))
            Catch ex As Exception
                titrePropose = ""
            End Try
        End If
        Form6.TextBox2.ReadOnly = False
        Form6.TextBox2.Enabled = True
        Form6.TextBox2.Text = titrePropose
        AudioLanguage = Arg("AudioLanguage", Argument)
        SubTitle = Arg("SubTitle", Argument)
        Pub = Arg("Pub", Argument) : Form6.TextBox3.Text = Pub
        AL = UCase(Arg("ModeHTTP", Argument))
        If AL = "" Or AL = "FALSE" Then ModeHTTP = False Else ModeHTTP = True
        Form6.CheckBox2.Checked = ModeHTTP


        If InStr(UCase(Nom), recepteurSat) > 0 And ModeHTTP = True Then
            Nom = Nom.Replace(recepteurSat & "Harddisk", "/hdd")
            Nom = Nom.Replace("\", "/")
            Nom = "http://" & Dreambox_IP & ":80/file?file=" & Nom
        End If



        ext = "file:///" : If Left(UCase(Nom), 4) = "HTTP" Then ext = ""

        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        vPlayer = Form6.VlcControl1
        ' Le volume de la ligne est lu plus bas : ici on le reprend directement, sinon on
        ' prereglerait le lecteur avec le volume de l'apercu precedent.
        PreparerLeVolumeAvantChangement(vPlayer, Val(Form4.DataGridView1.Rows(r).Cells("Volume").FormattedValue))
        vPlayer.SetMedia(New Uri(ext & Nom))     ' (option1/option2 : variables locales vides, retirees)


        CouperLeSonAvantDemarrage(vPlayer)
        vPlayer.Play()

        Dim cb2 As DataGridViewButtonCell = DirectCast(Form4.DataGridView1.Rows(r).Cells("Play"), DataGridViewButtonCell)
        If cb2.Value = "" Then cb2.Value = GetDuration(Nom)

        trouve = InStr(cb2.Value, "-")

        vol = Form4.DataGridView1.Rows(r).Cells("Volume").FormattedValue

        Form6.ComboBox1.Items.Clear()                  ' (s'allongeait a chaque apercu)
        For i = 0 To 100
            Form6.ComboBox1.Items.Add(i)
        Next
        Form6.ComboBox1.SelectedIndex = vol
        Form6.TextBox1.Text = Form4.DataGridView1.Rows(r).Cells("Durée").Value

        Form6.PipMuetDemande = False                   ' (les cases cochees a l'ouverture ne comptent pas : seul un clic sur PIP pendant le chargement du film)
        j = 0 : trouve2 = 0
        Do Until Form6.VlcControl1.IsPlaying = True
            Application.DoEvents()
            wait(1)
            j = j + 1 : If j > 25 Then trouve2 = 1 : Exit Do
        Loop

        ' La lecture est partie : volume voulu, puis son retabli (coupe avant le demarrage).
        SetVolume(vol, vPlayer)

        If ext = "" Then Form6.Button1.PerformClick()

        If trouve > 0 Then
            Form6.MaskedTextBox1.Text = Trim(Left(cb2.Value, trouve - 1))
            Form6.MaskedTextBox2.Text = Trim(Mid(cb2.Value, trouve + 1))
            Dim Minute = TimeSpan.ParseExact(Form6.MaskedTextBox1.Text, "c", Nothing).TotalMinutes

            If Right(UCase(Nom), 3) = ".TS" Then
                yu = 0 : erreur = 0
                Do Until Form6.VlcControl1.Length <> 0
                    wait(1)
                    yu = yu + 1 : If yu > 10 Then erreur = 1 : Exit Do
                Loop
                If erreur = 1 Then
                    Beep()
                    Exit Sub
                End If
                ppp = Minute * 60 * 1000 / Form6.VlcControl1.Length
                Form6.VlcControl1.Position = ppp

            Else
                Form6.VlcControl1.Time = Minute * 60 * 1000
            End If


        Else
            Form6.MaskedTextBox1.Text = "00:00:00"

            If cb2.Value <> "00:00:00" And cb2.Value <> Titre Then Form6.MaskedTextBox2.Text = cb2.Value

        End If
        Try



            Form6.ComboBox2.Items.Clear()          ' pistes audio et sous-titres : listes videes (elles
            Form6.ComboBox3.Items.Clear()          ' cumulaient les pistes des apercus precedents)
            nbTracks = vPlayer.Audio.Tracks.Count
            For i = 0 To nbTracks - 1
                Form6.ComboBox2.Items.Add(vPlayer.Audio.Tracks.All(CInt(i)).Name)
            Next

            nbSubs = vPlayer.SubTitles.Count
            For i = 0 To nbSubs - 1
                Form6.ComboBox3.Items.Add(vPlayer.SubTitles.All(CInt(i)).Name)
            Next

            If IsNumeric(AudioLanguage) Then
                Form6.ComboBox2.SelectedIndex = AudioLanguage
            End If
            If IsNumeric(SubTitle) Then
                Form6.ComboBox3.SelectedIndex = SubTitle
            End If


            If Form6.PipMuetDemande Then vol = 0           ' PIP coche pendant le chargement : le volume reste a 0 (22/10/2026)
            Form6.TrackBar2.Value = vol
            Form6.ComboBox1.Text = Form6.TrackBar2.Value.ToString
            pos1 = Form6.VlcControl1.Time / 1000
            Form6.Label8.Text = TimeSpan.FromSeconds(CDbl(pos1)).ToString("hh\:mm\:ss")
            pos2 = Form6.VlcControl1.Length / 1000
            Form6.Label9.Text = TimeSpan.FromSeconds(CDbl(pos2)).ToString("hh\:mm\:ss")
            Form6.TrackBar1.Maximum = pos2 '/ 60
            Form6.TrackBar1.Value = pos1 '/ 60
            Form6.GroupBox2.Visible = True
            Form6.GroupBox2.BringToFront()

        Catch ex As Exception

        End Try


        SetVolume(vol, Form6.VlcControl1)

        'wait(2)




    End Sub


    Sub PreviewTV(n, vol)
        ' La TV passe toujours par le lecteur VLC integre a Slide. (Le lecteur de TV de Windows puis le
        ' VLC externe ont ete retires le 13/10/2026.)
        PreviewTV_VLC(n, vol)
    End Sub




    Sub PreviewSat(n As Integer, vol As Integer, r As Integer)

        ClosePlayer()
        Form11.Text = "Preview SAT"
        Form11.StartPosition = FormStartPosition.CenterScreen
        Form11.Show()
        Form11.TopMost = True

        r = clickedCell.RowIndex
        If r = -1 Then r = LigneMinuterieEnCours.Index

        Argument = Form4.DataGridView1.Rows(r).Cells("Argument").Value

        Dim pip As Boolean
        Try
            pip = Arg("Pip", Argument)
            Pipsize = LireTaillePip(Argument)
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
        Catch ex As Exception
            pip = False
            Pipsize = PipSizeDefault
            PipPosition = "Bdroite"
        End Try


        Form11.CheckBox4.Checked = pip
        ' "Rester affiche" n'a de sens que pour un PIP : decochee et grisee si PIP est decoche.
        Form11.CheckBoxPipPersistant.Checked = Form11.CheckBox4.Checked AndAlso (UCase(Arg("PipPersistant", Argument)) = "TRUE")
        Form11.CheckBoxPipPersistant.Enabled = Form11.CheckBox4.Checked
        Form11.TrackBar3.Value = Pipsize
        Form11.Label11.Text = Pipsize

        If PipPosition = "Bdroite" Then Form11.RadioButton6.Checked = True
        If PipPosition = "Hdroite" Then Form11.RadioButton5.Checked = True
        If PipPosition = "Bgauche" Then Form11.RadioButton4.Checked = True
        If PipPosition = "Hgauche" Then Form11.RadioButton3.Checked = True
        If PipPosition = "Mgauche" Then Form11.RadioButton7.Checked = True
        If PipPosition = "Mdroite" Then Form11.RadioButton8.Checked = True
        Form11.CheckBoxSuite.Checked = UCase(Convert.ToString(Arg("Suite", Argument))) = "TRUE"
        Form11.CheckBoxSuite.Enabled = Not Form11.CheckBox4.Checked


        ID = ChannelID(n, 0)
        If ID = "" Then Exit Sub

        'options = ":dvb-frequency=" & ID & "--deinterlace-mode=discard"
        'Form11.VlcControl1.VlcMediaplayerOptions = {"--deinterlace-mode=discard"}
        'Form11.VlcControl1.SetMedia("dvbt://", options)
        p = InStr(ID, ":")
        If p = 0 Then Exit Sub

        If n = 1 Then
            Nom = "http://" & FluxReseaux_IP & ":8080/hdmi"
        Else
            Nom = "http://" & Dreambox_IP & ":8001/" & ChannelIDsat(n, 0)
        End If

        'Form11.VlcControl1.SetMedia(Nom, "--deinterlace-mode=discard", ":network-caching=10000")
        PreparerLeVolumeAvantChangement(Form11.VlcControl1, vol)
        Form11.VlcControl1.SetMedia(Nom, "--deinterlace-mode=discard")
        CouperLeSonAvantDemarrage(Form11.VlcControl1)
        Form11.VlcControl1.Play()

        ' Le reglage fait tout de suite apres Play est perdu : la sortie audio n'existe pas encore
        ' (le tuner met plusieurs secondes) et le silence demande avant le demarrage restait en
        ' place -> apercu muet. On attend donc la sortie audio avant d'appliquer le volume.
        ReglerLeSonDeLApercu(vol)

        Form11.TrackBar2.Value = vol
        Form11.Label1.Text = Form11.TrackBar2.Value.ToString

        Form11.GroupBox1.Visible = True
        Form11.GroupBox1.BringToFront()



        'Do Until Form11.VlcControl1.IsPlaying = True
        Application.DoEvents()
        'wait(1)
        'If FlagExit = True Then Exit Do
        'Debug.Print("status:" & Form11.VlcControl1.State & vbTab & Form11.VlcControl1.State.ToString)
        'Loop

        'FlagExit = False

        'Debug.Print("Sortie de la boucle")

    End Sub



    Sub PlaySAT_VLC(n As Integer, vol As Integer, Argument As String)

        If n = Nothing Then
            Beep()
            AfficheTitre(n & vbCrLf & "Chaine introuvable ...", "*")
            wait(2)

            PlanB_onError()
            Exit Sub
        End If

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then RemplacerLePipDuCoin(Argument)   ' un PIP par coin : les autres PIP restent

        Dim vPlayer, vPlayerSlideVideo As Vlc.DotNet.Forms.VlcControl
        vPlayerSlideVideo = Form3.VlcControl1
        Diffusion.Mode = "SAT-VLC_INT"
        Diffusion.Chaine = n
        Diffusion.Vol = vol
        Diffusion.Arg = Argument

        MasqueLegende()
        FlagExit = False : FlagStop = True
        If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False Then CacherSouris()
        ClosePlayer()
        Application.DoEvents()
        f3Location = Form3.Location 'x=.left y=.top
        f3Size = Form3.Size

        If PIP = True Then
            PIPvolume = vol
            vPlayer = Form12.VlcControl1
            If vol > 0 Then
                vPlayerSlideVideo.Audio.IsMute = True
                MusiqueVLC.MusiquePause()
                vPlayer.Audio.IsMute = True ' uniquement pour qu'il n'y ai pas de volume fort et ensuite le bon volume
                'SetVolume(vol, vPlayer)
            End If
            Form12.Close()
            Form1.CheckForIllegalCrossThreadCalls = False
            FlagStop = False
            TaskIsPlaying = True
            TaskIsPlayingState = True
            Form3.Timer4.Start()
            Task = New Task(Sub() PlaySAT_VLC_P2(n, vol, Argument, f3Location, f3Size))
            Task.Start()
        Else
            vPlayer = Form3.VlcControl2
            AfficheTitre(ChannelIDsat(n, 1) & vbCrLf & "en préparation ...", "*")
            vPlayerSlideVideo.Pause()
            PlaySAT_VLC_P2(n, vol, Argument, f3Location, f3Size)
        End If

    End Sub

    ''' <summary>
    ''' Taille et place du PIP (Form12), commun aux lecteurs SAT, TV, IPTV et film (chacun avait
    ''' sa copie de ce code).
    '''
    ''' Le PIP prend le FORMAT DE LA VIDEO (demande de Jacob : pas de bandes noires autour), dans
    ''' la limite de la taille reglee (<paramref name="part"/> = PipSize, part de l'ecran, puis proportionnel a la fenetre) : une
    ''' video 4:3 ou filmee en hauteur donne un PIP plus etroit, jamais plus grand. Il se colle
    ''' ensuite dans le coin choisi, a egale distance du bas (ou du haut) et du cote, et suit la fenetre.
    '''
    ''' Appele depuis le thread du lecteur : ne touche pas a Form3 (voir ZoneLegendeReservee dans
    ''' Module1), d'ou la position et la taille de la fenetre passees en parametres.
    ''' </summary>
    Sub PlacerLePip(vPlayer As Vlc.DotNet.Forms.VlcControl, part As Double, position As String,
                    f3L As Integer, f3T As Integer, f3W As Integer, f3H As Integer)
        ' Format de l'image : celui de la video, 0 si VLC ne l'a pas encore annonce (le PIP prend
        ' alors le format de la fenetre, comme avant).
        Dim formatVideo As Double = FormatDeLaVideo(vPlayer)

        ' Un film en boucle (ou un flux qui reprend) rappelle cette procedure : la taille choisie A LA SOURIS, retenue dans la session,
        ' est reprise au lieu de la taille d'origine du lancement (16/10/2026 : "apres un agrandissement a la souris certains PIP
        ' reprennent leur taille d'origine").
        Try
            Dim sessionTaille As SessionPip = _sessionDuFil
            If sessionTaille IsNot Nothing AndAlso CStr(If(Arg("PipSize", If(sessionTaille.Argument, "")), "")).Trim() <> "" Then
                part = LireTaillePip(sessionTaille.Argument) / 100.0
            End If
        Catch ex As Exception
        End Try

        ' Taille, coin et ecarts : voir Form12.PoserDansLeCoin. Puis le PIP s'accroche a la fenetre
        ' de Slide pour la suivre quand on la deplace ou la redimensionne (trouvee par
        ' FenetrePrincipale : on est dans le thread du lecteur, ou "Form3" serait une autre fenetre).
        ' Zone de l'IMAGE (zone client) de la fenetre, pas son cadre exterieur : sinon les
        ' bordures de Windows mangent l'ecart et le PIP parait colle au bord. Position et taille
        ' recues en parametres seulement si la fenetre est introuvable.
        Dim principale As Form = FenetrePrincipale()
        Dim zone As Rectangle = New Rectangle(f3L, f3T, f3W, f3H)
        Try
            If principale IsNot Nothing Then zone = principale.RectangleToScreen(principale.ClientRectangle)
        Catch ex As Exception
        End Try
        Form12.PoserDansLeCoin(zone, position, formatVideo, part)
        ' Place choisie a la souris (reglages de la tache : "!/PipX / PipY") : elle remplace le coin ; sinon le PIP reste dans son coin.
        Try
            Dim sessionPlace As SessionPip = _sessionDuFil
            Dim lx As Double, ly As Double
            If sessionPlace IsNot Nothing AndAlso LirePositionPip(sessionPlace.Argument, lx, ly) Then Form12.DefinirPlaceLibre(lx, ly) Else Form12.DefinirPlaceLibre(-1, -1)
        Catch ex As Exception
        End Try
        ' La session de ce PIP retient sa fenetre et le format de sa video : de quoi appliquer plus
        ' tard une nouvelle taille ou un nouveau coin sans le relancer (voir AjusterLePip).
        Dim session As SessionPip = _sessionDuFil
        If session IsNot Nothing Then
            session.Fenetre = Form12
            session.FormatVideo = formatVideo
        End If
        Form12.SuivreLaFenetre(principale)
        ' Premier plan, SAUF si le menu ou les reglages sont ouverts (remplace l'ancien
        ' "Form12.TopMost = True" de chaque lecteur, qui passait devant eux).
        Form12.AppliquerPremierPlan()

        DiagLog("PIP : format video=" & If(formatVideo > 0, Math.Round(formatVideo, 3).ToString(), "inconnu") &
                " -> " & Form12.Width & "x" & Form12.Height & " " & position & " | ecart aux bords=" & Form12.EcartActuel() & "px")
    End Sub

    ''' <summary>Largeur / hauteur de l'image de la video en cours, telle qu'affichee : pixels non
    ''' carres (SAR) et rotation (video de telephone) pris en compte. 0 si VLC ne l'a pas annoncee
    ''' dans le delai : un flux TV met parfois un instant a donner sa resolution.</summary>
    Function FormatDeLaVideo(vPlayer As Vlc.DotNet.Forms.VlcControl, Optional attenteMaxMs As Integer = 3000) As Double
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do
            Try
                Dim media As Vlc.DotNet.Core.VlcMedia = vPlayer.GetCurrentMedia()
                If media IsNot Nothing Then
                    For Each piste As Vlc.DotNet.Core.Interops.MediaTrack In media.Tracks
                        If piste.Type <> Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Video Then Continue For
                        Dim v As Vlc.DotNet.Core.Interops.VideoTrack = TryCast(piste.TrackInfo, Vlc.DotNet.Core.Interops.VideoTrack)
                        If v Is Nothing OrElse v.Width = 0 OrElse v.Height = 0 Then Continue For
                        Dim sar As Double = If(v.SarNum > 0 AndAlso v.SarDen > 0, CDbl(v.SarNum) / v.SarDen, 1.0)
                        Dim f As Double = v.Width * sar / v.Height
                        ' Orientations "Left*" / "Right*" = tournee d'un quart de tour : VLC la redresse.
                        If v.Orientation.ToString().StartsWith("Left") OrElse v.Orientation.ToString().StartsWith("Right") Then f = 1 / f
                        Return f
                    Next
                End If
            Catch ex As Exception
                ' media en cours de changement : on reessaie
            End Try
            If chrono.ElapsedMilliseconds >= attenteMaxMs Then Return 0
            ' Attente locale (variables locales) : ce code tourne dans le thread du lecteur, les
            ' attentes communes de Module1 partagent des variables avec le diaporama.
            Application.DoEvents()
            Threading.Thread.Sleep(100)
        Loop
    End Function

    ''' <summary>
    ''' Desentrelacement de la TNT. La TNT diffuse 50 demi-images par seconde ; "yadif2x" en fait
    ''' 50 images completes : les textes qui defilent (bandeaux d'info) avancent sans a-coups.
    ''' Avant, l'option etait passee sous la forme "--deinterlace-mode=discard", que VLC ignore
    ''' pour un flux (il faut ":" et non "--") : VLC choisissait seul, et avec la version 3.0.24 les
    ''' titres defilaient par saccades.
    ''' </summary>
    Public Const DesentrelacementTNT As String = "yadif2x"

    Sub PlaySAT_VLC_P2(n As Integer, vol As Integer, Argument As String, f3Location As Point, f3Size As Size)



        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        f3T = f3Location.Y
        f3L = f3Location.X
        f3H = f3Size.Height
        f3W = f3Size.Width

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then DebuterPip(Argument, "SAT", vol, CStr(n))


        If PIP = True Then
            vPlayer = Form12.VlcControl1
        Else
            vPlayer = Form3.VlcControl2
        End If
        vPlayer.Audio.IsMute = True

        If n = 1 Then
            Nom = "http://" & FluxReseaux_IP & ":8080/hdmi"
        Else
            Nom = "http://" & Dreambox_IP & ":8001/" & ChannelIDsat(n, 0)
        End If

        PreparerLeVolumeAvantChangement(vPlayer, vol)
        vPlayer.SetMedia(Nom, "--deinterlace-mode=discard")
        CouperLeSonAvantDemarrage(vPlayer)
        vPlayer.Play()
        Application.DoEvents()

        Try
            i = 0
            Do Until vPlayer.IsPlaying = True
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > TempsDattenteMaximum Then
                    SaveLogError(Nom, "VLC Erreur lecture impossible")
                    AfficheTitre(ChannelIDsat(n, 1) & vbCrLf & "Chaine introuvable ou Debit inssufisant...", "*")
                    GoTo fin
                    Exit Do                  ' echec lecture apres 2 minutes sortir
                End If
            Loop
        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try

        If PIP = True Then
            Pourcentage = Arg("PipSize", Argument)
            Pourcentage = Val(Replace(Pourcentage, ",", "."))
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
            Form12.Show()
            If vol > 0 Then
                RendreLeSonAuLecteur(vPlayer, vol)
                ' (piste audio remise si elle avait ete desactivee)
            Else
                CouperLeLecteur(vPlayer)                 ' PIP muet : piste audio desactivee (interrupteur partage avec le film)
            End If

            PlacerLePip(vPlayer, Pourcentage, PipPosition, f3L, f3T, f3W, f3H)


        Else
            SetVolume(vol, vPlayer)
            vPlayer.Top = 0
            vPlayer.Left = 0
            vPlayer.Height = Form3.ClientSize.Height
            vPlayer.Width = Form3.ClientSize.Width
            MasqueLegende()
            Form3.PictureBox1.Visible = False
            OublierLImageImmobile()                  ' (la carte du titre ne reste pas sous la video : elle reapparaissait a la fin)
            vPlayer.Visible = True
            vPlayer.BringToFront()
        End If

        If Not PIP Then FlagExit = False               ' un PIP ne touche pas au drapeau partage
        Try

            i = 0
            Do Until ArretDemande(PIP)

                DisplayedPictures = vPlayer.GetCurrentMedia.Statistics.DisplayedPictures
                Application.DoEvents()
                AttendreLecteur(1, PIP)
                i = i + 1
                'Debug.Print("LostPictures:" & vPlayer.GetCurrentMedia.Statistics.LostPictures & vbTab & "DisplayedPictures:" & vPlayer.GetCurrentMedia.Statistics.DisplayedPictures)

                If i = 10 Then
                    i = 0
                    If vPlayer.GetCurrentMedia.Statistics.DisplayedPictures = DisplayedPictures Or vPlayer.GetCurrentMedia.Statistics.LostPictures > 0 Then
                        DiagLog("Flux relance (" & If(PIP, "PIP", "plein ecran") & ") : images affichees " & vPlayer.GetCurrentMedia.Statistics.DisplayedPictures & If(vPlayer.GetCurrentMedia.Statistics.DisplayedPictures = DisplayedPictures, " (figees depuis 1 s)", "") & ", perdues " & vPlayer.GetCurrentMedia.Statistics.LostPictures)
                        'vPlayer.Pause()
                        Application.DoEvents()
                        PreparerLeVolumeAvantChangement(vPlayer, vol)
                        vPlayer.SetMedia(Nom, "--deinterlace-mode=discard")
                        CouperLeSonAvantDemarrage(vPlayer)
                        vPlayer.Play()
                        RetablirLeSonApresRelance(vPlayer, vol, PIP)   ' un PIP ne touche qu'a son propre son
                        wait(5)
                    End If
                End If
            Loop

        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try


fin:
        Dim etaitPersistant As Boolean = PIP AndAlso PipPersistant
        If Not PIP Then FlagStop = False                ' un PIP ne touche pas a la pause partagee
        Application.DoEvents()
        GardeTV.ArreterSansBloquer(vPlayer, 5000, "SAT (" & If(PIP, "PIP", "plein ecran") & ")")
        Application.DoEvents()
        Form12.Close()
        TaskIsPlaying = False
        If PIP Then TerminerPip()
        RecalculerLesPip()                         ' volume des PIP encore a l'ecran
        TaskIsPlaying = PipsActifs() > 0           ' un autre PIP peut encore jouer

        If PIP <> True Then
            Form3.VlcControl2.Visible = False
            'retablieTousLesVolumes()
        End If






    End Sub



    Sub PlayTV_VLC(n, vol, Argument)
        ID = ChannelID(n, 0)
        p = InStr(ID, ":")

        If ID = "" Or p = 0 Then
            Beep()
            AfficheTitre(ChannelID(n, 1) & vbCrLf & "Chaine introuvable ...", "*")
            Exit Sub
        End If

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then RemplacerLePipDuCoin(Argument)   ' un PIP par coin : les autres PIP restent

        LibererLeTunerTV()                         ' un seul tuner : un autre PIP TV doit le rendre
        If PIP = False Then
            FlagExit = True
            wait(1)
        End If

        Dim vPlayer, vPlayerSlideVideo As Vlc.DotNet.Forms.VlcControl
        vPlayerSlideVideo = Form3.VlcControl1
        Diffusion.Mode = "TV_VLC_INT"
        Diffusion.Chaine = n
        Diffusion.Vol = vol
        Diffusion.Arg = Argument

        MasqueLegende()
        'FlagExit = False : FlagStop = True
        If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False Then CacherSouris()
        ClosePlayer()
        Application.DoEvents()
        f3Location = Form3.Location 'x=.left y=.top
        f3Size = Form3.Size

        If Form12.VlcControl1.IsPlaying Then
            Form12.VlcControl1.Stop()
            'Form12.VlcControl1.Dispose()
        End If

        If Form3.VlcControl2.IsPlaying Then
            Form3.VlcControl2.Stop()
        End If
        ''
        'FlagExit = True
        ''
        If PIP = True Then

            PIPvolume = vol
            vPlayer = Form12.VlcControl1
            If vol > 0 Then
                vPlayerSlideVideo.Audio.IsMute = True
                MusiqueVLC.MusiquePause()
                'SetVolume(vol, vPlayer)
            End If
            Form12.Close()
            Form1.CheckForIllegalCrossThreadCalls = False
            FlagStop = False
            TaskIsPlaying = True
            TaskIsPlayingState = True
            Form3.Timer4.Start()
            Task = New Task(Sub() PlayTV_VLC_P2(n, vol, Argument, f3Location, f3Size))
            Task.Start()

        Else
            EndAndContinue()
            FlagExit = False : FlagStop = True
            vPlayer = Form3.VlcControl2
            AfficheTitre(ChannelID(n, 1) & vbCrLf & "en préparation ...", "*")
            vPlayerSlideVideo.Pause()
            PlayTV_VLC_P2(n, vol, Argument, f3Location, f3Size)
        End If



    End Sub

    Sub PlayTV_VLC_P2(n As Integer, vol As Integer, Argument As String, f3Location As Point, f3Size As Size)

        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        f3T = f3Location.Y
        f3L = f3Location.X
        f3H = f3Size.Height
        f3W = f3Size.Width

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        FermerLApercuTV()                    ' un seul tuner : l'apercu ne doit plus le tenir
        If PIP Then DebuterPip(Argument, "TV", vol, CStr(n))


        If PIP = True Then
            vPlayer = Form12.VlcControl1
        Else
            vPlayer = Form3.VlcControl2
        End If

        vPlayer.Audio.IsMute = True

        ID = ChannelID(n, 0)
        p = InStr(ID, ":")
        option1 = ":dvb-frequency=" & Left(ID, p - 1)
        option2 = Mid(ID, p)
        ' Jusqu'a 3 essais : sans image au bout de 30 s, la TV est arretee puis relancee ; apres le
        ' 3e echec, on abandonne et le programme continue (demande de Jacob, 04/10).
        Dim essaiTV As Integer = 1
essaiTNT:
        PreparerLeVolumeAvantChangement(vPlayer, vol)
        vPlayer.SetMedia("dvbt://", option1, option2, ":deinterlace=1", ":deinterlace-mode=" & DesentrelacementTNT, ":dvb-caching=5000")
        CouperLeSonAvantDemarrage(vPlayer)
        vPlayer.Play()

        Try
            i = 0
            Do Until vPlayer.IsPlaying = True
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > TempsDattenteMaximum Then SaveLogError(ID, "VLC Erreur lecture impossible") : GoTo fin : Exit Do                  ' echec lecture apres 2 minutes sortir
            Loop
        Catch ex As Exception
            'MsgBox(ex.Message)
            k = 2
        End Try

        If PIP = True Then
            'FlagExit = True

            Pourcentage = Arg("PipSize", Argument)
            Pourcentage = Val(Replace(Pourcentage, ",", "."))
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
            Form12.Show()
            If vol > 0 Then
                RendreLeSonAuLecteur(vPlayer, vol)
                ' (piste audio remise si elle avait ete desactivee)
            Else
                CouperLeLecteur(vPlayer)                 ' PIP muet : piste audio desactivee (interrupteur partage avec le film)
            End If

            PlacerLePip(vPlayer, Pourcentage, PipPosition, f3L, f3T, f3W, f3H)

            'Form12.Show()

        Else
            wait(4)
            SetVolume(vol, vPlayer)
            vPlayer.Top = 0
            vPlayer.Left = 0
            vPlayer.Height = Form3.ClientSize.Height
            vPlayer.Width = Form3.ClientSize.Width
            MasqueLegende()
            Form3.PictureBox1.Visible = False
            OublierLImageImmobile()                  ' (la carte du titre ne reste pas sous la video : elle reapparaissait a la fin)
            vPlayer.Visible = True
            vPlayer.BringToFront()
            Call AfficheHeureDeChabbatDansCadran()
        End If

        If Not PIP Then FlagExit = False               ' un PIP ne touche pas au drapeau partage

        'Call AfficheHeureDeChabbatDansCadran()

        Dim imagesA20s As Integer = 0, cadenceNotee As Boolean = False
        Dim imagesVues As Long = -1, derniereImageVue As Long = Environment.TickCount64, derniereNoteTNT As Long = Environment.TickCount64
        Try
            i = 0
            Do Until ArretDemande(PIP)

                Application.DoEvents()
                AttendreLecteur(1, PIP)
                If ArretDemande(PIP) Then Application.DoEvents() : Exit Do
                i = i + 1
                ' Cadence reelle (50 images/s attendues avec le desentrelacement 2x) : notee une fois.
                If i = 20 Then imagesA20s = vPlayer.GetCurrentMedia.Statistics.DisplayedPictures
                If i = 30 AndAlso Not cadenceNotee Then
                    cadenceNotee = True
                    DiagLog("TNT : " & Math.Round((vPlayer.GetCurrentMedia.Statistics.DisplayedPictures - imagesA20s) / 10.0, 1) & " images/s affichees, " & vPlayer.GetCurrentMedia.Statistics.LostPictures & " perdues (desentrelacement " & DesentrelacementTNT & ")")
                End If
                ' Image figee ou noire : le nombre d'images affichees n'avance plus. C'etait la "TV
                ' bloquee" (03/10 23h26 : aucune image jusqu'au lendemain ; 04/10 19h55 : bonne
                ' image au depart, puis plus rien pendant 40 min). Le flux s'arretait sans perte
                ' d'images ni coupure comptee : la relance existante (plus bas) ne voyait rien.
                ' Verifie toutes les 5 s : plus aucune image depuis 20 s (30 s au demarrage) ->
                ' arret et relance, 3 essais d'affilee au plus, puis abandon (le programme continue).
                If i Mod 5 = 0 Then
                    Dim affichees As Long = vPlayer.GetCurrentMedia.Statistics.DisplayedPictures
                    If Environment.TickCount64 - derniereNoteTNT > 300000 Then   ' trace toutes les 5 min
                        derniereNoteTNT = Environment.TickCount64
                        DiagLog("TNT : en cours (" & If(PIP, "PIP", "plein ecran") & "), " & affichees & " images affichees, joue=" & vPlayer.IsPlaying)
                    End If
                    If affichees <> imagesVues Then
                        imagesVues = affichees
                        derniereImageVue = Environment.TickCount64
                        If affichees > 3000 Then essaiTV = 1      ' 1 min d'images : la serie d'echecs est finie
                    ElseIf Environment.TickCount64 - derniereImageVue > If(affichees = 0, 30000, 20000) Then
                        DiagLog("TNT : " & If(affichees = 0, "aucune image", "image figee") & " depuis " &
                                (Environment.TickCount64 - derniereImageVue) \ 1000 & " s (" & ChannelID(n, 1) & ", " &
                                If(PIP, "PIP", "plein ecran") & ", essai " & essaiTV & "/3)")
                        If essaiTV < 3 AndAlso Not ArretDemande(PIP) AndAlso GardeTV.ArreterSansBloquer(vPlayer, 5000, "TNT") Then
                            essaiTV += 1
                            If Not PIP Then AfficheTitre(ChannelID(n, 1) & vbCrLf & "Nouvel essai " & essaiTV & "/3", "*")
                            wait(2)
                            If Not ArretDemande(PIP) Then GoTo essaiTNT
                        End If
                        DiagLog("TNT : pas d'image -> TV abandonnee, " & If(PIP, "PIP ferme", "retour au diaporama"))
                        If Not PIP Then AfficheTitre(ChannelID(n, 1) & vbCrLf & "Pas d'image (signal TV absent)", "*")
                        Exit Do
                    End If
                End If
                If i = 60 Then '60
                    i = 0
                    If vPlayer.GetCurrentMedia.Statistics.LostPictures > 100 Or vPlayer.GetCurrentMedia.Statistics.DemuxDiscontinuity > 4 Then
                        ' Cause de la relance (images en retard = ordinateur charge ; coupures du flux =
                        ' reception) : le 01/10 la TNT en PIP se relancait toutes les 66 s.
                        DiagLog("TNT relance (" & If(PIP, "PIP", "plein ecran") & ") : en 1 min, images perdues " &
                                vPlayer.GetCurrentMedia.Statistics.LostPictures & " (seuil 100), affichees " &
                                vPlayer.GetCurrentMedia.Statistics.DisplayedPictures & ", coupures du flux " &
                                vPlayer.GetCurrentMedia.Statistics.DemuxDiscontinuity & " (seuil 4), paquets corrompus " &
                                vPlayer.GetCurrentMedia.Statistics.DemuxCorrupted)
                        'vPlayer.Pause()
                        PreparerLeVolumeAvantChangement(vPlayer, vol)
                        vPlayer.SetMedia("dvbt://", option1, option2, ":deinterlace=1", ":deinterlace-mode=" & DesentrelacementTNT, ":dvb-caching=5000")
                        CouperLeSonAvantDemarrage(vPlayer)
                        vPlayer.Play()
                        RetablirLeSonApresRelance(vPlayer, vol, PIP)   ' un PIP ne touche qu'a son propre son
                        wait(5)
                    End If
                End If
            Loop

        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try


fin:
        If Not PIP AndAlso FlagExit = True Then FlagExit = False   ' (un PIP qui finit le remettait a zero : les autres lecteurs ne voyaient plus l'arret)
        Dim etaitPersistant As Boolean = PIP AndAlso PipPersistant
        If Not PIP Then FlagStop = False                ' un PIP ne touche pas a la pause partagee
        Application.DoEvents()
        GardeTV.ArreterSansBloquer(vPlayer, 5000, "TNT (" & If(PIP, "PIP", "plein ecran") & ")")
        Application.DoEvents()
        Form12.Close()
        TaskIsPlaying = False
        If PIP Then TerminerPip()
        RecalculerLesPip()                         ' volume des PIP encore a l'ecran
        TaskIsPlaying = PipsActifs() > 0           ' un autre PIP peut encore jouer

        If PIP <> True Then
            GardeTV.ArreterSansBloquer(Form3.VlcControl2, 5000, "TNT (plein ecran)")
            Form3.VlcControl2.Visible = False
            'retablieTousLesVolumes()
        End If



    End Sub

    Sub PlayIPTV_VLC(n As String, vol As Integer, Argument As String)

        If n = Nothing Then
            Beep()
            AfficheTitre(n & vbCrLf & "Chaine introuvable ...", "*")
            wait(2)
            PlanB_onError()
            Exit Sub
        End If


        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then RemplacerLePipDuCoin(Argument)   ' un PIP par coin : les autres PIP restent

        If PIP = False Then
            FlagExit = True
            wait(1)
        End If


        Dim vPlayer, vPlayerSlideVideo As Vlc.DotNet.Forms.VlcControl
        vPlayerSlideVideo = Form3.VlcControl1
        Diffusion.Mode = "IPTV_VLC_INT"
        Diffusion.Chaine = Val(Mid(n, 7, 4))
        Diffusion.Vol = vol
        Diffusion.Arg = Argument
        Diffusion.Source = n

        MasqueLegende()
        FlagExit = False : FlagStop = True
        If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False Then CacherSouris()
        ClosePlayer()
        Application.DoEvents()
        f3Location = Form3.Location 'x=.left y=.top
        f3Size = Form3.Size

        If PIP = True Then
            PIPvolume = vol
            vPlayer = Form12.VlcControl1
            If vol > 0 Then
                vPlayerSlideVideo.Audio.IsMute = True
                MusiqueVLC.MusiquePause()
                vPlayer.Audio.IsMute = True ' uniquement pour qu'il n'y ai pas de volume fort et ensuite le bon volume
                'SetVolume(vol, vPlayer)
            End If
            Form12.Close()
            Form1.CheckForIllegalCrossThreadCalls = False
            FlagStop = False
            TaskIsPlaying = True
            TaskIsPlayingState = True
            Form3.Timer4.Start()
            Task = New Task(Sub() PlayIPTV_VLC_P2(n, vol, Argument, f3Location, f3Size))
            Task.Start()
        Else
            vPlayer = Form3.VlcControl2
            AfficheTitre("Chaine IPTV " & n & vbCrLf & "en préparation ...", "*")
            vPlayerSlideVideo.Pause()
            PlayIPTV_VLC_P2(n, vol, Argument, f3Location, f3Size)
        End If



    End Sub

    Sub PlayIPTV_VLC_P2(n As String, vol As Integer, Argument As String, f3Location As Point, f3Size As Size)


        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        f3T = f3Location.Y
        f3L = f3Location.X
        f3H = f3Size.Height
        f3W = f3Size.Width

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then DebuterPip(Argument, "IPTV", vol, n)
        Nom = Arg("Url", Argument)

        If PIP = True Then
            vPlayer = Form12.VlcControl1
        Else
            vPlayer = Form3.VlcControl2
        End If
        If vPlayer.IsPlaying = True Then
            vPlayer.Stop()
        End If

        vPlayer.Audio.IsMute = True

        If Nom <> "" Then
            vPlayer.SetMedia(Nom)
            vPlayer.Play()
        Else
            Console.Beep()
            AfficheTitre("Chaine IPTV " & n & " introuvable", "*")
            wait(2)

            PlanB_onError()
            Exit Sub

        End If




        Try
            i = 0
            Do Until vPlayer.IsPlaying = True
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > TempsDattenteMaximum Then SaveLogError(Nom, "VLC Erreur lecture impossible") : Beep() : GoTo fin : Exit Do                  ' echec lecture apres 2 minutes sortir
            Loop
        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try

        If PIP = True Then
            Pourcentage = Arg("PipSize", Argument)
            Pourcentage = Val(Replace(Pourcentage, ",", "."))
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
            Form12.Show()

            If vol > 0 Then
                RendreLeSonAuLecteur(vPlayer, vol)
                ' (piste audio remise si elle avait ete desactivee)
            Else
                CouperLeLecteur(vPlayer)                 ' PIP muet : piste audio desactivee (interrupteur partage avec le film)
            End If

            PlacerLePip(vPlayer, Pourcentage, PipPosition, f3L, f3T, f3W, f3H)


        Else
            SetVolume(vol, vPlayer)
            vPlayer.Top = 0
            vPlayer.Left = 0
            vPlayer.Height = Form3.ClientSize.Height
            vPlayer.Width = Form3.ClientSize.Width
            MasqueLegende()
            Form3.PictureBox1.Visible = False
            OublierLImageImmobile()                  ' (la carte du titre ne reste pas sous la video : elle reapparaissait a la fin)
            vPlayer.Visible = True
            vPlayer.BringToFront()
        End If
        If Not PIP Then FlagExit = False               ' un PIP ne touche pas au drapeau partage

        Call AfficheHeureDeChabbatDansCadran()

        Try

            i = 0
            Do Until ArretDemande(PIP)

                DisplayedPictures = vPlayer.GetCurrentMedia.Statistics.DisplayedPictures
                Application.DoEvents()
                AttendreLecteur(1, PIP)
                i = i + 1
                'Debug.Print("LostPictures:" & vPlayer.GetCurrentMedia.Statistics.LostPictures & vbTab & "DisplayedPictures:" & vPlayer.GetCurrentMedia.Statistics.DisplayedPictures)

                If i = 10 Then
                    i = 0
                    If vPlayer.GetCurrentMedia.Statistics.DisplayedPictures = DisplayedPictures Or vPlayer.GetCurrentMedia.Statistics.LostPictures > 0 Then
                        DiagLog("Flux relance (" & If(PIP, "PIP", "plein ecran") & ") : images affichees " & vPlayer.GetCurrentMedia.Statistics.DisplayedPictures & If(vPlayer.GetCurrentMedia.Statistics.DisplayedPictures = DisplayedPictures, " (figees depuis 1 s)", "") & ", perdues " & vPlayer.GetCurrentMedia.Statistics.LostPictures)
                        'vPlayer.Pause()
                        Application.DoEvents()
                        PreparerLeVolumeAvantChangement(vPlayer, vol)
                        vPlayer.SetMedia(Nom)
                        CouperLeSonAvantDemarrage(vPlayer)
                        vPlayer.Play()
                        RetablirLeSonApresRelance(vPlayer, vol, PIP)   ' un PIP ne touche qu'a son propre son
                        wait(5)
                    End If
                End If
            Loop

        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try



fin:
        Dim etaitPersistant As Boolean = PIP AndAlso PipPersistant
        If Not PIP Then FlagStop = False                ' un PIP ne touche pas a la pause partagee
        Application.DoEvents()
        vPlayer.Stop()
        Application.DoEvents()
        Form12.Close()
        TaskIsPlaying = False
        If PIP Then TerminerPip()
        RecalculerLesPip()                         ' volume des PIP encore a l'ecran
        TaskIsPlaying = PipsActifs() > 0           ' un autre PIP peut encore jouer

        If PIP <> True Then
            Form3.VlcControl2.Visible = False
            'retablieTousLesVolumes()
        End If



    End Sub


    Sub PreviewIPTV_VLC(n As String, vol As Integer)
        ClosePlayer()

        Form11.Text = "Preview IPTV"
        Form11.StartPosition = FormStartPosition.CenterScreen
        Form11.Show()
        Form11.TopMost = True

        r = clickedCell.RowIndex
        If r = -1 Then r = LigneMinuterieEnCours.Index

        Argument = Form4.DataGridView1.Rows(r).Cells("Argument").Value

        Dim pip As Boolean
        Try
            pip = Arg("Pip", Argument)
            Pipsize = LireTaillePip(Argument)
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
        Catch ex As Exception
            pip = False
            Pipsize = PipSizeDefault
            PipPosition = "Bdroite"
        End Try


        Form11.CheckBox4.Checked = pip
        ' "Rester affiche" n'a de sens que pour un PIP : decochee et grisee si PIP est decoche.
        Form11.CheckBoxPipPersistant.Checked = Form11.CheckBox4.Checked AndAlso (UCase(Arg("PipPersistant", Argument)) = "TRUE")
        Form11.CheckBoxPipPersistant.Enabled = Form11.CheckBox4.Checked
        Form11.TrackBar3.Value = Pipsize
        Form11.Label11.Text = Pipsize

        If PipPosition = "Bdroite" Then Form11.RadioButton6.Checked = True
        If PipPosition = "Hdroite" Then Form11.RadioButton5.Checked = True
        If PipPosition = "Bgauche" Then Form11.RadioButton4.Checked = True
        If PipPosition = "Hgauche" Then Form11.RadioButton3.Checked = True
        If PipPosition = "Mgauche" Then Form11.RadioButton7.Checked = True
        If PipPosition = "Mdroite" Then Form11.RadioButton8.Checked = True
        Form11.CheckBoxSuite.Checked = UCase(Convert.ToString(Arg("Suite", Argument))) = "TRUE"
        Form11.CheckBoxSuite.Enabled = Not Form11.CheckBox4.Checked


        nom = Arg("Url", Argument)

        PreparerLeVolumeAvantChangement(Form11.VlcControl1, vol)
        Form11.VlcControl1.SetMedia(nom)
        CouperLeSonAvantDemarrage(Form11.VlcControl1)
        Form11.VlcControl1.Play()

        ' Le reglage fait tout de suite apres Play est perdu : la sortie audio n'existe pas encore
        ' (le tuner met plusieurs secondes) et le silence demande avant le demarrage restait en
        ' place -> apercu muet. On attend donc la sortie audio avant d'appliquer le volume.
        ReglerLeSonDeLApercu(vol)

        Form11.TrackBar2.Value = vol
        Form11.Label1.Text = Form11.TrackBar2.Value.ToString

        Form11.GroupBox1.Visible = True
        Form11.GroupBox1.BringToFront()

        Do Until Form11.VlcControl1.IsPlaying = True
            Application.DoEvents()
            wait(1)
            If FlagExit = True Then Exit Do
        Loop

        FlagExit = False
    End Sub

    Sub PreviewTV_VLC(n, vol)

        LibererLeTunerTV()                         ' l'apercu a besoin du tuner lui aussi
        FlagExit = True

        ClosePlayer()

        Form11.Text = "Preview TV"
        Form11.StartPosition = FormStartPosition.CenterScreen
        Form11.Show()
        Form11.TopMost = True

        If Diffusion.Mode = "TV_VLC_INT" Then
            EndAndContinue()

        End If

        r = clickedCell.RowIndex
        If r = -1 Then r = LigneMinuterieEnCours.Index

        Argument = Form4.DataGridView1.Rows(r).Cells("Argument").Value

        Dim pip As Boolean
        Try
            pip = Arg("Pip", Argument)
            Pipsize = LireTaillePip(Argument)
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
        Catch ex As Exception
            pip = False
            Pipsize = PipSizeDefault
            PipPosition = "Bdroite"
        End Try


        Form11.CheckBox4.Checked = pip
        ' "Rester affiche" n'a de sens que pour un PIP : decochee et grisee si PIP est decoche.
        Form11.CheckBoxPipPersistant.Checked = Form11.CheckBox4.Checked AndAlso (UCase(Arg("PipPersistant", Argument)) = "TRUE")
        Form11.CheckBoxPipPersistant.Enabled = Form11.CheckBox4.Checked
        Form11.TrackBar3.Value = Pipsize
        Form11.Label11.Text = Pipsize

        If PipPosition = "Bdroite" Then Form11.RadioButton6.Checked = True
        If PipPosition = "Hdroite" Then Form11.RadioButton5.Checked = True
        If PipPosition = "Bgauche" Then Form11.RadioButton4.Checked = True
        If PipPosition = "Hgauche" Then Form11.RadioButton3.Checked = True
        If PipPosition = "Mgauche" Then Form11.RadioButton7.Checked = True
        If PipPosition = "Mdroite" Then Form11.RadioButton8.Checked = True
        Form11.CheckBoxSuite.Checked = UCase(Convert.ToString(Arg("Suite", Argument))) = "TRUE"
        Form11.CheckBoxSuite.Enabled = Not Form11.CheckBox4.Checked


        ID = ChannelID(n, 0)
        If ID = "" Then Exit Sub

        'options = ":dvb-frequency=" & ID & "--deinterlace-mode=discard"
        'Form11.VlcControl1.VlcMediaplayerOptions = {"--deinterlace-mode=discard"}
        'Form11.VlcControl1.SetMedia("dvbt://", options)
        p = InStr(ID, ":")
        If p = 0 Then Exit Sub

        option1 = ":dvb-frequency=" & Left(ID, p - 1)
        option2 = Mid(ID, p)


        'Form11.VlcControl1.SetMedia("dvbt://", ":dvb-frequency=562166000", ":program=1537", "--deinterlace-mode=discard")
        'Form11.VlcControl1.SetMedia("dvbt://", option1, option2, "--deinterlace-mode=discard", "--audio-filter=equalizer", "--equalizer-preset=flat", "--audio-filter=normvol", "--norm-buff-size=20", "--norm-max-level=2")
        PreparerLeVolumeAvantChangement(Form11.VlcControl1, vol)
        Form11.VlcControl1.SetMedia("dvbt://", option1, option2, "--deinterlace-mode=discard")
        CouperLeSonAvantDemarrage(Form11.VlcControl1)
        Form11.VlcControl1.Play()

        ' Le reglage fait tout de suite apres Play est perdu : la sortie audio n'existe pas encore
        ' (le tuner met plusieurs secondes) et le silence demande avant le demarrage restait en
        ' place -> apercu muet. On attend donc la sortie audio avant d'appliquer le volume.
        ReglerLeSonDeLApercu(vol)

        Form11.TrackBar2.Value = vol
        Form11.Label1.Text = Form11.TrackBar2.Value.ToString

        Form11.GroupBox1.Visible = True
        Form11.GroupBox1.BringToFront()

        FlagExit = False
        Do Until Form11.VlcControl1.IsPlaying = True
            Application.DoEvents()
            wait(1)
            If FlagExit = True Then Exit Do
            If Form11.Visible = False Then Exit Do
        Loop

        'FlagExit = False
    End Sub

    ''' <summary>Vrai si une tache Film designe un FICHIER qui n'existe pas (une adresse web, Netflix,
    ''' Prime Video ou une recherche "#mot*.ext" ne sont pas des fichiers a verifier).</summary>
    Function FilmManquant(action As String, Optional argument As String = "") As Boolean
        Try
            If Convert.ToString(Arg("Url", argument)) <> "" Then Return False        ' film IPTV : adresse web, pas de fichier
            Dim fichier As String = CoherenceMinuterie.FichierDuFilm(action)
            Return fichier <> "" AndAlso Not IO.File.Exists(fichier)
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Vrai apres PlayFILM_VLC quand le film n'a pas ete joue faute de fichier : la minuterie
    ''' ne doit alors PAS "relancer comme au demarrage" (elle retrouvait la meme tache, qui echouait
    ''' aussitot, et ainsi de suite : l'ecran restait sur "Film introuvable" - vu par Jacob le 08/10/2026).</summary>
    Public FilmIntrouvable As Boolean

    ''' <summary>Le film d'une tache n'existe pas : "Film introuvable" et son NOM a l'ecran (avant
    ''' le 08/10/2026 : "Film introuvable" seul, on ne savait pas lequel), chemin complet au journal.</summary>
    Sub AnnoncerFilmIntrouvable(nom As String)
        Dim court As String = ""
        Try
            court = IO.Path.GetFileName(If(nom, "").TrimEnd("\"c, "/"c))
        Catch ex As Exception
        End Try
        If court.Length > 60 Then court = court.Substring(0, 57) & "..."
        DiagLog("Film introuvable : " & If(String.IsNullOrEmpty(nom), "(aucun fichier)", nom))
        FilmIntrouvable = True
        Console.Beep()
        AfficheTitre("Film introuvable" & If(court = "", "", vbCrLf & court), "*")
    End Sub


    ' ------------------------------------------------------------------------------------------
    ' PLUSIEURS PIP A LA FOIS : une "session" par PIP
    '
    ' Chaque lecteur PIP tourne dans son propre thread. Son etat (type, "Rester affiche", signal
    ' d'arret, volume, coin, film en cours) etait GLOBAL : un 2e PIP ecrasait celui du 1er (le
    ' 1er perdait son "Rester affiche", son arret, son volume...). Chaque PIP a maintenant sa
    ' session, creee dans son thread (DebuterPip) et retrouvee par ce thread (_sessionDuFil).
    '
    ' "Rester affiche" (case du PIP, argument "!/PipPersistant:True") : FlagExit arrete tous les
    ' lecteurs a la fois (film ou TV en plein ecran, touche N...) ; un PIP persistant ne l'ecoute
    ' pas, il ne s'arrete que sur SON signal (Arret), leve par un nouveau PIP dans le meme coin,
    ' la TV qui a besoin du tuner, ou un arret explicite (touche P, "Close All Player", "TV OFF"...).
    ' ------------------------------------------------------------------------------------------

    Public Class SessionPip
        Public Type As String = ""
        Public Persistant As Boolean
        Public Arret As Boolean
        Public Volume As Integer
        Public Coin As String = "Bdroite"
        Public Film As String = ""
        Public Argument As String = ""          ' reglages de la tache (taille, coin...) au lancement
        Public Cible As String = ""             ' chaine, flux ou film lance (pour le relancer : voir RelanceDeNuit)
        Public Play As String = ""              ' film : debut et duree demandes
        Public Duree As String = ""
        Public Fenetre As Form12                ' sa fenetre PIP (dans son thread), une fois placee
        Public FormatVideo As Double            ' largeur / hauteur de sa video (0 = inconnu)
    End Class

    Private ReadOnly _sessionsPip As New List(Of SessionPip)
    <ThreadStatic> Private _sessionDuFil As SessionPip

    Private Function SessionsPip() As List(Of SessionPip)
        SyncLock _sessionsPip
            Return _sessionsPip.ToList()
        End SyncLock
    End Function

    ''' <summary>Nombre de PIP en cours.</summary>
    Function PipsActifs() As Integer
        SyncLock _sessionsPip
            Return _sessionsPip.Count
        End SyncLock
    End Function

    ''' <summary>Dans le thread d'un PIP : ce PIP est-il "Rester affiche" ? Ailleurs : au moins un
    ''' PIP en cours l'est-il ?</summary>
    Public ReadOnly Property PipPersistant As Boolean
        Get
            Dim s As SessionPip = _sessionDuFil
            If s IsNot Nothing Then Return s.Persistant
            Return SessionsPip().Any(Function(x) x.Persistant)
        End Get
    End Property

    ''' <summary>Film joue par le PIP de ce thread ("" si aucun) : voir FilmDejaDansLePip.</summary>
    Public Property PipFilmEnCours As String
        Get
            Dim s As SessionPip = _sessionDuFil
            Return If(s Is Nothing, "", s.Film)
        End Get
        Set(value As String)
            Dim s As SessionPip = _sessionDuFil
            If s IsNot Nothing Then s.Film = If(value, "")
        End Set
    End Property

    ''' <summary>Coin demande par une tache PIP (Bdroite par defaut).</summary>
    Function CoinDuPip(argument As String) As String
        Dim c As String = CStr(Arg("PipPosition", If(argument, "")))
        Return If(c = "", "Bdroite", c)
    End Function

    ''' <summary>Le lecteur doit-il s'arreter ? Un PIP : son propre signal, plus FlagExit s'il n'est
    ''' pas "Rester affiche". Les autres lecteurs : FlagExit.</summary>
    ''' <summary>Vrai si le PIP lance par ce thread est une chaine TV (TNT) : son lecteur est cree
    ''' avec une reduction d'image de qualite (voir OptionsDuLecteurVlc).</summary>
    ''' <summary>Dans le thread d'un PIP : les reglages actuels de sa tache (mis a jour par
    ''' AjusterLePip quand la minuterie le retrouve deja en lecture) ; ailleurs, ou s'ils sont
    ''' vides : <paramref name="auLancement"/>.</summary>
    Function ArgumentActuelDuPip(auLancement As String) As String
        Dim s As SessionPip = _sessionDuFil
        If s IsNot Nothing AndAlso Not String.IsNullOrEmpty(s.Argument) Then Return s.Argument
        Return auLancement
    End Function

    Function PipDuFilEstUneTV() As Boolean
        Dim s As SessionPip = _sessionDuFil
        Return s IsNot Nothing AndAlso s.Type = "TV"
    End Function

    Function ArretDemande(estPip As Boolean) As Boolean
        Dim s As SessionPip = _sessionDuFil
        If estPip AndAlso s IsNot Nothing Then
            ' Touche N : le signal d'arret (FlagExit) ne concerne que le diaporama, les PIP restent.
            ' La dispense tombe d'elle-meme quand le signal est retombe.
            ' (Pas dans la demi-seconde qui suit l'appui : ce fil pouvait effacer la dispense juste
            '  AVANT que le signal ne soit leve, et le PIP se fermait - "le PIP a encore disparu".)
            If Not FlagExit AndAlso Environment.TickCount64 - _pipEpargnesLe > 500 Then _pipEpargnes = False
            Return s.Arret OrElse (Not s.Persistant AndAlso FlagExit AndAlso Not _pipEpargnes)
        End If
        Return FlagExit
    End Function

    ''' <summary>Vrai tant que le FlagExit leve par la touche N est en cours : il n'arrete pas les PIP.</summary>
    Private _pipEpargnes As Boolean
    Private _pipEpargnesLe As Long
    Private _suivantRapide As Boolean

    ''' <summary>Touche N (et menu Next) : element suivant du diaporama SANS fermer les PIP, "Rester
    ''' affiche" ou non (demande de Jacob : seule la touche P ferme les PIP). EndAndContinue leve
    ''' FlagExit, sur lequel un PIP ordinaire s'arretait ; il en est dispense pour cette fois.</summary>
    Sub SuivantSansFermerLesPip()
        _pipEpargnesLe = Environment.TickCount64
        _pipEpargnes = True
        _suivantDemandeLe = Environment.TickCount64
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim modeAvant As String = Diffusion.Mode
        ' Diaporama seul a l'ecran (photo ou video, avec ou sans PIP) : aucun lecteur externe a
        ' fermer, on evite la recherche de fenetres et de processus (~130 ms par appui).
        _suivantRapide = (modeAvant = "PHOTO" OrElse modeAvant = "VIDEO" OrElse (PipsActifs() > 0 AndAlso LecteursLances.Count = 0))
        ' (EndAndContinue passe deja au fichier de musique suivant dans ce cas : voir plus bas.)
        Dim fichierDejaPasse As Boolean = Music = True AndAlso (TaskIsPlaying = False OrElse (PipPersistant AndAlso PIPvolume = 0))
        Try
            EndAndContinue()
        Finally
            _suivantRapide = False
        End Try
        ' La touche N passe aussi a la MUSIQUE suivante, quelle que soit la musique (demandes de Jacob,
        ' 08/10/2026), seulement si elle joue en ce moment (une pause voulue, ou la musique coupee par
        ' un programme avec son, reste en place) :
        '   - Deezer dans Slide : morceau suivant (MusiqueSuivant, lui, ne fait que reprendre la lecture) ;
        '   - application Deezer externe : la commande "Deezer.Next" (clavier et souris, comme la telecommande) ;
        '   - radio : station suivante de la liste (pas enregistree dans les reglages) ;
        '   - fichiers : morceau suivant, si EndAndContinue ne l'a pas deja fait.
        Try
            ' (Le changement de SOURCE de musique est sur la touche S : voir Form3.FaireLaTouche.)
            If Music = True Then
                If MusiqueDeezer.Actif Then
                    If MusiqueDeezer.EnLecture Then
                        DiagLog("Touche N : morceau suivant de Deezer")
                        MusiqueDeezer.Suivant()
                    End If
                ElseIf Music_Deezer = True Then
                    DiagLog("Touche N : morceau suivant de l'application Deezer")
                    GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Next")
                ElseIf Music_Radio = True Then
                    If MusiqueVLC.MusiqueEnCours() Then
                        Dim station As String = MusiqueVLC.MusiqueRadioSuivante()
                        If station <> "" Then DiagLog("Touche N : radio suivante (" & station & ")")
                    End If
                ElseIf Music_Mp3 = True AndAlso Not fichierDejaPasse AndAlso MusiqueVLC.MusiqueEnCours() Then
                    DiagLog("Touche N : fichier de musique suivant")
                    MusiqueVLC.MusiqueSuivant()
                End If
            End If
        Catch ex As Exception
            DiagLog("Touche N (musique suivante) : " & ex.Message)
        End Try
        DiagLog("Touche N : suivant demande (mode " & modeAvant & "), traite en " & chrono.ElapsedMilliseconds & " ms")
    End Sub

    ''' <summary>Trace : le diaporama a bien pris en compte le "suivant" demande par la touche N.
    ''' Une ligne "Touche N" sans cette ligne derriere = la demande s'est perdue en route.</summary>
    Private _suivantDemandeLe As Long
    Sub NoterSuivantPrisEnCompte(ou As String)
        If _suivantDemandeLe = 0 Then Exit Sub
        DiagLog("Touche N : prise en compte (" & ou & ") apres " & (Environment.TickCount64 - _suivantDemandeLe) & " ms")
        _suivantAAfficherDepuis = _suivantDemandeLe
        _suivantDemandeLe = 0
    End Sub

    ''' <summary>Trace : temps entre l'appui sur N et l'element suivant reellement a l'ecran.</summary>
    Private _suivantAAfficherDepuis As Long
    Sub NoterSuivantAffiche(quoi As String)
        If _suivantAAfficherDepuis = 0 Then Exit Sub
        DiagLog("Touche N : " & quoi & " suivante a l'ecran " & (Environment.TickCount64 - _suivantAAfficherDepuis) & " ms apres l'appui")
        _suivantAAfficherDepuis = 0
    End Sub
    ''' <summary>Attente dans la boucle d'un lecteur, interrompue des qu'il doit s'arreter. wait()
    ''' sort aussitot quand FlagExit est leve : un PIP persistant tournerait alors a vide (processeur
    ''' a 100 %) ; un PIP attend donc a son rythme, interrompu par SON arret.</summary>
    Sub AttendreLecteur(secondes As Double, estPip As Boolean)
        If Not estPip OrElse _sessionDuFil Is Nothing Then
            wait(secondes)
            Exit Sub
        End If
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do While chrono.Elapsed.TotalSeconds < secondes AndAlso Not ArretDemande(True)
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop
    End Sub

    ''' <summary>Taille du PIP en % (TrackBar3 de Form6 / Form11) lue dans l'argument d'une tache.
    ''' Arg() rend "0.15" (point) : "0.15" * 100 echoue en culture francaise, et le Catch des
    ''' fenetres remettait la taille par defaut, decochait le PIP et remettait "Bdroite".
    ''' Lecture independante de la culture ; taille absente ou nulle = PipSizeDefault.</summary>
    Function LireTaillePip(argument As String) As Integer
        ' String.Replace et non Replace() de VB : celle-ci renvoie Nothing sur un texte vide (tache
        ' sans PipSize), et .Trim() plantait (NullReferenceException).
        Dim texte As String = CStr(If(Arg("PipSize", If(argument, "")), "")).Replace(",", ".").Trim()
        Dim part As Double
        If Not Double.TryParse(texte, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, part) OrElse part <= 0 Then
            Return Math.Max(0, Math.Min(100, PipSizeDefault))
        End If
        Return Math.Max(1, Math.Min(100, CInt(Math.Round(part * 100))))
    End Function

    ''' <summary>Debut d'un PIP, dans SON thread : cree sa session.</summary>
    Sub DebuterPip(argumentPip As String, typePip As String, Optional volume As Integer = 0,
                   Optional cible As String = "", Optional play As String = "", Optional duree As String = "")
        Dim s As New SessionPip With {
            .Type = typePip,
            .Persistant = (UCase(Arg("PipPersistant", argumentPip)) = "TRUE"),
            .Volume = volume,
            .Coin = CoinDuPip(argumentPip),
            .Argument = If(argumentPip, ""),
            .Cible = If(cible, ""), .Play = If(play, ""), .Duree = If(duree, "")}
        _sessionDuFil = s
        SyncLock _sessionsPip
            _sessionsPip.Add(s)
        End SyncLock
        RecalculerLesPip()
        If s.Persistant Then DiagLog("PIP " & typePip & " (" & s.Coin & ") : reste affiche pendant les evenements programmes")
    End Sub

    ''' <summary>Les PIP en cours, de quoi les relancer a l'identique : type, cible, debut, duree,
    ''' volume, reglages actuels (voir RelanceDeNuit).</summary>
    Function PipsEnCoursPourRelance() As List(Of String())
        Return SessionsPip().Where(Function(s) Not s.Arret).
            Select(Function(s) New String() {s.Type, s.Cible, s.Play, s.Duree, CStr(s.Volume), s.Argument}).ToList()
    End Function

    ''' <summary>Fin d'un PIP, dans SON thread : retire sa session.</summary>
    Sub TerminerPip()
        Dim s As SessionPip = _sessionDuFil
        _sessionDuFil = Nothing
        If s Is Nothing Then Exit Sub
        SyncLock _sessionsPip
            _sessionsPip.Remove(s)
        End SyncLock
        RecalculerLesPip()
    End Sub

    ''' <summary>Volume "PIP avec le son" (PIPvolume > 0 : la musique reste en pause) : le plus fort
    ''' des PIP en cours, 0 s'il n'y en a pas.</summary>
    Sub RecalculerLesPip()
        Dim s As List(Of SessionPip) = SessionsPip()
        PIPvolume = If(s.Count = 0, 0, s.Max(Function(x) x.Volume))
    End Sub

    ''' <summary>Leve le signal d'arret des PIP choisis et attend qu'ils soient fermes (5 s au plus).</summary>
    Private Sub ArreterLesPip(choix As Func(Of SessionPip, Boolean), quoi As String)
        Dim cibles As List(Of SessionPip) = SessionsPip().Where(choix).ToList()
        If cibles.Count = 0 Then Exit Sub
        For Each s As SessionPip In cibles
            s.Arret = True
            ' Son coupe tout de suite : le PIP met jusqu'a quelques secondes a se fermer.
            Try
                If s.Fenetre IsNot Nothing AndAlso Not s.Fenetre.IsDisposed Then CouperLeLecteur(s.Fenetre.VlcControl1)
            Catch ex As Exception
            End Try
        Next
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do While chrono.ElapsedMilliseconds < 5000
            Dim restants As List(Of SessionPip) = SessionsPip()
            If Not cibles.Any(Function(c) restants.Contains(c)) Then Exit Do
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        Loop
        DiagLog(quoi & " (" & String.Join(", ", cibles.Select(Function(c) c.Type & " " & c.Coin)) & ", " & chrono.ElapsedMilliseconds & " ms)")
    End Sub

    ''' <summary>
    ''' Apres une modification de la minuterie (OK des reglages) : un PIP en cours dont la tache n'existe plus - ligne
    ''' supprimee, desactivee, sans PIP, ou film / chaine change - est arrete (14/10/2026 : "si pip tourne et que je
    ''' modifie la minuterie et si ce pip n'est plus dans la minuterie alors il faut le tuer"). Un PIP dont la tache
    ''' est toujours la continue, avec ses reglages (taille, coin...) mis a jour par la minuterie.
    ''' </summary>
    Sub ArreterLesPipSansTache()
        Try
            If PipsActifs() = 0 Then Exit Sub
            Dim actives As New List(Of Minuterie)
            For Each o As Object In LigneMinuterie
                If TypeOf o Is Minuterie Then
                    Dim t As Minuterie = CType(o, Minuterie)
                    If t.Actif AndAlso UCase(Convert.ToString(Arg("Pip", t.Argument))) = "TRUE" Then actives.Add(t)
                End If
            Next
            ArreterLesPip(Function(s As SessionPip) As Boolean
                              If s.Arret Then Return False
                              Return Not actives.Any(Function(t As Minuterie) CorrespondALaTache(t, s))
                          End Function, "PIP sans tache dans la minuterie arrete")
        Catch ex As Exception
            DiagLog("PIP sans tache : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La tache <paramref name="t"/> de la minuterie est-elle celle qui a lance le PIP <paramref name="s"/> ?
    ''' Memes regles que le lancement (voir Module1 : numero de chaine pris dans Action ; film : fichier ou adresse).</summary>
    Private Function CorrespondALaTache(t As Minuterie, s As SessionPip) As Boolean
        Dim typeTache As String = UCase(Convert.ToString(t.Type)).Trim()
        Dim action As String = Convert.ToString(t.Action).Trim()
        Dim egal As Func(Of String, String, Boolean) =
            Function(a As String, b As String) As Boolean
                Return a <> "" AndAlso String.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase)
            End Function
        Select Case UCase(s.Type)
            Case "FILM"
                If typeTache <> "FILM" Then Return False
                Dim url As String = Convert.ToString(Arg("Url", t.Argument))
                Return egal(action, s.Film) OrElse egal(action, s.Cible) OrElse
                       (url <> "" AndAlso (egal(url, s.Film) OrElse egal(url, s.Cible)))
            Case "IPTV"
                Return typeTache = "IPTV" AndAlso egal(action, s.Cible)
            Case "TV"
                Return typeTache = "TV" AndAlso CStr(Val(Mid(action, 8, 2))) = s.Cible
            Case "SAT"
                Return typeTache = "SAT" AndAlso CStr(Val(Mid(action, 8, 3))) = s.Cible
            Case Else
                Return True                      ' type inconnu : on ne touche pas au PIP
        End Select
    End Function

    ''' <summary>Arrete les PIP "Rester affiche" (de ce type, ou de tout type si
    ''' <paramref name="typePip"/> = ""). Les PIP ordinaires s'arretent deja sur FlagExit.</summary>
    Sub ArreterLePipPersistant(Optional typePip As String = "")
        ArreterLesPip(Function(s) s.Persistant AndAlso (typePip = "" OrElse String.Equals(s.Type, typePip, StringComparison.OrdinalIgnoreCase)),
                      "PIP persistant arrete")
    End Sub

    ''' <summary>Arrete TOUS les PIP, "Rester affiche" ou non, chacun par son propre signal (touche
    ''' P, "Close All Player", "End &amp; Continue"). Avant, les PIP ordinaires ne s'arretaient que
    ''' sur le drapeau partage FlagExit : si un autre lecteur le remettait a zero avant qu'ils l'aient
    ''' vu, ils continuaient (ou un film en PIP se croyait coupe et repartait).</summary>
    Sub ArreterTousLesPip()
        ArreterLesPip(Function(s) True, "Tous les PIP arretes")
    End Sub

    ''' <summary>
    ''' La cle TNT n'a qu'UN tuner : il ne capte qu'une frequence a la fois. Un PIP TV le gardait :
    ''' une TV en plein ecran (ou un autre PIP TV) sur un autre multiplex echouait ("VLC Erreur
    ''' lecture impossible"). On arrete donc les PIP TV et on attend qu'ils aient rendu le tuner.
    ''' Un PIP IPTV ou film n'utilise pas le tuner : il n'est pas touche.
    ''' </summary>
    ''' <summary>Vrai tant que la fenetre d'apercu (Form11) est ouverte : elle tient le tuner TV.</summary>
    Public ApercuTvOuvert As Boolean

    ''' <summary>Avant de lancer la TV (plein ecran ou PIP) : ferme l'apercu s'il est encore ouvert,
    ''' sinon le tuner est pris et la TV reste noire. Depuis le thread d'un PIP, la fermeture est
    ''' confiee au thread de l'interface et on attend qu'elle soit faite (5 s au plus).</summary>
    Sub FermerLApercuTV()
        If Not ApercuTvOuvert Then Exit Sub
        If ConfierAuFilPrincipal(Sub() FermerLApercuTV(), "FermerLApercuTV") Then
            Dim chrono As Stopwatch = Stopwatch.StartNew()
            Do While ApercuTvOuvert AndAlso chrono.ElapsedMilliseconds < 5000
                Threading.Thread.Sleep(50)
            Loop
            Exit Sub
        End If
        DiagLog("Apercu TV encore ouvert : ferme pour liberer le tuner")
        Try
            For Each f As Form11 In My.Application.OpenForms.OfType(Of Form11)().ToList()
                f.Close()
            Next
        Catch ex As Exception
            DiagLog("Apercu TV : fermeture impossible (" & ex.Message & ")")
        End Try
        ApercuTvOuvert = False
    End Sub

    Sub LibererLeTunerTV()
        ArreterLesPip(Function(s) String.Equals(s.Type, "TV", StringComparison.OrdinalIgnoreCase),
                      "PIP TV arrete pour liberer le tuner")
    End Sub

    ''' <summary>Un nouveau PIP remplace celui qui occupe deja SON coin (les autres restent).</summary>
    Sub RemplacerLePipDuCoin(argumentPip As String)
        Dim coin As String = CoinDuPip(argumentPip)
        ArreterLesPip(Function(s) String.Equals(s.Coin, coin, StringComparison.OrdinalIgnoreCase),
                      "PIP remplace dans son coin")
    End Sub

    ''' <summary>Vrai si <paramref name="Nom"/> est demande en PIP et tourne deja dans un PIP. La
    ''' minuterie redemande la meme tache a chaque reevaluation (fermeture des reglages, tache BOOT
    ''' retrouvee faute d'autre...) : sans ce test, le film repartait du debut.</summary>
    Function FilmDejaDansLePip(Nom As String, Argument As String, Optional volume As Integer = -1) As Boolean
        If UCase(Arg("Pip", Argument)) <> "TRUE" Then Return False
        If Not Form12.EstAffiche Then Return False
        ' Film IPTV (Nflix) : l'Action de la tache est son TITRE, l'adresse est dans "!/Url:" ; le PIP en cours porte l'adresse.
        ' Reconnu par l'un ou l'autre, et a defaut par sa tache (meme adresse ou meme titre dans les reglages), pour que le OK des
        ' reglages ne relance plus un PIP qui tourne (16/10/2026 : "le PIP en haut a droite a ete relance").
        Dim adresse As String = CStr(If(Arg("Url", If(Argument, "")), "")).Trim()
        Dim titre As String = CStr(If(Arg("Titre", If(Argument, "")), "")).Trim()
        Dim egalNom As Func(Of String, Boolean) = Function(t) t <> "" AndAlso (String.Equals(t, Nom, StringComparison.OrdinalIgnoreCase) OrElse
                                                                                 (adresse <> "" AndAlso String.Equals(t, adresse, StringComparison.OrdinalIgnoreCase)))
        Dim s As SessionPip = SessionsPip().FirstOrDefault(Function(x) Not x.Arret AndAlso (egalNom(x.Film) OrElse egalNom(x.Cible)))
        If s Is Nothing AndAlso (adresse <> "" OrElse titre <> "") Then
            s = SessionsPip().FirstOrDefault(Function(x) Not x.Arret AndAlso String.Equals(x.Type, "FILM", StringComparison.OrdinalIgnoreCase) AndAlso
                                                 ((adresse <> "" AndAlso String.Equals(CStr(If(Arg("Url", If(x.Argument, "")), "")).Trim(), adresse, StringComparison.OrdinalIgnoreCase)) OrElse
                                                  (adresse = "" AndAlso titre <> "" AndAlso String.Equals(CStr(If(Arg("Titre", If(x.Argument, "")), "")).Trim(), titre, StringComparison.OrdinalIgnoreCase))))
        End If
        If s Is Nothing Then Return False
        ' Volume change dans les reglages depuis le lancement (16/10/2026 : "il s'est mis a 100 tout seul" : le PIP remettait a chaque
        ' reprise de sa boucle le volume de son LANCEMENT) : applique tout de suite et retenu pour la suite.
        If volume >= 0 AndAlso s.Volume <> volume Then
            DiagLog("PIP : volume " & s.Volume & " -> " & volume & " (reglages de la tache, film en cours)")
            s.Volume = volume
            AppliquerLeVolumeAuPip(s, volume)
        End If
        ' Taille ou coin changes dans les reglages depuis son lancement : on les applique au PIP en
        ' cours, sans reprendre le film au debut. Avant, il gardait l'ancienne taille (15 % alors
        ' que la tache disait 20 %).
        If LireTaillePip(s.Argument) <> LireTaillePip(Argument) OrElse
           Not String.Equals(CoinDuPip(s.Argument), CoinDuPip(Argument), StringComparison.OrdinalIgnoreCase) Then
            AjusterLePip(s, Argument)
        Else
            ' Autres reglages de la tache (ex. "Lire en boucle") : retenus pour le film en cours,
            ' qui les relit a sa fin (ArgumentActuelDuPip).
            s.Argument = If(Argument, "")
            DiagLog("PIP : deja en lecture, pas de relance (" & Nom & ")")
        End If
        Return True
    End Function

    ''' <summary>Regle le volume du lecteur d'un PIP en cours (son lecteur vit dans le thread de sa fenetre).</summary>
    Private Sub AppliquerLeVolumeAuPip(s As SessionPip, volume As Integer)
        Try
            Dim f As Form12 = s.Fenetre
            If f Is Nothing OrElse f.IsDisposed Then Exit Sub
            f.BeginInvoke(New Action(Sub()
                                         Try
                                             If f.IsDisposed Then Exit Sub
                                             f.VlcControl1.Audio.IsMute = (volume <= 0)
                                             f.VlcControl1.Audio.Volume = If(volume <= 0, 0, VolumeVLC(volume))
                                         Catch ex As Exception
                                         End Try
                                     End Sub))
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Nouvelle taille / nouveau coin pour un PIP en cours (sa fenetre vit dans son thread).</summary>
    Private Sub AjusterLePip(s As SessionPip, argument As String)
        Dim part As Double = LireTaillePip(argument) / 100.0
        Dim coin As String = CoinDuPip(argument)
        s.Argument = If(argument, "")
        s.Coin = coin
        Dim f As Form12 = s.Fenetre
        If f Is Nothing OrElse f.IsDisposed Then Exit Sub
        Dim principale As Form = FenetrePrincipale()
        If principale Is Nothing Then Exit Sub
        Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
        Dim format As Double = s.FormatVideo
        Try
            f.BeginInvoke(New Action(Sub()
                                         If f.IsDisposed Then Exit Sub
                                         f.PoserDansLeCoin(zone, coin, format, part)
                                         Dim lx As Double, ly As Double
                                         If LirePositionPip(argument, lx, ly) Then f.DefinirPlaceLibre(lx, ly) Else f.DefinirPlaceLibre(-1, -1)
                                     End Sub))
            DiagLog("PIP : reglages appliques au PIP en cours (" & s.Type & " " & coin & ", " & Math.Round(part * 100) & " %)")
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille d'un PIP choisie a la souris (molette ou cliquer-glisser sur le PIP, voir Form12).
    ' Elle est enregistree dans la tache de la minuterie qui l'a lance, comme si on avait bouge
    ' la glissiere "Taille" de la tache : le PIP garde cette taille au prochain lancement.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les reglages d'une tache avec une nouvelle taille de PIP (meme ecriture que Form6
    ''' et Form11 : "!/PipSize:0,20").</summary>
    Function ArgumentAvecTaillePip(argument As String, pourcent As Integer) As String
        Dim a As String = If(argument, "")
        Dim taille As String = "!/PipSize:" & Format(pourcent / 100, "0.00")
        Dim m As System.Text.RegularExpressions.Match =
            System.Text.RegularExpressions.Regex.Match(a, "!/PipSize:[^!]*", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        If m.Success Then Return a.Substring(0, m.Index) & taille & a.Substring(m.Index + m.Length)
        Return a & taille
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Place d'un PIP choisie a la souris (cliquer-glisser sur le PIP, voir Form12) : "!/PipX:0,42!/PipY:0,30", proportions de la place libre dans l'image de Slide
    ' (0 = bord gauche / haut, 1 = bord droit / bas). Enregistree dans la tache de la minuterie comme la taille ; sans PipX / PipY, le PIP reste dans son coin.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les reglages d'une tache avec la place du PIP (fx ou fy negatif : la place libre est retiree, le PIP retourne dans son coin).</summary>
    Function ArgumentAvecPositionPip(argument As String, fx As Double, fy As Double) As String
        Dim a As String = If(argument, "")
        a = System.Text.RegularExpressions.Regex.Replace(a, "!/PipX:[^!]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        a = System.Text.RegularExpressions.Regex.Replace(a, "!/PipY:[^!]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        If fx < 0 OrElse fy < 0 Then Return a
        Return a & "!/PipX:" & Format(fx, "0.000") & "!/PipY:" & Format(fy, "0.000")
    End Function

    ''' <summary>La place libre inscrite dans les reglages d'une tache ; False si le PIP reste dans son coin.</summary>
    Function LirePositionPip(argument As String, ByRef fx As Double, ByRef fy As Double) As Boolean
        Dim x As String = CStr(If(Arg("PipX", If(argument, "")), "")).Replace(",", ".").Trim()
        Dim y As String = CStr(If(Arg("PipY", If(argument, "")), "")).Replace(",", ".").Trim()
        If x = "" OrElse y = "" Then Return False
        If Not Double.TryParse(x, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, fx) Then Return False
        If Not Double.TryParse(y, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, fy) Then Return False
        Return fx >= 0 AndAlso fy >= 0
    End Function

    ''' <summary>Geste termine sur le PIP <paramref name="fenetre"/> (appele dans SON thread) : sa place est retenue dans sa session et dans la tache de la minuterie.</summary>
    Sub PositionPipChoisieALaSouris(fenetre As Form12, fx As Double, fy As Double)
        Try
            Dim s As SessionPip = SessionsPip().FirstOrDefault(Function(x) ReferenceEquals(x.Fenetre, fenetre))
            If s Is Nothing Then Exit Sub
            Dim ancien As String = s.Argument
            Dim nouveau As String = ArgumentAvecPositionPip(ancien, fx, fy)
            If String.Equals(ancien, nouveau, StringComparison.Ordinal) Then Exit Sub
            s.Argument = nouveau
            DiagLog("PIP : place choisie a la souris " & If(fx < 0, "(retour au coin)", Format(fx, "0.00") & " / " & Format(fy, "0.00")) & " (" & s.Type & " " & s.Coin & ")")
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing AndAlso principale.IsHandleCreated Then
                principale.BeginInvoke(New Action(Sub() EnregistrerLaTaillePip(ancien, nouveau)))
            End If
        Catch ex As Exception
            DiagLog("PIP : place a la souris non retenue (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Geste termine sur le PIP <paramref name="fenetre"/> (appele dans SON thread).</summary>
    Sub TaillePipChoisieALaSouris(fenetre As Form12, pourcent As Integer)
        Try
            Dim s As SessionPip = SessionsPip().FirstOrDefault(Function(x) ReferenceEquals(x.Fenetre, fenetre))
            If s Is Nothing Then Exit Sub
            If LireTaillePip(s.Argument) = pourcent Then Exit Sub
            Dim ancien As String = s.Argument
            Dim nouveau As String = ArgumentAvecTaillePip(ancien, pourcent)
            ' La session retient la nouvelle taille : la minuterie, qui retrouve ce PIP "deja en
            ' lecture" a chaque reevaluation, ne lui remet donc pas l'ancienne.
            s.Argument = nouveau
            DiagLog("PIP : taille choisie a la souris " & pourcent & " % (" & s.Type & " " & s.Coin & ")")
            ' La minuterie, le tableau des reglages et la base appartiennent au thread de l'interface.
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing AndAlso principale.IsHandleCreated Then
                principale.BeginInvoke(New Action(Sub() EnregistrerLaTaillePip(ancien, nouveau)))
            End If
        Catch ex As Exception
            DiagLog("PIP : taille a la souris non retenue (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Remplace les reglages <paramref name="ancien"/> par <paramref name="nouveau"/> dans
    ''' les taches de la minuterie : en memoire, dans le tableau des reglages, dans les reglages
    ''' enregistres et dans la base. Seule la colonne Argument des taches concernees est touchee
    ''' (les taches qui ont exactement les memes reglages de PIP changent ensemble).</summary>
    Private Sub EnregistrerLaTaillePip(ancien As String, nouveau As String)
        Dim enMemoire As Integer = 0, dansLaBase As Integer = -1
        Try
            Dim memes As Func(Of Object, Boolean) =
                Function(a As Object) ancien <> "" AndAlso String.Equals(CStr(If(a, "")).Replace("''", "'"), ancien.Replace("''", "'"), StringComparison.Ordinal)
            For i As Integer = 0 To LigneMinuterie.Count - 1
                Dim ligne As Minuterie = CType(LigneMinuterie(i), Minuterie)
                If memes(ligne.Argument) Then
                    ligne.Argument = nouveau
                    LigneMinuterie(i) = ligne
                    enMemoire += 1
                End If
            Next
            If enMemoire = 0 Then
                DiagLog("PIP : taille non enregistree, tache introuvable dans la minuterie (taille gardee jusqu'a la fermeture du PIP)")
                Exit Sub
            End If
            For Each row As DataGridViewRow In Form4.DataGridView1.Rows
                If memes(row.Cells("Argument").Value) Then row.Cells("Argument").Value = nouveau
            Next
            If My.Settings.MinuterieArgument IsNot Nothing Then
                For i As Integer = 0 To My.Settings.MinuterieArgument.Count - 1
                    If memes(My.Settings.MinuterieArgument(i)) Then My.Settings.MinuterieArgument(i) = nouveau
                Next
                My.Settings.Save()
            End If
            Using connexion As New OleDbConnection(OLEDBPROVIDER & DatabasePath)
                connexion.Open()
                Using commande As New OleDbCommand("UPDATE MinuterieActive SET [Argument] = ? WHERE [Argument] = ?", connexion)
                    ' (en memoire, une apostrophe peut etre restee doublee apres un enregistrement ; pas dans la base)
                    commande.Parameters.AddWithValue("@nouveau", nouveau.Replace("''", "'"))
                    commande.Parameters.AddWithValue("@ancien", ancien.Replace("''", "'"))
                    dansLaBase = commande.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            DiagLog("PIP : enregistrement de la taille incomplet (" & ex.Message & ")")
        End Try
        DiagLog("PIP : taille enregistree dans la minuterie (" & enMemoire & " tache(s), " &
                If(dansLaBase < 0, "base non mise a jour", dansLaBase & " ligne(s) dans la base") & ")")
    End Sub

    Sub PlayFILM_VLC(Nom As String, play As String, Durée As String, vol As Integer, Argument As String)

        ' Film IPTV (Nflix) : Action = titre, adresse dans les reglages ("!/Url:").
        Dim adresseIptv As String = Convert.ToString(Arg("Url", Argument))
        If adresseIptv <> "" Then Nom = adresseIptv

        If InStr(UCase(Nom), "WWW.PRIMEVIDEO.COM") > 0 Or InStr(UCase(Nom), "WWW.NETFLIX.COM") > 0 Then
            FlagExit = True
            EndAndContinue()
            'waitExclusif(1)
            SetVideoVolSlide(0)
            SetMusicVolSlide(0)
            FlagExit = False
            Application.DoEvents()
            Dim startInfo As New ProcessStartInfo()
            Dim myprocess As New Process()
            startInfo.FileName = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe "
            startInfo.Arguments = Nom & " --kiosk -new-window"
            'startInfo.FileName = "C:\Program Files\Mozilla Firefox\firefox.exe "
            'startInfo.Arguments = Nom
            myprocess.StartInfo = startInfo
            myprocess.Start()

            If InStr(UCase(Nom), "NETFLIX") > 0 Then
                SetVol(0)

                If LatenceNetflix > 0 Then
                    wait(LatenceNetflix)
                    'a = False
                    'Do Until a = True
                    'a = WindowProcess("mfpmp")
                    'wait(1)
                    'Loop
                    If RetourAuDebutNetflix = True Then
                        For k = 1 To 30 '5 mn max
                            My.Computer.Keyboard.SendKeys("{LEFT 1}")
                            waitExclusifEnDixiemeDeSeconde(5)
                        Next


                    End If

                    'mouse_event(&H2, 0, 0, 0, 0)
                    'mouse_event(&H4, 0, 0, 0, 0)

                    'My.Computer.Keyboard.SendKeys("{ENTER}")
                End If

            End If
            'myprocess.WaitForExit()
            FlagStop = True
            SetVol(vol)
            Form3.Timer5.Start()
            Exit Sub
        End If


        If Left(Nom, 22) = "STOPNETFLIXPRIMEVIDEO:" Then
            FlagStop = False
            ClosePlayer()
            retablieTousLesVolumes()
            Exit Sub
        End If

        If InStr(Nom, "#") > 0 Then
            For y = Len(Nom) To 1 Step -1
                If Mid(Nom, y, 1) = "\" Then trouve = y : Exit For
            Next
            'SourceDir = Left(Nom, trouve - 1)
            SourceDir = Nom.Substring(0, trouve - 1)
            trouve2 = InStr(Nom, "*")
            'mot = Mid(Nom, trouve + 3, trouve2 - trouve - 3)
            mot = Nom.Substring(trouve + 2, trouve2 - trouve - 3)
            'ext = Mid(Nom, trouve2)
            ext = Nom.Substring(trouve2 - 1)
            Nom = ReplayCherche(SourceDir, mot, ext)
        End If

        If Left(UCase(Nom), 4) <> "HTTP" Then
            If System.IO.File.Exists(Nom) = False Then
                AnnoncerFilmIntrouvable(Nom)
                wait(2)

                PlanB_onError()
                Exit Sub
            End If
        End If




        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)

        ' Ce film tourne DEJA dans le PIP : on le laisse continuer au lieu de le reprendre au debut.
        If FilmDejaDansLePip(Nom, Argument, vol) Then Exit Sub
        ' Un autre PIP remplace le PIP persistant eventuel (apres le test precedent : le meme film,
        ' lui, doit continuer).
        If PIP Then RemplacerLePipDuCoin(Argument)   ' un PIP par coin : les autres PIP restent

        If PIP = False Then
            FlagExit = True
            FlagStop = True
            wait(2)
        End If


        Dim vPlayer, vPlayerSlideVideo As Vlc.DotNet.Forms.VlcControl
        vPlayerSlideVideo = Form3.VlcControl1
        Diffusion.Mode = "FILM-VLC-INT"
        Diffusion.Vol = vol
        Diffusion.Arg = Argument

        MasqueLegende()
        FlagExit = False : FlagStop = True

        If FullScreen = True And Form2.Visible = False And Form3.ContextMenuStrip1.Visible = False Then CacherSouris()
        ClosePlayer()
        Titre = Arg("Titre", Argument)
        If Titre = "" Then Titre = Nom
        f3Location = Form3.Location 'x=.left y=.top
        f3Size = Form3.Size

        If PIP = True Then
            PIPvolume = vol
            vPlayer = Form12.VlcControl1
            If vol > 0 Then
                vPlayerSlideVideo.Audio.IsMute = True
                MusiqueVLC.MusiquePause()
                'SetVolume(vol, vPlayer)
            End If
            Form12.Close()
            Form1.CheckForIllegalCrossThreadCalls = False
            FlagStop = False
            TaskIsPlaying = True
            TaskIsPlayingState = True
            Form3.Timer4.Start()
            Task = New Task(Sub() PlayFILM_VLC_P2(Nom, play, Durée, vol, Argument, f3Location, f3Size))
            Task.Start()
        Else
            vPlayer = Form3.VlcControl2
            Typ = "FILM "
            If InStr(LCase(Nom), "http://") > 0 Then Typ = "IPTV "
            AfficheTitre(Titre, Durée, Typ)
            vPlayerSlideVideo.Pause()
            PlayFILM_VLC_P2(Nom, play, Durée, vol, Argument, f3Location, f3Size)
        End If

    End Sub

    Sub PlayFILM_VLC_P2(Nom As String, play As String, Durée As String, vol As Integer, Argument As String, f3Location As Point, f3Size As Size)
        Dim vPlayer As Vlc.DotNet.Forms.VlcControl
        f3T = f3Location.Y
        f3L = f3Location.X
        f3H = f3Size.Height
        f3W = f3Size.Width

        Dim PIP As Boolean
        PIP = If(UCase(Arg("Pip", Argument)) = "TRUE", True, False)
        If PIP Then DebuterPip(Argument, "FILM", vol, Nom, play, Durée)

        If PIP = True Then
            vPlayer = Form12.VlcControl1
        Else
            vPlayer = Form3.VlcControl2
        End If
        vPlayer.Audio.IsMute = True

        AudioLanguage = Arg("AudioLanguage", Argument)
        SubTitle = Arg("SubTitle", Argument)

        Pub = Arg("Pub", Argument)
        AL = UCase(Arg("ModeHTTP", Argument))
        If AL = "" Or AL = "FALSE" Then ModeHTTP = False Else ModeHTTP = True

        If InStr(UCase(Nom), recepteurSat) > 0 And ModeHTTP = True Then
            Nom = Nom.Replace(recepteurSat & "Harddisk", "/hdd")
            Nom = Nom.Replace("\", "/")
            Nom = "http://" & Dreambox_IP & ":80/file?file=" & Nom
        End If
        ext = "file:///" : If Left(UCase(Nom), 4) = "HTTP" Then ext = ""
        'Dim options As String = ":no-video-title-show :network-caching=10000 "
        options = ""

        trouve = 0
        Qtrouve = 0
        trouve = InStr(play, "-")
        Dim MinuteDebut As Double          ' (Double : une plage "00:00:00 - 00:00:50" etait arrondie a la minute ; 22/10/2026, avec l'annonce de l'enchainement)
        Dim MinuteFin As Double
        Dim plageAvecFin As Boolean = False
        MinuteDebut = 0
        If trouve > 0 Then
            debut = Trim(Left(play, trouve - 1)) : MinuteDebut = TimeSpan.ParseExact(debut, "c", Nothing).TotalMinutes
            fin = Trim(Mid(play, trouve + 1)) : MinuteFin = TimeSpan.ParseExact(fin, "c", Nothing).TotalMinutes
            plageAvecFin = True
        End If

        'attend la fin de lecture
        'Dim Chrono2 As Stopwatch = New Stopwatch
        'Chrono2.Start()
        FinDuFilm = False
        PositionActuelle = 0

EssaieEncore:

        CacheSize = "2000" '2 secondes de cache
        If UCase(Right(Nom, 3)) = ".TS" Then CacheSize = 12000 '12 seconde de cache '12000 millisecondes = 12 sec
        PreparerLeVolumeAvantChangement(vPlayer, vol)
        vPlayer.SetMedia(New Uri(ext & Nom), ":network-caching=" & CacheSize)

        CouperLeSonAvantDemarrage(vPlayer)
        vPlayer.Play()

        i = 0
        'attend le debut de lecture
        Do Until vPlayer.IsPlaying = True
            Application.DoEvents()
            AttendreEnSilence(vPlayer, 1)
            i = i + 1 : If i > TempsDattenteMaximum Then SaveLogError(Nom, "VLC Erreur lecture impossible") : GoTo fin : Exit Do    ' echec lecture apres 2 minutes sortir
        Loop

        If AudioLanguage <> "" Then vPlayer.Audio.Tracks.Current = vPlayer.Audio.Tracks.All.ElementAt(CInt(AudioLanguage))
        If SubTitle <> "" Then vPlayer.SubTitles.Current = vPlayer.SubTitles.All.ElementAt(CInt(SubTitle))

        i = 0
        PositionAatteindre = MinuteDebut * 60 * 1000

        If Right(UCase(Nom), 3) = ".TS" Then
            PositionAatteindre = PositionAatteindre / vPlayer.Length
            If PositionActuelle > 0 Then PositionAatteindre = (PositionActuelle * 1000) / vPlayer.Length

            vPlayer.Position = PositionAatteindre

            If vPlayer.Length > 0 Then
                Do Until vPlayer.Position > PositionAatteindre
                    Application.DoEvents()
                    AttendreEnSilence(vPlayer, 1)
                    i = i + 1 : If i > TempsDattenteMaximum Then SaveLogError(Nom, "VLC Erreur positionement impossible") : GoTo fin : Exit Do   ' echec position lecture apres 2 minutes sortir
                Loop
            End If

        Else
            If PositionActuelle > 0 Then PositionAatteindre = (PositionActuelle * 1000)
            vPlayer.Time = PositionAatteindre
            Do Until vPlayer.Time > PositionAatteindre + 1
                Application.DoEvents()
                AttendreEnSilence(vPlayer, 1)

                i = i + 1
                If i > TempsDattenteMaximum Or vPlayer.Time = -1 Then SaveLogError(Nom, "VLC Erreur positionement impossible") : GoTo fin : Exit Do   ' echec position lecture apres 2 minutes sortir
            Loop

        End If



        If PIP = True Then
            Pourcentage = Arg("PipSize", Argument)
            Pourcentage = Val(Replace(Pourcentage, ",", "."))
            PipPosition = Arg("PipPosition", Argument)
            If PipPosition = "" Then PipPosition = "Bdroite"
            Form12.Show()
            If vol > 0 Then
                ' Comme en plein ecran (voir plus bas) : attendre la sortie audio avant de regler.
                ReglerLeSonApresDemarrage(vPlayer, vol)
            Else
                CouperLeLecteur(vPlayer)                 ' PIP muet : piste audio desactivee
            End If

            PlacerLePip(vPlayer, Pourcentage, PipPosition, f3L, f3T, f3W, f3H)
            PipFilmEnCours = Nom          ' ce film est desormais dans le PIP (voir FilmDejaDansLePip)


        Else
            ' Son du film : on attend que VLC ait cree sa sortie audio, puis volume voulu et son
            ' retabli (comme pour les videos du diaporama). Avant : SetVolume direct, juste apres
            ' le saut au debut choisi ; tant que la sortie audio n'existait pas, VLC ignorait le
            ' "son retabli" et le film, coupe au demarrage, restait MUET malgre son volume (41).
            ReglerLeSonApresDemarrage(vPlayer, vol)
            _retablissementsFilm = 0
            'MsgBox("isMute:" & vPlayer.Audio.IsMute & "volume:" & vPlayer.Audio.Volume)

            vPlayer.Top = 0
            vPlayer.Left = 0
            vPlayer.Height = Form3.ClientSize.Height
            vPlayer.Width = Form3.ClientSize.Width
            MasqueLegende()
            Form3.PictureBox1.Visible = False
            ' La carte du titre ("Durée ...") restait dans PictureBox1 : a la fin du film elle reapparaissait un instant, avant la photo suivante (23/10/2026).
            OublierLImageImmobile()
            vPlayer.Visible = True
            vPlayer.BringToFront()
        End If

        OldPosition = -1
        TimePlaying = 0
        'Lost = 0

        ' Duree du film et position la plus avancee : servent a reconnaitre une fin NORMALE
        ' (voir plus bas, apres la boucle).
        Dim DureeFilm As Double = 0
        Dim PositionMax As Double = 0

        Call AfficheHeureDeChabbatDansCadran()
        Do Until vPlayer.IsPlaying = False
            TimePlaying = TimePlaying + 1
            If ArretDemande(PIP) Then
                GoTo fin
                Exit Do
            End If
            Application.DoEvents()
            PositionActuelle = vPlayer.Time / 1000
            If OldPosition > PositionActuelle Then PositionActuelle = OldPosition + 1
            If vPlayer.Length > 0 Then DureeFilm = vPlayer.Length / 1000.0
            If PositionActuelle > PositionMax Then PositionMax = PositionActuelle
            'si enleve Pub actif
            If Pub <> "" Then
                PositionActuelleHHMMSS = Format(DateAdd("s", PositionActuelle, "00:00:00"), "HH:mm:ss")
                k1 = InStr(Pub, "*" & PositionActuelleHHMMSS)
                If k1 > 0 Then NewPosition = Mid(Pub, k1 + 10, 8) : vPlayer.Time = TimeSpan.ParseExact(NewPosition, "c", Nothing).TotalMinutes * 60 * 1000 + 1
            End If


            If Right(UCase(Nom), 3) = ".TS" Then
                If TimePlaying > 30 Then
                    LostPictures = vPlayer.GetCurrentMedia.Statistics.LostPictures - LostPicturesOld
                    DemuxDiscontinuity = vPlayer.GetCurrentMedia.Statistics.DemuxDiscontinuity - DemuxDiscontinuityOld
                    If LostPictures > 100 Or DemuxDiscontinuity > 10 Then

                        Lost = Lost + 1
                        If Lost > 3 Then
                            'Lost = 0
                            LostPicturesOld = vPlayer.GetCurrentMedia.Statistics.LostPictures
                            DemuxDiscontinuityOld = vPlayer.GetCurrentMedia.Statistics.DemuxDiscontinuity
                            Exit Do
                        End If
                        If Lost <= 3 Then
                            vPlayer.Pause()
                            wait(3)
                            vPlayer.Play()
                            LostPicturesOld = vPlayer.GetCurrentMedia.Statistics.LostPictures
                            DemuxDiscontinuityOld = vPlayer.GetCurrentMedia.Statistics.DemuxDiscontinuity
                        End If

                    End If
                End If
            End If

            If Not PIP Then GarderLeSonDuFilm(vPlayer, vol)   ' son recoupe par ailleurs : retabli
            ' Film enchaine avec le suivant (ligne "a la suite") : a 30 s de la fin, la carte "A suivre dans ..." avec sa barre (FormAnnonceSuite).
            If Not PIP AndAlso SuiteTitre <> "" AndAlso My.Settings.AnnonceMinuterie Then
                Dim secondeDeFin As Double = If(plageAvecFin, MinuteFin * 60.0, DureeFilm)          ' (plageAvecFin : "trouve" sert aussi de drapeau aux attentes, il ne reste pas fiable dans la boucle)
                If secondeDeFin > 0 Then FormAnnonceSuite.Mettre(secondeDeFin - PositionActuelle, SuiteTitre, FenetrePrincipale())
            End If
            If trouve > 0 And PositionActuelle > MinuteFin * 60 Then FinDuFilm = True : Exit Do
            AttendreLecteur(1, PIP)
            If ArretDemande(PIP) Then Application.DoEvents() : GoTo fin   ' arret demande : pas une coupure reseau
            OldPosition = PositionActuelle
        Loop
        FormAnnonceSuite.Cacher()                   ' (le film est fini : l'annonce de la suite disparait)



        ' Fin NORMALE : le film est alle jusqu'au bout (a 3 secondes pres). Sans ce test, seul un
        ' film coupe a l'heure prevue (MinuteFin) comptait comme termine ; un film qui s'arrete
        ' tout seul - c'est le cas des films plus courts que la duree prevue, par exemple moins
        ' d'une minute - etait pris pour une coupure reseau et relance depuis le debut.
        If FinDuFilm <> True AndAlso DureeFilm > 0 AndAlso PositionMax >= DureeFilm - 3 Then
            FinDuFilm = True
            DiagLog("Film termine normalement : " & Math.Round(PositionMax) & " s sur " & Math.Round(DureeFilm) & " s (" & Nom & ")")
        End If

        ' "Lire en boucle" (case de Form6) : a la fin normale du film - ou de la plage choisie -
        ' il repart du debut de cette plage, tant qu'on ne l'arrete pas (FlagExit : touche, fin
        ' de la tache dans la minuterie...).
        ' Film en PIP : la case est relue dans les reglages ACTUELS de la tache (ils sont remis a
        ' jour quand la minuterie retrouve ce film "deja en lecture"), et non dans ceux du
        ' lancement. 02/10 : case cochee pendant que le film jouait -> il ne bouclait pas.
        Dim reglagesBoucle As String = ArgumentActuelDuPip(Argument)
        If FinDuFilm = True AndAlso Not ArretDemande(PIP) AndAlso UCase(Arg("Boucle", reglagesBoucle)) <> "TRUE" Then
            DiagLog("Film termine, sans boucle" & If(PIP, " [PIP ferme]", "") & " (" & Nom & ")")
        End If
        If FinDuFilm = True AndAlso Not ArretDemande(PIP) AndAlso UCase(Arg("Boucle", reglagesBoucle)) = "TRUE" Then
            DiagLog("Film en boucle : reprise au debut (" & Nom & ")")
            If PIP Then
                Dim sessionBoucle As SessionPip = _sessionDuFil
                If sessionBoucle IsNot Nothing Then vol = sessionBoucle.Volume          ' (volume actuel de la tache, pas celui du lancement)
            End If
            FinDuFilm = False
            PositionActuelle = 0
            GoTo EssaieEncore
        End If

        If FinDuFilm <> True AndAlso Not ArretDemande(PIP) AndAlso SilenceDesReglages.Actif Then
            ' Les reglages sont ouverts : le film a ete arrete par ce qu'on y fait (apercu d'un film, fermeture d'un
            ' apercu...), pas par une coupure reseau (14/10/2026 : "Erreur reseaux" a l'ecran en sortant de Form4).
            DiagLog("Film arrete pendant les reglages (pas une erreur reseau) : " & Math.Round(PositionMax) & " s sur " & Math.Round(DureeFilm) & " s (" & Nom & ")")
        ElseIf FinDuFilm <> True AndAlso Not ArretDemande(PIP) Then
            DiagLog("Film interrompu avant la fin : " & Math.Round(PositionMax) & " s sur " &
                    Math.Round(DureeFilm) & " s -> reprise (" & Nom & ")")
            OKrétabli = False
            AfficheTitre("Erreur réseaux", "*")
            ' Une adresse web n'est pas un fichier : l'attente du retour du fichier (90 s) n'a pas de sens, quelques
            ' secondes suffisent pour montrer le message.
            For q = 1 To If(Left(UCase(Nom), 4) = "HTTP", 3, 45)
                wait(2)
                If System.IO.File.Exists(Nom) = True Then Qtrouve = Qtrouve + 1 : OKrétabli = True : Exit For
            Next
            If Qtrouve < 5 And OKrétabli = True Then GoTo EssaieEncore
        End If
fin:
        FormAnnonceSuite.Cacher()                       ' (arret ou erreur : l'annonce de la suite disparait aussi)
        PipFilmEnCours = ""
        If Not PIP Then FlagExit = False                ' un PIP ne touche pas au drapeau partage
        Dim etaitPersistant As Boolean = PIP AndAlso PipPersistant
        If Not PIP Then FlagStop = False                ' un PIP ne touche pas a la pause partagee
        Application.DoEvents()
        vPlayer.Stop()
        Application.DoEvents()
        Form12.Close()
        TaskIsPlaying = False
        If PIP Then TerminerPip()
        RecalculerLesPip()                         ' volume des PIP encore a l'ecran
        TaskIsPlaying = PipsActifs() > 0           ' un autre PIP peut encore jouer
        If PIP <> True Then
            Form3.VlcControl2.Visible = False
            'retablieTousLesVolumes()
        End If

        ' Fin d'un film en PIP ("Rester affiche" ou non) : seul ce PIP se ferme. Avant, un PIP
        ' ordinaire qui finissait appelait EndAndContinue comme un film en plein ecran : le signal
        ' d'arret fermait les AUTRES PIP (la TV) et faisait sauter le diaporama a la suite
        ' (02/10 : film en PIP d'une minute au demarrage -> TV en PIP disparue a sa fin).
        If etaitPersistant OrElse PIP Then
            If Music AndAlso vol > 0 AndAlso (Diffusion.Mode = "PHOTO" OrElse Diffusion.Mode = "VIDEO") Then
                Dim principale As Form = FenetrePrincipale()
                If principale IsNot Nothing Then principale.BeginInvoke(New Action(Sub() MusiqueVLC.MusiqueJouer()))
            End If
        Else
            Call EndAndContinue()
        End If

        If Qtrouve = 5 Then ' si erreur apres 5 essaie de 45 sec
            AfficheTitre("Erreur réseaux", "*")
            wait(2)
            PlanB_onError()
        End If

    End Sub

    Sub Sonoff_Send(x)

        On Error GoTo fin
        grouper = False
        act = Trim(Right(x, 3))
        capteur = Trim(Left(x, Len(x) - Len(act)))

        trouve = -1
        For i = 0 To NombreDeCapteur - 1
            If Left(Sonoff(i, 0), Len(capteur)) = capteur Then trouve = i : Exit For
        Next

        If trouve <> -1 Then
            IPcapteur = Sonoff(trouve, 1)
        Else
            Exit Sub
        End If

        trouve = 0
        If Left(capteur, 1) = "$" Then
            grouper = True
            IPcapteur0 = Sonoff(trouve, 1)
            For i = Len(IPcapteur0) To 1 Step -1
                If Mid(IPcapteur0, i, 1) = "." Then trouve = i : Exit For
            Next
            p = InStr(IPcapteur0, "/")
            If p = 0 Then Exit Sub
            IPcapteur1 = Left(IPcapteur0, p - 1)
            IPcapteur2 = Left(IPcapteur0, trouve) & Mid(IPcapteur0, p + 1, Len(IPcapteur0))

        End If


        If grouper = False Then
            Sonoff_SendOne(act, IPcapteur)
        Else

            Sonoff_SendOne(act, IPcapteur1)
            Sonoff_SendOne(act, IPcapteur2)

        End If

fin:


    End Sub

    Sub Sonoff_SendOne(act, IPcapteur)

        On Error GoTo fin

        If act = "ON" Then
            url = "http://" & IPcapteur & "/control?cmd=gpio,12,1"
        Else
            url = "http://" & IPcapteur & "/control?cmd=gpio,12,0"
        End If

        Dim wb As New WebClient
        Dim html As String = wb.DownloadString(url)

fin:

    End Sub


    Function Sonoff_Test(x)

        result = False
        On Error GoTo fin

        grouper = False
        act = Trim(Right(x, 3))
        capteur = Trim(Left(x, Len(x) - Len(act)))

        trouve = -1
        For i = 0 To NombreDeCapteur - 1
            If Left(Sonoff(i, 0), Len(capteur)) = capteur Then trouve = i : Exit For
        Next

        If trouve <> -1 Then
            IPcapteur = Sonoff(trouve, 1)
        Else
            MsgBox("Erreur Sonoff " & capteur & " pas dans la liste !")
            Exit Function
        End If

        trouve = 0
        If Left(capteur, 1) = "$" Then
            grouper = True
            IPcapteur0 = Sonoff(trouve, 1)
            For i = Len(IPcapteur0) To 1 Step -1
                If Mid(IPcapteur0, i, 1) = "." Then trouve = i : Exit For
            Next
            p = InStr(IPcapteur0, "/")
            If p = 0 Then Exit Function
            IPcapteur1 = Left(IPcapteur0, p - 1)
            IPcapteur2 = Left(IPcapteur0, trouve) & Mid(IPcapteur0, p + 1, Len(IPcapteur0))

        End If


        If grouper = False Then
            result = Sonoff_TestONE(act, IPcapteur, capteur)
        Else
            result = Sonoff_TestONE(act, IPcapteur1, capteur)
            result = Sonoff_TestONE(act, IPcapteur2, capteur)
        End If


fin:
        Return result
    End Function
    Function Sonoff_TestONE(act, IPcapteur, capteur)

        result = False
        On Error GoTo fin

        result = My.Computer.Network.Ping(IPcapteur)
        'If result = False Then
        'MsgBox("Attention Sonoff " & capteur & " non connecté !")
        'End If

fin:
        Return result
    End Function
    Function CalculDiffHeureAvePub(t1 As Date, t2 As Date)
        Dim result As Date
        Dim t As TimeSpan
        'On Error Resume Next
        t = t2.Subtract(t1)
        Argument = Form6.TextBox3.Text
        k1 = InStr(UCase(Argument), "*")
        Do Until k1 = 0
            t1 = Mid(Argument, 2, 8)
            t2 = Mid(Argument, 11, 8)
            t -= t2.Subtract(t1)
            k1 = InStr(k1 + 1, UCase(Argument), "*")
        Loop

        Try
            result = t.Hours & ":" & t.Minutes & ":" & t.Seconds
        Catch ex As Exception

        End Try

        Return result
    End Function

    Function CalculAddHeure(t1, t2)



        result = ""
        On Error Resume Next

        Dim DT As Date = New Date(2016, 1, 1, 0, 0, 0, 0)
        Dim tt As TimeSpan '= TimeSpan.Parse(DT)
        Dim tdebut As TimeSpan = TimeSpan.Parse(t1)
        Dim tDurée As TimeSpan = TimeSpan.Parse(t2)

        tt = tdebut + tDurée
        'tt = tdebut.Add(tDurée)


        'result = Format(TimeSerial(tt.Hours, tt.Minutes , 0), "HH:mm")
        result = Format(TimeSerial(tt.Hours, tt.Minutes, 0), "HH:mm")
        Return result
    End Function



    Function CalculDedHeure(t1, t2)

        result = ""
        On Error Resume Next

        Dim DT As Date = New Date(2016, 1, 1, 0, 0, 0, 0)
        Dim tt As TimeSpan '= TimeSpan.Parse(DT)
        Dim tdebut As TimeSpan = TimeSpan.Parse(t1)
        Dim tDurée As TimeSpan = TimeSpan.Parse(t2)


        tt = tdebut.Add(-tDurée)
        'result = Format(TimeSerial(tt.Hours, tt.Minutes - 1, 0), "HH:mm")
        result = Format(TimeSerial(tt.Hours, tt.Minutes, 0), "HH:mm")
        Return result
    End Function

    Sub CloseWindow(Nom)
        If Nom = "Deezer" Then Exit Sub

        On Error Resume Next


        Dim myProcesses() As Process = Process.GetProcesses

        For Each p As Process In myProcesses
            'Debug.Print(p.ProcessName, p.MainWindowTitle)
            If p.MainWindowTitle.Contains(CStr(Nom)) Then
                p.CloseMainWindow()
                wait(1)
                If p.MainWindowTitle.Contains(CStr(Nom)) Then p.Kill()
            End If
        Next

        'verifie que la fenetre est fermeé sinon la tuer
        Dim myProcesses2() As Process = Process.GetProcesses
        For Each p As Process In myProcesses2
            If p.MainWindowTitle.Contains(CStr(Nom)) Then
                p.Kill()
            End If
        Next

    End Sub
    ' ------------------------------------------------------------------------------------------
    ' Lecteurs lances par Slide : seuls ceux-la sont fermes par ClosePlayer / Music_SWITCH_ON.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Processus des lecteurs lances par Slide (Deezer).</summary>
    Public LecteursLances As New HashSet(Of Integer)

    Sub RetenirLecteur(pid As Integer)
        If pid <= 0 Then Exit Sub
        SyncLock LecteursLances
            LecteursLances.Add(pid)
        End SyncLock
    End Sub

    Sub RetenirLecteur(p As Process)
        Try
            If p IsNot Nothing Then RetenirLecteur(p.Id)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Arrete les lecteurs retenus (tous, ou seulement ceux dont le nom de processus
    ''' contient <paramref name="filtre"/>, par exemple "Deezer").</summary>
    Sub FermerLesLecteursLances(Optional filtre As String = "")
        Dim pids As List(Of Integer)
        SyncLock LecteursLances
            pids = LecteursLances.ToList()
        End SyncLock
        For Each pid As Integer In pids
            Dim oublier As Boolean = True
            Try
                Using p As Process = Process.GetProcessById(pid)
                    If filtre = "" OrElse p.ProcessName.IndexOf(filtre, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        If Not p.HasExited Then p.Kill()
                    Else
                        oublier = False                     ' pas concerne : on le garde en memoire
                    End If
                End Using
            Catch ex As Exception
                ' deja ferme
            End Try
            If oublier Then
                SyncLock LecteursLances
                    LecteursLances.Remove(pid)
                End SyncLock
            End If
        Next
    End Sub

    ''' <summary>Ferme les fenetres Edge "Netflix" / "Prime Video" (ouvertes par Slide avec
    ''' -new-window). Edge rattache ces fenetres a son processus deja ouvert : on ferme donc la
    ''' FENETRE (WM_CLOSE), jamais le processus, pour garder les autres onglets.</summary>
    Sub FermerLesFenetresNetflixPrime()
        _fenetresAFermer.Clear()
        EnumWindows(_rappelNetflixPrime, IntPtr.Zero)
        For Each h As IntPtr In _fenetresAFermer
            PostMessage(h, &H10, IntPtr.Zero, IntPtr.Zero)     ' WM_CLOSE
        Next
    End Sub

    Private ReadOnly _fenetresAFermer As New List(Of IntPtr)
    ' Garde en champ : un delegue passe a Windows ne doit pas etre ramasse par le GC.
    Private ReadOnly _rappelNetflixPrime As New EnumWindowDelegate(AddressOf RepererNetflixPrime)

    Private Function RepererNetflixPrime(hWnd As IntPtr, lParam As IntPtr) As Boolean
        If Not IsWindowVisible(hWnd) Then Return True
        Dim titre As String = TitreFenetre(hWnd)
        If titre.IndexOf("Netflix", StringComparison.OrdinalIgnoreCase) < 0 AndAlso
           titre.IndexOf("Prime Video", StringComparison.OrdinalIgnoreCase) < 0 Then Return True
        Dim pid As Integer = 0
        GetWindowThreadProcessId(hWnd, pid)
        Try
            Using p As Process = Process.GetProcessById(pid)
                If String.Equals(p.ProcessName, "msedge", StringComparison.OrdinalIgnoreCase) Then _fenetresAFermer.Add(hWnd)
            End Using
        Catch ex As Exception
        End Try
        Return True
    End Function

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Function PostMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As Boolean
    End Function

    Sub KillProcess(Nom)
        'Exit Sub

        On Error Resume Next

        Dim myProcesses2() As Process = Process.GetProcesses
        For Each p As Process In myProcesses2
            pName = UCase(p.ProcessName)
            If pName.Contains(UCase(Nom)) Then
                p.Kill()
            End If
        Next

    End Sub
    Public Declare Auto Function GetWindowThreadProcessId Lib "user32.dll" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer

    Sub KillProcessByHandle(ProcessHandle)
        'Exit Sub
        Try
            'get the process ID
            GetWindowThreadProcessId(ProcessHandle, ProcId)
            'get the process
            Dim xproc As Process = Process.GetProcessById(ProcId)
            xproc.Kill()
        Catch ex As Exception

        End Try

    End Sub

    Sub ClosePlayer()
        ' Seulement les lecteurs lances par Slide (voir RetenirLecteur). Avant : tout processus dont
        ' le nom contenait VLC, EDGE ou DEEZER etait tue - donc Edge avec tous ses onglets et
        ' msedgewebview2 (nouvel Outlook, Teams, widgets...), ou le VLC ouvert par l'utilisateur.
        FermerLesFenetresNetflixPrime()
        FermerLesLecteursLances()

    End Sub
    Sub EndAndContinue(Optional ClosePIP = True)
        If ConfierAuFilPrincipal(Sub() EndAndContinue(ClosePIP), "EndAndContinue") Then Exit Sub
        FlagExit = True
        FlagStop = False
        Application.DoEvents()
        wait(2)
        If Not _suivantRapide Then ClosePlayer()
        If Music = True And (TaskIsPlaying = False OrElse (PipPersistant AndAlso PIPvolume = 0)) Then
            Music_SWITCH_ON(True)
            MusiqueVLC.MusiqueSuivant()
            Application.DoEvents()
        End If

        If ClosePIP = True Then
            Try
                Form3.VlcControl2.Stop()
            Catch ex As Exception
            End Try
            ' Les PIP ordinaires s'arretent sur FlagExit (leve plus haut) et retirent chacun leur
            ' session ; ceux "Rester affiche" continuent. L'etat global suit ce qui reste.
            If PipsActifs() = 0 Then
                Try
                    Form3.Timer4.Stop()
                Catch ex As Exception
                End Try
            End If
            RecalculerLesPip()
            TaskIsPlaying = PipsActifs() > 0
            TaskIsPlayingState = TaskIsPlaying
        End If

        'retablieTousLesVolumes()

    End Sub

    Function WindowExist(Nom)

        nom2 = UCase(Nom)
        Dim myProcesses() As Process = Process.GetProcesses

        Dim trouve As Boolean = False
        For Each p As Process In myProcesses
            If p.MainWindowTitle.ToUpper.Contains(CStr(nom2)) Then trouve = True : Exit For
            'If p.MainWindowTitle.Contains(Nom) Then trouve = True : Exit For
        Next

        Return trouve

    End Function
    Function WindowProcess(Nom)

        nom2 = UCase(Nom)
        Dim myProcesses() As Process = Process.GetProcessesByName(Nom)

        Dim trouve As Boolean = False
        For Each p As Process In myProcesses
            nm = p.ProcessName.ToUpper
            If nm.Contains(nom2) Then trouve = True : Exit For
            'r = r & nm & vbCrLf
        Next
        'MsgBox(r)
        Return trouve
    End Function

    Function WindowHandle(Nom)

        Dim myProcesses() As Process = Process.GetProcesses

        Dim trouve As IntPtr = 0

        For Each p As Process In myProcesses
            pName = UCase(p.ProcessName)
            If pName.Contains(UCase(Nom)) Then trouve = p.MainWindowHandle : Exit For
        Next

        Return trouve

    End Function

    Function ProcessExist(Nom)



        Dim myProcesses() As Process = Process.GetProcesses

        Dim trouve As Boolean = False
        For Each p As Process In myProcesses
            Pname = UCase(p.ProcessName)
            If Pname.Contains(UCase(Nom)) Then trouve = True : Exit For
        Next

        Return trouve

    End Function



    ''' <summary>Volume general Windows (0 a 100), -1 si CoreAudio.dll est indisponible : Slide ne s'arrete pas pour cela
    ''' (15/10/2026 : "erreur sur un nouveau PC", CoreAudio.dll manquante).</summary>
    Function GetVol() As Integer
        Try
            Return GetVolCoreAudio()
        Catch ex As Exception
            DiagCoreAudio(ex)
            Return -1
        End Try
    End Function

    Private _coreAudioDit As Boolean

    Private Sub DiagCoreAudio(ex As Exception)
        If _coreAudioDit Then Exit Sub
        _coreAudioDit = True
        DiagLog("Volume Windows : CoreAudio.dll indisponible (" & ex.GetType().Name & " : " & ex.Message & ") : le volume general n'est pas regle. Une mise a jour retablit le fichier.")
    End Sub

    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
    Private Function GetVolCoreAudio() As Integer        'Function to read current volume setting
        Dim MasterMinimum As Integer = 0
        Dim DevEnum As New MMDeviceEnumerator()
        Dim device As MMDevice = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
        Dim Vol As Integer = 0

        With device.AudioEndpointVolume
            Vol = CInt(.MasterVolumeLevelScalar * 100)
            If Vol < MasterMinimum Then
                Vol = MasterMinimum / 100
            End If
        End With
        Return Vol
    End Function
    ''' <summary>Coupe le son d'un lecteur VLC juste AVANT de le demarrer. Sans cela, VLC joue les
    ''' premieres secondes au volume qu'il avait (celui de la lecture precedente, ou 100 %) : c'est
    ''' le coup de son au debut. Le volume voulu est applique ensuite par SetVolume, qui retablit
    ''' le son. Sans effet tant que le lecteur n'est pas pret (Audio vaut alors Nothing).</summary>
    Sub CouperLeSonAvantDemarrage(lecteur As Vlc.DotNet.Forms.VlcControl)
        Try
            If lecteur IsNot Nothing AndAlso lecteur.Audio IsNot Nothing Then lecteur.Audio.IsMute = True
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Attend <paramref name="secondes"/> en gardant le lecteur MUET (23/10/2026 : "ne lance un film avec le volume que quand le volume desire est regle"). Quand VLC cree sa sortie audio,
    ''' il reprend son volume (100 %) et annule le silence demande avant Play : pendant l'attente du debut de lecture et du saut a la minute choisie, le film etait donc audible, trop fort, jusqu'a
    ''' ReglerLeSonApresDemarrage. Le silence est redemande toutes les 20 ms ; le son ne revient qu'avec le volume voulu.</summary>
    Sub AttendreEnSilence(lecteur As Vlc.DotNet.Forms.VlcControl, secondes As Double)
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do
            Try
                If lecteur IsNot Nothing AndAlso lecteur.Audio IsNot Nothing AndAlso Not lecteur.Audio.IsMute Then lecteur.Audio.IsMute = True
            Catch ex As Exception
            End Try
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < secondes * 1000
    End Sub

    ''' <summary>Son de la fenetre d'apercu (Form11). Le volume n'est retenu par VLC qu'une fois le
    ''' flux reellement en lecture, piste audio comprise : avant, le reglage est perdu et l'apercu
    ''' reste muet. On attend donc la piste audio (15 s au plus), puis on regle le volume.</summary>
    Sub ReglerLeSonDeLApercu(vol As Integer)
        Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form11.VlcControl1
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim piste As Boolean = False
        Do
            Try
                If lecteur.IsPlaying AndAlso lecteur.Audio IsNot Nothing AndAlso
                   lecteur.Audio.Tracks.All.Any(Function(p) p.ID <> -1) Then piste = True
            Catch ex As Exception
            End Try
            If piste OrElse Not Form11.Visible Then Exit Do
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        Loop While chrono.ElapsedMilliseconds < 15000
        If Not Form11.Visible Then Exit Sub
        If piste Then AttendreMillisecondes(500)       ' la sortie audio se cree juste apres la piste
        SetVolume(vol, lecteur)
        Dim lu As String = "?"
        Try
            lu = "volume VLC " & lecteur.Audio.Volume & ", muet=" & lecteur.Audio.IsMute &
                 ", piste=" & If(lecteur.Audio.Tracks.Current Is Nothing, "aucune", lecteur.Audio.Tracks.Current.ID.ToString())
        Catch ex As Exception
        End Try
        DiagLog("Apercu : son regle a " & vol & " apres " & chrono.ElapsedMilliseconds & " ms" &
                If(piste, "", " (piste audio non trouvee)") & " -> " & lu)

        ' La sortie audio peut n'etre creee qu'apres ce premier reglage (le tuner se cale) et
        ' repartir muette : on note l'etat puis on redonne le son 2, 5 et 9 secondes plus tard.
        Dim passage As Integer = 0
        Dim rappel As New System.Windows.Forms.Timer With {.Interval = 2000}
        AddHandler rappel.Tick,
            Sub()
                passage += 1
                If passage >= 3 OrElse Not Form11.Visible Then rappel.Stop() : rappel.Dispose()
                If Not Form11.Visible Then Exit Sub
                rappel.Interval = If(passage = 1, 3000, 4000)
                Dim volVoulu As Integer = Form11.TrackBar2.Value
                Dim etat As String = "?"
                Try
                    etat = "joue=" & lecteur.IsPlaying & " temps=" & lecteur.Time & " volume VLC " & lecteur.Audio.Volume &
                           ", muet=" & lecteur.Audio.IsMute & ", piste=" &
                           If(lecteur.Audio.Tracks.Current Is Nothing, "aucune", lecteur.Audio.Tracks.Current.ID.ToString()) &
                           " sur " & lecteur.Audio.Tracks.Count & ", volume general=" & GetVol() & " (regle " & VolumeGeneral & ", VolumeOFF=" & VolumeOFF & ")"
                Catch ex As Exception
                    etat = "erreur " & ex.Message
                End Try
                ' Niveau reellement envoye a Windows par Slide (vumetre du melangeur), sur 300 ms.
                Dim crete As Double = -1
                Try
                    VumetreSession.Rafraichir()
                    crete = 0
                    For n As Integer = 1 To 6
                        crete = Math.Max(crete, VumetreSession.Crete())
                        Threading.Thread.Sleep(50)
                    Next
                Catch ex As Exception
                End Try
                DiagLog("Apercu : etat du son au rappel " & passage & " (voulu " & volVoulu & ") -> " & etat &
                        ", niveau envoye a Windows=" & crete.ToString("0.000") & ", musique=" & MusiqueVLC.MusiqueFichierEnCours)
                If volVoulu <= 0 Then Exit Sub
                RendreLeSonAuLecteur(lecteur, volVoulu)
                ' Rien ne sort alors que tout est regle : on fait recreer la sortie audio a VLC en
                ' retirant puis en remettant la piste audio.
                If crete >= 0 AndAlso crete < 0.001 Then
                    Try
                        Dim courante = lecteur.Audio.Tracks.Current
                        Dim aucune = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID = -1)
                        If courante IsNot Nothing AndAlso courante.ID <> -1 AndAlso aucune IsNot Nothing Then
                            lecteur.Audio.Tracks.Current = aucune
                            AttendreMillisecondes(300)
                            lecteur.Audio.Tracks.Current = courante
                            AttendreMillisecondes(300)
                            lecteur.Audio.IsMute = False
                            lecteur.Audio.Volume = VolumeVLC(volVoulu)
                            DiagLog("Apercu : aucun son ne sort -> sortie audio recreee (piste " & courante.ID & ")")
                        End If
                    Catch ex As Exception
                        DiagLog("Apercu : recreation de la sortie audio : " & ex.Message)
                    End Try
                End If
            End Sub
        rappel.Start()
    End Sub

    ''' <summary>A appeler AVANT de changer de media, pendant que le lecteur joue encore : VLC
    ''' demarre le media suivant avec le volume qu'il a a cet instant. En le mettant tout de suite
    ''' au niveau voulu, la lecture suivante ne peut plus partir au volume du film precedent.
    ''' (Mesure au banc : un silence demande apres Stop, lui, n'est PAS conserve.)</summary>
    Sub PreparerLeVolumeAvantChangement(lecteur As Vlc.DotNet.Forms.VlcControl, volume As Integer)
        Try
            If lecteur IsNot Nothing AndAlso lecteur.Audio IsNot Nothing AndAlso lecteur.Audio.Volume >= 0 Then
                lecteur.Audio.Volume = VolumeVLC(volume)
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>A appeler juste APRES Play. VLC ne cree sa sortie audio qu'un instant plus tard,
    ''' et c'est a ce moment qu'il reprend SON volume (100 %) en annulant le silence demande avant
    ''' le demarrage : d'ou le coup de son des premieres secondes. On redemande donc le silence
    ''' tant que la sortie n'existe pas (Volume reste a -1), puis on applique le volume voulu, ce
    ''' qui retablit le son. Attente plafonnee a <paramref name="attenteMaxMs"/>.</summary>
    Sub ReglerLeSonApresDemarrage(lecteur As Vlc.DotNet.Forms.VlcControl, volume As Integer,
                                  Optional attenteMaxMs As Integer = 3000)
        If lecteur Is Nothing Then Exit Sub

        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim prete As Boolean = False
        Do
            Try
                If lecteur.Audio IsNot Nothing Then
                    lecteur.Audio.IsMute = True
                    If lecteur.Audio.Volume >= 0 Then prete = True
                End If
            Catch ex As Exception
                ' sortie audio pas encore en place
            End Try
            If prete Then Exit Do
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < attenteMaxMs

        If volume > 0 Then
            SetVolume(volume, lecteur)      ' volume voulu, et son retabli
        Else
            ' Video SANS son ("VideoAvecAudio" decoche) : le lecteur reste simplement muet. Surtout
            ' pas SetVolume(0) : il coupe tous les autres lecteurs (le PIP) et met la MUSIQUE en
            ' pause, alors que c'est justement pour l'entendre que le son de la video est coupe.
            Try
                If lecteur.Audio IsNot Nothing Then lecteur.Audio.IsMute = True
            Catch ex As Exception
            End Try
        End If
        DiagLog("Son : sortie audio prete en " & chrono.ElapsedMilliseconds & " ms, volume " & volume &
                If(prete, "", " (delai depasse)") & If(volume > 0, "", " (son de la video coupe par le reglage, musique conservee)"))
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Couper / rendre le son d'UN lecteur.
    '
    ' Mesure du 30/09 : le lecteur des films (Form3.VlcControl2) et le PIP (Form12) partagent le
    ' meme interrupteur "muet" de Windows. Couper le PIP coupait le film (film muet), rendre le son
    ' au film le rendait au PIP (film et LCI ensemble). On coupe donc un lecteur en DESACTIVANT SA
    ' PISTE AUDIO (propre a chaque lecteur), en plus de l'interrupteur ; la piste est remise quand
    ' on lui rend le son.
    ' ------------------------------------------------------------------------------------------
    Private ReadOnly _pisteAvantCoupure As New Dictionary(Of Vlc.DotNet.Forms.VlcControl, Integer)

    Sub CouperLeLecteur(lecteur As Vlc.DotNet.Forms.VlcControl)
        If lecteur Is Nothing Then Exit Sub
        Try
            If lecteur.Audio Is Nothing Then Exit Sub
            lecteur.Audio.IsMute = True
            If Not lecteur.IsPlaying Then Exit Sub
            Dim actuelle = lecteur.Audio.Tracks.Current
            If actuelle Is Nothing OrElse actuelle.ID = -1 Then Exit Sub
            Dim aucune = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID = -1)
            If aucune Is Nothing Then Exit Sub
            SyncLock _pisteAvantCoupure
                _pisteAvantCoupure(lecteur) = actuelle.ID
            End SyncLock
            lecteur.Audio.Tracks.Current = aucune
        Catch ex As Exception
        End Try
    End Sub

    Sub RendreLeSonAuLecteur(lecteur As Vlc.DotNet.Forms.VlcControl, volume As Integer)
        If lecteur Is Nothing Then Exit Sub
        ' Reglages ouverts : un lecteur qu'ils ont coupe reste muet jusqu'a leur fermeture.
        If SilenceDesReglages.Retient(lecteur, VolumeVLC(volume)) Then Exit Sub
        Try
            If lecteur.Audio Is Nothing Then Exit Sub
            Dim avant As Integer = -1
            SyncLock _pisteAvantCoupure
                If _pisteAvantCoupure.TryGetValue(lecteur, avant) Then _pisteAvantCoupure.Remove(lecteur)
            End SyncLock
            Dim actuelle = lecteur.Audio.Tracks.Current
            If actuelle Is Nothing OrElse actuelle.ID = -1 Then
                Dim piste = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID = avant AndAlso avant <> -1)
                If piste Is Nothing Then piste = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID <> -1)
                If piste IsNot Nothing Then lecteur.Audio.Tracks.Current = piste
            End If
            lecteur.Audio.IsMute = False
            lecteur.Audio.Volume = VolumeVLC(volume)
            LibererLeSonDeLaTache()                     ' le nouveau son est au bon volume
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Minuterie : des qu'une tache est lancee, le son en cours s'arrete, et le son ne revient
    ' que quand le volume est le bon (demande de Jacob, 09/10/2026).
    '
    ' Avant : la musique, la video du diaporama ou le film precedent continuaient pendant toute
    ' la preparation de la tache (attentes de 1 a 4 s, ouverture du flux), et EndAndContinue
    ' relancait meme la musique (morceau suivant) juste avant le film. Maintenant tout est coupe
    ' au lancement, et la musique n'est plus relancee tant que le lecteur de la tache n'a pas
    ' recu son volume (RendreLeSonAuLecteur) ou que le lancement n'est pas termine.
    ' ------------------------------------------------------------------------------------------
    Private _sonRetenuDepuis As Date = Date.MinValue

    ''' <summary>Garde-fou : passe ce delai, la musique n'est plus retenue quoi qu'il arrive.</summary>
    Private Const SonRetenuMaxSecondes As Integer = 180

    ''' <summary>Au lancement d'une tache qui prend le son (TV, film, IPTV, SAT avec du volume,
    ''' ecran noir) : coupe tous les lecteurs et met la musique en pause, tout de suite.</summary>
    Sub CouperLeSonPourLaTache(quoi As String)
        Try
            For Each Frm As Form In My.Application.OpenForms.Cast(Of Form)().ToList()
                For Each c As Control In Frm.Controls
                    Dim lecteur As Vlc.DotNet.Forms.VlcControl = TryCast(c, Vlc.DotNet.Forms.VlcControl)
                    If lecteur Is Nothing OrElse lecteur.Name = "VlcMusique" Then Continue For
                    CouperLeLecteur(lecteur)
                Next
            Next
        Catch ex As Exception
            DiagLog("Minuterie (son coupe) : " & ex.Message)
        End Try
        MusiqueVLC.MusiquePause()
        _sonRetenuDepuis = Now
        DiagLog("Minuterie : son en cours arrete au lancement de " & quoi)
    End Sub

    ''' <summary>Tache TV / film / IPTV / SAT : le son en cours n'est coupe que si elle en a un
    ''' (volume > 0). Avec un volume a 0 (PIP muet...), la musique continue.</summary>
    Sub CouperLeSonPourLaTache(quoi As String, volume As Object)
        If Val(volume) <= 0 Then Exit Sub
        CouperLeSonPourLaTache(quoi)
    End Sub

    ''' <summary>Tache d'arret ("Chaine OFF", "TV OFF", "Close All Player"...) : ce qui va
    ''' s'arreter se tait tout de suite, sans attendre sa fermeture (le lecteur plein ecran et
    ''' les PIP ordinaires ; les PIP "Rester affiche" sont coupes par ArreterLesPip).</summary>
    Sub CouperLeSonDeCeQuiSArrete()
        Try
            CouperLeLecteur(Form3.VlcControl2)
            For Each s As SessionPip In SessionsPip()
                If s.Persistant OrElse s.Fenetre Is Nothing OrElse s.Fenetre.IsDisposed Then Continue For
                CouperLeLecteur(s.Fenetre.VlcControl1)
            Next
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Vrai pendant le lancement d'une tache : la musique ne doit pas repartir.</summary>
    Function SonRetenuPourUneTache() As Boolean
        Return (Now - _sonRetenuDepuis).TotalSeconds < SonRetenuMaxSecondes
    End Function

    ''' <summary>Fin du lancement (lecteur de la tache a son volume, ou tache terminee).</summary>
    Sub LibererLeSonDeLaTache()
        _sonRetenuDepuis = Date.MinValue
    End Sub

    ''' <summary>
    ''' Pendant tout le film (plein ecran, volume > 0) : si son son a ete recoupe par ailleurs, on le
    ''' retablit. Mesure du 30/09 : le son etait retabli (volume 56) puis, 16 ms plus tard et pour
    ''' toute la suite, "muet" alors que VLC produisait bien le son : le film restait muet.
    ''' </summary>
    Private _retablissementsFilm As Integer

    Sub GarderLeSonDuFilm(lecteur As Vlc.DotNet.Forms.VlcControl, volume As Integer)
        If volume <= 0 OrElse lecteur Is Nothing Then Exit Sub
        Try
            If lecteur.Audio Is Nothing OrElse Not lecteur.Audio.IsMute Then Exit Sub
            RendreLeSonAuLecteur(lecteur, volume)
            _retablissementsFilm += 1
            If _retablissementsFilm <= 5 Then
                DiagLog("Film : son recoupe par ailleurs -> retabli (volume " & volume & ", fois " & _retablissementsFilm & ")")
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Apres la relance d'un flux gele (TV, IPTV, SAT). Un PIP ne regle que SON propre son :
    ''' avant, SetVolume coupait tous les autres lecteurs (le film en cours devenait muet) et
    ''' mettait la musique en pause. En plein ecran, reglage complet comme au demarrage. Dans les
    ''' deux cas, on attend que VLC ait recree sa sortie audio (sinon le reglage est ignore).
    ''' </summary>
    Sub RetablirLeSonApresRelance(lecteur As Vlc.DotNet.Forms.VlcControl, volume As Integer, estPip As Boolean)
        If lecteur Is Nothing Then Exit Sub
        If Not estPip Then
            ReglerLeSonApresDemarrage(lecteur, volume)
            Exit Sub
        End If
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do
            Try
                If lecteur.Audio IsNot Nothing AndAlso lecteur.Audio.Volume >= 0 Then Exit Do
            Catch ex As Exception
            End Try
            Application.DoEvents()
            Threading.Thread.Sleep(20)
        Loop While chrono.ElapsedMilliseconds < 3000
        If volume > 0 Then
            RendreLeSonAuLecteur(lecteur, volume)
        Else
            CouperLeLecteur(lecteur)                     ' piste audio desactivee (interrupteur partage)
        End If
        DiagLog("PIP relance : son du PIP seul regle (volume " & volume & ", " & chrono.ElapsedMilliseconds & " ms)")
    End Sub

    ''' <summary>Regle le son du lecteur VLC <paramref name="WinForm"/> (c'est un VlcControl) et
    ''' coupe celui de tous les autres. x = 0 coupe le son. Renvoie 1 si le lecteur a ete trouve.
    '''
    ''' Ratés corriges :
    ''' - le volume demande pouvait depasser la limite de VLC (volume + boost de 30 %) : il etait
    '''   alors refuse et le son ne changeait pas. Il est maintenant borne.
    ''' - le succes etait juge en RELISANT le volume ; tant que rien ne joue, VLC ne le rend pas,
    '''   donc l'appel etait declare en echec alors que tout allait bien.
    ''' - cet echec appelait SetVolumeAllFormS, qui CREAIT une instance de chaque fenetre du
    '''   programme (y compris celles a lecteur video) : couteux, jamais libere, et sans effet
    '''   puisque ces instances neuves ne jouent rien. Supprime.
    ''' - le lecteur etait reconnu en comparant des noms de parents : sans parent, la comparaison
    '''   levait une erreur avalee. On compare maintenant l'objet lui-meme.</summary>
    Function SetVolume(x As Integer, WinForm As Object) As Integer
        ' Appelee aussi par les PIP TV/IPTV/SAT, depuis leur propre thread, quand ils relancent un
        ' flux gele. Elle touche alors les lecteurs de la fenetre principale et met la musique en
        ' pause : "Form3" y designait une fenetre fantome (journal de l'autre PC : "Musique (pause) :
        ' Application-defined or object-defined error" a chaque relance), puis Slide s'arretait
        ' net apres 9 a 13 heures. Le reglage est donc confie au thread de l'interface.
        If ConfierAuFilPrincipal(Sub() SetVolume(x, WinForm), "SetVolume") Then Return 1
        SetMasterVol(VolumeGeneral)
        trouve = 0
        Dim cible As Vlc.DotNet.Forms.VlcControl = Nothing

        Try
            For Each Frm As Form In My.Application.OpenForms
                For Each c As Control In Frm.Controls
                    If TypeOf (c) Is Vlc.DotNet.Forms.VlcControl Then
                        VlcNet = DirectCast(c, Vlc.DotNet.Forms.VlcControl)
                        ' La musique a son propre lecteur et son propre volume (MusiqueVLC) : on ne
                        ' la coupe pas ici, sinon regler le son d'une video la rendrait muette.
                        If VlcNet.Name = "VlcMusique" Then Continue For
                        ' Lecteur pas encore demarre : Audio n'existe pas et tout acces au volume
                        ' echoue (verifie). Il n'y a alors rien a regler ni a couper sur celui-la.
                        If VlcNet.Audio Is Nothing Then Continue For
                        Try
                            If WinForm Is Nothing OrElse Not EstLeMemeLecteur(VlcNet, WinForm) Then
                                CouperLeLecteur(VlcNet)             ' piste desactivee : interrupteur partage
                            Else
                                cible = VlcNet                      ' reglee EN DERNIER (voir plus bas)
                                trouve = 1
                            End If
                        Catch ex As Exception
                            DiagLog("SetVolume (" & Frm.Name & "." & c.Name & ") : " & ex.Message)
                        End Try
                    End If
                Next
            Next

            ' Le lecteur voulu en dernier : si son interrupteur "muet" est partage avec un lecteur
            ' coupe juste avant, c'est son reglage a lui qui doit l'emporter.
            If cible IsNot Nothing Then
                If x = 0 Then
                    CouperLeLecteur(cible)
                Else
                    RendreLeSonAuLecteur(cible, x)
                    Application.DoEvents()
                End If
            End If

        Catch ex As Exception
            DiagLog("SetVolume : " & ex.Message)
        End Try

        MusiqueVLC.MusiquePause()
        If trouve = 0 Then DiagLog("SetVolume : lecteur introuvable parmi les fenetres ouvertes (volume " & x & ")")
        Return trouve
    End Function

    ''' <summary>Le meme controle VLC : l'objet lui-meme, sinon le meme nom dans la meme fenetre
    ''' (comparaison d'origine, gardee au cas ou l'appelant passe un autre exemplaire).</summary>
    Private Function EstLeMemeLecteur(lecteur As Vlc.DotNet.Forms.VlcControl, demande As Object) As Boolean
        If ReferenceEquals(lecteur, demande) Then Return True
        Dim autre As Control = TryCast(demande, Control)
        If autre Is Nothing Then Return False
        If lecteur.Name <> autre.Name Then Return False
        Dim p1 As String = If(lecteur.Parent Is Nothing, "", lecteur.Parent.Name)
        Dim p2 As String = If(autre.Parent Is Nothing, "", autre.Parent.Name)
        Return p1 = p2
    End Function

    ''' <summary>Garde-fou. Mesure faite avec la VLC livree dans data\VLC : elle accepte 0 a 400.
    ''' Avec le boost de 30 %, un volume de 100 donne 130 : on reste toujours en dessous.</summary>
    Public Const VolumeVLCMaxi As Integer = 400

    ''' <summary>Volume envoye a VLC : le volume demande, augmente du boost si l'option est cochee
    ''' (VLCBoost = True vaut -1, d'ou le double signe), puis borne.</summary>
    Private Function VolumeVLC(x As Integer) As Integer
        Dim v As Integer = x - 1 * (VolumeVLCBoost * CInt(VLCBoost))   'true = -1 donc - par - = +
        Return Math.Max(0, Math.Min(VolumeVLCMaxi, v))
    End Function

    ''' <summary>Volume VLC des videos du diaporama selon les reglages (celui que SetVideoVolSlide
    ''' applique), sans correction : base du lissage du son (NiveauSonore).</summary>
    Public Function VolumeVlcVideoDiaporama() As Integer
        Return VolumeVLC(If(VolumeVideoSlideParMinuterie <> 0, VolumeVideoSlideParMinuterie, VolumeVideoSlide))
    End Function
    Sub SetVol(x)
        Try
            SetVolCoreAudio(x)
        Catch ex As Exception
            DiagCoreAudio(ex)
        End Try
    End Sub

    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
    Private Sub SetVolCoreAudio(x)

        If VolumeOFF = True Then Exit Sub
        Dim DevEnum As New MMDeviceEnumerator()
        Dim device As MMDevice = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)

        v = (VolumeGeneral / 100) * (x / 100)
        device.AudioEndpointVolume.MasterVolumeLevelScalar = v
        VolumeActuel = x

    End Sub

    Sub SetMasterVol(x)
        Try
            SetMasterVolCoreAudio(x)
        Catch ex As Exception
            DiagCoreAudio(ex)
        End Try
    End Sub

    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
    Private Sub SetMasterVolCoreAudio(x)

        If VolumeOFF = True Then Exit Sub

        Dim DevEnum As New MMDeviceEnumerator()
        Dim device As MMDevice = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia)
        device.AudioEndpointVolume.MasterVolumeLevelScalar = x / 100

    End Sub

    Sub SetMusicVolSlide(x)
        ' Trace de diagnostic : dit pourquoi le volume de la musique n'est pas applique.
        ' (Deezer : seulement l'application externe ; le Deezer integre a Slide suit ce reglage.)
        Dim deezerExterne As Boolean = Music_Deezer = True AndAlso Not MusiqueDeezer.Actif
        If PIPvolume > 0 OrElse Music = False OrElse VolumeOFF = True OrElse deezerExterne Then
            DiagLog("Musique : volume non applique (PIPvolume=" & PIPvolume & " Music=" & Music &
                    " VolumeOFF=" & VolumeOFF & " Deezer=" & Music_Deezer & ")")
        End If
        If PIPvolume > 0 Then Exit Sub
        If Music = False Then Exit Sub
        If VolumeOFF = True Then Exit Sub
        If deezerExterne Then Exit Sub
        vv = x
        ' Deezer dans Slide : il a son propre volume, a la place de "Volume Music Slide" : celui
        ' de la minuterie (taches "Deezer", "Lance Deezer", "Volume Deezer") s'il y en a un, sinon
        ' celui des reglages (onglet Audio, cadre Deezer). (0 = silence demande : garde.)
        If MusiqueDeezer.Actif Then
            If Val(x) > 0 Then vv = If(VolumeDeezerParMinuterie >= 0, VolumeDeezerParMinuterie, VolumeDeezer)
        ElseIf VolumeMusicSlideParMinuterie <> 0 Then
            vv = VolumeMusicSlideParMinuterie
        End If
        MusiqueVLC.MusiqueDefinirVolume(CInt(Val(vv)))
        MusiqueVLC.MusiqueJouer()
        SetVol(100)

    End Sub
    Sub SetVideoVolSlide(x)
        If PIPvolume > 0 Then Exit Sub
        If VolumeOFF = True Then Exit Sub
        vv = x
        If VolumeVideoSlideParMinuterie <> 0 Then vv = VolumeVideoSlideParMinuterie
        'If Music = False Then Exit Sub
        If VideoWithAudio = False Then Exit Sub
        ' Video muette (volume 0, pas de piste son) : elle reste muette et la musique continue ;
        ' SetVolume couperait aussi le PIP et mettrait la musique en pause.
        If VideoDuDiaporamaMuette() Then
            Try
                If Form3.VlcControl1.Audio IsNot Nothing Then Form3.VlcControl1.Audio.IsMute = True
            Catch ex As Exception
            End Try
            Exit Sub
        End If
        SetVolume(vv, Form3.VlcControl1)
        MusiqueVLC.MusiquePause()

    End Sub
    Sub AjouterUneLigne(x As Object, r As Object, Optional alaFin As Boolean = False)


        'x=
        '1 premiere ligne seulement
        '2 rafraichir seulement et maj tri
        '3 rafraichir et ajouter une ligne
        'insertPosition = clickedCell.rowindex

        insertPos = 0 : plus1 = 1
        If Form4.DataGridView1.SelectedRows.Count > 0 Then
            insertPos = Form4.DataGridView1.SelectedRows.Item(0).Index
        End If
        If insertPos > 0 Then r = insertPos : plus1 = 0
        If x = 1 Then Form4.DataGridView1.Rows.Add({True, "Vendredi", "16:00", "Film", "", "", "", "100", "999999"}) : Exit Sub

        t = 0

        For Each row As DataGridViewRow In Form4.DataGridView1.Rows
            h = row.Cells("Heure").FormattedValue
            j = UCase(row.Cells("Jour").FormattedValue)

            t = 0
            For i = 0 To 6
                If UCase(jourDeLaSemaine(i)) = j Then t = i
            Next

            If t = 0 Then
                If j = "LUN-VEN" Then t = 7
                If j = "SAM-DIM" Then t = 8
                If j = "TLJ" Then t = 9
                If j = "BOOT" Then t = 0
            End If
            f = Format(t, "00") + Mid(h, 1, 2) + Mid(h, 4, 2)
            row.Cells("tri").Value = f
            row.Cells("Heure").Value = Left(row.Cells("Heure").FormattedValue, 5)
        Next

        ' Aucune ligne entiere selectionnee : la ligne de reference est la DERNIERE du tableau, quelle que soit la case ou se trouve le curseur
        ' (21/10/2026 : "le bouton + n'insere pas de ligne a la fin").
        If x = 3 AndAlso alaFin Then
            For i As Integer = Form4.DataGridView1.Rows.Count - 1 To 0 Step -1
                If Not Form4.DataGridView1.Rows(i).IsNewRow Then r = i : Exit For
            Next
        End If
        ' (Type et Action vides : plus un obstacle pour ajouter une ligne ; les lignes sans action sont retirees a l'enregistrement.)
        If x <> 3 Then
            aa = Form4.DataGridView1.Rows(r).Cells("Type").FormattedValue : If aa = "" Then Exit Sub
            aa = Form4.DataGridView1.Rows(r).Cells("Action").FormattedValue : If aa = "" Then Exit Sub
        End If
        h = Form4.DataGridView1.Rows(r).Cells("Heure").FormattedValue : If h = "" Then Exit Sub
        j = UCase(Form4.DataGridView1.Rows(r).Cells("Jour").FormattedValue) : If j = "" Then Exit Sub

        ' Ligne de reference de l'insertion, retenue AVANT le tri (qui deplace les lignes) : la nouvelle ligne est
        ' toujours inseree juste APRES la ligne selectionnee (ligne entiere ou cellule courante), d'apres elle
        ' (20/10/2026 : "il faut rajouter une ligne apres la ligne selectionnee, pas celle d'avant" ; une ligne
        ' entiere selectionnee faisait inserer AVANT elle).
        Dim reference As DataGridViewRow = Nothing
        Dim ancre As DataGridViewRow = Form4.DataGridView1.Rows(r)
        If x = 3 Then reference = Form4.DataGridView1.Rows(r)

        Form4.DataGridView1.Sort(Form4.DataGridView1.Columns("tri"), System.ComponentModel.ListSortDirection.Ascending)

        ' Aucune ligne entiere selectionnee (20/10/2026) : la nouvelle ligne va a la FIN du tableau, apres la derniere ligne.
        If x = 3 AndAlso alaFin Then
            For i As Integer = Form4.DataGridView1.Rows.Count - 1 To 0 Step -1
                If Not Form4.DataGridView1.Rows(i).IsNewRow Then reference = Form4.DataGridView1.Rows(i) : Exit For
            Next
        End If

        If x = 3 Then
            ' Heure : celle de la ligne de reference + sa duree (1 minute par defaut).
            h = Left(CStr(reference.Cells("Heure").FormattedValue), 5)
            D = Mid(CStr(reference.Cells("Durée").FormattedValue), 1, 5)
            If D = "" Then D = "00:01"
            Dim nextTime As String = CalculAddHeure(h, D)

            ' Jour : celui de la ligne de reference (et non de la derniere ligne du tableau,
            ' comme avant), le lendemain si l'heure passe minuit. Lun-Ven, Sam-Dim, TLJ et BOOT
            ' sont repris tels quels (avant : jour vide).
            Dim jourRef As String = CStr(reference.Cells("Jour").FormattedValue)
            Dim nextDay As String = jourRef
            Dim indexJour As Integer = Array.FindIndex(jourDeLaSemaine, Function(s) String.Equals(s, jourRef, StringComparison.OrdinalIgnoreCase))
            If indexJour >= 0 AndAlso indexJour <= 6 Then
                Dim passeMinuit As Boolean = String.CompareOrdinal(nextTime, h) < 0
                nextDay = jourDeLaSemaine(If(passeMinuit, (indexJour + 1) Mod 7, indexJour))
            End If

            ' Position apres le tri : juste apres la ligne de reference (la ligne selectionnee, ou la derniere du tableau).
            Dim position As Integer = reference.Index + 1
            Form4.DataGridView1.Rows.Insert(position, {True, nextDay, nextTime, "Film", "", "", "", "100", "999999"})
            Form4.DataGridView1.CurrentCell = Form4.DataGridView1.Rows(position).Cells("Action")

        End If


    End Sub


    Public Function TableExists(DatabasePath, TableName) As Boolean

        cnnStr = OLEDBPROVIDER & DatabasePath

        Dim dbConn As New OleDbConnection(cnnStr)
        dbConn.Open()
        Dim restrictions(3) As String
        restrictions(2) = TableName
        Dim dbTbl As DataTable = dbConn.GetSchema("Tables", restrictions)

        If dbTbl.Rows.Count = 0 Then
            TableExists = False
        Else
            TableExists = True
        End If

        dbConn.Close()


        Return TableExists
    End Function



    Sub CreateSqlDatabaseIFnoExist()

        Dim mycommand As OleDbCommand
        Dim myconnection As OleDbConnection
        Dim sql As String
        Dim table As New DataTable
        Dim chrono As Stopwatch = Stopwatch.StartNew()


        'If DatabaseNetPath.ToUpper.Contains(".MDB") = False Then
        ' DatabaseNetPath = AppPath & "\data\slide.accdb"
        ' MinuterieEnReseaux = False
        ' End If
        dir_name = ""

        If MinuterieEnReseaux = True Then
            trouve = 0
            DatabasePath = DatabaseNetPath & "\slide.accdb"
            For i = Len(DatabasePath) To 1 Step -1
                a = Mid(DatabasePath, i, 1)
                If a = "\" Then trouve = i : Exit For
            Next

            If trouve > 0 Then dir_name = Mid(DatabasePath, 1, trouve - 1)

            If Not Directory.Exists(dir_name) Then
                Directory.CreateDirectory(dir_name)
            End If

        End If

        If My.Computer.FileSystem.FileExists(DatabasePath) = False Then

            ' Base VIDE (seule la table de la minuterie est creee ci-dessous) : si c'est la base
            ' locale, la table Cities des localites manquera. On le note pour pouvoir l'expliquer.
            DiagLog("Base Access absente, creee vide (sans la table Cities) : " & DatabasePath)
            ' (L'objet COM ADOX n'est cree qu'ici, quand il faut vraiment creer la base : avant, il l'etait a chaque ouverture des reglages.)
            Dim cat As New Catalog()
            cat.Create(OLEDBPROVIDER & DatabasePath) '& ";Jet OLEDB: Engine Type=5")
            'cat = Nothing
        End If



        myconnection = New OleDbConnection(OLEDBPROVIDER & DatabasePath)
        myconnection.Open()
        Dim ouvertureMs As Long = chrono.ElapsedMilliseconds

        tablename = "MinuterieActive"

        ' La table est cherchee avec la connexion DEJA ouverte (avant : TableExists en ouvrait une seconde, lente sur un PC lent).
        Dim tableTrouvee As Boolean
        Try
            Dim restrictionsTable(3) As String
            restrictionsTable(2) = tablename
            tableTrouvee = myconnection.GetSchema("Tables", restrictionsTable).Rows.Count > 0
        Catch ex As Exception
            tableTrouvee = TableExists(DatabasePath, tablename)
        End Try
        DiagLog("Minuterie : base verifiee en " & chrono.ElapsedMilliseconds & " ms (ouverture " & ouvertureMs & " ms)")

        If tableTrouvee = False Then

            sql = "CREATE TABLE [" & tablename & "] ([Actif] BIT NULL, [Jour] NVARCHAR (20) NULL, [Heure] NVARCHAR (5) NULL, [Type] NVARCHAR (50) NULL, [Action] NVARCHAR (255) NULL, [Play] NVARCHAR (50) NULL, [Durée] NVARCHAR (8) NULL, [Volume] NVARCHAR (3) NULL, [tri] NVARCHAR (10) NULL, [Argument] NVARCHAR (255) NULL);"

            mycommand = New OleDb.OleDbCommand(sql, myconnection)
            mycommand.ExecuteNonQuery()
        End If

        myconnection.Close()

    End Sub


    Sub ChargeDataMinuterieActive()

        DiagLog("Minuterie : chargement, base " & DatabasePath & " (depuis les reglages : " & MinuterieFromSettings & ")")
        BaseMinuterie.PreparerLaBase(DatabasePath)          ' colonne Argument en texte long (voir BaseMinuterie)
        DiagLog("Minuterie : base preparee")
        Form4.DataGridView1.Rows.Clear()
        Form4.DataGridView2.Rows.Clear()
        DiagLog("Minuterie : tableaux vides")

        If MinuterieFromSettings = True Then


            For j = 0 To My.Settings.MinuterieActif.Count - 1

                _Actif = My.Settings.MinuterieActif(j)
                _Jour = My.Settings.MinuterieJour(j)
                _Heure = My.Settings.MinuterieHeure(j)
                _Type = TypeSansVlcExterne(My.Settings.MinuterieType(j))
                _Action = My.Settings.MinuterieAction(j)
                _Play = My.Settings.MinuteriePlay(j)
                _Durée = My.Settings.MinuterieDurée(j)
                _Volume = My.Settings.MinuterieVolume(j)
                _Tri = My.Settings.MinuterieTri(j)
                _Argument = My.Settings.MinuterieArgument(j)

                Form4.DataGridView1.Rows.Add({TexteEnBooleen(_Actif), _Jour, _Heure, _Type, _Action, _Play, _Durée, _Volume, _Tri, _Argument})
                ' Comme au chargement depuis la base : liste des chaines dans Action des lignes TV (et des actions Deezer).
                ' Sans cela la petite fleche n'apparaissait qu'apres avoir re-choisi TV (15/10/2026).
                ListeDesActionsDuType(Form4.DataGridView1.Rows.Count - 1, CStr(_Type), CStr(_Action))

            Next



            MinuterieFromSettings = False
            Exit Sub

        End If


        CreateSqlDatabaseIFnoExist()

        'DatabaseNetPath = AppPath & "\data\slide.accdb"
        Dim mycommand As OleDbCommand
        Dim myconnection As OleDbConnection
        Dim Sql As String
        Dim table As New DataTable

        ' con.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" & Environment.CurrentDirectory & "\\" & "databasedic.accdb"
        myconnection = New OleDbConnection(OLEDBPROVIDER & DatabasePath)
        DiagLog("Minuterie : ouverture de la base")
        myconnection.Open()
        DiagLog("Minuterie : base ouverte, lecture des taches")

        tablename = "MinuterieActive"

        Sql = "Select * from MinuterieActive"
        mycommand = New OleDb.OleDbCommand(Sql, myconnection)
        mycommand.ExecuteNonQuery()

        reader1 = mycommand.ExecuteReader()
        Dim ChargeMinuerie As Minuterie
        LigneMinuterie.Clear()
        VersionDeLaMinuterie += 1                   ' (une passe de la minuterie en cours le voit : voir Module1.Minuterie)

        Dim i As Boolean

        While reader1.Read()
            i = reader1("Actif")

            If i = True Then
                _Type = "" : _Type = TypeSansVlcExterne(reader1("Type"))
                _Action = "" : _Action = reader1("Action")
                _Action2 = Right(_Action, 5)

                ' Les taches Deezer ont leur propre type depuis le 08/10 : celles d'avant (rangees
                ' dans Divers, Volume ou Music Source) y sont mises ici ; la base suit au prochain
                ' OK des reglages.
                If UCase(TypeDeezerPour(Convert.ToString(_Type), Convert.ToString(_Action))) = "DEEZER" Then
                    _Action = ActionDeezerPour(Convert.ToString(_Action))
                    _Type = "Deezer"
                End If

                If _Type = "Ecran Noir" Then
                    Dim dtNoir As Date
                    Dim dtNoirNew As Date
                    dtNoir = Mid(_Action, 10)

                    If dtNoir < Now Then
                        wd = Weekday(dtNoir)
                        For n = 0 To 6
                            If Weekday(Now().AddDays(n)) = wd Then
                                dtNoirNew = Now().AddDays(n)
                                'dif = DateDiff(DateInterval.Day, dtNoir, dtNoirNew)
                                Exit For
                            End If
                        Next
                        _Action = Left(_Action, 10) & Format(dtNoirNew, "f")
                        _Action = Left(_Action, Len(_Action) - 5)
                        _Action = _Action & _Action2


                    End If
                End If

                _ArgumentModif = Modif_Old_Argument_Format(reader1("Argument"))

                Form4.DataGridView1.Rows.Add({reader1("Actif"), reader1("Jour"), reader1("Heure"), _Type, _Action, reader1("Play"), reader1("Durée"), reader1("Volume"), reader1("tri"), _ArgumentModif})
                Titre = Arg("Titre", _ArgumentModif)
                Form4.DataGridView1.Rows(Form4.DataGridView1.Rows.Count - 1).Cells("Play").ToolTipText = Titre
                NomURL = Arg("Url", _ArgumentModif)
                Form4.DataGridView1.Rows(Form4.DataGridView1.Rows.Count - 1).Cells("Action").ToolTipText = NomURL
                Form4.DataGridView1.Rows(Form4.DataGridView1.Rows.Count - 1).Cells(0).ToolTipText = _ArgumentModif
                ' Ligne TV : liste des chaines directement dans Action, pour changer de chaine
                ' sans re-choisir le Type (qui effacait Play et les reglages du PIP).
                ListeDesActionsDuType(Form4.DataGridView1.Rows.Count - 1, CStr(_Type), CStr(_Action))
                ChargeMinuerie.Actif = reader1("Actif")
                ChargeMinuerie.Jour = reader1("Jour")
                ChargeMinuerie.Heure = reader1("Heure")
                ChargeMinuerie.Type = _Type
                ChargeMinuerie.Action = _Action : ChargeMinuerie.Action = ChargeMinuerie.Action.Replace("''", "'")
                ChargeMinuerie.Play = reader1("Play")
                ChargeMinuerie.Durée = reader1("Durée")
                ChargeMinuerie.Volume = reader1("Volume")
                ChargeMinuerie.Tri = reader1("tri")
                ChargeMinuerie.Argument = _ArgumentModif
                LigneMinuterie.Add(ChargeMinuerie)

            Else

                _ArgumentModif = Modif_Old_Argument_Format(reader1("Argument"))
                ' (Taches Deezer d'avant le 08/10 : rangees sous le type "Deezer", comme les actives.)
                Dim typeInactif As Object = TypeSansVlcExterne(reader1("Type")), actionInactive As Object = reader1("Action")
                If UCase(TypeDeezerPour(Convert.ToString(typeInactif), Convert.ToString(actionInactive))) = "DEEZER" Then
                    actionInactive = ActionDeezerPour(Convert.ToString(actionInactive))
                    typeInactif = "Deezer"
                End If
                Form4.DataGridView2.Rows.Add({reader1("Actif"), reader1("Jour"), reader1("Heure"), typeInactif, actionInactive, reader1("Play"), reader1("Durée"), reader1("Volume"), reader1("tri"), _ArgumentModif})
            End If

        End While




        myconnection.Close()
        DiagLog("Minuterie : " & Form4.DataGridView1.RowCount & " tache(s) active(s), " & Form4.DataGridView2.RowCount & " inactive(s) chargee(s)")

        If Form4.DataGridView1.RowCount > 0 Then
            Form4.DataGridView1.CurrentCell = Form4.DataGridView1.Rows(Form4.DataGridView1.RowCount - 1).Cells("Action")
            Form4.DataGridView1.FirstDisplayedScrollingRowIndex = 0
        End If
        'Form4.Show()


    End Sub
    Function Modif_Old_Argument_Format(_ArgumentModif)



        If _ArgumentModif <> "" And InStr(_ArgumentModif, "!/") = 0 Then
            p1 = InStr(UCase(_ArgumentModif), "/URL:")
            If p1 > 0 Then
                Partie1 = Left(_ArgumentModif, p1 + 4)
                Partie1 = Replace(Partie1, "/", "!/")
                Partie2 = Mid(_ArgumentModif, p1 + 5)
                Partie3 = ""
                p2 = InStr(UCase(Partie2), "/PIP:")
                If p2 > 0 Then
                    Partie_inter = Partie2
                    Partie2 = Left(Partie2, p2 - 1)
                    Partie3 = Mid(Partie_inter, p2)
                    Partie3 = Replace(Partie3, "/", "!/")
                End If

                _ArgumentModif = Partie1 & Partie2 & Partie3
            Else
                _ArgumentModif = Replace(_ArgumentModif, "/", "!/")
            End If

        End If

        Modif_Old_Argument_Format = _ArgumentModif
    End Function

    Sub UpdateData()

        'DatabaseNetPath = AppPath & "\data\slide.accdb"
        Dim mycommand As OleDbCommand
        Dim myconnection As OleDbConnection
        Dim Sql As String
        Dim cat As New Catalog()
        Dim table As New DataTable

        Try


            myconnection = New OleDbConnection(OLEDBPROVIDER & DatabasePath)
            myconnection.Open()

            BaseMinuterie.PreparerLaBase(DatabasePath)
            tablename = "MinuterieActive"
            Sql = "delete from MinuterieActive"
            mycommand = New OleDb.OleDbCommand(Sql, myconnection)
            mycommand.ExecuteNonQuery()
            Dim ChargeMinuerie As Minuterie
            LigneMinuterie.Clear()
            VersionDeLaMinuterie += 1                    ' (une passe de la minuterie en cours le voit : voir Module1.Minuterie)

            My.Settings.MinuterieActif = New Specialized.StringCollection
            My.Settings.MinuterieJour = New Specialized.StringCollection
            My.Settings.MinuterieHeure = New Specialized.StringCollection
            My.Settings.MinuterieType = New Specialized.StringCollection
            My.Settings.MinuterieAction = New Specialized.StringCollection
            My.Settings.MinuteriePlay = New Specialized.StringCollection
            My.Settings.MinuterieDurée = New Specialized.StringCollection
            My.Settings.MinuterieVolume = New Specialized.StringCollection
            My.Settings.MinuterieTri = New Specialized.StringCollection
            My.Settings.MinuterieArgument = New Specialized.StringCollection

            Try

                For Each row As DataGridViewRow In Form4.DataGridView1.Rows

                    ' Toujours "True"/"False" (texte SQL et reglages), meme si une valeur texte "Vrai"/"Faux" collee s'est glissee dans la case.
                    _Actif = If(TexteEnBooleen(row.Cells("Actif").Value), "True", "False")
                    _Jour = row.Cells("Jour").FormattedValue
                    _Heure = Left(row.Cells("Heure").FormattedValue, 5)

                    _Type = row.Cells("Type").FormattedValue
                    _Action = row.Cells("Action").FormattedValue : _Action = _Action.replace("'", "''")
                    _Play = row.Cells("Play").FormattedValue
                    _Durée = row.Cells("Durée").FormattedValue
                    ' Volume seulement s'il sert a quelque chose pour cette tache (20/10/2026).
                    If Not row.IsNewRow AndAlso Not VolumeUtilePour(CStr(_Type), CStr(row.Cells("Action").FormattedValue)) Then row.Cells("Volume").Value = ""
                    _Volume = row.Cells("Volume").FormattedValue
                    _Tri = row.Cells("tri").FormattedValue
                    If _Jour = "BOOT" Then
                        _Heure = "00:00"
                        _Tri = "000000"
                    End If

                    _Argument = row.Cells("Argument").FormattedValue
                    Dim argumentBrut As String = Convert.ToString(_Argument)          ' (sans doublement des apostrophes : il passe en parametre)
                    _Argument = _Argument.replace("'", "''")

                    Sql = "Insert into MinuterieActive values (" & _Actif & ",'" & _Jour & "','" & _Heure & "','" & _Type & "','" & _Action & "','" & _Play & "','" & _Durée & "','" & _Volume & "','" & _Tri & "',?);"
                    mycommand = New OleDb.OleDbCommand(Sql, myconnection)
                    mycommand.Parameters.AddWithValue("argument", argumentBrut)
                    mycommand.ExecuteNonQuery()

                    ChargeMinuerie.Actif = _Actif
                    ChargeMinuerie.Jour = _Jour
                    ChargeMinuerie.Heure = _Heure
                    ChargeMinuerie.Type = _Type
                    ChargeMinuerie.Action = _Action : ChargeMinuerie.Action = ChargeMinuerie.Action.Replace("''", "'")
                    ChargeMinuerie.Play = _Play
                    ChargeMinuerie.Durée = _Durée
                    ChargeMinuerie.Volume = _Volume
                    ChargeMinuerie.Tri = _Tri
                    ChargeMinuerie.Argument = _Argument
                    LigneMinuterie.Add(ChargeMinuerie)

                    ''aussi dans settings
                    My.Settings.MinuterieActif.Add(_Actif)
                    My.Settings.MinuterieJour.Add(_Jour)
                    My.Settings.MinuterieHeure.Add(_Heure)
                    My.Settings.MinuterieType.Add(_Type)
                    My.Settings.MinuterieAction.Add(_Action)
                    My.Settings.MinuteriePlay.Add(_Play)
                    My.Settings.MinuterieDurée.Add(_Durée)
                    My.Settings.MinuterieVolume.Add(_Volume)
                    My.Settings.MinuterieTri.Add(_Tri)
                    My.Settings.MinuterieArgument.Add(_Argument)

                Next

                For Each row As DataGridViewRow In Form4.DataGridView2.Rows


                    _Actif = row.Cells("Inactif").FormattedValue.ToString
                    _Jour = row.Cells("Jour2").FormattedValue
                    _Heure = row.Cells("Heure2").FormattedValue
                    _Type = row.Cells("Type2").FormattedValue
                    _Action = row.Cells("Action2").FormattedValue : _Action = _Action.replace("'", "''")
                    _Play = row.Cells("Play2").FormattedValue
                    _Durée = row.Cells("Durée2").FormattedValue
                    _Volume = row.Cells("Volume2").FormattedValue
                    _Tri = row.Cells("tri2").FormattedValue
                    _Argument = row.Cells("Argument2").FormattedValue

                    Sql = "Insert into MinuterieActive values (" & _Actif & ",'" & _Jour & "','" & _Heure & "','" & _Type & "','" & _Action & "','" & _Play & "','" & _Durée & "','" & _Volume & "','" & _Tri & "',?);"
                    mycommand = New OleDb.OleDbCommand(Sql, myconnection)
                    mycommand.Parameters.AddWithValue("argument", Convert.ToString(_Argument))
                    mycommand.ExecuteNonQuery()

                Next

            Catch ex As Exception
                MsgBox(ex.Message)
            End Try

            myconnection.Close()
            My.Settings.Save()

            ' La minuterie vient d'etre modifiee : un PIP dont la tache n'y est plus est arrete.
            ArreterLesPipSansTache()

        Catch ex As Exception

        End Try
    End Sub

    Sub LoadMinuterieFromSettings()
        MinuterieFromSettings = True
        ChargeDataMinuterieActive()



    End Sub

    Sub HeureDeShabbat(Ville)

        Shabbat.Debut = ""
        Shabbat.Fin = ""
        Shabbat.PreDebut = ""

        Dim dt1 As Date
        Dim dt2 As Date

        trouve = 0
        For u = 1 To 50
            If City(u, 0) = Ville Then trouve = u : Exit For
        Next
        If trouve = 0 Then Exit Sub

        ' Deja calculee aujourd'hui pour cette ville : reprise telle quelle (voir HeureDeShabbatGardee).
        If HeureDeShabbatGardee(CStr(Ville), False) Then Exit Sub
        ajoute = 0
        If trouve = 4 Then ajoute = 60

        Dim locationName As String = City(trouve, 0)
        Dim latitude As Double = City(trouve, 1)
        Dim longitude As Double = City(trouve, 2)
        Dim elevation As Double = 0 'optional elevation
        Dim timeZone As ITimeZone = New OlsonTimeZone(City(trouve, 3))
        Dim location As New GeoLocation(locationName, latitude, longitude, elevation, timeZone)
        Dim zc As New ComplexZmanimCalendar(location)

        'Dim zc As New ComplexZmanimCalendar(location)
        'optionally set it to a specific date with a year, month and day
        'Dim zc As New ComplexZmanimCalendar(New DateTime(2016, 7, 8), location)
        'If IsDate(zc.GetCandleLighting.ToString) = True And IsDate(zc.GetTzais.ToString) = True Then
        ' Shabbat.Debut = Format(zc.GetCandleLighting, "HH:mm")
        ' dt = zc.GetTzais
        ' Shabbat.Fin = Format(dt.AddMinutes(1), "HH:mm")
        ' End If




        Dim ProchaiSamedi = GetNextDate(DayOfWeek.Saturday)
        Dim ProchainVendredi = ProchaiSamedi.AddDays(-1)

        Dim zc1 As New ComplexZmanimCalendar(ProchainVendredi, location)

        Dim zc2 As New ComplexZmanimCalendar(ProchaiSamedi, location)

        If IsDate(zc1.GetCandleLighting.ToString) = True And IsDate(zc2.GetTzais.ToString) = True Then
            dt1 = zc1.GetCandleLighting
            Shabbat.Debut = Format(dt1.AddMinutes(1 + ajoute), "HH:mm")
            Shabbat.PreDebut = Format(dt1.AddMinutes(-3 + ajoute), "HH:mm")

            dt2 = zc2.GetTzais
            Shabbat.Fin = Format(dt2.AddMinutes(1 + ajoute), "HH:mm")

        End If
        HeureDeShabbatGardee(CStr(Ville), True)
    End Sub

    ''' <summary>
    ''' Les heures de Shabbat calculees aujourd'hui pour une ville, gardees dans un petit fichier du
    ''' disque local (%LocalAppData%\Slide\HeureDeShabbat.txt). Elles ne dependent que de la ville et
    ''' du jour : le premier lancement de la journee les calcule, les suivants les relisent.
    ''' Pourquoi (13/10/2026) : le premier calcul charge la bibliotheque Zmanim et sa base des fuseaux
    ''' horaires - 0,15 s mesure hors de Slide, davantage depuis le lecteur reseau - a chaque lancement.
    ''' <paramref name="enregistrer"/> = Vrai : garde les heures qui viennent d'etre calculees ;
    ''' Faux : les relit, et rend Vrai si elles etaient gardees pour cette ville et ce jour.
    ''' </summary>
    Function HeureDeShabbatGardee(ville As String, enregistrer As Boolean) As Boolean
        Try
            Dim dossier As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Slide")
            Dim fichier As String = IO.Path.Combine(dossier, "HeureDeShabbat.txt")
            Dim cle As String = "v1|" & ville & "|" & Date.Today.ToString("yyyy-MM-dd")
            If enregistrer Then
                If Convert.ToString(Shabbat.Debut) = "" OrElse Convert.ToString(Shabbat.Fin) = "" Then Return False
                IO.Directory.CreateDirectory(dossier)
                IO.File.WriteAllText(fichier, cle & "|" & Convert.ToString(Shabbat.Debut) & "|" & Convert.ToString(Shabbat.Fin) & "|" & Convert.ToString(Shabbat.PreDebut))
                Return True
            End If
            If Not IO.File.Exists(fichier) Then Return False
            Dim texte As String = IO.File.ReadAllText(fichier).Trim()
            If Not texte.StartsWith(cle & "|", StringComparison.Ordinal) Then Return False
            Dim heures As String() = texte.Substring(cle.Length + 1).Split("|"c)
            If heures.Length <> 3 Then Return False
            For Each h As String In heures
                If Not System.Text.RegularExpressions.Regex.IsMatch(h, "^\d\d:\d\d$") Then Return False
            Next
            Shabbat.Debut = heures(0)
            Shabbat.Fin = heures(1)
            Shabbat.PreDebut = heures(2)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function


    Function GetNextDate(day As DayOfWeek) As DateTime
        Dim now As DateTime = DateTime.Today
        now = now.AddDays(-1)

        Dim today As Integer = CInt(now.DayOfWeek)
        Dim find As Integer = CInt(day)

        Dim delta As Integer = find - today
        If delta > 0 Then
            Return now.AddDays(delta)
        Else
            Return now.AddDays(7 - delta)
        End If
    End Function

    ''' <summary>Une video au hasard du dossier <paramref name="root"/>, differente de la derniere
    ''' tiree pour ce type (0 = Chabbat Chalom, 1 = Chavoua Tov). "" si le dossier est vide.
    ''' L'ancien tirage (Generator.Next(0, t - 1)) ne pouvait JAMAIS tomber sur le dernier fichier
    ''' du dossier (borne haute exclue) et pouvait redonner la meme video deux fois de suite.</summary>
    Function VideoAuHasard(root As String, x As Integer) As String
        Static derniere As String() = {"", ""}

        Dim fichiers As String() = Directory.GetFiles(root)
        If fichiers.Length = 0 Then
            DiagLog("AnnonceShabbat : aucun fichier dans " & root)
            Return ""
        End If

        Dim indice As Integer = If(x = 0, 0, 1)
        Dim choix As String = fichiers(Generator.Next(0, fichiers.Length))
        ' Avec au moins deux fichiers, on ne rejoue pas celui de la fois precedente.
        If fichiers.Length > 1 Then
            Dim essais As Integer = 0
            Do While choix = derniere(indice) AndAlso essais < 20
                choix = fichiers(Generator.Next(0, fichiers.Length))
                essais += 1
            Loop
        End If
        derniere(indice) = choix

        DiagLog("AnnonceShabbat : " & IO.Path.GetFileName(choix) & " (sur " & fichiers.Length & " fichiers de " & root & ")")
        Return choix
    End Function

    ''' <summary>Simulation d'annonce : data\TestAnnonce.txt contient "HH:MM;x" (x = 0 entree, 1 sortie).
    ''' A cette minute, l'annonce est jouee une fois et le fichier est efface.</summary>
    Function TestAnnonceAMinute(cherche As String, ByRef x As Integer) As Boolean
        Try
            Dim f As String = AppPath & "\data\TestAnnonce.txt"
            If Not IO.File.Exists(f) Then Return False
            Dim parties As String() = IO.File.ReadAllText(f).Trim().Split(";"c)
            If parties.Length < 2 OrElse cherche.Length < 4 Then Return False
            If parties.Length >= 3 AndAlso parties(2).Trim().ToLower() = "video" Then Return False   ' voir TestAnnoncePendantVideo
            If Right(cherche, 4) <> parties(0).Replace(":", "").Trim() Then Return False
            x = CInt(Val(parties(1)))
            IO.File.Delete(f)
            DiagLog("AnnonceShabbat : SIMULATION a " & parties(0) & " (" & If(x = 0, "entree", "sortie") & ")")
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Simulation pendant une video du diaporama : "HH:MM;x;video". A partir de HH:MM (et
    ''' pendant 30 min), l'annonce part des qu'une video du diaporama joue. Une seule fois.</summary>
    Function TestAnnoncePendantVideo(ByRef x As Integer) As Boolean
        Try
            Dim f As String = AppPath & "\data\TestAnnonce.txt"
            If Not IO.File.Exists(f) Then Return False
            Dim parties As String() = IO.File.ReadAllText(f).Trim().Split(";"c)
            If parties.Length < 3 OrElse parties(2).Trim().ToLower() <> "video" Then Return False
            Dim debut As Date
            If Not Date.TryParse(Today.ToString("yyyy-MM-dd") & " " & parties(0).Trim(), debut) Then Return False
            If Now < debut OrElse Now > debut.AddMinutes(30) Then Return False
            If Not (Diffusion.Mode = "VIDEO" AndAlso Form3.VlcControl1.IsPlaying AndAlso Form3.VlcControl1.Visible) Then Return False
            x = CInt(Val(parties(1)))
            IO.File.Delete(f)
            DiagLog("AnnonceShabbat : SIMULATION pendant une video (" & If(x = 0, "entree", "sortie") & ")")
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Trace : ou est le lecteur de l'annonce, qui est devant lui, et capture de l'ecran
    ''' 5 s plus tard (data\AnnonceEcran.png) - "il n'y avait rien a l'ecran".</summary>
    Sub DiagAnnonce(lecteur As Vlc.DotNet.Forms.VlcControl)
        Try
            Dim devant As New List(Of String)
            For Each c As Control In Form3.Controls
                If c Is lecteur Then Exit For
                If c.Visible AndAlso c.Bounds.IntersectsWith(lecteur.Bounds) Then devant.Add(c.Name & " " & c.Bounds.ToString())
            Next
            DiagLog("AnnonceShabbat : lecteur " & lecteur.Name & " visible=" & lecteur.Visible & " " & lecteur.Bounds.ToString() &
                    " parent=" & If(lecteur.Parent Is Nothing, "aucun", lecteur.Parent.Name) & " | Form3 visible=" & Form3.Visible &
                    " etat=" & Form3.WindowState.ToString() & " | devant : " & If(devant.Count = 0, "rien", String.Join(", ", devant)))
            Dim capture As New System.Windows.Forms.Timer With {.Interval = 5000}
            AddHandler capture.Tick, Sub()
                                         capture.Stop() : capture.Dispose()
                                         Try
                                             Dim zone As Rectangle = Form3.RectangleToScreen(Form3.ClientRectangle)
                                             Using bmp As New Bitmap(zone.Width, zone.Height)
                                                 Using g As Graphics = Graphics.FromImage(bmp)
                                                     g.CopyFromScreen(zone.Location, Point.Empty, zone.Size)
                                                 End Using
                                                 bmp.Save(AppPath & "\data\AnnonceEcran.png", Imaging.ImageFormat.Png)
                                             End Using
                                             DiagLog("AnnonceShabbat : ecran capture (data\AnnonceEcran.png), lecteur joue=" & lecteur.IsPlaying & " temps=" & lecteur.Time)
                                         Catch ex As Exception
                                             DiagLog("AnnonceShabbat : capture impossible (" & ex.Message & ")")
                                         End Try
                                     End Sub
            capture.Start()
        Catch ex As Exception
            DiagLog("AnnonceShabbat : trace impossible (" & ex.Message & ")")
        End Try
    End Sub

    Sub AnnonceShabbat(x)

        'x=0 shabbat chalom
        'x=1 chavouaTov

        FlagExit = False


        'If AfficheHeureDeShabbat = False Then Exit Sub

        If Diffusion.Mode = "FILM-VLC-EXT" Then
            AppActivate("vlc")
            My.Computer.Keyboard.SendKeys(" ")
            WinActivate("slide")
        Else
            Form3.VlcControl2.Pause()
            ClosePlayer()
        End If


        If FullScreen = True And Form3.ContextMenuStrip1.Visible = False And Form2.Visible = False And Form4.Visible = False And Form5.Visible = False Then CacherSouris()

        Shabbat.counter1 = Shabbat.ToutLesXminutes - 0.5

        'Dim root As String = AppPath & "\chavouatov\"

        If x = 0 Then
            root = AppPath & "\data\ShabbatChalom\"
        ElseIf x = 2 Then
            root = AppPath & "\data\HagSameah\"                ' entree d'une fete juive (Yom Tov)
        Else
            root = AppPath & "\data\chavouatov\"               ' sortie du Chabbat (1) ou d'une fete (3)
        End If
        If x = 2 AndAlso (Not IO.Directory.Exists(root) OrElse IO.Directory.GetFiles(root).Length = 0) Then root = AppPath & "\data\ShabbatChalom\"



        NomDuFichier = VideoAuHasard(root, CInt(x))
        If NomDuFichier = "" Then Exit Sub

        Nom = NomDuFichier

        Dim vPlayerSlideVideo As Vlc.DotNet.Forms.VlcControl
        vPlayerSlideVideo = Form3.VlcControl1
        ' Pendant l'annonce, le diaporama ne doit pas regler ce lecteur : en mode VIDEO, le lissage
        ' du son remettait le volume de la video du diaporama (bas) sur l'annonce.
        Dim modeAvantAnnonce As String = Diffusion.Mode
        If modeAvantAnnonce = "VIDEO" OrElse modeAvantAnnonce = "PHOTO" Then Diffusion.Mode = "ANNONCE"
        Dim chronoAnnonce As Stopwatch = Stopwatch.StartNew()

        ext = "file:///" : If Left(UCase(Nom), 4) = "HTTP" Then ext = ""
        PreparerLeVolumeAvantChangement(vPlayerSlideVideo, 75)
        vPlayerSlideVideo.SetMedia(ext & Nom)
        CouperLeSonAvantDemarrage(vPlayerSlideVideo)
        vPlayerSlideVideo.Play()
        Application.DoEvents()

        ''''''''''''
        oldFlagVolumeOFF = 0
        If VolumeOFF = True Then
            VolumeOFF = False
            SetMasterVol(VolumeGeneral)
            oldFlagVolumeOFF = 1
        End If


        '''''''''''


        SetVolume(75, vPlayerSlideVideo)
        MasqueLegende()


        Try
            i = 0
            'attend le debut de lecture
            Do Until vPlayerSlideVideo.IsPlaying = True
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > 60 Then Exit Do : SaveLogError(Nom, "Erreur lecture impossible") : GoTo fin    ' echec lecture apres 2 minutes sortir
            Loop
            DiagLog("AnnonceShabbat : lecture " & If(vPlayerSlideVideo.IsPlaying, "demarree", "NON demarree") & " apres " & chronoAnnonce.ElapsedMilliseconds & " ms (mode " & modeAvantAnnonce & ", VolumeOFF=" & oldFlagVolumeOFF & ")")

            i = 0

            vPlayerSlideVideo.Top = 0
            vPlayerSlideVideo.Left = 0
            vPlayerSlideVideo.Height = Form3.ClientSize.Height
            vPlayerSlideVideo.Width = Form3.ClientSize.Width

            Form3.PictureBox1.Visible = False
            vPlayerSlideVideo.Visible = True
            vPlayerSlideVideo.BringToFront()
            DiagAnnonce(vPlayerSlideVideo)

            'attend la fin de lecture
            dps = 0
            Do Until vPlayerSlideVideo.IsPlaying = False
                Application.DoEvents()
                wait(1)
                i = i + 1 : If i > 59 Then Exit Do
                If FlagExit = True Then Exit Do
            Loop
            vPlayerSlideVideo.Visible = False
            DiagLog("AnnonceShabbat : fin apres " & chronoAnnonce.ElapsedMilliseconds \ 1000 & " s" & If(FlagExit, " (interrompue)", ""))

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

fin:
        If Diffusion.Mode = "ANNONCE" Then Diffusion.Mode = modeAvantAnnonce
        If oldFlagVolumeOFF = 1 Then
            zero = 0
            SetMasterVol(zero)
            VolumeOFF = True
        End If



        retablieTousLesVolumes()
        'MsgBox(Diffusion.Mode)
        If Diffusion.Mode = "PHOTO" Then
            Form3.PictureBox1.Visible = True
        End If

        If Diffusion.Mode = "VIDEO" Then

            Form3.VlcControl1.Visible = True

        End If

        If Diffusion.Mode = "FILM-VLC-INT" Then
            SetVolume(Diffusion.Vol, Form3.VlcControl2)
            Form3.VlcControl2.Visible = True
            Form3.VlcControl2.Play()
        End If

        If Diffusion.Mode = "IPTV_VLC_INT" Then
            SetVolume(Diffusion.Vol, Form3.VlcControl2)
            Form3.VlcControl2.Visible = True
            Form3.VlcControl2.Play()
        End If

        If Diffusion.Mode = "FILM-VLC-EXT" Then
            SetVolume(0, Nothing)
            SetVol(Diffusion.Vol)
            WinActivate("vlc")
            My.Computer.Keyboard.SendKeys(" ")

        End If
        ''''''''''''''''
        If Left(Diffusion.Mode, 2) = "TV" Then

            'Form3.VlcControl2.Play()
            'SetVolume(Diffusion.Vol, Form3.VlcControl2)
            PlayTV_VLC(Diffusion.Chaine, Diffusion.Vol, Diffusion.Arg)

        End If


        If Diffusion.Mode = "SAT-VLC_INT" Then
            PlaySAT_VLC(Diffusion.Chaine, Diffusion.Vol, Diffusion.Arg)
        End If

        ' Ecran noir avec l'aquarium en cours (22/10/2026) : l'annonce a joue sur le meme lecteur et l'a remplace ; l'aquarium repart.
        ReprendreLAquarium()

    End Sub

    ''' <summary>La liste des radios DE CE POSTE : data\settings\radio.txt (20/10/2026). Propre a chaque PC : une mise a jour de Slide ne la touche
    ''' pas (ce que l'on y ajoute, supprime ou classe est garde). Au premier acces, elle est COPIEE depuis data\radio.txt, la liste fournie avec le
    ''' programme, qui reste la liste de depart des nouveaux PC.</summary>
    Function FichierDesRadios() As String
        Dim ici As String = Path.Combine(DossierReglages, "radio.txt")
        Try
            If Not File.Exists(ici) Then
                Dim modele As String = AppPath & "\Data\radio.txt"
                If File.Exists(modele) Then
                    Directory.CreateDirectory(DossierReglages)
                    File.Copy(modele, ici, False)
                    DiagLog("Radios : liste de depart copiee dans " & ici)
                End If
            End If
        Catch ex As Exception
            DiagLog("Radios : liste de depart non copiee (" & ex.Message & ")")
        End Try
        Return ici
    End Function

    Sub ChargeRadioList()
        Dim Nom As String = FichierDesRadios()
        Form4.ComboBox2.Items.Clear()
        ' Fichier absent (supprime, ou enregistrement interrompu avant l'ecriture sure) : la version precedente est reprise si elle existe.
        If Not File.Exists(Nom) Then
            Try
                Dim precedente As String = Path.Combine(DossierReglages, "radio.old_precedente.txt")
                If File.Exists(precedente) Then
                    File.Copy(precedente, Nom, False)
                    DiagLog("Radios : radio.txt absent, version precedente reprise")
                End If
            Catch ex As Exception
            End Try
            If Not File.Exists(Nom) Then Exit Sub
        End If
        ' Le fichier est FERME apres la lecture (Using) : avant, le lecteur restait ouvert jusqu'au ramasse-miettes, et ecrire la liste juste
        ' apres (ajout, deplacement, suppression) echouait : "le fichier est utilise par un autre processus" (20/10/2026).
        Using reader1 As New StreamReader(Nom)
            Dim line As String = Nothing
            i = 0
            While (reader1.Peek() <> -1)
                line = reader1.ReadLine()
                Radio(i, 0) = Trim(Mid(line, 1, 49))
                Radio(i, 1) = Trim(Mid(line, 50))
                Form4.ComboBox2.Items.Add(Radio(i, 0))
                'Form4.TextBox13.Text = radio(i, 1)
                i = i + 1
                If i > 499 Then Exit While
            End While
        End Using

    End Sub

    Sub SaveRadio()
        Dim Nom As String = FichierDesRadios()


        ' Ecriture SURE (20/10/2026) : la liste est ecrite dans un fichier provisoire, puis celui-ci REMPLACE radio.txt d'un coup ; la version
        ' precedente est gardee dans radio.old_precedente.txt (le motif ".old_" n'est jamais publie). Un plantage ou une coupure pendant
        ' l'enregistrement ne peut donc plus laisser une liste vide ou tronquee.
        Dim provisoire As String = Nom & ".tmp"
        Using writer As New StreamWriter(provisoire, False)
            For i = 0 To Form4.ComboBox2.Items.Count - 1
                txt = Form4.ComboBox2.Items(i)
                If Mid(txt, 1, 12) <> "# DELETED # " Then
                    ' (nom sur 49 colonnes au moins : un nom plus long ne fait plus echouer l'enregistrement)
                    writer.WriteLine(CStr(Radio(i, 0)).PadRight(49) & Radio(i, 1))
                End If
            Next
        End Using
        Try
            If File.Exists(Nom) Then File.Copy(Nom, Path.Combine(DossierReglages, "radio.old_precedente.txt"), True)
        Catch ex As Exception
            DiagLog("Radios : copie de la liste precedente impossible (" & ex.Message & ")")
        End Try
        File.Move(provisoire, Nom, True)
        Array.Clear(Radio, 0, Radio.Length)

        ChargeRadioList()
    End Sub


    ''' <summary>Les reglages d'une tache SANS son titre (le reste est garde : PIP, boucle, langue...).
    ''' Le titre appartient a UN film : des reglages repris d'une autre tache ne doivent pas l'emporter
    ''' (09/10/2026, "dans preview, changer titre, c'est rempli avec un titre faux").</summary>
    Function ReglagesSansTitre(reglages As Object) As String
        Return System.Text.RegularExpressions.Regex.Replace(Convert.ToString(reglages), "!/Titre:.*?(?=!/|$)", "!/Titre:",
                                                            System.Text.RegularExpressions.RegexOptions.IgnoreCase)
    End Function

    ''' <summary>Vrai si <paramref name="titre"/>, enregistre dans la tache de la ligne
    ''' <paramref name="ligne"/>, est en realite le nom (sans dossier ni extension) du fichier d'un
    ''' AUTRE film de la minuterie : c'est un titre recopie par erreur avec les reglages de ce film
    ''' (taches enregistrees avant la correction de VolumeEtArgumentAdapte).</summary>
    Function TitreDUnAutreFilm(grille As DataGridView, ligne As Integer, titre As String) As Boolean
        Try
            Dim t As String = If(titre, "").Trim()
            If t = "" OrElse grille Is Nothing OrElse ligne < 0 OrElse ligne >= grille.Rows.Count Then Return False
            Dim fichier As String = Convert.ToString(grille.Rows(ligne).Cells("Action").Value).Trim()
            If String.Equals(IO.Path.GetFileNameWithoutExtension(fichier), t, StringComparison.OrdinalIgnoreCase) Then Return False
            For i As Integer = 0 To grille.Rows.Count - 1
                If i = ligne Then Continue For
                Dim autre As String = Convert.ToString(grille.Rows(i).Cells("Action").Value).Trim()
                If autre = "" OrElse Not autre.Contains("\"c) OrElse String.Equals(autre, fichier, StringComparison.OrdinalIgnoreCase) Then Continue For
                If String.Equals(IO.Path.GetFileNameWithoutExtension(autre), t, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
        Catch ex As Exception
        End Try
        Return False
    End Function

    Function Arg(x, a)
        Argument = a + "!/"
        Result = ""
        LNG = Len(x) + 2
        k1 = InStr(UCase(Argument), "!/" & UCase(x) & ":")
        k2 = InStr(k1 + LNG, UCase(Argument), "!/")
        If k1 > 0 And k2 > 0 Then
            Result = Mid(Argument, k1 + LNG + 1, k2 - k1 - LNG - 1)
            If IsNumeric(Result) > 0 And InStr(Result, ",") > 0 Then Result = Replace(Result, ",", ".")
        End If

        If Result = "False" Then Result = "False"

        Return Result
    End Function

    Sub AfficheProgramme(BasePath, BaseName, g)


        Dim listeVide As Boolean = True
        '''''''''''''''''
        Form3.ListBox1.Items.Clear()
        Form3.TextBox5.Text = "GRILLE DES PROGRAMME " & BaseName
        If FullScreen = True Then
            Form3.ListBox1.Font = New System.Drawing.Font("Arial", 20)
            Form3.TextBox5.Font = New System.Drawing.Font("Arial Black", 20)
            Form3.ListBox1.Items.Add("")
        Else
            Form3.ListBox1.Font = New System.Drawing.Font("Arial", 10)
            Form3.TextBox5.Font = New System.Drawing.Font("Arial Black", 13)
        End If



        '''''''''''''''''''''
        Dim mycommand As OleDbCommand
        Dim myconnection As OleDbConnection
        Dim Sql As String
        Dim table As New DataTable
        myconnection = New OleDbConnection(OLEDBPROVIDER & BasePath)
        myconnection.Open()
        tablename = "MinuterieActive"
        Sql = "Select * from MinuterieActive"
        mycommand = New OleDb.OleDbCommand(Sql, myconnection)
        mycommand.ExecuteNonQuery()
        reader1 = mycommand.ExecuteReader()

        Dim act As Boolean
        While reader1.Read()
            act = reader1("Actif")

            If act = True Then

                RowJour = reader1("Jour")
                RowHeure = reader1("Heure")
                RowType = reader1("Type")
                RowAction = reader1("Action") : RowAction = RowAction.Replace("''", "'")
                rowArgument = reader1("Argument")

                Titre = Arg("Titre", rowArgument)
                If Titre = "" Then
                    Titre = Trim(RowAction)
                    trouve = 0
                    For i = Len(Titre) To 1 Step -1
                        If Mid(Titre, i, 2) = "!\" Then trouve = i : Exit For
                    Next
                    If trouve <> 0 Then Titre = Mid(Titre, trouve + 1)
                    trouve = 0
                    For i = 1 To Len(Titre)
                        If IsNumeric(Mid(Titre, i, 1)) = False And Mid(Titre, i, 1) <> " " And Mid(Titre, i, 1) <> "-" Then trouve = i : Exit For
                    Next
                    If trouve <> 0 Then Titre = Mid(Titre, trouve)
                End If
                Form3.ListBox1.Items.Add(" " & RowJour & vbTab & RowHeure & vbTab & RowType & " " & Titre)
                listeVide = False
            End If
        End While
        myconnection.Close()
        '''''''''''''''''''''''



        If listeVide = True Then Exit Sub

        '''''''''''''''''
        max = Form3.Width
        hauteur = Form3.Height
        Form3.Panel1.Width = max * 0.4
        Form3.TextBox5.Width = Form3.Panel1.Width
        largeurBandeau = Form3.Panel1.Width
        Form3.Panel1.Left = max
        Form3.Panel1.Top = hauteur * 0.15
        Form3.Panel1.Height = hauteur * 0.7
        Form3.ListBox1.Height = hauteur * 0.7
        Form3.ListBox1.Height = Form3.Panel1.Height - Form3.TextBox5.Height
        Form3.Button1.Height = hauteur * 0.7 * 0.8
        Form3.Button1.Top = Form3.Panel1.Top * 1.38
        Form3.Panel1.Visible = True


        'g = 1
        If g = 0 Then
            Form3.Button1.Visible = True
            For i = 1 To largeurBandeau + 2 Step 2
                Form3.Panel1.Left = max - i
                'Threading.Thread.Sleep(1)
                Form3.Panel1.Refresh()
                Application.DoEvents()
                Form3.Button1.Left = Form3.Panel1.Left - Form3.Button1.Width - 3
            Next
        Else
            For i = 1 To largeurBandeau + 2 Step 2
                Form3.Panel1.Left = i - largeurBandeau
                'Threading.Thread.Sleep(1)
                Form3.Panel1.Refresh()
                Application.DoEvents()
                'Form3.Button1.Left = largeurBandeau + Form3.Panel1.Left '- Form3.Button1.Width - 3
            Next

        End If

        wait(15)

        Form3.PictureBox1.Refresh()

        If g = 0 Then
            For i = largeurBandeau + 2 To 1 Step -2
                Form3.Panel1.Left = max - i
                Application.DoEvents()
                Form3.Button1.Left = Form3.Panel1.Left - Form3.Button1.Width
            Next
        Else
            For i = 0 To -largeurBandeau Step -2
                Form3.Panel1.Left = i
                Application.DoEvents()
                Form3.Button1.Left = Form3.Panel1.Left - Form3.Button1.Width
            Next

        End If

        Form3.Panel1.Visible = False : Form3.Button1.Visible = False
    End Sub




    Function ReplayCherche(SourceDir, Mot, ext)
        Filename = ""
        Mot = UCase(Mot)


        Dim list As List(Of String) = FileHelper.GetFilesRecursive(SourceDir, ext)
        Dim FileInfo As FileInfo
        Dim d1 As DateTime
        Dim d2 As DateTime = "1/1/1950"

        For Each Ppath In list
            If Len(Ppath) > 258 Then GoTo nx
            FileInfo = New FileInfo(Ppath)
            fichier = FileInfo.Name
            fichierA = UCase(Path.GetFileNameWithoutExtension(fichier))

            If InStr(fichierA, Mot) > 0 Then
                d1 = System.IO.File.GetCreationTime(Ppath)
                If d1 > d2 Then d2 = d1 : Filename = FileInfo.ToString
            End If
nx:
        Next



        ReplayCherche = Filename

    End Function


    Public Class FileHelper
        Public Shared Function GetFilesRecursive(ByVal initial As String, ext As String) As List(Of String)
            ' This list stores the results.
            Dim result As New List(Of String)

            ' This stack stores the directories to process.
            Dim stack As New Stack(Of String)

            ' Add the initial directory
            stack.Push(initial)

            ' Continue processing for each stacked directory
            Do While (stack.Count > 0)
                ' Get top directory string
                Dim dir As String = stack.Pop
                Try
                    ' Add all immediate file paths
                    'result.AddRange(Directory.GetFiles(dir, "*.*"))
                    result.AddRange(Directory.GetFiles(dir, ext))
                    ' Loop through all subdirectories and add them to the stack.
                    Dim directoryName As String
                    For Each directoryName In Directory.GetDirectories(dir)
                        stack.Push(directoryName)
                    Next

                Catch ex As Exception
                End Try
            Loop

            ' Return the list
            Return result
        End Function


    End Class




    Sub WinActivate(x)

        Try
            hWnd = WindowHandle(x)
            'hWnd = FindWindow(Nothing, x)
            SetForegroundWindow(hWnd)
        Catch ex As Exception

        End Try

    End Sub




    ''' <summary>
    ''' Bouton "Rechercher" de la fenetre Nflix (Ctrl+F) : le nom d'une chaine, d'un film ou d'une serie dans TOUS les
    ''' contenus (chaines, films et series ; demande de Jacob, 13/10/2026), ET les groupes (demande de Jacob, 13/10/2026 : "la
    ''' recherche doit porter aussi sur les groupes"). Voir RechercheNflix : liste gardee en memoire, accents et
    ''' majuscules ignores, plusieurs mots = tous presents, resultats TRIES (nom qui commence par le texte,
    ''' puis qui le contient, puis ordre alphabetique, puis ceux dont le groupe contient le texte).
    ''' La liste de gauche n'affiche plus que les groupes trouves ; une recherche vide la remet en entier.
    ''' </summary>
    Sub rechercherUnTitre()

        ' Fenetre de saisie avec la case "Dans ce groupe" (15/10/2026 : "dans rechercher rajoute option dans ce groupe").
        Dim groupeChoisi As String = If(Form14.ListBox1.SelectedItem Is Nothing, "", Convert.ToString(Form14.ListBox1.SelectedItem))
        Dim dansLeGroupe As Boolean = RechercheNflix.DerniereRechercheDansLeGroupe
        Dim saisieBrute As String = FormRechercheNflix.Demander(Form14, RechercheNflix.DerniereRecherche, groupeChoisi, dansLeGroupe)
        If saisieBrute Is Nothing Then Exit Sub                         ' (annule)
        Dim saisie As String = saisieBrute.Trim()
        RechercheNflix.DerniereRechercheDansLeGroupe = dansLeGroupe
        If RechercheNflix.DemandeTresBienNotes Then                       ' bouton "Films tres bien notes" (texte et groupe : restrictions)
            Form14.AfficherLesTresBienNotes(saisie, groupeChoisi, dansLeGroupe, RechercheNflix.DemandeExcellents, RechercheNflix.DemandeBons)
            Exit Sub
        End If
        If saisie = "" Then
            If RechercheNflix.FiltreDesGroupes IsNot Nothing Then
                RechercheNflix.FiltreDesGroupes = Nothing
                Form14.ReafficherLesGroupes()
                Form14.Text = "Nflix"
            End If
            Exit Sub
        End If
        RechercheNflix.DerniereRecherche = saisie
        Nom = saisie                                    ' (variable globale de l'ancien code)
        Form14.Cursor = Cursors.WaitCursor

        Try
            Form14.VlcControl1.Stop()
            Dim tronque As Boolean = False
            Dim groupesTrouves As List(Of String) = Nothing
            Dim avertissement As String = ""
            Form14.Cursor = Cursors.WaitCursor
            Dim listes As List(Of String) = IptvSources.ListesDeToutLeContenu(IptvSources.SelectionIptv, IptvSources.GenreAffiche, avertissement)
            Dim trouves As List(Of RechercheNflix.Entree) = RechercheNflix.ChercherDansTout(listes, saisie, RechercheNflix.MaximumDeResultats, tronque, groupesTrouves,
                                                                                            If(dansLeGroupe AndAlso groupeChoisi <> "", groupeChoisi, Nothing))
            If dansLeGroupe AndAlso groupeChoisi <> "" Then groupesTrouves = New List(Of String) From {groupeChoisi}      ' (la liste des groupes reste sur celui-la)
            RechercheNflix.FiltreDesGroupes = groupesTrouves
            Form14.ReafficherLesGroupes()
            RechercheNflix.RemplirLaGrille(Form14.DataGridView2, trouves)
            Form14.DataGridView2.Visible = True

            If trouves.Count = 0 AndAlso groupesTrouves.Count = 0 Then
                Form14.Text = "Nflix"
                MessageBox.Show(Form14, "Aucun résultat pour « " & saisie & " ».", "Rechercher", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                Dim parties As New List(Of String)
                Dim nbChaines As Integer = trouves.Where(Function(x) x.Contenu() = "Chaîne").Count()
                Dim nbFilms As Integer = trouves.Where(Function(x) x.Contenu() = "Film").Count()
                Dim nbSeries As Integer = trouves.Where(Function(x) x.Contenu() = "Série").Count()
                If nbChaines > 0 Then parties.Add(nbChaines & If(nbChaines > 1, " chaînes", " chaîne"))
                If nbFilms > 0 Then parties.Add(nbFilms & If(nbFilms > 1, " films", " film"))
                If nbSeries > 0 Then parties.Add(nbSeries & If(nbSeries > 1, " séries", " série"))
                If groupesTrouves.Count > 0 Then parties.Add(groupesTrouves.Count & If(groupesTrouves.Count > 1, " groupes", " groupe"))
                Form14.Text = "Nflix - " & If(tronque, "premiers résultats : ", "") & String.Join(", ", parties) & " pour « " & saisie & " »"
                If trouves.Count > 0 Then
                    Nom = trouves(0).Adresse
                    Title = trouves(0).Nom
                    ChannelLogo = trouves(0).Logo.Replace("_small", "_big")
                    AfficheLogo(ChannelLogo, 0, Nom, Title)
                End If
                If tronque Then MessageBox.Show(Form14, "Plus de " & RechercheNflix.MaximumDeResultats & " titres : seuls les premiers (les plus proches) sont affichés." & vbCrLf & "Précisez la recherche avec un mot de plus.", "Rechercher", MessageBoxButtons.OK, MessageBoxIcon.Information)
                If avertissement <> "" Then MessageBox.Show(Form14, "Certains contenus n'ont pas pu être inclus dans la recherche :" & vbCrLf & avertissement, "Rechercher", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show(Form14, "La recherche a échoué : " & ex.Message, "Rechercher", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            Form14.Cursor = Cursors.Default
        End Try

    End Sub

    Sub GestionControlAdistance(x)

        Try

            If x = True Then
                ' Reception refaite le 08/10/2026 (surveillance + rattrapage chaque seconde) : voir CommandesADistance.
                CommandesADistance.Armer(DatabaseNetPath & "\ControleAdistance\")

            Else
                CommandesADistance.Desarmer()
                'Form3.Timer7.Stop()
            End If

        Catch ex As Exception

        End Try



    End Sub

    Sub GestionControlAdistanceSub2(source As Object, e As FileSystemEventArgs, Optional Commande As String = "")
        If DatabaseNetPath = "" Then Exit Sub
        'If FswDeja = True Then Exit Sub

        FswDeja = True
        Dim instruction As String
        Try
            If Commande = "" Then
                fichier = e.FullPath
                instruction = UCase(e.Name)
            Else
                instruction = UCase(Commande)
            End If

            If Trim(instruction) = "" Then Exit Sub

            Select Case True

                ' Deezer dans Slide (MusiqueDeezer) : les memes commandes, envoyees au lecteur
                ' integre, sans prendre le clavier ni la souris. (Dessous : l'ancienne facon, pour
                ' l'application Deezer externe, quand "integre a Slide" est decoche.)
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.NEXT")
                    MusiqueDeezer.Suivant()
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.PAUSE")
                    MusiqueDeezer.Basculer()
                ' Lecture et pause en deux commandes distinctes (demande de Jacob, 08/10) :
                ' "Deezer.Play" joue, "Deezer.Stop" met en pause ("Deezer.Pause" reste la bascule).
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.PLAY")
                    MusiqueDeezer.LectureSurDemande()
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.STOP")
                    MusiqueDeezer.PauseSurDemande()
                ' "Deezer.Favo_Non" (08/10/2026) : le morceau qui joue est RETIRE des Coups de coeur. Place
                ' avant "Deezer.Favo", dont le nom commence pareil.
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.FAVO_NON")
                    MusiqueDeezer.RetirerDesFavoris()
                ' "Deezer.Favo" (et "Deezer.Like") : le morceau qui joue est ajoute aux Coups de coeur,
                ' comme la touche L envoyee a l'application Deezer. (Le 08/10, un moment, elle a lance
                ' la playlist des Coups de coeur : remis comme avant a la demande de Jacob.)
                Case MusiqueDeezer.Interne AndAlso (instruction.StartsWith("DEEZER.FAVO") OrElse instruction.StartsWith("DEEZER.LIKE"))
                    MusiqueDeezer.Favori()
                ' "Deezer.Loved" : lance la playlist "Coups de coeur" du compte (comme la tache "Deezer Favoris").
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.LOVED")
                    DiagLog("Deezer : commande Deezer.Loved -> playlist des Coups de coeur")
                    DeezerParLaMinuterie(Nothing, MusiqueDeezer.Favoris, True)
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.SONGNAMEBYMAIL")
                    MusiqueDeezer.EnvoyerLeTitre()
                ' Commandes ajoutees avec la telecommande .NET 10 (demande de Jacob, 08/10/2026).
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.PREV")
                    MusiqueDeezer.Precedent()
                Case MusiqueDeezer.Interne AndAlso instruction.StartsWith("DEEZER.FLOW")
                    DiagLog("Deezer : commande Deezer.Flow -> Flow")
                    DeezerParLaMinuterie(Nothing, MusiqueDeezer.Flow, True)
                Case instruction.StartsWith("DEEZER.PREV") OrElse instruction.StartsWith("DEEZER.FLOW") OrElse instruction.StartsWith("DEEZER.LOVED")
                    DiagLog("Deezer externe : " & instruction & " ignoree (commande du Deezer integre a Slide)")
                ' "Deezer.Lance" et "Deezer.Kill" : les taches "Lance Deezer" et "Kill Deezer" de la minuterie
                ' (Deezer integre ou application externe, selon le reglage).
                Case instruction.StartsWith("DEEZER.LANCE")
                    DiagLog("Deezer : commande Deezer.Lance")
                    If MusiqueDeezer.Interne Then DeezerParLaMinuterie(Nothing, "", True) Else TacheDeezer("LANCE DEEZER", Nothing, "")
                Case instruction.StartsWith("DEEZER.KILL")
                    DiagLog("Deezer : commande Deezer.Kill")
                    TacheDeezer("KILL DEEZER", Nothing, "")

                ' Application Deezer externe : Slide ne sait pas si elle joue ou non ; "Deezer.Play"
                ' et "Deezer.Stop" y font donc la bascule, comme "Deezer.Pause".
                Case instruction.StartsWith("DEEZER.PLAY") OrElse instruction.StartsWith("DEEZER.STOP")
                    DiagLog("Deezer externe : " & instruction & " -> bascule lecture/pause (etat de l'application inconnu)")
                    GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Pause")

                Case instruction.StartsWith("DEEZER.NEXT")
                    Dim rrect As RECT
                    Dim Foc = FocusWindow("Deezer", Nothing)
                    If Foc.Handle = 0 Then Exit Sub
                    rrect = Foc.W_RECT
                    waitExclusif(wwDeezer)
                    Application.DoEvents()
                    Cursor.Position = New Point(rrect.Left + 10, rrect.Bottom - 20)
                    mouse_event(&H2, 0, 0, 0, 0)
                    mouse_event(&H4, 0, 0, 0, 0)
                    SendKeys.SendWait("+{RIGHT}")
                    waitExclusif(2)
                    UnFocusWindow("Deezer", Nothing)
                    FocusWindow("Slide", Nothing)

                Case instruction.StartsWith("DEEZER.PAUSE")
                    Dim rrect As RECT
                    Dim Foc = FocusWindow("Deezer", Nothing)
                    If Foc.Handle = 0 Then Exit Sub
                    rrect = Foc.W_RECT
                    waitExclusif(wwDeezer)
                    Application.DoEvents()
                    Cursor.Position = New Point(rrect.Left + 10, rrect.Bottom - 20)
                    mouse_event(&H2, 0, 0, 0, 0)
                    mouse_event(&H4, 0, 0, 0, 0)
                    'waitExclusif(1)
                    SendKeys.SendWait(" ")
                    waitExclusif(2)
                    UnFocusWindow("Deezer", Nothing)
                    FocusWindow("Slide", Nothing)

                Case instruction.StartsWith("DEEZER.FAVO")
                    Dim rrect As RECT
                    Dim Foc = FocusWindow("Deezer", Nothing)
                    If Foc.Handle = 0 Then Exit Sub
                    rrect = Foc.W_RECT
                    waitExclusif(wwDeezer)
                    Application.DoEvents()
                    Cursor.Position = New Point(rrect.Left + 10, rrect.Bottom - 20)
                    mouse_event(&H2, 0, 0, 0, 0)
                    mouse_event(&H4, 0, 0, 0, 0)
                    SendKeys.SendWait("L")
                    waitExclusif(2)
                    UnFocusWindow("Deezer", Nothing)
                    FocusWindow("Slide", Nothing)

                Case instruction.StartsWith("DEEZER.SONGNAMEBYMAIL")
                    Dim rrect As RECT
                    Dim Foc = FocusWindow("Deezer", Nothing)
                    If Foc.Handle = 0 Then Exit Sub
                    rrect = Foc.W_RECT
                    handle = Foc.Handle
                    waitExclusif(wwDeezer)
                    Application.DoEvents()
                    Cursor.Position = New Point(rrect.Left + 10, rrect.Bottom - 20)
                    waitExclusif(2)
                    mouse_event(&H2, 0, 0, 0, 0)
                    mouse_event(&H4, 0, 0, 0, 0)

                    Dim img As New Bitmap(rrect.Right - rrect.Left, rrect.Bottom - rrect.Top)
                    Dim gr As Graphics = Graphics.FromImage(img)
                    gr.CopyFromScreen(New Point(rrect.Left, rrect.Top), Point.Empty, img.Size)
                    img.Save(DossierReglages & "\Artiste.png", System.Drawing.Imaging.ImageFormat.Png)
                    'Process.Start(DossierReglages & "\Artiste.png")
                    gr.Dispose()
                    img.Dispose()
                    waitExclusif(2)
                    UnFocusWindow("Deezer", Nothing)
                    FocusWindow("Slide", Nothing)
                    ' (L'image est montree dans le message, pas en piece jointe : demande de Jacob, 08/10/2026.)
                    Call SendEmail("jacob@ajc.fr", "Deezer Song Name By Mail", "<html><body><p><img src=""cid:Artiste.png"" style=""max-width:100%;height:auto""></p></body></html>", "", DossierReglages & "\Artiste.png")


                Case instruction.StartsWith("VOLUME.PLUS")
                    increment = Val(Mid(instruction, 12))
                    VolumeGeneral = VolumeGeneral + increment
                    If VolumeGeneral > 100 Then VolumeGeneral = 100
                    My.Settings.VolumeGeneral = VolumeGeneral
                    My.Settings.Save()
                    SetMasterVol(VolumeGeneral)


                Case instruction.StartsWith("VOLUME.MOINS")
                    increment = Val(Mid(instruction, 13))
                    VolumeGeneral = VolumeGeneral - increment
                    If VolumeGeneral < 0 Then VolumeGeneral = 0
                    My.Settings.VolumeGeneral = VolumeGeneral
                    My.Settings.Save()
                    SetMasterVol(VolumeGeneral)



            End Select


        Catch ex As Exception

        End Try


        If Commande = "" Then
            Try
                File.Delete(fichier)
            Catch ex As Exception

            End Try

        End If
        instruction = ""

        'FswDeja = False
    End Sub




    Function ChangeDatePhotoByExif(strFichier)

        'If InStr(UCase(strFichier), "IMG_054489.MP4") > 0 Then Stop

        If My.Settings.reprendreLeSlide = True Then
            My.Settings.reprendreLeSlide = False
            My.Settings.Save()
            IO.File.Delete(GridName)
            MsgBox("Veuillez relancer Slide et Recommencer la procedure SVP ")
            End
        End If


        Try
            qq = MetadataExtractor.ImageMetadataReader.ReadMetadata(strFichier)

        Catch ex As Exception

        End Try

        Dim Directories As IEnumerable(Of MetadataExtractor.Directory) = qq
        'Dim Directories As IEnumerable(Of MetadataExtractor.Directory) = MetadataExtractor.ImageMetadataReader.ReadMetadata(strFichier)
        'Dim subIfdDirectory = Directories.OfType(Of ExifSubIfdDirectory).FirstOrDefault
        If qq IsNot Nothing Then
            subIfdDirectory = Directories.OfType(Of ExifSubIfdDirectory).FirstOrDefault
        End If


        'Dim TagDateTimeOriginal2 As Date

        Try
            If subIfdDirectory Is Nothing Then

                sto = 0 : dd = ""
                For Each Directory In Directories
                    For Each MonInfo As MetadataExtractor.Tag In Directory.Tags
                        If UCase(MonInfo.Name) = "CREATED" Then
                            dd = MonInfo.Description
                            sto = 1
                            Exit For
                        End If
                    Next
                    If sto = 1 Then Exit For
                Next
                If sto = 1 Then
                    Dim result() As String
                    result = dd.Split(" ") ', StringSplitOptions.RemoveEmptyEntries)
                    aa = result(2) & " " & result(1) & " " & result(4) & " " & result(3)
                    If result(4) <> "1904" Then
                        Dim d As System.DateTime = DateTime.Parse(aa)
                        TagDateTimeOriginal2 = d.ToString("dd-MM-yyyy HH:mm")
                    End If
                Else
                    TagDateTimeOriginal2 = Nothing
                End If


            Else
                TagDateTimeOriginal = subIfdDirectory.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)

                TagDateTimeOriginal2 = DateSerial(Mid(TagDateTimeOriginal, 1, 4), Mid(TagDateTimeOriginal, 6, 2), Mid(TagDateTimeOriginal, 9, 2)) & Mid(TagDateTimeOriginal, 11)
            End If

        Catch ex As Exception
            TagDateTimeOriginal2 = Nothing
        End Try

        Return TagDateTimeOriginal2
    End Function

    Sub ModifDateParExifDate()


        Form3.Timer3.Stop()
        FlagStop = True
        ModeModifExif = True
        Form2.Cursor = Cursors.WaitCursor
        Form2.ProgressBar1.Visible = True
        Form2.ProgressBar2.Visible = True

        Form2.Button2.Visible = False
        Form2.Button3.Visible = False
        Form2.Button4.Visible = False
        Form2.Button5.Visible = False
        Form2.HScrollBar1.Visible = False
        Form2.HScrollBar2.Visible = False
        Form2.CheckBox1.Visible = False
        Form2.Label4.Visible = False

        j = 0

        Form2.Show()
        Form2.Refresh()

        total1 = Form2.DataGridView1.Rows.Count - 1
        total2 = +Form2.DataGridView2.Rows.Count - 1
        total = total1 + total2

        For i = 0 To total1
            Form2.DataGridView1.Rows(i).Cells(4).Value = ""
        Next
        For i = 0 To total2
            Form2.DataGridView2.Rows(i).Cells(4).Value = ""
        Next


        Form2.DataGridView1.Update()
        Form2.DataGridView2.Update()
        Application.DoEvents()





        'GoTo suivant
        For i = 0 To total1
            Nom = Form2.DataGridView1.Rows(i).Cells(3).Value
            ExifDate = ChangeDatePhotoByExif(Nom)
            If IsDate(ExifDate) Then Form2.DataGridView1.Rows(i).Cells(4).Value = ExifDate '"01/01/1900"
            j = j + 1 : If j = 250 Then
                Form2.ProgressBar1.Value = i * 100 / total1
                Application.DoEvents()
                j = 0
            End If
        Next
        Form2.DataGridView1.Update()

suivant:

        For k = 0 To total2
            Nom = Form2.DataGridView2.Rows(k).Cells(3).Value
            ExifDate = ChangeDatePhotoByExif(Nom)
            If IsDate(ExifDate) Then Form2.DataGridView2.Rows(k).Cells(4).Value = ExifDate '"01/01/1900"
            j = j + 1 : If j = 250 Then
                Form2.ProgressBar2.Value = (k * 100) / total2

                Application.DoEvents()
                j = 0
            End If
        Next


        Form2.DataGridView2.Update()

        'FlagStop = False
        Form2.ProgressBar1.Value = 100
        Form2.ProgressBar2.Value = 100

        Form2.Cursor = Cursors.Default
        Form2.Button6.Visible = True

        Application.DoEvents()
        Beep()

    End Sub


    Sub ChangeDatebyExifNow()

        Form2.ProgressBar1.Value = 0
        Form2.ProgressBar2.Value = 0




        Form2.Cursor = Cursors.WaitCursor

        Form2.ProgressBar1.Visible = True
        Form2.ProgressBar2.Visible = True
        Form2.Button2.Visible = False

        Dim d As System.DateTime
        Form2.Show()
        Form2.Refresh()

        total1 = Form2.DataGridView1.Rows.Count - 1
        total2 = Form2.DataGridView2.Rows.Count - 1
        total = total1 + total2

        'GoTo suivant
        j = 0
        For i = 0 To total1
            Nom = Form2.DataGridView1.Rows(i).Cells(3).Value
            fic = Path.GetFileName(Nom)

            DateExif = Form2.DataGridView1.Rows(i).Cells(4).Value
            If DateExif <> "" Then
                Dt = DateExif
            Else
                DateOrg = Form2.DataGridView1.Rows(i).Cells(2).Value
                d = DateTime.Parse(DateOrg)
                Dt = d.ToString
            End If

            DirSource = Path.GetDirectoryName(Nom)

            d = DateTime.Parse(Dt)
            Rep1 = d.ToString("yyyy")
            Rep2 = d.ToString("yyyy MM")

            trouve = 0
            For h = Len(DirSource) To 1 Step -1
                If Mid(DirSource, h, 1) = "\" And trouve = 1 Then trouve = h : Exit For
                If Mid(DirSource, h, 1) = "\" Then trouve = 1

            Next


            DirDestination = Left(DirSource, trouve) & Rep1 & "\" & Rep2 & "\" & fic

            If UCase(Nom) <> UCase(DirDestination) Then
                Try
                    My.Computer.FileSystem.MoveFile(Nom, DirDestination)
                Catch ex As Exception
                    'EndAndContinue()
                    Kill(Nom)
                    My.Computer.FileSystem.MoveFile(Nom, DirDestination)
                End Try
            End If


            'My.Computer.FileSystem.MoveFile(Nom, DirDestination)
            'My.Computer.FileSystem.CopyFile(Nom, DirDestination, True)
            Try
                File.SetLastWriteTime(DirDestination, Dt)
                File.SetCreationTime(DirDestination, Dt)
            Catch ex As Exception

            End Try


            j = j + 1 : If j = 250 Then
                Form2.ProgressBar1.Value = i * 100 / total1
                Application.DoEvents()
                j = 0
            End If
nx:
        Next
        Form2.ProgressBar1.Value = 100

suivant:

        j = 0
        For i = 0 To total2
            Nom = Form2.DataGridView2.Rows(i).Cells(3).Value
            fic = Path.GetFileName(Nom)
            DateExif = Form2.DataGridView2.Rows(i).Cells(4).Value

            If DateExif <> "" Then
                Dt = DateExif
            Else
                DateOrg = Form2.DataGridView2.Rows(i).Cells(2).Value
                d = DateTime.Parse(DateOrg)
                Dt = d.ToString
            End If

            DirSource = Path.GetDirectoryName(Nom)

            d = DateTime.Parse(Dt)
            Rep1 = d.ToString("yyyy")
            Rep2 = d.ToString("yyyy MM")


            trouve = 0
            For h = Len(DirSource) To 1 Step -1
                If Mid(DirSource, h, 1) = "\" And trouve = 1 Then trouve = h : Exit For
                If Mid(DirSource, h, 1) = "\" Then trouve = 1

            Next



            DirDestination = Left(DirSource, trouve) & Rep1 & "\" & Rep2 & "\" & fic

            If UCase(Nom) <> UCase(DirDestination) Then
                Try
                    My.Computer.FileSystem.MoveFile(Nom, DirDestination)
                Catch ex As Exception
                    'EndAndContinue()
                    Kill(Nom)
                    My.Computer.FileSystem.MoveFile(Nom, DirDestination)
                End Try
            End If


            'My.Computer.FileSystem.MoveFile(Nom, DirDestination)
            'My.Computer.FileSystem.CopyFile(Nom, DirDestination, True)
            File.SetLastWriteTime(DirDestination, Dt)
            File.SetCreationTime(DirDestination, Dt)

            j = j + 1 : If j = 100 Then
                Form2.ProgressBar2.Value = i * 100 / total2
                Application.DoEvents()
                j = 0
            End If
        Next
        Form2.ProgressBar2.Value = 100


        IO.File.Delete(GridName)
        Form2.Cursor = Cursors.Default

        MsgBox("ExifDate à eté executé avec succés")
        Application.DoEvents()
        End
        Form2.Button6.Visible = False

    End Sub

    Sub eteintPC()

        ' Use the Process class to execute the "shutdown" command
        Dim proc As New Process()
        proc.StartInfo.FileName = "shutdown"
        proc.StartInfo.Arguments = "-s -t 5" ' -s for shut down, -t for time in seconds
        proc.StartInfo.UseShellExecute = False
        proc.StartInfo.RedirectStandardOutput = True
        proc.Start()
    End Sub


    Function rechercher()

        trouve = 1
        Form14.ListBox1.Items.Clear()          ' (changement de source IPTV : les groupes de la precedente ne restent pas)
        Form14.Text = "Nflix"
        RechercheNflix.FiltreDesGroupes = Nothing           ' (une recherche precedente ne filtre plus la liste)
        ' Chaines : la liste de la source active ; films ou series : leur catalogue (voir IptvSources.GenreAffiche).
        If IptvSources.GenreAffiche = IptvSources.GenreIptv.Chaines AndAlso My.Computer.FileSystem.FileExists(DossierNflixParDefault) = False Then
            Form4.Cursor = Cursors.WaitCursor
            Application.DoEvents()
            Call create_IPTV_File()
            Form4.Cursor = Cursors.Default
        End If

        Try
            ' La liste est lue une fois et gardee (RechercheNflix) : groupes, contenu d'un groupe et recherche en viennent.
            Dim groupes As List(Of String) = RechercheNflix.Groupes(IptvSources.ListeAffichee())
            Form14.ListBox1.BeginUpdate()
            Try
                Form14.ListBox1.Items.AddRange(RechercheNflix.GroupesTries(groupes).ToArray())
            Finally
                Form14.ListBox1.EndUpdate()
            End Try
            trouve = If(groupes.Count > 0, 1, 0)

        Catch ex As Exception
            trouve = 0
            MessageBox.Show(ex.Message)
        End Try

        rechercher = trouve

    End Function


    Sub ChercheGroupe(groupeRecherché)

        Iptv_File = AppPath & "iptv_File" & ".m3u"
        Form14.Text = "Nflix"
        ' Le contenu du groupe vient de la liste gardee en memoire (RechercheNflix) : plus de relecture du fichier.
        RechercheNflix.RemplirLaGrille(Form14.DataGridView2, RechercheNflix.DuGroupe(IptvSources.ListeAffichee(), CStr(groupeRecherché)))
        Form14.DataGridView2.Visible = True

    End Sub


    Sub create_IPTV_File()



        If Trim(IPTV_url) = "" Then Exit Sub
        CheminFilm = DossierNflixParDefault

        ' Liste .m3u ou compte Xtream, de la source IPTV active (voir IptvSources).
        Dim erreurIptv As String = ""
        If Not IptvSources.Telecharger(IptvSources.SelectionIptv, erreurIptv) Then MsgBox(erreurIptv)




    End Sub



    ' ------------------------------------------------------------------------------------------
    ' Copie du programme sur la cle USB (menu du clic droit : "Syncronniser sur UsbKey")
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Dossier qui recoit la copie a la racine de la cle. Le nom de la cle, lui, est
    ''' choisi dans la fenetre (variable CleUsbNom, enregistree d'une fois sur l'autre).</summary>
    Public Const DossierSurLaCle As String = "Slide"

    ''' <summary>Dossier du projet Slide (celui qui contient bin, les sources et My Project),
    ''' deduit de l'endroit d'ou tourne le programme. "" si on ne le reconnait pas.</summary>
    Public Function DossierDuProjet() As String
        Try
            Dim p As Integer = InStr(UCase(AppPath), "\BIN\")
            If p = 0 Then Return ""
            Return Mid(AppPath, 1, p - 1)
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>Racine de la cle portant le nom <paramref name="nom"/> ("F:\"), "" si absente.</summary>
    Public Function RacineDeLaCle(Optional nom As String = "") As String
        If nom = "" Then nom = CleUsbNom
        For Each d As IO.DriveInfo In IO.DriveInfo.GetDrives()
            Try
                If d.IsReady AndAlso String.Equals(d.VolumeLabel, nom, StringComparison.OrdinalIgnoreCase) Then
                    Return d.RootDirectory.FullName
                End If
            Catch ex As Exception
                ' lecteur non pret : on passe au suivant
            End Try
        Next
        Return ""
    End Function

    ''' <summary>Ouvre la fenetre de copie sur cle USB : choix de la cle et deroulement affiche.</summary>
    Sub SynchroniserSurCleUsb()
        Dim fenetre As New FormSynchroUsb()
        fenetre.ShowDialog()
        fenetre.Dispose()
    End Sub

End Module





