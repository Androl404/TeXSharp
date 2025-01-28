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

    public SourceEditor(string path, Gtk.Grid grid) {
        // TODO: Take in account the path to open an instance of an editor with an existing file
        this.buffer = GtkSource.Buffer.New(null);
        this.view = GtkSource.View.NewWithBuffer(this.buffer);
        view.Monospace = true;
        view.ShowLineNumbers = true;
        view.HighlightCurrentLine = true;
        view.SetTabWidth(4);

        // Creation of a VIM mode
        // First step is to actually create the Input Module (IM) Context for the VIM mode
        // And to create an event controller key to handle the key events
        var eventControllerKey = Gtk.EventControllerKey.New();
        var VIMmode = VimIMContext.New();

        // Set the IM context to the event controller key
        eventControllerKey.SetImContext(VIMmode);
        eventControllerKey.SetPropagationPhase(Gtk.PropagationPhase.Capture);
        // Add the event controller key to the view
        // And the vim input module context to the view (editor)
        view.AddController(eventControllerKey);
        VIMmode.SetClientWidget(view);

        // Creating an entry for the command bar
        var TextEntry = new Gtk.Entry();
        grid.Attach(TextEntry, 0, 2, 1, 1);

        // Bind the command bar text to the text entry so that when we type ":" in the editor it will show up in the text entry at the bottom
        VIMmode.BindProperty("command-bar-text", TextEntry, "text", 0);
        VIMmode.BindProperty("command-text", TextEntry, "text", 0);

        // TODO : hide the entry and only show it when the user presses ":" in the editor
        // And hide it when the user presses "Enter" or "Escape"
        // And add a button that activate/deactivate the VIM mode + a shortcut like "Ctrl + Shift + V" for easier manipulation
        // Finally, add all this lines for the VIM mode inside a dedicated function to make the code cleaner

        var settings = Gtk.Settings.GetDefault();
        if (settings?.GtkApplicationPreferDarkTheme == true || settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
            this.buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme("Adwaita-dark"));
        this.language_manager = GtkSource.LanguageManager.New();
    }

    public void OpenFile(string path) {
        this.path = path;
        this.file_exists = true;
        this.buffer.Text = System.IO.File.ReadAllText(path);
        this.SetBufferLanguage();
    }

    public void SaveFile(string path) {
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
