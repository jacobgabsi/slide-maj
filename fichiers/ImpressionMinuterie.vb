Option Strict On
Option Explicit On

Imports System.Drawing.Printing

''' <summary>
''' Impression du programme de la minuterie (bouton "Imprimer" de l'onglet Minuterie des options),
''' refaite le 09/10/2026 a la demande de Jacob : "reprogramme completement l'icone imprimer, pour
''' imprimer le programme professionnellement sans modifier les largeurs des colonnes".
'''
''' L'ancienne impression se servait du tableau lui-meme comme brouillon : elle elargissait la colonne
''' Action, mettait Actif et Play a zero, ajoutait une colonne "Titre", imprimait les cases une a une
''' avec leurs largeurs d'ecran, puis remettait tout en place. Ici le tableau est seulement LU
''' (Relever) : rien n'y est change, ni largeur ni colonne. La mise en page est calculee pour le
''' papier :
'''  - titre, date d'impression, nombre de taches et nom du poste en haut de la premiere page ;
'''  - colonnes Jour, Heure, Type, Programme, Lecture, Duree, Volume, a la largeur de leur contenu,
'''    la colonne Programme prenant toute la place qui reste ;
'''  - le jour n'est ecrit qu'a la premiere tache de chaque jour (et en haut de chaque page), avec un
'''    filet plus marque entre deux jours ;
'''  - pour un film : son titre, et le chemin du fichier en petit en dessous ;
'''  - en-tete des colonnes repete sur chaque page, pied de page "Page 1 / 3" ;
'''  - une tache n'est jamais coupee entre deux pages.
''' </summary>
Friend NotInheritable Class ImpressionMinuterie

    ''' <summary>Une tache de la minuterie, telle qu'elle sera imprimee.</summary>
    Friend NotInheritable Class Tache
        Public Jour As String = ""
        Public Heure As String = ""
        Public Type As String = ""
        ''' <summary>Titre du film, ou l'action (chaine de TV, station, action Deezer...).</summary>
        Public Titre As String = ""
        ''' <summary>Chemin du fichier (ou l'action quand un titre la remplace) : en petit sous le titre.</summary>
        Public Detail As String = ""
        Public Lecture As String = ""
        Public Duree As String = ""
        Public Volume As String = ""
    End Class

    Private Const ColJour As Integer = 0
    Private Const ColHeure As Integer = 1
    Private Const ColType As Integer = 2
    Private Const ColProgramme As Integer = 3
    Private Const ColLecture As Integer = 4
    Private Const ColDuree As Integer = 5
    Private Const ColVolume As Integer = 6
    Private Shared ReadOnly Entetes As String() = {"Jour", "Heure", "Type", "Programme", "Lecture", "Durée", "Volume"}
    ''' <summary>Largeur maximale des colonnes a largeur de contenu (centiemes de pouce) ; Programme prend le reste.</summary>
    Private Shared ReadOnly Plafonds As Single() = {110, 80, 140, 0, 210, 90, 90}

    ''' <summary>Blanc a gauche et a droite du texte d'une colonne (centiemes de pouce).</summary>
    Private Const Marge As Single = 7.0F
    ''' <summary>Blanc au-dessus et au-dessous du texte d'une ligne.</summary>
    Private Const BlancV As Single = 4.0F

    Private Shared ReadOnly Encre As Color = Color.FromArgb(28, 28, 30)
    Private Shared ReadOnly Gris As Color = Color.FromArgb(110, 113, 120)
    Private Shared ReadOnly Accent As Color = Color.FromArgb(10, 100, 200)
    Private Shared ReadOnly FondEntete As Color = Color.FromArgb(234, 237, 242)
    Private Shared ReadOnly FondPair As Color = Color.FromArgb(247, 248, 250)
    Private Shared ReadOnly FiletFin As Color = Color.FromArgb(214, 217, 222)
    Private Shared ReadOnly FiletFort As Color = Color.FromArgb(140, 145, 153)

    Private ReadOnly _taches As List(Of Tache)
    Private ReadOnly _imprimeLe As Date = Now

    ' Mise en page, calculee a la premiere page avec les mesures de l'imprimante
    Private _largeurs As Single()
    Private _hauteurs As Single()
    Private _debuts As List(Of Integer)          ' numero de la premiere tache de chaque page
    Private _page As Integer
    Private _hTexte, _hDetail, _hEntete, _hPied As Single

    Private _fTitre, _fSous, _fEntete, _fTexte, _fJour, _fDetail, _fPied As Font
    Private _format As StringFormat

    Friend Sub New(taches As List(Of Tache))
        _taches = If(taches, New List(Of Tache))
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Lecture du tableau (rien n'y est modifie)
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les taches du tableau des taches actives, dans l'ordre ou il les montre.</summary>
    Friend Shared Function Relever(grille As DataGridView) As List(Of Tache)
        Dim taches As New List(Of Tache)
        If grille Is Nothing Then Return taches
        For Each ligne As DataGridViewRow In grille.Rows
            If ligne.IsNewRow Then Continue For
            Dim t As New Tache With {
                .Jour = TexteDe(ligne, "Jour"),
                .Heure = TexteDe(ligne, "Heure"),
                .Type = TexteDe(ligne, "Type"),
                .Duree = TexteDe(ligne, "Durée")
            }

            ' Programme : le titre du film (retenu dans l'infobulle de la case Play), sinon le nom du
            ' fichier sans son dossier ni son extension ; le chemin complet passe en petit dessous.
            Dim action As String = TexteDe(ligne, "Action")
            Dim titre As String = ""
            Try
                titre = If(ligne.Cells("Play").ToolTipText, "").Trim()
            Catch ex As Exception
            End Try
            ' (Titre recopie d'un autre film de la minuterie : pas imprime, voir Module2.TitreDUnAutreFilm.)
            If titre <> "" AndAlso TitreDUnAutreFilm(grille, ligne.Index, titre) Then titre = ""
            Dim fichier As Boolean = action.Contains("\"c) AndAlso Not action.Contains("://")
            If fichier AndAlso titre = "" Then titre = NomSansExtension(action)
            If titre <> "" AndAlso Not String.Equals(titre, action, StringComparison.OrdinalIgnoreCase) Then
                t.Titre = titre
                t.Detail = action
            Else
                t.Titre = action
            End If

            ' Lecture : le texte de la case Play (s'il ne repete pas la duree) et le PIP.
            Dim reglages As String = TexteDe(ligne, "Argument", valeurBrute:=True)
            Dim lecture As String = TexteDe(ligne, "Play")
            If lecture.StartsWith("Pip:", StringComparison.OrdinalIgnoreCase) Then lecture = ""      ' ancien texte des chaines, plus tenu a jour
            If lecture = t.Duree Then lecture = ""
            Try
                If UCase(CStr(Arg("Pip", reglages))) = "TRUE" Then
                    Dim taille As Integer = LireTaillePip(reglages)
                    Dim pip As String = If(taille > 0, "PIP " & taille & " %", "PIP")
                    lecture = If(lecture = "", pip, lecture & "  ·  " & pip)
                End If
            Catch ex As Exception
            End Try
            t.Lecture = lecture

            Dim volume As String = TexteDe(ligne, "Volume")
            Dim v As Integer
            t.Volume = If(Integer.TryParse(volume, v), v & " %", volume)
            taches.Add(t)
        Next
        Return taches
    End Function

    Private Shared Function TexteDe(ligne As DataGridViewRow, colonne As String, Optional valeurBrute As Boolean = False) As String
        Try
            If Not ligne.DataGridView.Columns.Contains(colonne) Then Return ""
            Dim c As DataGridViewCell = ligne.Cells(colonne)
            Return Convert.ToString(If(valeurBrute, c.Value, c.FormattedValue)).Trim()
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Shared Function NomSansExtension(chemin As String) As String
        Try
            Dim nom As String = IO.Path.GetFileNameWithoutExtension(chemin)
            Return If(String.IsNullOrWhiteSpace(nom), chemin, nom.Trim())
        Catch ex As Exception
            Return chemin
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Le bouton "Imprimer"
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Choix de l'imprimante, puis impression du programme. Le tableau n'est pas modifie.</summary>
    Friend Shared Sub Imprimer(proprietaire As IWin32Window, grille As DataGridView)
        Dim taches As List(Of Tache) = Relever(grille)
        If taches.Count = 0 Then
            MessageBox.Show(proprietaire, "La minuterie ne contient aucune tâche active : il n'y a rien à imprimer.", "Imprimer la minuterie",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim impression As New ImpressionMinuterie(taches)
        Try
            Using doc As PrintDocument = impression.Document(),
                  choix As New PrintDialog With {.Document = doc, .UseEXDialog = True, .AllowSomePages = False, .AllowSelection = False}
                If choix.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub
                doc.Print()
                DiagLog("Minuterie imprimee : " & taches.Count & " tache(s), " & impression.NombreDePages & " page(s), imprimante """ & doc.PrinterSettings.PrinterName & """")
            End Using
        Catch ex As Exception
            DiagLog("Impression de la minuterie : " & ex.Message)
            MessageBox.Show(proprietaire, "L'impression n'a pas pu se faire :" & vbCrLf & ex.Message, "Imprimer la minuterie",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>Nombre de pages de la derniere impression (0 tant que rien n'a ete mis en page).</summary>
    Friend ReadOnly Property NombreDePages As Integer
        Get
            Return If(_debuts Is Nothing, 0, _debuts.Count)
        End Get
    End Property

    ''' <summary>Le document a imprimer : paysage, marges d'un demi-pouce, origine du dessin aux marges
    ''' (les memes coordonnees a l'apercu et sur le papier).</summary>
    Friend Function Document() As PrintDocument
        Dim doc As New PrintDocument With {.DocumentName = "Slide - Programme de la minuterie", .OriginAtMargins = True}
        doc.DefaultPageSettings.Landscape = True
        doc.DefaultPageSettings.Margins = New Margins(50, 50, 45, 45)
        AddHandler doc.BeginPrint, AddressOf AuDebut
        AddHandler doc.PrintPage, AddressOf UnePage
        AddHandler doc.EndPrint, AddressOf ALaFin
        Return doc
    End Function

    Private Sub AuDebut(sender As Object, e As PrintEventArgs)
        _debuts = Nothing
        _page = 0
        _fTitre = New Font("Segoe UI Semibold", 17.0F)
        _fSous = New Font("Segoe UI", 9.0F)
        _fEntete = New Font("Segoe UI Semibold", 8.5F)
        _fTexte = New Font("Segoe UI", 9.5F)
        _fJour = New Font("Segoe UI Semibold", 9.5F)
        _fDetail = New Font("Segoe UI", 7.5F)
        _fPied = New Font("Segoe UI", 8.0F)
        _format = New StringFormat(StringFormatFlags.NoWrap) With {.LineAlignment = StringAlignment.Near}
    End Sub

    Private Sub ALaFin(sender As Object, e As PrintEventArgs)
        For Each f As Font In {_fTitre, _fSous, _fEntete, _fTexte, _fJour, _fDetail, _fPied}
            If f IsNot Nothing Then f.Dispose()
        Next
        If _format IsNot Nothing Then _format.Dispose()
    End Sub

    Private Sub UnePage(sender As Object, e As PrintPageEventArgs)
        ' (Origine aux marges : la zone commence en 0,0.)
        Dim zone As New RectangleF(0, 0, e.MarginBounds.Width, e.MarginBounds.Height)
        If _debuts Is Nothing Then MettreEnPage(e.Graphics, zone)
        DessinerLaPage(e.Graphics, zone, _page)
        _page += 1
        e.HasMorePages = _page < _debuts.Count
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Mise en page
    ' ------------------------------------------------------------------------------------------

    Private Shared Function TexteDeLaColonne(t As Tache, colonne As Integer) As String
        Select Case colonne
            Case ColJour : Return t.Jour
            Case ColHeure : Return t.Heure
            Case ColType : Return t.Type
            Case ColProgramme : Return t.Titre
            Case ColLecture : Return t.Lecture
            Case ColDuree : Return t.Duree
            Case Else : Return t.Volume
        End Select
    End Function

    ''' <summary>Hauteur de ce qui precede le tableau : le titre sur la premiere page, un rappel sur les suivantes.</summary>
    Private Function HautDePage(g As Graphics, premiere As Boolean) As Single
        If premiere Then Return CSng(Math.Ceiling(_fTitre.GetHeight(g) + _fSous.GetHeight(g))) + 22
        Return CSng(Math.Ceiling(_fSous.GetHeight(g))) + 12
    End Function

    Private Sub MettreEnPage(g As Graphics, zone As RectangleF)
        ' Largeur des colonnes : celle de leur contenu ; Programme prend ce qui reste.
        ReDim _largeurs(Entetes.Length - 1)
        Dim fixes As Single = 0
        For i As Integer = 0 To Entetes.Length - 1
            If i = ColProgramme Then Continue For
            Dim l As Single = 0
            Dim vide As Boolean = True
            For Each t As Tache In _taches
                Dim s As String = TexteDeLaColonne(t, i)
                If s = "" Then Continue For
                vide = False
                l = Math.Max(l, g.MeasureString(s, If(i = ColJour, _fJour, _fTexte)).Width)
            Next
            If vide AndAlso i = ColLecture Then Continue For          ' colonne Lecture sans rien : pas imprimee
            ' (L'en-tete est ecrit en capitales : c'est en capitales qu'il se mesure.)
            l = Math.Max(l, g.MeasureString(Entetes(i).ToUpperInvariant(), _fEntete).Width) + 2 * Marge
            _largeurs(i) = CSng(Math.Ceiling(Math.Min(l, Plafonds(i))))
            fixes += _largeurs(i)
        Next
        ' (Page etroite - portrait, petit papier : la colonne Lecture cede de sa place au Programme.)
        Dim manque As Single = 220 - (zone.Width - fixes)
        If manque > 0 AndAlso _largeurs(ColLecture) > 70 Then
            Dim cede As Single = Math.Min(manque, _largeurs(ColLecture) - 70)
            _largeurs(ColLecture) -= cede
            fixes -= cede
        End If
        _largeurs(ColProgramme) = Math.Max(100, zone.Width - fixes)

        ' Hauteur des lignes : une ligne de texte, deux pour un film (titre et chemin).
        _hTexte = CSng(Math.Ceiling(_fTexte.GetHeight(g)))
        _hDetail = CSng(Math.Ceiling(_fDetail.GetHeight(g)))
        _hEntete = CSng(Math.Ceiling(_fEntete.GetHeight(g))) + 2 * BlancV + 2
        _hPied = CSng(Math.Ceiling(_fPied.GetHeight(g))) + 10
        ReDim _hauteurs(Math.Max(0, _taches.Count - 1))
        For i As Integer = 0 To _taches.Count - 1
            _hauteurs(i) = _hTexte + 2 * BlancV + If(_taches(i).Detail = "", 0, _hDetail + 1)
        Next

        ' Pages : une tache n'est jamais coupee.
        _debuts = New List(Of Integer) From {0}
        Dim dispo As Single = zone.Height - HautDePage(g, True) - _hEntete - _hPied
        Dim y As Single = 0
        For i As Integer = 0 To _taches.Count - 1
            If y + _hauteurs(i) > dispo AndAlso i > _debuts(_debuts.Count - 1) Then
                _debuts.Add(i)
                y = 0
                dispo = zone.Height - HautDePage(g, False) - _hEntete - _hPied
            End If
            y += _hauteurs(i)
        Next
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dessin d'une page
    ' ------------------------------------------------------------------------------------------

    Private Sub Ecrire(g As Graphics, texte As String, police As Font, couleur As Color, x As Single, y As Single, largeur As Single,
                       Optional alignement As StringAlignment = StringAlignment.Near, Optional chemin As Boolean = False)
        If texte = "" OrElse largeur <= 2 * Marge Then Exit Sub
        _format.Alignment = alignement
        _format.Trimming = If(chemin, StringTrimming.EllipsisPath, StringTrimming.EllipsisCharacter)
        Using brosse As New SolidBrush(couleur)
            g.DrawString(texte, police, brosse, New RectangleF(x + Marge, y, largeur - 2 * Marge, police.GetHeight(g) + 1), _format)
        End Using
    End Sub

    Private Sub DessinerLaPage(g As Graphics, zone As RectangleF, page As Integer)
        Dim premier As Integer = _debuts(page)
        Dim dernier As Integer = If(page + 1 < _debuts.Count, _debuts(page + 1), _taches.Count) - 1
        Dim y As Single = zone.Top

        ' ---- Haut de page
        If page = 0 Then
            Ecrire(g, "Programme de la minuterie", _fTitre, Encre, zone.Left - Marge, y, zone.Width + 2 * Marge)
            y += CSng(Math.Ceiling(_fTitre.GetHeight(g)))
            Dim fr As New Globalization.CultureInfo("fr-FR")
            Dim sous As String = "Imprimé le " & _imprimeLe.ToString("dddd d MMMM yyyy 'à' HH:mm", fr) &
                                 "   ·   " & _taches.Count & If(_taches.Count > 1, " tâches actives", " tâche active") &
                                 "   ·   Poste " & Environment.MachineName
            Ecrire(g, sous, _fSous, Gris, zone.Left - Marge, y, zone.Width + 2 * Marge)
            y += CSng(Math.Ceiling(_fSous.GetHeight(g))) + 7
            Using trait As New Pen(Accent, 2.0F)
                g.DrawLine(trait, zone.Left, y, zone.Right, y)
            End Using
            y = zone.Top + HautDePage(g, True)
        Else
            Ecrire(g, "Programme de la minuterie (suite)", _fSous, Gris, zone.Left - Marge, y, zone.Width + 2 * Marge)
            y = zone.Top + HautDePage(g, False)
        End If

        ' ---- En-tete des colonnes
        Using fond As New SolidBrush(FondEntete)
            g.FillRectangle(fond, zone.Left, y, zone.Width, _hEntete)
        End Using
        Dim x As Single = zone.Left
        For i As Integer = 0 To Entetes.Length - 1
            If _largeurs(i) <= 0 Then Continue For
            Ecrire(g, Entetes(i).ToUpperInvariant(), _fEntete, Encre, x, y + BlancV, _largeurs(i), If(i = ColVolume, StringAlignment.Far, StringAlignment.Near))
            x += _largeurs(i)
        Next
        y += _hEntete
        Using trait As New Pen(FiletFort, 1.0F)
            g.DrawLine(trait, zone.Left, y, zone.Right, y)
        End Using

        ' ---- Les taches
        Using fin As New Pen(FiletFin, 0.6F), fort As New Pen(FiletFort, 1.0F), pair As New SolidBrush(FondPair)
            For i As Integer = premier To dernier
                Dim t As Tache = _taches(i)
                Dim nouveauJour As Boolean = i = 0 OrElse Not String.Equals(t.Jour, _taches(i - 1).Jour, StringComparison.OrdinalIgnoreCase)
                Dim h As Single = _hauteurs(i)
                If i Mod 2 = 1 Then g.FillRectangle(pair, zone.Left, y, zone.Width, h)
                If i > premier Then g.DrawLine(If(nouveauJour, fort, fin), zone.Left, y, zone.Right, y)

                x = zone.Left
                For c As Integer = 0 To Entetes.Length - 1
                    Dim l As Single = _largeurs(c)
                    If l <= 0 Then Continue For
                    Select Case c
                        Case ColJour
                            ' Le jour : a sa premiere tache seulement, et rappele en haut de chaque page.
                            If nouveauJour OrElse i = premier Then Ecrire(g, t.Jour, _fJour, Encre, x, y + BlancV, l)
                        Case ColProgramme
                            Ecrire(g, t.Titre, _fTexte, Encre, x, y + BlancV, l)
                            If t.Detail <> "" Then Ecrire(g, t.Detail, _fDetail, Gris, x, y + BlancV + _hTexte, l, chemin:=True)
                        Case ColVolume
                            Ecrire(g, t.Volume, _fTexte, Encre, x, y + BlancV, l, StringAlignment.Far)
                        Case Else
                            Ecrire(g, TexteDeLaColonne(t, c), _fTexte, Encre, x, y + BlancV, l)
                    End Select
                    x += l
                Next
                y += h
            Next
            g.DrawLine(fort, zone.Left, y, zone.Right, y)

            ' ---- Pied de page
            Dim yPied As Single = zone.Bottom - _hPied + 4
            g.DrawLine(fin, zone.Left, yPied, zone.Right, yPied)
            Ecrire(g, "Slide  ·  Minuterie", _fPied, Gris, zone.Left - Marge, yPied + 4, zone.Width / 2)
            Ecrire(g, "Page " & (page + 1) & " / " & _debuts.Count, _fPied, Gris, zone.Left + zone.Width / 2, yPied + 4, zone.Width / 2 + Marge, StringAlignment.Far)
        End Using
    End Sub

End Class
