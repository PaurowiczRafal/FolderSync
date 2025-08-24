
using FolderSync;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    //Default paths and interval
    static string _sourcePath = @"C:\Users\admin\Documents\FolderSync\source";
    static string _replicaPath = @"C:\Users\admin\Documents\FolderSync\replica";
    static string _logFilePath = @"C:\Users\admin\Documents\FolderSync\Log.txt";
    static int _interval = 2;

    static async Task Main(string[] args)
    {
       

        if (args.Length == 0)
        {
            Console.WriteLine("No arguments, using default paths and interval.");
        }
        else
        {
            ArgsParser(args);
        }

        await Logger.InitAsync(_logFilePath);
        await Logger.LogAsync("Starting program");
        await Logger.LogAsync($"Source folder path: {_sourcePath}");
        await Logger.LogAsync($"Replica folder path: {_replicaPath}");
        await Logger.LogAsync($"Log file path: {_logFilePath}");
        await Logger.LogAsync($"Synchornization interval: {_interval} seconds.");
        await Logger.LogAsync("Press CTRL + C, to stop synchronizing and exit.");

        SyncHandler syncHandle = new SyncHandler();
        await syncHandle.SyncHandleInit(_sourcePath, _replicaPath);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            await syncHandle.SyncFolders();
            stopwatch.Stop();
            await Logger.LogAsync($"Duraton: {stopwatch.ElapsedMilliseconds} ms.");
            await Task.Delay(TimeSpan.FromSeconds(_interval));
        }

        await Logger.LogAsync("Synchorization stopped.");

    }

    static void ArgsParser(string[] args)
    {
        Console.WriteLine("ArgsPArser");
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-s": // source
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("No value for -s (source).");
                    }
                    _sourcePath = args[++i];
                    break;

                case "-r": // replica
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("No value for -r (replica)");
                    }
                    _replicaPath = args[++i];
                    break;

                case "-i": // interval
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("No value for dla -i (interval).");
                    }
                    if (!int.TryParse(args[++i], out _interval) || _interval <= 0)
                    {
                        Console.WriteLine("The argument -i must be a positive integer (seconds).");
                    }
                    break;

                case "-l": // log
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("No value for -l (log).");
                    }
                    _logFilePath = args[++i];
                    break;

                default:
                    Console.WriteLine($"Unknown argumentt: {a}");
                    break;

            }
        }
    }
}