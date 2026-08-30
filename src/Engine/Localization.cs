using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScrollIt.Engine
{
    public enum AppLanguage
    {
        French,
        English,
        Russian
    }

    public static class I18n
    {
        private static AppLanguage _currentLanguage = AppLanguage.French;

        public static event Action LanguageChanged;

        public static AppLanguage CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    if (LanguageChanged != null)
                    {
                        LanguageChanged();
                    }
                }
            }
        }

        public static string CurrentLanguageCode
        {
            get
            {
                switch (_currentLanguage)
                {
                    case AppLanguage.English: return "en";
                    case AppLanguage.Russian: return "ru";
                    default: return "fr";
                }
            }
        }

        public static void SetLanguageByCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                SetAutoLanguage();
                return;
            }

            code = code.ToLowerInvariant();
            if (code.StartsWith("ru"))
            {
                CurrentLanguage = AppLanguage.Russian;
            }
            else if (code.StartsWith("en"))
            {
                CurrentLanguage = AppLanguage.English;
            }
            else
            {
                CurrentLanguage = AppLanguage.French;
            }
        }

        public static void SetAutoLanguage()
        {
            try
            {
                string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (cultureName == "ru" || cultureName == "be" || cultureName == "uk" || cultureName == "kk")
                {
                    CurrentLanguage = AppLanguage.Russian;
                }
                else if (cultureName == "fr")
                {
                    CurrentLanguage = AppLanguage.French;
                }
                else
                {
                    CurrentLanguage = AppLanguage.English;
                }
            }
            catch
            {
                CurrentLanguage = AppLanguage.English;
            }
        }

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings =
            new Dictionary<string, Dictionary<AppLanguage, string>>(StringComparer.OrdinalIgnoreCase)
        {
            // === Common / Window ===
            { "AppTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it" },
                { AppLanguage.English, "Scroll-it" },
                { AppLanguage.Russian, "Scroll-it" }
            }},
            { "AppTagline", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Moteur de défilement fluide pour Windows" },
                { AppLanguage.English, "Smooth Scrolling Engine for Windows" },
                { AppLanguage.Russian, "Движок плавного скролла для Windows" }
            }},
            { "Status_Active", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Actif" },
                { AppLanguage.English, "Active" },
                { AppLanguage.Russian, "Активен" }
            }},
            { "Status_Inactive", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Inactif" },
                { AppLanguage.English, "Inactive" },
                { AppLanguage.Russian, "Неактивен" }
            }},
            { "Status_Paused", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "En pause (Désactivé)" },
                { AppLanguage.English, "Paused (Disabled)" },
                { AppLanguage.Russian, "Приостановлен (Отключен)" }
            }},

            // === Navigation Tabs ===
            { "Tab_Physics", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Physique & Presets" },
                { AppLanguage.English, "Physics & Presets" },
                { AppLanguage.Russian, "Физика и профили" }
            }},
            { "Tab_Apps", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Applications & Exclusions" },
                { AppLanguage.English, "Apps & Exclusions" },
                { AppLanguage.Russian, "Приложения и исключения" }
            }},
            { "Tab_Options", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Options & Démarrage" },
                { AppLanguage.English, "Options & Startup" },
                { AppLanguage.Russian, "Параметры и автозапуск" }
            }},

            // === Physics Tab ===
            { "Physics_PresetsTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Profils de fluidité (1-Clic)" },
                { AppLanguage.English, "Smoothness Presets (1-Click)" },
                { AppLanguage.Russian, "Профили плавности (1 клик)" }
            }},
            { "Preset_MacOS_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Inertie fluide type macOS, accélération progressive et amorti soyeux." },
                { AppLanguage.English, "macOS-like fluid inertia, progressive acceleration, and silky dampening." },
                { AppLanguage.Russian, "Плавная инерция в стиле macOS, прогрессивное ускорение и шелковистое затухание." }
            }},
            { "Preset_Snappy_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Réponse vive et précise avec un amorti court, idéal pour travailler rapidement." },
                { AppLanguage.English, "Snappy and precise response with short dampening, ideal for fast work." },
                { AppLanguage.Russian, "Быстрый и точный отклик с коротким затуханием, идеально для продуктивной работы." }
            }},
            { "Preset_CinematicGlide_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Glisse ultra-douce et allongée, idéale pour la lecture d'articles et de flux." },
                { AppLanguage.English, "Ultra-smooth and elongated glide, ideal for reading articles and feeds." },
                { AppLanguage.Russian, "Сверхмягкое и длительное скольжение, идеально для чтения статей и лент." }
            }},
            { "Preset_UltraSmooth_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Défilement soyeux et équilibré avec accélération fluide sur plusieurs crans." },
                { AppLanguage.English, "Silky and balanced scroll with smooth multi-notch acceleration." },
                { AppLanguage.Russian, "Шелковистая и сбалансированная прокрутка с плавным ускорением." }
            }},
            { "Preset_Custom_Name", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Personnalisé" },
                { AppLanguage.English, "Custom" },
                { AppLanguage.Russian, "Пользовательский" }
            }},
            { "Preset_Custom_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Paramètres personnalisés ajustés manuellement." },
                { AppLanguage.English, "Custom settings adjusted manually." },
                { AppLanguage.Russian, "Пользовательские параметры, настроенные вручную." }
            }},

            // === Sliders ===
            { "Slider_StepSize_Title", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Taille du pas (Step Size)" },
                { AppLanguage.English, "Step Size (Distance)" },
                { AppLanguage.Russian, "Размер шага (Step Size)" }
            }},
            { "Slider_StepSize_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Distance parcourue pour un cran de molette (défaut Windows : 120 px)" },
                { AppLanguage.English, "Distance scrolled per mouse wheel notch (Windows default: 120 px)" },
                { AppLanguage.Russian, "Дистанция за один щелчок колесика (по умолчанию в Windows: 120 px)" }
            }},
            { "Slider_AnimTime_Title", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Durée d'animation (Animation Time)" },
                { AppLanguage.English, "Animation Time (Duration)" },
                { AppLanguage.Russian, "Время анимации (Animation Time)" }
            }},
            { "Slider_AnimTime_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Temps d'amortissement de la transition fluide" },
                { AppLanguage.English, "Dampening transition time for smoothness" },
                { AppLanguage.Russian, "Время затухания и плавности перехода" }
            }},
            { "Slider_Accel_Title", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Multiplicateur d'accélération (Inertia)" },
                { AppLanguage.English, "Acceleration Multiplier (Inertia)" },
                { AppLanguage.Russian, "Множитель ускорения (Inertia)" }
            }},
            { "Slider_Accel_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Vitesse exponentielle lors de coups de molette rapides consécutifs" },
                { AppLanguage.English, "Exponential speed boost upon consecutive fast scrolls" },
                { AppLanguage.Russian, "Экспоненциальный прирост скорости при быстрой непрерывной прокрутке" }
            }},
            { "Slider_Tail_Title", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Queue de décélération (Tail / Friction)" },
                { AppLanguage.English, "Deceleration Tail (Tail / Friction)" },
                { AppLanguage.Russian, "Хвост замедления (Tail / Friction)" }
            }},
            { "Slider_Tail_Desc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Douceur de la glisse finale avant l'arrêt complet" },
                { AppLanguage.English, "Gentleness of the final glide before full stop" },
                { AppLanguage.Russian, "Мягкость и плавность финального скольжения до полной остановки" }
            }},
            { "Btn_Donate", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "❤️ Faire un don" },
                { AppLanguage.English, "❤️ Donate / Sponsor" },
                { AppLanguage.Russian, "❤️ Поддержать проект" }
            }},

            // === Apps Tab ===
            { "Apps_CardTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Exceptions & Liste Noire d'Applications" },
                { AppLanguage.English, "App Exceptions & Blacklist" },
                { AppLanguage.Russian, "Исключения и черный список приложений" }
            }},
            { "Apps_CardDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it se désactive automatiquement sur les exécutables ci-dessous (idéal pour les jeux compétitifs, logiciels de modélisation 3D / CAD ou applications sensibles)." },
                { AppLanguage.English, "Scroll-it automatically disables itself for the applications below (ideal for competitive games, 3D / CAD modeling software, or sensitive apps)." },
                { AppLanguage.Russian, "Scroll-it автоматически отключается в указанных ниже приложениях (идеально для соревновательных игр, 3D/CAD софта или чувствительных программ)." }
            }},
            { "Apps_AddTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Rechercher ou ajouter une application" },
                { AppLanguage.English, "Search or add an application" },
                { AppLanguage.Russian, "Найти или добавить приложение" }
            }},
            { "Apps_SearchPlaceholder", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Rechercher un jeu ou une application (ex: League of Legends, discord)..." },
                { AppLanguage.English, "Search a game or app on PC (e.g. League of Legends, discord)..." },
                { AppLanguage.Russian, "Поиск игры или программы на ПК (напр. League of Legends, discord)..." }
            }},
            { "Apps_BrowseBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "📁 Parcourir" },
                { AppLanguage.English, "📁 Browse" },
                { AppLanguage.Russian, "📁 Обзор" }
            }},
            { "Apps_AddBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "+ Ajouter" },
                { AppLanguage.English, "+ Add" },
                { AppLanguage.Russian, "+ Добавить" }
            }},
            { "Apps_AddProcessBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Ajouter le processus" },
                { AppLanguage.English, "Add Process" },
                { AppLanguage.Russian, "Добавить процесс" }
            }},
            { "Apps_ListHeader", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Applications désactivées ({0})" },
                { AppLanguage.English, "Disabled Applications ({0})" },
                { AppLanguage.Russian, "Отключенные приложения ({0})" }
            }},
            { "Apps_EmptyTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Aucune application désactivée" },
                { AppLanguage.English, "No disabled applications" },
                { AppLanguage.Russian, "Нет отключенных приложений" }
            }},
            { "Apps_EmptyDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it est actif et fluide sur l'ensemble de vos logiciels et jeux." },
                { AppLanguage.English, "Scroll-it is active and smooth across all your software and games." },
                { AppLanguage.Russian, "Scroll-it активен и обеспечивает плавность во всех программах и играх." }
            }},
            { "Apps_DeleteBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "✕ Supprimer" },
                { AppLanguage.English, "✕ Remove" },
                { AppLanguage.Russian, "✕ Удалить" }
            }},

            // === Options Tab ===
            { "Options_CardTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Options Système & Comportement" },
                { AppLanguage.English, "System Options & Behavior" },
                { AppLanguage.Russian, "Системные параметры и поведение" }
            }},
            { "Options_ThemeTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Apparence & Effets Windows 11" },
                { AppLanguage.English, "Appearance & Windows 11 Effects" },
                { AppLanguage.Russian, "Внешний вид и эффекты Windows 11" }
            }},
            { "Theme_AccentLabel", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Couleur d'accentuation :" },
                { AppLanguage.English, "Accent Color:" },
                { AppLanguage.Russian, "Цвет акцента:" }
            }},
            { "Theme_BackdropLabel", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Effet d'arrière-plan (DWM / Fluent) :" },
                { AppLanguage.English, "Backdrop Effect (DWM / Fluent):" },
                { AppLanguage.Russian, "Эффект фона (DWM / Fluent):" }
            }},
            { "Backdrop_Mica", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Mica (Windows 11)" },
                { AppLanguage.English, "Mica (Windows 11)" },
                { AppLanguage.Russian, "Mica (Windows 11)" }
            }},
            { "Backdrop_Acrylic", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Acrylic (Flou translucide)" },
                { AppLanguage.English, "Acrylic (Translucent Blur)" },
                { AppLanguage.Russian, "Acrylic (Полупрозрачный)" }
            }},
            { "Backdrop_GlassDark", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Verre sombre classique" },
                { AppLanguage.English, "Classic Dark Glass" },
                { AppLanguage.Russian, "Классическое темное стекло" }
            }},
            { "Backdrop_OledBlack", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "OLED Noir profond" },
                { AppLanguage.English, "OLED Deep Black" },
                { AppLanguage.Russian, "OLED Глубокий черный" }
            }},
            { "Options_LanguageTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Langue de l'interface" },
                { AppLanguage.English, "Interface Language" },
                { AppLanguage.Russian, "Язык интерфейса" }
            }},
            { "Options_AutoStart", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Lancer au démarrage" },
                { AppLanguage.English, "Launch at startup" },
                { AppLanguage.Russian, "Автозапуск" }
            }},
            { "Options_CtrlZoom", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Zoom natif Ctrl" },
                { AppLanguage.English, "Native Ctrl zoom" },
                { AppLanguage.Russian, "Точный зум Ctrl" }
            }},
            { "Options_ReverseDirection", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Défilement naturel" },
                { AppLanguage.English, "Natural scrolling" },
                { AppLanguage.Russian, "Естественная прокрутка" }
            }},
            { "Options_MinimizeToTray", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Réduire en Systray" },
                { AppLanguage.English, "Minimize to tray" },
                { AppLanguage.Russian, "Сворачивать в трей" }
            }},
            { "Options_ResetDefaults", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Réinitialiser tous les réglages par défaut" },
                { AppLanguage.English, "Reset all settings to defaults" },
                { AppLanguage.Russian, "Сбросить все настройки по умолчанию" }
            }},

            // === Update Checker ===
            { "Update_CardTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Mises à jour de Scroll-it" },
                { AppLanguage.English, "Scroll-it Updates" },
                { AppLanguage.Russian, "Обновления Scroll-it" }
            }},
            { "Update_VersionLabel", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Version installée : v{0}" },
                { AppLanguage.English, "Installed Version: v{0}" },
                { AppLanguage.Russian, "Установленная версия: v{0}" }
            }},
            { "Update_CheckBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "🔄 Vérifier les mises à jour" },
                { AppLanguage.English, "🔄 Check for Updates" },
                { AppLanguage.Russian, "🔄 Проверить обновления" }
            }},
            { "Update_Checking", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Vérification en cours..." },
                { AppLanguage.English, "Checking for updates..." },
                { AppLanguage.Russian, "Проверка обновлений..." }
            }},
            { "Update_UpToDate", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "✓ Vous utilisez la dernière version (v{0})" },
                { AppLanguage.English, "✓ You are using the latest version (v{0})" },
                { AppLanguage.Russian, "✓ Вы используете последнюю версию (v{0})" }
            }},
            { "Update_Available", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "🎉 Nouvelle version v{0} disponible !" },
                { AppLanguage.English, "🎉 New version v{0} is available!" },
                { AppLanguage.Russian, "🎉 Доступна новая версия v{0}!" }
            }},
            { "Update_DownloadBtn", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "⬇ Télécharger la mise à jour" },
                { AppLanguage.English, "⬇ Download Update" },
                { AppLanguage.Russian, "⬇ Скачать обновление" }
            }},
            { "Update_Error", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Impossible de vérifier les mises à jour (vérifiez votre connexion)" },
                { AppLanguage.English, "Unable to check for updates (check your connection)" },
                { AppLanguage.Russian, "Не удалось проверить обновления (проверьте соединение)" }
            }},

            // === Systray Menu ===
            { "Tray_StatusActive", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Actif" },
                { AppLanguage.English, "Active" },
                { AppLanguage.Russian, "Активен" }
            }},
            { "Tray_StatusPaused", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "En pause (Désactivé)" },
                { AppLanguage.English, "Paused (Disabled)" },
                { AppLanguage.Russian, "Приостановлен (Отключен)" }
            }},
            { "Tray_PresetsMenu", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Profils / Presets" },
                { AppLanguage.English, "Presets / Profiles" },
                { AppLanguage.Russian, "Профили / Пресеты" }
            }},
            { "Tray_Settings", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Réglages Scroll-it..." },
                { AppLanguage.English, "Scroll-it Settings..." },
                { AppLanguage.Russian, "Настройки Scroll-it..." }
            }},
            { "Tray_AutoStart", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Lancer au démarrage" },
                { AppLanguage.English, "Launch at Startup" },
                { AppLanguage.Russian, "Запуск при старте" }
            }},
            { "Tray_Exit", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Quitter" },
                { AppLanguage.English, "Exit" },
                { AppLanguage.Russian, "Выход" }
            }},

            // === Setup Wizard ===
            { "Setup_WindowTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Installation de Scroll-it" },
                { AppLanguage.English, "Scroll-it Setup" },
                { AppLanguage.Russian, "Установка Scroll-it" }
            }},
            { "Setup_HeaderTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Installation de Scroll-it v1.1.1" },
                { AppLanguage.English, "Scroll-it Setup v1.1.1" },
                { AppLanguage.Russian, "Установка Scroll-it v1.1.1" }
            }},
            { "Setup_WelcomeHeading", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Bienvenue dans le programme d'installation de Scroll-it" },
                { AppLanguage.English, "Welcome to the Scroll-it Setup Wizard" },
                { AppLanguage.Russian, "Добро пожаловать в программу установки Scroll-it" }
            }},
            { "Setup_WelcomeDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it apporte le défilement ultra-fluide à l'ensemble de vos applications Windows." },
                { AppLanguage.English, "Scroll-it brings ultra-smooth scrolling to all your Windows applications." },
                { AppLanguage.Russian, "Scroll-it добавляет сверхплавную прокрутку во все ваши приложения Windows." }
            }},
            { "Setup_BtnCancel", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Annuler" },
                { AppLanguage.English, "Cancel" },
                { AppLanguage.Russian, "Отмена" }
            }},
            { "Setup_BtnNext", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Suivant >" },
                { AppLanguage.English, "Next >" },
                { AppLanguage.Russian, "Далее >" }
            }},
            { "Setup_BtnBack", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "< Précédent" },
                { AppLanguage.English, "< Back" },
                { AppLanguage.Russian, "< Назад" }
            }},
            { "Setup_BtnInstall", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Installer" },
                { AppLanguage.English, "Install" },
                { AppLanguage.Russian, "Установить" }
            }},
            { "Setup_BtnFinish", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Terminer" },
                { AppLanguage.English, "Finish" },
                { AppLanguage.Russian, "Готово" }
            }},
            { "Setup_PathTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Dossier de destination" },
                { AppLanguage.English, "Destination Folder" },
                { AppLanguage.Russian, "Папка назначения" }
            }},
            { "Setup_PathDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Choisissez le dossier dans lequel installer Scroll-it :" },
                { AppLanguage.English, "Choose the folder in which to install Scroll-it:" },
                { AppLanguage.Russian, "Выберите папку для установки Scroll-it:" }
            }},
            { "Setup_BtnBrowse", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Parcourir..." },
                { AppLanguage.English, "Browse..." },
                { AppLanguage.Russian, "Обзор..." }
            }},
            { "Setup_BrowseDialogDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Sélectionnez le dossier d'installation pour Scroll-it" },
                { AppLanguage.English, "Select the installation folder for Scroll-it" },
                { AppLanguage.Russian, "Выберите папку для установки Scroll-it" }
            }},
            { "Setup_OptionsTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Options supplémentaires" },
                { AppLanguage.English, "Additional Options" },
                { AppLanguage.Russian, "Дополнительные параметры" }
            }},
            { "Setup_ChkDesktop", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Créer un raccourci sur le Bureau" },
                { AppLanguage.English, "Create a Desktop shortcut" },
                { AppLanguage.Russian, "Создать ярлык на Рабочем столе" }
            }},
            { "Setup_ChkStartMenu", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Ajouter Scroll-it au Menu Démarrer" },
                { AppLanguage.English, "Add Scroll-it to the Start Menu" },
                { AppLanguage.Russian, "Добавить Scroll-it в меню «Пуск»" }
            }},
            { "Setup_ChkAutoStart", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Lancer Scroll-it automatiquement au démarrage de Windows" },
                { AppLanguage.English, "Launch Scroll-it automatically at Windows startup" },
                { AppLanguage.Russian, "Запускать Scroll-it автоматически при старте Windows" }
            }},
            { "Setup_ProgressTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Installation de Scroll-it en cours..." },
                { AppLanguage.English, "Installing Scroll-it..." },
                { AppLanguage.Russian, "Установка Scroll-it..." }
            }},
            { "Setup_ProgressPrep", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Préparation des fichiers..." },
                { AppLanguage.English, "Preparing files..." },
                { AppLanguage.Russian, "Подготовка файлов..." }
            }},
            { "Setup_ProgressStopProcesses", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Arrêt des processus existants..." },
                { AppLanguage.English, "Stopping existing processes..." },
                { AppLanguage.Russian, "Остановка запущенных процессов..." }
            }},
            { "Setup_ProgressCreateDir", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Création du répertoire d'installation..." },
                { AppLanguage.English, "Creating installation directory..." },
                { AppLanguage.Russian, "Создание папки установки..." }
            }},
            { "Setup_ProgressExtract", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Extraction des fichiers exécutables et ressources..." },
                { AppLanguage.English, "Extracting executables and resources..." },
                { AppLanguage.Russian, "Извлечение исполняемых файлов и ресурсов..." }
            }},
            { "Setup_ProgressShortcuts", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Création des raccourcis système..." },
                { AppLanguage.English, "Creating system shortcuts..." },
                { AppLanguage.Russian, "Создание системных ярлыков..." }
            }},
            { "Setup_ProgressRegistry", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Enregistrement dans Windows (Paramètres > Applications)..." },
                { AppLanguage.English, "Registering in Windows (Settings > Apps)..." },
                { AppLanguage.Russian, "Регистрация в Windows (Параметры > Приложения)..." }
            }},
            { "Setup_ProgressComplete", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Installation terminée avec succès !" },
                { AppLanguage.English, "Installation completed successfully!" },
                { AppLanguage.Russian, "Установка успешно завершена!" }
            }},
            { "Setup_FinishTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it a été installé avec succès !" },
                { AppLanguage.English, "Scroll-it was installed successfully!" },
                { AppLanguage.Russian, "Scroll-it успешно установлен!" }
            }},
            { "Setup_FinishDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "L'application est prête à l'emploi et intégrée à votre système Windows." },
                { AppLanguage.English, "The application is ready to use and integrated into your Windows system." },
                { AppLanguage.Russian, "Приложение готово к работе и интегрировано в систему Windows." }
            }},
            { "Setup_ChkLaunchNow", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Lancer Scroll-it maintenant" },
                { AppLanguage.English, "Launch Scroll-it now" },
                { AppLanguage.Russian, "Запустить Scroll-it сейчас" }
            }},
            { "Setup_CancelConfirm", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Voulez-vous vraiment annuler l'installation de Scroll-it ?" },
                { AppLanguage.English, "Are you sure you want to cancel the installation of Scroll-it?" },
                { AppLanguage.Russian, "Вы действительно хотите отменить установку Scroll-it?" }
            }},
            { "Setup_ErrorExtract", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Impossible d'extraire Scroll-it.exe." },
                { AppLanguage.English, "Unable to extract Scroll-it.exe." },
                { AppLanguage.Russian, "Не удалось извлечь Scroll-it.exe." }
            }},
            { "Setup_ErrorGeneral", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Erreur lors de l'installation : " },
                { AppLanguage.English, "Installation error: " },
                { AppLanguage.Russian, "Ошибка при установке: " }
            }},

            // === Uninstaller Wizard ===
            { "Uninst_WindowTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Désinstallation de Scroll-it" },
                { AppLanguage.English, "Scroll-it Uninstaller" },
                { AppLanguage.Russian, "Удаление Scroll-it" }
            }},
            { "Uninst_HeaderTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Désinstallation de Scroll-it" },
                { AppLanguage.English, "Scroll-it Uninstaller" },
                { AppLanguage.Russian, "Удаление Scroll-it" }
            }},
            { "Uninst_ConfirmHeading", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Voulez-vous vraiment désinstaller Scroll-it ?" },
                { AppLanguage.English, "Are you sure you want to uninstall Scroll-it?" },
                { AppLanguage.Russian, "Вы действительно хотите удалить Scroll-it?" }
            }},
            { "Uninst_ConfirmDesc", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Cette action supprimera Scroll-it de votre ordinateur, ainsi que ses raccourcis du Menu Démarrer et du Bureau." },
                { AppLanguage.English, "This will remove Scroll-it from your computer, including Start Menu and Desktop shortcuts." },
                { AppLanguage.Russian, "Это действие удалит Scroll-it с вашего компьютера, включая ярлыки в меню «Пуск» и на Рабочем столе." }
            }},
            { "Uninst_ChkDeleteSettings", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Supprimer également les configurations et profils enregistrés (%APPDATA%\\scroll-it)" },
                { AppLanguage.English, "Also delete saved configurations and presets (%APPDATA%\\scroll-it)" },
                { AppLanguage.Russian, "Также удалить сохраненные профили и настройки (%APPDATA%\\scroll-it)" }
            }},
            { "Uninst_BtnCancel", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Annuler" },
                { AppLanguage.English, "Cancel" },
                { AppLanguage.Russian, "Отмена" }
            }},
            { "Uninst_BtnUninstall", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Désinstaller" },
                { AppLanguage.English, "Uninstall" },
                { AppLanguage.Russian, "Удалить" }
            }},
            { "Uninst_BtnClose", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Fermer" },
                { AppLanguage.English, "Close" },
                { AppLanguage.Russian, "Закрыть" }
            }},
            { "Uninst_ProgressTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Désinstallation en cours..." },
                { AppLanguage.English, "Uninstalling Scroll-it..." },
                { AppLanguage.Russian, "Удаление Scroll-it..." }
            }},
            { "Uninst_ProgressStop", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Arrêt du processus Scroll-it..." },
                { AppLanguage.English, "Stopping Scroll-it processes..." },
                { AppLanguage.Russian, "Остановка процессов Scroll-it..." }
            }},
            { "Uninst_ProgressAutoStart", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Suppression du démarrage automatique..." },
                { AppLanguage.English, "Removing startup entries..." },
                { AppLanguage.Russian, "Удаление из автозагрузки..." }
            }},
            { "Uninst_ProgressShortcuts", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Suppression des raccourcis..." },
                { AppLanguage.English, "Removing shortcuts..." },
                { AppLanguage.Russian, "Удаление ярлыков..." }
            }},
            { "Uninst_ProgressRegistry", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Suppression de l'enregistrement Windows..." },
                { AppLanguage.English, "Removing Windows registration..." },
                { AppLanguage.Russian, "Удаление из реестра Windows..." }
            }},
            { "Uninst_ProgressSettings", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Nettoyage des paramètres utilisateur..." },
                { AppLanguage.English, "Cleaning up user settings..." },
                { AppLanguage.Russian, "Очистка пользовательских настроек..." }
            }},
            { "Uninst_ProgressComplete", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Finalisation de la désinstallation..." },
                { AppLanguage.English, "Finalizing uninstallation..." },
                { AppLanguage.Russian, "Завершение удаления..." }
            }},
            { "Uninst_FinishTitle", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Scroll-it a été entièrement désinstallé." },
                { AppLanguage.English, "Scroll-it has been completely uninstalled." },
                { AppLanguage.Russian, "Scroll-it был полностью удален." }
            }},
            { "Uninst_ErrorGeneral", new Dictionary<AppLanguage, string> {
                { AppLanguage.French, "Erreur lors de la désinstallation : " },
                { AppLanguage.English, "Uninstall error: " },
                { AppLanguage.Russian, "Ошибка при удалении: " }
            }}
        };

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            Dictionary<AppLanguage, string> langDict;
            if (Strings.TryGetValue(key, out langDict))
            {
                string val;
                if (langDict.TryGetValue(_currentLanguage, out val))
                {
                    return val;
                }
                if (langDict.TryGetValue(AppLanguage.English, out val))
                {
                    return val;
                }
                if (langDict.TryGetValue(AppLanguage.French, out val))
                {
                    return val;
                }
            }
            return key;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        public static string GetPresetDescription(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return string.Empty;

            switch (presetName)
            {
                case "Mac OS":
                    return T("Preset_MacOS_Desc");
                case "Snappy":
                    return T("Preset_Snappy_Desc");
                case "Cinematic Glide":
                    return T("Preset_CinematicGlide_Desc");
                case "Ultra Smooth":
                    return T("Preset_UltraSmooth_Desc");
                case "Personnalisé":
                case "Custom":
                case "Пользовательский":
                    return T("Preset_Custom_Desc");
                default:
                    return T("Preset_Custom_Desc");
            }
        }
    }
}
