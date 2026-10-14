Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Suivi de la memoire de Slide dans le journal (data\MigrationDiag.log), une ligne par minute :
'''
'''   Memoire : prive 612 Mo (+3) | gere 48 Mo | travail 540 Mo | handles 1830 | GDI 412 | USER 96 | fils 71 | mode PHOTO
'''
''' Pourquoi (06/10) : pendant une session d'essais, la memoire de Slide est passee de 560 Mo a
''' pres de 1,5 Go en quelques minutes, puis est redescendue ; sur l'autre PC, Slide s'est arrete
''' net apres 9 et 13 heures. Vue de l'exterieur, impossible de dire ce qui grossit. Avec cette
''' trace, le journal d'une nuit montre si la memoire monte sans redescendre (fuite), a quel
''' rythme, et ce que Slide faisait a ce moment (les lignes voisines du journal).
'''   - "prive"  : memoire reservee par le programme (le chiffre qui compte) ; entre parentheses,
'''                l'ecart avec la minute precedente ;
'''   - "gere"   : la part du code .NET ; si "prive" monte et pas "gere", c'est VLC ou Windows ;
'''   - handles, GDI, USER : objets de Windows ; ils ne doivent pas monter sans fin non plus.
''' Lecture seule : rien n'est libere ni force ici.
''' </summary>
Friend Module SuiviMemoire

    Private _minuteur As System.Windows.Forms.Timer
    Private _dernierPrive As Long = -1

    ''' <summary>A appeler une fois, depuis le fil de l'interface, au demarrage.</summary>
    Public Sub Demarrer()
        If _minuteur IsNot Nothing Then Exit Sub
        _minuteur = New System.Windows.Forms.Timer With {.Interval = 60000}
        AddHandler _minuteur.Tick, Sub() Noter()
        _minuteur.Start()
        Noter()
    End Sub

    Private Sub Noter()
        Try
            Using p As Process = Process.GetCurrentProcess()
                Dim prive As Long = p.PrivateMemorySize64 \ 1048576
                Dim ecart As String = If(_dernierPrive < 0, "", " (" & If(prive >= _dernierPrive, "+", "") & (prive - _dernierPrive) & ")")
                _dernierPrive = prive
                Dim mode As String = ""
                Try
                    mode = Diffusion.Mode
                Catch ex As Exception
                End Try
                DiagLog("Memoire : prive " & prive & " Mo" & ecart &
                        " | gere " & (GC.GetTotalMemory(False) \ 1048576) & " Mo" &
                        " | travail " & (p.WorkingSet64 \ 1048576) & " Mo" &
                        " | handles " & p.HandleCount &
                        " | GDI " & GetGuiResources(p.Handle, 0) &
                        " | USER " & GetGuiResources(p.Handle, 1) &
                        " | fils " & p.Threads.Count &
                        " | mode " & mode)
            End Using
        Catch ex As Exception
            ' le suivi ne doit jamais gener le programme
        End Try
    End Sub

    <DllImport("user32.dll")>
    Private Function GetGuiResources(processus As IntPtr, quoi As UInteger) As UInteger
    End Function

End Module
