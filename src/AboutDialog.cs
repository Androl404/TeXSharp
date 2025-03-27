using System;
using System.Reflection;
using GdkPixbuf;
using GLib;
using GObject;

public class TAboutDialog : Gtk.AboutDialog {
    public TAboutDialog(string Name) {
        Authors = new[] { "Johann Plasse", "Andrei Zeucianu" };
        Comments = Globals.Languages.ServeTrad("TeXSharp_description");
        Copyright = "© Andrei Zeucianu & Johann Plasse - 2025";
        License = "MIT License";
        Logo = LoadFromResource("TeXSharp.assets.logo.logo_dark_fg_stoke.png") ?? Gdk.Texture.NewFromFilename("./assets/logo/logo_dark_fg_stoke.png");
        Version = "0.1-bêta";
        Website = "https://github.com/Androl404/TeXSharp"; // Create a website for TeXSharp on GitHub pages
        LicenseType = Gtk.License.MitX11;
        ProgramName = $"{Name} - {Globals.Languages.ServeTrad("modern_latex_editor")}";
    }

    private static Gdk.Texture? LoadFromResource(string resourceName) {
        try {
            var Data = Assembly.GetExecutingAssembly().ReadResourceAsByteArray(resourceName);
            using var _Bytes = Bytes.New(Data);
            var PixBufLoader = PixbufLoader.New();
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
