using GtkSource;

class SourceEditor {
    private GtkSource.Buffer buffer;
    public GtkSource.Buffer _Buffer {
        get { return this.buffer; } // get method
    }
    private GtkSource.View view;
    public GtkSource.View _View {
        get { return this.view; } // get method
    }
    private string path;
    public string _Path {
        get { return this.path; }
    }
    private bool file_exists = false;
    public bool _Exists {
        get { return this.file_exists; }
    }
    private LanguageManager language_manager;
    private GtkSource.VimIMContext VIMmode = GtkSource.VimIMContext.New();
    public GtkSource.VimIMContext _VIMmode {
        get { return this.VIMmode; }
    }
    private Gtk.EventControllerKey VIMeventControllerKey = Gtk.EventControllerKey.New();
    public Gtk.EventControllerKey _VIMeventControllerKey {
        get { return this.VIMeventControllerKey; }
    }
    // 0 if vim mode is disabled, 1 if vim mode is enabled (default is disabled)
    private bool VIMmodeEnabled = false;
    public bool _VIMmodeEnabled {
        get { return this.VIMmodeEnabled; }
        set { this.VIMmodeEnabled = value; }
    }
    private Gtk.Entry TextEntry = new Gtk.Entry();
    public Gtk.Entry _TextEntry {
        get { return this.TextEntry; }
    }

    public SourceEditor(string path, Gtk.Grid grid) {
        // TODO: Take in account the path to open an instance of an editor with an existing file
        this.path = "";
        this.buffer = GtkSource.Buffer.New(null);
        this.view = GtkSource.View.NewWithBuffer(this.buffer);
        view.Monospace = true;
        view.ShowLineNumbers = true;
        view.HighlightCurrentLine = true;
        view.SetTabWidth(4);
        var settings = Gtk.Settings.GetDefault();
        if (settings?.GtkApplicationPreferDarkTheme == true || settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
            this.buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme("Adwaita-dark"));
        this.language_manager = GtkSource.LanguageManager.New();
    }

    public void OpenFile(string? path) {
        if (path is null) throw new System.ArgumentNullException("String path is null, cannot open file");
        this.path = path;
        this.file_exists = true;
        this.buffer.Text = System.IO.File.ReadAllText(path);
        this.SetBufferLanguage();
    }

    public void SaveFile(string? path) {
        if (path is null) throw new System.ArgumentNullException("String path is null, cannot save file");
        if (!this.file_exists) {
            this.path = path;
        }
        System.IO.File.WriteAllText(path, this.buffer.Text);
        this.file_exists = true;
        this.SetBufferLanguage();
    }

    private void SetBufferLanguage() { this.buffer.Language = this.language_manager.GuessLanguage(this.path, null); }

    // Manuals Getters
    public GtkSource.Buffer GetBuffer() { return this.buffer; }
    public GtkSource.View GetView() { return this.view; }
    public string GetPath() { return this.path; }
    public bool GetFileExists() { return this.file_exists; }
}
