Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Lie a Slide un programme que Slide a fait demarrer : quand Slide s'arrete - par son menu, par
''' Echap, par une relance, par "Arreter" dans Visual Studio ou par le Gestionnaire des taches -
''' Windows arrete ce programme aussitot.
'''
''' Fait pour le navigateur du Deezer integre (msedgewebview2, un programme a part), le 08/10/2026 :
''' Jacob : "quand je quitte Slide, la musique Deezer continue un bon moment". Slide se termine par
''' "End", sans rien fermer ; le navigateur ne s'apercevait de sa disparition qu'apres un moment,
''' et la musique continuait d'ici la.
'''
''' Moyen : un "job" de Windows regle sur "tout arreter quand le job est ferme". Slide garde le job
''' ouvert tant qu'il vit ; Windows le ferme a sa place quand il disparait, quelle qu'en soit la cause.
''' </summary>
Friend Module ProcessusLie

    Private _job As IntPtr = IntPtr.Zero
    Private ReadOnly _verrou As New Object

    ''' <summary>Lie le programme numero <paramref name="numero"/> a la vie de Slide. Renvoie False
    ''' (et l'ecrit au journal) si Windows refuse.</summary>
    Friend Function Lier(numero As Integer, quoi As String) As Boolean
        Try
            SyncLock _verrou
                If _job = IntPtr.Zero Then
                    Dim job As IntPtr = CreateJobObject(IntPtr.Zero, Nothing)
                    If job = IntPtr.Zero Then
                        DiagLog(quoi & " : lien avec Slide impossible (job, erreur " & Marshal.GetLastWin32Error() & ")")
                        Return False
                    End If
                    Dim reglage As New JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
                    reglage.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                    If Not SetInformationJobObject(job, JobObjectExtendedLimitInformation, reglage, CUInt(Marshal.SizeOf(Of JOBOBJECT_EXTENDED_LIMIT_INFORMATION)())) Then
                        DiagLog(quoi & " : lien avec Slide impossible (reglage, erreur " & Marshal.GetLastWin32Error() & ")")
                        CloseHandle(job)
                        Return False
                    End If
                    _job = job
                End If
            End SyncLock
            Using programme As Process = Process.GetProcessById(numero)
                If Not AssignProcessToJobObject(_job, programme.Handle) Then
                    DiagLog(quoi & " : lien avec Slide refuse par Windows (erreur " & Marshal.GetLastWin32Error() & ")")
                    Return False
                End If
            End Using
            Return True
        Catch ex As Exception
            DiagLog(quoi & " : lien avec Slide impossible (" & ex.Message & ")")
            Return False
        End Try
    End Function

    Private Const JobObjectExtendedLimitInformation As Integer = 9
    Private Const JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE As UInteger = &H2000UI

    <StructLayout(LayoutKind.Sequential)>
    Private Structure JOBOBJECT_BASIC_LIMIT_INFORMATION
        Public PerProcessUserTimeLimit As Long
        Public PerJobUserTimeLimit As Long
        Public LimitFlags As UInteger
        Public MinimumWorkingSetSize As UIntPtr
        Public MaximumWorkingSetSize As UIntPtr
        Public ActiveProcessLimit As UInteger
        Public Affinity As UIntPtr
        Public PriorityClass As UInteger
        Public SchedulingClass As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure IO_COUNTERS
        Public ReadOperationCount As ULong
        Public WriteOperationCount As ULong
        Public OtherOperationCount As ULong
        Public ReadTransferCount As ULong
        Public WriteTransferCount As ULong
        Public OtherTransferCount As ULong
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        Public BasicLimitInformation As JOBOBJECT_BASIC_LIMIT_INFORMATION
        Public IoInfo As IO_COUNTERS
        Public ProcessMemoryLimit As UIntPtr
        Public JobMemoryLimit As UIntPtr
        Public PeakProcessMemoryUsed As UIntPtr
        Public PeakJobMemoryUsed As UIntPtr
    End Structure

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Function CreateJobObject(attributs As IntPtr, nom As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function SetInformationJobObject(job As IntPtr, classe As Integer, ByRef reglage As JOBOBJECT_EXTENDED_LIMIT_INFORMATION, taille As UInteger) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function AssignProcessToJobObject(job As IntPtr, programme As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CloseHandle(poignee As IntPtr) As Boolean
    End Function

End Module
