using Gtk;
using GtkSource;

public class SourceEditorWrapper {
    private Gtk.Notebook EditorNotebook = Gtk.Notebook.New();
    public Gtk.Notebook _EditorNotebook {
        get { return this.EditorNotebook; }
    }

    private List<SourceEditor> Editors = new List<SourceEditor>();

    public SourceEditorWrapper(Gtk.Window window) {
        this.Editors.Add(new SourceEditor());
        this.EditorNotebook.AppendPage(this.Editors[0]._Box, EditorNotebookTabLabel(Globals.Languages.ServeTrad("new_file")));
        this.EditorNotebook.SetTabReorderable(this.Editors[0]._Box, true);
        // TODO: Change title of Window when tabs are switched in the Notebook
        // this.EditorNotebook.OnSwitchPage += (notebook, args) => {
        //     for (int i = 0; i < EditorNotebook.GetNPages(); i++) {
        //         if (args.Page == EditorNotebook.GetNthPage(i)) {
        //             window.SetTitle($"{Globals.Languages.ServeTrad("new_file")} - TeXSharp");
        //             break;
        //         }
        //         else
        //             window.SetTitle($"{this.GetCurrentSourceEditor().GetPath()} - TeXSharp");
        //     }
        // };
    }

    // Simple function to create a tab label for the Notebook pages. It simply returns a box with a label, the name of the file, and a close button (symbol of a cross) to close the tab
    public Gtk.Box EditorNotebookTabLabel(string label) {
        // We create the label
        Gtk.Label tabLabel = Gtk.Label.New(label);

        // We create the button
        Gtk.Button tabCloseButton = Gtk.Button.NewFromIconName("window-close-symbolic");
        tabCloseButton.SetHasFrame(false);
        tabCloseButton.OnClicked += (sender, args) => { this.CloseFile(); };

        // We create the box that will contain the label and the button
        Gtk.Box tabBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        tabBox.Append(tabLabel);
        tabBox.Append(tabCloseButton);

        return tabBox;
    }

    public void NewFile() {
        this.Editors.Add(new SourceEditor());
        this.EditorNotebook.AppendPage(this.Editors[Editors.Count - 1]._Box, EditorNotebookTabLabel(Globals.Languages.ServeTrad("new_file")));
        this.EditorNotebook.SetTabReorderable(this.Editors[Editors.Count - 1]._Box, true);
        this.EditorNotebook.NextPage();
    }

    public void OpenFile(string? path) {
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot open file");
        this.Editors.Add(new SourceEditor());
        this.Editors[Editors.Count - 1].OpenFile(path);
        this.EditorNotebook.AppendPage(this.Editors[Editors.Count - 1]._Box, EditorNotebookTabLabel(path.Split('/').Last()));
        this.EditorNotebook.SetTabReorderable(this.Editors[Editors.Count - 1]._Box, true);
        this.EditorNotebook.NextPage();
    }

    public void SaveFile(string? path) {
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot save file");
        this.Editors[this.GetCurrentEditorIndex()].SaveFile(path);
        this.EditorNotebook.SetTabLabel(this.EditorNotebook.GetNthPage(this.GetCurrentEditorIndex()), EditorNotebookTabLabel(path.Split('/').Last()));
    }

    public void CloseFile() {
        // We remove the page from the notebook
        // We make sure that we don't close the first tab. We need at least one tab open
        if (this.EditorNotebook.GetNPages() > 1) {
            int EditorToDelete = this.GetCurrentEditorIndex();
            this.EditorNotebook.RemovePage(EditorToDelete);
            // We remove the editor from the list
            this.Editors.RemoveAt(EditorToDelete);
        }
    }

    // Manuals getters and setters
    public int GetCurrentEditorIndex() {
        // We get the current page number
        return this.EditorNotebook.GetCurrentPage();
    }
    public SourceEditor GetCurrentSourceEditor() {
        // We get the current page number
        return this.Editors[this.EditorNotebook.GetCurrentPage()];
    }
}

public class SourceEditor {
    private GtkSource.Buffer Buffer;
    public GtkSource.Buffer _Buffer {
        get { return this.Buffer; } // get method
    }

    private GtkSource.View View;
    public GtkSource.View _View {
        get { return this.View; } // get method
    }

    private string Path;
    public string _Path {
        get { return this.Path; }
    }

    private bool FileExists = false;
    public bool _FileExists {
        get { return this.FileExists; }
    }

    private LanguageManager LanguageManager = GtkSource.LanguageManager.New();

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

    private Gtk.ScrolledWindow Scrolled = Gtk.ScrolledWindow.New();
    public Gtk.ScrolledWindow _Scrolled {
        get { return this.Scrolled; }
    }

    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
    public Gtk.Box _Box {
        get { return this.Box; }
    }

    private WSocket.WebSocketClient? WsClient = null;
    private WSocket.WebSocketServer? WsServer = null;

    public SourceEditor() {
        // Second, we create the buffer and the view
        this.Buffer = GtkSource.Buffer.New(null);
        this.View = GtkSource.View.NewWithBuffer(this.Buffer);

        // We set some properties to the view
        this.View.Monospace = true;
        this.View.ShowLineNumbers = true;
        this.View.HighlightCurrentLine = true;
        this.View.SetTabWidth(4);

        // We set the style scheme of the buffer
        this.Buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme(Globals.Settings._SettingsValues.EditorTheme));

        // We set the path to an empty string by default
        this.Path = string.Empty;

        this.Scrolled.SetHexpand(true);
        this.Scrolled.SetVexpand(true);
        this.Scrolled.SetChild(this.View);
        this.Box.Append(this.Scrolled);
        this.Box.Append(this.TextEntry);
        this.TextEntry.Hide();
        this.NewFile();
    }

    public void NewFile() {
        this.FileExists = false;
        this.Buffer.Text = "";
    }

    public void OpenFile(string path) {
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot open file");
        this.SetPath(path);

        this.GetBuffer().Text = System.IO.File.ReadAllText(this.GetPath());
        this.SetFileExists(true);
        this.SetBufferLanguage();
    }

    public void SaveFile(string path) {
        if (path is null)
            throw new System.ArgumentNullException("String path is null, cannot save file");
        if (!this.GetFileExists()) {
            this.SetPath(path);
        }
        System.IO.File.WriteAllText(path, this.GetBuffer().Text);
        this.SetFileExists(true);
        this.Buffer.Language = this.LanguageManager.GuessLanguage(path, null);
        this.SetBufferLanguage();
    }

    public void ChangeEditorTheme(string theme) { this.Buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme(theme)); }

    public async void StartWebSocketServer(int port, Gtk.Label statusBar) {
        if (!(this.WsServer is null)) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("server_already_started"));
            return;
        }
        if (!(this.WsClient is null)) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("server_did_not_start") + Globals.Languages.ServeTrad("client_already_started"));
            return;
        }
        if (!(this.GetFileExists())) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("server_did_not_start") + " " + Globals.Languages.ServeTrad("not_saved"));
            return;
        }
        this.WsServer = new WSocket.WebSocketServer(port, this.GetBuffer());
        try {
            statusBar.SetLabel(Globals.Languages.ServeTrad("server_did_start"));
            await this.WsServer.StartAsync(statusBar);
            if (this.WsServer._Failed)
                statusBar.SetLabel(Globals.Languages.ServeTrad("server_did_not_start"));
                this.WsServer = null;
        } catch (Exception e) {
            this.WsServer = null;
        }
    }

    public async void StopWebSocketServer(Gtk.Label statusBar) {
        if (this.WsServer is null) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("server_not_started"));
            return;
        }
        await this.WsServer.StopAsync();
        this.WsServer = null;
        statusBar.SetLabel(Globals.Languages.ServeTrad("server_stoped"));
    }

    public async void StartWebSocketClient(string server, int port, Gtk.Label statusBar) {
        if (!(this.WsClient is null)) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_already_started"));
            return;
        }
        if (!(this.WsServer is null)) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_did_not_start") + Globals.Languages.ServeTrad("server_already_started"));
            return;
        }
        if (this.GetFileExists()) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_did_not_start") + " " + Globals.Languages.ServeTrad("please_create_new_file"));
            return;
        }
        this.WsClient = new WSocket.WebSocketClient($"ws://{server}:{port}/");
        try {
            this.WsClient.Connected += (s, e) => Console.WriteLine("Connected to server");
            this.WsClient.Disconnected += (s, e) => {
                this.StopWebSocketClient(statusBar);
                Console.WriteLine("Disconnected from server");
            };
            this.WsClient.MessageReceived += (s, message) => {
                Console.WriteLine($"Received: {message}");
                this.HandleWebSocketClientMessage(message);
            };
            this.WsClient.ErrorOccurred += (s, ex) => Console.WriteLine($"Error: {ex.Message}");
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_did_connect"));
            await this.WsClient.ConnectAsync();
        } catch (Exception e) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_did_not_connect") + " " + e.Message);
            this.WsClient.Dispose();
            this.WsClient = null;
        }
    }

    public async void StopWebSocketClient(Gtk.Label statusBar) {
        if (this.WsClient is null) {
            statusBar.SetLabel(Globals.Languages.ServeTrad("client_not_connected"));
            return;
        }
        await this.WsClient.DisconnectAsync();
        this.WsClient.Dispose();
        this.WsClient = null;
        statusBar.SetLabel(Globals.Languages.ServeTrad("client_disconnected"));
    }

    private void HandleWebSocketClientMessage(string message) {
        if (message.StartsWith("full\n"))
            this.Buffer.Text = message;
    }

    // Manuals Getters and setters
    public GtkSource.Buffer GetBuffer() { return this.Buffer; }
    // Useless for now. Eventually have a use case for the shortcuts, but it doesn't seem to work right now
    public GtkSource.View GetView() { return this.View; }

    public string GetPath() { return this.Path; }
    public void SetPath(string path) { this.Path = path; }

    public bool GetFileExists() { return this.FileExists; }
    public void SetFileExists(bool fileExists) { this.FileExists = fileExists; }

    private void SetBufferLanguage() { this.Buffer.Language = this.LanguageManager.GuessLanguage(this.Path, null); }

    public bool GetVIMmodeEnabled() { return this.VIMmodeEnabled; }
    public void SetVIMmodeEnabled(bool VIMmodeEnabled) { this.VIMmodeEnabled = VIMmodeEnabled; }

    public Gtk.Entry GetTextEntry() { return this.TextEntry; }

    public GtkSource.VimIMContext GetVIMmode() { return this.VIMmode; }

    public Gtk.EventControllerKey GetVIMeventControllerKey() { return this.VIMeventControllerKey; }
}
