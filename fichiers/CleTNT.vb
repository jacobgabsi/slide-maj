Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Etat de la cle TNT (tuner USB DVB-T) avant d'ouvrir une chaine.
'''
''' Quand la cle est debranchee ou en erreur dans Windows (ex. "code 10 : le peripherique ne peut
''' pas demarrer"), VLC attendait jusqu'au bout du delai de lecture puis notait seulement "VLC
''' Erreur lecture impossible" : on ne savait pas que c'etait la cle. On la verifie donc avant,
''' via le gestionnaire de peripheriques (SetupAPI + CfgMgr32, sans paquet supplementaire), et on
''' dit clairement quoi faire.
''' </summary>
Module CleTNT

    Public Enum EtatCle
        Ok
        Absente
        EnErreur
    End Enum

    ' Toute cle TNT (quelle que soit la marque) passe par l'architecture standard des tuners TV
    ' de Windows (BDA) et y enregistre ces interfaces. L'enregistrement reste connu de Windows
    ' meme quand la cle est en erreur (le pilote ne demarre plus) : on la retrouve donc aussi.
    Private ReadOnly InterfacesTuner As Guid() = {
        New Guid("71985F48-1CA1-11D3-9CC8-00C04F7971E0"),    ' KSCATEGORY_BDA_NETWORK_TUNER
        New Guid("FD0A5AF4-B41D-11D2-9C95-00C04F7971E0")}    ' KSCATEGORY_BDA_RECEIVER_COMPONENT

    ''' <summary>Dernier recours (cle jamais demarree, donc sans interface enregistree) : mots
    ''' reconnus dans le nom d'un peripherique USB.</summary>
    Private ReadOnly MotsDuTuner As String() = {"DVB", "TNT", "TUNER", "TV STICK", "DIGITAL TV", "ISDB", "ATSC", "BDA"}

    Private _dernierEtatSignale As EtatCle = EtatCle.Ok
    Private _dernierCodeSignale As Integer

    ''' <summary>Un peripherique trouve : nom, enumerateur ("USB"...) et etat.</summary>
    Private Structure Tuner
        Public Nom As String
        Public Usb As Boolean
        Public Probleme As Integer
        Public EnErreur As Boolean
        Public InstanceId As String
    End Structure

    ''' <summary>Les cles TNT USB presentes (Windows les garde dans la liste meme desactivees ou en erreur).</summary>
    Private Function ListerLesCles() As List(Of Tuner)
        Dim tuners As New List(Of Tuner)
        For Each g As Guid In InterfacesTuner
            AjouterLesTuners(tuners, g, Nothing)
        Next
        If tuners.Count = 0 Then AjouterLesTuners(tuners, Guid.Empty, MotsDuTuner)   ' dernier recours

        ' Seulement les cles USB (un tuner interne eventuel n'est pas concerne).
        Return tuners.Where(Function(t) t.Usb).ToList()
    End Function

    ''' <summary>La cle TNT USB a activer / reinitialiser (23/11/2026) : de preference la premiere qui n'est pas en erreur, sinon la premiere trouvee. Faux si aucune.
    ''' <paramref name="instanceId"/> = identifiant du peripherique pour pnputil ; <paramref name="codeProbleme"/> = code d'erreur Windows (22 = desactivee, 0 = aucun).</summary>
    Public Function TrouverLaCle(ByRef nom As String, ByRef instanceId As String, ByRef codeProbleme As Integer) As Boolean
        nom = "" : instanceId = "" : codeProbleme = 0
        Dim cles As List(Of Tuner) = ListerLesCles()
        If cles.Count = 0 Then Return False
        Dim cle As Tuner = cles.FirstOrDefault(Function(t) Not t.EnErreur)
        If cle.Nom Is Nothing Then cle = cles(0)
        nom = cle.Nom
        instanceId = If(cle.InstanceId, "")
        codeProbleme = cle.Probleme
        Return instanceId <> ""
    End Function

    Private Function ScriptReinit() As String
        Return IO.Path.Combine(IO.Path.GetTempPath(), "slide_tnt_reinit.cmd")
    End Function

    Private Function JournalReinit() As String
        Return IO.Path.Combine(IO.Path.GetTempPath(), "slide_tnt_reinit.log")
    End Function

    ''' <summary>Reinitialise la cle TNT (23/11/2026). Apres un scan, la cle restait bloquee jusqu'au redemarrage du PC : « pnputil /restart-device » rendait 3010 (« redemarrage necessaire »), c'est-a-dire que
    ''' Windows ne pouvait pas arreter la cle parce que le programme (Slide, par le pilote de la TV) la tenait encore ouverte. On lance donc, avec l'autorisation administrateur (UAC, demandee tout de suite), un petit
    ''' script qui ATTEND que Slide soit ferme, puis active, desactive et reactive la cle et relance la detection ; l'appelant ferme et relance Slide (RelanceDeNuit.RelancerMaintenant). Le resultat de chaque
    ''' commande est ecrit dans un journal que Slide recopie dans son propre journal au prochain demarrage (<see cref="JournaliserLaReinit"/>).
    ''' Rend faux si l'autorisation est refusee ou si le script n'a pas pu partir (rien n'est alors change et Slide ne doit pas se fermer).</summary>
    Public Function LancerLaReinitialisation(instanceId As String, ByRef message As String) As Boolean
        message = ""
        Dim pid As Integer
        Using moi As Process = Process.GetCurrentProcess()
            pid = moi.Id
        End Using
        Dim s As New System.Text.StringBuilder()
        s.AppendLine("@echo off")
        s.AppendLine("chcp 65001 >nul")
        s.AppendLine("set ""LOG=" & JournalReinit() & """")
        s.AppendLine("echo %date% %time% : attente de la fermeture de Slide (PID " & pid & ") > ""%LOG%""")
        s.AppendLine("set n=0")
        s.AppendLine(":attente")
        s.AppendLine("tasklist /FI ""PID eq " & pid & """ 2>nul | find """ & pid & """ >nul")
        s.AppendLine("if errorlevel 1 goto reset")
        s.AppendLine("set /a n+=1")
        s.AppendLine("if %n% GEQ 90 goto abandon")
        s.AppendLine("ping -n 2 127.0.0.1 >nul")
        s.AppendLine("goto attente")
        s.AppendLine(":abandon")
        s.AppendLine("echo Slide ne s'est pas ferme en 90 s : abandon >> ""%LOG%""")
        s.AppendLine("exit /b 1")
        s.AppendLine(":reset")
        s.AppendLine("echo %time% : Slide est ferme >> ""%LOG%""")
        s.AppendLine("ping -n 3 127.0.0.1 >nul")
        s.AppendLine("echo %time% : enable-device >> ""%LOG%""")
        s.AppendLine("pnputil /enable-device """ & instanceId & """ >> ""%LOG%"" 2>&1")
        s.AppendLine("ping -n 2 127.0.0.1 >nul")
        s.AppendLine("echo %time% : disable-device >> ""%LOG%""")
        s.AppendLine("pnputil /disable-device """ & instanceId & """ >> ""%LOG%"" 2>&1")
        s.AppendLine("ping -n 4 127.0.0.1 >nul")
        s.AppendLine("echo %time% : enable-device >> ""%LOG%""")
        s.AppendLine("pnputil /enable-device """ & instanceId & """ >> ""%LOG%"" 2>&1")
        s.AppendLine("ping -n 3 127.0.0.1 >nul")
        s.AppendLine("echo %time% : scan-devices >> ""%LOG%""")
        s.AppendLine("pnputil /scan-devices >> ""%LOG%"" 2>&1")
        s.AppendLine("echo %time% : termine >> ""%LOG%""")
        s.AppendLine("exit /b 0")
        Try
            IO.File.WriteAllText(ScriptReinit(), s.ToString(), New System.Text.UTF8Encoding(False))
            Dim psi As New ProcessStartInfo("cmd.exe", "/c """ & ScriptReinit() & """") With {
                .UseShellExecute = True, .Verb = "runas", .WindowStyle = ProcessWindowStyle.Hidden}
            Using p As Process = Process.Start(psi)
            End Using
        Catch ex As System.ComponentModel.Win32Exception
            message = If(ex.NativeErrorCode = 1223, "Autorisation administrateur refusée : rien n'a été changé.", "Réinitialisation impossible : " & ex.Message)
            Return False
        Catch ex As Exception
            message = "Réinitialisation impossible : " & ex.Message
            Return False
        End Try
        DiagLog("Cle TNT : reinitialisation lancee (script en attente de la fermeture de Slide) pour " & instanceId)
        Return True
    End Function

    ''' <summary>Au demarrage de Slide : recopie dans le journal de Slide le resultat de la derniere reinitialisation de la cle (lignes « Cle TNT reinit : ... »), puis efface ce petit journal.</summary>
    Public Sub JournaliserLaReinit()
        Try
            Dim f As String = JournalReinit()
            If Not IO.File.Exists(f) Then Exit Sub
            Dim lignes As String() = IO.File.ReadAllLines(f)
            For Each l As String In lignes
                If l.Trim() <> "" Then DiagLog("Cle TNT reinit : " & l.Trim())
            Next
            IO.File.Delete(f)
            Dim fs As String = ScriptReinit()
            If IO.File.Exists(fs) Then IO.File.Delete(fs)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Etat des cles TNT USB : Ok si au moins une fonctionne, EnErreur si toutes celles
    ''' trouvees sont en erreur, Absente si aucune. <paramref name="nom"/> = peripherique retenu,
    ''' <paramref name="codeProbleme"/> = code d'erreur Windows (0 si aucun).</summary>
    Public Function EtatDeLaCleTNT(ByRef nom As String, ByRef codeProbleme As Integer) As EtatCle
        nom = "" : codeProbleme = 0
        Dim tuners As List(Of Tuner) = ListerLesCles()
        If tuners.Count = 0 Then Return EtatCle.Absente

        Dim bon As Tuner = tuners.FirstOrDefault(Function(t) Not t.EnErreur)
        If bon.Nom IsNot Nothing Then
            nom = bon.Nom
            Return EtatCle.Ok
        End If
        nom = tuners(0).Nom
        codeProbleme = tuners(0).Probleme
        Return EtatCle.EnErreur
    End Function

    ''' <summary>Ajoute les peripheriques presents exposant l'interface <paramref name="interfaceTuner"/>
    ''' (ou, si Guid.Empty, tous les peripheriques dont le nom contient un des <paramref name="mots"/>).</summary>
    Private Sub AjouterLesTuners(tuners As List(Of Tuner), interfaceTuner As Guid, mots As String())
        Dim liste As IntPtr
        If interfaceTuner = Guid.Empty Then
            liste = SetupDiGetClassDevsW(IntPtr.Zero, Nothing, IntPtr.Zero, DIGCF_PRESENT Or DIGCF_ALLCLASSES)
        Else
            liste = SetupDiGetClassDevsGuid(interfaceTuner, Nothing, IntPtr.Zero, DIGCF_PRESENT Or DIGCF_DEVICEINTERFACE)
        End If
        If liste = New IntPtr(-1) Then Exit Sub
        Try
            Dim info As New SP_DEVINFO_DATA With {.cbSize = Marshal.SizeOf(GetType(SP_DEVINFO_DATA))}
            Dim i As Integer = 0
            While SetupDiEnumDeviceInfo(liste, i, info)
                i += 1
                Dim libelle As String = Propriete(liste, info, SPDRP_FRIENDLYNAME)
                If libelle = "" Then libelle = Propriete(liste, info, SPDRP_DEVICEDESC)
                If mots IsNot Nothing AndAlso Not ContientUnMot(libelle, mots) Then Continue While
                If tuners.Any(Function(t) t.Nom = libelle) Then Continue While   ' meme cle vue par 2 interfaces

                Dim enumerateur As String = Propriete(liste, info, SPDRP_ENUMERATOR_NAME)
                Dim statut, probleme As Integer
                Dim enErreur As Boolean = False
                If CM_Get_DevNode_Status(statut, probleme, info.DevInst, 0) = CR_SUCCESS Then
                    enErreur = probleme <> 0 OrElse (statut And DN_HAS_PROBLEM) <> 0
                Else
                    enErreur = True                                           ' pas de noeud actif
                End If
                Dim idTexte As New System.Text.StringBuilder(512)
                Dim instanceId As String = ""
                If CM_Get_Device_IDW(info.DevInst, idTexte, idTexte.Capacity, 0) = CR_SUCCESS Then instanceId = idTexte.ToString()
                tuners.Add(New Tuner With {.Nom = libelle, .Usb = enumerateur.StartsWith("USB", StringComparison.OrdinalIgnoreCase),
                                           .Probleme = probleme, .EnErreur = enErreur, .InstanceId = instanceId})
            End While
        Finally
            SetupDiDestroyDeviceInfoList(liste)
        End Try
    End Sub

    ''' <summary>
    ''' A appeler avant d'ouvrir une chaine TNT. Vrai si on peut y aller. Sinon : message a
    ''' l'ecran et dans les journaux (une seule fois par changement d'etat, pas a chaque tentative).
    ''' </summary>
    Public Function CleTNTPrete() As Boolean
        Dim nom As String = "", code As Integer
        Dim etat As EtatCle
        Try
            etat = EtatDeLaCleTNT(nom, code)
        Catch ex As Exception
            Return True                                            ' verification impossible : on essaie quand meme
        End Try

        If etat = EtatCle.Ok Then
            If _dernierEtatSignale <> EtatCle.Ok Then DiagLog("Cle TNT : de nouveau disponible (" & nom & ")")
            _dernierEtatSignale = EtatCle.Ok
            Return True
        End If

        Dim message As String
        If etat = EtatCle.Absente Then
            message = "Clé TNT introuvable" & vbCrLf & "Branchez la clé TNT"
        Else
            message = "Clé TNT en erreur (code " & code & ")" & vbCrLf & "Débranchez et rebranchez la clé TNT"
        End If

        If etat <> _dernierEtatSignale OrElse code <> _dernierCodeSignale Then
            DiagLog("Cle TNT : " & If(etat = EtatCle.Absente, "absente", "en erreur, code " & code & " (" & nom & ")") &
                    " - chaine TV non ouverte")
            SaveLogError(If(nom = "", "Cle TNT", nom), message.Replace(vbCrLf, " : "))
        End If
        _dernierEtatSignale = etat
        _dernierCodeSignale = code

        Try
            AfficheTitre(message, "*")
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function ContientUnMot(libelle As String, mots As String()) As Boolean
        If String.IsNullOrEmpty(libelle) Then Return False
        Dim l As String = libelle.ToUpperInvariant()
        For Each mot As String In mots
            If l.Contains(mot) Then Return True
        Next
        Return False
    End Function

    ''' <summary>Propriete texte d'un peripherique (tampon gere par .NET, taille exacte transmise).</summary>
    Private Function Propriete(liste As IntPtr, ByRef info As SP_DEVINFO_DATA, code As Integer) As String
        Dim tampon(1023) As Byte
        Dim typeValeur, taille As Integer
        If Not SetupDiGetDeviceRegistryPropertyW(liste, info, code, typeValeur, tampon, tampon.Length, taille) Then Return ""
        Dim octets As Integer = Math.Min(taille, tampon.Length)
        Return System.Text.Encoding.Unicode.GetString(tampon, 0, octets).TrimEnd(ChrW(0))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Win32
    ' ------------------------------------------------------------------------------------------

    <StructLayout(LayoutKind.Sequential)>
    Private Structure SP_DEVINFO_DATA
        Public cbSize As Integer
        Public ClassGuid As Guid
        Public DevInst As Integer
        Public Reserved As IntPtr
    End Structure

    Private Const DIGCF_PRESENT As Integer = &H2
    Private Const DIGCF_ALLCLASSES As Integer = &H4
    Private Const DIGCF_DEVICEINTERFACE As Integer = &H10
    Private Const SPDRP_DEVICEDESC As Integer = &H0
    Private Const SPDRP_FRIENDLYNAME As Integer = &HC
    Private Const SPDRP_ENUMERATOR_NAME As Integer = &H16
    Private Const CR_SUCCESS As Integer = 0
    Private Const DN_HAS_PROBLEM As Integer = &H400

    <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function SetupDiGetClassDevsW(classGuid As IntPtr, enumerator As String, parent As IntPtr, flags As Integer) As IntPtr
    End Function

    <DllImport("setupapi.dll", EntryPoint:="SetupDiGetClassDevsW", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function SetupDiGetClassDevsGuid(ByRef classGuid As Guid, enumerator As String, parent As IntPtr, flags As Integer) As IntPtr
    End Function

    <DllImport("setupapi.dll", SetLastError:=True)>
    Private Function SetupDiEnumDeviceInfo(liste As IntPtr, index As Integer, ByRef info As SP_DEVINFO_DATA) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function SetupDiGetDeviceRegistryPropertyW(liste As IntPtr, ByRef info As SP_DEVINFO_DATA, propriete As Integer,
                                                       ByRef typeValeur As Integer, tampon As Byte(), tailleTampon As Integer,
                                                       ByRef tailleRequise As Integer) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("setupapi.dll", SetLastError:=True)>
    Private Function SetupDiDestroyDeviceInfoList(liste As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("cfgmgr32.dll", CharSet:=CharSet.Unicode)>
    Private Function CM_Get_Device_IDW(devInst As Integer, buffer As System.Text.StringBuilder, bufferLen As Integer, flags As Integer) As Integer
    End Function

    <DllImport("cfgmgr32.dll")>
    Private Function CM_Get_DevNode_Status(ByRef statut As Integer, ByRef probleme As Integer, devInst As Integer, flags As Integer) As Integer
    End Function

End Module
