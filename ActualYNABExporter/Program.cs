// See https://aka.ms/new-console-template for more information

using System.Data.SQLite;
using ActualYNABExporter;
using Dapper;

var validFile = false;

Console.WriteLine("Please input the full path to your db.sqlite file exported from actual.");
while (!validFile)
{
    var path = Console.ReadLine();
    path = path.Trim().Replace("\"","");

    if (!File.Exists(path) || !path.EndsWith(".sqlite"))
    {
        Console.WriteLine($"I wasn't able to find the file at {path}, please make sure you typed the directory in correctly and the file ends in .sqlite");
        Console.WriteLine("Please input the full path to your db.sqlite file exported from actual.");
    }
    else
    {
        SQLite.DBFile = path;
        validFile = true;
    }
}

var accounts = new List<Account>();

using (var cnn = SQLite.SqLiteConnection())
{
    cnn.Open();
    accounts = cnn.Query<Account>(
        @"SELECT id, name, type from accounts").ToList();
    Console.WriteLine($"Found a total of {accounts.Count} accounts.");
    
    Console.Write("Specify a directory to save the Account csv files to: ");
    var path = Console.ReadLine();
    
    bool validPath = false;
    while (!validPath)
    {
        if (Directory.Exists(path))
        {
            Console.WriteLine("Valid path supplied; ready to export transactions!\nPress any key to begin");
            validPath = true;
        }
        else
        {
            Console.Write($"The path {path} was not found or is not a directory, please provide a valid directory to export to:");
            path = Console.ReadLine();
        }
    }

    path.Trim();
    if (path.EndsWith("\\") || path.EndsWith("/"))
    {
        path.Substring(0, path.Length - 1);
    }
    Console.ReadLine();

    foreach (var account in accounts)
    {
        Console.WriteLine($"Exporting {account.name}");
        var transactions = new List<Transaction>();
        transactions = cnn.Query<Transaction>(
            @"SELECT date, payees.name, notes, amount FROM v_transactions
                INNER JOIN payees on payee = payees.id
                WHERE account = @account", new { account = account.id }).ToList();

        if (!CSVWriter.WriteCSVFile(path + $"\\{account.name}.csv", transactions))
        {
            Console.WriteLine($"{account.name} did not successfully create a csv file, please read the above error and try again.");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine($"Succesfully createad {path}\\{account.name}.csv");
        }
        // Console.WriteLine($"For account {account.name}, a total of {transactions.Count} were found. Listing them below:");
        // foreach (var transaction in transactions)
        // {
        //     Console.WriteLine($"{transaction.name}\t{transaction.date}\t{transaction.amount}\t{transaction.notes}");
        // }
        // Console.WriteLine($"====END OF ACCOUNT {account.name}====");
    }
    
    Console.WriteLine($"Export process complete! You can find your files @ {path}");
    Environment.Exit(0);
}

public static class SQLite
{
    public static string DBFile { get; set; }

    public static SQLiteConnection SqLiteConnection()
    {
        return new SQLiteConnection("Data Source=" + DBFile);
    }
}

public static class CSVWriter
{
    public static bool WriteCSVFile(string path, List<Transaction> transactions)
    {
        try
        {
            using (var sw = new StreamWriter(path))
            {
                sw.WriteLine("Date,Payee,Memo,Amount");
                foreach (var transaction in transactions)
                {
                    //var dt = DateTime.ParseExact(transaction.date.ToString(), "yyyMMdd", null);
                    sw.WriteLine(
                        $"{DateConverter(transaction.date.ToString())},{transaction.name},{transaction.notes},{transaction.amount}");
                }

            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private static string DateConverter(string date)
    {
        var year = date.Substring(0, 4);
        var month = date.Substring(4, 2);
        var day = date.Substring(6, 2);
        return $"{day}/{month}/{year}";
    }
}

