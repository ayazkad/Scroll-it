<div align="center">

  <img src="https://github.com/user-attachments/assets/941a67fa-f2c1-422c-93d5-ba8f91d0f2af" alt="Scroll-It Banner" width="800" />

  <br/><br/>

  <h1>⚡ Scroll-It</h1>
  <p><strong>Ultra-smooth macOS-like scrolling engine for Windows • High-frequency physics</strong></p>
  <p><em>Défilement ultra-fluide type macOS pour Windows • Moteur physique haute fréquence</em></p>

  <p>
    <a href="https://github.com/ayazkad/Scroll-it/releases">
      <img src="https://img.shields.io/badge/Release-v1.1.1-00d2ff?style=for-the-badge&logo=github" alt="Latest Release" />
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

  <p>
    <strong>🌐 Languages :</strong> 
    <a href="#-english">English</a> • 
    <a href="#-version-française">Français</a>
  </p>

</div>

---

<a name="-english"></a>
## 🇬🇧 English

**Scroll-It** is a lightweight, native Windows application (portable, zero external runtime dependencies) that intercepts the rigid and stepped 120-unit physical mouse wheel ticks and replaces them with a smooth high-frequency physics interpolation engine (macOS/iOS momentum, natural acceleration, and silky deceleration tail).

### 🌟 Key Features

- **🌊 Buttery-Smooth Scrolling**: Replaces rigid 120-unit discrete notches with continuous, fluid physics interpolation on any display.
- **↔️ Smooth Horizontal Scrolling (`Shift + Wheel`)**: Full horizontal physics support with instant, clean stop when releasing the `Shift` key.
- **🛡️ Smart Tab & Focus Protection**: Cancels momentum immediately on mouse clicks or navigation shortcuts (`Ctrl+Tab`, `Alt+Tab`, etc.) to prevent buffered scroll dumps when returning to tabs.
- **🌐 Full Multi-Language Support**: Complete UI, Installer, Uninstaller, and Systray menu localized in **English**, **French**, and **Russian** with instant live switching.
- **🚀 Natural Momentum & Acceleration**: Consecutive quick wheel turns build momentum to glide through long documents and web pages effortlessly.
- **🎯 Instant Brake & Direction Reversal**: Changing scroll direction instantly stops previous inertia for surgical responsiveness.
- **🍏 1-Click Profiles & Presets**:
  - **Mac Buttery** *(Default)*: Silky smooth feel identical to macOS.
  - **Snappy**: Crisp, rapid response with short damping, ideal for code editors and spreadsheets.
  - **Cinematic Glide**: Elongated, ultra-soft glide for articles and feeds.
  - **Ultra Smooth**: Powerful momentum with balanced damping.
  - **Custom**: Precision micro-adjustments per pixel and millisecond.
- **🎛 4 Physics Sliders with Precision Stepper Buttons [−] [+]**:
  - *Step Size*: Pixel distance per notch (±1 px).
  - *Animation Time*: Damping transition time (±10 ms).
  - *Acceleration Multiplier (Inertia)*: Momentum factor during rapid scrolling (±0.1x).
  - *Friction Tail*: Glide softness before reaching full stop (±0.01).
- **🎮 App & Game Manager (Blacklist / Exclusions)**: Automatic process detection with actual icons and 1-click exclusions for competitive games and sensitive software.
- **🔍 Native Zoom Pass-Through (`Ctrl + Wheel`)**: Preserves instant native browser & application zoom without interference.
- **📥 System Tray Companion**: Right-click menu to toggle, switch presets, or open settings.
- **🚀 Start with Windows**: Easy 1-click toggle directly in the UI.
- **🔄 Built-in Update Checker**: Automatic notifications when a new release is available on GitHub.

### 📁 Project Structure

```
Scroll-it/
├── bin/
│   ├── Scroll-it-Setup.exe       # 🚀 Standalone All-In-One Installer (Single-File, Multi-language)
│   └── Scroll-it-Portable.exe    # ⚡ Portable standalone version (No install needed)
├── src/
│   ├── Engine/
│   │   ├── Localization.cs       # 🌐 Localization Engine (English, Français, Русский)
│   │   ├── Win32.cs              # P/Invoke Interop, WH_MOUSE_LL, SendInput, High-Precision Timer
│   │   ├── SettingsManager.cs    # JSON Settings & Windows Registry Manager
│   │   ├── ScrollPhysics.cs      # Physics Momentum & High-Frequency V-Sync Engine
│   │   ├── MouseHook.cs          # Low-Level Mouse Hook & Event Dispatcher
│   │   ├── UpdateChecker.cs      # GitHub Release Update Checker
│   │   └── WebKitMomentumScroller.cs # WebKit-grade continuous momentum reference
│   ├── UI/
│   │   ├── Styles.cs             # Dark Glassmorphism / Fluent Theme & Design Tokens
│   │   ├── TrayManager.cs        # Systray Icon & Context Menu Manager
│   │   └── MainWindow.cs         # Modern WPF UI with live interactive testing arena
│   ├── Setup/
│   │   ├── SetupWindow.cs        # Graphical Installer with Language Selector
│   │   ├── UninstallWindow.cs    # Multi-language Uninstaller Wizard
│   │   └── SetupProgram.cs       # Setup & Uninstall Entry Point
│   └── Program.cs                # Main Application Entry Point with Single-Instance Mutex
├── build.ps1                     # PowerShell build script (via native csc.exe)
└── README.md
```

### 🚀 Getting Started

#### Run the Application:
- **Portable Version**: Double-click `bin\Scroll-it-Portable.exe`.
- **Install on Windows**: Double-click `bin\Scroll-it-Setup.exe` (Self-contained standalone wizard).

#### Recompile from Source:
Open PowerShell in the project folder and run:
```powershell
.\build.ps1
```

#### ⚙ Configuration File:
Custom preferences and blacklisted apps are automatically saved in:
`%APPDATA%\scroll-it\settings.json`

---

<a name="-version-française"></a>
## 🇫🇷 Version Française

**Scroll-It** est une application Windows native (portable, zéro dépendance externe) qui intercepte les crans saccadés de votre molette de souris matérielle et leur applique un moteur physique d'interpolation fluide (type macOS / iPhone, inertie naturelle, accélération progressive et amorti soyeux).

### 🌟 Fonctionnalités Principales

- **🌊 Défilement Buttery Smooth** : Remplace les 120 crans rigides de Windows par une interpolation continue et fluide quel que soit votre écran.
- **↔️ Défilement Horizontal Fluide (`Shift + Molette`)** : Prise en charge physique complète avec arrêt net instantané au relâchement de la touche `Shift`.
- **🛡️ Protection des Onglets & Focus** : Annulation instantanée de l'inertie lors d'un clic de souris ou d'un raccourci (`Ctrl+Tab`, `Alt+Tab`, etc.) pour éviter tout déversement de scroll au retour sur un onglet.
- **🌐 Support Multi-langues** : Interface complète, installateur, désinstallateur et menu Systray traduits en **Français**, **English** et **Русский** avec basculement dynamique instantané.
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
- **🔄 Vérificateur de Mises à Jour Intégré** : Notification automatique lorsqu'une nouvelle version est disponible sur GitHub.

### 🚀 Lancement & Utilisation

#### Lancer l'application :
- **Sans installation (Portable)** : Double-cliquez directement sur `bin\Scroll-it-Portable.exe`.
- **Installer dans Windows** : Double-cliquez sur `bin\Scroll-it-Setup.exe` (contient tout en interne).

#### Recompiler le projet :
Ouvrez PowerShell dans le dossier et exécutez :
```powershell
.\build.ps1
```

#### ⚙ Fichier de Configuration :
Les réglages personnalisés et la liste des applications exclues sont automatiquement sauvegardés dans :
`%APPDATA%\scroll-it\settings.json`
