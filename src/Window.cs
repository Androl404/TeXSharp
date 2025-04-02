using System;
using System.Runtime.InteropServices;
using Gtk;
using Gio;
using GtkSource;
using IronPdf;
using GLib;
using Cairo;

class Window {
    private Gio.Application Sender;  // The sender args on window activation
    public Gio.Application _Sender { // Public property used to access the sender attribute in read-only
        get { return this.Sender; }  // get method
    }
    private Gtk.ApplicationWindow MWindow;  // The main window
    public Gtk.ApplicationWindow _MWindow { // Public property used to access the window attribute in read-only
        get { return this.MWindow; }        // get method
    }
    private Gtk.Grid Grid = Gtk.Grid.New();
    public Gtk.Grid _Grid {
        get { return this.Grid; } // get method
    }

    private ButtonBar ButtonBar = new ButtonBar();
    private Gtk.ScrolledWindow? PDFViewer;
    private Gtk.ScrolledWindow TextEditor;

    private SourceEditorWrapper EditorWrapper;

    // Constructor of the windows
    // Takes title, size, and flag from event in Main
    public Window(string title, int sizeX, int sizeY, Gio.Application sender) {
        this.MWindow = Gtk.ApplicationWindow.New((Gtk.Application)sender); // Create the window
        this.MWindow.Title = title;                                        // Set the title
        this.MWindow.SetDefaultSize(sizeX, sizeY);                         // Set the size (x, y)
        this.MWindow.Show();                                               // Show the window (it's always better to see it)
        // To set the sender arg
        this.Sender = sender;
        this.EditorWrapper = new SourceEditorWrapper(this.MWindow);
        // To create the grid
        this.Grid.SetHexpand(true);
        this.Grid.SetVexpand(true);
        this.Grid.ColumnSpacing = 10;
        this.MWindow.SetChild(this.Grid); // Set the grid as the window's child
        this.Grid.Attach(this.EditorWrapper._EditorNotebook, 0, 1, 1, 1);
        this.EditorWrapper._EditorNotebook.SetScrollable(true);
        this.MakeButtonBar();
        this.MakePDFViewerWrapper(null);
        this.MWindow.OnCloseRequest += (sender, args) => {
            if (this.EditorWrapper.GetCurrentSourceEditor().GetFileExists())
                this.GetFunc("save")(sender, null);
            return false;
        };
    }

    // To construct the header bar of the window
    // By default, the desktop manager takes care of that, but we decideed to make
    // our own header bar
    public void SetHeaderBar(Gtk.Window window) {
        var HeaderBar = new AppHeaderBar(); // Create the header bar

        // TODO: make the menu bar with all the options (file, edit, etc.)
        HeaderBar.AddMenuButon(Globals.Languages.ServeTrad("file"), false);
        HeaderBar.AddButtonInMenu([Globals.Languages.ServeTrad("new"), Globals.Languages.ServeTrad("open"), Globals.Languages.ServeTrad("save"), Globals.Languages.ServeTrad("exit")], ["Ctrl+N", "Ctrl+O", "Ctrl+S", ""], [GetFunc("new"), GetFunc("open"), GetFunc("save"), GetFunc("quit")], false, true);

        HeaderBar.AddMenuButon("LaTeX", false);
        HeaderBar.AddButtonInMenu([Globals.Languages.ServeTrad("compile")], ["Ctrl+Shift+C"], [GetFunc("compile")], false, true);

        HeaderBar.AddMenuButon(Globals.Languages.ServeTrad("tools"), false);
        HeaderBar.AddButtonInMenu([Globals.Languages.ServeTrad("settings")], ["Ctrl+Shift+P"], [GetFunc("toogle_settings")], false, true);

        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        var button_icon = Gio.ThemedIcon.New("open-menu-symbolic"); // We create an image with an icon
        HeaderBar.AddMenuButon(button_icon, false);
        HeaderBar.AddButtonInMenu([Globals.Languages.ServeTrad("about")], [""], [GetFunc("about")], false, false);
        HeaderBar.SetWindowHeaderBar(window);
    }

    async private void MakePDFViewerWrapper(string? pdfPath) { this.PDFViewer = await this.MakePDFViewer(pdfPath); }

    // To create the PDF viewer and returns the associated ScrolledWindow
    async public Task<Gtk.ScrolledWindow> MakePDFViewer(string? pdfPath) {
        // IronPdf.PdfDocument pdf = new IronPdf.PdfDocument("./assets/pdf_test.pdf");
        if (pdfPath is null) {
            var Scrolled = Gtk.ScrolledWindow.New();
            this.Grid.Attach(Scrolled, 1, 1, 1, 1); // Spans 3 columns in the third row/column
            return Scrolled;
        }
        IronPdf.PdfDocument Pdf = new IronPdf.PdfDocument(pdfPath);

        string Path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) // If we are on a niche operating system for games
                          ? Environment.ExpandEnvironmentVariables("%temp%/")
                          : "/tmp/";

        // Render the PDF as images in temp folder
        await System.Threading.Tasks.Task.Run(() => { Pdf.RasterizeToImageFiles($"{Path}*.png", 2160, 3840, IronPdf.Imaging.ImageType.Png, 300); });

        var ImageBox = Gtk.Box.New(Gtk.Orientation.Vertical, 5);

        for (int i = 1; i <= Pdf.PageCount; ++i) {
            // Usage of Gtk.Image widget
            // -------------------------
            // var imagePdf = Gtk.Image.NewFromFile(path + i + ".png");
            // Initial size of the image
            // imagePdf.PixelSize = 500;
            // var zoom = Gtk.GestureZoom.New();
            // We change the size of the image based on the scale factor, only when the zoom is detected (touchpad pinched)
            // zoom.OnScaleChanged += (sender, args) => { imagePdf.PixelSize = (int)(500 * zoom.GetScaleDelta()); };

            // We add the gesture zoom to the box so that the entire box is rescaled when zooming, and not the image alone
            // ImageBox.AddController(zoom);

            // Usage of Gtk.Picture widget
            // ---------------------------
            // Usage of Gtk.Picture widget instead of Gtk.Image
            var ImagePdf = Gtk.Picture.New();
            // for (int i = 1; i <= pdf.PageCount; ++i) {
            ImagePdf = Gtk.Picture.NewForFilename(Path + i + ".png");
            // Make the image fill the available space horizontally
            ImagePdf.SetHexpand(true);
            ImagePdf.SetContentFit(ContentFit.Fill);
            // IMPORTANT: this need to be on 'false' or else, the scrolled window will not work
            ImagePdf.SetCanShrink(false);
            // Keep the aspect ratio of the image, which avoid the image to be stretched when resizing the window
            ImagePdf.SetKeepAspectRatio(true);

            // And we add each image to the box
            ImageBox.Append(ImagePdf);
        }

        // We remove whatever is in the grid before showing the PDF
        if (Globals.Settings.GetShowing()) {
            var Func = this.GetFunc("toogle_settings");
            if (Func is null)
                throw new System.ArgumentNullException("Function is null");
            await Func(null, new EventArgs());
        }

        // We also clean the previous PDF file
        this.Grid.Remove(this.PDFViewer);

        // We put the PDF images into a scrollable element
        var ScrolledPdf = Gtk.ScrolledWindow.New();
        ScrolledPdf.SetHexpand(true);
        ScrolledPdf.SetVexpand(true);
        ScrolledPdf.SetChild(ImageBox);
        this.Grid.Attach(ScrolledPdf, 1, 1, 1, 1); // Spans 3 columns in the third row/column

        return ScrolledPdf;
    }

    public void MakeButtonBar() {
        this.ButtonBar.AddButton("new", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-new-symbolic")), GetFunc("new"));
        // We set the widget to the window so that it is possible to use the shortcut even if not focusing the editor view
        this.ButtonBar.AddShortcut(this.MWindow, "<Control>N", "newFileAction", GetFunc("new"), this.Sender);

        this.ButtonBar.AddButton("save", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-save-symbolic")), GetFunc("save"));
        // In this case, we set the widget to the editor view so that the shortcut is only available when focusing the editor view. (we don't want to save the file when we are not focusing the editor, right? (it can be slow))
        // EDIT : It actually depends, it works on new file, but seem to not work on opened file. Stricking to window for now.
        this.ButtonBar.AddShortcut(this.EditorWrapper._EditorNotebook, "<Control>S", "saveAction", GetFunc("save"), this.Sender);

        this.ButtonBar.AddButton("open", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-open-symbolic")), GetFunc("open"));
        this.ButtonBar.AddShortcut(this.MWindow, "<Control>O", "openAction", GetFunc("open"), this.Sender);

        this.ButtonBar.AddButton("close", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("window-close-symbolic")), GetFunc("close"));
        this.ButtonBar.AddShortcut(this.MWindow, "<Control>W", "closeAction", GetFunc("close"), this.Sender);

        this.ButtonBar.AddButton("compile", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("media-playback-start-symbolic")), GetFunc("compile"));
        this.ButtonBar.AddShortcut(this.MWindow, "<Control><Shift>C", "compileAction", GetFunc("compile"), this.Sender);

        this.ButtonBar.AddButton("vim", Gtk.Image.NewFromFile("./assets/vimlogo.png"), GetFunc("vim"));
        this.ButtonBar.AddShortcut(this.MWindow, "<Control><Shift>V", "vimAction", GetFunc("vim"), this.Sender);

        this.ButtonBar.AddButton("settings", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("applications-system-symbolic")), GetFunc("toogle_settings"));
        this.ButtonBar.AddShortcut(this.MWindow, "<Control><Shift>P", "toogle_settingsAction", GetFunc("toogle_settings"), this.Sender);

        this.ButtonBar.AddStatusBar();
        this.Grid.Attach(this.ButtonBar.GetBox(), 0, 0, 2, 1); // Spans 2 columns in the third row
    }

    private Func<object?, EventArgs, System.Threading.Tasks.Task>? GetFunc(string function) {
        var FuncOpen = async (object? sender, EventArgs args) => {
            var OpenDialog = Gtk.FileDialog.New();
            try {
                OpenDialog.SetTitle(Globals.Languages.ServeTrad("choose_file"));
                var OpenTask = OpenDialog.OpenAsync(this.MWindow);
                await OpenTask;
                if (OpenTask.Result is null)
                    throw new System.ArgumentNullException("Opening task is null.");
                this.EditorWrapper.OpenFile(OpenTask.Result.GetPath());
                this.MWindow.SetTitle($"{this.EditorWrapper.GetCurrentSourceEditor().GetPath()} - TeXSharp");
                // this.Grid.Attach(this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry(), 0, 2, 1, 1);
                this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry().Hide();

                this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("file_opened") + " " + OpenTask.Result.GetPath() + ".");
            } catch (Exception e) {
                Console.WriteLine("WARNING: Dismissed by user\n" + e.StackTrace);
            } finally {
                OpenDialog.Dispose();
                if (System.IO.File.Exists(this.EditorWrapper.GetCurrentSourceEditor().GetPath()[..^ 3] + "pdf"))
                    this.PDFViewer = await this.MakePDFViewer(this.EditorWrapper.GetCurrentSourceEditor().GetPath()[..^ 3] + "pdf");
            }
        };

        var FuncSave = async (object? sender, EventArgs args) => {
            var SaveDialog = Gtk.FileDialog.New();
            try {
                if (!this.EditorWrapper.GetCurrentSourceEditor().GetFileExists()) {
                    SaveDialog.SetTitle(Globals.Languages.ServeTrad("save_file"));
                    var SaveTask = SaveDialog.SaveAsync(this.MWindow);
                    await SaveTask;
                    if (SaveTask.Result is null)
                        throw new System.ArgumentNullException("Saving task is null.");
                    this.EditorWrapper.SaveFile(SaveTask.Result.GetPath());
                } else {
                    this.EditorWrapper.SaveFile(this.EditorWrapper.GetCurrentSourceEditor().GetPath());
                }
                this.MWindow.SetTitle($"{this.EditorWrapper.GetCurrentSourceEditor().GetPath()} - TeXSharp");
                this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("file_saved") + " " + this.EditorWrapper.GetCurrentSourceEditor().GetPath() + ".");
            } catch (Exception e) {
                Console.WriteLine($"WARNING: Dismissed by user, {e.Message} \n" + e.StackTrace);
            } finally {
                SaveDialog.Dispose();
            }
        };

        var FuncNewFile = async (object? sender, EventArgs args) => {
            this.EditorWrapper.NewFile();
            // The only way to add the TextEntry to the editor is here. Otherwise, SourceEditor class can't access the grid
            // this.Grid.Attach(this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry(), 0, 2, 1, 1);
            this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry().Hide();
            this.MWindow.SetTitle($"{Globals.Languages.ServeTrad("new_file")} - TeXSharp");
            this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("new_file") + " " + Globals.Languages.ServeTrad("created") + ".");
        };

        var FuncClose = async (object? sender, EventArgs args) => {
            this.EditorWrapper.CloseFile();
        };

        var FuncQuit = async (object? sender, EventArgs args) => {
            this.MWindow.Destroy();
        };

        var FuncAbout = async (object? sender, EventArgs args) => {
            var Dialog = new TAboutDialog("TeXSharp");
            Dialog.Application = (Gtk.Application)this.Sender; // CS0030: Impossible de convertir le type 'Gtk.Button' en 'Gtk.Application'
            Dialog.Show();
        };

        var FuncCompile = async (object? sender, EventArgs args) => {
            await FuncSave(sender, args);
            if (this.EditorWrapper.GetCurrentSourceEditor().GetFileExists()) {
                this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("compilation_started") + "...");
                var Process = await ProcessAsyncHelper.ExecuteShellCommand("latexmk", "-pdf -bibtex -interaction=nonstopmode -cd " + this.EditorWrapper.GetCurrentSourceEditor().GetPath(), 50000);
                if (Process.ExitCode == 0)
                    this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("compilation_finished") + ".");
                else
                    this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("compilation_finished") + " " + Globals.Languages.ServeTrad("with_errors") + ".");
                this.PDFViewer = await this.MakePDFViewer(this.EditorWrapper.GetCurrentSourceEditor().GetPath()[..^ 3] + "pdf");
            } else {
                this.ButtonBar._StatusBar.SetLabel(Globals.Languages.ServeTrad("not_saved_cannot_compile"));
            }
        };

        var FuncVim = async (object? sender, EventArgs args) => {
            // If the VIM mode is enabled (1), we disable it
            if (this.EditorWrapper.GetCurrentSourceEditor().GetVIMmodeEnabled()) {
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMeventControllerKey().SetPropagationPhase(Gtk.PropagationPhase.None);
                this.EditorWrapper.GetCurrentSourceEditor().GetView().RemoveController(this.EditorWrapper.GetCurrentSourceEditor().GetVIMeventControllerKey());
                this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry().Hide();
                this.EditorWrapper.GetCurrentSourceEditor().SetVIMmodeEnabled(false);
                this.ButtonBar._StatusBar.SetLabel("VIM Mode " + Globals.Languages.ServeTrad("desactivated") + ".");
            } else {
                // If the VIM mode is disabled (0), we enable it
                this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry().Show();
                this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry().SetPlaceholderText("Vim command bar");

                // Set the IM context to the event controller key
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMeventControllerKey().SetImContext(this.EditorWrapper.GetCurrentSourceEditor().GetVIMmode());
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMeventControllerKey().SetPropagationPhase(Gtk.PropagationPhase.Capture);
                // Add the event controller key to the view
                // And the vim input module context to the view (editor)
                this.EditorWrapper.GetCurrentSourceEditor().GetView().AddController(this.EditorWrapper.GetCurrentSourceEditor().GetVIMeventControllerKey());
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMmode().SetClientWidget(this.EditorWrapper.GetCurrentSourceEditor().GetView());

                // Bind the command bar text to the text entry so that when we type ":" in the editor it will show up in the text entry at the bottom
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMmode().BindProperty("command-bar-text", this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry(), "text", 0);
                this.EditorWrapper.GetCurrentSourceEditor().GetVIMmode().BindProperty("command-text", this.EditorWrapper.GetCurrentSourceEditor().GetTextEntry(), "text", 0);

                this.EditorWrapper.GetCurrentSourceEditor().SetVIMmodeEnabled(true);
                this.ButtonBar._StatusBar.SetLabel("VIM Mode " + Globals.Languages.ServeTrad("activated") + ".");
            }
        };

        var FuncToggleSettings = async (object? sender, EventArgs args) => {
            if (!Globals.Settings.GetShowing()) {
                Globals.Settings.OnToggle(this.EditorWrapper, this.ButtonBar._StatusBar);
                this.Grid.Remove(this.PDFViewer);
                this.Grid.Attach(Globals.Settings.GetScrolledWindow(), 1, 1, 1, 1);
                // this.Grid.AttachNextTo(this.settings.GetScrolledWindow(), this.TextEditor, Gtk.PositionType.Right, 1, 1);
                Globals.Settings.SetShowing(true);
            } else {
                this.Grid.Remove(Globals.Settings.GetScrolledWindow());
                this.Grid.Attach(this.PDFViewer, 1, 1, 1, 1); // Spans 3 columns in the third row/column
                Globals.Settings.SetShowing(false);
            }
        };

        switch (function) {
        case "new":
            return FuncNewFile;
        case "open":
            return FuncOpen;
        case "save":
            return FuncSave;
        case "close":
            return FuncClose;
        case "compile":
            return FuncCompile;
        case "quit":
            return FuncQuit;
        case "about":
            return FuncAbout;
        case "vim":
            return FuncVim;
        case "toogle_settings":
            return FuncToggleSettings;
        default:
            return null;
        }
    }

    // Manuals getters
    public Gtk.Window GetWindow() { return this.MWindow; }
    public Gtk.Grid GetGrid() { return this.Grid; }
}
