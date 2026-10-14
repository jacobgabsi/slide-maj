Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Vumetre des sessions audio Windows du programme (le niveau que montre le melangeur de volume),
''' lu DIRECTEMENT par les interfaces Windows, sans la bibliotheque CoreAudio.dll.
'''
''' Pourquoi : mesure au banc, les objets de CoreAudio.dll utilises par le lissage du son
'''   - fuyaient : +45 Mo par heure (4 h simulees : 8 -> 189 Mo), jamais rendus ;
'''   - et leur nettoyage automatique (finaliseur) peut LEVER UNE ERREUR ("Element introuvable",
'''     0x80070490), ce qui ferme le programme sans passer par la gestion d'erreurs de Slide.
''' Ici : un seul gestionnaire de sessions, cree une fois (et refait toutes les 5 minutes, au cas
''' ou la sortie audio par defaut change), et chaque objet Windows est libere explicitement.
''' A utiliser depuis un seul thread (celui de l'interface : le minuteur de NiveauSonore).
''' </summary>
Friend Module VumetreSession

    Private _gestionnaire As IAudioSessionManager2
    Private _gestionnaireCreeLe As Date = Date.MinValue
    Private ReadOnly _vumetres As New List(Of IAudioMeterInformation)

    ''' <summary>Relit la liste des sessions de ce programme (a appeler toutes les 2 s environ).</summary>
    Public Sub Rafraichir()
        Liberer(_vumetres)
        If _gestionnaire Is Nothing OrElse (Date.Now - _gestionnaireCreeLe).TotalMinutes >= 5 Then
            If _gestionnaire IsNot Nothing Then Marshal.ReleaseComObject(_gestionnaire) : _gestionnaire = Nothing
            _gestionnaire = CreerGestionnaire()
            _gestionnaireCreeLe = Date.Now
        End If
        If _gestionnaire Is Nothing Then Exit Sub

        Dim moi As UInteger = CUInt(Environment.ProcessId)
        Dim enumerateur As IAudioSessionEnumerator = Nothing
        Try
            Marshal.ThrowExceptionForHR(_gestionnaire.GetSessionEnumerator(enumerateur))
            Dim nombre As Integer
            Marshal.ThrowExceptionForHR(enumerateur.GetCount(nombre))
            For i As Integer = 0 To nombre - 1
                Dim controle As IAudioSessionControl2 = Nothing
                If enumerateur.GetSession(i, controle) <> 0 OrElse controle Is Nothing Then Continue For
                Dim pid As UInteger
                If controle.GetProcessId(pid) = 0 AndAlso pid = moi Then
                    ' Le meme objet COM expose aussi le vumetre (QueryInterface).
                    _vumetres.Add(DirectCast(controle, IAudioMeterInformation))
                Else
                    Marshal.ReleaseComObject(controle)
                End If
            Next
        Finally
            If enumerateur IsNot Nothing Then Marshal.ReleaseComObject(enumerateur)
        End Try
    End Sub

    ''' <summary>Crete la plus forte parmi les sessions de ce programme (0 a 1).</summary>
    Public Function Crete() As Double
        Dim maxi As Single = 0
        For Each v As IAudioMeterInformation In _vumetres
            Dim p As Single
            If v.GetPeakValue(p) = 0 AndAlso p > maxi Then maxi = p
        Next
        Return maxi
    End Function

    Private Function CreerGestionnaire() As IAudioSessionManager2
        Dim enumerateur As IMMDeviceEnumerator = Nothing
        Dim peripherique As IMMDevice = Nothing
        Try
            enumerateur = DirectCast(Activator.CreateInstance(Type.GetTypeFromCLSID(New Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"))), IMMDeviceEnumerator)
            If enumerateur.GetDefaultAudioEndpoint(0, 1, peripherique) <> 0 OrElse peripherique Is Nothing Then Return Nothing  ' eRender, eMultimedia
            Dim iid As Guid = GetType(IAudioSessionManager2).GUID
            Dim objet As Object = Nothing
            If peripherique.Activate(iid, 23, IntPtr.Zero, objet) <> 0 OrElse objet Is Nothing Then Return Nothing          ' CLSCTX_ALL
            Return DirectCast(objet, IAudioSessionManager2)
        Catch ex As Exception
            DiagLog("Vumetre : " & ex.Message)
            Return Nothing
        Finally
            If peripherique IsNot Nothing Then Marshal.ReleaseComObject(peripherique)
            If enumerateur IsNot Nothing Then Marshal.ReleaseComObject(enumerateur)
        End Try
    End Function

    Private Sub Liberer(liste As List(Of IAudioMeterInformation))
        For Each v As IAudioMeterInformation In liste
            Try
                Marshal.ReleaseComObject(v)
            Catch
            End Try
        Next
        liste.Clear()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Interfaces Windows (mmdeviceapi.h, audiopolicy.h, endpointvolume.h), dans l'ordre exact de
    ' leurs methodes. Seules celles qui servent ont une vraie signature ; les autres ne sont la
    ' que pour tenir leur place.
    ' ------------------------------------------------------------------------------------------

    <ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceEnumerator
        <PreserveSig> Function EnumAudioEndpoints(flux As Integer, etat As Integer, ByRef liste As IntPtr) As Integer
        <PreserveSig> Function GetDefaultAudioEndpoint(flux As Integer, role As Integer, ByRef peripherique As IMMDevice) As Integer
    End Interface

    <ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDevice
        <PreserveSig> Function Activate(ByRef iid As Guid, contexte As Integer, parametres As IntPtr,
                                        <MarshalAs(UnmanagedType.IUnknown)> ByRef interfaceObtenue As Object) As Integer
    End Interface

    <ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioSessionManager2
        <PreserveSig> Function GetAudioSessionControl(ByRef groupe As Guid, flags As Integer, ByRef controle As IntPtr) As Integer
        <PreserveSig> Function GetSimpleAudioVolume(ByRef groupe As Guid, flags As Integer, ByRef volume As IntPtr) As Integer
        <PreserveSig> Function GetSessionEnumerator(ByRef enumerateur As IAudioSessionEnumerator) As Integer
    End Interface

    <ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioSessionEnumerator
        <PreserveSig> Function GetCount(ByRef nombre As Integer) As Integer
        <PreserveSig> Function GetSession(index As Integer, ByRef controle As IAudioSessionControl2) As Integer
    End Interface

    <ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioSessionControl2
        ' IAudioSessionControl
        <PreserveSig> Function GetState(ByRef etat As Integer) As Integer
        <PreserveSig> Function GetDisplayName(ByRef nom As IntPtr) As Integer
        <PreserveSig> Function SetDisplayName(nom As IntPtr, ByRef contexte As Guid) As Integer
        <PreserveSig> Function GetIconPath(ByRef chemin As IntPtr) As Integer
        <PreserveSig> Function SetIconPath(chemin As IntPtr, ByRef contexte As Guid) As Integer
        <PreserveSig> Function GetGroupingParam(ByRef groupe As Guid) As Integer
        <PreserveSig> Function SetGroupingParam(ByRef groupe As Guid, ByRef contexte As Guid) As Integer
        <PreserveSig> Function RegisterAudioSessionNotification(rappel As IntPtr) As Integer
        <PreserveSig> Function UnregisterAudioSessionNotification(rappel As IntPtr) As Integer
        ' IAudioSessionControl2
        <PreserveSig> Function GetSessionIdentifier(ByRef identifiant As IntPtr) As Integer
        <PreserveSig> Function GetSessionInstanceIdentifier(ByRef identifiant As IntPtr) As Integer
        <PreserveSig> Function GetProcessId(ByRef pid As UInteger) As Integer
    End Interface

    <ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioMeterInformation
        <PreserveSig> Function GetPeakValue(ByRef crete As Single) As Integer
    End Interface

End Module
