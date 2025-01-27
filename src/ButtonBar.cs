using Gtk;

class ButtonBar {
    private Gtk.Box box;
    public Gtk.Box _Box {
        get { return this.box; }
    }

    public ButtonBar() {
        this.box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
    }

    public void AddButton(Gio.ThemedIcon icon, Func<object?, EventArgs, System.Threading.Tasks.Task> func) {
        var button = Gtk.Button.New(); // We create a button
        button.SetHasFrame(false); // without a frame
        var button_icon = Gtk.Image.NewFromGicon(icon); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        button.OnClicked += (sender, args) => {
            func(sender, args);
        };
        this.box.Append(button);
    }

    // Manuals getter
    public Gtk.Box GetBox() {
        return this.box;
    }
}
