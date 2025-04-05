using Gtk;
using System.Text.RegularExpressions;

public class LogErrorParser {

    public LogErrorParser() {}

    static public bool IsDummyLine(string line) { return Regex.IsMatch(line, @"/^(.)\1*$/"); }

    static public bool IsFileLine(string line) { return line.StartsWith("From file "); }

    static public string IsImportantLine(string line) {
        string return_string = "";
        if (line.ToLower().Contains("warning")) {
            return_string = "warning";
        } else if (line.StartsWith('!')) {
            return_string = "error";
        } else if (Regex.IsMatch(line.ToLower(), @"/\binfo\b[^\w]?/") || line.Contains("Overfull") || line.Contains("Underfull")) {
            return_string = "info";
        }
        return return_string;
    }

    static async public Task<string> LogProcess(string file_path) {
        var LogProcess = await ProcessAsyncHelper.ExecuteShellCommand("texlogsieve", file_path + " --no-summary --no-page-delay --no-shipouts --file-banner --box-detail", 50000);
        string Output = LogProcess.Output;
        Console.WriteLine(LogProcess.Output); // It's working now!!!
        Console.WriteLine("------------------- End of Log -------------------");
        return Output;
    }

    // LogProcess returns the output in a single string. But when printed, it is printed line by line. So we can splint the string by each \n and then process each line.
    static public void ParseLog(string log) {
        Console.WriteLine("------------------- Start of Log Parsing -------------------");
        string[] lines = log.Split('\n');
        foreach (string line in lines) {
            if (IsDummyLine(line)) {
                continue; // We skip and it executes the next iteration of the loop.
            } else if (IsFileLine(line)) {
                if (line.Contains("./")) {
                    int startPos = line.IndexOf("./");
                    int endPos = line.IndexOf(":");
                    Console.WriteLine("File: " + line.Substring(startPos + 2, endPos - startPos - 2));
                } else {
                    int startPos = line.IndexOf("/");
                    int endPos = line.IndexOf(":");
                    Console.WriteLine("File: " + line.Substring(startPos, endPos - startPos));
                }
            } else if (line.StartsWith("l.")) {
                Console.WriteLine("Line:  " + line.Substring(2));
            } else {
                string type = IsImportantLine(line);
                if (type == "warning") {
                    Console.WriteLine("Warning: " + line);
                } else if (type == "error") {
                    Console.WriteLine("Error: " + line);
                } else if (type == "info") {
                    Console.WriteLine("Info: " + line);
                } else if (type == "") {
                    if (!(Regex.IsMatch(line, @"^[*-]+$")))
                        Console.WriteLine(line);
                }
            }
        }
    }
}
