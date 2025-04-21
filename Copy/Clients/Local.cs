using Copy.Types;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    /// <summary>
    /// Local file system client.
    /// </summary>
    internal class Local : IClient
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;

        public Client Config => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Local(Client credentials)
        {
            _credentials = credentials;
        }

        public bool DoFileExist(string path)
        {
            return File.Exists(path);
        }

        #region Native Methods
        [StructLayout(LayoutKind.Sequential)]
        private struct Passwd
        {
            public IntPtr pw_name;   // Username
            public IntPtr pw_passwd; // Password
            public uint pw_uid;     // User ID
            public uint pw_gid;     // Group ID
            public IntPtr pw_gecos;  // User Info
            public IntPtr pw_dir;    // Home Directory
            public IntPtr pw_shell;  // Shell
        }

        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr getpwuid(uint uid);

        [DllImport("libc", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int stat(string path, out Stat buf);

        [StructLayout(LayoutKind.Sequential)]
        private struct Stat
        {
            public uint st_dev;
            public uint st_ino;
            public uint st_mode;
            public uint st_nlink;
            public uint st_uid;
            public uint st_gid;
            public uint st_rdev;
            public long st_size;
            public long st_atime;
            public long st_mtime;
            public long st_ctime;
            public uint st_blksize;
            public uint st_blocks;
        }
        #endregion

        private static string GetUnixFileOwner(string filePath)
        {
            try
            {
                if (stat(filePath, out Stat statBuf) != 0)
                {
                    throw new FileOwnerNotFoundException($"Impossible to get owner of {filePath}");
                }

                IntPtr passwd = getpwuid(statBuf.st_uid);
                if (passwd == IntPtr.Zero)
                {
                    return statBuf.st_uid.ToString();
                }

                var pwStruct = Marshal.PtrToStructure<Passwd>(passwd);
                return Marshal.PtrToStringAnsi(pwStruct.pw_name) ?? statBuf.st_uid.ToString();
            }
            catch (DllNotFoundException)
            {
                return Environment.UserName;
            }
        }

        private static string GetFileOwner(string filePath)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.GetAccessControl().GetOwner(typeof(NTAccount))?.Value 
                        ?? throw new FileOwnerNotFoundException($"Impossible to get owner of {filePath}");
                }
                catch (PlatformNotSupportedException)
                {
                    return Environment.UserName;
                }
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return GetUnixFileOwner(filePath);
            }
            else
            {
                return Environment.UserName;
            }
        }

        public string[] ListFiles(string path, CopyFilter filter)
        {
            if (!Directory.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);

            return [.. Directory.GetFiles(path)
                .Where(f =>
                {
                    FileInfo fileInfo = new(f);
                    bool authorMatch = authorRegex.IsMatch(GetFileOwner(f));
                    return nameRegex.IsMatch(Path.GetFileName(f)) && 
                           authorMatch && 
                           File.GetCreationTime(f) >= filter.CreatedAfter && 
                           (ulong)fileInfo.Length <= filter.MaxSize && 
                           (ulong)fileInfo.Length >= filter.MinSize;
                })];
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path))
            {
                Logger.Error($"File {path} does not exist");
                throw new FileNotFoundException($"File {path} does not exist");
            }

            return File.OpenRead(path);
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            File.Move(sourcePath, destinationPath);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            File.Copy(sourcePath, destinationPath, true);
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            File.Delete(path);
        }

        public void Dispose()
        {
            // No resources to dispose in this class
        }
    }
}
