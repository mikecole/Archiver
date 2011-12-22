using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Ionic.Zip;
using Ionic.Zlib;

namespace Archiver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Directory.Exists(ArchiveDirectoryFromConfig))
                {
                    Console.WriteLine("Archiving...");

                    foreach (var path in args)
                    {
                        Archive(path);
                    }

                    if (DeleteAfterDays.HasValue && DeleteAfterDays.Value >= 0)
                    {
                        PurgeOldItems();
                    }

                    Console.WriteLine(String.Format("Archiver Version: {0}", Application.ProductVersion));
                    Console.WriteLine("Archive complete! Press any key to continue...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine(String.Format("Archive directory {0} does not exist. Press any key to continue...", ArchiveDirectoryFromConfig));
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("No archive source specified. Press any key to continue...");
                Console.ReadKey();
            }
        }
        static void ZipProgress_Reported(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
            {
                Console.WriteLine("Archived {0}", e.CurrentEntry.FileName);
            }
        }
        private static void Archive(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                ArchiveDirectory(path);
            }
            else
            {
                ArchiveFile(path);
            }
        }
        private static void ArchiveFile(string path)
        {
            var file = new FileInfo(path);
            var destinationPath = Path.Combine(ArchiveDirectoryFromConfig, String.Concat(Path.GetFileNameWithoutExtension(file.Name), "_", GetFormattedDate(), "_", Environment.UserName, ".zip"));

            using (var zip = new ZipFile())
            {
                zip.SaveProgress += ZipProgress_Reported;
                zip.CompressionLevel = CompressionLevel.BestCompression;
                zip.AddFile(file.FullName, String.Empty);
                zip.Save(destinationPath);
            }
        }
        private static void ArchiveDirectory(string path)
        {
            var directory = new DirectoryInfo(path);
            var destinationPath = Path.Combine(ArchiveDirectoryFromConfig, String.Concat(directory.Name, "_", GetFormattedDate(), "_", Environment.UserName, ".zip"));

            using (var zip = new ZipFile())
            {
                zip.SaveProgress += ZipProgress_Reported;
                zip.CompressionLevel = CompressionLevel.BestCompression;
                zip.AddDirectory(directory.FullName, String.Empty);
                zip.Save(destinationPath);
            }
        }
        private static void PurgeOldItems()
        {
            var oldFiles = from f in new DirectoryInfo(ArchiveDirectoryFromConfig).GetFiles()
                           where f.CreationTime < DateTime.Now.Subtract(TimeSpan.FromDays(DeleteAfterDays.Value))
                           select f;

            oldFiles.ToList().ForEach(DeleteOldFile);
        }
        private static string GetFormattedDate()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        }
        private static void DeleteOldFile(FileInfo file)
        {
            Console.WriteLine("Purged {0}", file.Name);
            file.Delete();
        }

        private static string ArchiveDirectoryFromConfig
        {
            get
            {
                return ConfigurationManager.AppSettings["ArchiveDirectory"];
            }
        }
        private static int? DeleteAfterDays
        {
            get
            {
                int? result = null;

                if (ConfigurationManager.AppSettings.AllKeys.Contains("DeleteAfterDays"))
                {
                    result = Int32.Parse(ConfigurationManager.AppSettings["DeleteAfterDays"]);
                }

                return result;
            }
        }
    }
}