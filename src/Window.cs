using System;
using System.Runtime.InteropServices;
using Gtk;
using GtkSource;
using Gio;
using IronPdf;

class Window {
    private Gio.Application sender;        // The sender args on window activation
    private Gtk.ApplicationWindow window;  // The main window
    public Gtk.ApplicationWindow _Window { // Public property used to access the window attribute in read-only
        get { return this.window; }        // get method
    }
    private Gtk.Grid grid;
    public Gtk.Grid _Grid {
        get { return this.grid; } // get method
    }
    private Dictionary<string, SourceEditor> editors;
    private string active_editor;

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
        this.editors = new Dictionary<string, SourceEditor>();
    }

    // To construct the header bar of the window
    // By default, the desktop manager takes care of that, but we decideed to make
    // our own header bar
    public void SetHeaderBar(Gtk.Window window) {
        var header_bar = new AppHeaderBar(); // Create the header bar

        // TODO: make the menu bar with all the options (file, edit, etc.)

        header_bar.AddMenuButon(Globals.lan.ServeTrad("file"), false);
        header_bar.AddButtonInMenu([Globals.lan.ServeTrad("open"), Globals.lan.ServeTrad("save"), Globals.lan.ServeTrad("exit")], [GetFunc("open"), GetFunc("save"), GetFunc("quit")], false, true);

        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        var button_icon = Gio.ThemedIcon.New("open-menu-symbolic"); // We create an image with an icon
        header_bar.AddMenuButon(button_icon, false);
        header_bar.AddButtonInMenu([Globals.lan.ServeTrad("about")], [GetFunc("about")], false, false);
        header_bar.SetWindowHeaderBar(window);
    }

    // To create the editor and returns the ScrolledWindow associated
    public Gtk.ScrolledWindow MakeTextEditor() {
        // Create ScrolledWindow for scrolling capability
        var scrolled = Gtk.ScrolledWindow.New();
        scrolled.SetHexpand(true);
        scrolled.SetVexpand(true);

        this.editors.Add("new1", new SourceEditor(""));
        var editor_view = this.editors["new1"].GetView();
        this.active_editor = "new1";

        // Add TextView to ScrolledWindow
        scrolled.SetChild(editor_view);
        this.grid.Attach(scrolled, 0, 1, 1, 1); // Spans 2 columns in the third row

        return scrolled;
    }

    // To create the PDF viewer and returns the associated ScrolledWindow
    public Gtk.ScrolledWindow MakePDFViewer() {
        IronPdf.PdfDocument pdf = new IronPdf.PdfDocument("./assets/pdf_test.pdf");

        string path = "";
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { // If we are on a niche operating system for games
            path = Environment.ExpandEnvironmentVariables("%temp%/");
        } else { // Unix-based OS
            path = "/tmp/";
        }
        // Render the PDF as images in temp folder
        pdf.RasterizeToImageFiles($"{path}*.png", 2160, 3840, IronPdf.Imaging.ImageType.Png, 300);

        var image_box = Gtk.Box.New(Gtk.Orientation.Vertical, 5);

        // TODO: Find another way to show images than converting them to images, it is not convenient
        for (int i = 1; i <= pdf.PageCount; ++i) {
            var zoom = Gtk.GestureZoom.New();

            // Usage of Gtk.Picture widget instead of Gtk.Image
            var imagePdf = Gtk.Image.NewFromFile(path + i + ".png");

            // Initial size of the image
            imagePdf.PixelSize = 500;
            // We change the size of the image based on the scale factor, only when the zoom is detected (touchpad pinched)
            zoom.OnScaleChanged += (sender, args) => { imagePdf.PixelSize = (int)(500 * zoom.GetScaleDelta()); };

            // We add the gesture zoom to the box so that the entire box is rescaled when zooming, and not the image alone
            image_box.AddController(zoom);
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
        var main_box = new ButtonBar();
        main_box.AddButton(Gio.ThemedIcon.New("document-save-symbolic"), GetFunc("save"));
        main_box.AddButton(Gio.ThemedIcon.New("document-open-symbolic"), GetFunc("open"));
        this.grid.Attach(main_box.GetBox(), 0, 0, 2, 1); // Spans 2 columns in the third row
    }

    private Func<object?, EventArgs, System.Threading.Tasks.Task> GetFunc(string function) {
        var func_open  = async (object? sender, EventArgs args) => {
            var open_dialog = Gtk.FileDialog.New();
            try {
                open_dialog.SetTitle(Globals.lan.ServeTrad("choose_file"));
                var open_task = open_dialog.OpenAsync(this.window);
                await open_task;
                this.editors["new1"].OpenFile(open_task.Result.GetPath());
            } catch (Exception e) {
                Console.WriteLine($"WARNING: {e.Message}");
            } finally {
                open_dialog.Dispose();
            }
        };

        var func_save = async (object? sender, EventArgs args) => {
            var save_dialog = Gtk.FileDialog.New();
            try {
                if (!this.editors["new1"]._Exists) {
                    save_dialog.SetTitle(Globals.lan.ServeTrad("save_file"));
                    var save_task = save_dialog.SaveAsync(this.window);
                    await save_task;
                    this.editors["new1"].SaveFile(save_task.Result.GetPath());
                } else {
                    this.editors["new1"].SaveFile(this.editors["new1"]._Path);
                }
            } catch (Exception e) {
                Console.WriteLine($"WARNING: {e.Message}");
            } finally {
                save_dialog.Dispose();
            }
        };

        var func_quit = async (object? sender, EventArgs args) => {
            this.window.Destroy();
        };

        var func_about = async (object? sender, EventArgs args) => {
            var dialog = new TAboutDialog("TeXSharp");
            dialog.Application = (Gtk.Application)this.sender; // CS0030: Impossible de convertir le type 'Gtk.Button' en 'Gtk.Application'
            dialog.Show();
        };

        switch (function) {
        case "open":
            return func_open;
        case "save":
            return func_save;
        case "quit":
            return func_quit;
        case "about":
            return func_about;
        default:
            return null;
        }
    }

    // Manuals getters
    public Gtk.Window GetWindow() { return this.window; }
    public Gtk.Grid GetGrid() { return this.grid; }
}
