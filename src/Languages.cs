using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

/// <summary>
/// Handles localization and language translation using a SQLite database.
/// </summary>
public class Languages {
    /// <value>Dictionary containing all the translations.</value>
    public Dictionary<string, string>? _translations;

    /// <value> String containing the path to the database.</value>
    private readonly string _dbPath;

    /// <value> String containing the locale to use.</value>
    private string _locale = string.Empty;

    /// <value> String containing the name of the table in the database to find the translations in.</value>
    private string _languageTable = string.Empty;

    /// <summary>
    /// Public constructor. Calls <see cref="Create"/> for the true instanciation.
    /// </summary>
    /// <param name="dbPath">The path to the SQLite database.</param>
    public Languages(string dbPath) {
        _dbPath = dbPath;
        this.Create();
    }

    /// <summary>
    /// Asynchronous factory method to make the initialization of the class properly.
    /// </summary>
    /// <returns>This method doesn't return anything.</returns>
    private void Create() { this.Initialize(); }

    /// <summary>
    /// Initializes language data: locale, table name, and translations.
    /// </summary>
    /// <returns>This method doesn't return anything.</returns>
    private void Initialize() {
        _locale = GetLocaleFromLanguage(Globals.Settings._SettingsValues.Language);
        _languageTable =  GetLanguageTable();
        _translations =  GetTranslations();
    }

    /// <summary>
    /// Retrieves a list of all language names from the database.
    /// </summary>
    /// <returns>This method returns a list of strings.</returns>
    public List<string> GetAllLanguages() {
        var languages = new List<string>();

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
         conn.Open();

        const string query = "SELECT full_name FROM LANGUAGES";
        using var cmd = new SqliteCommand(query, conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            languages.Add(reader.GetString(0));
        }

        return languages;
    }

    /// <summary>
    /// Retrieves the locale string corresponding to the specified language name.
    /// </summary>
    /// <param name="language">The language from which to get the locale.</param>
    /// <returns>This method returns a string.</returns>
    private string GetLocaleFromLanguage(string language) {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        const string query = "SELECT locale FROM LANGUAGES WHERE full_name = @language";
        using var cmd = new SqliteCommand(query, conn);
        cmd.Parameters.AddWithValue("@language", language);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? reader.GetString(0) : throw new Exception("Locale not found.");
    }

    /// <summary>
    /// Retrieves the translation table name for the current locale.
    /// </summary>
    /// <returns>This method returns a string.</returns>
    private string GetLanguageTable() {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        const string query = "SELECT table_name FROM LANGUAGES WHERE locale = @locale";
        using var cmd = new SqliteCommand(query, conn);
        cmd.Parameters.AddWithValue("@locale", _locale);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? reader.GetString(0) : throw new Exception("Language table not found.");
    }

    /// <summary>
    /// Retrieves all translations from the current language table.
    /// </summary>
    /// <returns>This methods returns a dictionnary of strings with the translations for the giver language.</returns>
    private Dictionary<string, string> GetTranslations() {
        var translations = new Dictionary<string, string>();

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        var query = $"SELECT field, traduction FROM [{_languageTable}]";

        using var cmd = new SqliteCommand(query, conn);

        using var reader = cmd.ExecuteReader();
        if (!reader.HasRows)
            throw new Exception("Translation table is empty.");

        while (reader.Read()) {
            var key = reader.GetString(0);
            var value = reader.GetString(1);
            translations[key] = value;
        }

        return translations;
    }

    /// <summary>
    /// Gets the translation for the specified key.
    /// </summary>
    /// <param name="field">The field key to translate.</param>
    /// <returns>The translated value.</returns>
    public string Translate(string field) {
        if (_translations == null)
            throw new InvalidOperationException("Translations not initialized.");

        if (!_translations.TryGetValue(field, out var value))
            throw new KeyNotFoundException($"Translation for '{field}' not found.");

        return value;
    }
}
