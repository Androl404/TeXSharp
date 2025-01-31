using Gtk;

class ButtonBar {
    private Gtk.Box box;
    public Gtk.Box _Box {
        get { return this.box; }
    }
    private Dictionary<string, Gtk.Button> button_list;

    public ButtonBar() {
        this.box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
        this.button_list = new Dictionary<string, Gtk.Button>();
    }

    public void AddButton(string label, Gio.ThemedIcon icon, Func<object?, EventArgs, System.Threading.Tasks.Task> func) {
        if (this.button_list.ContainsKey(label))
            throw new System.FieldAccessException("The key already exists in the dictionnary !");
        var button = Gtk.Button.New();                  // We create a button
        button.SetHasFrame(false);                      // without a frame
        var button_icon = Gtk.Image.NewFromGicon(icon); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        button.OnClicked += (sender, args) => { func(sender, args); };
        button.OnActivate += (sender, args) => { func(sender, args); };
        this.button_list.Add(label, button);
        this.box.Append(button);
    }

    // public void AddActivatedEvent(string label, Func<object?, EventArgs, System.Threading.Tasks.Task> func) {
    //     if (!this.button_list.ContainsKey(label))
    //         throw new System.FieldAccessException("The key does not exists in the dictionnary !");
    //     this.button_list[label].OnActivate += (sender, args) => { func(sender, args); };
    // }

    // Manuals getter
    public Gtk.Box GetBox() { return this.box; }
    public Gtk.Button GetButton(string label) {
        if (!this.button_list.ContainsKey(label))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.button_list[label];
    }
}
