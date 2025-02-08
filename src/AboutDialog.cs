using System;
using System.Reflection;
using GdkPixbuf;
using GObject;

// No need for a namespace
// namespace TAboutDialog;

public class TAboutDialog : Gtk.AboutDialog {
    public TAboutDialog(string Name) {
        Authors = new[] { "Johann Plasse", "Andrei Zeucianu" };
        Comments = Globals.lan.ServeTrad("TeXSharp_description");
        Copyright = "© Andrei Zeucianu & Johann Plasse - 2025";
        License = "MIT License";
        Logo = LoadFromResource("TeXSharp.assets.logo.logo_dark_fg_stoke.png") ?? Gdk.Texture.NewFromFilename("./assets/logo/logo_dark_fg_stoke.png");
        Version = "0.1-bêta";
        Website = "https://github.com/Androl404/TeXSharp"; // Create a website for TeXSharp on GitHub pages
        LicenseType = Gtk.License.MitX11;
        ProgramName = $"{Name} - {Globals.lan.ServeTrad("modern_latex_editor")}";
    }

    private static Gdk.Texture? LoadFromResource(string resourceName) {
        try {
            var bytes = Assembly.GetExecutingAssembly().ReadResourceAsByteArray(resourceName);
            var pixbuf = PixbufLoader.FromBytes(bytes);
            return Gdk.Texture.NewForPixbuf(pixbuf);
        } catch (Exception e) {
            Console.WriteLine($"Unable to load image resource '{resourceName}': {e.Message}" + e);
            return null;
        }
    }
}
