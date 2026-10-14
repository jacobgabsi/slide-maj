Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.RegularExpressions

''' <summary>
''' Tableau « Tuya » de la minuterie (demande de Jacob, 29/10/2026) : entre le tableau des taches actives et celui des inactives, il regroupe les commandes Tuya, et d'autres taches
''' si on les y met. Un interrupteur ON/OFF fait tourner la minuterie SANS les lignes de ce tableau (essais, absence...).
'''
''' Une ligne est dans ce tableau si son type est « Tuya » ou si ses reglages portent la marque « !/Groupe:Tuya » (ajoutee quand on y colle une autre tache). Les lignes restent des
''' lignes ordinaires de la minuterie (meme base, meme enregistrement) : seul leur affichage change, voir Form4.TableauTuya.vb.
''' L'interrupteur est propre a chaque poste (data\settings\TuyaMinuterieOff.txt : present = OFF), jamais publie.
''' </summary>
Friend Module TableauTuya

    Friend Const CleGroupe As String = "Groupe"
    Friend Const ValeurGroupe As String = "Tuya"

    Private _enMarche As Boolean? = Nothing

    Private Function FichierArret() As String
        Return Path.Combine(Convert.ToString(AppPath), "data", "settings", "TuyaMinuterieOff.txt")
    End Function

    ''' <summary>Vrai (ON) : les lignes du tableau Tuya sont jouees par la minuterie. Faux (OFF) : elles sont ignorees.</summary>
    Friend Function EstEnMarche() As Boolean
        If _enMarche.HasValue Then Return _enMarche.Value
        Try
            _enMarche = Not File.Exists(FichierArret())
        Catch ex As Exception
            _enMarche = True
        End Try
        Return _enMarche.Value
    End Function

    Friend Sub FixerMarche(enMarche As Boolean)
        _enMarche = enMarche
        Try
            If enMarche Then
                If File.Exists(FichierArret()) Then File.Delete(FichierArret())
            Else
                Directory.CreateDirectory(Path.GetDirectoryName(FichierArret()))
                File.WriteAllText(FichierArret(), "OFF depuis le " & Now.ToString("dd/MM/yyyy HH:mm"))
            End If
            DiagLog("Minuterie : tableau Tuya " & If(enMarche, "ON", "OFF (ses lignes ne sont plus jouees)"))
        Catch ex As Exception
            DiagLog("Minuterie : interrupteur du tableau Tuya impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private ReadOnly MotifMarque As New Regex("!/" & CleGroupe & ":([^!]*)", RegexOptions.IgnoreCase)
    Friend Const ValeurPrincipale As String = "Principale"

    ''' <summary>La ligne appartient-elle a la minuterie optionnelle ? Sa marque « !/Groupe:Tuya » (ou « Principale » : restee dans le tableau principal) decide ; sans marque, seules les lignes
    ''' de type Tuya y sont (les lignes d'avant ce tableau).</summary>
    Friend Function EstDuTableau(type As Object, argument As Object) As Boolean
        Dim a As String = TryCast(argument, String)
        If a IsNot Nothing Then
            Dim m As Match = MotifMarque.Match(a)
            If m.Success Then Return String.Equals(m.Groups(1).Value.Trim(), ValeurGroupe, StringComparison.OrdinalIgnoreCase)
        End If
        Return String.Equals(Convert.ToString(type).Trim(), TuyaSlide.TypeMinuterie, StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>Cette ligne de la minuterie en memoire est ignoree : elle est de la minuterie optionnelle, qui est sur OFF. Vaut pour tout ce qui lit la minuterie (jeu, annonce « a suivre dans... », enchainement, relance).</summary>
    Friend Function Ignoree(l As Minuterie) As Boolean
        Return Not EstEnMarche() AndAlso EstDuTableau(l.Type, l.Argument)
    End Function

    ''' <summary>Les reglages d'une ligne avec la marque de la minuterie optionnelle (<paramref name="dedans"/>) ou, sinon, sans marque
    ''' (« Principale » pour une ligne de type Tuya, qui sans cela y retournerait).</summary>
    Friend Function ArgumentAvecMarque(argument As String, dedans As Boolean, Optional typeTuya As Boolean = False) As String
        Dim a As String = MotifMarque.Replace(If(argument, ""), "")
        If dedans Then
            a &= "!/" & CleGroupe & ":" & ValeurGroupe
        ElseIf typeTuya Then
            a &= "!/" & CleGroupe & ":" & ValeurPrincipale
        End If
        Return a
    End Function

End Module