using System;
using System.Collections;
using Gio;
using Gtk;

class AppHeaderBar {
    private Gtk.HeaderBar HeaderBar = Gtk.HeaderBar.New();
    public Gtk.HeaderBar _HeaderBar {
        get { return this.HeaderBar; }
    }
    private Gtk.MenuButton? MenuButton;

    public AppHeaderBar() { }

    public void AddMenuButon(Gio.ThemedIcon icon, bool frame) {
        var Button = Gtk.MenuButton.New();              // We create a button
        Button.SetHasFrame(frame);                      // without a frame
        var ButtonIcon = Gtk.Image.NewFromGicon(icon); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        Button.SetChild(ButtonIcon); // We set the icon as child of the button (the child will be contained in the button)
        this.MenuButton = Button;
    }

    public void AddMenuButon(string label, bool frame) {
        var Button = Gtk.MenuButton.New(); // We create a button
        Button.SetHasFrame(frame);         // without a frame
        Button.Label = label;
        this.MenuButton = Button;
    }

    // TODO: Refactor this function in order to maintain an access to the Button in case we nedd to aplly them a method
    // Functions passed into the array must be async
    public void AddButtonInMenu(string[] label, string[] shortcut, Func<object?, EventArgs, System.Threading.Tasks.Task>?[] funcs, bool frame, bool packStart) {
        if (label.Length != funcs.Length)
            throw new System.OverflowException("Lenght of two tables are not equal !");
        var PopFile = Gtk.Popover.New();                        // New popover menu
        var BoxFile = Gtk.Box.New(Gtk.Orientation.Vertical, 0); // New box to put in the popover menu
        for (int i = 0; i < label.Length; ++i) {
            // We create a little horizontal box to put the button and the label of the shorctut
            var Box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);

            var ButtonFileOpen = Gtk.Button.New(); // Button to put in the box
            ButtonFileOpen.SetLabel(label[i]);     // Label of the button
            ButtonFileOpen.SetHasFrame(frame);     // Without frame
            // Create a local copy of i to capture the corresponding value of i
            int Localindex = i;
            ButtonFileOpen.OnClicked += (sender, args) => {
                funcs[Localindex](sender, args); // Utiliser la copie locale
            };

            // We create the label of the shortcut. And add a CSS class to it. The label will appear grey, like we can see on Nautilus file
            var ShortcutLabel = Gtk.Label.New(shortcut[i]);
            ShortcutLabel.AddCssClass("dim-label");
            ShortcutLabel.SetHalign(Gtk.Align.End);

            Box.Append(ButtonFileOpen);
            Box.Append(ShortcutLabel);
            BoxFile.Append(Box);
            // BoxFile.Append(ButtonFileOpen); // Ajouter le bouton à la box
        }
        PopFile.SetChild(BoxFile);
        if (this.MenuButton is null)
            throw new System.ArgumentNullException("Menu-button is null.");
        this.MenuButton.SetPopover(PopFile);
        if (packStart)
            this.HeaderBar.PackStart(this.MenuButton);
        else
            this.HeaderBar.PackEnd(this.MenuButton);
    }

    public void SetWindowHeaderBar(Gtk.Window window) {
        window.SetTitlebar(this.HeaderBar); // Set the header bar
    }

    // Manuals getter
    public Gtk.HeaderBar GetAppHeaderBar() { return this.HeaderBar; }
}
