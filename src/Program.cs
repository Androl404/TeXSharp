using System;
using Gtk;
using GtkSource;

class Program {
    public static int Main(string[] args) {
        GtkSource.Module.Initialize(); // To initialize the text editor
        var application = Gtk.Application.New("com.github.TeXSharp", Gio.ApplicationFlags.FlagsNone);
        application.OnActivate += (sender, args) => {
            // Create main Window
            var window = new Window($"{Globals.lan.ServeTrad("new_file")} - TeXSharp", 800, 600, sender);
            window.SetHeaderBar(window._Window);
        };

        return application.RunWithSynchronizationContext(null);
    }
}
