Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Security.Cryptography
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq

''' <summary>
''' Deezer DANS Slide (demande de Jacob, 07/10 : "la source audio Deezer est geree en externe,
''' peux-tu l'integrer a Slide en interne").
'''
''' Avant : Slide ouvrait l'application Deezer de Windows, attendait sa fenetre, la mettait au
''' premier plan, y cliquait et lui envoyait des touches (espace, Maj+Droite, L). Cela prenait le
''' clavier et la souris, dependait de la place de la fenetre, et Slide ne savait ni mettre Deezer
''' en pause pendant une video ou la TV, ni regler son volume.
'''
''' Maintenant : le lecteur web officiel de Deezer (www.deezer.com) tourne dans une fenetre de
''' Slide (FormDeezer, un navigateur WebView2), le plus souvent hors de l'ecran. Il n'existe pas
''' d'API Deezer qui laisse un autre programme jouer les morceaux entiers (l'API publique ne donne
''' que des extraits de 30 secondes) ; mais le lecteur web a sa propre interface de commande,
''' l'objet "dzPlayer" de la page : control.play / pause / nextSong / prevSong / setVolume,
''' isPlaying, getSongTitle, getArtistName... (verifie sur le site le 07/10/2026). Slide l'appelle
''' directement : ni clic ni touche. Si cet objet venait a changer, on se rabat sur les boutons de
''' la barre du lecteur (reperes par leur "data-testid").
'''
''' Connexion au compte Deezer : avec l'identifiant et le mot de passe des reglages (onglet Audio),
''' Slide remplit lui-meme la page de connexion ; sinon, ou si Deezer demande une verification
''' (code, "je ne suis pas un robot"), la fenetre est montree pour la finir a la main. Elle n'est
''' a faire qu'une fois : le navigateur la retient sur ce PC, dans %LocalAppData%\Slide\Deezer.
''' Le mot de passe est enregistre CHIFFRE par Windows pour l'utilisateur en cours (Proteger) ;
''' il n'est jamais ecrit dans le journal.
'''
''' La musique jouee est celle que Deezer reprend (la derniere file d'attente) ou celle qu'on
''' choisit dans la fenetre (reglages, onglet Audio, "Ouvrir Deezer...").
'''
''' Branche sur la musique du diaporama : MusiqueVLC.MusiqueJouer / MusiquePause / volume passent
''' par ici quand la source est Deezer, donc tout ce qui met la musique en pause (video avec son,
''' TV, PIP avec son, ecran noir) ou la reprend vaut aussi pour Deezer.
'''
''' Reglage "Deezer integre a Slide" (My.Settings.DeezerInterne) : decoche, on retrouve
''' l'ancienne facon (application Deezer externe).
''' </summary>
Friend Module MusiqueDeezer

    Friend Const Accueil As String = "https://www.deezer.com/"
    ''' <summary>Mot a mettre a la place de l'adresse d'une playlist pour lancer le Flow de Deezer
    ''' (demande de Jacob, 08/10 : "comment lancer la playlist flow"). Le Flow n'a pas d'adresse ;
    ''' le lecteur web le lance par dzPlayer.play({type:'user', id:numero du compte, radio:true}),
    ''' comme son propre bouton (lu dans son code le 08/10/2026). Le numero du compte est
    ''' demande a Deezer par la page elle-meme (deezer.getUserData, ce qu'elle fait a son chargement).</summary>
    Friend Const Flow As String = "flow"
    ''' <summary>Mot qui designe la playlist "Coups de coeur" du compte (08/10 : mot "favoris" du champ
    ''' Playlist, tache "Deezer Favoris", commande a distance "Deezer.Loved"). Son numero est propre a chaque
    ''' compte : Deezer le donne avec le compte (LOVEDTRACKS_ID de deezer.getUserData), et le
    ''' lecteur la lance par dzPlayer.play({type:'playlist', id:numero}), comme son propre lien
    ''' "/user/me/tracks" (lu dans son code le 08/10/2026).</summary>
    Friend Const Favoris As String = "favoris"

    ''' <summary>Vrai pour ce qui se lance par l'interface du lecteur, sans page a ouvrir : le
    ''' Flow, ses variantes, et les Coups de coeur.</summary>
    Friend Function SansPage(adresse As String) As Boolean
        Return EstUnFlow(adresse) OrElse adresse = Favoris
    End Function
    Private Const DestinataireDuTitre As String = "jacob@ajc.fr"    ' (demande de Jacob, 08/10/2026 ; avant : alain@ajc.fr)
    Private Const AttenteDeLaConnexion As Double = 20               ' secondes laissees a la connexion automatique

    ''' <summary>Ce que la page dit d'elle-meme (lu toutes les deux secondes).</summary>
    Friend Class EtatDuLecteur
        Public Charge As Boolean        ' la page est chargee
        Public Pret As Boolean          ' le lecteur de la page est charge
        Public Connecte As Boolean      ' un compte Deezer est connecte
        Public Formulaire As Boolean    ' c'est la page de connexion (identifiant, mot de passe)
        Public Joue As Boolean
        Public Titre As String = ""
        Public Artiste As String = ""
        Public Album As String = ""
        Public Pochette As String = ""  ' adresse de l'image de l'album (pour le mail du titre)
        Public Volume As Double = -1    ' 0 a 1
        Public Muet As Boolean          ' le son du lecteur de Deezer est coupe (son bouton haut-parleur)
        Public Position As Double       ' secondes ecoulees dans le morceau
        Public Duree As Double          ' duree du morceau en secondes (0 : inconnue)
        Public Morceaux As Integer      ' dans la file d'attente
        Public Contexte As String = ""  ' d'ou vient la file d'attente : "playlist_page:53362031", "album_page:302127"...
        Public Adresse As String = ""
        Public Favoris As String = ""   ' numero de la playlist "Coups de coeur" du compte ("" tant que Deezer ne l'a pas donne)
        Public Compte As String = ""    ' ce que Deezer repond quand on lui demande le compte : "oui", "non" (session perdue), "" pas encore demande
        Public Courriel As String = ""  ' adresse e-mail du compte connecte, telle que Deezer la donne ("" si inconnue)
        Public Page As String = ""      ' numero du chargement de la page (change quand elle est rechargee)
    End Class

    ''' <summary>Petit programme installe dans chaque page de Deezer : il donne a Slide des
    ''' commandes simples, quel que soit le moyen (interface dzPlayer, sinon boutons).</summary>
    Friend Const ScriptDAide As String =
"(function(){
  if (window.__slide) return;
  var S = window.__slide = {};
  S.page = String(Date.now()) + Math.random().toString(36).slice(2, 8);     // change a chaque chargement de page
  function lecteur(){ var d = window.dzPlayer; return (d && d.control) ? d : null; }
  function clic(id){ var b = document.querySelector('[data-testid=""' + id + '""]'); if (b) { b.click(); return true; } return false; }
  S.jouer = function(){
    try { var d = lecteur(); if (d && d.control.play) { if (!d.isPlaying()) d.control.play(); return 'interface'; } } catch (e) {}
    return clic('play_button_play') ? 'bouton' : 'rien';
  };
  S.pause = function(){
    try { var d = lecteur(); if (d && d.control.pause) { if (d.isPlaying()) d.control.pause(); return 'interface'; } } catch (e) {}
    return clic('play_button_pause') ? 'bouton' : 'rien';
  };
  S.suivant = function(){
    try { var d = lecteur(); if (d && d.control.nextSong) { d.control.nextSong(); return 'interface'; } } catch (e) {}
    return clic('next_track_button') ? 'bouton' : 'rien';
  };
  S.precedent = function(){
    try { var d = lecteur(); if (d && d.control.prevSong) { d.control.prevSong(); return 'interface'; } } catch (e) {}
    return clic('previous_track_button') ? 'bouton' : 'rien';
  };
  S.volume = function(v){
    try { var d = lecteur(); if (d && d.control.setVolume) { d.control.setVolume(v); return 'interface'; } } catch (e) {}
    return 'rien';
  };
  S.flow = function(variante){
    var d = lecteur();
    if (!d || !d.play) return 'rien';
    if (variante) {
      try { d.play({ type: 'multi_flow', id: variante, radio: true, context: { TYPE: 'sidebar_flow_config', ID: variante } }); return 'flow ' + variante; } catch (e) { return 'erreur ' + e; }
    }
    function lancer(id){
      try { d.play({ type: 'user', id: id, radio: true, context: { TYPE: 'sidebar_user_radio', ID: id } }); return 'flow ' + id; } catch (e) { return 'erreur ' + e; }
    }
    if (S.utilisateur) return lancer(S.utilisateur);
    try { if (d.getRadioType && d.getRadioType() === 'user' && d.getRadioId()) { S.utilisateur = d.getRadioId(); return lancer(S.utilisateur); } } catch (e) {}
    if (S.sansUtilisateur) return 'sans compte';
    if (!S.rechercheEnCours) {
      S.rechercheEnCours = true;
      fetch('/ajax/gw-light.php?method=deezer.getUserData&input=3&api_version=1.0&api_token=', { credentials: 'include' })
        .then(function(r){ return r.json(); })
        .then(function(j){
          var u = j && j.results && j.results.USER && j.results.USER.USER_ID;
          S.rechercheEnCours = false;
          if (u && String(u) !== '0') { S.utilisateur = u; lancer(u); } else { S.sansUtilisateur = true; }
        })
        .catch(function(){ S.rechercheEnCours = false; });
    }
    return 'recherche';
  };
  S.oublierLaLectureAuto = function(){
    try { if (/[?&]autoplay=/.test(location.search)) { history.replaceState(history.state, '', location.pathname + location.hash); return 'retire'; } } catch (e) {}
    return 'interface';
  };
  S.relancer = function(){
    try { var d = lecteur(); if (d) { d.control.pause(); setTimeout(function(){ try { d.control.play(); } catch (e) {} }, 800); return 'relance'; } } catch (e) {}
    return 'rien';
  };
  S.retablirLeSon = function(){
    try { var d = lecteur(); if (d && d.isMuted && d.isMuted() && d.control.mute) { d.control.mute(false); return d.isMuted() ? 'encore coupe' : 'retabli'; } } catch (e) {}
    return 'interface';
  };
  S.favori = function(){
    // Le coeur du morceau QUI JOUE : celui de la barre du lecteur, pas ceux des listes de la page.
    // Son repere dit s'il est deja aime (add_to_favorite_button_on) ou non (..._off) : lu dans le
    // code du lecteur le 08/10/2026. Deja aime : on n'y touche pas (un clic l'enleverait des Coups de coeur).
    var barre = document.querySelector('[data-testid=""miniplayer_container""]') || document;
    if (barre.querySelector('[data-testid=""add_to_favorite_button_on""]')) return 'deja';
    var b = barre.querySelector('[data-testid=""add_to_favorite_button_off""]');
    if (b) { b.click(); return 'bouton'; }
    return 'rien';
  };
  S.favoriNon = function(){
    // L'inverse (commande Deezer.Favo_Non) : on ne clique que si le morceau qui joue EST aime.
    var barre = document.querySelector('[data-testid=""miniplayer_container""]') || document;
    if (barre.querySelector('[data-testid=""add_to_favorite_button_off""]')) return 'deja';
    var b = barre.querySelector('[data-testid=""add_to_favorite_button_on""]');
    if (b) { b.click(); return 'bouton'; }
    return 'rien';
  };
  S.ecouterLaPage = function(){
    var b = document.querySelector('[data-testid=""toolbar""] button[data-testid$=""play_small-button""], button[data-testid$=""play_small-button""]');
    if (!b) {
      var zone = document.querySelector('[data-testid=""toolbar""]') || document.querySelector('[data-testid=""masthead""]');
      if (zone) b = zone.querySelector('button[data-testid*=""play""]');
    }
    if (!b) {
      var tous = document.querySelectorAll('button');
      for (var i = 0; i < tous.length && !b; i++) {
        if (tous[i].closest('[data-testid=""miniplayer_container""]')) continue;
        var nom = ((tous[i].getAttribute('aria-label') || '') + ' ' + (tous[i].textContent || '')).trim().toLowerCase();
        if (/^(écouter|ecouter|listen|lecture|lire)/.test(nom)) b = tous[i];
      }
    }
    if (b) { b.click(); return 'bouton ' + (b.getAttribute('data-testid') || b.getAttribute('aria-label') || '').slice(0, 40); }
    return 'rien';
  };
  function lienDeConnexion(){ return document.querySelector('a[href*=""account.deezer.com""][href*=""login""]'); }
  function champDuMotDePasse(){ return document.querySelector('[data-testid=""password-field""], input[type=""password""]'); }
  S.versLaConnexion = function(){
    var a = lienDeConnexion();
    if (a) { location.href = a.href; return 'lien'; }
    // Session perdue : la page garde son allure de page connectee, sans lien de connexion.
    // Rechargee, elle redevient celle d'un visiteur, avec son lien.
    if (S.compte === 'non') { location.reload(); return 'rechargee'; }
    return 'rien';
  };
  S.compte = '';
  S.favoris = '';                                                    // numero de la playlist des Coups de coeur du compte
  S.jouerLesFavoris = function(){
    var d = lecteur();
    if (!d || !d.play) return 'rien';
    function lancer(){
      try { d.play({ type: 'playlist', id: S.favoris, context: { TYPE: 'playlist_page', ID: S.favoris } }); return 'favoris ' + S.favoris; } catch (e) { return 'erreur ' + e; }
    }
    if (S.favoris) return lancer();
    if (S.compte === 'non') return 'sans compte';
    if (!S.rechercheDesFavoris) {
      S.rechercheDesFavoris = true;
      fetch('/ajax/gw-light.php?method=deezer.getUserData&input=3&api_version=1.0&api_token=', { credentials: 'include' })
        .then(function(r){ return r.json(); })
        .then(function(j){
          S.rechercheDesFavoris = false;
          var U = j && j.results && j.results.USER;
          if (U && U.USER_ID && String(U.USER_ID) !== '0' && U.LOVEDTRACKS_ID) { S.utilisateur = U.USER_ID; S.favoris = String(U.LOVEDTRACKS_ID); S.favorisPanne = ''; lancer(); }
          else S.favorisPanne = !U ? 'reponse de Deezer sans compte' : (String(U.USER_ID) === '0' ? 'aucun compte connecte' : 'ce compte ne donne pas le numero de ses Coups de coeur');
        })
        .catch(function(){ S.rechercheDesFavoris = false; S.favorisPanne = 'Deezer ne repond pas'; });
    }
    return 'recherche' + (S.favorisPanne ? ' (' + S.favorisPanne + ')' : '');
  };
  S.verifierLeCompte = function(){
    if (S.verificationEnCours) return 'interface';
    S.verificationEnCours = true;
    fetch('/ajax/gw-light.php?method=deezer.getUserData&input=3&api_version=1.0&api_token=', { credentials: 'include' })
      .then(function(r){ return r.json(); })
      .then(function(j){
        S.verificationEnCours = false;
        var u = j && j.results && j.results.USER ? j.results.USER.USER_ID : undefined;
        if (u === undefined || u === null) return;                 // reponse inattendue : on ne conclut rien
        if (String(u) === '0') { S.compte = 'non'; S.courriel = ''; } else { S.compte = 'oui'; S.utilisateur = u; S.courriel = String(j.results.USER.EMAIL || '').toLowerCase(); if (j.results.USER.LOVEDTRACKS_ID) S.favoris = String(j.results.USER.LOVEDTRACKS_ID); }
      })
      .catch(function(){ S.verificationEnCours = false; });        // pas de reseau : on ne conclut rien
    return 'interface';
  };
  S.connexion = function(identifiant, motDePasse){
    var m = document.querySelector('[data-testid=""email-field""], input[type=""email""], input[name=""email""]');
    var p = champDuMotDePasse();
    if (!m || !p) return 'champs';
    var valeur = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value');
    function mettre(champ, texte){
      champ.focus();
      valeur.set.call(champ, texte);
      champ.dispatchEvent(new Event('input', { bubbles: true }));
      champ.dispatchEvent(new Event('change', { bubbles: true }));
    }
    mettre(m, identifiant);
    mettre(p, motDePasse);
    var b = document.querySelector('[data-testid=""login-button""]') || (p.form ? p.form.querySelector('button[type=""submit""]') : null);
    if (!b) return 'bouton';
    setTimeout(function(){ b.click(); }, 400);
    return 'envoye';
  };
  function pochette(d){
    // Adresse de la pochette du morceau qui joue, en 500 points (pour le mail Deezer.SongNameByMail).
    // getCover() donne le numero de l'image ; sinon, l'image affichee dans la barre du lecteur.
    var h = ''; try { h = d.getCover() || ''; } catch (x) {}
    if (/^[0-9a-f]{32}$/.test(h)) return 'https://cdn-images.dzcdn.net/images/cover/' + h + '/500x500-000000-80-0-0.jpg';
    var i = document.querySelector('[data-testid=""miniplayer_container""] img');
    return i && i.src ? i.src.replace(/\/\d+x\d+-/, '/500x500-') : '';
  }
  S.etat = function(){
    var e = { charge: document.readyState === 'complete', pret: false, connecte: true, formulaire: false, joue: false,
              titre: '', artiste: '', album: '', pochette: '', volume: -1, muet: false, position: 0, duree: 0, morceaux: 0, contexte: '', adresse: location.href, page: S.page };
    try { e.formulaire = !!champDuMotDePasse(); } catch (x) {}
    try { e.connecte = location.hostname.indexOf('account.') !== 0 && !lienDeConnexion() && S.compte !== 'non'; } catch (x) {}
    e.compte = S.compte;
    e.favoris = S.favoris;
    e.courriel = S.courriel || '';
    try {
      var d = lecteur();
      if (d) {
        e.pret = !!d.playerLoaded;
        e.joue = !!d.isPlaying();
        e.titre = d.getSongTitle() || '';
        e.artiste = d.getArtistName() || '';
        e.album = d.getAlbumTitle() || '';
        try { e.pochette = pochette(d); } catch (y) {}
        e.volume = d.getVolume();
        try { e.muet = !!d.isMuted(); } catch (y) {}
        try { e.position = d.getPosition() || 0; } catch (y) {}
        try {
          var du = 0;
          if (d.getDuration) du = d.getDuration();
          if (!du && d.getRemainingTime) du = e.position + d.getRemainingTime();
          if (!du && d.getCurrentSong) { var cs = d.getCurrentSong(); du = (cs && (cs.DURATION || cs.duration)) || 0; }
          e.duree = Number(du) || 0;
        } catch (y) {}
        e.morceaux = d.getNbSongs();
        try { var c = d.getContext(); if (c) e.contexte = (c.TYPE || '') + ':' + (c.ID || ''); } catch (y) {}
      }
    } catch (x) {}
    return JSON.stringify(e);
  };
})();"

    Private _fenetre As FormDeezer
    Private _etat As New EtatDuLecteur
    Private _demande As String = ""             ' "jouer" ou "pause" : ce que Slide attend encore du lecteur
    Private _demandeDepuis As Date              ' quand cette demande a ete faite
    Private _volume As Integer = -1             ' 0 a 100, -1 : celui de Deezer
    Private _sansLecteurDepuis As Date          ' depuis quand la page n'a pas de lecteur (Date.MinValue : elle en a un)
    Private _jouait As Boolean                  ' etat du lecteur a la lecture precedente (pour le journal)
    Private _positionVue As Double = -1         ' position du morceau a la lecture precedente
    Private _immobileDepuis As Date             ' depuis quand elle n'avance plus alors que le lecteur dit jouer
    Private _immobileDit As Boolean
    Private _deconnexionDite As Boolean         ' la perte de la session a deja ete ecrite dans le journal
    Private _compteVerifieLe As Date            ' derniere demande du compte a Deezer
    Private _relanceTentee As Boolean           ' pause puis lecture deja essaye pour cette lecture figee
    Private _voulu As Boolean                   ' Slide veut que la musique joue (dernier ordre : jouer)
    Private _pageVue As String = ""             ' numero du chargement de la page lu la derniere fois
    Private _pretAvant As Boolean               ' le lecteur etait pret et connecte a la lecture precedente
    Private _rechargementVoulu As Boolean       ' bouton "Accueil Deezer" : la page est rechargee expres
    Private _sonRetabliLe As Date
    Private _connexionDemandee As Boolean       ' la fenetre a deja ete montree pour se connecter
    Private _identifiantsEnvoyes As Boolean     ' la page de connexion a deja ete remplie (une seule fois)
    Private _envoiLe As Date
    Private _versConnexionLe As Date
    Private _choixDemande As Boolean            ' la fenetre a deja ete montree pour choisir une musique
    Private _dernierTitre As String = ""
    Private _lectureEnCours As Boolean
    Private _playlistLancee As String = ""      ' adresse de la playlist deja lancee dans ce lecteur
    Private _playlistEnCours As String = ""     ' adresse de celle qu'on est en train de lancer ("" : aucune)
    Private _playlistEssais As Integer
    Private WithEvents Minuteur As New Timer With {.Interval = 2000}

    ''' <summary>Son coupe dans le navigateur (pour les bancs d'essai : aucun son).</summary>
    Friend Muet As Boolean
    ''' <summary>Pour les bancs d'essai seulement : faire comme si un compte etait connecte (sans
    ''' compte, le site joue des extraits, ce qui suffit pour essayer le lancement d'une playlist).</summary>
    Friend ExtraitsPermis As Boolean

    ' ------------------------------------------------------------------------------------------
    ' Playlist a lancer (demande de Jacob, 07/10 : "peut-on lancer une playlist determinee")
    '
    ' Reglage "Playlist" de l'onglet Audio (My.Settings.DeezerPlaylist), ou celle de la tache
    ' "Music Source / Deezer" de la minuterie (DeezerPlaylistParMinuterie) : l'adresse Deezer
    ' d'une playlist, d'un album... Mesure du 07/10/2026 sur le site : ouvrir cette adresse avec
    ' "?autoplay=true" lance la lecture de la playlist ; a defaut, le bouton "Ecouter" de la page.
    ' Vide : Deezer joue ce qu'il a dans sa file d'attente, comme avant.
    ' Elle est lancee UNE fois (au premier "jouer" ou quand elle change) ; ensuite pause et
    ' reprise se font dans la file d'attente, la ou la musique en etait.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Adresse a ouvrir pour la playlist voulue ("" : aucune).</summary>
    Friend Function PlaylistVoulue() As String
        Return AdresseDeezer(If(DeezerPlaylistParMinuterie <> "", DeezerPlaylistParMinuterie, DeezerPlaylist))
    End Function

    ''' <summary>Verifie que <paramref name="texte"/> est bien une adresse de Deezer et la met en
    ''' forme (https, genre et numero : "https://www.deezer.com/playlist/53362031"). Renvoie ""
    ''' pour tout autre site : le lecteur n'ouvre que Deezer.</summary>
    Friend Function AdresseDeezer(texte As String) As String
        Dim t As String = If(texte, "").Trim()
        If t = "" Then Return ""
        ' Le Flow n'a pas d'adresse : le mot "flow" le designe, et "flow:nom" une de ses variantes.
        If t.ToLowerInvariant() = Flow Then Return Flow
        ' Les Coups de coeur du compte n'ont pas d'adresse commune a tous : le mot "favoris" les designe.
        Select Case t.ToLowerInvariant()
            Case "favoris", "favori", "favorites", "favorite", "coups de coeur", "coups de cœur", "loved" : Return Favoris
        End Select
        If t.ToLowerInvariant().StartsWith(Flow & ":") AndAlso t.Substring(5).Trim() <> "" Then Return Flow & ":" & t.Substring(5).Trim()
        If Not t.Contains("://") Then t = "https://" & t
        Dim u As Uri = Nothing
        If Not Uri.TryCreate(t, UriKind.Absolute, u) OrElse (u.Scheme <> Uri.UriSchemeHttps AndAlso u.Scheme <> Uri.UriSchemeHttp) Then Return ""
        Dim hote As String = u.Host.ToLowerInvariant()
        If Not (hote = "deezer.com" OrElse hote.EndsWith(".deezer.com") OrElse hote = "deezer.page.link") Then Return ""
        Dim m As Text.RegularExpressions.Match = Text.RegularExpressions.Regex.Match(
            u.AbsolutePath, "/(playlist|album|artist|show|radio|smarttracklist)/(\d+)", Text.RegularExpressions.RegexOptions.IgnoreCase)
        If m.Success AndAlso (hote = "deezer.com" OrElse hote = "www.deezer.com") Then
            Return "https://www.deezer.com/" & m.Groups(1).Value.ToLowerInvariant() & "/" & m.Groups(2).Value
        End If
        Return "https://" & u.Host & u.PathAndQuery               ' (lien de partage : tel quel)
    End Function

    ''' <summary>Vrai pour "flow" (le Flow) et "flow:nom" (une variante du Flow : Pop, Triste...).</summary>
    Friend Function EstUnFlow(adresse As String) As Boolean
        Return adresse = Flow OrElse adresse.StartsWith(Flow & ":")
    End Function

    ''' <summary>Nom que Deezer donne a la variante du Flow ("" pour le Flow de base).</summary>
    Friend Function VarianteDuFlow(adresse As String) As String
        Return If(adresse.StartsWith(Flow & ":"), adresse.Substring(5), "")
    End Function

    ''' <summary>
    ''' Bouton "Jouer ceci au demarrage" de la fenetre Deezer (demande de Jacob, 08/10 : "dans
    ''' flow il y a plusieurs options, comme Pop, Triste..."). Deezer ne publie pas le nom
    ''' interne de ces variantes : Slide l'apprend de ce que le lecteur joue. L'origine de la
    ''' file d'attente (dzPlayer.getContext : genre et numero) dit si c'est une playlist, un
    ''' album, le Flow ou une de ses variantes ; elle devient la playlist des reglages.
    ''' Renvoie le message a montrer.
    ''' </summary>
    Friend Function RetenirLaMusiqueEnCours() As String
        Dim origine As String = _etat.Contexte
        Dim p As Integer = origine.LastIndexOf(":"c)
        If Not _etat.Pret OrElse p <= 0 OrElse p = origine.Length - 1 Then Return "Rien à retenir pour l'instant : lancez d'abord une musique dans Deezer."
        Dim genre As String = origine.Substring(0, p).ToLowerInvariant(), numero As String = origine.Substring(p + 1)
        Dim retenue As String, nom As String
        If genre.Contains("flow_config") OrElse genre.Contains("multi_flow") Then
            retenue = Flow & ":" & numero : nom = "le Flow « " & numero & " »"
        ElseIf genre.Contains("user_radio") OrElse genre.Contains("flow") Then
            retenue = Flow : nom = "le Flow"
        ElseIf genre.Contains("playlist") AndAlso numero = _etat.Favoris Then
            retenue = Favoris : nom = "vos Coups de cœur"
        ElseIf genre.Contains("playlist") AndAlso IsNumeric(numero) Then
            retenue = "https://www.deezer.com/playlist/" & numero : nom = "cette playlist"
        ElseIf genre.Contains("album") AndAlso IsNumeric(numero) Then
            retenue = "https://www.deezer.com/album/" & numero : nom = "cet album"
        Else
            DiagLog("Deezer : musique non retenue (file d'attente : " & origine & ")")
            Return "Cette musique ne peut pas être retenue (origine : " & origine & "). Lancez une playlist, un album ou un Flow, puis recommencez."
        End If
        DeezerPlaylist = retenue
        My.Settings.DeezerPlaylist = retenue
        My.Settings.Save()
        _playlistLancee = AdresseDeezer(retenue)                ' elle joue deja : pas de relance
        _origineLancee = origine
        _playlistEnCours = ""
        ' Les reglages sont peut-etre ouverts : leur champ Playlist suit (sinon leur bouton OK remettrait l'ancienne).
        Try
            For Each reglages As Form4 In Application.OpenForms.OfType(Of Form4)().ToList()
                reglages.TextBoxDeezerPlaylist.Text = retenue
            Next
        Catch ex As Exception
        End Try
        DiagLog("Deezer : musique retenue pour le demarrage : " & retenue & " (file d'attente : " & origine & ")")
        Return "Retenu : Slide lancera " & nom & " au démarrage (réglages, onglet Audio, champ Playlist : " & retenue & ")."
    End Function

    ''' <summary>Numero de la playlist (ou de l'album) dans son adresse, "" si on ne le voit pas.</summary>
    Private Function NumeroDe(adresse As String) As String
        ' (Seulement les adresses mises en forme par AdresseDeezer. Avant, le lien de partage
        '  "link.deezer.com/s/34Cfz..." donnait le numero "34" : la playlist n'etait jamais reconnue,
        '  et Slide rappuyait sur "Ecouter", ce qui la mettait en pause. Vu chez Jacob le 08/10.)
        Dim m As Text.RegularExpressions.Match = Text.RegularExpressions.Regex.Match(adresse, "^https://www\.deezer\.com/(?:playlist|album|artist|show|radio|smarttracklist)/(\d+)$")
        Return If(m.Success, m.Groups(1).Value, "")
    End Function

    ''' <summary>Vrai si la file d'attente du lecteur vient de cette adresse.</summary>
    Private Function EstEnCours(adresse As String) As Boolean
        If adresse = Favoris Then Return _etat.Favoris <> "" AndAlso _etat.Contexte.EndsWith(":" & _etat.Favoris)
        If adresse = Flow Then Return _etat.Contexte.Contains("user_radio") AndAlso Not _etat.Contexte.Contains("multi_flow")
        If EstUnFlow(adresse) Then Return _etat.Contexte.EndsWith(":" & VarianteDuFlow(adresse)) AndAlso (_etat.Contexte.Contains("flow_config") OrElse _etat.Contexte.Contains("multi_flow"))
        Dim numero As String = NumeroDe(adresse)
        Return numero <> "" AndAlso _etat.Contexte.EndsWith(":" & numero)
    End Function

    ''' <summary>La playlist voulue si elle reste a lancer, "" sinon.</summary>
    Private Function PlaylistALancer() As String
        Dim voulue As String = PlaylistVoulue()
        Return If(voulue <> "" AndAlso voulue <> _playlistLancee, voulue, "")
    End Function

    ''' <summary>Une tache de la minuterie (re)lance Deezer : si le lecteur joue autre chose que
    ''' la playlist voulue (choisie a la main dans la fenetre...), elle sera relancee.</summary>
    Friend Sub RevenirALaPlaylist()
        Dim voulue As String = PlaylistVoulue()
        If voulue <> "" AndAlso _playlistEnCours = "" AndAlso Not EstEnCours(voulue) Then _playlistLancee = ""
    End Sub

    ''' <summary>Page ouverte au lancement du lecteur : celle de la playlist voulue s'il y en a une
    ''' (SANS lecture automatique : rien ne joue tant que Slide ne le demande pas), sinon l'accueil
    ''' de Deezer. Avant (08/10) : toujours l'accueil, puis la page de la playlist, soit deux
    ''' chargements ; la musique ne partait que 14 secondes apres le lancement de Slide.</summary>
    Friend Function PremiereAdresse() As String
        Dim voulue As String = PlaylistVoulue()
        Return If(voulue = "" OrElse SansPage(voulue), Accueil, voulue)
    End Function

    ''' <summary>Vrai si la page affichee par le lecteur est deja celle de cette adresse.</summary>
    Private Function SurLaPage(adresse As String) As Boolean
        Dim page As String = _etat.Adresse
        If page.Contains("?") Then page = page.Substring(0, page.IndexOf("?"c))
        Return AdresseDeezer(page).TrimEnd("/"c) = adresse.TrimEnd("/"c)
    End Function

    Private Sub LancerLaPlaylist(f As FormDeezer, adresse As String)
        _playlistEnCours = adresse
        _favorisParLaPage = False
        _playlistEssais = 0
        _playlistDepuis = Now
        _playlistClics = 0
        _contexteAvant = _etat.Contexte
        If adresse = Favoris Then
            DiagLog("Deezer : lancement des Coups de coeur (file d'attente actuelle : " & If(_etat.Contexte = "", "?", _etat.Contexte) & ")")
            _playlistClics = 1
            Commander("jouerLesFavoris")
        ElseIf EstUnFlow(adresse) Then
            ' Le Flow se lance par l'interface du lecteur, depuis n'importe quelle page de Deezer.
            DiagLog("Deezer : lancement du Flow" & If(VarianteDuFlow(adresse) = "", "", " " & VarianteDuFlow(adresse)) & " (file d'attente actuelle : " & If(_etat.Contexte = "", "?", _etat.Contexte) & ")")
            _playlistClics = 1
            Commander("flow")
        ElseIf SurLaPage(adresse) Then
            ' Sa page est deja la (ouverte au lancement) : il suffit d'appuyer sur "Ecouter".
            DiagLog("Deezer : lancement de la playlist " & adresse & " (sa page est deja ouverte)")
            _playlistClics = 1
            Commander("ecouterLaPage")
        Else
            DiagLog("Deezer : lancement de la playlist " & adresse & " (file d'attente actuelle : " & If(_etat.Contexte = "", "?", _etat.Contexte) & ")")
            f.Aller(adresse & If(adresse.Contains("?"), "&", "?") & "autoplay=true")
        End If
    End Sub

    Private _contexteAvant As String = ""       ' d'ou venait la file d'attente avant le lancement
    ''' <summary>Page des Coups de coeur du compte connecte, quel qu'il soit (celle que Jacob avait mise dans
    ''' le champ Playlist le 08/10/2026, et qui se lancait bien). Sert quand l'interface du lecteur ne
    ''' lance pas les Coups de coeur.</summary>
    Friend PageDesFavoris As String = "https://www.deezer.com/fr/library/loved"
    Private _favorisParLaPage As Boolean        ' le second moyen est en cours
    Private _origineLancee As String = ""       ' origine de la file d'attente une fois la playlist lancee

    ''' <summary>
    ''' Bouton OK des reglages quand la case "La lancer quand je clique sur OK" est DECOCHEE : la
    ''' musique en cours continue, meme si le champ Playlist a change (sans cela, la nouvelle
    ''' playlist partirait a la prochaine photo, quand Slide redemande la musique). La playlist
    ''' du champ servira au prochain lancement de Slide.
    ''' </summary>
    Friend Sub GarderLaMusiqueEnCours()
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed OrElse Not _etat.Pret Then Exit Sub
        If _playlistEnCours <> "" Then Exit Sub                  ' un lancement est en cours (demarrage) : on le laisse finir
        Dim voulue As String = PlaylistVoulue()
        If voulue <> _playlistLancee Then
            DiagLog("Deezer : OK des reglages, case decochee -> la musique en cours continue (playlist du champ : " & If(voulue = "", "aucune", voulue) & ", pour le prochain lancement)")
            _playlistLancee = voulue
            _origineLancee = ""
        End If
    End Sub

    ''' <summary>
    ''' Bouton OK des reglages, case "La lancer quand je clique sur OK" cochee (demande de Jacob,
    ''' 08/10 : "quand je clique sur OK dans Form4, il doit jouer la playlist Deezer indiquee dans
    ''' le champ Playlist"). Si le lecteur joue autre chose que cette playlist (musique choisie a
    ''' la main dans la fenetre Deezer, playlist d'une tache de la minuterie...), elle est
    ''' lancee ; si c'est deja elle, la musique continue ou elle en est, sans repartir du debut.
    ''' Champ vide : rien ne change.
    ''' </summary>
    ''' <param name="jouerMaintenant">Faux si Slide tient la musique en pause (film, TV, PIP
    ''' avec son...) : la playlist partira quand la musique reprendra.</param>
    Friend Sub AppliquerLaPlaylistDesReglages(jouerMaintenant As Boolean)
        If Not Actif Then Exit Sub
        _pauseDemandee = False                                  ' OK des reglages : la musique est voulue
        DeezerPlaylistParMinuterie = ""                         ' le champ des reglages reprend la main
        Dim voulue As String = PlaylistVoulue()
        If voulue = "" Then Exit Sub
        Dim dejaElle As Boolean = _playlistEnCours = voulue OrElse EstEnCours(voulue) OrElse
                                  (voulue = _playlistLancee AndAlso _origineLancee <> "" AndAlso _etat.Contexte = _origineLancee)
        If Not dejaElle Then
            DiagLog("Deezer : OK des reglages -> la playlist " & voulue & " sera lancee (file d'attente actuelle : " & If(_etat.Contexte = "", "?", _etat.Contexte) & ")")
            _playlistLancee = ""
            _playlistEnCours = ""
        End If
        If jouerMaintenant Then Jouer()
    End Sub
    Private _playlistDepuis As Date             ' debut du lancement (page prete)
    Private _playlistClics As Integer           ' appuis sur "Ecouter" deja faits

    ''' <summary>
    ''' La page de la playlist est chargee : est-ce bien ELLE qui joue ? On le voit a l'origine
    ''' de la file d'attente (son numero ; pour une adresse sans numero, comme les "Coups de
    ''' coeur" .../library/loved, au fait que cette origine a change). Sinon, le bouton "Ecouter"
    ''' de la page, trois fois ; au bout de 24 secondes, on se contente de la file de Deezer.
    ''' (Vu chez Jacob le 08/10 : pour .../library/loved, "?autoplay=true" n'avait fait que
    ''' reprendre l'ancienne file, et on s'en contentait a tort.)
    ''' </summary>
    Private Sub SuivreLaPlaylist(e As EtatDuLecteur)
        _playlistEssais += 1
        If _playlistEssais = 1 Then _playlistDepuis = Now       ' la page vient d'etre prete
        Dim attente As Double = (Now - _playlistDepuis).TotalSeconds
        ' La file d'attente a-t-elle change depuis le lancement ?
        Dim changee As Boolean = e.Contexte <> "" AndAlso e.Contexte <> _contexteAvant
        ' (Coups de coeur lances par leur page : on les reconnait, comme une adresse sans numero, au
        '  fait que l'origine de la file d'attente a change.)
        Dim parLaPage As Boolean = _playlistEnCours = Favoris AndAlso _favorisParLaPage
        Dim reconnaissable As Boolean = Not parLaPage AndAlso (SansPage(_playlistEnCours) OrElse NumeroDe(_playlistEnCours) <> "")
        Dim laBonne As Boolean = If(reconnaissable, EstEnCours(_playlistEnCours), changee)
        ' (Adresse reconnaissable mais origine inattendue : on s'en contente si la file a bien
        '  change apres notre appui sur "Ecouter".)
        If e.Joue AndAlso (laBonne OrElse (changee AndAlso _playlistClics > 0)) Then
            DiagLog("Deezer : playlist lancee (" & _playlistEnCours & ", " & e.Morceaux & " morceaux, file d'attente : " & e.Contexte & ")")
            _playlistLancee = _playlistEnCours
            _origineLancee = e.Contexte
            _playlistEnCours = ""
            _demande = ""
            _choixDemande = False
            ' "?autoplay=true" ne reste pas dans l'adresse de la page : si Deezer redessinait
            ' la page, il relancerait la playlist depuis le debut.
            Commander("oublierLaLectureAuto")
        ElseIf _playlistEnCours = Favoris AndAlso Not _favorisParLaPage AndAlso attente >= 6 AndAlso Not changee Then
            ' L'interface du lecteur n'a pas lance les Coups de coeur (vu sur l'autre poste de Jacob le
            ' 08/10/2026, avec un autre compte que le sien) : second moyen, leur page et son bouton
            ' "Ecouter", qui ne depend pas du numero de la playlist.
            _favorisParLaPage = True
            DiagLog("Deezer : les Coups de coeur ne partent pas par l'interface du lecteur (file d'attente : " & If(e.Contexte = "", "?", e.Contexte) & ") -> par leur page, " & PageDesFavoris)
            _playlistEssais = 0
            _playlistDepuis = Now
            _playlistClics = 0
            If _fenetre IsNot Nothing AndAlso Not _fenetre.IsDisposed Then _fenetre.Aller(PageDesFavoris & "?autoplay=true")
        ElseIf ((_playlistClics = 0 AndAlso attente >= 1.5) OrElse (_playlistClics = 1 AndAlso attente >= 5) OrElse (_playlistClics = 2 AndAlso attente >= 10)) AndAlso
               (Not e.Joue OrElse Not changee) Then
            ' La lecture automatique n'a pas pris (ou pas pour cette page) : le bouton "Ecouter".
            ' Jamais quand la nouvelle file joue deja : ce bouton la mettrait en pause.
            _playlistClics += 1
            Commander(If(_playlistEnCours = Favoris AndAlso Not _favorisParLaPage, "jouerLesFavoris", If(EstUnFlow(_playlistEnCours), "flow", "ecouterLaPage")))
        ElseIf attente >= 24 Then
            DiagLog("Deezer : la playlist ne demarre pas (" & _playlistEnCours & ", joue = " & e.Joue & ", file d'attente : " & If(e.Contexte = "", "?", e.Contexte) &
                    ") -> lecture de la file d'attente de Deezer")
            _playlistLancee = _playlistEnCours                  ' pas de nouvel essai en boucle
            _playlistEnCours = ""
            _demandeDepuis = Now
            Commander("oublierLaLectureAuto")
        End If
    End Sub

    ''' <summary>Note ce que Slide attend du lecteur ; tant qu'il l'attend, l'etat est lu deux fois
    ''' par seconde au lieu d'une fois toutes les deux secondes (la musique part plus vite).</summary>
    ''' <summary>
    ''' Reconnexion (question de Jacob, 08/10 : "Slide sait-il gerer la reconnexion ?"). La page
    ''' de Deezer vient d'etre rechargee - reseau revenu, reconnexion au compte, page rechargee
    ''' apres une lecture figee : le lecteur repart avec la file d'attente de Deezer, pas avec la
    ''' playlist voulue. Elle est donc a relancer, et tout de suite si Slide voulait la musique.
    ''' (Pas quand c'est Slide qui ouvre la page de la playlist, ni apres le bouton "Accueil".)
    ''' </summary>
    Private Sub PageRechargee(e As EtatDuLecteur)
        If Not (e.Pret AndAlso e.Connecte) Then _pretAvant = False
        If e.Page = "" OrElse e.Page = _pageVue Then Exit Sub
        Dim premiere As Boolean = _pageVue = ""
        _pageVue = e.Page
        If premiere OrElse _playlistEnCours <> "" Then Exit Sub
        If _rechargementVoulu Then
            _rechargementVoulu = False
            Exit Sub
        End If
        DiagLog("Deezer : la page a ete rechargee (" & e.Adresse & ")" & If(PlaylistVoulue() = "", "", " -> la playlist sera relancee") & If(_voulu, ", la musique reprendra", ""))
        _playlistLancee = ""
        If _voulu Then Demander("jouer")
    End Sub

    ''' <summary>Secondes entre deux verifications du compte aupres de Deezer (dix minutes).</summary>
    Friend IntervalleDeVerificationDuCompte As Double = 600

    ''' <summary>Demande a Deezer si le compte est toujours connecte, au plus une fois toutes les
    ''' <paramref name="auPlusTot"/> secondes. La reponse arrive dans l'etat lu ensuite (Compte).</summary>
    Private Sub VerifierLeCompte(auPlusTot As Double)
        If (Now - _compteVerifieLe).TotalSeconds < auPlusTot Then Exit Sub
        _compteVerifieLe = Now
        Commander("verifierLeCompte")
    End Sub

    ''' <summary>Bouton "Accueil Deezer" de la fenetre : la page est rechargee expres, la
    ''' playlist n'a pas a etre relancee pour autant.</summary>
    Friend Sub RechargementVoulu()
        _rechargementVoulu = True
    End Sub

    Private Sub Demander(quoi As String)
        If _demande <> quoi Then
            _demandeDepuis = Now
            ' (Seulement quand cela change quelque chose : Slide redemande la lecture a chaque photo.)
            If (quoi = "jouer") <> _etat.Joue Then DiagLog("Deezer : " & If(quoi = "jouer", "lecture", "pause") & " demandee par Slide")
        End If
        _demande = quoi
        Minuteur.Interval = 500
    End Sub

    ''' <summary>Vrai si Deezer est joue dans Slide (reglage), et non par l'application externe.</summary>
    Friend ReadOnly Property Interne As Boolean
        Get
            Return DeezerInterne
        End Get
    End Property

    ''' <summary>Vrai si la musique du diaporama est Deezer, joue dans Slide.</summary>
    Friend ReadOnly Property Actif As Boolean
        Get
            Return Music_Deezer AndAlso DeezerInterne
        End Get
    End Property

    ''' <summary>Dernier etat lu dans la page (au plus vieux de deux secondes).</summary>
    Friend ReadOnly Property Etat As EtatDuLecteur
        Get
            Return _etat
        End Get
    End Property

    Friend ReadOnly Property EnLecture As Boolean
        Get
            Return _fenetre IsNot Nothing AndAlso _etat.Joue
        End Get
    End Property

    Friend ReadOnly Property Fenetre As FormDeezer
        Get
            Return _fenetre
        End Get
    End Property

    ' ------------------------------------------------------------------------------------------
    ' Commandes
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ouvre le lecteur Deezer dans Slide (fenetre hors de l'ecran) s'il ne l'est pas deja.</summary>
    Friend Sub Demarrer()
        If ConfierAuFilPrincipal(Sub() Demarrer(), "Deezer") Then Exit Sub
        If _quitte Then Exit Sub                                ' "Kill Deezer" : ferme jusqu'a ce qu'on le relance expres
        Try
            If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then
                _fenetre = New FormDeezer()
                _changementRegarde = False
                _etat = New EtatDuLecteur()
                _sansLecteurDepuis = Date.MinValue
                _jouait = False
                _pageVue = ""
                _pretAvant = False
                _pauseDemandee = False
                _rechargementVoulu = False
                _connexionDemandee = False
                _identifiantsEnvoyes = False
                _choixDemande = False
                _playlistLancee = ""
                _playlistEnCours = ""
                DiagLog("Deezer : ouverture du lecteur dans Slide")
            End If
            If _enVeille Then DiagLog("Deezer : sortie de veille (lecteur deja charge)")
            _enVeille = False
            _fenetre.Preparer()
            Minuteur.Start()
        Catch ex As Exception
            DiagLog("Deezer : ouverture impossible (" & ex.Message & ")")
        End Try
    End Sub

    Friend Sub Jouer()
        If ConfierAuFilPrincipal(Sub() Jouer(), "Deezer") Then Exit Sub
        If _quitte OrElse _pauseDemandee Then Exit Sub
        _prechauffage = False
        _voulu = True
        Demander("jouer")
        Demarrer()
        ' Une playlist a lancer : c'est la surveillance qui s'en charge (ouvrir sa page), pas
        ' une simple reprise de la file d'attente.
        If PlaylistALancer() = "" AndAlso _playlistEnCours = "" Then Commander("jouer")
    End Sub

    ''' <summary>
    ''' La playlist voulue vient de changer (commande "Deezer.Loved", "Deezer.Flow", tache de la
    ''' minuterie) : si la musique joue en ce moment, ou si Slide la veut, elle est lancee TOUT DE
    ''' SUITE. Sans cela elle n'etait lancee qu'a la prochaine demande de musique par le diaporama
    ''' (a chaque photo) ; pendant les videos muettes, ou la musique continue sans etre redemandee,
    ''' elle ne partait jamais (journal de l'autre poste de Jacob, 08/10/2026 : "mode VIDEO" en
    ''' continu, "Deezer.Loved" recu, aucun lancement). Si Slide tient la musique en pause (video
    ''' avec son, TV, film), rien ne part : la playlist sera lancee quand la musique reprendra.
    ''' </summary>
    Friend Sub JouerSiLaMusiqueJoue()
        If ConfierAuFilPrincipal(Sub() JouerSiLaMusiqueJoue(), "Deezer") Then Exit Sub
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed OrElse _quitte OrElse _pauseDemandee Then Exit Sub
        If _etat.Joue OrElse _voulu Then
            Jouer()
        ElseIf PlaylistALancer() <> "" Then
            DiagLog("Deezer : la musique est en pause (video avec son, TV...) -> la playlist " & PlaylistALancer() & " sera lancee quand elle reprendra")
        End If
    End Sub

    ''' <summary>Met Deezer en pause. Sans effet si le lecteur n'a pas ete ouvert.</summary>
    Friend Sub Pause()
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then Exit Sub
        If ConfierAuFilPrincipal(Sub() Pause(), "Deezer") Then Exit Sub
        CarteDeezer.Retirer()                                   ' la pochette quitte l'image tout de suite
        _voulu = False
        Demander("pause")
        Commander("pause")
    End Sub

    ''' <summary>Lecture / pause (commande a distance "Deezer.Pause", tache "Deezer Play/Pause").</summary>
    ''' <remarks>
    ''' La pause demandee ainsi TIENT jusqu'au prochain "Play/Pause" (ou a une relance de la
    ''' musique : Music ON, Lance Deezer, morceau suivant, OK des reglages). Sans cela, elle ne
    ''' durait que jusqu'a la photo suivante, ou Slide redemande la musique (08/10). Avec
    ''' l'application Deezer externe, Slide ne redemandait rien : la pause tenait toute seule.
    ''' Sans lecteur ouvert (Deezer quitte, autre source audio), la commande est ignoree, comme
    ''' elle l'etait quand la fenetre de l'application Deezer n'existait pas.
    ''' </remarks>
    Friend Sub Basculer()
        If ConfierAuFilPrincipal(Sub() Basculer(), "Deezer") Then Exit Sub
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then
            DiagLog("Deezer : Play/Pause ignore (le lecteur n'est pas ouvert)")
            Exit Sub
        End If
        ' (En pause sans l'avoir demande, c'est Slide qui la tient - video avec son, PIP... : Play.)
        If Not _pauseDemandee AndAlso (_etat.Joue OrElse _demande = "jouer") Then PauseSurDemande() Else LectureSurDemande()
    End Sub

    ''' <summary>
    ''' Commandes distinctes (demande de Jacob, 08/10 : "peux-tu dissocier play et pause par deux
    ''' commandes distinctes") : tache "Deezer Play" et commande a distance "Deezer.Play". La
    ''' musique joue, qu'elle ait ete mise en pause par "Deezer Pause" ou par Slide. Sans effet si
    ''' elle joue deja, ou si le lecteur n'est pas ouvert (pour l'ouvrir : "Lance Deezer").
    ''' </summary>
    Friend Sub LectureSurDemande()
        If ConfierAuFilPrincipal(Sub() LectureSurDemande(), "Deezer") Then Exit Sub
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then
            DiagLog("Deezer : Play ignore (le lecteur n'est pas ouvert)")
            Exit Sub
        End If
        DiagLog("Deezer : Play" & If(_etat.Joue, " (la musique joue deja)", ""))
        _pauseDemandee = False
        Jouer()
    End Sub

    ''' <summary>Tache "Deezer Pause" et commande a distance "Deezer.Stop" : la musique s'arrete
    ''' et la pause TIENT (Slide ne la relance pas a la photo suivante) jusqu'a "Deezer Play",
    ''' "Deezer Play/Pause", le morceau suivant, ou une relance de la musique.</summary>
    Friend Sub PauseSurDemande()
        If ConfierAuFilPrincipal(Sub() PauseSurDemande(), "Deezer") Then Exit Sub
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then
            DiagLog("Deezer : Pause ignoree (le lecteur n'est pas ouvert)")
            Exit Sub
        End If
        DiagLog("Deezer : Pause (tenue jusqu'a Play)" & If(_pauseDemandee, " (deja en pause)", ""))
        Pause()
        _pauseDemandee = True
    End Sub

    Private _pauseDemandee As Boolean           ' pause voulue par "Deezer Play/Pause" : Slide ne relance pas la musique

    Friend Sub Suivant()
        If ConfierAuFilPrincipal(Sub() Suivant(), "Deezer") Then Exit Sub
        _pauseDemandee = False                                  ' (le morceau suivant se met a jouer)
        Demarrer()
        Commander("suivant")
    End Sub

    Friend Sub Precedent()
        If ConfierAuFilPrincipal(Sub() Precedent(), "Deezer") Then Exit Sub
        Demarrer()
        Commander("precedent")
    End Sub

    ''' <summary>Ajoute le morceau en cours aux "Coups de coeur" (commande "Deezer.Favo").</summary>
    Friend Sub Favori()
        If ConfierAuFilPrincipal(Sub() Favori(), "Deezer") Then Exit Sub
        Commander("favori")
    End Sub

    ''' <summary>Retire le morceau en cours des "Coups de coeur" (commande "Deezer.Favo_Non", 08/10/2026).
    ''' S'il n'y est pas, rien ne se passe.</summary>
    Friend Sub RetirerDesFavoris()
        If ConfierAuFilPrincipal(Sub() RetirerDesFavoris(), "Deezer") Then Exit Sub
        Commander("favoriNon")
    End Sub

    ''' <summary>Volume de Deezer, de 0 a 100 comme le reglage "volume de la musique" de Slide.</summary>
    Friend Sub DefinirVolume(valeur As Integer)
        If ConfierAuFilPrincipal(Sub() DefinirVolume(valeur), "Deezer") Then Exit Sub
        _volume = Math.Max(0, Math.Min(100, valeur))
        Commander("volume")
    End Sub

    ''' <summary>Montre la fenetre de Deezer : pour choisir une playlist, finir une connexion...</summary>
    Friend Sub Montrer(Optional message As String = "")
        If ConfierAuFilPrincipal(Sub() Montrer(message), "Deezer") Then Exit Sub
        _quitte = False                                         ' (ouvrir la fenetre, c'est relancer Deezer)
        Demarrer()
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then Exit Sub
        _fenetre.Montrer(message)
    End Sub

    ''' <summary>Slide se ferme (menu, Echap) : le son de Deezer est coupe A L'INSTANT, puis son lecteur
    ''' est ferme, sans attendre la fin de la fermeture de Slide (demande de Jacob, 08/10/2026 :
    ''' "quand je quitte Slide, la musique Deezer continue un bon moment").</summary>
    Friend Sub ArreterALaSortie()
        If _fenetre Is Nothing Then Exit Sub
        If ConfierAuFilPrincipal(Sub() ArreterALaSortie(), "Deezer") Then Exit Sub
        Try
            DiagLog("Deezer : Slide se ferme -> son coupe, lecteur ferme")
            If Not _fenetre.IsDisposed Then _fenetre.Taire()
        Catch ex As Exception
        End Try
        Fermer()
    End Sub

    ''' <summary>
    ''' Retour sur Deezer par l'alternance ou la touche S (15/10/2026 : "quand c'est au tour de Deezer, envoie un next song,
    ''' c'est quasi immediat") : si le lecteur est deja pret (resté en veille, ou prechauffe), Slide n'attend plus le
    ''' rechargement ni le relancement de la playlist (jusqu'a 25 s) : il envoie "morceau suivant" a la file d'attente
    ''' qu'il a deja. Sans lecteur pret, rien n'est fait ici : le lancement habituel se poursuit.
    ''' </summary>
    Friend Sub ReprendreAuMorceauSuivant()
        If ConfierAuFilPrincipal(Sub() ReprendreAuMorceauSuivant(), "Deezer") Then Exit Sub
        Try
            Dim f As FormDeezer = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.NavigateurPret OrElse _quitte Then Exit Sub
            If _playlistLancee = "" AndAlso _playlistEnCours <> "" Then Exit Sub        ' un lancement de playlist est deja en cours
            _playlistEnCours = ""                                                      ' la file d'attente du lecteur est gardee
            If _playlistLancee = "" Then _playlistLancee = PlaylistVoulue()
            _pauseDemandee = False
            DiagLog("Deezer : retour par le morceau suivant (file d'attente deja chargee, playlist non relancee)")
            Commander("suivant")
            ' Deezer repart tout de suite : le MP3 ou la radio qui jouait encore est mis en pause MAINTENANT, au lieu d'attendre la lecture
            ' suivante de l'etat (jusqu'a 1 s de double son relevé dans le journal du 15-16/10/2026).
            MusiqueVLC.PauserCarDeezerJoue()
        Catch ex As Exception
            DiagLog("Deezer (morceau suivant au retour) : " & ex.Message)
        End Try
    End Sub

    Private _enVeille As Boolean
    Private WithEvents TimerVeille As New Timer With {.Interval = 600}
    Private _veilleEssais As Integer

    Private Sub TimerVeille_Tick(sender As Object, e As EventArgs) Handles TimerVeille.Tick
        _veilleEssais += 1
        If Not _enVeille OrElse _fenetre Is Nothing OrElse _fenetre.IsDisposed OrElse _veilleEssais > 4 Then TimerVeille.Stop()
        If _enVeille Then PauserSansVerifier()
    End Sub

    ''' <summary>Pause envoyee a la page sans consulter l'etat (qui n'est plus lu en veille).</summary>
    Private Async Sub PauserSansVerifier()
        Try
            Dim f As FormDeezer = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.NavigateurPret Then Exit Sub
            Await f.Executer("window.__slide ? __slide.pause() : 'page'")
        Catch ex As Exception
        End Try
    End Sub
    Private _surveilleLaFin As Boolean
    Private _prechauffage As Boolean
    Private _prechauffeDepuis As Date

    ''' <summary>
    ''' Charge Deezer a l'avance, en silence, quand il fait partie des sources cochees mais que la musique joue autre chose :
    ''' ouverture du lecteur, page, connexion. Le premier passage sur Deezer (touche S, alternance) ne l'attend plus (15/10/2026 :
    ''' "trop de temps d'attente pour passer de mp3 a deezer"). Une fois pret, il est mis en veille (pause, ouvert).
    ''' </summary>
    Friend Sub Prechauffer()
        If ConfierAuFilPrincipal(Sub() Prechauffer(), "Deezer") Then Exit Sub
        If Not DeezerInterne OrElse _quitte OrElse (_fenetre IsNot Nothing AndAlso Not _fenetre.IsDisposed) Then Exit Sub
        DiagLog("Deezer : prechauffage (le lecteur est charge a l'avance, sans jouer)")
        _prechauffage = True
        _prechauffeDepuis = Now
        Demarrer()
    End Sub

    ''' <summary>L'alternance des sources attend la fin du morceau de Deezer : l'etat est lu deux fois par seconde (au lieu
    ''' d'une fois toutes les 2 s) pour attraper la fin au plus juste.</summary>
    Friend Sub SurveillerLaFin(actif As Boolean)
        _surveilleLaFin = actif
        If actif AndAlso Minuteur.Enabled Then Minuteur.Interval = 500
    End Sub

    ''' <summary>
    ''' La source de la musique n'est plus Deezer (touche S, alternance) : le lecteur est mis en PAUSE et reste ouvert, sans
    ''' etre ferme (15/10/2026 : "pourquoi autant de temps pour passer sur Deezer, tu peux le mettre en stop a la place de te
    ''' reconnecter a chaque fois"). Le retour sur Deezer ne recharge plus la page ni ne se reconnecte : la lecture reprend.
    ''' </summary>
    Friend Sub MettreEnVeille()
        If _fenetre Is Nothing OrElse _fenetre.IsDisposed OrElse _enVeille OrElse _prechauffage Then Exit Sub
        If ConfierAuFilPrincipal(Sub() MettreEnVeille(), "Deezer") Then Exit Sub
        Try
            _enVeille = True
            DiagLog("Deezer : mis en veille (lecteur garde ouvert, pause) : retour rapide")
            Pause()
            Minuteur.Stop()
            _etat = New EtatDuLecteur()
            ' La pause est renvoyee quatre fois (0,6 s, 1,2 s, 1,8 s, 2,4 s) (15/10/2026 : "deezer et mp3 jouent en meme temps") : la premiere peut se perdre
            ' (page occupee, etat pas a jour) et Slide ne lit plus l'etat de Deezer en veille.
            _veilleEssais = 0
            TimerVeille.Start()
        Catch ex As Exception
            DiagLog("Deezer (veille) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Arrete Deezer et ferme son lecteur (la source audio n'est plus Deezer, tache
    ''' "Kill Deezer").</summary>
    Friend Sub Fermer()
        If _fenetre Is Nothing Then Exit Sub
        If ConfierAuFilPrincipal(Sub() Fermer(), "Deezer") Then Exit Sub
        Try
            Minuteur.Stop()
            _enVeille = False
            _demande = ""
            Dim f As FormDeezer = _fenetre
            _fenetre = Nothing
            CarteDeezer.Retirer()
            _etat = New EtatDuLecteur()
            If f IsNot Nothing AndAlso Not f.IsDisposed Then f.FermerPourDeBon()
            DiagLog("Deezer : lecteur ferme")
        Catch ex As Exception
            DiagLog("Deezer (fermeture) : " & ex.Message)
        End Try
    End Sub

    Private _quitte As Boolean                  ' Deezer a ete quitte expres (tache "Kill Deezer")

    ''' <summary>
    ''' Tache "Kill Deezer" de la minuterie, avec le Deezer integre : le lecteur est ferme et le
    ''' RESTE, comme l'application Deezer une fois tuee. (Avant le 08/10 : ferme, il se rouvrait a
    ''' la photo suivante, quand Slide redemande la musique.) Il revient par "Lance Deezer",
    ''' "Music Source / Deezer", "Music ON", le menu Music, le bouton "Ouvrir Deezer...", ou au
    ''' prochain lancement de Slide.
    ''' </summary>
    Friend Sub Quitter()
        If ConfierAuFilPrincipal(Sub() Quitter(), "Deezer") Then Exit Sub
        DiagLog("Deezer : quitte (Kill Deezer) -> ferme jusqu'a une relance")
        _quitte = True
        _voulu = False
        _pauseDemandee = False
        Fermer()
    End Sub

    ''' <summary>Deezer est relance expres (Music ON, Lance Deezer, changement de source...) :
    ''' il peut se rouvrir.</summary>
    Friend Sub Relancer()
        If _quitte Then DiagLog("Deezer : relance apres avoir ete quitte")
        _quitte = False
        _pauseDemandee = False
    End Sub

    ''' <summary>Vrai si Deezer a ete quitte expres et attend d'etre relance.</summary>
    Friend ReadOnly Property EstQuitte As Boolean
        Get
            Return _quitte
        End Get
    End Property

    ''' <summary>Le navigateur s'est arrete tout seul : on le rouvre, et la musique reprend si
    ''' elle jouait.</summary>
    Friend Sub Rouvrir(raison As String)
        Dim jouait As Boolean = _etat.Joue OrElse _demande = "jouer"
        DiagLog("Deezer : " & raison & " -> le lecteur est rouvert")
        Fermer()
        If Actif AndAlso jouait Then Jouer()
    End Sub

    ''' <summary>"Artiste - Titre" du morceau en cours ("" si on ne sait pas).</summary>
    Friend Function MorceauEnCours() As String
        If _etat.Titre = "" Then Return ""
        Return If(_etat.Artiste = "", _etat.Titre, _etat.Artiste & " - " & _etat.Titre)
    End Function

    ''' <summary>Envoie par mail le nom du morceau en cours (commande "Deezer.SongNameByMail").
    ''' Avant : une photo de la fenetre de l'application Deezer.</summary>
    Friend Async Sub EnvoyerLeTitre()
        If ConfierAuFilPrincipal(Sub() EnvoyerLeTitre(), "Deezer") Then Exit Sub
        Try
            Dim morceau As String = MorceauEnCours()
            If morceau = "" Then
                DiagLog("Deezer : pas de morceau en cours, rien a envoyer")
                Exit Sub
            End If
            ' Le morceau est retenu tout de suite : il peut changer pendant que la pochette arrive.
            Dim titre As String = _etat.Titre, artiste As String = _etat.Artiste, album As String = _etat.Album
            Dim fichier As String = IO.Path.Combine(DossierReglages, "Artiste.png")
            ' La pochette de l'album (demande de Jacob, 08/10/2026). Sans elle (pas recue) : la carte avec le texte seul.
            Using pochette As Image = Await TelechargerLaPochette(_etat.Pochette)
                DessinerLeTitre(fichier, titre, artiste, album, pochette)
                ' La carte est montree DANS le message (cid), pas en piece jointe (demande de Jacob, 08/10/2026) ;
                ' le texte dessous reste lisible meme si le logiciel de mail n'affiche pas les images.
                EnvoiDuMail(DestinataireDuTitre, "Deezer Song Name By Mail : " & morceau,
                          "<html><body><p><img src=""cid:" & IO.Path.GetFileName(fichier) & """ width=""660"" style=""max-width:100%;height:auto"" alt=""" &
                          Net.WebUtility.HtmlEncode(morceau) & """></p><p><b>" & Net.WebUtility.HtmlEncode(titre) & "</b><br>" & Net.WebUtility.HtmlEncode(artiste) &
                          "<br><i>" & Net.WebUtility.HtmlEncode(album) & "</i></p></body></html>", fichier)
                DiagLog("Deezer : nom du morceau envoye (" & morceau & If(pochette IsNot Nothing, ", avec la pochette", ", sans pochette") & ")")
            End Using
        Catch ex As Exception
            DiagLog("Deezer (envoi du titre) : " & ex.Message &
                    If(ex.Message.Contains("80040217"), " => Gmail refuse l'identifiant ou le mot de passe d'application de SendMail.SendEmail (rien n'est parti)", ""))
        End Try
    End Sub

    ''' <summary>Envoi du mail (SendMail.SendEmail), l'image etant mise dans le corps du message et non en
    ''' piece jointe. Remplace dans les bancs d'essai, pour ne rien envoyer.</summary>
    Friend EnvoiDuMail As Action(Of String, String, String, String) = Sub(destinataire, sujet, corps, image) SendEmail(destinataire, sujet, corps, "", image)

    Private ReadOnly _web As New Net.Http.HttpClient With {.Timeout = TimeSpan.FromSeconds(6), .MaxResponseContentBufferSize = 8000000}

    ''' <summary>L'image de l'album, prise sur le serveur d'images de Deezer (adresse donnee par la page).
    ''' Nothing si elle n'arrive pas en 6 secondes : le mail part alors sans elle.</summary>
    Friend Async Function TelechargerLaPochette(adresse As String) As Task(Of Image)
        Try
            If adresse = "" Then Return Nothing
            ' (L'adresse vient de la page : on ne va la chercher que sur le serveur d'images de Deezer.)
            Dim u As Uri = Nothing
            If Not Uri.TryCreate(adresse, UriKind.Absolute, u) OrElse u.Scheme <> Uri.UriSchemeHttps OrElse
               Not u.Host.EndsWith(".dzcdn.net", StringComparison.OrdinalIgnoreCase) Then
                DiagLog("Deezer : adresse de pochette ecartee (" & adresse & ")")
                Return Nothing
            End If
            Dim octets As Byte() = Await _web.GetByteArrayAsync(u)
            Using flux As New IO.MemoryStream(octets), lue As Image = Image.FromStream(flux)
                Return New Bitmap(lue)                      ' (copie : l'image lue depend du flux)
            End Using
        Catch ex As Exception
            DiagLog("Deezer : pochette non recue (" & ex.Message & ")")
            Return Nothing
        End Try
    End Function

    ''' <summary>Carte jointe au mail : la pochette de l'album a gauche, le titre, l'artiste et l'album
    ''' a droite. Sans pochette : le texte seul.</summary>
    Friend Sub DessinerLeTitre(fichier As String, titre As String, artiste As String, album As String, Optional pochette As Image = Nothing)
        Dim avecPochette As Boolean = pochette IsNot Nothing
        Dim largeur As Integer = If(avecPochette, 1100, 900), hauteur As Integer = If(avecPochette, 400, 260)
        Dim x As Single = If(avecPochette, 410.0F, 30.0F), place As Single = largeur - x - 30
        Using carte As New Bitmap(largeur, hauteur)
          ' (Resolution fixe : sinon la carte prend celle de l'ecran, et le texte, plus gros, deborde de ses cases.)
          carte.SetResolution(96, 96)
          Using g As Graphics = Graphics.FromImage(carte),
              grand As New Font("Segoe UI Semibold", 30), moyen As New Font("Segoe UI", 22), petit As New Font("Segoe UI", 16, FontStyle.Italic),
              gris As New SolidBrush(Color.FromArgb(200, 200, 205)),
              uneLigne As New StringFormat With {.Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap},
              deuxLignes As New StringFormat With {.Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.LineLimit}
            g.Clear(Color.FromArgb(28, 28, 30))
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
            Dim y As Single = 30
            If avecPochette Then
                ' La pochette, carree, coins arrondis.
                Dim cadre As New Rectangle(30, 30, 340, 340), r As Integer = 28
                Using contour As New GraphicsPath()
                    contour.AddArc(cadre.Left, cadre.Top, r, r, 180, 90)
                    contour.AddArc(cadre.Right - r, cadre.Top, r, r, 270, 90)
                    contour.AddArc(cadre.Right - r, cadre.Bottom - r, r, r, 0, 90)
                    contour.AddArc(cadre.Left, cadre.Bottom - r, r, r, 90, 90)
                    contour.CloseFigure()
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.SetClip(contour)
                    Dim cote As Integer = Math.Min(pochette.Width, pochette.Height)       ' (le carre du milieu, sans deformer)
                    g.DrawImage(pochette, cadre, New Rectangle((pochette.Width - cote) \ 2, (pochette.Height - cote) \ 2, cote, cote), GraphicsUnit.Pixel)
                    g.ResetClip()
                End Using
                ' Le titre peut prendre deux lignes ; l'artiste et l'album suivent juste dessous.
                Dim h As Single = g.MeasureString(titre, grand, New SizeF(place, 116), deuxLignes).Height
                g.DrawString(titre, grand, Brushes.White, New RectangleF(x, y, place, 116), deuxLignes)
                y += h + 14
            Else
                g.DrawString(titre, grand, Brushes.White, New RectangleF(x, y, place, 60), uneLigne)
                y = 105
            End If
            g.DrawString(artiste, moyen, gris, New RectangleF(x + 2, y, place, 45), uneLigne)
            g.DrawString(album, petit, gris, New RectangleF(x + 2, y + 60, place, 36), uneLigne)
            g.DrawString("Deezer - " & Now.ToString("dd/MM/yyyy HH:mm"), petit, Brushes.Gray, New RectangleF(x + 2, hauteur - 66, place, 36), uneLigne)
            carte.Save(fichier, Imaging.ImageFormat.Png)
          End Using
        End Using
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Identifiant et mot de passe des reglages
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly _sel As Byte() = Text.Encoding.UTF8.GetBytes("Slide.Deezer")

    ''' <summary>Chiffre le mot de passe pour l'enregistrer dans les reglages : seul le meme
    ''' utilisateur de Windows, sur le meme PC, peut le relire (protection de donnees de Windows).</summary>
    Friend Function Proteger(clair As String) As String
        If String.IsNullOrEmpty(clair) Then Return ""
        Return "W1:" & Convert.ToBase64String(ProtectedData.Protect(Text.Encoding.UTF8.GetBytes(clair), _sel, DataProtectionScope.CurrentUser))
    End Function

    ''' <summary>Le mot de passe en clair, ou "" s'il n'y en a pas ou s'il n'est pas lisible ici
    ''' (reglages copies d'un autre PC : il faut le ressaisir).</summary>
    Friend Function Deproteger(protege As String) As String
        Try
            If String.IsNullOrEmpty(protege) OrElse Not protege.StartsWith("W1:") Then Return ""
            Return Text.Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protege.Substring(3)), _sel, DataProtectionScope.CurrentUser))
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private _compteControlePour As String = ""  ' identifiant des reglages pour lequel le controle a deja agi

    ''' <summary>
    ''' Slide controle lui-meme que le compte connecte dans son lecteur est celui des reglages
    ''' (demande de Jacob, 08/10/2026 : "fais-le faire par Slide : controle que le nouveau login est
    ''' different, si oui alors lance Changer de compte"). Deezer donne l'adresse e-mail du compte
    ''' connecte ; si elle differe de l'identifiant des reglages, le compte en place est oublie et
    ''' Slide se connecte avec celui des reglages. Une seule tentative par identifiant : si, apres
    ''' elle, les deux different toujours (connexion par un autre moyen que l'e-mail...), Slide
    ''' n'insiste pas. Sans adresse donnee par Deezer, ou si l'identifiant des reglages n'est pas
    ''' une adresse e-mail, rien n'est fait.
    ''' </summary>
    Private Sub ControlerLeCompte(e As EtatDuLecteur)
        Dim voulu As String = If(DeezerLogin, "").Trim()
        If voulu = "" OrElse Not voulu.Contains("@"c) OrElse e.Courriel = "" OrElse Not e.Connecte Then Exit Sub
        If e.Courriel.Equals(voulu, StringComparison.OrdinalIgnoreCase) Then
            _compteControlePour = ""                            ' c'est le bon compte
            Exit Sub
        End If
        If _compteControlePour.Equals(voulu, StringComparison.OrdinalIgnoreCase) Then Exit Sub
        _compteControlePour = voulu
        DiagLog("Deezer : le compte connecte (" & Masquer(e.Courriel) & ") n'est pas celui des reglages (" & Masquer(voulu) & ") -> changement de compte")
        ChangerDeCompte()
    End Sub

    ''' <summary>Une adresse e-mail pour le journal : "j***@domaine.fr".</summary>
    Private Function Masquer(adresse As String) As String
        Dim a As Integer = adresse.IndexOf("@"c)
        If a <= 0 Then Return "***"
        Return adresse.Substring(0, 1) & "***" & adresse.Substring(a)
    End Function

    ''' <summary>Petit fichier laisse quand un changement de compte est demande alors que le lecteur
    ''' est ferme : il sera fait a sa prochaine ouverture, meme apres une relance de Slide.</summary>
    Friend MarqueDuChangementDeCompte As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Slide", "Deezer.changer_de_compte")
    Private _changementRegarde As Boolean       ' la marque a ete regardee pour ce lecteur

    ''' <summary>
    ''' L'identifiant des reglages a change, ou le bouton "Changer de compte" de la fenetre Deezer
    ''' (bug signale par Jacob, 08/10/2026 : "j'ai mis un autre login et quand je clique sur Ouvrir
    ''' Deezer je tombe sur mon compte, pas le nouveau"). Le navigateur de Slide garde la connexion
    ''' du compte precedent : changer l'identifiant des reglages ne le deconnectait pas. On oublie
    ''' donc ce compte (temoins de connexion), l'accueil de Deezer revient deconnecte, et la
    ''' connexion automatique repart avec l'identifiant et le mot de passe des reglages.
    ''' </summary>
    Friend Sub ChangerDeCompte()
        If ConfierAuFilPrincipal(Sub() ChangerDeCompte(), "Deezer") Then Exit Sub
        Try
            _identifiantsEnvoyes = False
            _connexionDemandee = False
            Dim f As FormDeezer = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.NavigateurPret Then
                IO.File.WriteAllText(MarqueDuChangementDeCompte, Now.ToString("s"))
                DiagLog("Deezer : changement de compte demande, lecteur ferme -> il sera fait a sa prochaine ouverture")
                Exit Sub
            End If
            DiagLog("Deezer : changement de compte -> le compte en place est oublie" & If(DeezerLogin = "", " (pas d'identifiant dans les reglages : connexion a la main)", ", connexion avec l'identifiant des reglages"))
            _rechargementVoulu = True                   ' (page rechargee expres)
            _playlistLancee = "" : _playlistEnCours = ""
            _compteVerifieLe = Date.MinValue
            _etat = New EtatDuLecteur()
            CarteDeezer.Retirer()
            f.OublierLeCompte(Sub(fait As Boolean)
                                  Try
                                      If Not fait Then Exit Sub
                                      If IO.File.Exists(MarqueDuChangementDeCompte) Then IO.File.Delete(MarqueDuChangementDeCompte)
                                      If _voulu Then Demander("jouer")        ' la musique reprendra une fois le nouveau compte connecte
                                  Catch ex As Exception
                                      DiagLog("Deezer (changement de compte) : " & ex.Message)
                                  End Try
                              End Sub)
        Catch ex As Exception
            DiagLog("Deezer (changement de compte) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Les reglages ont change (identifiant, mot de passe) : la connexion automatique
    ''' pourra etre retentee.</summary>
    Friend Sub IdentifiantsChanges()
        _identifiantsEnvoyes = False
        _connexionDemandee = False
    End Sub

    ''' <summary>Remplit la page de connexion de Deezer avec l'identifiant et le mot de passe des
    ''' reglages, et la valide. Le mot de passe ne passe que par ici : jamais dans le journal.</summary>
    Private Async Sub EnvoyerLesIdentifiants(f As FormDeezer)
        Try
            Dim clair As String = Deproteger(DeezerMotDePasse)
            If clair = "" Then
                DiagLog("Deezer : mot de passe des reglages illisible sur ce PC (a ressaisir dans l'onglet Audio)")
                DemanderLaConnexion(f, "Le mot de passe enregistré n'est pas lisible sur ce PC : connectez-vous ici, ou ressaisissez-le dans les réglages de Slide.")
                Exit Sub
            End If
            Dim script As String = "window.__slide ? __slide.connexion(" & Newtonsoft.Json.JsonConvert.SerializeObject(DeezerLogin) & "," &
                                   Newtonsoft.Json.JsonConvert.SerializeObject(clair) & ") : 'page'"
            Dim reponse As String = Texte(Await f.Executer(script))
            DiagLog("Deezer : connexion avec l'identifiant des reglages -> " & If(reponse = "", "(sans reponse)", reponse))
        Catch ex As Exception
            DiagLog("Deezer (connexion) : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dialogue avec la page
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Envoie une commande au lecteur de la page. Si la page n'est pas encore la, la
    ''' demande (jouer, pause, volume) est reprise par la surveillance, toutes les deux secondes.</summary>
    Private Async Sub Commander(quoi As String)
        Try
            Dim f As FormDeezer = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.NavigateurPret Then Exit Sub
            ' Pas de lecture tant qu'aucun compte n'est connecte (ce seraient des extraits).
            If quoi = "jouer" AndAlso Not (_etat.Pret AndAlso _etat.Connecte) Then Exit Sub
            If quoi = "pause" AndAlso Not _etat.Pret Then Exit Sub         ' pas de lecteur dans la page : rien ne joue
            Dim script As String
            If quoi = "flow" Then
                script = "window.__slide ? __slide.flow(" & Newtonsoft.Json.JsonConvert.SerializeObject(VarianteDuFlow(_playlistEnCours)) & ") : 'page'"
            ElseIf quoi = "volume" Then
                If _volume < 0 Then Exit Sub
                script = "window.__slide ? __slide.volume(" & (_volume / 100.0).ToString("0.###", Globalization.CultureInfo.InvariantCulture) & ") : 'page'"
            Else
                script = "window.__slide ? __slide." & quoi & "() : 'page'"
            End If
            Dim reponse As String = Texte(Await f.Executer(script))
            ' (Page rechargee pour retrouver son lien de connexion : on y va des la lecture suivante.)
            If quoi = "versLaConnexion" AndAlso reponse = "rechargee" Then _versConnexionLe = Date.MinValue
            If quoi = "favori" OrElse quoi = "favoriNon" Then
                ' (Dit clairement ce qui s'est passe, dans le journal et sous l'annonce a l'ecran.)
                Dim morceau As String = If(MorceauEnCours() = "", "(morceau inconnu)", MorceauEnCours())
                Dim resultat As String
                If quoi = "favori" Then
                    resultat = If(reponse = "bouton", "ajoute aux Coups de coeur", If(reponse = "deja", "deja dans les Coups de coeur, rien n'est change", ""))
                Else
                    resultat = If(reponse = "bouton", "retire des Coups de coeur", If(reponse = "deja", "n'etait pas dans les Coups de coeur, rien n'est change", ""))
                End If
                If resultat = "" Then resultat = "coeur introuvable (" & If(reponse = "", "sans reponse", reponse) & ")"
                DiagLog("Deezer : Coups de coeur, " & morceau & " -> " & resultat)
                ' (A l'ecran, en bon francais ; le journal, lui, reste sans accents.)
                Dim aLEcran As String
                If quoi = "favori" Then
                    aLEcran = If(reponse = "bouton", "ajouté aux Coups de cœur", If(reponse = "deja", "déjà dans les Coups de cœur", "cœur introuvable"))
                Else
                    aLEcran = If(reponse = "bouton", "retiré des Coups de cœur", If(reponse = "deja", "n'était pas dans les Coups de cœur", "cœur introuvable"))
                End If
                AnnonceEcran.Preciser("Deezer.", morceau & " : " & aLEcran)
            ElseIf quoi <> "volume" AndAlso (quoi = "suivant" OrElse quoi = "precedent" OrElse quoi = "favori" OrElse reponse <> "interface") Then
                DiagLog("Deezer : " & quoi & " -> " & If(reponse = "", "(sans reponse)", reponse))
            End If
        Catch ex As Exception
            DiagLog("Deezer (" & quoi & ") : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Le texte renvoye par la page arrive entre guillemets (JSON).</summary>
    Private Function Texte(json As String) As String
        Try
            If String.IsNullOrEmpty(json) OrElse json = "null" Then Return ""
            Return If(Newtonsoft.Json.JsonConvert.DeserializeObject(Of String)(json), "")
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>Toutes les deux secondes : lit l'etat du lecteur et fait aboutir ce que Slide a
    ''' demande (connexion, jouer ou pause, volume) des que la page est prete.</summary>
    Private Async Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles Minuteur.Tick
        If _lectureEnCours Then Exit Sub
        _lectureEnCours = True
        Try
            Dim f As FormDeezer = _fenetre
            If f Is Nothing OrElse f.IsDisposed Then
                Minuteur.Stop()
                Exit Sub
            End If
            f.TenirEnPlace()
            If Not f.NavigateurPret Then
                If f.Erreur <> "" AndAlso Not f.ErreurDite Then
                    f.ErreurDite = True
                    DiagLog("Deezer : navigateur indisponible (" & f.Erreur & ")")
                    SaveLogError("Deezer", "navigateur WebView2 indisponible (" & f.Erreur & ")")
                End If
                Exit Sub
            End If
            Dim brut As String = Texte(Await f.Executer("window.__slide ? __slide.etat() : ''"))
            If _fenetre IsNot f Then Exit Sub                   ' ferme pendant l'attente
            Dim lu As New EtatDuLecteur()
            If brut <> "" Then
                Dim o As JObject = JObject.Parse(brut)
                lu.Charge = CBool(o("charge"))
                lu.Pret = CBool(o("pret"))
                lu.Connecte = CBool(o("connecte"))
                lu.Formulaire = CBool(o("formulaire"))
                lu.Joue = CBool(o("joue"))
                lu.Titre = CStr(o("titre"))
                lu.Artiste = CStr(o("artiste"))
                lu.Album = CStr(o("album"))
                If o("pochette") IsNot Nothing Then lu.Pochette = CStr(o("pochette"))
                lu.Volume = CDbl(o("volume"))
                lu.Muet = CBool(o("muet"))
                lu.Position = CDbl(o("position"))
                If o("duree") IsNot Nothing Then lu.Duree = CDbl(o("duree"))
                lu.Morceaux = CInt(o("morceaux"))
                lu.Contexte = CStr(o("contexte"))
                lu.Adresse = CStr(o("adresse"))
                lu.Page = CStr(o("page"))
                lu.Compte = CStr(o("compte"))
                lu.Favoris = CStr(o("favoris"))
                If o("courriel") IsNot Nothing Then lu.Courriel = CStr(o("courriel"))
                If ExtraitsPermis Then lu.Connecte = True       ' (banc d'essai)
            End If
            _etat = lu
            ' La pochette et le titre sur l'image de Slide (pas pendant qu'une pause est demandee).
            CarteDeezer.Suivre(_etat, _demande <> "pause")
            Suivre(f)
            If _prechauffage Then
                If Actif Then
                    _prechauffage = False                                  ' Deezer est devenu la source : plus rien a preparer
                ElseIf _etat.Pret AndAlso _etat.Connecte OrElse (Now - _prechauffeDepuis).TotalSeconds > 150 Then
                    Dim pret As Boolean = _etat.Pret AndAlso _etat.Connecte
                    _prechauffage = False
                    DiagLog("Deezer : prechauffage " & If(pret, "termine en " & CInt((Now - _prechauffeDepuis).TotalSeconds) & " s", "abandonne (pas pret apres 150 s)") & ", mis en veille")
                    MettreEnVeille()
                End If
            End If
            ' Plus rien d'attendu : retour au rythme lent.
            Dim rythme As Integer = If(_demande <> "" OrElse _playlistEnCours <> "" OrElse _surveilleLaFin, 500, 2000)
            If Minuteur.Interval <> rythme Then Minuteur.Interval = rythme
        Catch ex As Exception
            DiagLog("Deezer (etat) : " & ex.Message)
        Finally
            _lectureEnCours = False
        End Try
    End Sub

    Private Sub Suivre(f As FormDeezer)
        Dim e As EtatDuLecteur = _etat
        ' Un changement de compte demande pendant que le lecteur etait ferme : maintenant.
        If Not _changementRegarde Then
            _changementRegarde = True
            If IO.File.Exists(MarqueDuChangementDeCompte) Then
                ChangerDeCompte()
                Exit Sub
            End If
        End If
        f.Afficher(e)
        PageRechargee(e)

        ' Page de connexion : avec les identifiants des reglages, ou a la main.
        If e.Formulaire AndAlso Not e.Pret Then
            _sansLecteurDepuis = Date.MinValue
            SeConnecter(f)
            Exit Sub
        End If

        If Not e.Pret Then
            ' Page chargee, sans lecteur, qui propose de se connecter : c'est la page d'accueil
            ' de Deezer pour qui n'a pas de compte connecte.
            If e.Charge AndAlso Not e.Connecte Then
                _sansLecteurDepuis = Date.MinValue
                AllerALaConnexion(f)
                Exit Sub
            End If
            ' Page pas encore chargee, ou restee en panne (pas de reseau au demarrage du PC...) :
            ' au bout d'une minute, on la recharge.
            If _sansLecteurDepuis = Date.MinValue Then _sansLecteurDepuis = Now
            If (Now - _sansLecteurDepuis).TotalSeconds >= 60 Then
                _sansLecteurDepuis = Date.MinValue
                DiagLog("Deezer : lecteur toujours absent de la page (" & e.Adresse & "), page rechargee")
                f.RechargerLaMusique()
            End If
            Exit Sub
        End If
        _sansLecteurDepuis = Date.MinValue

        If Not e.Connecte Then
            If e.Compte = "non" AndAlso Not _deconnexionDite Then
                _deconnexionDite = True
                DiagLog("Deezer : le compte n'est plus connecte (session terminee par Deezer) -> reconnexion")
            End If
            If e.Joue Then Commander("pause")                   ' pas d'extraits de 30 secondes
            AllerALaConnexion(f)
            Exit Sub
        End If
        ' Le lecteur vient d'etre pret (demarrage, page rechargee, reconnexion) : le delai
        ' laisse a la lecture pour demarrer part de maintenant, pas du moment de la demande.
        If Not _pretAvant Then _demandeDepuis = Now
        _pretAvant = True
        If _connexionDemandee OrElse _identifiantsEnvoyes Then
            DiagLog("Deezer : compte connecte")
            If _connexionDemandee Then f.Dire("Connecté à Deezer. Choisissez votre musique si besoin, puis masquez cette fenêtre.")
            _connexionDemandee = False
            _identifiantsEnvoyes = False
        End If

        ' Le compte est-il toujours connecte ? Quand Deezer termine la session, sa page garde
        ' son allure et affiche seulement un message (lu dans son code le 08/10/2026) : on le
        ' demande donc a Deezer, comme le fait sa page, toutes les dix minutes ; et plus tot si la
        ' musique ne part pas (voir plus bas).
        _deconnexionDite = False
        VerifierLeCompte(IntervalleDeVerificationDuCompte)
        ControlerLeCompte(e)

        ' Volume de Slide (reglage "volume de la musique", minuterie).
        If _volume >= 0 AndAlso e.Volume >= 0 AndAlso Math.Abs(e.Volume - _volume / 100.0) > 0.011 Then Commander("volume")
        ' Son coupe dans le lecteur de Deezer (son bouton haut-parleur, garde d'un lancement a
        ' l'autre) : le lecteur "joue" mais on n'entend rien. C'est Slide qui regle le son : on
        ' le retablit (sauf volume 0 demande).
        If e.Muet AndAlso _volume <> 0 AndAlso (Now - _sonRetabliLe).TotalSeconds >= 30 Then
            _sonRetabliLe = Now
            DiagLog("Deezer : le son du lecteur etait coupe (bouton haut-parleur de Deezer), il est retabli")
            Commander("retablirLeSon")
        End If

        If e.Joue Then
            Dim morceau As String = MorceauEnCours()
            If morceau <> _dernierTitre Then
                _dernierTitre = morceau
                DiagLog("Deezer : " & morceau)
            End If
        End If
        ' Journal : chaque passage de la lecture a la pause et retour, avec le volume ; et une
        ' lecture qui n'avance pas (le lecteur dit jouer, la position reste la meme 10 secondes).
        If e.Joue <> _jouait Then
            _jouait = e.Joue
            DiagLog("Deezer : " & If(e.Joue, "la musique joue", "en pause") & " (volume " & CInt(Math.Round(e.Volume * 100)) & " %" &
                    If(e.Muet, ", SON COUPE", "") & If(_demande = "", "", ", demande en cours : " & _demande) & ")")
            _immobileDepuis = Date.MinValue
            _immobileDit = False
        End If
        If e.Joue AndAlso Math.Abs(e.Position - _positionVue) < 0.05 Then
            If _immobileDepuis = Date.MinValue Then _immobileDepuis = Now
            If Not _immobileDit AndAlso (Now - _immobileDepuis).TotalSeconds >= 10 Then
                _immobileDit = True
                DiagLog("Deezer : le lecteur dit jouer mais n'avance pas (" & MorceauEnCours() & ", position " & e.Position.ToString("0") & " s)")
            End If
            ' Coupure du reseau, flux perdu... : Deezer ne repart pas toujours seul. Au bout de
            ' 20 secondes, pause puis lecture ; au bout d'une minute, la page est rechargee (et la
            ' playlist relancee, voir PageRechargee).
            Dim fige As Double = (Now - _immobileDepuis).TotalSeconds
            If fige >= 20 AndAlso Not _relanceTentee Then
                _relanceTentee = True
                DiagLog("Deezer : lecture figee depuis 20 s -> pause puis lecture")
                VerifierLeCompte(20)
                Commander("relancer")
            ElseIf fige >= 60 Then
                DiagLog("Deezer : lecture figee depuis une minute -> la page est rechargee")
                _immobileDepuis = Date.MinValue
                _relanceTentee = False
                f.RechargerLaMusique()
            End If
        Else
            _immobileDepuis = Date.MinValue
            _immobileDit = False
            _relanceTentee = False
        End If
        _positionVue = e.Position

        Select Case _demande
            Case "jouer"
                If _playlistEnCours <> "" Then
                    SuivreLaPlaylist(e)
                ElseIf PlaylistALancer() <> "" Then
                    LancerLaPlaylist(f, PlaylistALancer())
                ElseIf e.Joue Then
                    _demande = ""
                    _choixDemande = False
                Else
                    Commander("jouer")
                    ' Huit secondes que la musique ne part pas : la session est peut-etre perdue.
                    If (Now - _demandeDepuis).TotalSeconds >= 8 Then VerifierLeCompte(20)
                    If (Now - _demandeDepuis).TotalSeconds >= 20 Then
                        ' Vingt secondes que le lecteur ne demarre pas : rien dans la file
                        ' d'attente. On montre la fenetre (une fois) pour choisir une musique.
                        _demande = ""
                        DiagLog("Deezer : la lecture ne demarre pas (" & e.Morceaux & " morceau(x) en attente)")
                        ' (Reseau coupe, ou deja en cours de reprise apres une panne : la fenetre ne s'ouvre pas, Slide reessaie tout seul - voir SurveillerLaPanne.)
                        If Not _choixDemande AndAlso Not _enPanne AndAlso System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
                            _choixDemande = True
                            f.Montrer("Deezer n'a rien à jouer : choisissez une musique, puis masquez cette fenêtre.")
                        End If
                    End If
                End If
            Case "pause"
                If Not e.Joue Then
                    _demande = ""
                Else
                    Commander("pause")
                    If (Now - _demandeDepuis).TotalSeconds >= 20 Then _demande = ""
                End If
        End Select
        SurveillerLaPanne(f, e)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Reprise apres une panne (21/10/2026 : "la connexion Internet a du etre coupee quelques minutes : Deezer affichait « Erreur : lecture impossible
    ' (MS0610) » et ne repartait pas. Il faut gerer cela et relancer Deezer si necessaire").
    ' Slide veut de la musique Deezer et rien ne joue depuis 30 secondes : il controle d'abord le reseau (sans reseau, il attend et re-controle
    ' toutes les 10 secondes) ; une fois le reseau revenu il clique sur « Reessayer » si Deezer affiche une erreur, redemande la lecture, et tous les
    ' trois essais (ou si l'erreur persiste) recharge la page, ce qui relance la playlist (voir PageRechargee). Essais espaces de 45 secondes. Quand la
    ' musique repart, la fenetre de Deezer qui aurait ete ouverte entre-temps est remise hors de l'ecran.
    ' ------------------------------------------------------------------------------------------
    Private _panneDepuis As Date = Date.MinValue
    Private _dernierEssaiPanne As Date = Date.MinValue
    Private _essaisPanne As Integer
    Private _panneEnCours As Boolean
    Private _enPanne As Boolean
    Private _sansReseauDit As Boolean
    Private ReadOnly _httpPanne As New System.Net.Http.HttpClient With {.Timeout = TimeSpan.FromSeconds(5)}

    ''' <summary>Detecte le message d'erreur de Deezer (« lecture impossible », code MSxxxx) et clique sur son bouton « Reessayer ».</summary>
    Private Const ScriptErreur As String = "(function(){ var t=(document.body&&document.body.innerText)||''; " &
        "var erreur=/lecture impossible|ne peut pas être chargé|MS0\d+/i.test(t); var clic=''; " &
        "if(erreur){ var bs=[].slice.call(document.querySelectorAll('button,[role=button],a')); " &
        "for(var i=0;i<bs.length;i++){ var x=(bs[i].innerText||'').trim(); if(/^r[eé]essayer$|^retry$/i.test(x)){ bs[i].click(); clic='clic'; break; } } } " &
        "return erreur ? ('erreur '+clic) : ''; })()"

    ''' <summary>Vrai si Internet repond (une reponse de Deezer, quelle qu'elle soit).</summary>
    Private Async Function InternetDisponible() As Task(Of Boolean)
        If Not System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then Return False
        Try
            Using reponse As System.Net.Http.HttpResponseMessage = Await _httpPanne.GetAsync("https://www.deezer.com/robots.txt", System.Net.Http.HttpCompletionOption.ResponseHeadersRead)
                Return True
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub SurveillerLaPanne(f As FormDeezer, e As EtatDuLecteur)
        Dim attendue As Boolean = Actif AndAlso _voulu AndAlso Not _quitte AndAlso Music AndAlso Not e.Joue
        If Not attendue Then
            _panneDepuis = Date.MinValue
            If e.Joue AndAlso _enPanne Then
                DiagLog("Deezer : la musique a repris apres la panne (" & _essaisPanne & " essai(s))")
                _enPanne = False
                _essaisPanne = 0
                _sansReseauDit = False
                _choixDemande = False
                Try
                    f.Masquer()                    ' (la fenetre ouverte par la panne repart hors de l'ecran)
                Catch ex As Exception
                End Try
            ElseIf Not _voulu OrElse Not Actif Then
                _enPanne = False
                _essaisPanne = 0
            End If
            Exit Sub
        End If
        If _panneDepuis = Date.MinValue Then _panneDepuis = Now
        If (Now - _panneDepuis).TotalSeconds < 30 Then Exit Sub
        If (Now - _dernierEssaiPanne).TotalSeconds < 45 Then Exit Sub
        ReprendreApresPanne(f)
    End Sub

    Private Async Sub ReprendreApresPanne(f As FormDeezer)
        If _panneEnCours Then Exit Sub
        _panneEnCours = True
        Try
            _dernierEssaiPanne = Now
            _enPanne = True
            Dim reseau As Boolean = Await InternetDisponible()
            If _fenetre IsNot f Then Exit Sub                  ' ferme pendant l'attente
            If Not reseau Then
                If Not _sansReseauDit Then
                    _sansReseauDit = True
                    DiagLog("Deezer : pas de connexion Internet, la musique reprendra au retour du reseau")
                End If
                _dernierEssaiPanne = Now.AddSeconds(-35)       ' nouveau controle du reseau dans 10 secondes
                Exit Sub
            End If
            If _sansReseauDit Then
                _sansReseauDit = False
                DiagLog("Deezer : la connexion Internet est revenue")
            End If
            _essaisPanne += 1
            Dim erreur As String = Texte(Await f.Executer(ScriptErreur))
            If _fenetre IsNot f Then Exit Sub
            DiagLog("Deezer : reprise apres panne, essai " & _essaisPanne & If(erreur <> "", " (Deezer affiche une erreur : " & erreur & ")", ""))
            If _essaisPanne Mod 3 = 0 OrElse (erreur <> "" AndAlso _essaisPanne >= 2) Then
                DiagLog("Deezer : la page est rechargee, la playlist sera relancee")
                f.RechargerLaMusique()
            Else
                _choixDemande = False
                Demander("jouer")
            End If
        Catch ex As Exception
            DiagLog("Deezer (reprise apres panne) : " & ex.Message)
        Finally
            _panneEnCours = False
        End Try
    End Sub

    ''' <summary>Pas de compte connecte : on va a la page de connexion de Deezer (son lien est
    ''' dans la page).</summary>
    Private Sub AllerALaConnexion(f As FormDeezer)
        If (Now - _versConnexionLe).TotalSeconds < 15 Then Exit Sub
        _versConnexionLe = Now
        If _etat.Adresse.Contains("account.deezer.com") Then
            ' Les identifiants viennent d'etre envoyes : la page change, on lui laisse le temps
            ' (vu chez Jacob le 07/10 : fenetre montree 2 s apres l'envoi, connexion faite 12 s plus tard).
            If _identifiantsEnvoyes AndAlso (Now - _envoiLe).TotalSeconds <= AttenteDeLaConnexion Then
                _versConnexionLe = Date.MinValue
                Exit Sub
            End If
            ' Deja chez "account.deezer.com", sans les champs : verification demandee par Deezer.
            DemanderLaConnexion(f, "Deezer demande une vérification : terminez la connexion ici.")
        Else
            DiagLog("Deezer : aucun compte connecte, ouverture de la page de connexion")
            Commander("versLaConnexion")
        End If
    End Sub

    ''' <summary>Sur la page de connexion : les identifiants des reglages sont envoyes UNE fois ;
    ''' sans identifiants, ou si la connexion n'a pas abouti au bout de vingt secondes, la fenetre
    ''' est montree pour la faire a la main.</summary>
    Private Sub SeConnecter(f As FormDeezer)
        Dim identifiants As Boolean = DeezerLogin <> "" AndAlso DeezerMotDePasse <> ""
        If Not identifiants Then
            DemanderLaConnexion(f, "Connectez-vous à votre compte Deezer (une seule fois sur ce PC). On peut aussi saisir l'identifiant et le mot de passe dans les réglages de Slide, onglet Audio.")
        ElseIf Not _identifiantsEnvoyes Then
            _identifiantsEnvoyes = True
            _envoiLe = Now
            EnvoyerLesIdentifiants(f)
        ElseIf (Now - _envoiLe).TotalSeconds > AttenteDeLaConnexion Then
            DemanderLaConnexion(f, "La connexion automatique n'a pas abouti (mot de passe refusé, ou vérification demandée par Deezer) : terminez-la ici.")
        End If
    End Sub

    Private Sub DemanderLaConnexion(f As FormDeezer, message As String)
        If _connexionDemandee Then Exit Sub
        _connexionDemandee = True
        DiagLog("Deezer : connexion a faire a la main, la fenetre est montree")
        f.Montrer(message)
    End Sub

End Module
