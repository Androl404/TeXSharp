using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gtk;
using GtkSource;
using Pango;

public class SettingsValues {
    public string Language { get; set; }
    public string EditorTheme { get; set; }
    public SettingsValues() {
        this.Language = "";
        this.EditorTheme = "";
    }
}

public class Settings {
    private Gtk.ScrolledWindow Scrolled = Gtk.ScrolledWindow.New();
    public Gtk.ScrolledWindow _Scrolled {
        get { return this.Scrolled; }
    }
    private bool Schowing = false;
    public bool _Schowing {
        get { return this.Schowing; }
    }
    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
    private SettingsValues SettingsValues = new SettingsValues();
    public SettingsValues _SettingsValues {
        get { return this.SettingsValues; }
    }
    private bool Toggled = false;

    public Settings() {
        this.Scrolled.MinContentWidth = 500;
        this.Scrolled.SetChild(this.Box);
        this.InitSettingsValues();
    }

    private void InitSettingsValues() {
        if (!this.SettingExists()) {
            this.SettingsValues = new SettingsValues();
            this.SettingsValues.Language = "English";
            var Settings = Gtk.Settings.GetDefault();
            if (Settings?.GtkApplicationPreferDarkTheme == true || Settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
                this.SettingsValues.EditorTheme = "Adwaita-dark";
            else
                this.SettingsValues.EditorTheme = "Adwaita";
            this.SaveSettings();
        } else {
            string path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) // If we are on a niche operating system for games
                              ? Environment.ExpandEnvironmentVariables("%appdata%") + "/TeXSharp/config.json"
                              : Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json";

            this.SettingsValues = JsonSerializer.Deserialize<SettingsValues>(System.IO.File.ReadAllText(path)) ?? new SettingsValues();
        }
    }

    public void OnToggle(SourceEditorWrapper editorWrapper, Gtk.Label statusBar) {
        if (!this.Toggled) {
            this.AddMainTitle();
            this.AddLanguagesOptions();
            this.AddEditorThemeOptions(editorWrapper);
            this.AddCollaborationOptions(editorWrapper, statusBar);
            this.Toggled = true;
        }
    }

    private void AddMainTitle() {
        this.AddText(Globals.Languages.ServeTrad("settings"), 20);
        // var PortBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 2);
        // var Label = Gtk.Label.New(Globals.Languages.ServeTrad("choose_port"));
    }

    async private void AddLanguagesOptions() {
        var LanBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Label = Gtk.Label.New(Globals.Languages.ServeTrad("choose_language"));
        var Languages = await Globals.Languages.DBGetAllLanguages();
        string[] Lang = Languages.ToArray();
        var DropDown = Gtk.DropDown.NewFromStrings(Lang);
        for (uint i = 0; i < Lang.Length; ++i) {
            if (Lang[i] == this.SettingsValues.Language) {
                DropDown.SetSelected(i);
            }
        }
        DropDown.OnNotify += (sender, args) => {
            this.SettingsValues.Language = Lang[DropDown.GetSelected()];
            this.SaveSettings();
        };
        LanBox.Append(Label);
        LanBox.Append(DropDown);
        this.Box.Append(LanBox);
    }

    private void AddEditorThemeOptions(SourceEditorWrapper editorWrapper) {
        var SchemeBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Label = Gtk.Label.New(Globals.Languages.ServeTrad("choose_theme"));
        string[] Themes = { "Adwaita-dark", "classic-dark", "cobalt-light", "kate-dark", "oblivion", "solarized-light", "tango", "Yaru", "Adwaita", "classic", "cobalt", "kate", "solarized-dark", "Yaru-dark" };
        var DropDown = Gtk.DropDown.NewFromStrings(Themes);
        for (uint i = 0; i < Themes.Length; ++i) {
            if (Themes[i] == this.SettingsValues.EditorTheme) {
                DropDown.SetSelected(i);
            }
        }
        DropDown.OnNotify += (sender, args) => {
            this.SettingsValues.EditorTheme = Themes[DropDown.GetSelected()];
            editorWrapper.GetCurrentSourceEditor().ChangeEditorTheme(this.SettingsValues.EditorTheme);
            this.SaveSettings();
        };
        SchemeBox.Append(Label);
        SchemeBox.Append(DropDown);
        this.Box.Append(SchemeBox);
    }

    private void AddText(string text, int size) {
        var AttrList = Pango.AttrList.New();
        var Font = Pango.FontDescription.New();
        Font.SetWeight(Pango.Weight.Bold);
        Font.SetSize(size * Globals.PangoScale);
        var FontAttribute = Pango.AttrFontDesc.New(Font);
        AttrList.Insert(FontAttribute);
        var Label = Gtk.Label.New(text);
        Label.SetAttributes(AttrList);
        this.Box.Append(Label);
    }

    private void AddCollaborationOptions(SourceEditorWrapper editorWrapper, Gtk.Label statusBar) {
        this.AddText(Globals.Languages.ServeTrad("rt_collaboration"), 12);
        this.AddText(Globals.Languages.ServeTrad("server"), 10);
        var PortBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var SpinButton = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        PortBox.Append(Gtk.Label.New(Globals.Languages.ServeTrad("choose_port") + " :"));
        PortBox.Append(SpinButton);
        this.Box.Append(PortBox);
        var ButtonStart = Gtk.Button.NewWithLabel(Globals.Languages.ServeTrad("start_server"));
        var ButtonStop = Gtk.Button.NewWithLabel(Globals.Languages.ServeTrad("stop_server"));
        ButtonStart.SetMarginEnd(12);
        ButtonStop.SetMarginEnd(12);
        ButtonStart.OnClicked += (serder, args) => { editorWrapper.GetCurrentSourceEditor().StartWebSocketServer((int)SpinButton.GetValue(), statusBar); };
        ButtonStop.OnClicked += (serder, args) => { editorWrapper.GetCurrentSourceEditor().StopWebSocketServer(statusBar); };
        this.Box.Append(ButtonStart);
        this.Box.Append(ButtonStop);
        this.AddText(Globals.Languages.ServeTrad("client"), 10);
        var _IpBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Entry = Gtk.Entry.New();
        _IpBox.Append(Gtk.Label.New(Globals.Languages.ServeTrad("choose_server") + " (IP) :"));
        _IpBox.Append(Entry);
        var _PortBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var _SpinButton = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        _PortBox.Append(Gtk.Label.New(Globals.Languages.ServeTrad("choose_port") + " :"));
        _PortBox.Append(_SpinButton);
        this.Box.Append(_IpBox);
        this.Box.Append(_PortBox);
        var _ButtonStart = Gtk.Button.NewWithLabel(Globals.Languages.ServeTrad("connect"));
        _ButtonStart.SetMarginEnd(12);
        var _ButtonStop = Gtk.Button.NewWithLabel(Globals.Languages.ServeTrad("disconnect"));
        _ButtonStop.SetMarginEnd(12);
        _ButtonStart.OnClicked += (serder, args) => { editorWrapper.GetCurrentSourceEditor().StartWebSocketClient(Entry.GetText(), (int)_SpinButton.GetValue(), statusBar); };
        _ButtonStop.OnClicked += (serder, args) => { editorWrapper.GetCurrentSourceEditor().StopWebSocketClient(statusBar); };
        this.Box.Append(_ButtonStart);
        this.Box.Append(_ButtonStop);
    }

    private bool SettingExists() {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for video games
            return System.IO.File.Exists(Environment.ExpandEnvironmentVariables("%appdata%") + "/TeXSharp/config.json");
        } else { // Unix-based OS
            return System.IO.File.Exists(Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json");
        }
    }

    private void SaveSettings() {
        byte[] JsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.SettingsValues); // Faster and better
        string Path = "";
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for games
            string AppData = Environment.ExpandEnvironmentVariables("%appdata%");
            System.IO.Directory.CreateDirectory(AppData + "/TeXSharp/");
            Path = AppData + "/TeXSharp/config.json";
        } else { // Unix-based OS
            string HomeUser = Environment.GetEnvironmentVariable("HOME") ?? "/home/";
            System.IO.Directory.CreateDirectory(HomeUser + "/.config");
            System.IO.Directory.CreateDirectory(HomeUser + "/.config/texsharp");
            Path = HomeUser + "/.config/texsharp/config.json";
        }
        System.IO.File.WriteAllBytes(Path, JsonUtf8Bytes);
    }

    // Manuals getters
    public Gtk.ScrolledWindow GetScrolledWindow() { return this.Scrolled; }
    public bool GetShowing() { return this.Schowing; }

    // Manuels setters
    public void SetShowing(bool isShowing) { this.Schowing = isShowing; }
}
