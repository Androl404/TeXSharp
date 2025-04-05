using Gtk;
using System.Text.RegularExpressions;

public class LogErrorParser
{

    public LogErrorParser() { }

    public bool IsDummyLine(string line) { return Regex.IsMatch(line, @"/^(.)\1*$/"); }

    public bool IsFileLine(string line) { return line.StartsWith("From file "); }

    public string IsImportantLine(string line)
    {
        string return_string = "";
        if (line.ToLower().Contains("warning"))
        {
            return_string = "warning";
        }
        else if (line.StartsWith('!'))
        {
            return_string = "error";
        }
        else if (Regex.IsMatch(line.ToLower(), @"/\binfo\b[^\w]?/") || line.Contains("Overfull") || line.Contains("Underfull"))
        {
            return_string = "info";
        }
        return return_string;
    }

    static async public Task<string> ParseLog(string file_path)
    {
        var LogProcess = await ProcessAsyncHelper.ExecuteShellCommand("texlogsieve", file_path + " --no-summary --no-page-delay --no-shipouts --file-banner --box-detail", 50000);
        string Output = LogProcess.Output;
        Console.WriteLine(LogProcess.Output); // It's working now!!!
        return Output;
    }
}
