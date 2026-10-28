using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

[assembly: AssemblyTitle("Veil Setup")]
[assembly: AssemblyDescription("Instalador do Veil")]
[assembly: AssemblyCompany("NetCat Test")]
[assembly: AssemblyProduct("Veil")]
[assembly: AssemblyCopyright("NetCat Test · netcattest.com")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace VeilSetup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Text.Use(args);
            bool uninstall = args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));
            bool quiet = args.Any(a => a.Equals("/silent", StringComparison.OrdinalIgnoreCase));
            if (quiet)
            {
                if (uninstall) Installer.Uninstall(false);
                else Installer.Install(true, null);
                return;
            }
            var app = new Application();
            app.Run(new SetupWindow(uninstall));
        }
    }

    static class Text
    {
        static bool Pt = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";

        public static void Use(string[] args)
        {
            foreach (var a in args)
            {
                if (a.Equals("/lang=en", StringComparison.OrdinalIgnoreCase)) Pt = false;
                if (a.Equals("/lang=pt", StringComparison.OrdinalIgnoreCase)) Pt = true;
            }
        }

        static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            { "version",      new[] { "Versão {0} · NetCat Test", "Version {0} · NetCat Test" } },
            { "intro",        new[] { "A rede Tor no Windows, bonita e simples. A instalação é só para o seu usuário e não pede administrador.",
                                      "The Tor network on Windows, beautifully simple. Installs for your user only, no admin needed." } },
            { "intro_up",     new[] { "Já existe um Veil instalado. Ele será atualizado e suas configurações serão mantidas.",
                                      "Veil is already installed. It will be updated and your settings kept." } },
            { "desktop",      new[] { "Atalho na área de trabalho", "Desktop shortcut" } },
            { "launch",       new[] { "Abrir o Veil ao terminar", "Open Veil when done" } },
            { "install",      new[] { "Instalar", "Install" } },
            { "update",       new[] { "Atualizar", "Update" } },
            { "installing",   new[] { "Instalando…", "Installing…" } },
            { "closing",      new[] { "Fechando o Veil em execução…", "Closing the running Veil…" } },
            { "copying",      new[] { "Copiando arquivos…", "Copying files…" } },
            { "shortcuts",    new[] { "Criando atalhos…", "Creating shortcuts…" } },
            { "done",         new[] { "Pronto! O Veil está no Menu Iniciar.", "Done! Veil is in your Start menu." } },
            { "open",         new[] { "Abrir o Veil", "Open Veil" } },
            { "finish",       new[] { "Concluir", "Finish" } },
            { "failed",       new[] { "Não foi possível concluir: {0}", "Could not finish: {0}" } },
            { "u_intro",      new[] { "Remover o Veil deste computador?", "Remove Veil from this computer?" } },
            { "u_data",       new[] { "Apagar também configurações e cache do Tor", "Also delete settings and Tor cache" } },
            { "u_button",     new[] { "Desinstalar", "Uninstall" } },
            { "u_working",    new[] { "Removendo…", "Removing…" } },
            { "u_done",       new[] { "O Veil foi removido.", "Veil has been removed." } },
            { "close",        new[] { "Fechar", "Close" } },
        };

        public static string S(string key, params object[] args)
        {
            var s = T[key][Pt ? 0 : 1];
            return args.Length > 0 ? string.Format(s, args) : s;
        }
    }

    static class Installer
    {
        public const string Version = "1.1";
        static string Local { get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); } }
        public static string Dir { get { return Path.Combine(Local, "Programs", "Veil"); } }
        public static string Exe { get { return Path.Combine(Dir, "Veil.exe"); } }
        static string Uninstaller { get { return Path.Combine(Dir, "uninstall.exe"); } }
        static string StartMenuLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Veil.lnk"); } }
        static string DesktopLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Veil.lnk"); } }
        static string DataDir { get { return Path.Combine(Local, "Veil"); } }
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Veil";

        public static bool Installed { get { return File.Exists(Exe); } }

        static void CloseRunning()
        {
            foreach (var p in Process.GetProcessesByName("Veil"))
            {
                try
                {
                    string path = "";
                    try { path = p.MainModule.FileName; } catch { }
                    if (path.StartsWith(Dir, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kill();
                        p.WaitForExit(5000);
                    }
                }
                catch { }
            }
        }

        public static void Install(bool desktop, Action<int, string> progress)
        {
            Action<int, string> report = progress ?? ((a, b) => { });
            report(10, Text.S("closing"));
            CloseRunning();

            report(35, Text.S("copying"));
            Directory.CreateDirectory(Dir);
            using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("Veil.exe"))
            using (var f = File.Create(Exe))
                res.CopyTo(f);
            string self = Assembly.GetExecutingAssembly().Location;
            if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(Uninstaller), StringComparison.OrdinalIgnoreCase))
                File.Copy(self, Uninstaller, true);

            report(70, Text.S("shortcuts"));
            Shortcut(StartMenuLink, Exe);
            if (desktop) Shortcut(DesktopLink, Exe);
            else if (File.Exists(DesktopLink)) File.Delete(DesktopLink);

            using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                k.SetValue("DisplayName", "Veil");
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", "NetCat Test");
                k.SetValue("URLInfoAbout", "https://netcattest.com");
                k.SetValue("DisplayIcon", Exe + ",0");
                k.SetValue("InstallLocation", Dir);
                k.SetValue("UninstallString", "\"" + Uninstaller + "\" /uninstall");
                k.SetValue("QuietUninstallString", "\"" + Uninstaller + "\" /uninstall /silent");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)(new FileInfo(Exe).Length / 1024), RegistryValueKind.DWord);
                k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            }
            report(100, Text.S("done"));
        }

        public static void Uninstall(bool removeData)
        {
            CloseRunning();
            foreach (var link in new[] { StartMenuLink, DesktopLink })
                try { if (File.Exists(link)) File.Delete(link); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
            try
            {
                using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    if (run != null) run.DeleteValue("Veil", false);
            }
            catch { }
            try { if (File.Exists(Exe)) File.Delete(Exe); } catch { }
            if (removeData)
                try { if (Directory.Exists(DataDir)) Directory.Delete(DataDir, true); } catch { }

            string self = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            if (self.StartsWith(Path.GetFullPath(Dir), StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + Dir + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetTempPath()
                });
            }
            else
                try { if (Directory.Exists(Dir)) Directory.Delete(Dir, true); } catch { }
        }

        static void Shortcut(string link, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(link));
            var type = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(type);
            object sc = type.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { link });
            var st = sc.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { Path.GetDirectoryName(target) });
            st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { target + ",0" });
            st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "Veil" });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
        }
    }

    class SetupWindow : Window
    {
        readonly FrameworkElement ui;
        readonly bool uninstall;
        bool finished;

        T F<T>(string n) where T : class { return ui.FindName(n) as T; }

        public SetupWindow(bool uninstallMode)
        {
            uninstall = uninstallMode;
            Title = uninstall ? "Veil" : "Veil Setup";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("setup.xaml"))
                ui = (FrameworkElement)XamlReader.Load(s);
            Content = ui;

            using (var ico = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ico;
                img.EndInit();
                F<Image>("Logo").Source = img;
                Icon = img;
            }

            ui.MouseLeftButtonDown += (a, e) => { if (e.ButtonState == MouseButtonState.Pressed) try { DragMove(); } catch { } };
            F<Button>("BtnClose").Click += (a, b) => Close();
            F<Button>("BtnMain").Click += (a, b) => Main();
            F<TextBlock>("TxtVersion").Text = Text.S("version", Installer.Version);
            F<TextBlock>("TxtPath").Text = Installer.Dir;

            if (uninstall)
            {
                F<TextBlock>("TxtIntro").Text = Text.S("u_intro");
                F<TextBlock>("LblOption1").Text = Text.S("u_data");
                F<CheckBox>("Chk1").IsChecked = false;
                F<FrameworkElement>("OptionSep").Visibility = Visibility.Collapsed;
                F<FrameworkElement>("Option2").Visibility = Visibility.Collapsed;
                F<TextBlock>("LblMain").Text = Text.S("u_button");
                F<Button>("BtnMain").Background = new SolidColorBrush(Color.FromRgb(0xD9, 0x3B, 0x33));
            }
            else
            {
                F<TextBlock>("TxtIntro").Text = Text.S(Installer.Installed ? "intro_up" : "intro");
                F<TextBlock>("LblOption1").Text = Text.S("desktop");
                F<TextBlock>("LblOption2").Text = Text.S("launch");
                F<TextBlock>("LblMain").Text = Text.S(Installer.Installed ? "update" : "install");
            }

            Loaded += (a, b) => Intro();
        }

        void Intro()
        {
            var scale = F<ScaleTransform>("LogoScale");
            var pop = new DoubleAnimation(0.6, 1, TimeSpan.FromMilliseconds(520)) { EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut } };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            ui.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)));
        }

        void SetProgress(int value, string status)
        {
            var track = F<Border>("Track");
            track.Opacity = 1;
            F<TextBlock>("TxtStatus").Text = status;
            F<Border>("Fill").BeginAnimation(WidthProperty, new DoubleAnimation(track.ActualWidth * value / 100.0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        void Main()
        {
            var btn = F<Button>("BtnMain");
            if (finished)
            {
                if (!uninstall && F<CheckBox>("Chk2").IsChecked == true)
                    try { Process.Start(Installer.Exe); } catch { }
                Close();
                return;
            }
            btn.IsEnabled = false;
            F<FrameworkElement>("Options").IsEnabled = false;
            bool opt1 = F<CheckBox>("Chk1").IsChecked == true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string error = null;
                try
                {
                    if (uninstall)
                    {
                        Dispatcher.Invoke(new Action(() => SetProgress(40, Text.S("u_working"))));
                        Installer.Uninstall(opt1);
                        Thread.Sleep(300);
                        Dispatcher.Invoke(new Action(() => SetProgress(100, Text.S("u_done"))));
                    }
                    else
                        Installer.Install(opt1, (v, s) => Dispatcher.Invoke(new Action(() => SetProgress(v, s))));
                }
                catch (Exception ex) { error = ex.Message; }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    btn.IsEnabled = true;
                    if (error != null)
                    {
                        F<TextBlock>("TxtStatus").Text = Text.S("failed", error);
                        F<TextBlock>("TxtStatus").Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x80));
                        F<FrameworkElement>("Options").IsEnabled = true;
                        return;
                    }
                    finished = true;
                    btn.Background = new SolidColorBrush(Color.FromRgb(0x30, 0xB1, 0x58));
                    F<TextBlock>("LblMain").Text = uninstall ? Text.S("close")
                        : F<CheckBox>("Chk2").IsChecked == true ? Text.S("open") : Text.S("finish");
                }));
            });
        }
    }
}
