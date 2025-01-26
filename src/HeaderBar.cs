using System;
using System.Collections;
using Gio;
using Gtk;

class AppHeaderBar {
    private Gtk.HeaderBar headerbar;
    public Gtk.HeaderBar _HeaderBar {
        get { return this.headerbar; }
    }
    private Gtk.MenuButton menu_button;

    public AppHeaderBar() {
        this.headerbar = Gtk.HeaderBar.New();
    }

    public void AddMenuButon(Gio.ThemedIcon icon, bool frame) {
        var button = Gtk.MenuButton.New(); // We create a button
        button.SetHasFrame(frame); // without a frame
        var button_icon = Gtk.Image.NewFromGicon(icon); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        this.menu_button = button;
    }

    public void AddMenuButon(string label, bool frame) {
        var button = Gtk.MenuButton.New(); // We create a button
        button.SetHasFrame(false); // without a frame
        button.Label = label;
        this.menu_button = button;
    }

    public void AddButtonInMenu(string[] label, Action<object?, EventArgs>[] funcs, bool frame, bool pack_start) {
        if (label.Length != funcs.Length) throw new System.OverflowException("Lenght of two tables are not equal !");

        var pop_file = Gtk.Popover.New(); // New popover menu
        var box_file = Gtk.Box.New(Gtk.Orientation.Vertical, 0);    // New box to put in the popover menu
        for (int i = 0; i < label.Length; ++i) {
            var button_file_open = Gtk.Button.New(); // Button to put in the box
            button_file_open.SetLabel(label[i]); // Label of the button
            button_file_open.SetHasFrame(frame); // Without frame
            // Create a local copy of i to capture the corresponding value of i
            int localIndex = i;
            button_file_open.OnClicked += (sender, args) => {
                funcs[localIndex](sender, args); // Utiliser la copie locale
            };
            box_file.Append(button_file_open); // Ajouter le bouton à la box
        }
        pop_file.SetChild(box_file);
        this.menu_button.SetPopover(pop_file);
        if (pack_start) this.headerbar.PackStart(this.menu_button);
        else this.headerbar.PackEnd(this.menu_button);
    }

    // Overload for async functions to be executed
    public void AddButtonInMenu(string[] label, Func<object?, EventArgs, System.Threading.Tasks.Task>[] funcs, bool frame, bool pack_start) {
        if (label.Length != funcs.Length) throw new System.OverflowException("Lenght of two tables are not equal !");

        var pop_file = Gtk.Popover.New(); // New popover menu
        var box_file = Gtk.Box.New(Gtk.Orientation.Vertical, 0);    // New box to put in the popover menu
        for (int i = 0; i < label.Length; ++i) {
            var button_file_open = Gtk.Button.New(); // Button to put in the box
            button_file_open.SetLabel(label[i]); // Label of the button
            button_file_open.SetHasFrame(frame); // Without frame
            // Create a local copy of i to capture the corresponding value of i
            int localIndex = i;
            button_file_open.OnClicked += (sender, args) => {
                funcs[localIndex](sender, args); // Utiliser la copie locale
            };
            box_file.Append(button_file_open); // Ajouter le bouton à la box
        }
        pop_file.SetChild(box_file);
        this.menu_button.SetPopover(pop_file);
        if (pack_start) this.headerbar.PackStart(this.menu_button);
        else this.headerbar.PackEnd(this.menu_button);
    }

    public void SetWindowHeaderBar(Gtk.Window window) {
        window.SetTitlebar(this.headerbar); // Set the header bar
    }

    // Manuals getter
    public Gtk.HeaderBar GetAppHeaderBar() {
        return this.headerbar;
    }
}
