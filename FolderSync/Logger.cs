namespace FolderSync
{
    internal static class Logger
    {
        private static string _logFilePath = string.Empty;
        private static readonly SemaphoreSlim _gate = new(1, 1);
        public static async Task InitAsync(string path)
        {
            _logFilePath = path;
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
            await LogAsync("Logger is ready.");
        }
        public static async Task LogAsync(string message)
        {
            string logMessage = DateTime.Now.ToString("dd-MM-yyyy | HH:mm:ss | ") + message;
            await _gate.WaitAsync();
            Console.WriteLine(logMessage);
            try
            {
                await File.AppendAllTextAsync(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
