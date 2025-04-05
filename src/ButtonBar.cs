using System.Diagnostics;
using Gtk;
using GLib;

class ButtonBar {
    /// <value>Attribute <c>Box</c> represents the box containing all the buttons.</value>
    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
    /// <value>Attribute <c>_Box</c> is a wrapper for <c>Box</c> with get property.</value>
    public Gtk.Box _Box {
        get { return this.Box; }
    }

    /// <value>Attribute <c>StatusBar</c> represents the status bar next to the button bar.</value>
    /// <remarks>It contains the current action made by this application.</remarks>
    private Gtk.Label StatusBar = Gtk.Label.New(Globals.Languages.Translate("status_bar_default"));
    /// <value>Attribute <c>_StatusBar</c> is a wrapper for <c>StatusBar</c> with get property.</value>
    public Gtk.Label _StatusBar {
        get { return this.StatusBar; }
    }

    /// <value>Attribute <c>ButtonList</c> is a dictionnary containing all the button into the button bar, with a string as a key for each button.</value>
    private Dictionary<string, Gtk.Button> ButtonList = new Dictionary<string, Gtk.Button>();

    /// <value>Attribute <c>Actions</c> is a dictionnary conbining all the callback action to their corresponding key (a string).</value>
    private Dictionary<string, Gtk.CallbackAction> Actions = new Dictionary<string, Gtk.CallbackAction>();

    /// <sumary>
    /// The constructor of <c>ButtonBar</c>. Doesn't do anything.
    /// </sumarry>
    public ButtonBar() {}

    /// <sumary>
    /// This methods adds a button into the button bar, with the corresponding callback function.
    /// </sumary>
    /// <param name="label">The label corresponding to the button. It is a short string.</param>
    /// <param name="image">The image to put into the button.</param>
    /// <param name="func">The function to call when the button is pressed. This function can be null. This function is asynchronous so it returns a task and takes an <c>object</c> and <c>EventArgs</c>.</param>
    /// <returns>Does not return anything.</returns>
    public void AddButton(string label, Gtk.Image image, Func<object?, EventArgs, System.Threading.Tasks.Task>? func) {
        Debug.Assert(!(func is null), "The function passed as argument is null."); // Assertion to verify that the function is not null
        Debug.Assert(!this.ButtonList.ContainsKey(label), "The key already exists in the dictionnary !");
        var button = Gtk.Button.New(); // We create a button
        button.SetHasFrame(false);     // without a frame
        var button_icon = image;       // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/ on GNU/Linux distributions
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        button.OnClicked += (sender, args) => { func(sender, args); };
        button.OnActivate += (sender, args) => { func(sender, args); };
        this.ButtonList.Add(label, button); // We add the button into the dictionnary
        this.Box.Append(button);            // We put the button into the button bar
    }

    /// <sumary>
    /// This methods adds a shortcut for the corresponding button so a keyboard shortcut can be used instead of pressing a button.
    /// </sumary>
    /// <param name="widget">The widget on which the shortcut is active, can be the editor, the window and any widget.</param>
    /// <param name="trigger">A string containing the keyboard keys to press in order to activate the callback function.</param>
    /// <param name="actionName">The name of the action, will be added to the <c>Actions</c> dictionnary.</param>
    /// <param name="func">The function to call when the button is pressed. This function can be null. This function is asynchronous so it returns a task and takes an <c>object</c> and <c>EventArgs</c>.</param>
    /// <param name="sender">The sender of the shortcut. This can be null.</param>
    /// <returns>Does not return anything.</returns>
    public void AddShortcut(Gtk.Widget widget, string trigger, string actionName, Func<object?, EventArgs, System.Threading.Tasks.Task>? func, object? sender) {
        Debug.Assert(!(func is null), "The function passed as argument is null."); // Assertion to verify that the function is not null
        // Create a ShortcutController and set its scope
        // The ShortcutController, as the name suggests, is here to detect and control shortcuts
        // The scope is the level at which the shortcuts are detected. Wet set it to Local to say the shortcut will be handled by the widget it is added to
        var ShortcutController = Gtk.ShortcutController.New();
        ShortcutController.SetScope(Gtk.ShortcutScope.Local);
        widget.AddController(ShortcutController);

        // Create a new CallbackAction for the specific actionName
        // The CallbackAction is a simple action that calls a function when triggered
        // And since we want to give a parameter to ShortFunc, we use a lambda function to call it with the actionName, and still be able to be conform to the delegate function we need to pass to the CallbackAction
        // We also pass it a function, from the GetFunc() method of Window class (Window.cs file), and the sender for the function
        var ShortcutAction = Gtk.CallbackAction.New((Widget w, Variant? args) => ShortcutFunc(w, func, sender));
        // Store the action for later use if needed
        this.Actions.Add(actionName, ShortcutAction);

        // Create a new Shortcut with the desired trigger and action
        var ShortcutTrigger = Gtk.ShortcutTrigger.ParseString(trigger);
        var Shortcut = Gtk.Shortcut.New(ShortcutTrigger, ShortcutAction);
        ShortcutController.AddShortcut(Shortcut);
    }

    /// <sumary>
    /// This methods is called to execute the corresponding function when a shortcut is pressed.
    /// </sumary>
    /// <param name="widget">The widget on which the shortcut is active, can be the editor, the window and any widget.</param>
    /// <param name="func">The function to call when the button is pressed. This function is asynchronous so it returns a task and takes an <c>object</c> and <c>EventArgs</c>.</param>
    /// <param name="sender">The sender of the shortcut. This can be null.</param>
    /// <returns>Returns a boolen to indicate that the action was handled.</returns>
    private bool ShortcutFunc(Widget widget, Func<object?, EventArgs, System.Threading.Tasks.Task> func, object? sender) {
        func(sender, new EventArgs()); // Get the function associated with the actionName
        return true;                   // Return true to indicate the action was handled
    }

    /// <sumary>
    /// This methods appends the status bar to the button bar.
    /// </sumary>
    /// <remarks>
    /// This methods should be called after all the button are placed into the button bar.
    /// </remarks>
    /// <returns>Does not return anything.</returns>
    public void AddStatusBar() {
        this.Box.Append(Gtk.Label.New("     "));
        this.Box.Append(this.StatusBar);
    }

    // Manual getters
    /// <sumary>
    /// This getter allow you to get the button bar <c>Box</c> with a method.
    /// </sumary>
    /// <returns>Returns the box with all the buttons ans the status bar.</returns>
    public Gtk.Box GetBox() { return this.Box; }

    /// <sumary>
    /// This getter allow you to get the button corresponding to the string passed as a parameter.
    /// </sumary>
    /// <remarks>
    /// The button must exists into the <c>ButtonList</c> dictionnary.
    /// </remarks>
    /// <param name="label">The string corresponding to the button we want to return.</param>
    /// <returns>Returns the button corresponding to the parameter.</returns>
    public Gtk.Button GetButton(string label) {
        Debug.Assert(this.ButtonList.ContainsKey(label), "The key does not exists in the dictionnary !");
        return this.ButtonList[label];
    }

    /// <sumary>
    /// This getter allow you to get the callback action corresponding to the string passed as a parameter.
    /// </sumary>
    /// <remarks>
    /// The callback action must exists into the <c>Actions</c> dictionnary.
    /// </remarks>
    /// <param name="label">The string corresponding to the callback action we want to return.</param>
    /// <returns>Returns the <c>CallbackAction</c> correspond to the parameter.</returns>
    public Gtk.CallbackAction GetAction(string actionName) {
        Debug.Assert(this.Actions.ContainsKey(actionName), "The key does not exists in the dictionnary !");
        return this.Actions[actionName];
    }
}
