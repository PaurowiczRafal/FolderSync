
using FolderSync;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        //Default paths and interval
        string sourceFolderPathDefault = @"C:\Users\admin\Documents\Test\source";
        string replicaFolderPathDefault = @"C:\Users\admin\Documents\Test\replica";
        string logFilePathDefault = @"C:\Users\admin\Documents\Test\Log.txt";
        int intervalDefault = 10;


        string sourcePath = sourceFolderPathDefault;
        string replicaPath = replicaFolderPathDefault;
        string logFilePath = logFilePathDefault;
        int interval = intervalDefault;

        #region Verify if any args
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments, using default paths and interval.");
        }
        else
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Incorrect number of arguments. There should be 4 arguments. (sourcePath | replicaPath | interval | LogFilePath)");
                return;
            }
            if (args.Length == 4)
            {
                sourcePath = args[0];
                replicaPath = args[1];
                if (!int.TryParse(args[2], out interval))
                {
                    Console.WriteLine("Wrong, argument should be a number");
                }
                logFilePath = args[3];

                Console.WriteLine($"Using: FolderSync {sourcePath} | {replicaPath} | {interval} second| {logFilePath}");
            }
        }
        #endregion

        await Logger.InitAsync(logFilePath);
        await Logger.LogAsync("Starting program");
        await Logger.LogAsync($"Source folder path: {sourcePath}");
        await Logger.LogAsync($"Replica folder path: {replicaPath}");
        await Logger.LogAsync($"Log file path: {logFilePath}");
        await Logger.LogAsync($"Synchornization interval: {interval} seconds.");
        await Logger.LogAsync("Press CTRL + c, to stop synchronizing and exit.");

        SyncHandler syncHandle = new SyncHandler();
        await syncHandle.SyncHandleInit(sourcePath, replicaPath);

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
            await Task.Delay(TimeSpan.FromSeconds(interval));
        }

        await Logger.LogAsync("Synchorization stopped.");

    }
}
