using System;
using Gtk;
using Cairo;
using IronPdf;

// Utilisation de GirCore

namespace TeXSharp {
class Program {
  public static int Main(string[] args) {
    // Console.WriteLine("Hello, World!");

    var application =
        Gtk.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);
    application.OnActivate += (sender, args) => {
      var window = Gtk.ApplicationWindow.New((Gtk.Application)sender);
      window.Title = "Gtk4 Window";
      window.SetDefaultSize(800, 600);

      // Create Grid
      var grid = Grid.New();
      grid.SetHexpand(true);
      grid.SetVexpand(true);

      // Create ScrolledWindow for scrolling capability
      var scrolled = ScrolledWindow.New();
      scrolled.SetHexpand(true);
      scrolled.SetVexpand(true);

      // Create TextView
      var textView = TextView.New();

      // Add TextView to ScrolledWindow
      scrolled.SetChild(textView);

      // Create some other elements for example
      var button1 = Button.New();
      button1.Label = "Save";

      var button2 = Button.New();
      button2.Label = "Load";

      var label = Label.New("Editor:");

      // Load the PDF file
      var pdf = new PdfDocument("/home/jojo/LATEX/modele/modele.pdf");

      // Render the PDF to an image
      var image = pdf.RasterizeToImageFiles("/tmp/*.png");

      var imagePdf = Gtk.Image.NewFromFile("/tmp/1.png");
      imagePdf.PixelSize = 500;
      imagePdf.SetHexpand(true);
      imagePdf.SetVexpand(true);

      var scrolledPdf = ScrolledWindow.New();
      scrolledPdf.SetHexpand(true);
      scrolledPdf.SetVexpand(true);

      scrolledPdf.SetChild(imagePdf);

      // Attach widgets to the grid
      // Syntax: grid.Attach(widget, column, row, width, height)
      grid.Attach(label, 0, 0, 2, 1);       // Spans 2 columns in the first row
      grid.Attach(button1, 0, 1, 1, 1);     // First column, second row
      grid.Attach(button2, 1, 1, 1, 1);     // Second column, second row
      grid.Attach(scrolled, 0, 2, 2, 1);    // Spans 2 columns in the third row
      grid.Attach(scrolledPdf, 2, 2, 1, 1); // Spans 2 columns in the third row

      // Optional: Add some spacing between elements
      grid.RowSpacing = 10;
      grid.ColumnSpacing = 10;
      grid.SetMarginStart(15);
      grid.SetMarginTop(15);
      grid.SetMarginBottom(15);
      /*grid.SetMarginEnd(15);*/

      // Set the grid as the window's child
      window.SetChild(grid);

      // If you want to control whether it's editable:
      textView.Editable = true; // or false to make it read-only
      textView.Buffer.Text = "Hello, World!";
      window.Show();
    };

    return application.RunWithSynchronizationContext(null);
  }
}
}
