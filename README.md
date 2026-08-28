<div align="center">

  <img src="https://github.com/user-attachments/assets/941a67fa-f2c1-422c-93d5-ba8f91d0f2af" alt="Scroll-It Banner" width="800" />

  <br/><br/>

  <h1>⚡ Scroll-It</h1>
  <p><strong>Défilement ultra-fluide type macOS pour Windows • Moteur physique haute fréquence (144Hz / 240Hz)</strong></p>

  <p>
    <a href="https://github.com/ayazkad/Scroll-it/releases">
      <img src="https://img.shields.io/badge/Release-v1.0.0-00d2ff?style=for-the-badge&logo=github" alt="Latest Release" />
    </a>
    <a href="https://github.com/ayazkad/Scroll-it/releases">
      <img src="https://img.shields.io/badge/Downloads-Portable%20%26%20Setup-00f2fe?style=for-the-badge&logo=windows" alt="Downloads" />
    </a>
    <img src="https://img.shields.io/badge/Windows-7%20%7C%208%20%7C%2010%20%7C%2011-0080ff?style=for-the-badge&logo=windows&logoColor=white" alt="Windows Support" />
    <a href="LICENSE">
      <img src="https://img.shields.io/badge/License-GPL--3.0-00c853?style=for-the-badge" alt="License GPL-3.0" />
    </a>
  </p>

  <br/>

</div>

**Scroll-It** est une application Windows native (portable, zéro dépendance externe) qui intercepte les crans saccadés de votre molette de souris matérielle et leur applique un moteur physique d'interpolation fluide (type macOS / iPhone, inertie naturelle, accélération progressive et amorti soyeux).

---

## 🌟 Fonctionnalités Principales

- **🌊 Défilement Buttery Smooth** : Remplace les 120 crans rigides de Windows par une interpolation continue à haute fréquence (144Hz / 240Hz).
- **🚀 Accélération & Inertie Naturelle** : Plusieurs coups de molette rapides consécutifs accumulent de l'élan pour parcourir de longues pages et documents sans effort pour vos doigts.
- **🎯 Freinage Instantané** : Dès que vous changez de sens de défilement, l'élan précédent est instantanément stoppé pour une réactivité chirurgicale.
- **🍏 Profils & Presets en 1 Clic** :
  - **Mac Buttery** *(Par défaut)* : Sensation fluide et soyeuse identique à macOS.
  - **Snappy** : Réponse très rapide et arrêt net, idéal pour les éditeurs de code et la bureautique.
  - **Cinematic Glide** : Défilement allongé et très amorti, idéal pour les articles longs et flux de lecture.
  - **Ultra Smooth** : Élan puissant et grande douceur.
  - **Personnalisé** : Réglage fin au pixel et à la milliseconde près.
- **🎛 4 Curseurs Physiques avec Boutons de Précision [−] [+]** :
  - *Taille du pas (Step Size)* : Distance en pixels par cran (±1 px).
  - *Durée d'animation (Animation Time)* : Temps d'amortissement de la transition (±10 ms).
  - *Multiplicateur d'accélération (Inertia)* : Coefficient d'élan lors de défilements rapides (±0.1x).
  - *Queue de décélération (Tail / Friction)* : Douceur de la glisse avant l'arrêt complet (±0.01).
- **🎮 Gestionnaire d'Applications & Jeux (Exclusions)** : Détection automatique des applications avec leurs icônes réelles et désactivation au choix sur les jeux compétitifs ou logiciels sensibles.
- **🔍 Bypass Intelligent du Zoom (`Ctrl + Molette`)** : Préserve le zoom natif instantané et précis dans les navigateurs et logiciels sans interférence.
- **📥 Zone de Notification (Systray)** : Menu clic droit pour activer/désactiver, changer de preset ou ouvrir les réglages.
- **🚀 Démarrage avec Windows** : Option activable en 1 clic dans l'interface.

---

## 📁 Structure du Projet

```
Scroll-it/
├── bin/
│   ├── Scroll-it-Setup.exe       # 🚀 Installateur autonome tout-en-un (Single-File, fonctionne seul sans aucun fichier tiers)
│   └── Scroll-it-Portable.exe    # ⚡ Version portable prête à l'emploi (sans installation requise)
├── src/
│   ├── Engine/
│   │   ├── Win32.cs              # Interop P/Invoke, WH_MOUSE_LL, SendInput, Timer haute précision
│   │   ├── SettingsManager.cs    # Gestionnaire des paramètres JSON & Registre Windows
│   │   ├── ScrollPhysics.cs      # Moteur physique d'interpolation et d'inertie
│   │   └── MouseHook.cs          # Hook bas-niveau et routage d'évènements
│   ├── UI/
│   │   ├── Styles.cs             # Thème sombre Dark Glassmorphism / Fluent
│   │   ├── TrayManager.cs        # Gestion de l'icône Systray & Menu contextuel
│   │   └── MainWindow.cs         # Interface WPF moderne avec zone de test en direct
│   ├── Setup/
│   │   ├── SetupWindow.cs        # Interface graphique d'installation avec extraction intégrée
│   │   ├── UninstallWindow.cs    # Assistant de désinstallation
│   │   └── SetupProgram.cs      # Point d'entrée de l'installateur / désinstallateur
│   └── Program.cs                # Point d'entrée avec Mutex d'instance unique
├── build.ps1                     # Script de compilation PowerShell (via csc.exe)
└── README.md
```

---

## 🚀 Lancement & Utilisation

### Lancer l'application :
- **Sans installation (Portable)** : Double-cliquez directement sur `bin\Scroll-it-Portable.exe`.
- **Installer dans Windows** : Double-cliquez sur `bin\Scroll-it-Setup.exe` (vous pouvez le déplacer où vous voulez, il contient tout ce qu'il faut en interne).

### Recompiler le projet :
Ouvrez PowerShell dans le dossier et exécutez :
```powershell
.\build.ps1
```

---

## ⚙ Fichier de Configuration

Les réglages personnalisés et la liste des applications exclues sont automatiquement sauvegardés dans :
`%APPDATA%\scroll-it\settings.json`
