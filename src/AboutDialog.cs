using System;
using System.Reflection;
using GdkPixbuf;
using GLib;
using GObject;

/// <summary>
/// Allows to create the 'About' dialog window, which GTK's preconfigurations.
/// </summary>
/// <remarks>
/// This class is heriting from the <c>Gtk.AboutDialog</c> class and is a wrapprt around that class.
/// </remarks>
public class TAboutDialog : Gtk.AboutDialog {
    /// <summary>
    /// Constructor of TAboutDialog. Creates a TAboutDialog, it sets all of the propterties of the inherited class.
    /// </summary>
    /// <param name="name">The full name of the application.</param>
    public TAboutDialog(string name) {
        this.Authors = new[] { "Johann Plasse", "Andrei Zeucianu" };
        this.Comments = Globals.Languages.Translate("TeXSharp_description");
        this.Copyright = "© Andrei Zeucianu & Johann Plasse - 2025";
        this.License = "MIT License";
        this.Logo = LoadFromResource("TeXSharp.assets.logo.logo_dark_fg_stoke.png") ?? Gdk.Texture.NewFromFilename("./assets/logo/logo_dark_fg_stoke.png"); // Load the logo of TeXSharp
        this.Version = "0.1-bêta";
        this.Website = "https://github.com/Androl404/TeXSharp"; // Create a website for TeXSharp on GitHub pages
        this.LicenseType = Gtk.License.MitX11;
        this.ProgramName = $"{name} - {Globals.Languages.Translate("modern_latex_editor")}";
    }

    /// <summary>
    /// Private and static method to load a texte from a name.
    /// </summary>
    /// <param name="resourceName">The ressource name to load, it represents a file with a weird filename ('.' instead of '/').</param>
    /// <returns>Returns a <c>Gdk.Texture</c> which might be null. It's the loaded texture.</returns>
    private static Gdk.Texture? LoadFromResource(string resourceName) {
        try {
            var Data = Assembly.GetExecutingAssembly().ReadResourceAsByteArray(resourceName); // Charge the file as an array of bytes
            using var _Bytes = GLib.Bytes.New(Data);
            var PixBufLoader = GdkPixbuf.PixbufLoader.New(); // Pixel buffer
            PixBufLoader.WriteBytes(_Bytes);
            PixBufLoader.Close();
            var PixBuf = PixBufLoader.GetPixbuf() ?? throw new Exception("No pixbuf loaded");
            return Gdk.Texture.NewForPixbuf(PixBuf);
        } catch (Exception e) {
            Console.WriteLine($"Unable to load image resource '{resourceName}': {e.Message}\n" + e.StackTrace);
            return null;
        }
    }
}
