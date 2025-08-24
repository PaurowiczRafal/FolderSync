using System.Security.Cryptography;
using System.Text;

namespace FolderSync
{
    internal class SyncHandler
    {
        private string _sourcePath = string.Empty;
        private string _replicaPath = string.Empty;

        public async Task SyncHandleInit(string sourcePath, string replicaPath)
        {
            _sourcePath = await CheckIfFolderExistsAsync(sourcePath);
            _replicaPath = await CheckIfFolderExistsAsync(replicaPath);
        }

        public async Task SyncFolders()
        {
            await Logger.LogAsync("Start sync.");

            var sourceFolders = Directory.GetDirectories(_sourcePath, "*", SearchOption.AllDirectories);
            var replicaFolders = Directory.GetDirectories(_replicaPath, "*", SearchOption.AllDirectories);

            var sourceFiles = Directory.GetFiles(_sourcePath, "*", SearchOption.AllDirectories);
            var replicaFiles = Directory.GetFiles(_replicaPath, "*", SearchOption.AllDirectories);

            if (await CompareFolderStructureHashAsync(sourceFolders, sourceFiles, replicaFolders, replicaFiles))
            {
                await Logger.LogAsync("Folders are identical. No sync needed.");
                return;
            }

            await MirrorFoldersAsync(sourceFolders);
            await VerifyFileToCopyOrUpdateAsync(sourceFolders);
            await DeleteFilesAsync(replicaFolders);
        }

        private async Task MirrorFoldersAsync(string[] sourceFolders)
        {
            await Logger.LogAsync("Mirroring folders.");
            foreach (var sourceDir in sourceFolders)
            {
                string sourceFolderName = Path.GetRelativePath(_sourcePath, sourceDir);
                string replicaFolderPath = Path.Combine(_replicaPath, sourceFolderName);
                await CheckIfFolderExistsAsync(replicaFolderPath);
            }
            await Logger.LogAsync("Mirroring folders DONE");
        }

        private async Task VerifyFileToCopyOrUpdateAsync(string[] sourceFolders)
        {
            await Logger.LogAsync("Copying or updating files.");
            foreach (var sourceDir in sourceFolders)
            {
                string sourceFileName = Path.GetRelativePath(_sourcePath, sourceDir);
                string replicaFilePath = Path.Combine(_replicaPath, sourceFileName);

                if (!File.Exists(replicaFilePath))
                {
                    await CopyFileAsync(sourceDir, replicaFilePath);
                }
                else
                {
                    var sourceFileInfo = new FileInfo(sourceDir);
                    var replicaFileInfo = new FileInfo(replicaFilePath);

                    if (sourceFileInfo.Length != replicaFileInfo.Length ||
                        sourceFileInfo.LastWriteTimeUtc != replicaFileInfo.LastWriteTimeUtc)
                    {
                        await CopyFileAsync(sourceDir, replicaFilePath);
                        continue;
                    }
                    else
                    {
                        var sourceHashFile = await ComputeHashFile(sourceDir);
                        var replicaHashFile = await ComputeHashFile(replicaFilePath);
                        if (!sourceHashFile.SequenceEqual(replicaHashFile))
                        {
                            await CopyFileAsync(sourceDir, replicaFilePath);
                        }
                    }
                }

            }
            await Logger.LogAsync("Copying and updating DONE.");
        }

        private async Task CopyFileAsync(string sourceDir, string replicaFilePath)
        {
            File.Copy(sourceDir, replicaFilePath, overwrite: true);
            File.SetLastWriteTimeUtc(replicaFilePath, File.GetLastWriteTimeUtc(sourceDir));
            await Logger.LogAsync($"File {Path.GetFileName(sourceDir)} was copied to path: {replicaFilePath}.");
        }

        private async Task DeleteFilesAsync(string[] replicaFolders)
        {
            await Logger.LogAsync("Deleting files and folders.");
            foreach (var replicaFolder in replicaFolders)
            {
                string replicaFileName = Path.GetRelativePath(_replicaPath, replicaFolder);
                string sourceFilePath = Path.Combine(_sourcePath, replicaFileName);
                if (!File.Exists(sourceFilePath))
                {
                    File.Delete(replicaFolder);
                    await Logger.LogAsync($"File {replicaFileName} was deleted from {replicaFolder}");
                }
            }
            await Logger.LogAsync("Deleting files DONE.");

            var foldersToDelete = replicaFolders.OrderByDescending(f => f.Length);

            foreach (var folder in foldersToDelete)
            {
                string replicaFolderName = Path.GetRelativePath(_replicaPath, folder);
                string sourceFolderPath = Path.Combine(_sourcePath, replicaFolderName);
                if (!Directory.Exists(sourceFolderPath))
                {
                    Directory.Delete(folder, recursive: true);
                    await Logger.LogAsync($"Folder {replicaFolderName} was deleted from {sourceFolderPath}");
                }
            }
            await Logger.LogAsync("Deleting folders DONE.");
        }

        private async Task<byte[]> ComputeHashFile(string path)
        {
            return await Task.Run(() =>
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(path))
                {
                    return md5.ComputeHash(stream);
                }
            });
        }

        private async Task<byte[]> ComputeHashFolders(string[] folderStructure, string[] fileStructure, string folderPath)
        {
            var stringBuilder = new StringBuilder();
            var sortedFolders = folderStructure.Select(p => Path.GetRelativePath(folderPath, p))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
            var sortedFiles = fileStructure.Select(p => Path.GetRelativePath(folderPath, p))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var folder in sortedFolders)
            {
                stringBuilder.Append("Folder|").AppendLine(folder);
            }
            foreach (var file in sortedFiles)
            {
                stringBuilder.Append("File|").AppendLine(file);
            }

            using var md5 = MD5.Create();
            return md5.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        }

        private async Task<bool> CompareFolderStructureHashAsync(string[] sourceFolderStructure, string[] sourceFileStructure, string[] replicaFolderStructure, string[] repliceFileStructure)
        {
            var sourceHash = await ComputeHashFolders(sourceFolderStructure, sourceFileStructure, _sourcePath);
            var replicaHash = await ComputeHashFolders(replicaFolderStructure, repliceFileStructure, _replicaPath);
            if (!sourceHash.SequenceEqual(replicaHash))
            {
                await Logger.LogAsync("Folder structures are different.");
                return false;
            }
            return true;
        }

        private async Task<string> CheckIfFolderExistsAsync(string path)
        {
            await Logger.LogAsync($"Checking if folder exists: {Path.GetFileName(path)}");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                await Logger.LogAsync($"Folder created: {path}");
            }
            else
            {
                await Logger.LogAsync("Folder exists.");
            }
            return path;
        }
    }
}

