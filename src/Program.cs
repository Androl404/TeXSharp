using System;
using Gtk;
using Gio;

class Program {
    public static int Main(string[] args) {
        GtkSource.Module.Initialize(); // Pour initialiser l'éditeur de texte
        var application = Gtk.Application.New("com.github.TeXSharp", Gio.ApplicationFlags.FlagsNone);
        application.OnActivate += (sender, args) => {
            // Create main Window
            var window = new Window($"TeXSharp - {Globals.lan.ServeTrad("modern_latex_editor")}", 800, 600, sender);
            window.SetHeaderBar(window._Window);

            var text_editor = window.MakeTextEditor();
            var PDF_viewer = window.MakePDFViewer(null);
            window.MakeButtonBar();

            // Set the grid as the window's child
            window._Window.SetChild(window._Grid);
        };

        return application.RunWithSynchronizationContext(null);
    }
}
