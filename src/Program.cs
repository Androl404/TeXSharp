using System;
using Gtk;
using GtkSource;

class Program {
    public static int Main(string[] args) {
        GtkSource.Module.Initialize(); // To initialize the text editor
        var Application = Gtk.Application.New("com.github.TeXSharp", Gio.ApplicationFlags.FlagsNone);
        Application.OnActivate += (sender, args) => {
            // Create main Window
            var Window = new Window($"{Globals.Languages.ServeTrad("new_file")} - TeXSharp", 800, 600, sender);
            Window.SetHeaderBar(Window._MWindow);
        };

        return Application.RunWithSynchronizationContext(null);
    }
}
