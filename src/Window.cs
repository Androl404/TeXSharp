using System;
using System.Runtime.InteropServices;
using Gtk;
using Gio;
using IronPdf;

class Window {
    private Gtk.ApplicationWindow window; // The main window
    private Gio.Application sender;
    public Gtk.ApplicationWindow _Window { // Public property used to access the window attribute in read-only
        get { return this.window; } // get method
    }

    // Constructor of the windows
    // Takes title, size, and flag from event in Main
    public Window(string title, int sizeX, int sizeY, Gio.Application sender) {
        this.window = Gtk.ApplicationWindow.New((Gtk.Application)sender); // Create the window
        this.window.Title = title;                                        // Set the title
        this.window.SetDefaultSize(sizeX, sizeY);                         // Set the size (x, y)
        this.window.Show();                                               // Show the window (it's always better to see it)
        this.sender = sender;
    }

    // To construct the header bar of the window
    // By default, the desktop manager takes care of that, but we decideed to make
    // our own header bar
    public void SetHeaderBar() {
        var header_bar = Gtk.HeaderBar.New(); // Create the header bar

        // TODO: make the menu bar with all the options (file, edit, etc.)
        header_bar.PackStart(Gtk.Label.New("Mettre la menu bar ici")); // We put a label at the beginning of the header bar

        var button = Gtk.MenuButton.New();                                                  // We create a button
        button.SetHasFrame(false);                                                          // without a frame
        var button_icon = Gtk.Image.NewFromGicon(Gio.ThemedIcon.New("open-menu-symbolic")); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)

        var pop = Gtk.Popover.New();                           // New popover menu
        var box = Gtk.Box.New(Gtk.Orientation.Vertical, 0);    // New box to put in the popover menu
        var button_about = Gtk.Button.New();                   // Button to put in the box
        button_about.SetLabel(Globals.lan.ServeTrad("about")); // Label of the button
        button_about.SetHasFrame(false);                       // Without frame
        button_about.OnClicked += (sender, args) => {
            var dialog = new TAboutDialog.TAboutDialog("TeXSharp");
            dialog.Application = (Gtk.Application)this.sender; // CS0030: Impossible de convertir le type 'Gtk.Button' en 'Gtk.Application'
            dialog.Show();
        };

        // We then encapsulate our elements
        box.Append(button_about);
        pop.SetChild(box);
        button.SetPopover(pop);
        header_bar.PackEnd(button);
        this.window.SetTitlebar(header_bar); // Set the header bar
    }

    public Gtk.ScrolledWindow MakeTextEditor() {
        // Create ScrolledWindow for scrolling capability
        var scrolled = Gtk.ScrolledWindow.New();
        scrolled.SetHexpand(true);
        scrolled.SetVexpand(true);

        // Create TextView
        var textView = Gtk.TextView.New();
        // If you want to control whether it's editable:
        textView.Editable = true; // or false to make it read-only
        textView.Buffer.Text = Globals.lan.ServeTrad("hello_world");

        // Add TextView to ScrolledWindow
        scrolled.SetChild(textView);
        return scrolled;
    }

    public Gtk.ScrolledWindow MakePDFViewer() {
        IronPdf.PdfDocument pdf = new IronPdf.PdfDocument("./assets/pdf_test.pdf");

        string path = "";
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            path = Environment.ExpandEnvironmentVariables("%temp%/");
        } else { // Unix-based OS
            path = "/tmp/";
        }
        // Render the PDF as images in temp folder
        pdf.RasterizeToImageFiles($"{path}*.png", 2160, 3840, IronPdf.Imaging.ImageType.Png, 300);

        var image_box = Gtk.Box.New(Gtk.Orientation.Vertical, 0);

        // TODO: Find another way to show images than converting them to images, it is not convenient
        // Usage of Gtk.Picture widget instead of Gtk.Image
        var imagePdf = Gtk.Picture.New();
        for (int i = 1; i <= pdf.PageCount; ++i) {
            imagePdf = Gtk.Picture.NewForFilename(path + i + ".png");
            // Make the image fill the available space horizontally
            imagePdf.SetHexpand(true);
            imagePdf.SetContentFit(ContentFit.Fill);
            // IMPORTANT: this need to be on 'false' or else, the scrolled window will not work
            imagePdf.SetCanShrink(false);
            // Keep the aspect ratio of the image, which avoid the image to be stretched when resizing the window
            imagePdf.SetKeepAspectRatio(true);

            image_box.Append(imagePdf);
        }

        // We put the PDF images into a scrollable element
        var scrolledPdf = Gtk.ScrolledWindow.New();
        scrolledPdf.SetHexpand(true);
        scrolledPdf.SetVexpand(true);
        scrolledPdf.SetChild(image_box);
        return scrolledPdf;
    }
}
