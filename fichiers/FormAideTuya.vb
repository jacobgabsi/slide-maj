''' <summary>
''' Aide du cadre Tuya (bouton « Aide ») : pas a pas, comment obtenir l'identifiant client, la cle secrete et l'UID sur la plateforme Tuya IoT, puis
''' les saisir dans Slide. Le texte est ecrit ici ; les lignes qui commencent par "#" sont des titres.
''' </summary>
Public Class FormAideTuya

    Friend Shared Sub Montrer(proprietaire As IWin32Window)
        Using f As New FormAideTuya()
            f.ShowDialog(proprietaire)
        End Using
    End Sub

    Private Sub FormAideTuya_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        Remplir()
    End Sub

    Private Sub ButtonPlateforme_Click(sender As Object, e As EventArgs) Handles ButtonPlateforme.Click
        Try
            Process.Start(New ProcessStartInfo("https://iot.tuya.com") With {.UseShellExecute = True})
        Catch ex As Exception
            DiagLog("Aide Tuya : ouverture du navigateur impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub Remplir()
        Dim titre As New Font(RichTextBoxAide.Font.FontFamily, RichTextBoxAide.Font.Size + 2, FontStyle.Bold)
        Dim normal As New Font(RichTextBoxAide.Font.FontFamily, RichTextBoxAide.Font.Size + 1, FontStyle.Regular)
        RichTextBoxAide.Clear()
        For Each ligne As String In Texte()
            If ligne.StartsWith("#") Then
                RichTextBoxAide.SelectionFont = titre
                RichTextBoxAide.AppendText(vbCrLf & ligne.TrimStart("#"c).Trim() & vbCrLf)
            Else
                RichTextBoxAide.SelectionFont = normal
                RichTextBoxAide.AppendText(ligne & vbCrLf)
            End If
        Next
        RichTextBoxAide.SelectionStart = 0
        RichTextBoxAide.ScrollToCaret()
    End Sub

    Private Shared Function Texte() As String()
        Return {
            "Slide pilote vos appareils Smart Life / Tuya par le cloud de Tuya. Pour cela il lui faut trois informations : un identifiant client (Client ID), une clé secrète (Client Secret) et l'identifiant de votre compte (UID). On les obtient gratuitement sur la plateforme développeur de Tuya, en une demi-heure environ, une seule fois. Les libellés exacts des menus de Tuya peuvent changer un peu : suivez l'idée.",
            "#Avant de commencer",
            "• Installez l'application Smart Life (ou Tuya Smart) sur votre téléphone, créez votre compte et ajoutez-y vos appareils. Vérifiez qu'ils fonctionnent depuis l'application.",
            "• Notez le pays choisi à la création du compte de l'application : il détermine le centre de données Tuya (pour la France, en général « Europe centrale » ; sinon « Europe de l'ouest »). Il faudra choisir le même plus loin.",
            "#Étape 1 : créer un compte développeur",
            "1. Cliquez sur le bouton « Ouvrir la plateforme Tuya IoT » en bas de cette fenêtre (https://iot.tuya.com).",
            "2. Cliquez sur « Sign up » (créer un compte), saisissez votre adresse e-mail, le code reçu, un mot de passe, puis choisissez votre pays. C'est un compte distinct de celui de l'application Smart Life, mais il peut avoir la même adresse.",
            "3. Connectez-vous, acceptez les conditions et indiquez si besoin que vous développez pour un usage personnel.",
            "#Étape 2 : créer un projet cloud",
            "1. Dans le menu de gauche, ouvrez « Cloud » puis « Cloud Project » puis « Project Management » (dans les versions plus anciennes de la plateforme : « Cloud » puis « Development »).",
            "2. Si la page le propose, cliquez sur « Upgrade IoT Core Plan » et souscrivez à « IoT Core » (offre d'essai gratuite) : sans lui, Tuya refuse les commandes.",
            "3. Cliquez sur « Create Cloud Project ». Project Name : par exemple « Slide ». Development Method : « Smart Home ». Le formulaire demande : « Project Name » (obligatoire), « Description » (facultative), « Industry » (obligatoire : choisissez « Smart Home » ou « Other »), « Development Method » (obligatoire : « Smart Home ») et « Data Center » (facultatif : mettez le centre de votre application, Europe centrale pour la France).",
            "4. Validez : une fenêtre propose les services à activer (« API Authorization » / « Cloud Services »). Dans « Cloud » > « Cloud Project » > « Cloud Services », vérifiez que ces services sont bien souscrits et autorisés pour votre projet : « Country and City Info », « Smart Home Basic Service » et « IoT Core » (ainsi que « Authorization Token Management » s'il est proposé). Le service « [Deprecate] Smart Home Scene Linkage » est abandonné par Tuya : inutile.",
            "#Étape 3 : récupérer l'identifiant client et la clé secrète",
            "1. Dans « Project Management », cliquez sur « Open Project » dans la colonne « Operation » de votre projet : la page du projet s'ouvre, onglet « Overview » (les onglets du projet sont : « Overview », « Authorization », « Service API », « Devices », « Message Service »).",
            "2. Dans « Authorization Key » vous voyez « Access ID / Client ID » et « Access Secret / Client Secret » (cliquez sur l'œil pour la révéler).",
            "3. Copiez-les : l'identifiant client est le « Client ID », la clé secrète est le « Client Secret ». Ne les donnez à personne : avec eux on peut commander vos appareils.",
            "#Étape 4 : relier votre application Smart Life au projet (et trouver l'UID)",
            "0. Un projet peut exister dans plusieurs centres de données, et la page du projet affiche en haut à droite un sélecteur de centre de données (par exemple « China Data Center » par défaut sur certains comptes). Choisissez-y d'abord le centre qui correspond à votre application (« Central Europe Data Center » pour la France) : sinon la liste des comptes liés et des appareils paraît vide (« 0 accounts », « No data found »).",
            "1. Dans la page de votre projet, ouvrez l'onglet « Devices » : il contient les sous-onglets « All Devices », « Link My App », « Link My WeChat Mini Program », « Link App Account », « Link PID », « Link SaaS ». Ouvrez « Link App Account », cliquez sur le bouton bleu « Add App Account » et choisissez « Tuya App Account Authorization ». Le tableau a les colonnes « App Account », « UID », « App Name », etc.",
            "2. Un code QR s'affiche. Sur le téléphone, ouvrez Smart Life (ou Tuya Smart) et scannez-le avec l'icône de scan de l'application (en haut à droite de l'écran d'accueil ou de l'onglet « Moi » ; Tuya ne précise pas l'écran exact et il varie selon les versions). Gardez le mode « Automatic Link » proposé par défaut, puis confirmez sur le téléphone.",
            "3. Le compte apparaît dans la liste « Link Tuya App Account », avec une colonne « UID » (une suite de lettres et de chiffres, du style eu1234…). Copiez cet UID : c'est l'identifiant du compte.",
            "4. Dans « All Devices », vous devez voir vos appareils. S'ils n'y sont pas, la liaison n'est pas faite : recommencez le point 2.",
            "#Étape 5 : saisir les informations dans Slide",
            "1. Réglages de Slide, onglet « Tuya », cadre « Tuya / Smart Life ».",
            "2. Collez l'identifiant client, la clé secrète et l'UID dans les trois champs. Choisissez le « Centre de données » identique à celui de votre projet (Europe centrale, etc.).",
            "3. Cliquez sur « Charger/Actualiser appareils » (la liste se charge aussi seule à l'ouverture des réglages quand les champs sont remplis). Vos appareils apparaissent dans le tableau, avec le code de leur interrupteur et leur état (ON ou OFF) ; le même bouton actualise la liste et les états.",
            "4. Cochez ceux que vous voulez piloter depuis la minuterie : chacun donne deux commandes, « Nom ON » et « Nom OFF ». Un appareil sans interrupteur (un robot, par exemple) ne peut pas être coché.",
            "5. Sur la ligne d'un appareil, cliquez sur le bouton « ON » ou « OFF » de la colonne Tester : il doit réagir, et son état change dans le tableau. Le message sous le bouton « Charger/Actualiser appareils » indique le résultat ou l'erreur.",
            "6. Facultatif, les groupes : « Nouveau… » crée un groupe (par exemple Salon) ; cochez à droite les appareils qui en font partie. Dans la minuterie, « Groupe Salon ON » ou « Groupe Salon OFF » agit sur tous.",
            "7. Plusieurs comptes Tuya (par exemple une maison et un bureau, avec des projets cloud différents) : en haut du cadre, « Nouveau... » crée un compte (donnez-lui un nom) ; saisissez ses identifiants et chargez ses appareils. Tous les appareils sont dans la même liste et peuvent être mélangés dans un groupe. Dans la minuterie, quand vous choisissez le type Tuya, Slide demande d'abord de quel compte est l'appareil de la ligne, puis ne propose que les appareils de ce compte (et les groupes).",
            "8. Validez par OK en bas des réglages : c'est ce qui enregistre tout.",
            "#Étape 6 : utiliser Tuya dans la minuterie",
            "1. Onglet « Minuterie » : ajoutez une ligne, choisissez le jour et l'heure.",
            "2. Type : « Tuya ». La case Action propose les commandes (« Nom ON », « Nom OFF » et celles des groupes).",
            "3. Un clic sur la petite case de test de la ligne envoie la commande tout de suite. À l'heure prévue, Slide l'envoie lui-même, sans ralentir la minuterie. Le journal garde la trace (« Tuya : ... envoyée » ou la raison de l'échec).",
            "#Si ça ne marche pas",
            "• « sign invalid » ou erreur de signature : le Client ID ou le Client Secret est faux (espace en trop, ou copie incomplète). Recopiez-les.",
            "• « permission deny » (code 1106) ou liste vide : l'application n'est pas liée au projet (étape 4), ou l'UID est faux.",
            "• « No permissions » (code 28841105) : un service n'est pas activé pour le projet. Dans « Cloud » > « Cloud Project » > « Cloud Services », vérifiez « Country and City Info », « Smart Home Basic Service » et « IoT Core » (et « Authorization Token Management » s'il est proposé) ; l'essai gratuit d'IoT Core peut demander à être prolongé à la main.",
            "• Erreur 2406, ou aucun appareil trouvé alors que la liaison est faite : le « Centre de données » choisi dans Slide n'est pas celui du projet (ou le projet est trop ancien). Essayez l'autre centre d'Europe.",
            "• Un appareil est « hors ligne » : Tuya refuse la commande. Vérifiez son alimentation et le Wi-Fi depuis l'application.",
            "• Sur un autre PC : la clé secrète est chiffrée pour le PC où elle a été saisie. Ressaisissez-la, ou utilisez l'export / import des paramètres (clic droit > Paramètres), qui la transporte et la rechiffre.",
            "#À retenir",
            "Les identifiants restent dans vos réglages sur votre PC (la clé secrète est chiffrée par Windows) : ils ne sont jamais publiés avec Slide."}
    End Function

End Class
