Program that synchronizes one-way, periodically folder source to folder replica

# Example of windows run in terminal
FolderSync.exe "C:\Users\FolderSync\source" "C:\Users\FolderSync\replica" 15 "C:\Users\FolderSync\log.txt"

# Notes
- source folder, replica folder & log file are created if missing, and has default paths
- Stop program with CTRL + C

# What it does:
1. Compute fast hash for both folders. If folders are equal, skip synchornization.
2. Take a snapshot of folders and files.
3. Mirror missing directories.
4. Create/Update files.
5. Delete files/folders not present in source.
6. Log every action to console and file.

