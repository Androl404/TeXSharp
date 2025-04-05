using System.Diagnostics;
using Gtk;
using GtkSource;

/// <summary>
/// Wrapper class managing a notebook of source editors.
/// Provides tabbed editing functionality.
/// </summary>
public class SourceEditorWrapper {
    /// <value>Attribute containing the notebook for multiple files.</value>
    private Gtk.Notebook EditorNotebook = Gtk.Notebook.New();
    /// <value>Public getter for the internal Gtk.Notebook used to manage tabs.</value>
    public Gtk.Notebook _EditorNotebook {
        get { return this.EditorNotebook; }
    }

    /// <value>List of all the source editors to manage the multiple files.</value>
    /// <remarks>The idnex must be the same than in the <c>EditorNotebook</c>.</remarks>
    private List<SourceEditor> Editors = new List<SourceEditor>();

    /// <summary>
    /// Initializes the editor wrapper with a single default tab.
    /// </summary>
    public SourceEditorWrapper(Gtk.Window window) {
        this.Editors.Add(new SourceEditor());
        this.EditorNotebook.AppendPage(this.Editors[0]._Box, EditorNotebookTabLabel(Globals.Languages.Translate("new_file")));
        this.EditorNotebook.SetTabReorderable(this.Editors[0]._Box, true);
        // TODO: Change title of Window when tabs are switched in the Notebook
        // this.EditorNotebook.OnSwitchPage += (notebook, args) => {
        //     for (int i = 0; i < EditorNotebook.GetNPages(); i++) {
        //         if (args.Page == EditorNotebook.GetNthPage(i)) {
        //             window.SetTitle($"{Globals.Languages.Translate("new_file")} - TeXSharp");
        //             break;
        //         }
        //         else
        //             window.SetTitle($"{this.GetCurrentSourceEditor().GetPath()} - TeXSharp");
        //     }
        // };
    }

    /// <summary>
    /// Create a tab label for the Notebook pages. It simply returns a box with a label, the name of the file, and a close button (symbol of a cross) to close the tab.
    /// </summary>
    /// <param name="label">The label to put into the notebook tab.</param>
    /// <returns>Returns a box to put into the tab fo the notebook.</returns>
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

    /// <summary>
    /// Creates a new file in a new tab.
    /// </summary>
    /// <returns>This method does not return anything.</returns>
    public void NewFile() {
        this.Editors.Add(new SourceEditor());
        this.EditorNotebook.AppendPage(this.Editors[Editors.Count - 1]._Box, EditorNotebookTabLabel(Globals.Languages.Translate("new_file")));
        this.EditorNotebook.SetTabReorderable(this.Editors[Editors.Count - 1]._Box, true);
        this.EditorNotebook.NextPage();
    }

    /// <summary>
    /// Open the current file to the specified path.
    /// </summary>
    /// <param name="label">The path in which to open the file. It might be null.</param>
    /// <returns>This method does not return anything.</returns>
    public void OpenFile(string? path) {
        Debug.Assert(!(path is null), "String path is null, cannot open file");
        this.Editors.Add(new SourceEditor());
        this.Editors[Editors.Count - 1].OpenFile(path);
        this.EditorNotebook.AppendPage(this.Editors[Editors.Count - 1]._Box, EditorNotebookTabLabel(path.Split('/').Last()));
        this.EditorNotebook.SetTabReorderable(this.Editors[Editors.Count - 1]._Box, true);
        this.EditorNotebook.NextPage();
    }

    /// <summary>
    /// Saves the current file to the specified path.
    /// </summary>
    /// <param name="label">The path in which to save the file. It might be null.</param>
    /// <returns>This method does not return anything.</returns>
    public void SaveFile(string? path) {
        Debug.Assert(!(path is null), "String path is null, cannot open file");
        this.Editors[this.GetCurrentEditorIndex()].SaveFile(path);
        this.EditorNotebook.SetTabLabel(this.EditorNotebook.GetNthPage(this.GetCurrentEditorIndex()), EditorNotebookTabLabel(path.Split('/').Last()));
    }

    /// <summary>
    /// Closes the current file with the tab of the notebook.
    /// </summary>
    /// <returns>This method does not return anything.</returns>
    public void CloseFile() {
        if (this.GetCurrentSourceEditor().GetFileExists())
            this.GetCurrentSourceEditor().SaveFile(this.GetCurrentSourceEditor().GetPath());
        // We remove the page from the notebook
        // We make sure that we don't close the first tab. We need at least one tab open
        if (this.EditorNotebook.GetNPages() > 1) {
            int EditorToDelete = this.GetCurrentEditorIndex();
            this.EditorNotebook.RemovePage(EditorToDelete);
            // We remove the editor from the list
            this.Editors.RemoveAt(EditorToDelete);
        }
    }

    // Manual getters
    /// <summary>
    /// Gets the current editor index.
    /// </summary>
    /// <returns>Returns the current editor index (an integer).</returns>
    public int GetCurrentEditorIndex() {
        return this.EditorNotebook.GetCurrentPage(); // We get the current page number
    }

    /// <summary>
    /// Gets the current source editor.
    /// </summary>
    /// <returns>Returns the current <c>SourceEditor</c>.</returns>
    public SourceEditor GetCurrentSourceEditor() {
        return this.Editors[this.EditorNotebook.GetCurrentPage()]; // We get the current page number
    }
}

/// <summary>
/// Represents a single source code editor with VIM mode, collaborative editing, and file operations.
/// </summary>
public class SourceEditor {
    /// <value>The buffer in which is stored the text.</value>
    private GtkSource.Buffer Buffer;
    /// <value>A wrapper around the buffer.</value>
    public GtkSource.Buffer _Buffer {
        get { return this.Buffer; }
    }

    /// <value>The view in which is shown the text.</value>
    private GtkSource.View View;
    /// <value>A wrapper around the view.</value>
    public GtkSource.View _View {
        get { return this.View; }
    }

    /// <value>The path to the editor files.</value>
    private string Path;
    /// <value>A wrapper around the path.</value>
    public string _Path {
        get { return this.Path; }
    }

    /// <value>A boolean to indicate if the file exists on the file system.</value>
    private bool FileExists = false;
    /// <value>A wrapper around the <c>FileExists</c> attribute.</value>
    public bool _FileExists {
        get { return this.FileExists; }
    }

    /// <value>The <c>LanguageManager</c> which detects the language of the buffer.</value>
    private LanguageManager LanguageManager = GtkSource.LanguageManager.New();

    /// <value>Attribute related to VIM mode.</value>
    private GtkSource.VimIMContext VIMmode = GtkSource.VimIMContext.New();
    /// <value>Wrapper around attribute related to VIM mode.</value>
    public GtkSource.VimIMContext _VIMmode {
        get { return this.VIMmode; }
    }

    /// <value>Attribute related to VIM mode.</value>
    private Gtk.EventControllerKey VIMeventControllerKey = Gtk.EventControllerKey.New();
    /// <value>Wrapper around attribute related to VIM mode.</value>
    public Gtk.EventControllerKey _VIMeventControllerKey {
        get { return this.VIMeventControllerKey; }
    }

    /// <value>Boolean to indicate if VIM mode is active into the current editor.</value>
    /// <remarks><c>false</c> if VIM mode is disabled, <c>true</c> if VIM mode is enabled (default is disabled)</remarks>
    private bool VIMmodeEnabled = false;
    /// <value>Wrapper around the boolean which indicate if VIM mode is active into the current editor.</value>
    public bool _VIMmodeEnabled {
        get { return this.VIMmodeEnabled; }
        set { this.VIMmodeEnabled = value; }
    }

    /// <value>Entry for the VIM mode.</value>
    private Gtk.Entry TextEntry = new Gtk.Entry();
    /// <value>Wrapper around the entry for the VIM mode.</value>
    public Gtk.Entry _TextEntry {
        get { return this.TextEntry; }
    }

    /// <value>Scrolling window which will contain the editor.</value>
    private Gtk.ScrolledWindow Scrolled = Gtk.ScrolledWindow.New();
    /// <value>Wrapper around the scrolling window which will contain the editor.</value>
    public Gtk.ScrolledWindow _Scrolled {
        get { return this.Scrolled; }
    }

    /// <value>Box which will contain the buffer and the netry for the VIM mode.</value>
    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
    /// <value>Wrapper around the box which will contain the buffer and the netry for the VIM mode.</value>
    public Gtk.Box _Box {
        get { return this.Box; }
    }

    /// <value>The client for the WebSocket.</value>
    /// <remarks>Is null is inactive.</remarks>
    private WSocket.WebSocketClient? WsClient = null;

    /// <value>The server for the WebSocket.</value>
    /// <remarks>Is null is inactive.</remarks>
    private WSocket.WebSocketServer? WsServer = null;

    /// <value>The parser for real time collaboration receveid messages.</value>
    private WsMessageParser Parser = new WsMessageParser();

    /// <value>A boolean which indicate if we should synchronize the change if we are collaborating.</value>
    private bool SyncChanges = false;

    /// <value>A string containing the old text of the buffer to find the changes and send them throught network.</value>
    private string? OldBufferText = null;

    /// <summary>
    /// Initializes a new instance of the SourceEditor.
    /// </summary>
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
        // For collaborative editing
        this.Buffer.OnChanged += (buffer, args) => {
            if (this.Buffer.Text is null || this.OldBufferText is null)
                return;
            if (this.Buffer.Text.Length == this.OldBufferText.Length)
                return;
            this.GetDiffs();
            this.OldBufferText = this.Buffer.Text;
        };
    }

    /// <summary>
    /// Resets buffer content for a new file.
    /// </summary>
    /// <returns>This method does not return anything.</returns>
    public void NewFile() {
        this.FileExists = false;
        this.Buffer.Text = "";
    }

    /// <summary>
    /// Loads file contents into the editor.
    /// </summary>
    /// <param name="path">The path in which to open the file.</param>
    /// <returns>This method does not return anything.</returns>
    public void OpenFile(string path) {
        Debug.Assert(!(path is null), "String path is null, cannot open file");
        this.SetPath(path);
        this.GetBuffer().Text = System.IO.File.ReadAllText(this.GetPath());
        this.SetFileExists(true);
        this.SetBufferLanguage();
    }

    /// <summary>
    /// Saves buffer contents to a file.
    /// </summary>
    /// <param name="path">The path in which to save the file.</param>
    /// <returns>This method does not return anything.</returns>
    public void SaveFile(string path) {
        Debug.Assert(!(path is null), "String path is null, cannot save file");
        if (!this.GetFileExists()) {
            this.SetPath(path);
        }
        System.IO.File.WriteAllText(path, this.GetBuffer().Text);
        this.SetFileExists(true);
        this.Buffer.Language = this.LanguageManager.GuessLanguage(path, null);
        this.SetBufferLanguage();
    }

    /// <summary>
    /// Changes the syntax highlighting theme.
    /// </summary>
    /// <param name="theme">The theme to apply to the view.</param>
    /// <returns>This method does not return anything.</returns>
    public void ChangeEditorTheme(string theme) { this.Buffer.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme(theme)); }

    /// <summary>
    /// Starts a WebSocket server for collaborative editing.
    /// </summary>
    /// <param name="port">The port to which to listen.</param>
    /// <param name="statusBar">The status bar to give feedback to the user.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StartWebSocketServer(int port, Gtk.Label statusBar) {
        if (!(this.WsServer is null)) {
            statusBar.SetLabel(Globals.Languages.Translate("server_already_started"));
            return;
        }
        if (!(this.WsClient is null)) {
            statusBar.SetLabel(Globals.Languages.Translate("server_did_not_start") + Globals.Languages.Translate("client_already_started"));
            return;
        }
        if (!(this.GetFileExists())) {
            statusBar.SetLabel(Globals.Languages.Translate("server_did_not_start") + " " + Globals.Languages.Translate("not_saved"));
            return;
        }
        this.WsServer = new WSocket.WebSocketServer(port, this.GetBuffer());
        try {
            statusBar.SetLabel(Globals.Languages.Translate("server_did_start"));
            this.StartSync();
            this.WsServer.MessageReceived += (s, message) => { this.ReceivedMessage(message); };
            await this.WsServer.StartAsync();
            if (this.WsServer._Failed) {
                statusBar.SetLabel(Globals.Languages.Translate("server_did_not_start"));
                this.WsServer = null;
            }
        } catch (Exception e) {
            this.WsServer = null;
            this.StopSync();
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    /// <param name="statusBar">The status bar to give feedback to the user.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StopWebSocketServer(Gtk.Label statusBar) {
        if (this.WsServer is null) {
            statusBar.SetLabel(Globals.Languages.Translate("server_not_started"));
            return;
        }
        await this.WsServer.StopAsync();
        this.WsServer = null;
        this.StopSync();
        statusBar.SetLabel(Globals.Languages.Translate("server_stoped"));
    }

    /// <summary>
    /// Connects to a collaborative WebSocket server as a client.
    /// </summary>
    /// <param name="server">The server to which to connect.</param>
    /// <param name="port">The port to which to connect.</param>
    /// <param name="statusBar">The status bar to give feedback to the user.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StartWebSocketClient(string server, int port, Gtk.Label statusBar) {
        if (!(this.WsClient is null)) {
            statusBar.SetLabel(Globals.Languages.Translate("client_already_started"));
            return;
        }
        if (!(this.WsServer is null)) {
            statusBar.SetLabel(Globals.Languages.Translate("client_did_not_start") + Globals.Languages.Translate("server_already_started"));
            return;
        }
        if (this.GetFileExists()) {
            statusBar.SetLabel(Globals.Languages.Translate("client_did_not_start") + " " + Globals.Languages.Translate("please_create_new_file"));
            return;
        }
        this.WsClient = new WSocket.WebSocketClient($"ws://{server}:{port}/");
        try {
            this.WsClient.Connected += (s, e) => Console.WriteLine("Connected to server");
            this.WsClient.Disconnected += (s, e) => Console.WriteLine("Disconnected from server");
            this.WsClient.MessageReceived += (s, message) => { this.ReceivedMessage(message); };
            this.WsClient.ErrorOccurred += (s, ex) => Console.WriteLine($"Error: {ex.Message}");
            statusBar.SetLabel(Globals.Languages.Translate("client_did_connect"));
            await this.WsClient.ConnectAsync();
            this.StartSync();
            this.OldBufferText = this.Buffer.Text;
        } catch (Exception e) {
            statusBar.SetLabel(Globals.Languages.Translate("client_did_not_connect") + " " + e.Message);
            this.WsClient.Dispose();
            this.WsClient = null;
            this.StopSync();
        }
    }

    /// <summary>
    /// Disconnects the editor from a WebSocket server.
    /// </summary>
    /// <param name="statusBar">The status bar to give feedback to the user.</param>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    public async Task StopWebSocketClient(Gtk.Label statusBar) {
        if (this.WsClient is null) {
            statusBar.SetLabel(Globals.Languages.Translate("client_not_connected"));
            return;
        }
        await this.WsClient.DisconnectAsync();
        this.WsClient.Dispose();
        this.WsClient = null;
        this.StopSync();
        statusBar.SetLabel(Globals.Languages.Translate("client_disconnected"));
    }

    /// <summary>
    /// Handle the received message.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>This methods does not return anything.</returns>
    private void ReceivedMessage(string message) {
        var final_message = Parser.ParseMessage(message);
        if (final_message.Type == WsMessageParser.MessageType.FullMessageComplete) {
            this.StopSync();
            this.Buffer.Text = final_message.Content;
            this.StartSync();
        } else if (final_message.Type == WsMessageParser.MessageType.RelativeMessageComplete) {
            this.StopSync();
            var MessageContent = final_message.Content.Split(':');
            if (MessageContent[0] == "insertion") {
                if (MessageContent[2].Contains("/colon/"))
                    MessageContent[2] = ":";
                if (int.Parse(MessageContent[1]) > this.Buffer.Text.Length) {
                    this.Buffer.Text += MessageContent[2][0].ToString();
                } else {
                    this.Buffer.Text = this.Buffer.Text.Insert(int.Parse(MessageContent[1]), MessageContent[2][0].ToString());
                }
            } else if (MessageContent[0] == "deletion") {
                if (int.Parse(MessageContent[1]) == this.Buffer.Text.Length) {
                    this.Buffer.Text = this.Buffer.Text[..^ 1];
                } else {
                    this.Buffer.Text = this.Buffer.Text.Remove(int.Parse(MessageContent[1]), 1);
                }
            }
            this.StartSync();
        }
    }

    /// <summary>
    /// Parse the buffer and the attribute <see cref="OldBufferText"/> to get the changes made by the user.
    /// </summary>
    /// <returns>This methods does return a task because it is asynchronous.</returns>
    async private Task GetDiffs() {
        if (!this.SyncChanges)
            return;
        string? Text = this.Buffer.Text;
        bool? Insertion = null;
        int Length = 0;
        string Diff = string.Empty;
        if (Text.Length - 1 == this.OldBufferText.Length) {
            // Insertion
            Insertion = true;
            Length = this.OldBufferText.Length;
        } else if (Text.Length + 1 == this.OldBufferText.Length) {
            // Deletion
            Insertion = false;
            Length = Text.Length;
        } else if (Text.Length != this.OldBufferText.Length) { // Strings differ of multiple caracters, we should send all the buffer
            if (!(this.WsServer is null)) {                    // The server is active
                await this.WsServer.BroadcastMessage($"full:sample-guid-1234:START\n" + Text + $"\nfull:sample-guid-1234:STOP\n");
            } else if (!(this.WsClient is null)) { // The client is active
                await this.WsClient.SendMessageAsync($"full:sample-guid-1234:START\n" + Text + $"\nfull:sample-guid-1234:STOP\n");
            }
            this.OldBufferText = Text;
            return;
        } else
            return; // The strings are equals, ans something weird is going on.
        // We should send a full copy of the file then
        for (int i = 0; i < Length; i++) {
            if (Text[i] != this.OldBufferText[i]) {
                if ((bool)Insertion) {
                    if (!(Text[i] == ':'))
                        Diff = $"insertion:{i}:{Text[i]}";
                    else
                        Diff = $"insertion:{i}:/colon/";
                    break;
                } else {
                    Diff = $"deletion:{i}:{this.OldBufferText[i]}";
                    break;
                }
            }
        }
        if (Diff == string.Empty) {
            if ((bool)Insertion) {
                if (!(Text[Text.Length - 1] == ':'))
                    Diff = $"insertion:{Text.Length}:{Text[Text.Length - 1]}";
                else
                    Diff = $"insertion:{Text.Length}:/colon/";
            } else {
                Diff = $"deletion:{this.OldBufferText.Length}:{this.OldBufferText[this.OldBufferText.Length - 1]}";
            }
        }
        string message = $"relative:START\n{Diff}\nrelative:STOP\n";
        if (Diff != string.Empty) {
            if (!(this.WsServer is null)) { // The server is active
                await this.WsServer.BroadcastMessage(message);
            } else if (!(this.WsClient is null)) { // The client is active
                await this.WsClient.SendMessageAsync(message);
            }
        }
        this.OldBufferText = this.Buffer.Text;
    }

    /// <summary>
    /// Starts the synchronization.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void StartSync() {
        this.SyncChanges = true;
        this.OldBufferText = this.Buffer.Text;
    }

    /// <summary>
    /// Stops the synchronization.
    /// </summary>
    /// <returns>This methods does not return anything.</returns>
    private void StopSync() {
        this.SyncChanges = false;
        this.OldBufferText = null;
    }

    // Manual getters and setters
    /// <summary>
    /// Gets the current buffer.
    /// </summary>
    /// <returns>Returns the current buffer.</returns>
    public GtkSource.Buffer GetBuffer() { return this.Buffer; }

    /// <summary>
    /// Gets the current view.
    /// </summary>
    /// <returns>Returns the current view.</returns>
    public GtkSource.View GetView() { return this.View; }

    /// <summary>
    /// Gets the current file path.
    /// </summary>
    /// <returns>Returns the current file path.</returns>
    public string GetPath() { return this.Path; }

    /// <summary>
    /// Sets the file path.
    /// </summary>
    /// <param name="path">The path to set to the source editor.</param>
    /// <returns>Does not return anything.</returns>
    public void SetPath(string path) { this.Path = path; }

    /// <summary>
    /// Gets if the file of the editor exists.
    /// </summary>
    /// <returns>Returns if the file of the editor exists (a boolean).</returns>
    public bool GetFileExists() { return this.FileExists; }

    /// <summary>
    /// Sets if the file exists.
    /// </summary>
    /// <param name="fileExists">The value to set the attribute to.</param>
    /// <returns>Does not return anything.</returns>
    public void SetFileExists(bool fileExists) { this.FileExists = fileExists; }

    /// <summary>
    /// Sets the buffer language automatically with the <c>LanguageManager</c>.
    /// </summary>
    /// <returns>Does not return anything.</returns>
    private void SetBufferLanguage() { this.Buffer.Language = this.LanguageManager.GuessLanguage(this.Path, null); }

    /// <summary>
    /// Gets if VIM mode is enabled.
    /// </summary>
    /// <returns>Returns if VIM mode is enabled (a boolean).</returns>
    public bool GetVIMmodeEnabled() { return this.VIMmodeEnabled; }

    /// <summary>
    /// Sets if VIM mode is enabled.
    /// </summary>
    /// <param name="VIMmodeEnabled">The value to set the attribute to.</param>
    /// <returns>Does not return anything.</returns>
    public void SetVIMmodeEnabled(bool VIMmodeEnabled) { this.VIMmodeEnabled = VIMmodeEnabled; }

    /// <summary>
    /// Gets the text entry for VIM mode.
    /// </summary>
    /// <returns>Returns the entry for the VIM mode.</returns>
    public Gtk.Entry GetTextEntry() { return this.TextEntry; }

    /// <summary>
    /// Gets the VIM mode context.
    /// </summary>
    /// <returns>Returns the VIM mode context.</returns>
    public GtkSource.VimIMContext GetVIMmode() { return this.VIMmode; }

    /// <summary>
    /// Gets the VIM mode <c>EventControllerKey</c>.
    /// </summary>
    /// <returns>Returns the VIM mode <c>EventControllerKey</c>.</returns>
    public Gtk.EventControllerKey GetVIMeventControllerKey() { return this.VIMeventControllerKey; }
}
