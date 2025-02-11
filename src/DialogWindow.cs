using Gtk;

class DialogWindow {
    private Gtk.Window window;

    public DialogWindow(string content, Gio.ThemedIcon icon, string type, Gtk.Window parent) {
        this.window = Gtk.Window.New();
        var box = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        var main_box = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        var image = Gtk.Image.NewFromGicon(icon);
        var button = Gtk.Button.New();
        button.SetLabel("OK");
        button.OnClicked += (sender, args) => { this.window.Destroy(); };
        image.SetPixelSize(50);
        box.Append(image);
        box.Append(Gtk.Label.New(content));
        main_box.Append(box);
        main_box.Append(button);
        this.window.SetResizable(false);
        // this.window.SetDefaultSize(500, 200);
        this.window.SetTitle(type + " - TeXSharp");
        this.window.SetChild(main_box); // Make the dialog window modal to block the parent window
        this.window.SetModal(true);     // Define parent of dialog window
        this.window.SetTransientFor(parent);
        this.window.Present();
    }
}
