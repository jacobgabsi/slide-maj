Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D

''' <summary>
''' Repere "PIP" dessine dans la colonne Play de la minuterie (Form4), pour voir d'un coup d'oeil
''' qu'une tache s'affiche en PIP, dans quel coin et a quelle taille (demandes de Jacob, 06/10).
'''
''' Une pastille bleue, a gauche de la case : un petit ecran avec l'incrustation dans SON coin
''' (bas-droite, bas-gauche, haut-droite, haut-gauche), suivi de "PIP" et de la taille du PIP
''' ("PIP 28 %"). Le texte de la case (duree du film, ou plage "debut - fin") reste a droite,
''' toujours au meme endroit : la place de la pastille est reservee meme sur les lignes sans PIP.
''' Un PIP "Rester affiche" a une pastille ORANGE et porte en plus une petite epingle blanche
''' (la couleur seule ne se comprenait pas : "pourquoi l'icone est parfois jaune ?" ; Jacob a
''' voulu garder l'orange et ajouter l'epingle).
'''
''' Quand la case est trop etroite pour tout montrer, on retire dans cet ordre : le mot "PIP"
''' (le petit ecran le dit deja), puis un peu de la taille des lettres du texte, et en dernier
''' la taille du PIP.
'''
''' C'est un DESSIN : la valeur de la case n'est pas modifiee. Ajouter "PIP" au texte lui-meme
''' l'aurait enregistre dans la base (l'enregistrement relit le texte affiche) et la lecture du
''' film ne l'aurait plus compris.
''' </summary>
Public Module ReperePip

    Private ReadOnly Bleu As Color = Color.FromArgb(10, 132, 255)
    Private ReadOnly Orange As Color = Color.FromArgb(255, 149, 0)

    ''' <summary>Jusqu'ou les lettres du texte peuvent retrecir pour laisser sa place a la pastille.</summary>
    Private Const TexteReduitMini As Single = 0.78F

    ''' <param name="zone">Interieur de la case (ou du bouton).</param>
    ''' <param name="coin">Bdroite, Bgauche, Hdroite ou Hgauche.</param>
    ''' <param name="pourcent">Taille du PIP en % de l'image (0 = inconnue : pas affichee).</param>
    ''' <param name="texte">Texte de la case, dessine a droite de la pastille.</param>
    ''' <param name="resteAffiche">PIP "Rester affiche" : pastille orange, avec une epingle au bout.</param>
    ''' <param name="avecPastille">Faux pour une tache sans PIP : seul le texte est dessine, au meme
    ''' endroit que sur les lignes a pastille.</param>
    Public Sub Dessiner(g As Graphics, zone As Rectangle, police As Font, couleurTexte As Color,
                        coin As String, pourcent As Integer, texte As String, resteAffiche As Boolean,
                        Optional avecPastille As Boolean = True)
        If zone.Width < 12 OrElse zone.Height < 8 Then Exit Sub
        Dim ancienLissage As SmoothingMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.AntiAlias
        Try
            Using petite As New Font(police.FontFamily, police.Size * 0.78F, FontStyle.Bold, police.Unit)
                Dim drapeaux As TextFormatFlags = TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine Or TextFormatFlags.NoPrefix
                Dim infini As New Size(Integer.MaxValue, Integer.MaxValue)
                Dim tailleTexte As Size = If(texte = "", Size.Empty, TextRenderer.MeasureText(g, texte, police, infini, drapeaux))
                Dim hLettres As Integer = TextRenderer.MeasureText(g, "PIP", petite, infini, drapeaux).Height

                ' Le petit ecran : 16/10, a peu pres la hauteur des lettres.
                Dim hEcran As Integer = Math.Max(7, CInt(Math.Round(hLettres * 0.72)))
                Dim lEcran As Integer = CInt(Math.Round(hEcran * 1.55))
                Dim marge As Integer = Math.Max(3, hEcran \ 3)
                Dim hPastille As Integer = Math.Min(zone.Height, Math.Max(hEcran + 2 * marge, hLettres + 2))
                Dim ecart As Integer = marge + 1
                ' "Rester affiche" : une petite epingle blanche au bout de la pastille.
                Dim cEpingle As Integer = hEcran + 1
                Dim lEpingle As Integer = If(resteAffiche, cEpingle + marge, 0)

                ' Ce qu'on ecrit dans la pastille, du plus complet au plus court, et la largeur de la
                ' pastille dans chaque cas.
                Dim taille As String = If(pourcent > 0, pourcent & " %", "")
                Dim mots As String() = If(taille = "", {"PIP", ""}, {"PIP " & taille, taille, ""})
                Dim largeurs(mots.Length - 1) As Integer
                Dim lMots(mots.Length - 1) As Integer
                For i As Integer = 0 To mots.Length - 1
                    lMots(i) = If(mots(i) = "", 0, TextRenderer.MeasureText(g, mots(i), petite, infini, drapeaux).Width)
                    largeurs(i) = marge + lEcran + marge + If(mots(i) = "", 0, lMots(i) + marge) + lEpingle
                Next

                ' La place de la pastille la plus large ("PIP 100 %" et l'epingle) est reservee dans TOUTES
                ' les cases de la colonne, avec ou sans PIP : les pastilles commencent au meme endroit et
                ' les textes aussi ("la colonne Play n'est pas jolie car les elements ne sont pas bien
                ' alignes", Jacob, 09/10/2026 - le texte etait centre dans ce qui restait, donc a un
                ' endroit different sur chaque ligne).
                Dim place As Integer = marge + lEcran + marge + TextRenderer.MeasureText(g, "PIP 100 %", petite, infini, drapeaux).Width + marge + cEpingle + marge
                Dim aligne As Boolean = place + If(texte = "", 0, ecart + tailleTexte.Width) <= zone.Width

                ' Case trop etroite pour cet alignement : le premier qui tient, d'abord avec le texte a
                ' sa taille normale, puis avec le texte un peu reduit ; a defaut, le petit ecran seul.
                Dim choix As Integer = 0
                If Not aligne Then
                    choix = mots.Length - 1
                    Dim trouve As Boolean = False
                    For Each part As Single In {1.0F, TexteReduitMini}
                        For i As Integer = 0 To mots.Length - 1
                            Dim besoin As Integer = largeurs(i) + If(texte = "", 0, ecart + CInt(Math.Ceiling(tailleTexte.Width * part)))
                            If besoin <= zone.Width Then choix = i : trouve = True : Exit For
                        Next
                        If trouve Then Exit For
                    Next
                End If
                Dim mot As String = mots(choix)
                Dim lPastille As Integer = Math.Min(zone.Width, largeurs(choix))
                Dim pastille As New Rectangle(zone.Left, zone.Top + (zone.Height - hPastille) \ 2, lPastille, hPastille)

                If avecPastille Then
                    Using fond As New SolidBrush(If(resteAffiche, Orange, Bleu)), chemin As GraphicsPath = Arrondi(pastille, hPastille \ 3)
                        g.FillPath(fond, chemin)
                    End Using

                    ' Ecran (contour blanc) et incrustation (pleine) dans le coin du PIP.
                    Dim ecran As New Rectangle(pastille.Left + marge, pastille.Top + (hPastille - hEcran) \ 2, lEcran, hEcran)
                    Using trait As New Pen(Color.White, Math.Max(1.0F, hEcran / 9.0F))
                        g.DrawRectangle(trait, ecran)
                    End Using
                    Dim lIncr As Integer = Math.Max(3, CInt(Math.Round(lEcran * 0.48)))
                    Dim hIncr As Integer = Math.Max(2, CInt(Math.Round(hEcran * 0.48)))
                    Dim c As String = If(coin, "")
                    Dim x As Integer = If(c.EndsWith("gauche", StringComparison.OrdinalIgnoreCase), ecran.Left + 1, ecran.Right - lIncr)
                    Dim y As Integer = If(c.StartsWith("H", StringComparison.OrdinalIgnoreCase), ecran.Top + 1, ecran.Bottom - hIncr)
                    g.FillRectangle(Brushes.White, New Rectangle(x, y, lIncr, hIncr))

                    ' "PIP 28 %" (ou "28 %") en blanc.
                    If mot <> "" Then
                        Dim zoneMot As New Rectangle(ecran.Right + marge, pastille.Top, lMots(choix) + 1, hPastille)
                        TextRenderer.DrawText(g, mot, petite, zoneMot, Color.White, drapeaux Or TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)
                    End If

                    ' L'epingle : une tete ronde en haut a droite, la pointe vers le bas a gauche.
                    If resteAffiche AndAlso pastille.Width >= largeurs(choix) Then
                        Dim boite As New Rectangle(pastille.Right - marge - cEpingle, pastille.Top + (hPastille - cEpingle) \ 2, cEpingle, cEpingle)
                        Dim tete As Single = cEpingle * 0.58F
                        Using trait As New Pen(Color.White, Math.Max(1.4F, cEpingle / 6.0F)) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
                            g.DrawLine(trait, boite.Left + 0.5F, boite.Bottom - 0.5F, boite.Right - tete / 2, boite.Top + tete / 2)
                        End Using
                        g.FillEllipse(Brushes.White, boite.Right - tete, boite.Top, tete, tete)
                    End If
                End If

                ' Le texte de la case : a gauche, juste apres la place reservee (le meme depart sur toutes
                ' les lignes) ; case trop etroite : centre dans ce qui reste, comme avant.
                If texte <> "" Then
                    Dim reste As Rectangle
                    Dim dessin As TextFormatFlags = drapeaux Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis
                    If aligne Then
                        reste = New Rectangle(zone.Left + place + ecart, zone.Top, zone.Width - place - ecart, zone.Height)
                        dessin = dessin Or TextFormatFlags.Left
                    ElseIf avecPastille Then
                        reste = New Rectangle(pastille.Right + ecart, zone.Top, zone.Right - pastille.Right - ecart, zone.Height)
                        dessin = dessin Or TextFormatFlags.HorizontalCenter
                    Else
                        reste = zone
                        dessin = dessin Or TextFormatFlags.HorizontalCenter
                    End If
                    If reste.Width > 6 Then
                        If tailleTexte.Width <= reste.Width Then
                            TextRenderer.DrawText(g, texte, police, reste, couleurTexte, dessin)
                        Else
                            ' Une plage "debut - fin" est longue : lettres un peu plus petites plutot
                            ' que des points de suspension.
                            Dim echelle As Single = Math.Max(TexteReduitMini, CSng(reste.Width) / tailleTexte.Width * 0.98F)
                            Using reduite As New Font(police.FontFamily, police.Size * echelle, police.Style, police.Unit)
                                TextRenderer.DrawText(g, texte, reduite, reste, couleurTexte, dessin)
                            End Using
                        End If
                    End If
                End If
            End Using
        Finally
            g.SmoothingMode = ancienLissage
        End Try
    End Sub

    Private Function Arrondi(r As Rectangle, rayon As Integer) As GraphicsPath
        Dim chemin As New GraphicsPath()
        Dim d As Integer = Math.Max(2, Math.Min(Math.Min(r.Width, r.Height), rayon * 2))
        chemin.AddArc(r.Left, r.Top, d, d, 180, 90)
        chemin.AddArc(r.Right - d, r.Top, d, d, 270, 90)
        chemin.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        chemin.AddArc(r.Left, r.Bottom - d, d, d, 90, 90)
        chemin.CloseFigure()
        Return chemin
    End Function

End Module
