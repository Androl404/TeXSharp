/// <sumary>
/// A list of global attributes.
/// </sumary>
/// <remarks>
/// These attributes can be used anywhere into the project.
/// All of these attributes are readonly and static.
/// </remarks>
public static class Globals {
    /// <value>Attribute <c>Settings</c> containing all the settings of the application.</value>
    public static readonly Settings Settings = new Settings();
    /// <value>Attribute <c>Languages</c> containing the languages content and queries to the database.</value>
    public static readonly Languages Languages = new Languages("./assets/languages.sqlite");
    /// <value>Attribute <c>PangoScale</c> to correctly scale the fonts.</value>
    public static readonly int PangoScale = 1024;
}
