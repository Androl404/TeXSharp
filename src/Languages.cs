using System;
using Microsoft.Data.Sqlite;

public class Languages {
    private Dictionary<string, string> Traduction;
    private string DBPath;
    private string locale;
    private string language;

    public Languages(string DBPath, string locale) {
        this.locale = locale;
        this.DBPath = DBPath;
        this.DBGetLanguage();
        this.DBGetTrad();
        Console.WriteLine();
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
            // TODO: Make a graphical popup window in case of error
            Console.WriteLine("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
            // var diag = new Gtk.MessageDialog();
            // diag.Text = "Texte de secours";
            // diag.Show();
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
                string mkey = Convert.ToString(dataReader["field"]);
                string mvalue = Convert.ToString(dataReader["traduction"]);
                this.Traduction.Add(mkey, mvalue);
            }
            await conn.CloseAsync();
        } catch (Exception e) {
            // TODO: Make a graphical popup window in case of error
            Console.WriteLine("Erreur lors de la connection et de la récupération des données de la la base de données.\n" + e);
        }
    }

    public string ServeTrad(string field) {
        return this.Traduction[field];
    }
}

// using (var connection = new SqliteConnection("Data Source=./assets/languages.sqlite"))
// {
//     connection.Open();

//     var command = connection.CreateCommand();
//     command.CommandText =
//     @"
//         SELECT traduction
//         FROM FRENCH
//         WHERE field = $id
//     ";
//     command.Parameters.AddWithValue("$id", "save");

//     using (var reader = command.ExecuteReader())
//     {
//         while (reader.Read())
//         {
//             var name = reader.GetString(0);

//             Console.WriteLine($"Hello, {name}!");
//         }
//     }
// }
