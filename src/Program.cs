﻿using System;
using Gtk;
using Gio;

class Program {
    public static int Main(string[] args) {
        var application = Gtk.Application.New("com.github.TeXSharp", Gio.ApplicationFlags.FlagsNone);
        application.OnActivate += (sender, args) => {
            // Create main Window
            var window = new Window("TeXSharp - A modern LaTeX editor", 800, 600, sender);
            window.SetHeaderBar();

            var text_editor = window.MakeTextEditor();
            var PDF_viewer = window.MakePDFViewer();

            // Create some other elements for example
            var button1 = Gtk.Button.New();
            button1.Label = "Save";
            var button2 = Gtk.Button.New();
            button2.Label = "Load";
            var label = Gtk.Label.New("Editor:");

            // Create Grid
            var grid = Gtk.Grid.New();
            grid.SetHexpand(true);
            grid.SetVexpand(true);

            // Attach widgets to the grid
            // Syntax: grid.Attach(widget, column, row, width, height)
            grid.Attach(label, 0, 0, 2, 1); // Spans 2 columns in the first row
            grid.Attach(button1, 0, 1, 1, 1); // First column, second row
            grid.Attach(button2, 1, 1, 1, 1); // Second column, second row
            grid.Attach(text_editor, 0, 2, 2, 1); // Spans 2 columns in the third row
            grid.Attach(PDF_viewer, 2, 2, 1, 1); // Spans 2 columns in the third row

            // Optional: Add some spacing between elements
            grid.RowSpacing = 10;
            grid.ColumnSpacing = 10;
            grid.SetMarginStart(15);
            grid.SetMarginTop(15);
            grid.SetMarginBottom(15);
            // grid.SetMarginEnd(15);

            // Set the grid as the window's child
            window._Window.SetChild(grid);

        };

        return application.RunWithSynchronizationContext(null);
    }
}
