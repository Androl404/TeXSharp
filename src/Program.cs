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

            // Create some other elements for example
            // var button1 = Gtk.Button.New();
            // button1.Label = Globals.lan.ServeTrad("save");
            // var button2 = Gtk.Button.New();
            // button2.Label = Globals.lan.ServeTrad("load");
            // var label = Gtk.Label.New(Globals.lan.ServeTrad("editor"));

            // Attach widgets to the grid
            // Syntax: grid.Attach(widget, column, row, width, height)
            // window._Grid.Attach(label, 0, 0, 2, 1);       // Spans 2 columns in the first row/column
            // window._Grid.Attach(button1, 0, 1, 1, 1);     // First column, second row
            // window._Grid.Attach(button2, 1, 1, 1, 1);     // Second column, second row
            // window._Grid.Attach(text_editor, 0, 2, 2, 1); // Spans 2 columns in the third row
            // window._Grid.Attach(PDF_viewer, 2, 2, 3, 1);  // Spans 3 columns in the third row/column

            // Optional: Add some spacing between elements
            // window._Grid.RowSpacing = 10;
            window._Grid.ColumnSpacing = 10;
            // window._Grid.SetMarginStart(15);
            // window._Grid.SetMarginTop(15);
            // window._Grid.SetMarginBottom(15);
            // grid.SetMarginEnd(15);

            // Set the grid as the window's child
            window._Window.SetChild(window._Grid);
        };

        return application.RunWithSynchronizationContext(null);
    }
}
