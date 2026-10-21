using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using IOPath = System.IO.Path;
using SD = System.Drawing;
using WF = System.Windows.Forms;

[assembly: AssemblyTitle("Veil")]
[assembly: AssemblyDescription("Controle simples e elegante da rede Tor")]
[assembly: AssemblyCompany("NetCat Test")]
[assembly: AssemblyProduct("Veil")]
[assembly: AssemblyCopyright("NetCat Test · netcattest.com")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Veil
{
    static class Program
    {
        public const string Version = "1.0";

        [STAThread]
        static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "Veil_SingleInstance", out created))
            using (var showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "Veil_Show"))
            {
                if (!created)
                {
                    showSignal.Set();
                    return;
                }
                var settings = Settings.Load();
                I18n.Use(settings.Lang);
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.DispatcherUnhandledException += (s, e) =>
                {
                    MessageBox.Show(e.Exception.ToString(), "Veil", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                MainWindow win;
                try { win = new MainWindow(settings); }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Veil", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var listener = new Thread(() =>
                {
                    while (showSignal.WaitOne())
                        win.Dispatcher.BeginInvoke(new Action(win.ShowFromTray));
                }) { IsBackground = true };
                listener.Start();
                win.Show();
                app.Run();
            }
        }
    }

    static class I18n
    {
        public static string Lang = "pt";

        public static void Use(string setting)
        {
            Lang = setting == "pt" || setting == "en" ? setting
                 : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt" : "en";
        }

        static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            { "off",            new[] { "Desligado", "Disconnected" } },
            { "off_sub",        new[] { "Toque no botão para entrar na rede Tor", "Tap the button to join the Tor network" } },
            { "starting",       new[] { "Conectando…", "Connecting…" } },
            { "on",             new[] { "Conectado", "Connected" } },
            { "on_sub",         new[] { "Proxy SOCKS5 ativo em {0}", "SOCKS5 proxy running on {0}" } },
            { "stopping",       new[] { "Desligando…", "Disconnecting…" } },
            { "preparing",      new[] { "Preparando o Tor…", "Preparing Tor…" } },
            { "via_direct",     new[] { "Conexão direta", "Direct connection" } },
            { "via_bridge",     new[] { "Via ponte {0}", "Through {0} bridge" } },
            { "crashed_sub",    new[] { "O Tor parou — veja o registro", "Tor stopped — check the log" } },
            { "tab_home",       new[] { "Início", "Home" } },
            { "tab_circuit",    new[] { "Circuito", "Circuit" } },
            { "tab_bridges",    new[] { "Pontes", "Bridges" } },
            { "tab_log",        new[] { "Registro", "Log" } },
            { "details_show",   new[] { "Mostrar detalhes", "Show details" } },
            { "details_hide",   new[] { "Ocultar detalhes", "Hide details" } },
            { "exit_mini",      new[] { "SAÍDA", "EXIT" } },
            { "traffic_mini",   new[] { "TRÁFEGO", "TRAFFIC" } },
            { "hdr_conn",       new[] { "CONEXÃO", "CONNECTION" } },
            { "port",           new[] { "Porta SOCKS", "SOCKS port" } },
            { "proxy",          new[] { "Endereço do proxy", "Proxy address" } },
            { "copied",         new[] { "Copiado!", "Copied!" } },
            { "hdr_country",    new[] { "PAÍS DE SAÍDA", "EXIT COUNTRY" } },
            { "auto_country",   new[] { "Automático", "Automatic" } },
            { "auto_country_d", new[] { "O Tor escolhe o melhor caminho", "Tor picks the best path" } },
            { "exits_n",        new[] { "{0} saídas", "{0} exits" } },
            { "exits_known",    new[] { "{0} relays de saída neste país", "{0} exit relays in this country" } },
            { "exits_unknown",  new[] { "Somente saídas deste país", "Only exits from this country" } },
            { "search",         new[] { "Buscar país…", "Search country…" } },
            { "hdr_circuit",    new[] { "CIRCUITO ATUAL", "CURRENT CIRCUIT" } },
            { "newnym",         new[] { "Novo circuito", "New circuit" } },
            { "circuit_empty",  new[] { "Conecte-se para ver o circuito em tempo real", "Connect to see your circuit live" } },
            { "circuit_wait",   new[] { "Aguardando o primeiro circuito…", "Waiting for the first circuit…" } },
            { "role_you",       new[] { "VOCÊ", "YOU" } },
            { "you_title",      new[] { "Este computador", "This computer" } },
            { "role_guard",     new[] { "GUARDA", "GUARD" } },
            { "role_bridge",    new[] { "PONTE", "BRIDGE" } },
            { "role_middle",    new[] { "MEIO", "MIDDLE" } },
            { "role_exit",      new[] { "SAÍDA", "EXIT" } },
            { "role_dest",      new[] { "DESTINO", "DESTINATION" } },
            { "dest_title",     new[] { "Internet", "Internet" } },
            { "dest_detail",    new[] { "Os sites veem o IP da saída", "Websites see the exit's IP" } },
            { "hidden_ip",      new[] { "endereço oculto", "hidden address" } },
            { "hdr_traffic",    new[] { "TRÁFEGO", "TRAFFIC" } },
            { "down",           new[] { "DOWNLOAD", "DOWNLOAD" } },
            { "up",             new[] { "UPLOAD", "UPLOAD" } },
            { "total",          new[] { "{0} no total", "{0} total" } },
            { "hdr_bridge",     new[] { "PONTE", "BRIDGE" } },
            { "none",           new[] { "Sem ponte", "No bridge" } },
            { "none_d",         new[] { "Conexão direta à rede Tor", "Connect directly to Tor" } },
            { "obfs4",          new[] { "obfs4", "obfs4" } },
            { "obfs4_d",        new[] { "Pontes embutidas, tráfego ofuscado", "Built-in bridges, obfuscated traffic" } },
            { "snow",           new[] { "Snowflake", "Snowflake" } },
            { "snow_d",         new[] { "Usa voluntários via WebRTC", "Uses volunteers over WebRTC" } },
            { "meek",           new[] { "meek", "meek" } },
            { "meek_d",         new[] { "Parece tráfego de CDN · mais lento", "Looks like CDN traffic · slower" } },
            { "custom",         new[] { "Personalizada", "Custom" } },
            { "custom_d",       new[] { "Buscar do site ou colar os dados", "Fetch from the site or paste them" } },
            { "hdr_custom",     new[] { "DADOS DA PONTE", "BRIDGE DETAILS" } },
            { "fetch",          new[] { "Buscar do site", "Fetch from site" } },
            { "fetching",       new[] { "Buscando pontes em bridges.torproject.org…", "Fetching bridges from bridges.torproject.org…" } },
            { "fetch_ok",       new[] { "✓ {0} ponte(s) {1} recebidas", "✓ Received {0} {1} bridge(s)" } },
            { "fetch_ok_mix",   new[] { "✓ {0} pontes novas + {1} embutidas de reserva", "✓ {0} new bridges + {1} built-in fallbacks" } },
            { "fetch_fail",     new[] { "Não foi possível buscar: {0}", "Could not fetch: {0}" } },
            { "fetch_none",     new[] { "nenhuma ponte retornada", "no bridges returned" } },
            { "placeholder",    new[] { "Uma ponte por linha, ex.:\nobfs4 1.2.3.4:443 FINGERPRINT cert=… iat-mode=0",
                                        "One bridge per line, e.g.:\nobfs4 1.2.3.4:443 FINGERPRINT cert=… iat-mode=0" } },
            { "open_site",      new[] { "Abrir bridges.torproject.org no navegador", "Open bridges.torproject.org in browser" } },
            { "hdr_log",        new[] { "REGISTRO", "LOG" } },
            { "copy",           new[] { "Copiar", "Copy" } },
            { "clear",          new[] { "Limpar", "Clear" } },
            { "settings",       new[] { "Ajustes", "Settings" } },
            { "hdr_lang",       new[] { "IDIOMA", "LANGUAGE" } },
            { "lang_auto",      new[] { "Sistema", "System" } },
            { "hdr_behavior",   new[] { "COMPORTAMENTO", "BEHAVIOR" } },
            { "tray",           new[] { "Manter na bandeja", "Keep in tray" } },
            { "tray_d",         new[] { "Ao fechar, o Veil continua rodando ao lado do relógio", "When closed, Veil keeps running next to the clock" } },
            { "hdr_about",      new[] { "SOBRE", "ABOUT" } },
            { "about2",         new[] { "Versão {0} · Criado por NetCat Test", "Version {0} · Made by NetCat Test" } },
            { "about3",         new[] { "Tor {0} · Expert Bundle {1}. Nenhuma consulta externa além da busca de pontes.",
                                        "Tor {0} · Expert Bundle {1}. No external requests besides fetching bridges." } },
            { "quit_app",       new[] { "Sair do Veil", "Quit Veil" } },
            { "done",           new[] { "Concluído", "Done" } },
            { "tray_open",      new[] { "Abrir o Veil", "Open Veil" } },
            { "tray_connect",   new[] { "Conectar", "Connect" } },
            { "tray_disconnect",new[] { "Desconectar", "Disconnect" } },
            { "tray_exit",      new[] { "Sair", "Quit" } },
            { "tray_hint",      new[] { "O Veil continua rodando na bandeja.", "Veil is still running in the tray." } },
            { "unknown_country",new[] { "Desconhecido", "Unknown" } },
            { "port_in_use",    new[] { "A porta {0} já está em uso. Escolha outra porta.", "Port {0} is already in use. Pick another port." } },
            { "need_bridge",    new[] { "Cole ou busque pelo menos uma ponte.", "Paste or fetch at least one bridge." } },
            { "log_start_br",   new[] { "Iniciando Tor com {0} ponte(s) {1}", "Starting Tor with {0} {1} bridge(s)" } },
            { "log_start",      new[] { "Iniciando Tor (conexão direta)", "Starting Tor (direct connection)" } },
            { "log_stopped",    new[] { "Tor desligado.", "Tor stopped." } },
            { "log_crashed",    new[] { "Tor encerrou inesperadamente (código {0}).", "Tor exited unexpectedly (code {0})." } },
            { "log_extract",    new[] { "Erro ao extrair o Tor: {0}", "Failed to extract Tor: {0}" } },
            { "log_start_err",  new[] { "Falha ao iniciar tor.exe: {0}", "Failed to start tor.exe: {0}" } },
            { "log_no_builtin", new[] { "Nenhuma ponte embutida encontrada para {0}", "No built-in bridges found for {0}" } },
            { "log_received",   new[] { "Recebidas {0} ponte(s) {1} de bridges.torproject.org", "Received {0} {1} bridge(s) from bridges.torproject.org" } },
            { "log_control",    new[] { "Painel de controle conectado ao Tor.", "Control panel attached to Tor." } },
            { "log_country",    new[] { "País de saída: {0}", "Exit country: {0}" } },
            { "log_newnym",     new[] { "Novo circuito solicitado.", "New circuit requested." } },
            { "log_exits",      new[] { "{0} relays de saída em {1} países.", "{0} exit relays in {1} countries." } },
            { "st_conn",        new[] { "Conectando a um relay", "Connecting to a relay" } },
            { "st_handshake",   new[] { "Negociando conexão segura", "Negotiating secure connection" } },
            { "st_dir",         new[] { "Baixando informações da rede", "Downloading network information" } },
            { "st_desc",        new[] { "Carregando relays", "Loading relays" } },
            { "st_circuit",     new[] { "Construindo circuito", "Building circuit" } },
            { "st_done",        new[] { "Pronto", "Done" } },
        };

        public static string S(string key, params object[] args)
        {
            string[] v;
            string s = T.TryGetValue(key, out v) ? v[Lang == "en" ? 1 : 0] : key;
            return args.Length > 0 ? string.Format(s, args) : s;
        }

        public static string Stage(string tag, string fallback)
        {
            if (tag == "done") return S("st_done");
            if (tag == "circuit_create" || tag.StartsWith("ap_")) return S("st_circuit");
            if (tag.Contains("descriptors") || tag == "enough_dirinfo") return S("st_desc");
            if (tag.Contains("status") || tag.Contains("keys") || tag == "onehop_create") return S("st_dir");
            if (tag.StartsWith("handshake")) return S("st_handshake");
            if (tag.StartsWith("conn") || tag == "starting") return S("st_conn");
            return fallback;
        }
    }

    static class Job
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr attr, string name);
        [DllImport("kernel32.dll")]
        static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint len);
        [DllImport("kernel32.dll")]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [StructLayout(LayoutKind.Sequential)]
        struct BASIC_LIMIT
        {
            public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct IO_COUNTERS { public ulong a, b, c, d, e, f; }
        [StructLayout(LayoutKind.Sequential)]
        struct EXTENDED_LIMIT
        {
            public BASIC_LIMIT Basic;
            public IO_COUNTERS Io;
            public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
        }

        static IntPtr handle = IntPtr.Zero;

        public static void Add(Process p)
        {
            try
            {
                if (handle == IntPtr.Zero)
                {
                    handle = CreateJobObject(IntPtr.Zero, null);
                    var info = new EXTENDED_LIMIT();
                    info.Basic.LimitFlags = 0x2000;
                    int len = Marshal.SizeOf(typeof(EXTENDED_LIMIT));
                    IntPtr ptr = Marshal.AllocHGlobal(len);
                    Marshal.StructureToPtr(info, ptr, false);
                    SetInformationJobObject(handle, 9, ptr, (uint)len);
                    Marshal.FreeHGlobal(ptr);
                }
                AssignProcessToJobObject(handle, p.Handle);
            }
            catch { }
        }
    }

    class Settings
    {
        public int Port = 9050;
        public int BridgeMode = 0;
        public string Transport = "obfs4";
        public string CustomBridges = "";
        public string Lang = "auto";
        public string ExitCountry = "";
        public bool Tray = true;
        public bool TrayHintShown = false;
        public Dictionary<string, int> ExitCounts = new Dictionary<string, int>();

        static string FilePath { get { return IOPath.Combine(TorFiles.Root, "settings.txt"); } }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i < 0) continue;
                    string k = line.Substring(0, i), v = line.Substring(i + 1);
                    switch (k)
                    {
                        case "port": int.TryParse(v, out s.Port); break;
                        case "bridge": int.TryParse(v, out s.BridgeMode); break;
                        case "transport": s.Transport = v == "webtunnel" ? "webtunnel" : "obfs4"; break;
                        case "lang": s.Lang = v == "pt" || v == "en" ? v : "auto"; break;
                        case "exit": s.ExitCountry = Regex.IsMatch(v, "^[a-z]{2}$") ? v : ""; break;
                        case "tray": s.Tray = v != "0"; break;
                        case "trayhint": s.TrayHintShown = v == "1"; break;
                        case "custom": s.CustomBridges = Encoding.UTF8.GetString(Convert.FromBase64String(v)); break;
                        case "exits":
                            foreach (var pair in v.Split(','))
                            {
                                var kv = pair.Split(':');
                                int n;
                                if (kv.Length == 2 && int.TryParse(kv[1], out n)) s.ExitCounts[kv[0]] = n;
                            }
                            break;
                    }
                }
            }
            catch { }
            if (s.Port < 1 || s.Port > 65535) s.Port = 9050;
            if (s.BridgeMode < 0 || s.BridgeMode > 4) s.BridgeMode = 0;
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(TorFiles.Root);
                File.WriteAllLines(FilePath, new[] {
                    "port=" + Port,
                    "bridge=" + BridgeMode,
                    "transport=" + Transport,
                    "lang=" + Lang,
                    "exit=" + ExitCountry,
                    "tray=" + (Tray ? "1" : "0"),
                    "trayhint=" + (TrayHintShown ? "1" : "0"),
                    "exits=" + string.Join(",", ExitCounts.Select(kv => kv.Key + ":" + kv.Value).ToArray()),
                    "custom=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(CustomBridges ?? ""))
                });
            }
            catch { }
        }
    }

    static class Embedded
    {
        public static Stream Open(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }
    }

    static class TorFiles
    {
        public const string BundleVersion = "15.0.26";
        public const string TorVersion = "0.4.9.14";

        public static string Root
        {
            get { return IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veil"); }
        }
        public static string BundleDir { get { return IOPath.Combine(Root, "bundle"); } }
        public static string TorExe { get { return IOPath.Combine(BundleDir, "tor", "tor.exe"); } }
        public static string DataDir { get { return IOPath.Combine(BundleDir, "data"); } }

        public static void EnsureExtracted()
        {
            string stamp = IOPath.Combine(BundleDir, "version.txt");
            if (File.Exists(TorExe) && File.Exists(stamp) && File.ReadAllText(stamp).Trim() == BundleVersion)
                return;

            if (Directory.Exists(BundleDir)) Directory.Delete(BundleDir, true);
            Directory.CreateDirectory(BundleDir);

            using (var res = Embedded.Open("tor.zip"))
            using (var zip = new ZipArchive(res, ZipArchiveMode.Read))
            {
                string rootFull = IOPath.GetFullPath(BundleDir) + IOPath.DirectorySeparatorChar;
                foreach (var entry in zip.Entries)
                {
                    string rel = entry.FullName.Replace('/', IOPath.DirectorySeparatorChar);
                    string dest = IOPath.GetFullPath(IOPath.Combine(BundleDir, rel));
                    if (!dest.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) continue;
                    if (rel.EndsWith(IOPath.DirectorySeparatorChar.ToString())) { Directory.CreateDirectory(dest); continue; }
                    Directory.CreateDirectory(IOPath.GetDirectoryName(dest));
                    entry.ExtractToFile(dest, true);
                }
            }
            File.WriteAllText(stamp, BundleVersion);
        }

        public static List<string> BuiltinBridges(string kind)
        {
            var list = new List<string>();
            try
            {
                string json = File.ReadAllText(IOPath.Combine(BundleDir, "tor", "pluggable_transports", "pt_config.json"));
                var root = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
                var bridges = (Dictionary<string, object>)root["bridges"];
                object arr;
                if (bridges.TryGetValue(kind, out arr))
                    foreach (var o in (object[])arr) list.Add(o.ToString());
            }
            catch { }
            return list;
        }
    }

    static class Countries
    {
        static Dictionary<string, string[]> names;
        static Dictionary<string, byte[]> flags;
        static readonly Dictionary<string, Brush> cache = new Dictionary<string, Brush>();

        static void Load()
        {
            if (names != null) return;
            using (var s = Embedded.Open("countries.json"))
            using (var r = new StreamReader(s, Encoding.UTF8))
            {
                var raw = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(r.ReadToEnd());
                names = raw.ToDictionary(kv => kv.Key, kv => ((object[])kv.Value).Select(o => o.ToString()).ToArray());
            }
            flags = new Dictionary<string, byte[]>();
            using (var s = Embedded.Open("flags.zip"))
            using (var zip = new ZipArchive(s, ZipArchiveMode.Read))
                foreach (var e in zip.Entries)
                    using (var es = e.Open())
                    using (var ms = new MemoryStream())
                    {
                        es.CopyTo(ms);
                        flags[IOPath.GetFileNameWithoutExtension(e.Name)] = ms.ToArray();
                    }
        }

        public static IEnumerable<string> Codes { get { Load(); return names.Keys; } }

        public static bool Known(string cc) { Load(); return cc != null && names.ContainsKey(cc); }

        public static string Name(string cc)
        {
            Load();
            string[] v;
            if (cc != null && names.TryGetValue(cc.ToLowerInvariant(), out v)) return v[I18n.Lang == "en" ? 1 : 0];
            return I18n.S("unknown_country");
        }

        public static Brush Flag(string cc)
        {
            Load();
            cc = (cc ?? "").ToLowerInvariant();
            Brush b;
            if (cache.TryGetValue(cc, out b)) return b;
            byte[] data;
            if (flags.TryGetValue(cc, out data))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = new MemoryStream(data);
                img.EndInit();
                img.Freeze();
                b = new ImageBrush(img) { Stretch = Stretch.UniformToFill };
            }
            else b = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
            b.Freeze();
            cache[cc] = b;
            return b;
        }
    }

    static class BridgeFetcher
    {
        const string Url = "https://bridges.torproject.org/moat/circumvention/defaults";

        public static List<string> Fetch(string transport)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            var req = (HttpWebRequest)WebRequest.Create(Url);
            req.Method = "POST";
            req.ContentType = "application/vnd.api+json";
            req.UserAgent = "Veil/" + Program.Version;
            req.Timeout = 30000;
            byte[] body = Encoding.UTF8.GetBytes("{\"transports\":[\"" + transport + "\"]}");
            using (var s = req.GetRequestStream()) s.Write(body, 0, body.Length);

            string json;
            using (var resp = req.GetResponse())
            using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                json = r.ReadToEnd();

            var root = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
            var fresh = new List<string>();
            var builtin = new List<string>();
            object settingsObj;
            if (root.TryGetValue("settings", out settingsObj) && settingsObj is object[])
            {
                foreach (Dictionary<string, object> setting in (object[])settingsObj)
                {
                    var b = (Dictionary<string, object>)setting["bridges"];
                    if ((string)b["type"] != transport) continue;
                    var target = (b.ContainsKey("source") && (string)b["source"] == "builtin") ? builtin : fresh;
                    foreach (var line in (object[])b["bridge_strings"])
                        if (!target.Contains(line.ToString())) target.Add(line.ToString());
                }
            }
            return fresh.Count > 0 ? fresh : builtin;
        }
    }

    class ControlException : Exception
    {
        public ControlException(string msg) : base(msg) { }
    }

    class ControlClient : IDisposable
    {
        TcpClient tcp;
        StreamReader reader;
        StreamWriter writer;
        readonly object gate = new object();

        public static ControlClient Connect(int port, byte[] cookie)
        {
            var c = new ControlClient();
            c.tcp = new TcpClient();
            c.tcp.Connect(IPAddress.Loopback, port);
            c.tcp.ReceiveTimeout = 15000;
            var s = c.tcp.GetStream();
            c.reader = new StreamReader(s, Encoding.UTF8);
            c.writer = new StreamWriter(s, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
            c.Send("AUTHENTICATE " + BitConverter.ToString(cookie).Replace("-", ""));
            return c;
        }

        public List<string> Send(string command)
        {
            lock (gate)
            {
                writer.WriteLine(command);
                var entries = new List<string>();
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) throw new IOException("control connection closed");
                    if (line.Length < 4) continue;
                    char kind = line[3];
                    string rest = line.Substring(4);
                    if (kind == '+')
                    {
                        var sb = new StringBuilder(rest);
                        string d;
                        while ((d = reader.ReadLine()) != null && d != ".")
                            sb.Append('\n').Append(d.StartsWith("..") ? d.Substring(1) : d);
                        entries.Add(sb.ToString());
                    }
                    else if (kind == '-') entries.Add(rest);
                    else if (kind == ' ')
                    {
                        if (line[0] != '2') throw new ControlException(line);
                        if (rest != "OK") entries.Add(rest);
                        return entries;
                    }
                }
            }
        }

        public Dictionary<string, string> GetInfo(params string[] keys)
        {
            var result = new Dictionary<string, string>();
            foreach (var e in Send("GETINFO " + string.Join(" ", keys)))
            {
                int i = e.IndexOf('=');
                if (i < 0) continue;
                result[e.Substring(0, i)] = e.Substring(i + 1).TrimStart('\n');
            }
            return result;
        }

        public void Dispose()
        {
            try { if (tcp != null) tcp.Close(); } catch { }
        }
    }

    class Relay
    {
        public string Fingerprint, Nickname, Ip, Country;
        public long Bandwidth;
        public bool IsBridge;
    }

    class Circuit
    {
        public string Id;
        public List<Relay> Hops = new List<Relay>();
        public string Key { get { return Id + ":" + string.Join(",", Hops.Select(h => h.Fingerprint).ToArray()); } }
    }

    public class CountryItem
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Sub { get; set; }
        public Brush Flag { get; set; }
    }

    class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        static readonly Color Purple = Color.FromRgb(0x9B, 0x5D, 0xE5);
        static readonly Color PurpleLight = Color.FromRgb(0xD7, 0xA6, 0xFF);
        static readonly Color Green = Color.FromRgb(0x30, 0xD1, 0x58);
        static readonly Color GreenLight = Color.FromRgb(0x9B, 0xF5, 0xB4);
        static readonly Color Orange = Color.FromRgb(0xFF, 0x9F, 0x0A);
        static readonly Color Red = Color.FromRgb(0xFF, 0x45, 0x3A);
        static readonly Color Blue = Color.FromRgb(0x0A, 0x84, 0xFF);
        static readonly Color TorPurple = Color.FromRgb(0x7D, 0x46, 0x98);
        static readonly Color BlobGreen = Color.FromRgb(0x1F, 0x9D, 0x55);

        static readonly string[] BridgeKeys = { null, "obfs4", "snowflake", "meek", null };
        static readonly string[] BridgeLabel = { "", "obfs4", "Snowflake", "meek", "" };

        enum State { Off, Starting, On, Stopping }

        readonly Settings settings;
        readonly FrameworkElement ui;
        Process tor;
        volatile State state = State.Off;
        ScaleTransform rootScale;
        TranslateTransform ShimmerX, BlobShift;
        Rectangle Shimmer;
        Point lastWindowPos;
        bool animatingOut;
        volatile int descTotal = 9000;
        long descStartBytes = -1;
        double smoothDown, smoothUp;
        bool userStopped, crashed, exiting;
        int pct;
        string viaText = "";
        List<string> activeBridges = new List<string>();

        ControlClient control;
        Thread poller;
        volatile bool polling;
        volatile bool circuitDirty;
        bool exitsLoaded;
        string shownCircuit = "";
        readonly Dictionary<string, Relay> relayCache = new Dictionary<string, Relay>();
        readonly List<double> downSamples = new List<double>();
        readonly List<double> upSamples = new List<double>();
        long lastRead = -1, lastWritten = -1;

        WF.NotifyIcon tray;
        Window trayMenu;
        FrameworkElement trayRoot;
        System.Windows.Threading.DispatcherTimer trayTimer;
        ScaleTransform jellyScale;
        SkewTransform jellySkew;
        System.Windows.Threading.DispatcherTimer jellyTimer;
        SD.Icon trayOff, trayBusy, trayOn;

        TextBlock TxtStatus, TxtSub, TxtPct, TxtStage, TxtProxy, TxtFetch, PhBridges, PhSearch, LnkSite;
        TextBlock TxtExitCountry, TxtExitIp, TxtDownMini, TxtUpMini, TxtDown, TxtUp, TxtDownTotal, TxtUpTotal;
        TextBlock TxtCountry, TxtCountrySub, TxtCircuitEmpty;
        TextBox TxtPort, TxtBridges, TxtLog, TxtSearch;
        Border BarTrack, BarFill, Stepper, ExitFlag, CountryFlag, PopupBox;
        Button BtnPower, BtnFetch, CountryBtn, BtnNewnym;
        RadioButton[] bridgeRows, tabs;
        RadioButton SegObfs4, SegWeb, SegAuto, SegPt, SegEn;
        CheckBox ChkTray;
        FrameworkElement CustomPanel, BridgeCard, Overlay, Sheet, OverlayDim, CircuitEmpty;
        FrameworkElement[] pages;
        StackPanel CircuitHost;
        FrameworkElement CountryLayer;
        ScaleTransform PopupScale;
        ListBox CountryList;
        Canvas Spark;
        Polyline SparkDown, SparkUp;
        Polygon SparkFill;
        RotateTransform ChevronRot;
        GradientStop BarStop1, BarStop2;
        GradientStop Blob1Stop;
        readonly List<AnimationClock> ambient = new List<AnimationClock>();
        bool suppressCountryEvent;

        T F<T>(string name) where T : class { return ui.FindName(name) as T; }

        public MainWindow(Settings s)
        {
            settings = s;

            Title = "Veil";
            Width = 440;
            Height = 820;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 52,
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            });
            using (var ico = Embedded.Open("favicon.ico"))
                Icon = BitmapFrame.Create(ico, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            using (var res = Embedded.Open("ui.xaml"))
                ui = (FrameworkElement)XamlReader.Load(res);
            Content = ui;

            Bind();
            Wire();
            SetupTray();
            ApplyLanguage();
            ApplyState(State.Off);
            UpdateCountryButton();
            RenderCircuit(null);
            StartBlobAnimation();

            rootScale = new ScaleTransform(1, 1);
            jellyScale = new ScaleTransform(1, 1);
            jellySkew = new SkewTransform(0, 0);
            ui.RenderTransformOrigin = new Point(0.5, 0.5);
            ui.RenderTransform = new TransformGroup { Children = { rootScale, jellyScale, jellySkew } };
            jellyTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            jellyTimer.Tick += (a, b) => { jellyTimer.Stop(); JellySettle(); };
            ui.Opacity = 0;

            SourceInitialized += (a, b) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, -16, GetWindowLong(hwnd, -16) & ~0x00080000);
                ApplyBackdrop();
                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(WndProc);
            };
            Loaded += (a, b) => { lastWindowPos = new Point(Left, Top); PlaySplash(); };
            IsVisibleChanged += (a, b) => PauseAmbient(!IsVisible);
            StateChanged += (a, b) => PauseAmbient(WindowState == WindowState.Minimized);
            LocationChanged += (a, b) => OnMoved();
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
        {
            if (msg == 0x0231) Lift(true);
            else if (msg == 0x0232) Lift(false);
            return IntPtr.Zero;
        }

        static double PathLength(Geometry g)
        {
            double total = 0;
            foreach (var fig in g.GetFlattenedPathGeometry(0.25, ToleranceType.Absolute).Figures)
            {
                var cur = fig.StartPoint;
                foreach (var seg in fig.Segments)
                {
                    var pts = seg is PolyLineSegment ? ((PolyLineSegment)seg).Points
                            : seg is LineSegment ? new PointCollection { ((LineSegment)seg).Point } : new PointCollection();
                    foreach (var pt in pts) { total += (pt - cur).Length; cur = pt; }
                }
                if (fig.IsClosed) total += (fig.StartPoint - cur).Length;
            }
            return total;
        }

        static DoubleAnimation At(double from, double to, int beginMs, int durMs, IEasingFunction ease = null)
        {
            return new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durMs))
            {
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = ease ?? new CubicEase { EasingMode = EasingMode.EaseOut }
            };
        }

        void PlaySplash()
        {
            var splash = F<FrameworkElement>("Splash");
            var main = F<FrameworkElement>("Main");
            ui.Opacity = 1;
            main.Opacity = 0;
            var back = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };

            var tileScale = F<ScaleTransform>("SpTileScale");
            F<UIElement>("SpTile").BeginAnimation(OpacityProperty, At(0, 1, 0, 260));
            tileScale.BeginAnimation(ScaleTransform.ScaleXProperty, At(0.55, 1, 0, 460, back));
            tileScale.BeginAnimation(ScaleTransform.ScaleYProperty, At(0.55, 1, 0, 460, back));
            F<UIElement>("SpGloss").BeginAnimation(OpacityProperty, At(0, 1, 260, 400));
            F<UIElement>("SplashGlow").BeginAnimation(OpacityProperty, At(0, 1, 0, 700));

            int start = 220;
            foreach (var name in new[] { "SpL1", "SpL2" })
            {
                var path = F<System.Windows.Shapes.Path>(name);
                double dash = PathLength(path.Data) / path.StrokeThickness + 1;
                path.StrokeDashArray = new DoubleCollection { dash, dash };
                path.StrokeDashOffset = dash;
                path.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,
                    At(dash, 0, start, 560, new CubicEase { EasingMode = EasingMode.EaseInOut }));
                start += 170;
            }

            var core = F<ScaleTransform>("SpCoreScale");
            core.BeginAnimation(ScaleTransform.ScaleXProperty, At(0, 1, 720, 360, back));
            core.BeginAnimation(ScaleTransform.ScaleYProperty, At(0, 1, 720, 360, back));
            F<UIElement>("SplashName").BeginAnimation(OpacityProperty, At(0, 1, 820, 320));
            F<TranslateTransform>("SplashNameY").BeginAnimation(TranslateTransform.YProperty, At(10, 0, 820, 380));
            F<UIElement>("SplashBy").BeginAnimation(OpacityProperty, At(0, 1, 950, 320));

            var outScale = new ScaleTransform(1, 1);
            splash.RenderTransformOrigin = new Point(0.5, 0.5);
            splash.RenderTransform = outScale;
            var hide = At(1, 0, 1450, 320, new CubicEase { EasingMode = EasingMode.EaseIn });
            hide.Completed += (a, b) => splash.Visibility = Visibility.Collapsed;
            splash.BeginAnimation(OpacityProperty, hide);
            outScale.BeginAnimation(ScaleTransform.ScaleXProperty, At(1, 1.06, 1450, 320, new CubicEase { EasingMode = EasingMode.EaseIn }));
            outScale.BeginAnimation(ScaleTransform.ScaleYProperty, At(1, 1.06, 1450, 320, new CubicEase { EasingMode = EasingMode.EaseIn }));

            var mainScale = new ScaleTransform(0.94, 0.94);
            main.RenderTransformOrigin = new Point(0.5, 0.5);
            main.RenderTransform = mainScale;
            main.BeginAnimation(OpacityProperty, At(0, 1, 1520, 360));
            mainScale.BeginAnimation(ScaleTransform.ScaleXProperty, At(0.94, 1, 1520, 480, new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }));
            mainScale.BeginAnimation(ScaleTransform.ScaleYProperty, At(0.94, 1, 1520, 480, new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }));
        }

        bool detailsOpen;

        void ToggleDetails()
        {
            detailsOpen = !detailsOpen;
            var panel = F<FrameworkElement>("DetailsPanel");
            var scale = F<ScaleTransform>("DetailsScale");
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            F<RotateTransform>("DetailsRot").BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(detailsOpen ? 180 : 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease });
            F<TextBlock>("LblDetails").Text = I18n.S(detailsOpen ? "details_hide" : "details_show");
            if (detailsOpen)
            {
                panel.Visibility = Visibility.Visible;
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(380)) { EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut } });
                panel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromMilliseconds(60), EasingFunction = ease });
            }
            else
            {
                var shrink = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                shrink.Completed += (a, b) => { if (!detailsOpen) panel.Visibility = Visibility.Collapsed; };
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
                panel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)));
            }
        }

        void AnimateIn()
        {
            var ease = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };
            var grow = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = ease };
            rootScale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            rootScale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            ui.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)));
        }

        void AnimateOut(Action done)
        {
            if (animatingOut) return;
            animatingOut = true;
            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
            var shrink = new DoubleAnimation(1, 0.94, TimeSpan.FromMilliseconds(190)) { EasingFunction = ease };
            rootScale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            rootScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(190)) { EasingFunction = ease };
            fade.Completed += (a, b) => { animatingOut = false; done(); };
            ui.BeginAnimation(OpacityProperty, fade);
        }

        void Lift(bool up)
        {
            var to = up ? 0.975 : 1.0;
            IEasingFunction ease = up ? (IEasingFunction)new CubicEase { EasingMode = EasingMode.EaseOut }
                                      : new ElasticEase { Oscillations = 1, Springiness = 5, EasingMode = EasingMode.EaseOut };
            var a = new DoubleAnimation(to, TimeSpan.FromMilliseconds(up ? 160 : 520)) { EasingFunction = ease };
            rootScale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            rootScale.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            ui.BeginAnimation(OpacityProperty, new DoubleAnimation(up ? 0.9 : 1.0, TimeSpan.FromMilliseconds(up ? 160 : 300)));
        }

        void Jelly(double dx, double dy)
        {
            double skewX = Math.Max(-8, Math.Min(8, -dx * 0.45));
            double skewY = Math.Max(-3, Math.Min(3, -dy * 0.12));
            double stretchX = 1 + Math.Min(0.05, Math.Abs(dx) * 0.0025) - Math.Min(0.03, Math.Abs(dy) * 0.0015);
            double stretchY = 1 + Math.Min(0.05, Math.Abs(dy) * 0.0025) - Math.Min(0.03, Math.Abs(dx) * 0.0015);
            var d = TimeSpan.FromMilliseconds(70);
            jellySkew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(skewX, d));
            jellySkew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(skewY, d));
            jellyScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(stretchX, d));
            jellyScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(stretchY, d));
            jellyTimer.Stop();
            jellyTimer.Start();
        }

        void JellySettle()
        {
            var wobble = new ElasticEase { Oscillations = 3, Springiness = 3, EasingMode = EasingMode.EaseOut };
            var d = TimeSpan.FromMilliseconds(900);
            jellySkew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(0, d) { EasingFunction = wobble });
            jellySkew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(0, d) { EasingFunction = wobble });
            jellyScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, d) { EasingFunction = wobble });
            jellyScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, d) { EasingFunction = wobble });
        }

        void OnMoved()
        {
            var pos = new Point(Left, Top);
            double dx = pos.X - lastWindowPos.X, dy = pos.Y - lastWindowPos.Y;
            lastWindowPos = pos;
            if (Math.Abs(dx) > 200 || Math.Abs(dy) > 200) return;
            Jelly(dx, dy);
            double x = Math.Max(-70, Math.Min(70, BlobShift.X - dx * 0.6));
            double y = Math.Max(-70, Math.Min(70, BlobShift.Y - dy * 0.6));
            var spring = new ElasticEase { Oscillations = 2, Springiness = 4, EasingMode = EasingMode.EaseOut };
            BlobShift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, 0, TimeSpan.FromMilliseconds(1100)) { EasingFunction = spring });
            BlobShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, 0, TimeSpan.FromMilliseconds(1100)) { EasingFunction = spring });
        }

        void Bind()
        {
            TxtStatus = F<TextBlock>("TxtStatus");
            TxtSub = F<TextBlock>("TxtSub");
            TxtPct = F<TextBlock>("TxtPct");
            TxtStage = F<TextBlock>("TxtStage");
            TxtProxy = F<TextBlock>("TxtProxy");
            TxtFetch = F<TextBlock>("TxtFetch");
            PhBridges = F<TextBlock>("PhBridges");
            PhSearch = F<TextBlock>("PhSearch");
            LnkSite = F<TextBlock>("LnkSite");
            TxtExitCountry = F<TextBlock>("TxtExitCountry");
            TxtExitIp = F<TextBlock>("TxtExitIp");
            TxtDownMini = F<TextBlock>("TxtDownMini");
            TxtUpMini = F<TextBlock>("TxtUpMini");
            TxtDown = F<TextBlock>("TxtDown");
            TxtUp = F<TextBlock>("TxtUp");
            TxtDownTotal = F<TextBlock>("TxtDownTotal");
            TxtUpTotal = F<TextBlock>("TxtUpTotal");
            TxtCountry = F<TextBlock>("TxtCountry");
            TxtCountrySub = F<TextBlock>("TxtCountrySub");
            TxtCircuitEmpty = F<TextBlock>("TxtCircuitEmpty");
            TxtPort = F<TextBox>("TxtPort");
            TxtBridges = F<TextBox>("TxtBridges");
            TxtLog = F<TextBox>("TxtLog");
            TxtSearch = F<TextBox>("TxtSearch");
            BarTrack = F<Border>("BarTrack");
            BarFill = F<Border>("BarFill");
            Stepper = F<Border>("Stepper");
            ExitFlag = F<Border>("ExitFlag");
            CountryFlag = F<Border>("CountryFlag");
            PopupBox = F<Border>("PopupBox");
            BtnPower = F<Button>("BtnPower");
            BtnFetch = F<Button>("BtnFetch");
            CountryBtn = F<Button>("CountryBtn");
            BtnNewnym = F<Button>("BtnNewnym");
            SegObfs4 = F<RadioButton>("SegObfs4");
            SegWeb = F<RadioButton>("SegWeb");
            SegAuto = F<RadioButton>("SegAuto");
            SegPt = F<RadioButton>("SegPt");
            SegEn = F<RadioButton>("SegEn");
            ChkTray = F<CheckBox>("ChkTray");
            CustomPanel = F<FrameworkElement>("CustomPanel");
            BridgeCard = F<FrameworkElement>("BridgeCard");
            Overlay = F<FrameworkElement>("Overlay");
            Sheet = F<FrameworkElement>("Sheet");
            OverlayDim = F<FrameworkElement>("OverlayDim");
            CircuitEmpty = F<FrameworkElement>("CircuitEmpty");
            CircuitHost = F<StackPanel>("CircuitHost");
            CountryLayer = F<FrameworkElement>("CountryLayer");
            PopupScale = F<ScaleTransform>("PopupScale");
            CountryList = F<ListBox>("CountryList");
            Spark = F<Canvas>("Spark");
            SparkDown = F<Polyline>("SparkDown");
            SparkUp = F<Polyline>("SparkUp");
            SparkFill = F<Polygon>("SparkFill");
            ChevronRot = F<RotateTransform>("ChevronRot");
            BarStop1 = F<GradientStop>("BarStop1");
            BarStop2 = F<GradientStop>("BarStop2");
            Blob1Stop = F<GradientStop>("Blob1Stop");
            Shimmer = F<Rectangle>("Shimmer");
            ShimmerX = F<TranslateTransform>("ShimmerX");
            BlobShift = F<TranslateTransform>("BlobShift");
            bridgeRows = new[] { F<RadioButton>("RbNone"), F<RadioButton>("RbObfs4"), F<RadioButton>("RbSnow"),
                                 F<RadioButton>("RbMeek"), F<RadioButton>("RbCustom") };
            tabs = new[] { F<RadioButton>("TabHome"), F<RadioButton>("TabCircuit"), F<RadioButton>("TabBridges"), F<RadioButton>("TabLog") };
            pages = new[] { F<FrameworkElement>("PageHome"), F<FrameworkElement>("PageCircuit"),
                            F<FrameworkElement>("PageBridges"), F<FrameworkElement>("PageLog") };
        }

        void ApplyBackdrop()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            bool acrylic = false;
            try
            {
                int build = 0;
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    if (k != null) int.TryParse(Convert.ToString(k.GetValue("CurrentBuild")), out build);

                int dark = 1, round = 2;
                DwmSetWindowAttribute(hwnd, 20, ref dark, 4);
                DwmSetWindowAttribute(hwnd, 33, ref round, 4);
                if (build >= 22621)
                {
                    int backdrop = 3;
                    acrylic = DwmSetWindowAttribute(hwnd, 38, ref backdrop, 4) == 0;
                }
            }
            catch { }

            var src = HwndSource.FromHwnd(hwnd);
            if (acrylic && src != null && src.CompositionTarget != null)
                src.CompositionTarget.BackgroundColor = Colors.Transparent;
            else
                ((Panel)ui).Background = new LinearGradientBrush(Color.FromRgb(0x16, 0x14, 0x22), Color.FromRgb(0x0C, 0x0C, 0x12), 90);
        }

        void StartBlobAnimation()
        {
            var rnd = new Random(7);
            foreach (var name in new[] { "Blob1", "Blob2", "Blob3" })
            {
                var tt = new TranslateTransform();
                F<FrameworkElement>(name).RenderTransform = tt;
                var cx = Drift(60 + rnd.Next(40), 9 + rnd.Next(6)).CreateClock();
                var cy = Drift(50 + rnd.Next(50), 11 + rnd.Next(6)).CreateClock();
                tt.ApplyAnimationClock(TranslateTransform.XProperty, cx);
                tt.ApplyAnimationClock(TranslateTransform.YProperty, cy);
                ambient.Add(cx);
                ambient.Add(cy);
            }
        }

        static DoubleAnimation Drift(double by, double seconds)
        {
            var a = new DoubleAnimation(-by / 2, by / 2, TimeSpan.FromSeconds(seconds))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Timeline.SetDesiredFrameRate(a, 20);
            return a;
        }

        void PauseAmbient(bool pause)
        {
            foreach (var c in ambient)
            {
                if (pause) c.Controller.Pause();
                else c.Controller.Resume();
            }
        }

        void Wire()
        {
            foreach (var n in new[] { "BtnClose", "BtnMin", "BtnSettings" })
                WindowChrome.SetIsHitTestVisibleInChrome(F<IInputElement>(n), true);
            F<Button>("BtnClose").Click += (a, b) => Close();
            F<Button>("BtnMin").Click += (a, b) => WindowState = WindowState.Minimized;
            F<Button>("BtnSettings").Click += (a, b) => ShowSettings(true);
            F<Button>("BtnSheetClose").Click += (a, b) => ShowSettings(false);
            F<Button>("BtnDone").Click += (a, b) => ShowSettings(false);
            F<Button>("BtnQuitApp").Click += (a, b) => ExitApp();
            OverlayDim.MouseLeftButtonUp += (a, b) => ShowSettings(false);

            BtnPower.Click += (a, b) => Toggle();
            F<Button>("BtnDetails").Click += (a, b) => ToggleDetails();

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].Checked += (a, b) => ShowPage(index);
            }
            F<FrameworkElement>("CardExit").MouseLeftButtonUp += (a, b) => tabs[1].IsChecked = true;

            TxtPort.Text = settings.Port.ToString();
            TxtPort.PreviewTextInput += (a, e) => e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
            DataObject.AddPastingHandler(TxtPort, (a, e) =>
            {
                var t = e.DataObject.GetData(typeof(string)) as string;
                if (t == null || !Regex.IsMatch(t, "^[0-9]+$")) e.CancelCommand();
            });
            TxtPort.TextChanged += (a, b) => UpdateProxy();
            TxtPort.LostKeyboardFocus += (a, b) => TxtPort.Text = GetPort().ToString();
            TxtPort.KeyDown += (a, e) =>
            {
                if (e.Key == Key.Up) { TxtPort.Text = Math.Min(65535, GetPort() + 1).ToString(); e.Handled = true; }
                if (e.Key == Key.Down) { TxtPort.Text = Math.Max(1, GetPort() - 1).ToString(); e.Handled = true; }
            };
            F<Button>("BtnPortMinus").Click += (a, b) => TxtPort.Text = Math.Max(1, GetPort() - 1).ToString();
            F<Button>("BtnPortPlus").Click += (a, b) => TxtPort.Text = Math.Min(65535, GetPort() + 1).ToString();
            F<Button>("BtnCopy").Click += (a, b) => CopyProxy();

            bridgeRows[settings.BridgeMode].IsChecked = true;
            foreach (var rb in bridgeRows) rb.Checked += (a, b) => UpdateCustomPanel(true);
            UpdateCustomPanel(false);

            (settings.Transport == "webtunnel" ? SegWeb : SegObfs4).IsChecked = true;
            TxtBridges.Text = settings.CustomBridges;
            TxtBridges.TextChanged += (a, b) => PhBridges.Visibility = TxtBridges.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            PhBridges.Visibility = TxtBridges.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            BtnFetch.Click += (a, b) => FetchBridges();
            LnkSite.MouseLeftButtonUp += (a, b) => OpenUrl("https://bridges.torproject.org/bridges/en?transport=" + Transport());
            F<TextBlock>("LnkNetcat").MouseLeftButtonUp += (a, b) => OpenUrl("https://netcattest.com");

            CountryBtn.Click += (a, b) => OpenCountryPopup();
            CountryLayer.PreviewMouseLeftButtonDown += (a, e) =>
            {
                if (!PopupBox.IsMouseOver) { CloseCountryPopup(); e.Handled = true; }
            };
            CountryLayer.PreviewKeyDown += (a, e) => { if (e.Key == Key.Escape) { CloseCountryPopup(); e.Handled = true; } };
            TxtSearch.TextChanged += (a, b) =>
            {
                PhSearch.Visibility = TxtSearch.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                FillCountryList();
            };
            TxtSearch.PreviewKeyDown += (a, e) =>
            {
                if (e.Key == Key.Down && CountryList.Items.Count > 0)
                {
                    CountryList.SelectedIndex = Math.Max(0, CountryList.SelectedIndex);
                    var item = CountryList.ItemContainerGenerator.ContainerFromIndex(CountryList.SelectedIndex) as ListBoxItem;
                    if (item != null) item.Focus();
                    e.Handled = true;
                }
                if (e.Key == Key.Enter && CountryList.Items.Count > 0) { PickCountry((CountryItem)CountryList.Items[0]); e.Handled = true; }
                if (e.Key == Key.Escape) CloseCountryPopup();
            };
            CountryList.PreviewMouseLeftButtonUp += (a, e) =>
            {
                var item = CountryList.SelectedItem as CountryItem;
                if (item != null) PickCountry(item);
            };
            CountryList.KeyDown += (a, e) =>
            {
                if (e.Key == Key.Enter && CountryList.SelectedItem is CountryItem) PickCountry((CountryItem)CountryList.SelectedItem);
                if (e.Key == Key.Escape) CloseCountryPopup();
            };
            BtnNewnym.Click += (a, b) => NewCircuit();

            F<Button>("BtnLogCopy").Click += (a, b) => { try { Clipboard.SetText(TxtLog.Text); } catch { } };
            F<Button>("BtnLogClear").Click += (a, b) => TxtLog.Clear();

            (settings.Lang == "en" ? SegEn : settings.Lang == "pt" ? SegPt : SegAuto).IsChecked = true;
            SegAuto.Checked += (a, b) => SetLanguage("auto");
            SegPt.Checked += (a, b) => SetLanguage("pt");
            SegEn.Checked += (a, b) => SetLanguage("en");

            ChkTray.IsChecked = settings.Tray;
            ChkTray.Checked += (a, b) => { settings.Tray = true; settings.Save(); };
            ChkTray.Unchecked += (a, b) => { settings.Tray = false; settings.Save(); };

            BarTrack.SizeChanged += (a, b) => SetBar(pct, false);
            Spark.SizeChanged += (a, b) => DrawSpark();
        }

        static void OpenUrl(string url)
        {
            try { Process.Start(url); } catch { }
        }

        void ShowPage(int index)
        {
            CloseCountryPopup();
            for (int i = 0; i < pages.Length; i++)
            {
                if (i != index) { pages[i].Visibility = Visibility.Collapsed; continue; }
                var p = pages[i];
                p.Visibility = Visibility.Visible;
                var tt = new TranslateTransform();
                p.RenderTransform = tt;
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
                p.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
            }
            if (index == 1) circuitDirty = true;
            if (index == 3)
                Dispatcher.BeginInvoke(new Action(() => TxtLog.ScrollToEnd()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        int GetPort()
        {
            int p;
            if (!int.TryParse(TxtPort.Text, out p)) p = settings.Port;
            return Math.Max(1, Math.Min(65535, p));
        }

        string ProxyAddress() { return "127.0.0.1:" + GetPort(); }
        string Transport() { return SegWeb.IsChecked == true ? "webtunnel" : "obfs4"; }

        int BridgeMode()
        {
            for (int i = 0; i < bridgeRows.Length; i++) if (bridgeRows[i].IsChecked == true) return i;
            return 0;
        }

        void UpdateProxy()
        {
            TxtProxy.Text = "socks5://" + ProxyAddress();
        }

        void CopyProxy()
        {
            try { Clipboard.SetText(ProxyAddress()); } catch { return; }
            TxtProxy.Text = I18n.S("copied");
            TxtProxy.Foreground = new SolidColorBrush(Green);
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1300) };
            t.Tick += (a, b) => { t.Stop(); TxtProxy.Foreground = (Brush)ui.FindResource("Secondary"); UpdateProxy(); };
            t.Start();
        }

        void UpdateCustomPanel(bool animate)
        {
            bool show = BridgeMode() == 4;
            if (show && CustomPanel.Visibility != Visibility.Visible)
            {
                CustomPanel.Visibility = Visibility.Visible;
                if (animate) Fade(CustomPanel, 0, 1, 260);
            }
            else if (!show) CustomPanel.Visibility = Visibility.Collapsed;
        }

        static void Fade(UIElement el, double from, double to, int ms, EventHandler done = null)
        {
            var a = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            if (done != null) a.Completed += done;
            el.BeginAnimation(OpacityProperty, a);
        }

        void ShowSettings(bool show)
        {
            var scale = (ScaleTransform)Sheet.RenderTransform;
            if (show)
            {
                Overlay.Visibility = Visibility.Visible;
                Fade(Overlay, 0, 1, 200);
                var pop = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(320)) { EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut } };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            }
            else
            {
                var shrink = new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(160));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
                Fade(Overlay, 1, 0, 160, (a, b) => Overlay.Visibility = Visibility.Collapsed);
            }
        }

        void SetLanguage(string lang)
        {
            settings.Lang = lang;
            I18n.Use(lang);
            settings.Save();
            ApplyLanguage();
            ApplyState(state);
            UpdateCountryButton();
            shownCircuit = "";
            circuitDirty = true;
            if (state == State.Off) RenderCircuit(null);
        }

        void ApplyLanguage()
        {
            var map = new Dictionary<string, string>
            {
                { "LblTabHome", "tab_home" }, { "LblTabCircuit", "tab_circuit" }, { "LblTabBridges", "tab_bridges" }, { "LblTabLog", "tab_log" },
                { "LblExitMini", "exit_mini" }, { "LblTrafficMini", "traffic_mini" },
                { "HdrConn", "hdr_conn" }, { "LblPort", "port" }, { "LblProxy", "proxy" },
                { "HdrCountry", "hdr_country" }, { "HdrCircuit", "hdr_circuit" }, { "LblNewnym", "newnym" },
                { "HdrTraffic", "hdr_traffic" }, { "LblDown", "down" }, { "LblUp", "up" }, { "PhSearch", "search" },
                { "HdrBridge", "hdr_bridge" },
                { "LblNone", "none" }, { "DescNone", "none_d" },
                { "LblObfs4", "obfs4" }, { "DescObfs4", "obfs4_d" },
                { "LblSnow", "snow" }, { "DescSnow", "snow_d" },
                { "LblMeek", "meek" }, { "DescMeek", "meek_d" },
                { "LblCustom", "custom" }, { "DescCustom", "custom_d" },
                { "HdrCustom", "hdr_custom" }, { "LblFetch", "fetch" }, { "PhBridges", "placeholder" },
                { "HdrLog", "hdr_log" }, { "LblLogCopy", "copy" }, { "LblLogClear", "clear" },
                { "TxtSettingsTitle", "settings" }, { "HdrLang", "hdr_lang" },
                { "HdrBehavior", "hdr_behavior" }, { "LblTray", "tray" }, { "DescTray", "tray_d" },
                { "HdrAbout", "hdr_about" }, { "LblDone", "done" }, { "LblQuitApp", "quit_app" },
            };
            foreach (var kv in map)
            {
                var tb = F<TextBlock>(kv.Key);
                if (tb != null) tb.Text = I18n.S(kv.Value);
            }
            SegAuto.Content = I18n.S("lang_auto");
            F<TextBlock>("LblDetails").Text = I18n.S(detailsOpen ? "details_hide" : "details_show");
            F<Run>("LnkSiteText").Text = I18n.S("open_site");
            F<TextBlock>("TxtAbout1").Text = "Veil";
            F<TextBlock>("TxtAbout2").Text = I18n.S("about2", Program.Version);
            F<TextBlock>("TxtAbout3").Text = I18n.S("about3", TorFiles.TorVersion, TorFiles.BundleVersion);
            TxtCircuitEmpty.Text = I18n.S(state == State.Off ? "circuit_empty" : "circuit_wait");
            UpdateProxy();
            UpdateTrayTexts();
        }

        void ApplyState(State s)
        {
            state = s;
            bool off = s == State.Off;
            Stepper.IsEnabled = off;
            BridgeCard.IsEnabled = off;
            CustomPanel.IsEnabled = off;
            BtnPower.IsEnabled = s != State.Stopping;
            BtnNewnym.IsEnabled = s == State.On;

            var glow = new SolidColorBrush(TorPurple) { Opacity = 0.45 };
            Color barA = Purple, barB = PurpleLight, blob = TorPurple;
            switch (s)
            {
                case State.Off:
                    TxtStatus.Text = I18n.S("off");
                    TxtSub.Text = crashed ? I18n.S("crashed_sub") : I18n.S("off_sub");
                    TxtStage.Text = "";
                    BtnPower.Foreground = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF));
                    SetBar(0, true);
                    break;
                case State.Starting:
                    TxtStatus.Text = I18n.S("starting");
                    TxtSub.Text = viaText;
                    glow = new SolidColorBrush(Purple);
                    glow.BeginAnimation(SolidColorBrush.OpacityProperty, new DoubleAnimation(0.25, 0.9, TimeSpan.FromSeconds(1.1))
                    {
                        AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
                    BtnPower.Foreground = new SolidColorBrush(PurpleLight);
                    break;
                case State.On:
                    TxtStatus.Text = I18n.S("on");
                    TxtSub.Text = I18n.S("on_sub", ProxyAddress()) + (viaText.Length > 0 ? "  ·  " + viaText : "");
                    TxtStage.Text = "";
                    glow = new SolidColorBrush(Green) { Opacity = 0.85 };
                    BtnPower.Foreground = new SolidColorBrush(GreenLight);
                    barA = Green; barB = GreenLight; blob = BlobGreen;
                    SetBar(100, true);
                    break;
                case State.Stopping:
                    TxtStatus.Text = I18n.S("stopping");
                    glow = new SolidColorBrush(Orange) { Opacity = 0.5 };
                    BtnPower.Foreground = new SolidColorBrush(Orange);
                    break;
            }
            BtnPower.Background = glow;
            if (s == State.Starting)
            {
                Shimmer.Opacity = 1;
                ShimmerX.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-90, 420, TimeSpan.FromSeconds(1.6))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
            }
            else
            {
                Shimmer.Opacity = 0;
                ShimmerX.BeginAnimation(TranslateTransform.XProperty, null);
            }
            var dur = TimeSpan.FromMilliseconds(700);
            BarStop1.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(barA, dur));
            BarStop2.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(barB, dur));
            Blob1Stop.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(Color.FromArgb(0xA0, blob.R, blob.G, blob.B), TimeSpan.FromMilliseconds(1200)));
            TxtCircuitEmpty.Text = I18n.S(off ? "circuit_empty" : "circuit_wait");
            UpdateTrayTexts();
        }

        void SetBar(int value, bool animate)
        {
            pct = value;
            TxtPct.Text = value + "%";
            double w = Math.Max(0, BarTrack.ActualWidth * value / 100.0);
            if (value > 0) w = Math.Max(w, 10);
            if (animate && BarTrack.ActualWidth > 0)
                BarFill.BeginAnimation(WidthProperty, new DoubleAnimation(w, TimeSpan.FromMilliseconds(500))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            else
            {
                BarFill.BeginAnimation(WidthProperty, null);
                BarFill.Width = w;
            }
        }

        void Log(string line)
        {
            bool follow = TxtLog.ViewportHeight <= 0 || TxtLog.VerticalOffset + TxtLog.ViewportHeight >= TxtLog.ExtentHeight - 24;
            if (TxtLog.Text.Length > 60000) TxtLog.Text = TxtLog.Text.Substring(30000);
            TxtLog.AppendText((TxtLog.Text.Length > 0 ? Environment.NewLine : "") + line);
            if (follow) TxtLog.ScrollToEnd();
        }

        void Ui(Action a)
        {
            try { Dispatcher.BeginInvoke(a); } catch { }
        }

        void SetupTray()
        {
            using (var s = Embedded.Open("favicon.ico"))
            {
                var baseIcon = new SD.Icon(s, WF.SystemInformation.SmallIconSize);
                trayOff = baseIcon;
                trayBusy = Badge(baseIcon, SD.Color.FromArgb(255, 159, 10));
                trayOn = Badge(baseIcon, SD.Color.FromArgb(48, 209, 88));
            }
            tray = new WF.NotifyIcon { Icon = trayOff, Text = "Veil", Visible = true };
            tray.MouseUp += (a, e) =>
            {
                if (e.Button == WF.MouseButtons.Left) ShowFromTray();
                else if (e.Button == WF.MouseButtons.Right) ShowTrayMenu();
            };
            BuildTrayMenu();
        }

        void BuildTrayMenu()
        {
            FrameworkElement root;
            using (var res = Embedded.Open("tray.xaml"))
                root = (FrameworkElement)XamlReader.Load(res);
            trayRoot = root;
            ((Image)root.FindName("Logo")).Source = (ImageSource)ui.FindResource("Logo");
            trayMenu = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowActivated = true,
                Content = root
            };
            trayMenu.Deactivated += (a, b) => HideTrayMenu();
            trayMenu.PreviewKeyDown += (a, e) => { if (e.Key == Key.Escape) HideTrayMenu(); };
            Action<string, Action> on = (name, act) => ((Button)root.FindName(name)).Click += (a, b) => { HideTrayMenu(); act(); };
            on("BtnOpen", ShowFromTray);
            on("BtnToggle", Toggle);
            on("BtnNew", NewCircuit);
            on("BtnQuit", ExitApp);
            trayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            trayTimer.Tick += (a, b) => RefreshTrayMenu();
        }

        void RefreshTrayMenu()
        {
            if (trayRoot == null) return;
            Func<string, TextBlock> t = n => (TextBlock)trayRoot.FindName(n);
            Color dot = state == State.On ? Green : state == State.Off ? Color.FromRgb(0x8E, 0x8E, 0x93) : Orange;
            ((Ellipse)trayRoot.FindName("Dot")).Fill = new SolidColorBrush(dot);
            t("Status").Text = state == State.On ? I18n.S("on") + "  ·  " + ProxyAddress()
                             : state == State.Off ? I18n.S("off")
                             : I18n.S("starting") + "  " + pct + "%";
            bool live = state == State.On && TxtExitCountry.Text != "—";
            ((FrameworkElement)trayRoot.FindName("InfoBox")).Visibility = state == State.On ? Visibility.Visible : Visibility.Collapsed;
            ((Border)trayRoot.FindName("Flag")).Background = live ? ExitFlag.Background : new ImageBrush((ImageSource)ui.FindResource("Logo"));
            t("Exit").Text = live ? TxtExitCountry.Text : I18n.S("circuit_wait");
            t("ExitIp").Text = live ? I18n.S("exit_mini") + "  " + TxtExitIp.Text : " ";
            t("Down").Text = TxtDownMini.Text;
            t("Up").Text = TxtUpMini.Text;
            t("LblOpen").Text = I18n.S("tray_open");
            t("LblToggle").Text = I18n.S(state == State.Off ? "tray_connect" : "tray_disconnect");
            ((Button)trayRoot.FindName("BtnToggle")).IsEnabled = state != State.Stopping;
            t("LblNew").Text = I18n.S("newnym");
            ((Button)trayRoot.FindName("BtnNew")).IsEnabled = state == State.On;
            t("LblQuit").Text = I18n.S("tray_exit");
        }

        void ShowTrayMenu()
        {
            RefreshTrayMenu();
            trayMenu.Opacity = 0;
            trayMenu.Show();
            trayMenu.UpdateLayout();
            var src = PresentationSource.FromVisual(trayMenu);
            double sx = src != null ? src.CompositionTarget.TransformToDevice.M11 : 1;
            double sy = src != null ? src.CompositionTarget.TransformToDevice.M22 : 1;
            var cursor = WF.Cursor.Position;
            var area = WF.Screen.FromPoint(cursor).WorkingArea;
            double w = trayMenu.ActualWidth, h = trayMenu.ActualHeight;
            double x = cursor.X / sx - w + 16, y = cursor.Y / sy - h + 16;
            x = Math.Max(area.Left / sx - 16, Math.Min(x, area.Right / sx - w + 16));
            y = Math.Max(area.Top / sy - 16, Math.Min(y, area.Bottom / sy - h + 16));
            trayMenu.Left = x;
            trayMenu.Top = y;
            trayMenu.Activate();
            var scale = (ScaleTransform)trayRoot.FindName("MenuScale");
            var pop = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut } };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            trayMenu.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
            trayTimer.Start();
        }

        void HideTrayMenu()
        {
            if (!trayMenu.IsVisible) return;
            trayTimer.Stop();
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (a, b) => trayMenu.Hide();
            trayMenu.BeginAnimation(OpacityProperty, fade);
        }

        static SD.Icon Badge(SD.Icon icon, SD.Color color)
        {
            var bmp = icon.ToBitmap();
            using (var g = SD.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias;
                int d = Math.Max(6, bmp.Width / 2 - 1);
                var r = new SD.Rectangle(bmp.Width - d - 1, bmp.Height - d - 1, d, d);
                using (var border = new SD.SolidBrush(SD.Color.FromArgb(30, 20, 40))) g.FillEllipse(border, SD.Rectangle.Inflate(r, 1, 1));
                using (var fill = new SD.SolidBrush(color)) g.FillEllipse(fill, r);
            }
            return SD.Icon.FromHandle(bmp.GetHicon());
        }

        void UpdateTrayTexts()
        {
            if (tray == null) return;
            string status = state == State.On ? I18n.S("on") : state == State.Off ? I18n.S("off") : I18n.S("starting");
            tray.Text = "Veil — " + status;
            tray.Icon = state == State.On ? trayOn : state == State.Off ? trayOff : trayBusy;
            if (trayMenu != null && trayMenu.IsVisible) RefreshTrayMenu();
        }

        public void ShowFromTray()
        {
            bool wasHidden = !IsVisible;
            Show();
            if (wasHidden) AnimateIn();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        void ExitApp()
        {
            exiting = true;
            if (IsVisible) AnimateOut(Close);
            else Close();
        }

        bool closeAnimated;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!exiting && settings.Tray)
            {
                e.Cancel = true;
                AnimateOut(Hide);
                if (!settings.TrayHintShown)
                {
                    settings.TrayHintShown = true;
                    settings.Save();
                    tray.ShowBalloonTip(3000, "Veil", I18n.S("tray_hint"), WF.ToolTipIcon.None);
                }
                return;
            }
            if (!closeAnimated && IsVisible && ui.Opacity > 0.5)
            {
                e.Cancel = true;
                closeAnimated = true;
                exiting = true;
                AnimateOut(Close);
                return;
            }
            SaveSettings();
            StopPolling();
            if (tor != null)
            {
                userStopped = true;
                try { if (!tor.HasExited) { tor.Kill(); tor.WaitForExit(3000); } } catch { }
            }
            var t = tray;
            tray = null;
            t.Visible = false;
            t.Dispose();
            if (trayMenu != null) trayMenu.Close();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        void FetchBridges()
        {
            string transport = Transport();
            BtnFetch.IsEnabled = false;
            TxtFetch.Foreground = (Brush)ui.FindResource("Secondary");
            TxtFetch.Text = I18n.S("fetching");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<string> got = null;
                string err = null;
                int fresh = 0;
                try
                {
                    got = BridgeFetcher.Fetch(transport);
                    fresh = got.Count;
                    TorFiles.EnsureExtracted();
                    if (transport == "obfs4")
                        foreach (var b in TorFiles.BuiltinBridges("obfs4"))
                            if (!got.Contains(b)) got.Add(b);
                }
                catch (Exception ex) { err = ex.Message; }
                Ui(() =>
                {
                    BtnFetch.IsEnabled = true;
                    if (err != null || got == null || got.Count == 0)
                    {
                        TxtFetch.Foreground = new SolidColorBrush(Red);
                        TxtFetch.Text = I18n.S("fetch_fail", err ?? I18n.S("fetch_none"));
                        return;
                    }
                    TxtBridges.Text = string.Join(Environment.NewLine, got.ToArray());
                    TxtFetch.Foreground = new SolidColorBrush(Green);
                    TxtFetch.Text = got.Count > fresh ? I18n.S("fetch_ok_mix", fresh, got.Count - fresh) : I18n.S("fetch_ok", got.Count, transport);
                    Log(I18n.S("log_received", fresh, transport));
                });
            });
        }

        static string NormalizeBridge(string line)
        {
            line = Regex.Replace(line.Trim(), @"\s+", " ");
            if (line.StartsWith("Bridge ", StringComparison.OrdinalIgnoreCase)) line = line.Substring(7).Trim();
            return line;
        }

        static string TransportOf(string bridge)
        {
            string first = bridge.Split(' ')[0];
            return Regex.IsMatch(first, @"^[\d\[]") ? "vanilla" : first;
        }

        void OpenCountryPopup()
        {
            TxtSearch.Text = "";
            FillCountryList();
            var origin = CountryBtn.TranslatePoint(new Point(0, CountryBtn.ActualHeight), ui);
            PopupBox.Width = CountryBtn.ActualWidth;
            PopupBox.Margin = new Thickness(origin.X, origin.Y + 2, 0, 0);
            CountryLayer.Visibility = Visibility.Visible;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            PopupBox.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
            PopupScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
            ChevronRot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(180, TimeSpan.FromMilliseconds(200)));
            Dispatcher.BeginInvoke(new Action(() => { TxtSearch.Focus(); Keyboard.Focus(TxtSearch); }), System.Windows.Threading.DispatcherPriority.Input);
        }

        void CloseCountryPopup()
        {
            if (CountryLayer.Visibility != Visibility.Visible) return;
            ChevronRot.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)));
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(140));
            fade.Completed += (a, b) => CountryLayer.Visibility = Visibility.Collapsed;
            PopupBox.BeginAnimation(OpacityProperty, fade);
            PopupScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, TimeSpan.FromMilliseconds(140)));
        }

        static string Fold(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().ToLowerInvariant();
        }

        void FillCountryList()
        {
            string q = Fold(TxtSearch.Text.Trim());
            var items = new List<CountryItem>();
            var logo = new ImageBrush((ImageSource)ui.FindResource("Logo")) { Stretch = Stretch.UniformToFill };
            if (q.Length == 0)
                items.Add(new CountryItem { Code = "", Name = I18n.S("auto_country"), Sub = "", Flag = logo });

            bool haveCounts = settings.ExitCounts.Count > 0;
            var codes = Countries.Codes.Where(c => !haveCounts || settings.ExitCounts.ContainsKey(c) || c == settings.ExitCountry);
            var list = codes.Select(c =>
            {
                int n;
                settings.ExitCounts.TryGetValue(c, out n);
                return new { Code = c, Name = Countries.Name(c), Count = n };
            })
            .Where(x => q.Length == 0 || Fold(x.Name).Contains(q) || x.Code == q)
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.CurrentCulture);

            foreach (var x in list)
                items.Add(new CountryItem
                {
                    Code = x.Code,
                    Name = x.Name,
                    Sub = x.Count > 0 ? I18n.S("exits_n", x.Count) : "",
                    Flag = Countries.Flag(x.Code)
                });

            suppressCountryEvent = true;
            CountryList.ItemsSource = items;
            CountryList.SelectedItem = items.FirstOrDefault(i => i.Code == settings.ExitCountry);
            if (CountryList.SelectedItem != null) CountryList.ScrollIntoView(CountryList.SelectedItem);
            suppressCountryEvent = false;
        }

        void PickCountry(CountryItem item)
        {
            if (suppressCountryEvent || item == null) return;
            CloseCountryPopup();
            if (item.Code == settings.ExitCountry) return;
            settings.ExitCountry = item.Code;
            settings.Save();
            UpdateCountryButton();
            Log(I18n.S("log_country", item.Code.Length == 0 ? I18n.S("auto_country") : Countries.Name(item.Code)));
            var c = control;
            if (c == null) return;
            string code = item.Code;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (code.Length == 0) c.Send("RESETCONF ExitNodes StrictNodes");
                    else c.Send("SETCONF ExitNodes={" + code + "} StrictNodes=1");
                    c.Send("SIGNAL NEWNYM");
                    Thread.Sleep(2500);
                    circuitDirty = true;
                }
                catch (Exception ex) { Ui(() => Log(ex.Message)); }
            });
        }

        void UpdateCountryButton()
        {
            string cc = settings.ExitCountry;
            if (cc.Length == 0)
            {
                TxtCountry.Text = I18n.S("auto_country");
                TxtCountrySub.Text = I18n.S("auto_country_d");
                CountryFlag.Background = new ImageBrush((ImageSource)ui.FindResource("Logo")) { Stretch = Stretch.UniformToFill };
                return;
            }
            int n;
            TxtCountry.Text = Countries.Name(cc);
            TxtCountrySub.Text = settings.ExitCounts.TryGetValue(cc, out n) ? I18n.S("exits_known", n) : I18n.S("exits_unknown");
            CountryFlag.Background = Countries.Flag(cc);
        }

        void NewCircuit()
        {
            var c = control;
            if (c == null) return;
            BtnNewnym.IsEnabled = false;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { c.Send("SIGNAL NEWNYM"); Ui(() => Log(I18n.S("log_newnym"))); } catch { }
                Thread.Sleep(2500);
                circuitDirty = true;
                Thread.Sleep(5000);
                Ui(() => BtnNewnym.IsEnabled = state == State.On);
            });
        }

        void Toggle()
        {
            if (state == State.Off) StartTor();
            else if (state != State.Stopping) StopTor();
        }

        static bool PortFree(int port)
        {
            try
            {
                var l = new TcpListener(IPAddress.Loopback, port);
                l.Start();
                l.Stop();
                return true;
            }
            catch { return false; }
        }

        void SaveSettings()
        {
            settings.Port = GetPort();
            settings.BridgeMode = BridgeMode();
            settings.Transport = Transport();
            settings.CustomBridges = TxtBridges.Text;
            settings.Save();
        }

        void StartTor()
        {
            int port = GetPort();
            int mode = BridgeMode();
            TxtPort.Text = port.ToString();
            SaveSettings();
            crashed = false;

            if (!PortFree(port))
            {
                ShowFromTray();
                MessageBox.Show(this, I18n.S("port_in_use", port), "Veil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bridges = new List<string>();
            if (mode == 4)
            {
                foreach (var l in TxtBridges.Text.Split('\n'))
                {
                    string b = NormalizeBridge(l);
                    if (b.Length > 0 && !b.StartsWith("#")) bridges.Add(b);
                }
                if (bridges.Count == 0)
                {
                    ShowFromTray();
                    tabs[2].IsChecked = true;
                    MessageBox.Show(this, I18n.S("need_bridge"), "Veil", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            TxtLog.Clear();
            viaText = "";
            descTotal = 9000;
            descStartBytes = -1;
            ResetLiveViews();
            ApplyState(State.Starting);
            TxtStage.Text = I18n.S("preparing");
            SetBar(0, true);

            try { TorFiles.EnsureExtracted(); }
            catch (Exception ex)
            {
                Log(I18n.S("log_extract", ex.Message));
                ApplyState(State.Off);
                return;
            }

            if (BridgeKeys[mode] != null)
            {
                bridges = TorFiles.BuiltinBridges(BridgeKeys[mode]);
                if (bridges.Count == 0)
                {
                    Log(I18n.S("log_no_builtin", BridgeKeys[mode]));
                    ApplyState(State.Off);
                    return;
                }
            }
            activeBridges = bridges;

            string label = mode == 4 ? TransportOf(bridges[0]) : BridgeLabel[mode];
            viaText = bridges.Count > 0 ? I18n.S("via_bridge", label) : I18n.S("via_direct");
            TxtSub.Text = viaText;

            Directory.CreateDirectory(TorFiles.DataDir);
            foreach (var f in new[] { "control.port", "control.cookie" })
                try { File.Delete(IOPath.Combine(TorFiles.DataDir, f)); } catch { }

            var rc = new StringBuilder();
            Func<string, string> abs = rel => "\"" + IOPath.GetFullPath(IOPath.Combine(TorFiles.BundleDir, rel)).Replace("\\", "\\\\") + "\"";
            rc.AppendLine("DataDirectory " + abs("data"));
            rc.AppendLine("GeoIPFile " + abs("data/geoip"));
            rc.AppendLine("GeoIPv6File " + abs("data/geoip6"));
            rc.AppendLine("SocksPort 127.0.0.1:" + port);
            rc.AppendLine("ControlPort auto");
            rc.AppendLine("ControlPortWriteToFile " + abs("data/control.port"));
            rc.AppendLine("CookieAuthentication 1");
            rc.AppendLine("CookieAuthFile " + abs("data/control.cookie"));
            rc.AppendLine("__OwningControllerProcess " + Process.GetCurrentProcess().Id);
            rc.AppendLine("Log notice stdout");
            rc.AppendLine("AvoidDiskWrites 1");
            if (settings.ExitCountry.Length > 0)
            {
                rc.AppendLine("ExitNodes {" + settings.ExitCountry + "}");
                rc.AppendLine("StrictNodes 1");
            }
            if (bridges.Count > 0)
            {
                rc.AppendLine("UseBridges 1");
                rc.AppendLine("ClientTransportPlugin meek_lite,obfs2,obfs3,obfs4,scramblesuit,webtunnel exec tor\\pluggable_transports\\lyrebird.exe");
                rc.AppendLine("ClientTransportPlugin snowflake exec tor\\pluggable_transports\\lyrebird.exe");
                rc.AppendLine("ClientTransportPlugin conjure exec tor\\pluggable_transports\\conjure-client.exe -registerURL https://registration.refraction.network/api");
                foreach (var b in bridges) rc.AppendLine("Bridge " + b);
            }
            File.WriteAllText(IOPath.Combine(TorFiles.BundleDir, "torrc"), rc.ToString());

            Log(bridges.Count > 0 ? I18n.S("log_start_br", bridges.Count, label) : I18n.S("log_start"));

            var psi = new ProcessStartInfo
            {
                FileName = TorFiles.TorExe,
                Arguments = "-f torrc --defaults-torrc NUL",
                WorkingDirectory = TorFiles.BundleDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            try
            {
                userStopped = false;
                var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                p.OutputDataReceived += (a, e) => { if (e.Data != null) Ui(() => OnTorLine(e.Data)); };
                p.ErrorDataReceived += (a, e) => { if (e.Data != null) Ui(() => Log(e.Data)); };
                p.Exited += (a, e) => Ui(() => OnTorExited(p));
                tor = p;
                p.Start();
                Job.Add(p);
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                StartPolling();
            }
            catch (Exception ex)
            {
                Log(I18n.S("log_start_err", ex.Message));
                tor = null;
                ApplyState(State.Off);
            }
        }

        void OnTorLine(string line)
        {
            var m = Regex.Match(line, @"^\w{3} \d{2} [\d:.]+ \[(\w+)\] (.*)$");
            string msg = m.Success ? m.Groups[2].Value : line;
            Log(m.Success && m.Groups[1].Value != "notice" ? "[" + m.Groups[1].Value + "] " + msg : msg);

            var d = Regex.Match(msg, @"we have \d+/(\d+)");
            if (d.Success) int.TryParse(d.Groups[1].Value, out descTotal);

            var b = Regex.Match(msg, @"Bootstrapped (\d+)% \(([^)]*)\): (.*)");
            if (b.Success && state == State.Starting)
            {
                int value = int.Parse(b.Groups[1].Value);
                if (value >= pct || value >= 100)
                {
                    TxtStage.Text = I18n.Stage(b.Groups[2].Value, b.Groups[3].Value);
                    SetBar(Math.Min(100, value), true);
                }
                if (value >= 100)
                {
                    ApplyState(State.On);
                    circuitDirty = true;
                }
            }
        }

        void OnTorExited(Process p)
        {
            if (p != tor || tray == null) return;
            int code = -1;
            try { code = p.ExitCode; } catch { }
            tor = null;
            crashed = !userStopped;
            StopPolling();
            Log(crashed ? I18n.S("log_crashed", code) : I18n.S("log_stopped"));
            ApplyState(State.Off);
            ResetLiveViews();
        }

        void StopTor()
        {
            if (tor == null) { ApplyState(State.Off); return; }
            userStopped = true;
            ApplyState(State.Stopping);
            StopPolling();
            var p = tor;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { if (!p.HasExited) { p.Kill(); p.WaitForExit(5000); } } catch { }
            });
        }

        void ResetLiveViews()
        {
            downSamples.Clear();
            upSamples.Clear();
            lastRead = lastWritten = -1;
            smoothDown = smoothUp = 0;
            TxtDown.Text = TxtUp.Text = TxtDownMini.Text = TxtUpMini.Text = "0 B/s";
            TxtDownTotal.Text = TxtUpTotal.Text = "";
            DrawSpark();
            RenderCircuit(null);
        }

        void StartPolling()
        {
            StopPolling();
            polling = true;
            exitsLoaded = false;
            shownCircuit = "";
            poller = new Thread(PollLoop) { IsBackground = true };
            poller.Start();
        }

        void StopPolling()
        {
            polling = false;
            var c = control;
            control = null;
            if (c != null) c.Dispose();
        }

        void PollLoop()
        {
            var clock = Stopwatch.StartNew();
            double lastTraffic = -1, lastCircuit = -10, lastBoot = -1;
            while (polling)
            {
                try
                {
                    if (control == null)
                    {
                        var c = TryConnect();
                        if (c != null)
                        {
                            control = c;
                            lastTraffic = -1;
                            Ui(() => Log(I18n.S("log_control")));
                        }
                        else if (clock.Elapsed.TotalSeconds > 60) return;
                    }
                    var ctl = control;
                    if (ctl != null)
                    {
                        double now = clock.Elapsed.TotalSeconds;
                        bool on = state == State.On;
                        if (state == State.Starting && now - lastBoot >= 0.5)
                        {
                            lastBoot = now;
                            PollBootstrap(ctl);
                        }
                        if (lastTraffic < 0 || now - lastTraffic >= 1)
                        {
                            PollTraffic(ctl, lastTraffic < 0 ? 0 : now - lastTraffic);
                            lastTraffic = now;
                        }
                        if (on && (circuitDirty || now - lastCircuit >= 4))
                        {
                            circuitDirty = false;
                            lastCircuit = now;
                            var circ = ReadCircuit(ctl);
                            Ui(() => RenderCircuit(circ));
                        }
                        if (on && !exitsLoaded)
                        {
                            exitsLoaded = true;
                            ThreadPool.QueueUserWorkItem(_ => { try { LoadExitCounts(ctl); } catch { } });
                        }
                    }
                }
                catch (Exception)
                {
                    var c = control;
                    control = null;
                    if (c != null) c.Dispose();
                }
                Thread.Sleep(150);
            }
        }

        void PollBootstrap(ControlClient ctl)
        {
            var phase = ctl.GetInfo("status/bootstrap-phase")["status/bootstrap-phase"];
            var m = Regex.Match(phase, @"PROGRESS=(\d+) TAG=(\S+) SUMMARY=""([^""]*)""");
            if (!m.Success) return;
            int value = int.Parse(m.Groups[1].Value);
            string tag = m.Groups[2].Value, summary = m.Groups[3].Value;
            string extra = "";
            if (tag == "loading_descriptors" && lastRead >= 0)
            {
                if (descStartBytes < 0) descStartBytes = lastRead;
                long got = lastRead - descStartBytes;
                long expected = Math.Max(1, descTotal) * 560L;
                int estimate = 50 + (int)Math.Min(24, 25.0 * got / expected);
                value = Math.Max(value, estimate);
                extra = "  ·  " + Bytes(got);
            }
            Ui(() =>
            {
                if (state != State.Starting || value < pct || value >= 100) return;
                SetBar(value, true);
                TxtStage.Text = I18n.Stage(tag, summary) + extra;
            });
        }

        ControlClient TryConnect()
        {
            string portFile = IOPath.Combine(TorFiles.DataDir, "control.port");
            string cookieFile = IOPath.Combine(TorFiles.DataDir, "control.cookie");
            if (!File.Exists(portFile) || !File.Exists(cookieFile)) return null;
            try
            {
                var m = Regex.Match(File.ReadAllText(portFile), @"PORT=[\d.]+:(\d+)");
                if (!m.Success) return null;
                byte[] cookie = File.ReadAllBytes(cookieFile);
                if (cookie.Length != 32) return null;
                return ControlClient.Connect(int.Parse(m.Groups[1].Value), cookie);
            }
            catch { return null; }
        }

        void PollTraffic(ControlClient ctl, double seconds)
        {
            var info = ctl.GetInfo("traffic/read", "traffic/written");
            long read = long.Parse(info["traffic/read"]), written = long.Parse(info["traffic/written"]);
            bool first = lastRead < 0 || seconds <= 0;
            double down = first ? 0 : Math.Max(0, read - lastRead) / seconds;
            double up = first ? 0 : Math.Max(0, written - lastWritten) / seconds;
            lastRead = read;
            lastWritten = written;
            if (first) return;
            Ui(() =>
            {
                smoothDown = smoothDown * 0.4 + down * 0.6;
                smoothUp = smoothUp * 0.4 + up * 0.6;
                downSamples.Add(smoothDown);
                upSamples.Add(smoothUp);
                while (downSamples.Count > 60) downSamples.RemoveAt(0);
                while (upSamples.Count > 60) upSamples.RemoveAt(0);
                TxtDown.Text = TxtDownMini.Text = Bytes(smoothDown) + "/s";
                TxtUp.Text = TxtUpMini.Text = Bytes(smoothUp) + "/s";
                TxtDownTotal.Text = I18n.S("total", Bytes(read));
                TxtUpTotal.Text = I18n.S("total", Bytes(written));
                DrawSpark();
            });
        }

        static string Bytes(double b)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int u = 0;
            while (b >= 1024 && u < units.Length - 1) { b /= 1024; u++; }
            return (u == 0 ? b.ToString("0") : b.ToString(b < 10 ? "0.0" : "0")) + " " + units[u];
        }

        void DrawSpark()
        {
            double w = Spark.ActualWidth, h = Spark.Height;
            SparkDown.Points = new PointCollection();
            SparkUp.Points = new PointCollection();
            SparkFill.Points = new PointCollection();
            if (w <= 0 || downSamples.Count < 2) return;
            double max = Math.Max(2048, Math.Max(downSamples.Max(), upSamples.Max())) * 1.15;
            double step = w / 59.0;
            double x0 = w - step * (downSamples.Count - 1);
            var dp = new PointCollection();
            var upp = new PointCollection();
            for (int i = 0; i < downSamples.Count; i++)
            {
                dp.Add(new Point(x0 + i * step, h - 2 - downSamples[i] / max * (h - 6)));
                upp.Add(new Point(x0 + i * step, h - 2 - upSamples[i] / max * (h - 6)));
            }
            var fill = new PointCollection(dp) { new Point(w, h), new Point(x0, h) };
            SparkDown.Points = dp;
            SparkUp.Points = upp;
            SparkFill.Points = fill;
        }

        Circuit ReadCircuit(ControlClient ctl)
        {
            var info = ctl.GetInfo("circuit-status", "stream-status");
            var circuits = new Dictionary<string, string[]>();
            string best = null;
            int bestId = -1;
            foreach (var line in info["circuit-status"].Split('\n'))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length < 3 || parts[1] != "BUILT") continue;
                if (!line.Contains("PURPOSE=GENERAL") || line.Contains("IS_INTERNAL") || line.Contains("ONEHOP_TUNNEL")) continue;
                var hops = parts[2].Split(',');
                if (hops.Length < 3) continue;
                circuits[parts[0]] = hops;
                int id;
                if (int.TryParse(parts[0], out id) && id > bestId) { bestId = id; best = parts[0]; }
            }
            string stream;
            if (info.TryGetValue("stream-status", out stream))
                foreach (var line in stream.Split('\n').Reverse())
                {
                    var parts = line.Trim().Split(' ');
                    if (parts.Length >= 3 && parts[1] == "SUCCEEDED" && circuits.ContainsKey(parts[2])) { best = parts[2]; break; }
                }
            if (best == null) return null;

            var circ = new Circuit { Id = best };
            for (int i = 0; i < circuits[best].Length; i++)
            {
                var hop = circuits[best][i];
                var m = Regex.Match(hop, @"^\$?([0-9A-Fa-f]{40})(?:[~=](.+))?$");
                if (!m.Success) continue;
                string fp = m.Groups[1].Value.ToUpperInvariant();
                Relay r;
                if (!relayCache.TryGetValue(fp, out r))
                {
                    r = LookupRelay(ctl, fp, m.Groups[2].Value, i == 0);
                    relayCache[fp] = r;
                }
                circ.Hops.Add(r);
            }
            return circ;
        }

        Relay LookupRelay(ControlClient ctl, string fp, string nick, bool first)
        {
            var r = new Relay { Fingerprint = fp, Nickname = nick.Length > 0 ? nick : fp.Substring(0, 8), Ip = "", Country = "" };
            try
            {
                var ns = ctl.GetInfo("ns/id/" + fp)["ns/id/" + fp];
                foreach (var line in ns.Split('\n'))
                {
                    var p = line.Trim().Split(' ');
                    if (p[0] == "r" && p.Length >= 7) { r.Nickname = p[1]; r.Ip = p[6]; }
                    if (p[0] == "w")
                        foreach (var kv in p)
                            if (kv.StartsWith("Bandwidth=")) long.TryParse(kv.Substring(10), out r.Bandwidth);
                }
            }
            catch (ControlException)
            {
                if (first)
                {
                    var bridge = activeBridges.FirstOrDefault(b => b.ToUpperInvariant().Contains(fp));
                    if (bridge != null)
                    {
                        r.IsBridge = true;
                        var parts = bridge.Split(' ');
                        string addr = Regex.IsMatch(parts[0], @"^[\d\[]") ? parts[0] : parts.Length > 1 ? parts[1] : "";
                        var am = Regex.Match(addr, @"^\[?([^\]]+?)\]?:\d+$");
                        r.Ip = am.Success ? am.Groups[1].Value : "";
                        if (r.Ip.StartsWith("192.0.2.") || r.Ip.StartsWith("2001:db8", StringComparison.OrdinalIgnoreCase)) r.Ip = "";
                        r.Nickname = TransportOf(bridge);
                    }
                }
            }
            if (r.Ip.Length > 0)
            {
                try
                {
                    string cc = ctl.GetInfo("ip-to-country/" + r.Ip)["ip-to-country/" + r.Ip];
                    if (Regex.IsMatch(cc, "^[a-z]{2}$")) r.Country = cc;
                }
                catch { }
            }
            return r;
        }

        void LoadExitCounts(ControlClient ctl)
        {
            var all = ctl.GetInfo("ns/all")["ns/all"];
            var exits = new List<string>();
            string ip = null;
            foreach (var raw in all.Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("r "))
                {
                    var p = line.Split(' ');
                    ip = p.Length >= 7 ? p[6] : null;
                }
                else if (line.StartsWith("s ") && ip != null)
                {
                    var flags = line.Split(' ');
                    if (flags.Contains("Exit") && !flags.Contains("BadExit")) exits.Add(ip);
                    ip = null;
                }
            }
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < exits.Count; i += 200)
            {
                var keys = exits.Skip(i).Take(200).Select(x => "ip-to-country/" + x).ToArray();
                foreach (var kv in ctl.GetInfo(keys))
                {
                    if (!Regex.IsMatch(kv.Value, "^[a-z]{2}$") || !Countries.Known(kv.Value)) continue;
                    int n;
                    counts.TryGetValue(kv.Value, out n);
                    counts[kv.Value] = n + 1;
                }
            }
            if (counts.Count == 0) return;
            Ui(() =>
            {
                settings.ExitCounts = counts;
                settings.Save();
                UpdateCountryButton();
                Log(I18n.S("log_exits", counts.Values.Sum(), counts.Count));
            });
        }

        void RenderCircuit(Circuit circ)
        {
            if (circ == null)
            {
                if (state == State.Off || CircuitHost.Children.Count == 0)
                {
                    CircuitHost.Children.Clear();
                    CircuitEmpty.Visibility = Visibility.Visible;
                    shownCircuit = "";
                    ExitFlag.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
                    TxtExitCountry.Text = "—";
                    TxtExitIp.Text = " ";
                }
                return;
            }
            if (circ.Key == shownCircuit) return;
            shownCircuit = circ.Key;
            CircuitEmpty.Visibility = Visibility.Collapsed;
            CircuitHost.Children.Clear();

            var exit = circ.Hops.Last();
            ExitFlag.Background = Countries.Flag(exit.Country);
            TxtExitCountry.Text = exit.Country.Length > 0 ? Countries.Name(exit.Country) : exit.Nickname;
            TxtExitIp.Text = exit.Ip.Length > 0 ? exit.Ip : " ";

            var rows = new List<UIElement>();
            rows.Add(Node(I18n.S("role_you"), I18n.S("you_title"), "socks5://" + ProxyAddress(), null, "M4,5 H20 A1,1 0 0 1 21,6 V15 A1,1 0 0 1 20,16 H4 A1,1 0 0 1 3,15 V6 A1,1 0 0 1 4,5 Z M8,20 H16 M12,16 V20", Blue, 0, false, ""));
            for (int i = 0; i < circ.Hops.Count; i++)
            {
                var h = circ.Hops[i];
                bool last = i == circ.Hops.Count - 1;
                string role = i == 0 ? I18n.S(h.IsBridge ? "role_bridge" : "role_guard") : last ? I18n.S("role_exit") : I18n.S("role_middle");
                Color dot = i == 0 ? Purple : last ? Green : Color.FromRgb(0x64, 0xD2, 0xFF);
                string place = h.Country.Length > 0 ? Countries.Name(h.Country) : I18n.S("unknown_country");
                string detail = (h.Ip.Length > 0 ? h.Ip : I18n.S("hidden_ip")) + "  ·  " + place;
                string bw = h.Bandwidth > 0 ? Bytes(h.Bandwidth * 1024.0) + "/s" : "";
                rows.Add(Node(role, h.Nickname, detail, h.Country.Length > 0 ? Countries.Flag(h.Country) : null,
                    "M12,3 A9,9 0 1 0 12.01,3 Z M3,12 H21 M12,3 C8.5,7 8.5,17 12,21 M12,3 C15.5,7 15.5,17 12,21", dot, i + 1, false, bw));
            }
            rows.Add(Node(I18n.S("role_dest"), I18n.S("dest_title"), I18n.S("dest_detail"), null,
                "M12,3 A9,9 0 1 0 12.01,3 Z M3,12 H21 M12,3 C8.5,7 8.5,17 12,21 M12,3 C15.5,7 15.5,17 12,21", Color.FromRgb(0x8E, 0x8E, 0x93), circ.Hops.Count + 1, true, ""));

            for (int i = 0; i < rows.Count; i++)
            {
                var el = (FrameworkElement)rows[i];
                el.Opacity = 0;
                var tt = new TranslateTransform(0, 8);
                el.RenderTransform = tt;
                CircuitHost.Children.Add(el);
                var begin = TimeSpan.FromMilliseconds(60 * i);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                el.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)) { BeginTime = begin, EasingFunction = ease });
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(320)) { BeginTime = begin, EasingFunction = ease });
            }
        }

        static Brush Halo(Color c, byte alpha)
        {
            var b = new RadialGradientBrush();
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, c.R, c.G, c.B), 0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alpha / 3), c.R, c.G, c.B), 0.45));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1));
            b.Freeze();
            return b;
        }

        UIElement Node(string role, string title, string detail, Brush flag, string glyph, Color dot, int index, bool last, string right)
        {
            const double rowH = 62;
            var g = new Grid { Height = rowH };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var timeline = new Canvas { Width = 30, Height = rowH, ClipToBounds = false };
            if (!last)
            {
                var line = new Rectangle { Width = 2, Height = rowH - 12, RadiusX = 1, RadiusY = 1 };
                line.Fill = new LinearGradientBrush(Color.FromArgb(0x70, dot.R, dot.G, dot.B), Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF), 90);
                Canvas.SetLeft(line, 14);
                Canvas.SetTop(line, 30);
                timeline.Children.Add(line);

                var pulse = new Ellipse { Width = 14, Height = 14, Fill = Halo(dot, 0xFF) };
                Canvas.SetLeft(pulse, 8);
                var move = new TranslateTransform();
                pulse.RenderTransform = move;
                timeline.Children.Add(pulse);
                var dur = TimeSpan.FromMilliseconds(1400);
                var begin = TimeSpan.FromMilliseconds(220 * index);
                var travel = new DoubleAnimation(24, rowH + 10, dur)
                {
                    BeginTime = begin, RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                Timeline.SetDesiredFrameRate(travel, 30);
                move.BeginAnimation(TranslateTransform.YProperty, travel);
                var fade = new DoubleAnimationUsingKeyFrames { BeginTime = begin, RepeatBehavior = RepeatBehavior.Forever, Duration = dur };
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0.2)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromPercent(0.75)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
                Timeline.SetDesiredFrameRate(fade, 30);
                pulse.Opacity = 0;
                pulse.BeginAnimation(OpacityProperty, fade);
            }
            var halo = new Ellipse { Width = 30, Height = 30, Fill = Halo(dot, 0x70), IsHitTestVisible = false };
            Canvas.SetLeft(halo, 0);
            Canvas.SetTop(halo, 8);
            timeline.Children.Add(halo);
            var node = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(dot), Stroke = new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF)), StrokeThickness = 1.5 };
            Canvas.SetLeft(node, 9);
            Canvas.SetTop(node, 17);
            timeline.Children.Add(node);
            g.Children.Add(timeline);

            FrameworkElement badge;
            if (flag != null)
                badge = new Border { Width = 32, Height = 22, CornerRadius = new CornerRadius(5), Background = flag, BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)), BorderThickness = new Thickness(0.5) };
            else
            {
                var path = new System.Windows.Shapes.Path { Data = Geometry.Parse(glyph), Stroke = Brushes.White, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
                var canvas = new Canvas { Width = 24, Height = 24 };
                canvas.Children.Add(path);
                badge = new Border
                {
                    Width = 32, Height = 22, CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromArgb(0x30, dot.R, dot.G, dot.B)),
                    Child = new Viewbox { Width = 14, Height = 14, Child = canvas }
                };
            }
            badge.VerticalAlignment = VerticalAlignment.Top;
            badge.Margin = new Thickness(4, 12, 12, 0);
            Grid.SetColumn(badge, 1);
            g.Children.Add(badge);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 0, 0) };
            text.Children.Add(new TextBlock { Text = role, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(0xCC, dot.R, dot.G, dot.B)) });
            text.Children.Add(new TextBlock { Text = title, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 1, 0, 0) });
            text.Children.Add(new TextBlock { Text = detail, FontSize = 11, Foreground = (Brush)ui.FindResource("Secondary"), FontFamily = (FontFamily)ui.FindResource("Mono"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 1, 0, 0) });
            Grid.SetColumn(text, 2);
            g.Children.Add(text);

            if (right.Length > 0)
            {
                var r = new TextBlock { Text = right, FontSize = 10.5, Foreground = (Brush)ui.FindResource("Tertiary"), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8, 24, 0, 0) };
                Grid.SetColumn(r, 3);
                g.Children.Add(r);
            }
            return g;
        }
    }
}
