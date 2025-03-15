using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gtk;
using GtkSource;
using Pango;

public class SettingsValues {
    public string language { get; set; }
    public string editor_theme { get; set; }
    public SettingsValues() {
        this.language = "";
        this.editor_theme = "";
    }
}

public class Settings {
    private Gtk.ScrolledWindow scrolled;
    public Gtk.ScrolledWindow _Scrolled {
        get { return this.scrolled; }
    }
    private bool showing;
    public bool _Schowing {
        get { return this.showing; }
    }
    private Gtk.Box box;
    private SettingsValues settings_values;
    public SettingsValues _Settings_values {
        get { return this.settings_values; }
    }
    private bool toggled;

    public Settings() {
        this.scrolled = Gtk.ScrolledWindow.New();
        this.box = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        this.scrolled.MinContentWidth = 500;
        this.scrolled.SetChild(this.box);
        this.showing = false;
        this.toggled = false;
        this.settings_values = new SettingsValues();
        this.InitSettingsValues();
    }

    private void InitSettingsValues() {
        if (!this.SettingExists()) {
            this.settings_values = new SettingsValues();
            this.settings_values.language = "English";
            var settings = Gtk.Settings.GetDefault();
            if (settings?.GtkApplicationPreferDarkTheme == true || settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
                this.settings_values.editor_theme = "Adwaita-dark";
            else
                this.settings_values.editor_theme = "Adwaita";
            this.SaveSettings();
        } else {
            string path = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for video games
                path = Environment.ExpandEnvironmentVariables("%appdata%") + "/Local/TeXSharp/config.json";
            } else { // Unix-based OS
                path = Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json";
            }
            this.settings_values = JsonSerializer.Deserialize<SettingsValues>(System.IO.File.ReadAllText(path)) ?? new SettingsValues();
        }
    }

    public void OnToggle(SourceEditor editor, Gtk.Label status_bar) {
        if (!this.toggled) {
            this.AddMainTitle();
            this.AddLanguagesOptions();
            this.AddEditorThemeOptions(editor);
            this.AddCollaborationOptions(editor, status_bar);
            this.toggled = true;
        }
    }

    private void AddMainTitle() {
        this.AddText(Globals.lan.ServeTrad("settings"), 20);
        var port_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 2);
        var label = Gtk.Label.New(Globals.lan.ServeTrad("choose_port"));
    }

    async private void AddLanguagesOptions() {
        var lan_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var label = Gtk.Label.New(Globals.lan.ServeTrad("choose_language"));
        var languages = await Globals.lan.DBGetAllLanguages();
        string[] lang = languages.ToArray();
        var drop_down = Gtk.DropDown.NewFromStrings(lang);
        for (uint i = 0; i < lang.Length; ++i) {
            if (lang[i] == this.settings_values.language) {
                drop_down.SetSelected(i);
            }
        }
        drop_down.OnNotify += (sender, args) => {
            this.settings_values.language = lang[drop_down.GetSelected()];
            this.SaveSettings();
        };
        lan_box.Append(label);
        lan_box.Append(drop_down);
        this.box.Append(lan_box);
    }

    private void AddEditorThemeOptions(SourceEditor editor) {
        var scheme_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var label = Gtk.Label.New(Globals.lan.ServeTrad("choose_theme"));
        string[] themes = { "Adwaita-dark", "classic-dark", "cobalt-light", "kate-dark", "oblivion", "solarized-light", "tango", "Yaru", "Adwaita", "classic", "cobalt", "kate", "solarized-dark", "Yaru-dark" };
        var drop_down = Gtk.DropDown.NewFromStrings(themes);
        for (uint i = 0; i < themes.Length; ++i) {
            if (themes[i] == this.settings_values.editor_theme) {
                drop_down.SetSelected(i);
            }
        }
        drop_down.OnNotify += (sender, args) => {
            this.settings_values.editor_theme = themes[drop_down.GetSelected()];
            editor.ChangeEditorTheme(this.settings_values.editor_theme);
            this.SaveSettings();
        };
        scheme_box.Append(label);
        scheme_box.Append(drop_down);
        this.box.Append(scheme_box);
    }

    private void AddText(string text, int size) {
        var attr_list = Pango.AttrList.New();
        var font = Pango.FontDescription.New();
        font.SetWeight(Pango.Weight.Bold);
        font.SetSize(size*Globals.PAGNO_SCALE);
        var font_attribute = Pango.AttrFontDesc.New(font);
        attr_list.Insert(font_attribute);
        var label = Gtk.Label.New(text);
        label.SetAttributes(attr_list);
        this.box.Append(label);
    }

    private void AddCollaborationOptions(SourceEditor editor, Gtk.Label status_bar) {
        this.AddText(Globals.lan.ServeTrad("rt_collaboration"), 12);
        this.AddText(Globals.lan.ServeTrad("server"), 10);
        var port_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var spin_button = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        port_box.Append(Gtk.Label.New(Globals.lan.ServeTrad("choose_port") + " :"));
        port_box.Append(spin_button);
        this.box.Append(port_box);
        var button_start = Gtk.Button.NewWithLabel(Globals.lan.ServeTrad("start_server"));
        var button_stop = Gtk.Button.NewWithLabel(Globals.lan.ServeTrad("stop_server"));
        button_start.SetMarginEnd(12);
        button_stop.SetMarginEnd(12);
        button_start.OnClicked += (serder, args) => {
            editor.StartWebSocketServer((int)spin_button.GetValue(), status_bar);
        };
        button_stop.OnClicked += (serder, args) => {
            editor.StopWebSocketServer(status_bar);
        };
        this.box.Append(button_start);
        this.box.Append(button_stop);
        this.AddText(Globals.lan.ServeTrad("client"), 10);
        var _ip_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var entry = Gtk.Entry.New();
        _ip_box.Append(Gtk.Label.New(Globals.lan.ServeTrad("choose_server") + " (IP) :"));
        _ip_box.Append(entry);
        var _port_box = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var _spin_button = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        _port_box.Append(Gtk.Label.New(Globals.lan.ServeTrad("choose_port") + " :"));
        _port_box.Append(_spin_button);
        this.box.Append(_ip_box);
        this.box.Append(_port_box);
        var _button_start = Gtk.Button.NewWithLabel(Globals.lan.ServeTrad("connect"));
        _button_start.SetMarginEnd(12);
        var _button_stop = Gtk.Button.NewWithLabel(Globals.lan.ServeTrad("disconnect"));
        _button_stop.SetMarginEnd(12);
        _button_start.OnClicked += (serder, args) => {
            editor.StartWebSocketServer((int)spin_button.GetValue(), status_bar);
        };
        _button_stop.OnClicked += (serder, args) => {
            editor.StopWebSocketServer(status_bar);
        };
        this.box.Append(_button_start);
        this.box.Append(_button_stop);
    }

    private bool SettingExists() {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for video games
            return System.IO.File.Exists(Environment.ExpandEnvironmentVariables("%appdata%") + "/Local/TeXSharp/config.json");
        } else { // Unix-based OS
            return System.IO.File.Exists(Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json");
        }
    }

    private void SaveSettings() {
        byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.settings_values); // Faster and better
        string path = "";
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for games
            string appdata = Environment.ExpandEnvironmentVariables("%appdata%");
            System.IO.Directory.CreateDirectory(appdata + "/Local/TeXSharp/");
            path = appdata + "/Local/TeXSharp/config.json";
        } else { // Unix-based OS
            string home_user = Environment.GetEnvironmentVariable("HOME") ?? "/home/";
            System.IO.Directory.CreateDirectory(home_user + "/.config");
            System.IO.Directory.CreateDirectory(home_user + "/.config/texsharp");
            path = home_user + "/.config/texsharp/config.json";
        }
        System.IO.File.WriteAllBytes(path, jsonUtf8Bytes);
    }

    // Manuals getters
    public Gtk.ScrolledWindow GetScrolledWindow() { return this.scrolled; }
    public bool GetShowing() { return this.showing; }

    // Manuels setters
    public void SetShowing(bool is_showing) { this.showing = is_showing; }
}
