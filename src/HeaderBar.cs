using System;
using System.Collections;
using System.Diagnostics;
using Gio;
using Gtk;

/// <sumary>
/// Class for the header bar of the application.
/// </sumary>
/// <remarks>
/// This is a wrapper around the <c>Gtk.HeaderBar</c> class.
/// </remarks>
class AppHeaderBar {
    /// <value>Attribute <c>HeaderBar</c> containing the header bar from GTK.</value>
    private Gtk.HeaderBar HeaderBar = Gtk.HeaderBar.New();
    /// <value>Attribute <c>_HeaderBar</c> is a wrapper for <c>HeaderBar</c> with get property.</value>
    public Gtk.HeaderBar _HeaderBar {
        get { return this.HeaderBar; }
    }

    /// <value>Attribute <c>MenuButton</c> stores the current menubutton being build.</value>
    private Gtk.MenuButton? MenuButton;

    /// <sumary>
    /// The constructor of <c>AppHeaderBar</c>. Doesn't do anything.
    /// </sumarry>
    public AppHeaderBar() {}

    /// <sumary>
    /// This methods adds a button into the header bar with an icon.
    /// </sumary>
    /// <param name="icon">A <c>Gio.ThemedIcon</c> representing the icon to put on the menu button.</param>
    /// <param name="frame">A boolean: if the button should have a frame or not.</param>
    /// <returns>Does not return anything.</returns>
    public void AddMenuButon(Gio.ThemedIcon icon, bool frame) {
        var Button = Gtk.MenuButton.New();             // We create a button
        Button.SetHasFrame(frame);                     // without a frame
        var ButtonIcon = Gtk.Image.NewFromGicon(icon); // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        Button.SetChild(ButtonIcon); // We set the icon as child of the button (the child will be contained in the button)
        this.MenuButton = Button;
    }

    /// <sumary>
    /// This methods adds a button into the header bar with a label (a string).
    /// </sumary>
    /// <param name="label">A <c>string</c> which contains the text to put into the menu button.</param>
    /// <param name="frame">A boolean: if the button should have a frame or not.</param>
    /// <returns>Does not return anything.</returns>
    public void AddMenuButon(string label, bool frame) {
        var Button = Gtk.MenuButton.New(); // We create a button
        Button.SetHasFrame(frame);         // without a frame
        Button.Label = label;
        this.MenuButton = Button;
    }

    // TODO: Refactor this function in order to maintain an access to the Button in case we nedd to aplly them a method
    // Functions passed into the array must be async
    /// <sumary>
    /// This methods adds a button into the menu button.
    /// </sumary>
    /// <param name="label">An array of <c>string</c> containing all the label of the buttons to put into the menu button.</param>
    /// <param name="shortcut">An array of <c>string</c> containing all the shortcuts of the buttons to put into the menu button.</param>
    /// <param name="funcs">An arrays of functions to call when the button into the menu button is pressed. These functions can be null. This function is asynchronous so it returns a task and takes an <c>object</c> and <c>EventArgs</c>.</param>
    /// <param name="frame">A boolean: if the button should have a frame or not.</param>
    /// <param name="packStart">A boolean: if the button should be at the beginning or at the end of the header bar.</param>
    /// <remarks>
    /// The content of all the arrays passed as arguments must be correctly arranged, for the shortcuts and the callback action to be called properly.
    /// </remarks>
    /// <returns>Does not return anything.</returns>
    public void AddButtonInMenu(string[] label, string[] shortcut, Func<object?, EventArgs, System.Threading.Tasks.Task>?[] funcs, bool frame, bool packStart) {
        Debug.Assert(label.Length == funcs.Length, "Lenght of two tables are not equal !");
        var PopFile = Gtk.Popover.New();                        // New popover menu
        var BoxFile = Gtk.Box.New(Gtk.Orientation.Vertical, 0); // New box to put in the popover menu
        for (int i = 0; i < label.Length; ++i) {
            // We create a little horizontal box to put the button and the label of the shorctut
            var Box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);

            var ButtonFileOpen = Gtk.Button.New(); // Button to put in the box
            ButtonFileOpen.SetLabel(label[i]);     // Label of the button
            ButtonFileOpen.SetHasFrame(frame);     // Without frame
            int Localindex = i;                    // Create a local copy of i to capture the corresponding value of ix
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
        }
        PopFile.SetChild(BoxFile);
        Debug.Assert(!(this.MenuButton is null), "Menu-button is null.");
        this.MenuButton.SetPopover(PopFile);
        if (packStart)
            this.HeaderBar.PackStart(this.MenuButton);
        else
            this.HeaderBar.PackEnd(this.MenuButton);
    }

    /// <sumary>
    /// This methods set the current header bar to the window passed as parameter.
    /// </sumary>
    /// <param name="window">The window to which we set the header bar.</param>
    /// <returns>Does not return anything.</returns>
    public void SetWindowHeaderBar(Gtk.Window window) {
        window.SetTitlebar(this.HeaderBar); // Set the header bar
    }

    // Manual getters
    /// <sumary>
    /// This getters allows you to get the <c>Gtk.HeaderBar</c> of this <c>AppHeaderBar</c>.
    /// </sumary>
    /// <param name="window">The window to which we set the header bar.</param>
    /// <returns>Returns the <c>Gtk.HeaderBar</c> of the instance of the object .</returns>
    public Gtk.HeaderBar GetAppHeaderBar() { return this.HeaderBar; }
}
