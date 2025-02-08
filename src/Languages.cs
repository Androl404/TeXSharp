using System;
using Microsoft.Data.Sqlite;

public class Languages {
    private Dictionary<string, string>? Traduction;
    private string DBPath;
    private string locale;
    private string language;

    public Languages(string DBPath) {
        this.DBPath = DBPath;
        this.AfterConstruction();
        this.locale = "";
        this.language = "";
    }

    async public void AfterConstruction() {
        this.locale = await DBGetLocaleFromLanguage(Globals.settings._Settings_values.language);
        this.DBGetLanguage();
        this.DBGetTrad();
    }

    async public Task<List<string>> DBGetAllLanguages() {
        await using var conn = new SqliteConnection($"Data Source={this.DBPath}");
        List<string> languages = new List<string>();
        try {
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand($"SELECT * FROM LANGUAGES", conn);
            await using var dataReader = await cmd.ExecuteReaderAsync();

            while (await dataReader.ReadAsync()) {
                languages.Add((string)dataReader["full_name"]);
            }
            await conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
        return languages;
    }

    async private Task<string> DBGetLocaleFromLanguage(string language) {
        await using var conn = new SqliteConnection($"Data Source={this.DBPath}");
        string locale = "";
        try {
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand($"SELECT locale FROM LANGUAGES WHERE full_name = '{language}'", conn);
            await using var dataReader = await cmd.ExecuteReaderAsync();

            while (await dataReader.ReadAsync()) {
                locale = (string)dataReader["locale"];
            }
            this.locale = locale;
            await conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
        return locale;
    }

    async private void DBGetLanguage() {
        await using var conn = new SqliteConnection($"Data Source={this.DBPath}");
        try {
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand($"SELECT table_name FROM LANGUAGES WHERE locale = '{this.locale}'", conn);
            await using var dataReader = await cmd.ExecuteReaderAsync();

            while (await dataReader.ReadAsync()) {
                this.language = (string)dataReader["table_name"];
            }
            await conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
    }

    async private void DBGetTrad() {
        await using var conn = new SqliteConnection($"Data Source={this.DBPath}");

        try {
            await conn.OpenAsync();

            await using var cmd = new SqliteCommand($"SELECT field, traduction FROM {this.language}", conn);
            await using var dataReader = await cmd.ExecuteReaderAsync();

            this.Traduction = new Dictionary<string, string>();
            if (!dataReader.HasRows) throw new System.IndexOutOfRangeException("Database response is empty");
            while (await dataReader.ReadAsync()) {
                string? mkey = Convert.ToString(dataReader["field"]); // (string)
                string? mvalue = Convert.ToString(dataReader["traduction"]);
                if (mkey != null && mvalue != null)
                this.Traduction.Add(mkey, mvalue);
            }
            await conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
    }

    public string ServeTrad(string field) {
        if (this.Traduction is null) throw new System.ArgumentNullException("Dictionnary translation is null");
        return this.Traduction[field];
    }
}
