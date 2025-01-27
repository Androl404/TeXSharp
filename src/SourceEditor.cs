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

    public SourceEditor(string path) {
        // TODO: Take in account the path to open an instance of an editor with an existing file
        this.buffer = GtkSource.Buffer.New(null);
        this.view = GtkSource.View.NewWithBuffer(this.buffer);
        view.Monospace = true;
        view.ShowLineNumbers = true;
        var settings = Gtk.Settings.GetDefault();
        if (settings?.GtkApplicationPreferDarkTheme == true || settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
            this.buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme("Adwaita-dark"));
    }

    public void OpenFile(string path) {
        this.path = path;
        this.file_exists = true;
        this.buffer.Text = System.IO.File.ReadAllText(path);
        var languageManager = GtkSource.LanguageManager.New();
        this.buffer.Language = languageManager.GuessLanguage(this.path, null);
        this.view.SetHscrollPolicy(Gtk.ScrollablePolicy.Minimum);
    }

    public void SaveFile(string path) {
        if (!this.file_exists) {
            this.path = path;
        }
        System.IO.File.WriteAllText(path, this.buffer.Text);
        this.file_exists = true;
    }

    // Manuals Getters
    public GtkSource.Buffer GetBuffer() {
        return this.buffer;
    }
    public GtkSource.View GetView() {
        return this.view;
    }
    public string GetPath() {
        return this.path;
    }
    public bool GetFileExists() {
        return this.file_exists;
    }
}
