// A list of global variables
public static class Globals {
    // public const Int32 BUFFER_SIZE = 512; // Unmodifiable
    // public static String FILE_NAME = "Output.txt"; // Modifiable
    // public static readonly String CODE_PREFIX = "US-"; // Unmodifiable
    public static readonly Settings Settings = new Settings();
    public static readonly Languages Languages = new Languages("./assets/languages.sqlite");
    public static readonly int PangoScale = 1024;
}
