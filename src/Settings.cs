using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gtk;
using GtkSource;
using Pango;

/// <summary>
/// Represents the user-configurable settings values like language and theme.
/// </summary>
public class SettingsValues {
    /// <value>String containing the languages setted.</value>
    public string Language { get; set; }
    /// <value>String containing the editor theme.</value>
    public string EditorTheme { get; set; }
}

/// <summary>
/// GUI component and manager for user settings such as language, theme, and collaboration options.
/// Handles loading, saving, and displaying settings.
/// </summary>
public class Settings {
    /// <value><c>ScrolledWindow</c> containing all the settings.</value>
    private Gtk.ScrolledWindow Scrolled = Gtk.ScrolledWindow.New();

    /// <value>Boolean representing of the settings are currently showing on the screen.</value>
    private bool Schowing = false;

    /// <value>Box containing all the settings elements. This box will be inside the <c>ScrolledWindow</c>.</value>
    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Vertical, 10);

    /// <value>Instance of <c>SettingsValues</c> to store the settings values.</value>
    private SettingsValues SettingsValues = new SettingsValues();
    /// <value>Wrapper around the <c>SettingsValues</c> attribute to get external access to the attribute.</value>
    public SettingsValues _SettingsValues => this.SettingsValues;

    /// <value>Boolean representing if the settings had been toggled on.</value>
    private bool Toggled = false;

    /// <summary>
    /// Constructor initializes the ScrolledWindow and loads settings from file.
    /// </summary>
    public Settings() {
        this.Scrolled.MinContentWidth = 500;
        this.Scrolled.SetChild(this.Box);
        this.InitSettingsValues();
    }

    /// <summary>
    /// Initializes SettingsValues from disk if it exists, otherwise creates defaults.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void InitSettingsValues() {
        if (!this.SettingExists()) {
            this.SettingsValues = new SettingsValues { Language = "English" };

            var systemSettings = Gtk.Settings.GetDefault();
            this.SettingsValues.EditorTheme = systemSettings?.GtkApplicationPreferDarkTheme == true || systemSettings?.GtkThemeName?.ToLower()?.Contains("dark") == true ? "Adwaita-dark" : "Adwaita";

            this.SaveSettings();
        } else {
            string path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Environment.ExpandEnvironmentVariables("%appdata%") + "/TeXSharp/config.json" : Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json";

            this.SettingsValues = JsonSerializer.Deserialize<SettingsValues>(System.IO.File.ReadAllText(path)) ?? new SettingsValues();
        }
    }

    /// <summary>
    /// Dynamically builds the UI section only once. Adds language, theme, and collaboration settings.
    /// </summary>
    /// <param name="editorWrapper">The source editor wrapper to apply settings on.</param>
    /// <param name="statusBar">A status label for feedback.</param>
    /// <returns>This methods does not return anything.</returns>
    public void OnToggle(SourceEditorWrapper editorWrapper, Gtk.Label statusBar) {
        if (!this.Toggled) {
            this.AddMainTitle();
            this.AddLanguagesOptions();
            this.AddEditorThemeOptions(editorWrapper);
            this.AddCollaborationOptions(editorWrapper, statusBar);
            this.Toggled = true;
        }
    }

    /// <summary>
    /// Adds the main section title.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void AddMainTitle() { this.AddText(Globals.Languages.Translate("settings"), 20); }

    /// <summary>
    /// Adds a dropdown menu to select the language. Updates settings on selection change.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void AddLanguagesOptions() {
        var LanBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Label = Gtk.Label.New(Globals.Languages.Translate("choose_language"));
        var Languages = Globals.Languages.GetAllLanguages();
        string[] Lang = Languages.ToArray();
        var DropDown = Gtk.DropDown.NewFromStrings(Lang);

        for (uint i = 0; i < Lang.Length; ++i) {
            if (Lang[i] == this.SettingsValues.Language)
                DropDown.SetSelected(i);
        }

        DropDown.OnNotify += (sender, args) => {
            this.SettingsValues.Language = Lang[DropDown.GetSelected()];
            this.SaveSettings();
        };

        LanBox.Append(Label);
        LanBox.Append(DropDown);
        this.Box.Append(LanBox);
    }

    /// <summary>
    /// Adds a theme selector dropdown and applies the selected theme to the editor.
    /// </summary>
    /// <param name="editorWrapper">The source editor wrapper to apply the theme editor on.</param>
    /// <returns>This methods does not return anything.</returns>
    private void AddEditorThemeOptions(SourceEditorWrapper editorWrapper) {
        var SchemeBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Label = Gtk.Label.New(Globals.Languages.Translate("choose_theme"));
        string[] Themes = { "Adwaita-dark", "classic-dark", "cobalt-light", "kate-dark", "oblivion", "solarized-light", "tango", "Yaru", "Adwaita", "classic", "cobalt", "kate", "solarized-dark", "Yaru-dark" };
        var DropDown = Gtk.DropDown.NewFromStrings(Themes);

        for (uint i = 0; i < Themes.Length; ++i) {
            if (Themes[i] == this.SettingsValues.EditorTheme)
                DropDown.SetSelected(i);
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

    /// <summary>
    /// Adds a Pango-styled title or label to the UI.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
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

    /// <summary>
    /// Builds the collaboration section UI with WebSocket server/client controls.
    /// </summary>
    /// <param name="editorWrapper">The source editor wrapper to apply settings on.</param>
    /// <param name="statusBar">A status label for feedback.</param>
    /// <returns>This methods does not return anything.</returns>
    private void AddCollaborationOptions(SourceEditorWrapper editorWrapper, Gtk.Label statusBar) {
        this.AddText(Globals.Languages.Translate("rt_collaboration"), 12);
        this.AddText(Globals.Languages.Translate("server"), 10);

        // Server
        var PortBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var SpinButton = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        PortBox.Append(Gtk.Label.New(Globals.Languages.Translate("choose_port") + " :"));
        PortBox.Append(SpinButton);
        this.Box.Append(PortBox);

        var ButtonStart = Gtk.Button.NewWithLabel(Globals.Languages.Translate("start_server"));
        var ButtonStop = Gtk.Button.NewWithLabel(Globals.Languages.Translate("stop_server"));
        ButtonStart.SetMarginEnd(12);
        ButtonStop.SetMarginEnd(12);

        ButtonStart.OnClicked += (_, _) => editorWrapper.GetCurrentSourceEditor().StartWebSocketServer((int)SpinButton.GetValue(), statusBar);
        ButtonStop.OnClicked += (_, _) => editorWrapper.GetCurrentSourceEditor().StopWebSocketServer(statusBar);

        this.Box.Append(ButtonStart);
        this.Box.Append(ButtonStop);

        // Client
        this.AddText(Globals.Languages.Translate("client"), 10);
        var _IpBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var Entry = Gtk.Entry.New();
        _IpBox.Append(Gtk.Label.New(Globals.Languages.Translate("choose_server") + " (IP) :"));
        _IpBox.Append(Entry);

        var _PortBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var _SpinButton = Gtk.SpinButton.NewWithRange(1024, 49151, 1);
        _PortBox.Append(Gtk.Label.New(Globals.Languages.Translate("choose_port") + " :"));
        _PortBox.Append(_SpinButton);

        this.Box.Append(_IpBox);
        this.Box.Append(_PortBox);

        var _ButtonStart = Gtk.Button.NewWithLabel(Globals.Languages.Translate("connect"));
        var _ButtonStop = Gtk.Button.NewWithLabel(Globals.Languages.Translate("disconnect"));
        _ButtonStart.SetMarginEnd(12);
        _ButtonStop.SetMarginEnd(12);

        _ButtonStart.OnClicked += (_, _) => editorWrapper.GetCurrentSourceEditor().StartWebSocketClient(Entry.GetText(), (int)_SpinButton.GetValue(), statusBar);
        _ButtonStop.OnClicked += (_, _) => editorWrapper.GetCurrentSourceEditor().StopWebSocketClient(statusBar);

        this.Box.Append(_ButtonStart);
        this.Box.Append(_ButtonStop);
    }

    /// <summary>
    /// Checks if the settings config file exists.
    /// </summary>
    /// <returns>Returns a boolean: if the settings exists or not.</returns>
    private bool SettingExists() { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? System.IO.File.Exists(Environment.ExpandEnvironmentVariables("%appdata%") + "/TeXSharp/config.json") : System.IO.File.Exists(Environment.GetEnvironmentVariable("HOME") + "/.config/texsharp/config.json"); }

    /// <summary>
    /// Saves the current settings to the config file.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void SaveSettings() {
        byte[] JsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(this.SettingsValues);
        string path;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            string appData = Environment.ExpandEnvironmentVariables("%appdata%");
            Directory.CreateDirectory(appData + "/TeXSharp/");
            path = appData + "/TeXSharp/config.json";
        } else {
            string home = Environment.GetEnvironmentVariable("HOME") ?? "/home/";
            Directory.CreateDirectory(home + "/.config/texsharp/");
            path = home + "/.config/texsharp/config.json";
        }

        System.IO.File.WriteAllBytes(path, JsonUtf8Bytes);
    }

    // Manual getters
    /// <summary>
    /// Returns the ScrolledWindow that wraps the settings UI.
    /// </summary>
    /// <returns>Returns the <c>Gtk.ScrolledWindow</c> in which the settings are showed.</returns>
    public Gtk.ScrolledWindow GetScrolledWindow() => this.Scrolled;

    /// <summary>
    /// Returns whether the settings window is currently shown.
    /// </summary>
    /// <returns>Returns a boolean: if the settings are being shown or not.</returns>
    public bool GetShowing() => this.Schowing;

    // Manual setter
    /// <summary>
    /// Sets the visibility state of the settings UI.
    /// </summary>
    /// <param name="isShowing">A boolean which represents if the settings are showing or not.</param>
    /// <returns>This methods does not return anything.</returns>
    public void SetShowing(bool isShowing) { this.Schowing = isShowing; }
}
