using Gtk;
using System.Text.RegularExpressions;

/// <summary>
/// Utility class to parse and categorize lines from a LaTeX log file,
/// primarily by running `texlogsieve` and analyzing its output.
/// </summary>
public class LogErrorParser {

    /// <summary>
    /// Default constructor. Currently does not perform any initialization.
    /// </summary>
    public LogErrorParser() {}

    /// <summary>
    /// Checks if a line consists of the same repeating character.
    /// Typically used to identify and skip visual dividers (e.g., "-----" or "*****").
    /// </summary>
    /// <param name="line">The line of text to analyze.</param>
    /// <returns>True if the line contains only a repeated character; false otherwise.</returns>
    static public bool IsDummyLine(string line) { return Regex.IsMatch(line, @"/^(.)\1*$/"); }

    /// <summary>
    /// Checks if the line denotes the start of a file-related section in the log.
    /// </summary>
    /// <param name="line">The line of text to check.</param>
    /// <returns>True if the line starts with "From file"; false otherwise.</returns>
    static public bool IsFileLine(string line) { return line.StartsWith("From file "); }

    /// <summary>
    /// Categorizes a line based on its content as an error, warning, or info.
    /// </summary>
    /// <param name="line">The line to categorize.</param>
    /// <returns>
    /// "error" if the line starts with '!',
    /// "warning" if it contains the word "warning",
    /// "info" if it contains "info", "Overfull", or "Underfull",
    /// otherwise an empty string.
    /// </returns>
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

    /// <summary>
    /// Executes the `texlogsieve` tool on a LaTeX log file and returns the processed output as a single string.
    /// </summary>
    /// <param name="file_path">The path to the log file.</param>
    /// <returns>The output string from texlogsieve.</returns>
    static async public Task<string> LogProcess(string file_path) {
        var LogProcess = await ProcessAsyncHelper.ExecuteShellCommand("texlogsieve", file_path + " --no-summary --no-page-delay --no-shipouts --file-banner --box-detail", 50000);

        string Output = LogProcess.Output;
        Console.WriteLine(LogProcess.Output); // Debug output
        Console.WriteLine("------------------- End of Log -------------------");
        return Output;
    }

    /// <summary>
    /// Parses the output of a LaTeX log (processed by texlogsieve), line-by-line,
    /// identifying file names, line numbers, and categorizing log messages.
    /// </summary>
    /// <param name="log">The complete log string to parse.</param>
    static public void ParseLog(string log) {
        Console.WriteLine("------------------- Start of Log Parsing -------------------");
        string[] lines = log.Split('\n');

        foreach (string line in lines) {
            // Skip decorative lines
            if (IsDummyLine(line)) {
                continue;
            }

            // Detect and print file names
            else if (IsFileLine(line)) {
                if (line.Contains("./")) {
                    int startPos = line.IndexOf("./");
                    int endPos = line.IndexOf(":");
                    Console.WriteLine("File: " + line.Substring(startPos + 2, endPos - startPos - 2));
                } else {
                    int startPos = line.IndexOf("/");
                    int endPos = line.IndexOf(":");
                    Console.WriteLine("File: " + line.Substring(startPos, endPos - startPos));
                }
            }

            // Detect line numbers (e.g., "l.12")
            else if (line.StartsWith("l.")) {
                Console.WriteLine("Line:  " + line.Substring(2));
            }

            // Categorize message by type
            else {
                string type = IsImportantLine(line);
                if (type == "warning") {
                    Console.WriteLine("Warning: " + line);
                } else if (type == "error") {
                    Console.WriteLine("Error: " + line);
                } else if (type == "info") {
                    Console.WriteLine("Info: " + line);
                }

                // Print all other messages unless they're pure decoration
                else if (type == "") {
                    if (!Regex.IsMatch(line, @"^[*-]+$")) {
                        Console.WriteLine(line);
                    }
                }
            }
        }
    }
}
