using System;
using Microsoft.Data.Sqlite;

public class Languages {
    private Dictionary<string, string> Traduction;
    private string DBPath;
    private string locale;
    private string? language;

    public Languages(string DBPath, string locale) {
        this.locale = locale;
        this.DBPath = DBPath;
        this.DBGetLanguage();
        this.DBGetTrad();
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
        return this.Traduction[field];
    }
}
