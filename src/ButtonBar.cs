using Gtk;
using GLib;

class ButtonBar {
    private Gtk.Box box;
    public Gtk.Box _Box {
        get { return this.box; }
    }
    private Dictionary<string, Gtk.Button> button_list;
    private Dictionary<string, Gtk.CallbackAction> actions = new Dictionary<string, Gtk.CallbackAction>();

    public ButtonBar() {
        this.box = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
        this.button_list = new Dictionary<string, Gtk.Button>();
    }

    public void AddButton(string label, Gtk.Image image, Func<object?, EventArgs, System.Threading.Tasks.Task>? func) {
        if (func is null)
            throw new System.ArgumentNullException("The function passed as argument is null.");
        if (this.button_list.ContainsKey(label))
            throw new System.FieldAccessException("The key already exists in the dictionnary !");
        var button = Gtk.Button.New(); // We create a button
        button.SetHasFrame(false);     // without a frame
        var button_icon = image;       // We create an image with an icon
        // The names of the available icons can be found with `gtk4-icon-browser`, or in /usr/share/icons/
        button.SetChild(button_icon); // We set the icon as child of the button (the child will be contained in the button)
        button.OnClicked += (sender, args) => { func(sender, args); };
        button.OnActivate += (sender, args) => { func(sender, args); };
        this.button_list.Add(label, button);
        this.box.Append(button);
    }

    public void AddShortcut(Gtk.Widget widget, string trigger, string actionName, Func<object?, EventArgs, System.Threading.Tasks.Task>? func, Object? sender) {
        if (func is null)
            throw new System.ArgumentNullException("The function passed as argument is null.");
        // Create a ShortcutController and set its scope
        // The ShortcutController, as the name suggests, is here to detect and control shortcuts
        // The scope is the level at which the shortcuts are detected. Wet set it to Local to say the shortcut will be handled by the widget it is added to
        var shortcutController = Gtk.ShortcutController.New();
        shortcutController.SetScope(Gtk.ShortcutScope.Local);
        widget.AddController(shortcutController);

        // Create a new CallbackAction for the specific actionName
        // The CallbackAction is a simple action that calls a function when triggered
        // And since we want to give a parameter to ShortFunc, we use a lambda function to call it with the actionName, and still be able to be conform to the delegate function we need to pass to the CallbackAction
        // We also pass it a function, from the GetFunc() method of Window class (Window.cs file), and the sender for the function
        var shortcutAction = Gtk.CallbackAction.New((Widget w, Variant? args) => ShortcutFunc(w, actionName, func, sender));
        // Store the action for later use if needed
        this.actions.Add(actionName, shortcutAction);

        // Create a new Shortcut with the desired trigger and action
        var shortcutTrigger = Gtk.ShortcutTrigger.ParseString(trigger);
        var shortcut = Gtk.Shortcut.New(shortcutTrigger, shortcutAction);
        shortcutController.AddShortcut(shortcut);
    }

    private bool ShortcutFunc(Widget widget, string actionName, Func<object?, EventArgs, System.Threading.Tasks.Task> funk, Object? sender) {
        // Get the function associated with the actionName
        funk(sender, new EventArgs());

        // Return true to indicate the action was handled
        return true;
    }

    // Manuals getter
    public Gtk.Box GetBox() { return this.box; }
    public Gtk.Button GetButton(string label) {
        if (!this.button_list.ContainsKey(label))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.button_list[label];
    }
    public Gtk.CallbackAction GetAction(string actionName) {
        if (!this.actions.ContainsKey(actionName))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.actions[actionName];
    }
    public Gtk.Button GetButton(string label) {
        if (!this.button_list.ContainsKey(label)) throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.button_list[label];
    }
    public Gtk.CallbackAction GetAction(string actionName) {
        if (!this.actions.ContainsKey(actionName))
            throw new System.FieldAccessException("The key does not exists in the dictionnary !");
        return this.actions[actionName];
    }
}
