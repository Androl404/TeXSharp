using System;
using Microsoft.Data.Sqlite;

public class Languages {
    private Dictionary<string, string>? Traduction;
    private string DBPath;
    private string Locale = string.Empty;
    private string Language = string.Empty;

    public Languages(string DBPath) {
        this.DBPath = DBPath;
        this.AfterConstruction();
    }

    async public void AfterConstruction() {
        this.Locale = await DBGetLocaleFromLanguage(Globals.Settings._SettingsValues.Language);
        this.DBGetLanguage();
        this.DBGetTrad();
    }

    async public Task<List<string>> DBGetAllLanguages() {
        await using var Conn = new SqliteConnection($"Data Source={this.DBPath}");
        List<string> Languages = new List<string>();
        try {
            await Conn.OpenAsync();
            await using var Cmd = new SqliteCommand($"SELECT * FROM LANGUAGES", Conn);
            await using var DataReader = await Cmd.ExecuteReaderAsync();

            while (await DataReader.ReadAsync()) {
                Languages.Add((string)DataReader["full_name"]);
            }
            await Conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
        return Languages;
    }

    async private Task<string> DBGetLocaleFromLanguage(string language) {
        await using var Conn = new SqliteConnection($"Data Source={this.DBPath}");
        string Locale = "";
        try {
            await Conn.OpenAsync();
            await using var Cmd = new SqliteCommand($"SELECT locale FROM LANGUAGES WHERE full_name = '{language}'", Conn);
            await using var DataReader = await Cmd.ExecuteReaderAsync();

            while (await DataReader.ReadAsync()) {
                Locale = (string)DataReader["locale"];
            }
            this.Locale = Locale;
            await Conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
        return Locale;
    }

    async private void DBGetLanguage() {
        await using var Conn = new SqliteConnection($"Data Source={this.DBPath}");
        try {
            await Conn.OpenAsync();
            await using var Cmd = new SqliteCommand($"SELECT table_name FROM LANGUAGES WHERE locale = '{this.Locale}'", Conn);
            await using var DataReader = await Cmd.ExecuteReaderAsync();

            while (await DataReader.ReadAsync()) {
                this.Language = (string)DataReader["table_name"];
            }
            await Conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
    }

    async private void DBGetTrad() {
        await using var Conn = new SqliteConnection($"Data Source={this.DBPath}");

        try {
            await Conn.OpenAsync();
            await using var Cmd = new SqliteCommand($"SELECT field, traduction FROM {this.Language}", Conn);
            await using var DataReader = await Cmd.ExecuteReaderAsync();

            this.Traduction = new Dictionary<string, string>();
            if (!DataReader.HasRows)
                throw new System.IndexOutOfRangeException("Database response is empty");
            while (await DataReader.ReadAsync()) {
                string? Mkey = Convert.ToString(DataReader["field"]); // (string)
                string? Mvalue = Convert.ToString(DataReader["traduction"]);
                if (Mkey != null && Mvalue != null)
                    this.Traduction.Add(Mkey, Mvalue);
            }
            await Conn.CloseAsync();
        } catch (Exception e) {
            throw new System.NullReferenceException("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
    }

    public string ServeTrad(string field) {
        if (this.Traduction is null)
            throw new System.ArgumentNullException("Dictionnary translation is null");
        return this.Traduction[field];
    }
}
