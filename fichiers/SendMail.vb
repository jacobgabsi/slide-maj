''' <summary>
''' Envoi de mails par Gmail (CDO, smtp.gmail.com:465). Separe de Module3.vb le 14/10/2026 (demande de Jacob).
''' Le mot de passe d'application Google n'est PAS dans le programme (le depot GitHub des mises a jour est public et
''' publie le programme compile) : il est lu dans <programme>\data\gmail.txt. Le dossier data\ n'est jamais publie ni
''' ecrase par les mises a jour : on y depose ce fichier une seule fois, avec la premiere copie de Slide sur un PC.
''' </summary>
Module SendMail

    ''' <summary>Mot de passe d'application Gmail (premiere ligne de data\gmail.txt) ; exception claire s'il manque.</summary>
    Private Function MotDePasseGmail() As String
        Dim fichier As String = IO.Path.Combine(DossierReglages, "gmail.txt")
        If Not IO.File.Exists(fichier) Then
            Throw New InvalidOperationException("mot de passe Gmail absent : creer " & fichier & " (le mot de passe d'application Google sur une ligne)")
        End If
        Dim lignes As String() = IO.File.ReadAllLines(fichier)
        Dim mdp As String = If(lignes.Length > 0, lignes(0).Trim(), "")
        If mdp = "" Then Throw New InvalidOperationException("mot de passe Gmail vide dans " & fichier)
        Return mdp
    End Function

    ''' <summary>Image montree DANS le message, pas en piece jointe (demande de Jacob, 08/10/2026) :
    ''' le corps du message la designe par &lt;img src="cid:nom du fichier"&gt;.</summary>
    Sub MettreLImageDansLeCorps(message As Object, image As String)
        If image = "" Then Exit Sub
        Dim nom As String = IO.Path.GetFileName(image)
        Dim partie = message.AddRelatedBodyPart(image, nom, 0)              ' 0 = designee par son identifiant (cid)
        partie.Fields.Item("urn:schemas:mailheader:content-id") = "<" & nom & ">"
        partie.Fields.Item("urn:schemas:mailheader:content-disposition") = "inline; filename=""" & nom & """"
        partie.Fields.Update()
    End Sub

    ' fichier : piece jointe ("" : aucune). imageDuCorps : image montree dans le message ("" : aucune).
    Sub SendEmail(Dest As Object, sujet As Object, corpDuMessage As Object, fichier As Object, Optional imageDuCorps As String = "")

        iMsg = CreateObject("CDO.Message")
        iConf = CreateObject("CDO.Configuration")
        Flds = iConf.Fields

        ' send one copy with Google SMTP server (with autentication)
        schema = "http://schemas.microsoft.com/cdo/configuration/"
        Flds.Item(schema & "sendusing") = 2
        Flds.Item(schema & "smtpserver") = "smtp.gmail.com"
        Flds.Item(schema & "smtpserverport") = 465
        Flds.Item(schema & "smtpauthenticate") = 1
        Flds.Item(schema & "sendusername") = "jacobgabsi@gmail.com"
        Flds.Item(schema & "sendpassword") = MotDePasseGmail()
        Flds.Item(schema & "smtpusessl") = 1
        Flds.Update()

        'Dim att As Attachment
        'att = New Attachment(fichier)


        With iMsg
            .To = Dest
            ' Expediteur = le compte Gmail qui s'authentifie (08/10/2026). Avec une autre adresse, Gmail accepte
            ' le message puis le serveur du destinataire le rejette sans rien dire (constate sur 707PENNY le 15/09).
            .From = "jacobgabsi@gmail.com"
            .Subject = sujet
            '.TextBody = corpDuMessage
            .HTMLBody = corpDuMessage
            .Configuration = iConf
            If CStr(fichier) <> "" Then .AddAttachment(fichier)
            MettreLImageDansLeCorps(iMsg, imageDuCorps)
            .Send()
        End With

        iMsg = Nothing
        iConf = Nothing
        Flds = Nothing
        ' example Call SendEmail("alain@ajc.fr", "IP public à changé le " + Format(Now) + " ", Label1.Text)

        'bq3 = "<html><body><pre>" + "<span style = 'font-family:monospace;font-size:7pt'>" + bq + "</span>" + "</pre></body></html>"
        'Call SendEmail("compta2@ajc.fr", "BQ du " + Format(Now), bq3)

    End Sub
End Module
