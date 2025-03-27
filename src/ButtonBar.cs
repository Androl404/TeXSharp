using Gtk;
using GLib;

class ButtonBar {
    private Gtk.Box Box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
    public Gtk.Box _Box {
        get { return this.Box; }
    }
    private Gtk.Label StatusBar = Gtk.Label.New(Globals.Languages.ServeTrad("status_bar_default"));
    public Gtk.Label _StatusBar {
        get { return this.StatusBar; }
    }
    private Dictionary<string, Gtk.Button> ButtonList = new Dictionary<string, Gtk.Button>();
    private Dictionary<string, Gtk.CallbackAction> Actions = new Dictionary<string, Gtk.CallbackAction>();

    public ButtonBar() { }

    public void AddButton(string label, Gtk.Image image, Func<object?, EventArgs, System.Threading.Tasks.Task>? func) {
        if (func is null)
            throw new System.ArgumentNullException("The function passed as argument is null.");
        if (this.ButtonList.ContainsKey(label))
            throw new System.FieldAccessException("The key already exists in the dictionnary !");
        var button = Gtk.Button.New(); // We create a button
        button.SetHasFrame(false);     // without a frame
        var button_icon = image;       // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        button.OnClicked += (sender, args) => { func(sender, args); };
        button.OnActivate += (sender, args) => { func(sender, args); };
        this.ButtonList.Add(label, button);
        this.Box.Append(button);
    }

     public void AddShortcut(Gtk.Widget widget, string trigger, string actionName, Func<object?, EventArgs, System.Threading.Tasks.Task>? func, Object? sender) {
        if (func is null)
            throw new System.ArgumentNullException("The function passed as argument is null.");
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
        var ShortcutAction = Gtk.CallbackAction.New((Widget w, Variant? args) => ShortcutFunc(w, actionName, func, sender));
        // Store the action for later use if needed
        this.Actions.Add(actionName, ShortcutAction);

        // Create a new Shortcut with the desired trigger and action
        var ShortcutTrigger = Gtk.ShortcutTrigger.ParseString(trigger);
        var Shortcut = Gtk.Shortcut.New(ShortcutTrigger, ShortcutAction);
        ShortcutController.AddShortcut(Shortcut);
    }

    private bool ShortcutFunc(Widget widget, string Actionname, Func<object?, EventArgs, System.Threading.Tasks.Task> func, Object? sender) {
        // Get the function associated with the actionName
        func(sender, new EventArgs());

        // Return true to indicate the action was handled
        return true;
    }

    public void AddStatusBar() {
        this.Box.Append(Gtk.Label.New("     "));
        this.Box.Append(this.StatusBar);
    }

    // Manuals getter
    public Gtk.Box GetBox() { return this.Box; }
    public Gtk.Button GetButton(string label) {
        if (!this.ButtonList.ContainsKey(label))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.ButtonList[label];
    }
    public Gtk.CallbackAction GetAction(string actionName) {
        if (!this.Actions.ContainsKey(actionName))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.Actions[actionName];
    }
}
