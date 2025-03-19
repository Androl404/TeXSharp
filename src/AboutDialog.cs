using System;
using System.Reflection;
using GdkPixbuf;
using GLib;
using GObject;

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
            var data = Assembly.GetExecutingAssembly().ReadResourceAsByteArray(resourceName);
            using var bytes = Bytes.New(data);
            var pixbufLoader = PixbufLoader.New();
            pixbufLoader.WriteBytes(bytes);
            pixbufLoader.Close();

            var pixbuf = pixbufLoader.GetPixbuf() ?? throw new Exception("No pixbuf loaded");
            return Gdk.Texture.NewForPixbuf(pixbuf);
        } catch (Exception e) {
            Console.WriteLine($"Unable to load image resource '{resourceName}': {e.Message}\n" + e.StackTrace);
            return null;
        }
    }
}
