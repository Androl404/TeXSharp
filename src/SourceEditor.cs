using GtkSource;

public class SourceEditor {
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

    private string name;
    public string _Name {
        get { return this.name; }
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

    private Gtk.Notebook editor_notebook = Gtk.Notebook.New();
    public Gtk.Notebook _EditorNotebook {
        get { return this.editor_notebook; }
    }

    public List<SourceEditor> editors = new List<SourceEditor>();

    public SourceEditor(int index) {
        // First, the name of the editor is set (it should start at 0). We use an index variable to be sure the name is unique. Otherwise, each editor will have the same name (for example, editor0)
        this.name = "editor" + index;

        // Second, we create the buffer and the view
        this.buffer = GtkSource.Buffer.New(null);
        this.view = GtkSource.View.NewWithBuffer(this.buffer);

        // We set some properties to the view
        this.view.Monospace = true;
        this.view.ShowLineNumbers = true;
        this.view.HighlightCurrentLine = true;
        this.view.SetTabWidth(4);

        // We set the style scheme of the buffer
        this.buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme(Globals.settings._Settings_values.editor_theme));

        // We set the path to an empty string by default
        this.path = string.Empty;

        // We create a new language manager, so we can guess the language (for example latex) of the file
        this.language_manager = GtkSource.LanguageManager.New();
    }

    public void NewFile() {
        // First, we create the editor
        this.editors.Add(new SourceEditor(this.editors.Count));

        // Second step, we create the scrolled window that will contain the editor
        var scrolled_window = new Gtk.ScrolledWindow();
        scrolled_window.SetHexpand(true);
        scrolled_window.SetVexpand(true);

        // Third step, we add the view (what we see) to the scrolled window
        scrolled_window.SetChild(this.editors[this.editors.Count - 1].view);

        // Fourth step, we add the scrolled window as a page of the notebook
        this.editor_notebook.AppendPage(scrolled_window, EditorNotebookTabLabel(Globals.lan.ServeTrad("new_file")));
        // We then move to the next page. So the one we just created
        this.editor_notebook.NextPage();
    }

    // Simple function to create a tab label for the Notebook pages. It simply returns a box with a label, the name of the file, and a close button (symbol of a cross) to close the tab
    public Gtk.Box EditorNotebookTabLabel(string label) {
        // We create the label
        Gtk.Label tabLabel = Gtk.Label.New(label);

        // We create the button
        Gtk.Button tabCloseButton = Gtk.Button.NewFromIconName("window-close-symbolic");
        tabCloseButton.SetHasFrame(false);
        tabCloseButton.OnClicked += (o, args) => { this.CloseFile(); };

        // We create the box that will contain the label and the button
        Gtk.Box tabBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        tabBox.Append(tabLabel);
        tabBox.Append(tabCloseButton);

        return tabBox;
    }

    public void OpenFile(string? path) {
        // First, we create the editor
        this.editors.Add(new SourceEditor(this.editors.Count));

        // We check if the path is null. If it is, we throw an exception
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot open file");

        // Second step, we create the scrolled window that will contain the editor
        var scrolled_window = new Gtk.ScrolledWindow();
        scrolled_window.SetHexpand(true);
        scrolled_window.SetVexpand(true);

        // Third step, we add the view (what we see) to the scrolled window
        scrolled_window.SetChild(this.editors[this.editors.Count - 1].view);

        // Fourth step, we add the scrolled window as a page of the notebook. And we extract the name of the file from the path
        this.editor_notebook.AppendPage(scrolled_window, EditorNotebookTabLabel(path.Split('/').Last()));
        // We then go to the next page. So the one we just created.
        // The reason why we do this is because we work with the current page/editor. So if we don't go to the next page, the path will be set to the previous page. Same for the buffer. resulting in a wrong path and buffer. Typically, if we don't do this, we will open a new tab, with a blank buffer,
        // and the text we read from the file, will be put in the previous tab. Which isn't ideal
        this.editor_notebook.NextPage();
        this.SetPath(path);

        this.SetFileExists(true);

        this.GetBuffer().Text = System.IO.File.ReadAllText(this.GetPath());
        this.SetBufferLanguage();
    }

    public void SaveFile(string? path) {
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot save file");
        if (!this.GetFileExists()) {
            this.SetPath(path);
        }
        System.IO.File.WriteAllText(path, this.GetBuffer().Text);
        this.SetFileExists(true);
        this.editors[this.GetCurrentEditorIndex()].buffer.Language = this.language_manager.GuessLanguage(path, null);
        this.SetBufferLanguage();
    }

    public void CloseFile() {
        // We remove the page from the notebook
        // We make sure that we don't close the first tab. We need at least one tab open
        if (this.editor_notebook.GetNPages() == 1) {
        } else {
            this.editor_notebook.RemovePage(this.GetCurrentEditorIndex());
            // We remove the editor from the list
            this.editors.RemoveAt(this.GetCurrentEditorIndex());
        }
    }

    public void ChangeEditorTheme(string theme) { this.editors[this.GetCurrentEditorIndex()].buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme(theme)); }

    public async void StartWebSocketServer(int port, Gtk.Label status_bar) { throw new System.NotImplementedException("Not implemented yet."); }

    public async void StopWebSocketServer(Gtk.Label status_bar) { throw new System.NotImplementedException("Not implemented yet."); }

    public async void StartWebSocketClient(string server, int port, Gtk.Label status_bar) { throw new System.NotImplementedException("Not implemented yet."); }

    public async void StopWebSocketClient(Gtk.Label status_bar) { throw new System.NotImplementedException("Not implemented yet."); }

    // Manuals Getters

    public int GetCurrentEditorIndex() {
        // We get the current page number
        int current_page_number = editor_notebook.GetCurrentPage();
        return current_page_number;
    }

    public GtkSource.Buffer GetBuffer() { return this.editors[this.GetCurrentEditorIndex()].buffer; }
    // Useless for now. Eventually have a use case for the shortcuts, but it doesn't seem to work right now
    public GtkSource.View GetView() { return this.editors[this.GetCurrentEditorIndex()].view; }

    public string GetPath() { return this.editors[this.GetCurrentEditorIndex()].path; }
    public void SetPath(string path) { this.editors[this.GetCurrentEditorIndex()].path = path; }

    public bool GetFileExists() { return this.editors[this.GetCurrentEditorIndex()].file_exists; }
    public void SetFileExists(bool file_exists) { this.editors[this.GetCurrentEditorIndex()].file_exists = file_exists; }

    private void SetBufferLanguage() { this.editors[this.GetCurrentEditorIndex()].buffer.Language = this.editors[this.GetCurrentEditorIndex()].language_manager.GuessLanguage(this.editors[this.GetCurrentEditorIndex()].path, null); }

    public bool GetVIMmodeEnabled() { return this.editors[this.GetCurrentEditorIndex()].VIMmodeEnabled; }
    public void SetVIMmodeEnabled(bool VIMmodeEnabled) { this.editors[this.GetCurrentEditorIndex()].VIMmodeEnabled = VIMmodeEnabled; }

    public Gtk.Entry GetTextEntry() { return this.editors[this.GetCurrentEditorIndex()].TextEntry; }

    public GtkSource.VimIMContext GetVIMmode() { return this.editors[this.GetCurrentEditorIndex()].VIMmode; }

    public Gtk.EventControllerKey GetVIMeventControllerKey() { return this.editors[this.GetCurrentEditorIndex()].VIMeventControllerKey; }
}
