using System;
using System.Runtime.InteropServices;
using Gtk;
using Gio;
using GtkSource;
using IronPdf;
using GLib;
using Cairo;

class Window {
    private Gio.Application sender;  // The sender args on window activation
    public Gio.Application _Sender { // Public property used to access the sender attribute in read-only
        get { return this.sender; }  // get method
    }
    private Gtk.ApplicationWindow window;  // The main window
    public Gtk.ApplicationWindow _Window { // Public property used to access the window attribute in read-only
        get { return this.window; }        // get method
    }
    private Gtk.Grid grid;
    public Gtk.Grid _Grid {
        get { return this.grid; } // get method
    }

    private ButtonBar button_bar;
    private Gtk.ScrolledWindow PDFViewer;
    private Gtk.ScrolledWindow TextEditor;

    private SourceEditor editor = new SourceEditor(0);

    // Constructor of the windows
    // Takes title, size, and flag from event in Main
    public Window(string title, int sizeX, int sizeY, Gio.Application sender) {
        this.window = Gtk.ApplicationWindow.New((Gtk.Application)sender); // Create the window
        this.window.Title = title;                                        // Set the title
        this.window.SetDefaultSize(sizeX, sizeY);                         // Set the size (x, y)
        this.window.Show();                                               // Show the window (it's always better to see it)
        // To set the sender arg
        this.sender = sender;
        // To create the grid
        this.grid = Gtk.Grid.New();
        grid.SetHexpand(true);
        grid.SetVexpand(true);
        this.grid.ColumnSpacing = 10;
        this.window.SetChild(this.grid); // Set the grid as the window's child

        this.grid.Attach(this.editor._EditorNotebook, 0, 1, 1, 1);
        this.editor._EditorNotebook.SetScrollable(true);

        // We create the first editor, so the first page of the notebook (see SourceEditor.cs)
        GetFunc("new")(null, null);

        this.PDFViewer = this.MakePDFViewer(null);
        this.button_bar = new ButtonBar();
        this.MakeButtonBar();
    }

    // To construct the header bar of the window
    // By default, the desktop manager takes care of that, but we decideed to make
    // our own header bar
    public void SetHeaderBar(Gtk.Window window) {
        var header_bar = new AppHeaderBar(); // Create the header bar

        // TODO: make the menu bar with all the options (file, edit, etc.)

        header_bar.AddMenuButon(Globals.lan.ServeTrad("file"), false);
        header_bar.AddButtonInMenu([Globals.lan.ServeTrad("new"), Globals.lan.ServeTrad("open"), Globals.lan.ServeTrad("save"), Globals.lan.ServeTrad("exit")], [GetFunc("new"), GetFunc("open"), GetFunc("save"), GetFunc("quit")], false, true);

        header_bar.AddMenuButon("LaTeX", false);
        header_bar.AddButtonInMenu([Globals.lan.ServeTrad("compile")], [GetFunc("compile")], false, true);

        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        var button_icon = Gio.ThemedIcon.New("open-menu-symbolic"); // We create an image with an icon
        header_bar.AddMenuButon(button_icon, false);
        header_bar.AddButtonInMenu([Globals.lan.ServeTrad("about")], [GetFunc("about")], false, false);
        header_bar.SetWindowHeaderBar(window);
    }

    // To create the PDF viewer and returns the associated ScrolledWindow
    public Gtk.ScrolledWindow MakePDFViewer(string? pdf_path) {
        // IronPdf.PdfDocument pdf = new IronPdf.PdfDocument("./assets/pdf_test.pdf");
        if (pdf_path is null) {
            var scrolled = Gtk.ScrolledWindow.New();
            this.grid.Attach(scrolled, 1, 1, 1, 1); // Spans 3 columns in the third row/column
            return scrolled;
        }
        IronPdf.PdfDocument pdf = new IronPdf.PdfDocument(pdf_path);

        string path = "";
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for games
            path = Environment.ExpandEnvironmentVariables("%temp%/");
        } else { // Unix-based OS
            path = "/tmp/";
        }
        // Render the PDF as images in temp folder
        pdf.RasterizeToImageFiles($"{path}*.png", 2160, 3840, IronPdf.Imaging.ImageType.Png, 300);

        var image_box = Gtk.Box.New(Gtk.Orientation.Vertical, 5);

        for (int i = 1; i <= pdf.PageCount; ++i) {
            // Usage of Gtk.Image widget
            // -------------------------
            // var imagePdf = Gtk.Image.NewFromFile(path + i + ".png");
            // Initial size of the image
            // imagePdf.PixelSize = 500;
            // var zoom = Gtk.GestureZoom.New();
            // We change the size of the image based on the scale factor, only when the zoom is detected (touchpad pinched)
            // zoom.OnScaleChanged += (sender, args) => { imagePdf.PixelSize = (int)(500 * zoom.GetScaleDelta()); };

            // We add the gesture zoom to the box so that the entire box is rescaled when zooming, and not the image alone
            // image_box.AddController(zoom);

            // Usage of Gtk.Picture widget
            // ---------------------------
            // Usage of Gtk.Picture widget instead of Gtk.Image
            var imagePdf = Gtk.Picture.New();
            // for (int i = 1; i <= pdf.PageCount; ++i) {
            imagePdf = Gtk.Picture.NewForFilename(path + i + ".png");
            // Make the image fill the available space horizontally
            imagePdf.SetHexpand(true);
            imagePdf.SetContentFit(ContentFit.Fill);
            // IMPORTANT: this need to be on 'false' or else, the scrolled window will not work
            imagePdf.SetCanShrink(false);
            // Keep the aspect ratio of the image, which avoid the image to be stretched when resizing the window
            imagePdf.SetKeepAspectRatio(true);

            // And we add each image to the box
            image_box.Append(imagePdf);
        }

        // We put the PDF images into a scrollable element
        var scrolledPdf = Gtk.ScrolledWindow.New();
        scrolledPdf.SetHexpand(true);
        scrolledPdf.SetVexpand(true);
        scrolledPdf.SetChild(image_box);
        this.grid.Attach(scrolledPdf, 1, 1, 1, 1); // Spans 3 columns in the third row/column

        return scrolledPdf;
    }

    public void MakeButtonBar() {
        this.button_bar.AddButton("new", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-new-symbolic")), GetFunc("new"));
        // We set the widget to the window so that it is possible to use the shortcut even if not focusing the editor view
        this.button_bar.AddShortcut(this.window, "<Control>N", "newFileAction", GetFunc("new"), this.sender);

        this.button_bar.AddButton("save", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-save-symbolic")), GetFunc("save"));
        // In this case, we set the widget to the editor view so that the shortcut is only available when focusing the editor view. (we don't want to save the file when we are not focusing the editor, right? (it can be slow))
        // EDIT : It actually depends, it works on new file, but seem to not work on opened file. Stricking to window for now.
        this.button_bar.AddShortcut(this.editor.GetView(), "<Control>S", "saveAction", GetFunc("save"), this.sender);

        this.button_bar.AddButton("open", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("document-open-symbolic")), GetFunc("open"));
        this.button_bar.AddShortcut(this.window, "<Control>O", "openAction", GetFunc("open"), this.sender);

        this.button_bar.AddButton("close", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("window-close-symbolic")), GetFunc("close"));
        this.button_bar.AddShortcut(this.window, "<Control>W", "closeAction", GetFunc("close"), this.sender);

        this.button_bar.AddButton("compile", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("media-playback-start-symbolic")), GetFunc("compile"));
        this.button_bar.AddShortcut(this.window, "<Control><Shift>C", "compileAction", GetFunc("compile"), this.sender);

        this.button_bar.AddButton("vim", Gtk.Image.NewFromFile("./assets/vimlogo.png"), GetFunc("vim"));
        this.button_bar.AddShortcut(this.window, "<Control><Shift>V", "vimAction", GetFunc("vim"), this.sender);

        this.button_bar.AddButton("settings", Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("applications-system-symbolic")), GetFunc("toogle_settings"));
        this.button_bar.AddShortcut(this.editor.GetView(), "<Control><Shift>P", "toogle_settingsAction", GetFunc("toogle_settings"), this.sender);
        this.grid.Attach(this.button_bar.GetBox(), 0, 0, 2, 1); // Spans 2 columns in the third row
    }

    private Func<object?, EventArgs, System.Threading.Tasks.Task>? GetFunc(string function) {

        var func_open = async (object? sender, EventArgs args) =>
        {
            var open_dialog = Gtk.FileDialog.New();
            try {
                open_dialog.SetTitle(Globals.lan.ServeTrad("choose_file"));
                var open_task = open_dialog.OpenAsync(this.window);
                await open_task;
                if (open_task.Result is null)
                    throw new System.ArgumentNullException("Opening task is null.");
                this.editor.OpenFile(open_task.Result.GetPath());
                this.window.SetTitle($"{this.editor.GetPath()} - TeXSharp");
                this.grid.Attach(this.editor.GetTextEntry(), 0, 2, 1, 1);
                this.editor.GetTextEntry().Hide();

            } catch (Exception e) {
                Console.WriteLine("WARNING: Dismissed by user\n" + e.StackTrace);
                // new DialogWindow($"{Globals.lan.ServeTrad("cannot_open")} {e.Message}", Gio.ThemedIcon.New("dialog-warning-symbolic"), "warning", this.window);
            } finally {
                open_dialog.Dispose();
                if (System.IO.File.Exists(this.editor.GetPath()[..^ 3] + "pdf"))
                    this.PDFViewer = this.MakePDFViewer(this.editor.GetPath()[..^ 3] + "pdf");
            }
        };

        var func_save = async (object? sender, EventArgs args) =>
        {
            var save_dialog = Gtk.FileDialog.New();
            try {
                if (!this.editor.GetFileExists()) {
                    save_dialog.SetTitle(Globals.lan.ServeTrad("save_file"));
                    var save_task = save_dialog.SaveAsync(this.window);
                    await save_task;
                    if (save_task.Result is null)
                        throw new System.ArgumentNullException("Saving task is null.");
                    this.editor.SaveFile(save_task.Result.GetPath());
                } else {
                    this.editor.SaveFile(this.editor.GetPath());
                }
                this.window.SetTitle($"{this.editor.GetPath()} - TeXSharp");
            } catch (Exception e) {
                Console.WriteLine("WARNING: Dismissed by user\n" + e.StackTrace);
                // new DialogWindow($"{Globals.lan.ServeTrad("cannot_save")} {e.Message}", Gio.ThemedIcon.New("dialog-warning-symbolic"), "warning", this.window);
            } finally {
                save_dialog.Dispose();
            }
        };

        var func_newFile = async (object? sender, EventArgs args) =>
        {
            this.editor.NewFile();
            // The only way to add the TextEntry to the editor is here. Otherwise, SourceEditor class can't access the grid
            this.grid.Attach(this.editor.GetTextEntry(), 0, 2, 1, 1);
            this.editor.GetTextEntry().Hide();
        };

        var func_close = async (object? sender, EventArgs args) =>
        {
            this.editor.CloseFile();
        };

        var func_quit = async (object? sender, EventArgs args) =>
        {
            this.window.Destroy();
        };

        var func_about = async (object? sender, EventArgs args) =>
        {
            var dialog = new TAboutDialog("TeXSharp");
            dialog.Application = (Gtk.Application)this.sender; // CS0030: Impossible de convertir le type 'Gtk.Button' en 'Gtk.Application'
            dialog.Show();
        };

        var func_compile = async (object? sender, EventArgs args) =>
        {
            await func_save(sender, args);
            if (this.editor.GetFileExists()) {
                var process = await ProcessAsyncHelper.ExecuteShellCommand("latexmk", "-pdf -bibtex -interaction=nonstopmode -cd " + this.editor.GetPath(), 50000);
                this.PDFViewer = this.MakePDFViewer(this.editor.GetPath()[..^ 3] + "pdf");
            } else {
                // TODO: Make a graphical popup window in case of error
                new DialogWindow(Globals.lan.ServeTrad("not_saved_cannot_compile"), Gio.ThemedIcon.New("dialog-warning-symbolic"), Globals.lan.ServeTrad("warning"), this.window);
            }
        };

        var func_vim = async (object? sender, EventArgs args) =>
        {
            // If the VIM mode is enabled (1), we disable it
            if (this.editor.GetVIMmodeEnabled()) {
                this.editor.GetVIMeventControllerKey().SetPropagationPhase(Gtk.PropagationPhase.None);
                this.editor.GetView().RemoveController(this.editor.GetVIMeventControllerKey());
                this.editor.GetTextEntry().Hide();
                this.editor.SetVIMmodeEnabled(false);
            } else {
                // If the VIM mode is disabled (0), we enable it
                this.editor.GetTextEntry().Show();
                this.editor.GetTextEntry().SetPlaceholderText("Vim command bar");

                // Set the IM context to the event controller key
                this.editor.GetVIMeventControllerKey().SetImContext(this.editor.GetVIMmode());
                this.editor.GetVIMeventControllerKey().SetPropagationPhase(Gtk.PropagationPhase.Capture);
                // Add the event controller key to the view
                // And the vim input module context to the view (editor)
                this.editor.GetView().AddController(this.editor.GetVIMeventControllerKey());
                this.editor.GetVIMmode().SetClientWidget(this.editor.GetView());

                // Bind the command bar text to the text entry so that when we type ":" in the editor it will show up in the text entry at the bottom
                this.editor.GetVIMmode().BindProperty("command-bar-text", this.editor.GetTextEntry(), "text", 0);
                this.editor.GetVIMmode().BindProperty("command-text", this.editor.GetTextEntry(), "text", 0);

                this.editor.SetVIMmodeEnabled(true);
            }
        };

        var func_toogle_settings = async (object? sender, EventArgs args) =>
        {
            if (!Globals.settings.GetShowing()) {
                Globals.settings.OnToggle(this.editor.editors[this.editor.GetCurrentEditorIndex()]);
                this.grid.Remove(this.PDFViewer);
                this.grid.Attach(Globals.settings.GetScrolledWindow(), 1, 1, 1, 1);
                // this.grid.AttachNextTo(this.settings.GetScrolledWindow(), this.TextEditor, Gtk.PositionType.Right, 1, 1);
                Globals.settings.SetShowing(true);
            } else {
                this.grid.Remove(Globals.settings.GetScrolledWindow());
                this.grid.Attach(this.PDFViewer, 1, 1, 1, 1); // Spans 3 columns in the third row/column
                Globals.settings.SetShowing(false);
            }
        };

        switch (function) {
        case "new":
            return func_newFile;
        case "open":
            return func_open;
        case "save":
            return func_save;
        case "close":
            return func_close;
        case "compile":
            return func_compile;
        case "quit":
            return func_quit;
        case "about":
            return func_about;
        case "vim":
            return func_vim;
        case "toogle_settings":
            return func_toogle_settings;
        default:
            return null;
        }
    }

    // Manuals getters
    public Gtk.Window GetWindow() { return this.window; }
    public Gtk.Grid GetGrid() { return this.grid; }
}
