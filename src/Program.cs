using System;
using Gtk;
using GtkSource;

/// <summary>
/// Main class which goal is to create the application and lauch the window when the application stars.
/// </summary>
class Program {
    /// <summary>
    /// Main methods lauched on application startup.
    /// </summary>
    /// <param name="args">The command line arguments for the application.</param>
    /// <returns>Returns an <c>int</c>, the exit code of the application.</returns>
    public static int Main(string[] args) {
        GtkSource.Module.Initialize(); // To initialize the text editor
        var Application = Gtk.Application.New("com.github.TeXSharp", Gio.ApplicationFlags.FlagsNone);
        Application.OnActivate += (sender, args) => {
            // Create main Window
            var Window = new Window($"{Globals.Languages.Translate("new_file")} - TeXSharp", 800, 600, sender);
            Window.SetHeaderBar(Window._MWindow);
        };

        return Application.RunWithSynchronizationContext(null); // Run the application
    }
}
