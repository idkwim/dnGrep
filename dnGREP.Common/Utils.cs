using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace dnGREP.Common
{
    public static class Utils
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static Utils()
        {
            ArchiveExtensions = new List<string>();
        }

        /// <summary>
        /// Copies the folder recursively. Uses includePattern to avoid unnecessary objects
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="includePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
        /// <param name="excludePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
        public static void CopyFiles(string sourceDirectory, string destinationDirectory, string includePattern, string excludePattern)
        {
            String[] files;

            destinationDirectory = FixFolderName(destinationDirectory);

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            files = Directory.GetFileSystemEntries(sourceDirectory);

            foreach (string element in files)
            {
                if (!string.IsNullOrEmpty(includePattern) && File.Exists(element) && !Regex.IsMatch(element, includePattern))
                    continue;

                if (!string.IsNullOrEmpty(excludePattern) && File.Exists(element) && Regex.IsMatch(element, excludePattern))
                    continue;

                // Sub directories
                if (Directory.Exists(element))
                    CopyFiles(element, destinationDirectory + Path.GetFileName(element), includePattern, excludePattern);
                // Files in directory
                else
                    CopyFile(element, destinationDirectory + Path.GetFileName(element), true);
            }
        }

        /// <summary>
        /// Copies file based on search results. If folder does not exist, creates it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="overWrite"></param>
        public static void CopyFiles(List<GrepSearchResult> source, string sourceDirectory, string destinationDirectory, bool overWrite)
        {
            sourceDirectory = FixFolderName(sourceDirectory);
            destinationDirectory = FixFolderName(destinationDirectory);

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            List<string> files = new List<string>();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal) && result.FileNameDisplayed.Contains(sourceDirectory))
                {
                    files.Add(result.FileNameReal);
                    FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
                    FileInfo destinationFileInfo = new FileInfo(destinationDirectory + result.FileNameReal.Substring(sourceDirectory.Length));
                    if (sourceFileInfo.FullName != destinationFileInfo.FullName)
                        CopyFile(sourceFileInfo.FullName, destinationFileInfo.FullName, overWrite);
                }
            }
        }

        /// <summary>
        /// Returns true if destinationDirectory is not included in source files
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationDirectory"></param>
        /// <returns></returns>
        public static bool CanCopyFiles(List<GrepSearchResult> source, string destinationDirectory)
        {
            if (destinationDirectory == null || source == null || source.Count == 0)
                return false;

            destinationDirectory = FixFolderName(destinationDirectory);

            List<string> files = new List<string>();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);
                    FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
                    FileInfo destinationFileInfo = new FileInfo(destinationDirectory + Path.GetFileName(result.FileNameReal));
                    if (sourceFileInfo.FullName == destinationFileInfo.FullName)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a CSV file from search results
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationPath"></param>
        public static void SaveResultsAsCSV(List<GrepSearchResult> source, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.WriteAllText(destinationPath, GetResultsAsCSV(source), Encoding.UTF8);
        }

        /// <summary>
        /// Creates a text file from search results
        /// </summary>
        /// <param name="source">the search results</param>
        /// <param name="destinationPath">the file name to save</param>
        public static void SaveResultsAsText(List<GrepSearchResult> source, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.WriteAllText(destinationPath, GetResultLines(source), Encoding.UTF8);
        }

        public static void SaveResultsReport(List<GrepSearchResult> source, string options, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            int fileCount = source.Where(r => !string.IsNullOrWhiteSpace(r.FileNameReal)).Select(r => r.FileNameReal).Distinct().Count();
            int lineCount = source.Sum(s => s.Matches.Where(r => r.LineNumber > 0).Select(r => r.LineNumber).Distinct().Count());
            int matchCount = source.Sum(s => s.Matches == null ? 0 : s.Matches.Count);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("dnGrep Search Results").AppendLine();
            sb.Append(options).AppendLine();
            sb.AppendFormat("Found {0} matches on {1} lines in {2} files",
                matchCount.ToString("#,##0"), lineCount.ToString("#,##0"), fileCount.ToString("#,##0"))
                .AppendLine().AppendLine();
            sb.Append(GetResultLinesWithContext(source));

            File.WriteAllText(destinationPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Returns a CSV structure from search results
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationPath"></param>
        public static string GetResultsAsCSV(List<GrepSearchResult> source)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("File Name,Line Number,String");
            foreach (GrepSearchResult result in source)
            {
                if (result.SearchResults == null)
                {
                    sb.AppendLine("\"" + result.FileNameDisplayed + "\"");
                }
                else
                {
                    foreach (GrepSearchResult.GrepLine line in result.SearchResults)
                    {
                        if (!line.IsContext)
                            sb.AppendLine("\"" + result.FileNameDisplayed + "\"," + line.LineNumber + ",\"" + line.LineText.Replace("\"", "\"\"") + "\"");
                    }
                }
            }
            return sb.ToString();
        }

        public static string GetResultLines(List<GrepSearchResult> source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (GrepSearchResult result in source)
            {
                if (result.SearchResults != null)
                {
                    foreach (GrepSearchResult.GrepLine line in result.SearchResults)
                    {
                        if (!line.IsContext)
                            sb.AppendLine(line.LineText);
                    }
                }
            }
            return sb.ToString();
        }

        public static string GetResultLinesWithContext(List<GrepSearchResult> source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var result in source)
            {
                // this call to SearchResults can be expensive if the results are not yet cached
                var searchResults = result.SearchResults;
                if (searchResults != null)
                {
                    int matchCount = (result.Matches == null ? 0 : result.Matches.Count);
                    var lineCount = result.Matches.Where(r => r.LineNumber > 0)
                        .Select(r => r.LineNumber).Distinct().Count();

                    sb.AppendLine(result.FileNameDisplayed)
                      .AppendFormat("has {0} matches on {1} lines:", matchCount, lineCount).AppendLine();

                    if (searchResults.Any())
                    {
                        int prevLineNum = -1;
                        foreach (var line in searchResults)
                        {
                            // Adding separator
                            if (line.LineNumber != prevLineNum + 1)
                                sb.AppendLine();

                            sb.Append(line.LineNumber.ToString().PadLeft(6, ' ')).Append(":  ").AppendLine(line.LineText);
                            prevLineNum = line.LineNumber;
                        }
                    }
                    else
                    {
                        sb.AppendLine("[File not found: has it been deleted or moved?]");
                    }
                }
                sb.AppendLine("--------------------------------------------------------------------------------").AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deletes file based on search results. 
        /// </summary>
        /// <param name="source"></param>
        public static void DeleteFiles(List<GrepSearchResult> source)
        {
            List<string> files = new List<string>();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);
                    DeleteFile(result.FileNameReal);
                }
            }
        }

        /// <summary>
        /// Copies file. If folder does not exist, creates it.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="overWrite"></param>
        public static void CopyFile(string sourcePath, string destinationPath, bool overWrite)
        {
            if (File.Exists(destinationPath) && !overWrite)
                throw new IOException("File: '" + destinationPath + "' exists.");

            if (!new FileInfo(destinationPath).Directory.Exists)
                new FileInfo(destinationPath).Directory.Create();

            File.Copy(sourcePath, destinationPath, overWrite);
        }

        /// <summary>
        /// Deletes files even if they are read only
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        /// <summary>
        /// Deletes folder even if it contains read only files
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFolder(string path)
        {
            string[] files = GetFileList(path, "*.*", null, false, true, true, true, false, 0, 0, FileDateFilter.None, null, null);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Detects the byte order mark of a file and returns
        /// an appropriate encoding for the file.
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static Encoding GetFileEncoding(string srcFile)
        {
            // TODO: Unit tests. At least a regression test for Google Code issue 204. In order to properly unit test this method, we should decouple it from the file system by passing in an object that can be easily faked/mocked (a Stream object?).
            using (FileStream readStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var detector = new Ude.CharsetDetector();
                detector.Feed(readStream);
                detector.DataEnd();
                return DotNetEncodingFromUde(detector.Charset) ?? Encoding.Default; // If we detected an encoding, use it, otherwise use default.
            }
        }

        /// <summary>
        /// Maps a Ude charset to a System.Text.Encoding.
        /// </summary>
        /// <returns>
        /// The System.Text.Encoding for the given Ude CharsetDetector.Charset, or null if not found in the map.
        /// </returns>
        private static Encoding DotNetEncodingFromUde(string udeCharset)
        {
            if (udeCharset == null)
                return null;

            // Ude to .NET encoding name mapping. We should update this if ever both support new encodings.
            // I got this list by comparing Ude.Core\Charsets.cs with the table shown in the System.Text.Encoding
            // docs at http://msdn.microsoft.com/en-us/library/vstudio/system.text.encoding(v=vs.100).aspx
            // Note that it's a many-to-one mapping, in some cases, like (UTF-16LE, UTF-16BE) => utf-16.
            var udeToDotNet = new Dictionary<string, string>()
                                   {{"Big-5", "big5"},
                                    {"EUC-JP", "euc-jp"},
                                    {"EUC-KR", "euc-kr"},
                                    {"gb18030", "GB18030"},
                                    {"HZ-GB-2312", "hz-gb-2312"},
                                    {"IBM855", "IBM855"},
                                    {"ISO-2022-JP", "iso-2022-jp"},
                                    {"ISO-2022-KR", "iso-2022-kr"},
                                    {"ISO-8859-2", "iso-8859-2"},
                                    {"ISO-8859-5", "iso-8859-5"},
                                    {"ISO-8859-7", "iso-8859-7"},
                                    {"ISO-8859-8", "iso-8859-8"},
                                    {"KOI8-R", "koi8-r"},
                                    {"Shift-JIS", "shift_jis"},
                                    {"ASCII", "us-ascii"},
                                    {"UTF-16LE", "utf-16"},
                                    {"UTF-16BE", "utf-16"},
                                    {"UTF-32BE", "utf-32"},
                                    {"UTF-32LE", "utf-32"},
                                    {"UTF-8", "utf-8"},
                                    {"windows-1251", "windows-1251"},
                                    {"windows-1252", "Windows-1252"},
                                    {"windows-1253", "windows-1253"},
                                    {"windows-1255", "windows-1255"},
                                    {"x-mac-cyrillic", "x-mac-cyrillic"}};

            if (udeToDotNet.ContainsKey(udeCharset))
                return Encoding.GetEncoding(udeToDotNet[udeCharset]);
            else
                return null;
        }

        /// <summary>
        /// Returns true is file is binary. Algorithm taken from winGrep.
        /// The function scans first 10KB for 0x0000 sequence
        /// and if found, assumes the file to be binary
        /// </summary>
        /// <param name="filePath">Path to a file</param>
        /// <returns>True is file is binary otherwise false</returns>
        public static bool IsBinary(string srcFile)
        {
            using (FileStream readStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return IsBinary(readStream);
            }
        }

        public static bool IsBinary(Stream stream)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int count = stream.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < count - 1; i = i + 2)
                {
                    if (buffer[i] == 0 && buffer[i + 1] == 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the source file extension is ".pdf"
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsPdfFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Equals(".PDF", StringComparison.CurrentCultureIgnoreCase))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is ".doc" or ".docx"
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsWordFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) && 
                (ext.Equals(".DOC", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".DOCX", StringComparison.CurrentCultureIgnoreCase)))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is a recognized archive file
        /// </summary>
        /// <param name="srcFile">a file name</param>
        /// <returns></returns>
        public static bool IsArchive(string srcFile)
        {
            if (!string.IsNullOrWhiteSpace(srcFile))
            {
                return IsArchiveExtension(Path.GetExtension(srcFile));
            }
            return false;
        }

        /// <summary>
        /// Returns true if the parameter is a recognized archive file format file extension.
        /// </summary>
        /// <param name="ext">a file extension, with/without a leading '.'</param>
        /// <returns></returns>
        public static bool IsArchiveExtension(string ext)
        {
            if (!string.IsNullOrWhiteSpace(ext))
            {
                // regex extensions may have a 'match end of line' char: remove it
                ext = ext.TrimStart('.').TrimEnd('$').ToLower();
                return ArchiveExtensions.Contains(ext);
            }
            return false;
        }

        /// <summary>
        /// Gets or set the list of archive extensions (lowercase, without leading '.')
        /// </summary>
        public static List<string> ArchiveExtensions { get; set; }

        /// <summary>
        /// Add DirectorySeparatorChar to the end of the folder path if does not exist
        /// </summary>
        /// <param name="name">Folder path</param>
        /// <returns></returns>
        public static string FixFolderName(string name)
        {
            if (name != null && name.Length > 1 && name[name.Length - 1] != Path.DirectorySeparatorChar)
                name += Path.DirectorySeparatorChar;
            return name;
        }

        /// <summary>
        /// Validates whether the path is a valid directory, file, or list of files
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <returns>True is all paths are valid, otherwise false</returns>
        public static bool IsPathValid(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                string[] paths = SplitPath(path);
                foreach (string subPath in paths)
                {
                    if (subPath.Trim() != "" && !File.Exists(subPath) && !Directory.Exists(subPath))
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes whitespace from the individual paths
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CleanPath(string path)
        {
            string result = path;
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    List<string> items = new List<string>();
                    string[] paths = SplitPath(path);
                    foreach (string subPath in paths)
                    {
                        string p = subPath.Trim();
                        if (!string.IsNullOrWhiteSpace(p) && (File.Exists(p) || Directory.Exists(p)))
                            items.Add(p);
                    }

                    result = string.Join(";", items.ToArray());
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
            }

            return result;
        }

        /// <summary>
        /// Returns base folder of one or many files or folders. 
        /// If multiple files are passed in, takes the first one.
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <returns>Base folder path or null if none exists</returns>
        public static string GetBaseFolder(string path)
        {
            try
            {
                string[] paths = SplitPath(path);
                if (paths[0].Trim() != "" && File.Exists(paths[0]))
                    return Path.GetDirectoryName(paths[0]);
                else if (paths[0].Trim() != "" && Directory.Exists(paths[0]))
                    return paths[0];
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Splits path into subpaths if ; or , are found in path.
        /// If folder name contains ; or , returns as one path
        /// </summary>
        /// <param name="path">Path to split</param>
        /// <returns>Array of strings. If path is null, returns null. If path is empty, returns empty array.</returns>
        public static string[] SplitPath(string path)
        {
            if (path == null)
                return new string[0];
            else if (path.Trim() == "")
                return new string[0];

            List<string> output = new List<string>();
            string[] paths = path.Split(';', ',');
            int splitterIndex = -1;
            for (int i = 0; i < paths.Length; i++)
            {
                splitterIndex += paths[i].Length + 1;
                string splitter = (splitterIndex < path.Length ? path[splitterIndex].ToString() : "");
                StringBuilder sb = new StringBuilder();
                if (File.Exists(paths[i]) || Directory.Exists(paths[i]))
                    output.Add(paths[i]);
                else
                {
                    int subSplitterIndex = 0;
                    bool found = false;
                    sb.Append(paths[i] + splitter);
                    for (int j = i + 1; j < paths.Length; j++)
                    {
                        subSplitterIndex += paths[j].Length + 1;
                        sb.Append(paths[j]);
                        if (File.Exists(sb.ToString()) || Directory.Exists(sb.ToString()))
                        {
                            output.Add(sb.ToString());
                            splitterIndex += subSplitterIndex;
                            i = j;
                            found = true;
                            break;
                        }
                        sb.Append(splitterIndex + subSplitterIndex < path.Length ? path[splitterIndex + subSplitterIndex].ToString() : "");
                    }
                    if (!found && !string.IsNullOrWhiteSpace(paths[i]))
                        output.Add(paths[i].TrimStart());
                }
            }
            return output.ToArray();
        }

        public static bool CancelSearch = false;

        public static void PrepareFilters(FileFilter filter, List<Regex> includeRegexPatterns, List<Regex> excludeRegexPatterns)
        {
            if (includeRegexPatterns == null || excludeRegexPatterns == null)
                return;

            var includePatterns = SplitPath(filter.NamePatternToInclude);
            var excludePatterns = SplitPath(filter.NamePatternToExclude);
            if (!filter.IsRegex)
            {
                foreach (var pattern in includePatterns)
                    includeRegexPatterns.Add(new Regex(wildcardToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase));

                foreach (var pattern in excludePatterns)
                    excludeRegexPatterns.Add(new Regex(wildcardToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            else
            {
                foreach (var pattern in includePatterns)
                    includeRegexPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

                foreach (var pattern in excludePatterns)
                    excludeRegexPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }

        /// <summary>
        /// Iterator based file search
        /// Searches folder and it's subfolders for files that match pattern and
        /// returns array of strings that contain full paths to the files.
        /// If no files found returns 0 length array.
        /// </summary>
        /// <param name="filter">the file filter parameters</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFileListEx(FileFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Path) || filter.NamePatternToInclude == null)
            {
                yield break;
            }

            // Hash set to ensure file name uniqueness
            HashSet<string> matches = new HashSet<string>();

            var includeRegexPatterns = new List<Regex>();
            var excludeRegexPatterns = new List<Regex>();
            PrepareFilters(filter, includeRegexPatterns, excludeRegexPatterns);

            foreach (var subPath in SplitPath(filter.Path))
            {
                if (File.Exists(subPath))
                {
                    if (!matches.Contains(subPath))
                    {
                        matches.Add(subPath);
                        yield return subPath;
                    }
                    continue;
                }
                else if (!Directory.Exists(subPath))
                {
                    continue;
                }
                foreach (var dirPath in (!filter.IncludeSubfolders ? new string[] { subPath }.AsEnumerable() :
                    new string[] { subPath }.AsEnumerable().Concat(Directory.EnumerateDirectories(subPath, "*", SearchOption.AllDirectories))))
                {
                    DirectoryInfo dirInfo = null;
                    if (!filter.IncludeHidden)
                    {
                        if (dirInfo == null)
                            dirInfo = new DirectoryInfo(dirPath);
                        if (dirInfo.Root.Name != dirInfo.Name && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;
                    }
                    if (!hasListPermissionOnDir(dirPath))
                        continue;
                    foreach (var filePath in Directory.EnumerateFiles(dirPath))
                    {
                        bool excludeMatch = false;
                        bool includeMatch = false;
                        FileInfo fileInfo = null;
                        try
                        {
                            if (!filter.IncludeHidden)
                            {
                                if (fileInfo == null)
                                    fileInfo = new FileInfo(filePath);
                                if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                    continue;
                            }

                            if (!filter.IncludeArchive && IsArchive(filePath))
                                continue;

                            if (!IsArchive(filePath) && !filter.IncludeBinary && IsBinary(filePath))
                                continue;

                            if (filter.SizeFrom > 0 || filter.SizeTo > 0)
                            {
                                if (fileInfo == null)
                                    fileInfo = new FileInfo(filePath);

                                long sizeKB = fileInfo.Length / 1000;
                                if (filter.SizeFrom > 0 && sizeKB < filter.SizeFrom)
                                {
                                    continue;
                                }
                                if (filter.SizeTo > 0 && sizeKB > filter.SizeTo)
                                {
                                    continue;
                                }
                            }
                            if (filter.DateFilter != FileDateFilter.None)
                            {
                                if (fileInfo == null)
                                    fileInfo = new FileInfo(filePath);

                                DateTime fileDate = filter.DateFilter == FileDateFilter.Created ? fileInfo.CreationTime : fileInfo.LastWriteTime;
                                if (filter.StartTime.HasValue && fileDate < filter.StartTime.Value)
                                {
                                    continue;
                                }
                                if (filter.EndTime.HasValue && fileDate >= filter.EndTime.Value)
                                {
                                    continue;
                                }
                            }
                            if (filter.IncludeArchive && IsArchive(filePath))
                            {
                                includeMatch = true;
                            }
                            if (!includeMatch)
                            {
                                foreach (var pattern in includeRegexPatterns)
                                {
                                    if (pattern.IsMatch(filePath) || CheckShebang(filePath, pattern.ToString()))
                                    {
                                        includeMatch = true;
                                        break;
                                    }
                                }
                            }
                            foreach (var pattern in excludeRegexPatterns)
                            {
                                if (pattern.IsMatch(filePath) || CheckShebang(filePath, pattern.ToString()))
                                {
                                    excludeMatch = true;
                                    break;
                                }
                            }
                            if (excludeMatch || !includeMatch)
                                continue;
                        }
                        catch (Exception ex)
                        {
                            logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                        }

                        if (!matches.Contains(filePath))
                        {
                            matches.Add(filePath);
                            yield return filePath;
                        }
                    }
                }
            }
        }

        public static bool CheckShebang(string file, string pattern)
        {
            if (pattern == null || pattern.Length <= 2 || (pattern[0] != '#' && pattern[1] != '!'))
                return false;
            try
            {
                using (FileStream readStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader streamReader = new StreamReader(readStream))
                {
                    string firstLine = streamReader.ReadLine();
                    // Check if first 2 bytes are '#!'
                    if (firstLine[0] == 0x23 && firstLine[1] == 0x21)
                    {
                        // Do more reading (start from 3rd character in case there is a space after #!)
                        for (int i = 3; i < firstLine.Length; i++)
                        {
                            if (firstLine[i] == ' ' || firstLine[i] == '\r' || firstLine[i] == '\n' || firstLine[i] == '\t')
                            {
                                firstLine = firstLine.Substring(0, i);
                                break;
                            }
                        }
                        return Regex.IsMatch(firstLine.Substring(2).Trim(), pattern.Substring(2), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Does not have shebang
                        return false;
                    }
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static bool hasListPermissionOnDir(string dirPath)
        {
            try
            {
                foreach (string path in Directory.EnumerateFiles(dirPath))
                {
                    break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Searches folder and it's subfolders for files that match pattern and
        /// returns array of strings that contain full paths to the files.
        /// If no files found returns 0 length array.
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <param name="namePatternToInclude">File name pattern. (E.g. *.cs) or regex to include. If null returns empty array. If empty string returns all files.</param>
        /// <param name="namePatternToExclude">File name pattern. (E.g. *.cs) or regex to exclude. If null or empty is ignored.</param>
        /// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
        /// <param name="includeSubfolders">Include sub folders</param>
        /// <param name="includeHidden">Include hidden folders</param>
        /// <param name="includeBinary">Include binary files</param>
        /// <param name="includeArchive">Include search in archives</param>
        /// <param name="sizeFrom">Size in KB</param>
        /// <param name="sizeTo">Size in KB</param>
        /// <param name="dateFilter">Filter by file modified or created date time range</param>
        /// <param name="startTime">start of time range</param>
        /// <param name="endTime">end of time range</param>
        /// <returns>List of file or empty list if nothing is found</returns>
        public static string[] GetFileList(string path, string namePatternToInclude, string namePatternToExclude, bool isRegex,
            bool includeSubfolders, bool includeHidden, bool includeBinary, bool includeArchive, int sizeFrom, int sizeTo,
            FileDateFilter dateFilter, DateTime? startTime, DateTime? endTime)
        {
            var filter = new FileFilter(path, namePatternToInclude, namePatternToExclude, isRegex,
                includeSubfolders, includeHidden, includeBinary, includeArchive, sizeFrom, sizeTo, dateFilter, startTime, endTime);
            return GetFileListEx(filter).ToArray();
        }

        /// <summary>
        /// Converts unix asterisk based file pattern to regex
        /// </summary>
        /// <param name="wildcard">Asterisk based pattern</param>
        /// <returns>Regular expression of null is empty</returns>
        public static string wildcardToRegex(string wildcard)
        {
            if (wildcard == null || wildcard == "") return wildcard;

            StringBuilder sb = new StringBuilder();

            char[] chars = wildcard.ToCharArray();
            for (int i = 0; i < chars.Length; ++i)
            {
                if (chars[i] == '*')
                    sb.Append(".*");
                else if (chars[i] == '?')
                    sb.Append(".");
                else if ("+()^$.{}|\\".IndexOf(chars[i]) != -1)
                    sb.Append('\\').Append(chars[i]); // prefix all metacharacters with backslash
                else
                    sb.Append(chars[i]);
            }
            sb.Append("$");
            return sb.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Parses text into int
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <returns>Attempts to parse string. Otherwise returns int.MinValue</returns>
        public static int ParseInt(string value)
        {
            return ParseInt(value, int.MinValue);
        }

        /// <summary>
        /// Parses text into int
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <param name="defaultValue">Default value if fails to parse.</param>
        /// <returns>Attempts to parse string. Otherwise returns defaultValue</returns>
        public static int ParseInt(string value, int defaultValue)
        {
            if (value != null && value.Length != 0)
            {
                int output;
                value = value.Trim();
                if (int.TryParse(value, out output))
                {
                    return output;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Parses text into double
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <returns>Attempts to parse string. Otherwise returns double.MinValue</returns>
        public static double ParseDouble(string value)
        {
            return ParseDouble(value, double.MinValue);
        }

        /// <summary>
        /// Parses text into double
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <param name="defaultValue">Default value if fails to parse.</param>
        /// <returns>Attempts to parse string. Otherwise returns defaultValue</returns>
        public static double ParseDouble(string value, double defaultValue)
        {
            if (value != null && value.Length != 0)
            {
                double output;
                value = value.Trim();
                if (double.TryParse(value, out output))
                {
                    return output;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Parses text into bool
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.
        /// Text may be in the format of True/False, Yes/No, Y/N, On/Off, 1/0</param>
        /// <returns></returns>
        public static bool ParseBoolean(string value)
        {
            return ParseBoolean(value, false);
        }

        /// <summary>
        /// Parses text into bool
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.
        /// Text may be in the format of True/False, Yes/No, Y/N, On/Off, 1/0</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns></returns>
        public static bool ParseBoolean(string value, bool defaultValue)
        {
            if (value != null && value.Length != 0)
            {
                switch (value.Trim().ToLower())
                {
                    case "true":
                    case "yes":
                    case "y":
                    case "on":
                    case "1":
                        return true;
                    case "false":
                    case "no":
                    case "n":
                    case "off":
                    case "0":
                        return false;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Parses text into enum
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="value">Value to parse</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns></returns>
        public static T ParseEnum<T>(string value, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            T result = defaultValue;
            try
            {
                // Check if enum is nullable
                var enumType = Nullable.GetUnderlyingType(typeof(T));
                if (enumType == null)
                    result = (T)Enum.Parse(typeof(T), value);
                else
                    result = (T)Enum.Parse(enumType, value);
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Open file using either default editor or the one provided via customEditor parameter
        /// </summary>
        /// <param name="fileName">File to open</param>
        /// <param name="line">Line number</param>
        /// <param name="useCustomEditor">True if customEditor parameter is provided</param>
        /// <param name="customEditor">Custom editor path</param>
        /// <param name="customEditorArgs">Arguments for custom editor</param>
        public static void OpenFile(OpenFileArgs args)
        {
            if (!args.UseCustomEditor || args.CustomEditor == null || args.CustomEditor.Trim() == "")
            {
                try
                {
                    System.Diagnostics.Process.Start(@"" + args.SearchResult.FileNameDisplayed + "");
                }
                catch
                {
                    ProcessStartInfo info = new ProcessStartInfo("notepad.exe");
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    info.Arguments = args.SearchResult.FileNameDisplayed;
                    System.Diagnostics.Process.Start(info);
                }
            }
            else
            {
                ProcessStartInfo info = new ProcessStartInfo(args.CustomEditor);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                if (args.CustomEditorArgs == null)
                    args.CustomEditorArgs = "";
                info.Arguments = args.CustomEditorArgs.Replace("%file", "\"" + args.SearchResult.FileNameDisplayed + "\"")
                    .Replace("%line", args.LineNumber.ToString())
                    .Replace("%pattern", args.Pattern);
                System.Diagnostics.Process.Start(info);
            }
        }

        /// <summary>
        /// Returns path to a temp folder used by dnGREP (including trailing slash). If folder does not exist
        /// it gets created.
        /// </summary>
        /// <returns></returns>
        public static string GetTempFolder()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "~dnGREP-Temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            return tempPath + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Deletes temp folder
        /// </summary>
        public static void DeleteTempFolder()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "~dnGREP-Temp");
            try
            {
                if (Directory.Exists(tempPath))
                    DeleteFolder(tempPath);
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to delete temp folder", ex);
            }
        }

        /// <summary>
        /// Open folder in explorer
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenContainingFolder(string fileName)
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + fileName + "\"");
        }

        /// <summary>
        /// Returns current path of DLL without trailing slash
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentPath()
        {
            return GetCurrentPath(typeof(Utils));
        }

        private static bool? canUseCurrentFolder = null;
        /// <summary>
        /// Returns path to folder where user has write access to. Either current folder or user APP_DATA.
        /// </summary>
        /// <returns></returns>
        public static string GetDataFolderPath()
        {
            string currentFolder = GetCurrentPath(typeof(Utils));
            if (canUseCurrentFolder == null)
            {
                canUseCurrentFolder = hasWriteAccessToFolder(currentFolder);
            }

            if (canUseCurrentFolder == true)
            {
                return currentFolder;
            }
            else
            {
                string dataFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\dnGREP";
                if (!Directory.Exists(dataFolder))
                    Directory.CreateDirectory(dataFolder);
                return dataFolder;
            }
        }

        private static bool hasWriteAccessToFolder(string folderPath)
        {
            string filename = FixFolderName(folderPath) + "~temp.dat";
            bool canAccess = true;
            //1. Provide early notification that the user does not have permission to write.
            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, filename);
            var permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(writePermission);
            bool isGranted = permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
            if (!isGranted)
            {
                //No permission. 
                canAccess = false;
            }


            //2. Attempt the action but handle permission changes.
            if (canAccess)
            {
                try
                {
                    using (FileStream fstream = new FileStream(filename, FileMode.Create))
                    using (TextWriter writer = new StreamWriter(fstream))
                    {
                        writer.WriteLine("sometext");
                    }
                }
                catch
                {
                    //No permission. 
                    canAccess = false;
                }
            }

            // Cleanup
            try
            {
                DeleteFile(filename);
            }
            catch
            {
                // Ignore
            }

            return canAccess;
        }


        /// <summary>
        /// Returns current path of DLL without trailing slash
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns></returns>
        public static string GetCurrentPath(Type type)
        {
            Assembly thisAssembly = Assembly.GetAssembly(type);
            return Path.GetDirectoryName(thisAssembly.Location);
        }

        /// <summary>
        /// Returns read only files
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static List<string> GetReadOnlyFiles(List<GrepSearchResult> results)
        {
            List<string> files = new List<string>();
            if (results == null || results.Count == 0)
                return files;

            foreach (GrepSearchResult result in results)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    if (IsReadOnly(result))
                    {
                        files.Add(result.FileNameReal);
                    }
                }
            }
            return files;
        }

        public static bool IsReadOnly(GrepSearchResult result)
        {
            if (File.Exists(result.FileNameReal) && (File.GetAttributes(result.FileNameReal) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || result.ReadOnly)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns line and line number from a multiline string based on character index
        /// </summary>
        /// <param name="body">Multiline string</param>
        /// <param name="index">Index of any character in the line</param>
        /// <param name="lineNumber">Return parameter - 1-based line number or -1 if index is outside text length</param>
        /// <returns>Line of text or null if index is outside text length</returns>
        [Obsolete]
        public static string GetLine(string body, int index, out int lineNumber)
        {
            if (body == null || index < 0 || index > body.Length)
            {
                lineNumber = -1;
                return null;
            }

            string subBody1 = body.Substring(0, index);
            string[] lines1 = GetLines(subBody1);
            string subBody2 = body.Substring(index);
            string[] lines2 = GetLines(subBody2);
            lineNumber = lines1.Length;
            return lines1[lines1.Length - 1] + lines2[0];
        }

        /// <summary>
        /// Returns lines and line numbers from a multiline string based on character index and length
        /// </summary>
        /// <param name="body">Multiline string</param>
        /// <param name="index">Index of any character in the line</param>
        /// <param name="length">Length of a line</param>
        /// <param name="lineNumbers">Return parameter - 1-based line numbers or null if index is outside text length</param>
        /// <returns>Line of text or null if index is outside text length</returns>
        public static List<string> GetLines(string body, int index, int length, out List<GrepSearchResult.GrepMatch> matches, out List<int> lineNumbers)
        {
            List<string> result = new List<string>();
            lineNumbers = new List<int>();
            matches = new List<GrepSearchResult.GrepMatch>();
            if (body == null || index < 0 || index + 1 > body.Length || index + length + 1 > body.Length)
            {
                lineNumbers = null;
                matches = null;
                return null;
            }

            string subBody1 = body.Substring(0, index);
            string[] lines1 = GetLines(subBody1);
            string subBody2 = body.Substring(index, length);
            string[] lines2 = GetLines(subBody2);
            string subBody3 = body.Substring(index + length);
            string[] lines3 = GetLines(subBody3);
            for (int i = 0; i < lines2.Length; i++)
            {
                string line = "";
                lineNumbers.Add(lines1.Length + i);
                if (i == 0)
                {
                    if (lines2.Length == 1 && lines3.Length > 0)
                    {
                        line = lines1[lines1.Length - 1] + lines2[0] + lines3[0];
                    }
                    else
                    {
                        line = lines1[lines1.Length - 1] + lines2[0];
                    }

                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, index - subBody1.Length + lines1[lines1.Length - 1].Length, lines2[0].Length));
                }
                else if (i == lines2.Length - 1)
                {
                    if (lines3.Length > 0)
                    {
                        line = lines2[lines2.Length - 1] + lines3[0];
                    }
                    else
                    {
                        line = lines2[lines2.Length - 1];
                    }

                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, 0, lines2[lines2.Length - 1].Length));
                }
                else
                {
                    line = lines2[i];
                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, 0, lines2[i].Length));
                }
                result.Add(line);
            }

            return result;
        }

        public static string[] GetLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new string[0];
            }
            else
            {
                return text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            }
        }

        /// <summary>
        /// Retrieves lines with context based on matches
        /// </summary>
        /// <param name="body">Text</param>
        /// <param name="bodyMatchesClone">List of matches with positions relative to entire text body</param>
        /// <param name="beforeLines">Context line (before)</param>
        /// <param name="afterLines">Context line (after</param>
        /// <returns></returns>
        public static List<GrepSearchResult.GrepLine> GetLinesEx(TextReader body, List<GrepSearchResult.GrepMatch> bodyMatches, int beforeLines, int afterLines)
        {
            if (body == null || bodyMatches == null)
                return new List<GrepSearchResult.GrepLine>();

            List<GrepSearchResult.GrepMatch> bodyMatchesClone = new List<GrepSearchResult.GrepMatch>(bodyMatches);
            Dictionary<int, GrepSearchResult.GrepLine> results = new Dictionary<int, GrepSearchResult.GrepLine>();
            List<GrepSearchResult.GrepLine> contextLines = new List<GrepSearchResult.GrepLine>();
            Dictionary<int, string> lineStrings = new Dictionary<int, string>();
            List<int> lineNumbers = new List<int>();
            List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();

            // Context line (before)
            Queue<string> beforeQueue = new Queue<string>();
            // Context line (after)
            int currentAfterLine = 0;
            bool startRecordingAfterLines = false;
            // Current line
            int lineNumber = 0;
            // Current index of character
            int currentIndex = 0;
            int startIndex = 0;
            int tempLinesTotalLength = 0;
            int startLine = 0;
            bool startMatched = false;
            Queue<string> lineQueue = new Queue<string>();

            using (EolReader reader = new EolReader(body))
            {
                while (!reader.EndOfStream && (bodyMatchesClone.Count > 0 || startRecordingAfterLines))
                {
                    lineNumber++;
                    string line = reader.ReadLine();
                    bool moreMatches = true;
                    // Building context queue
                    if (beforeLines > 0)
                    {
                        if (beforeQueue.Count >= beforeLines + 1)
                            beforeQueue.Dequeue();

                        beforeQueue.Enqueue(line.TrimEndOfLine());
                    }
                    if (startRecordingAfterLines && currentAfterLine < afterLines)
                    {
                        currentAfterLine++;
                        contextLines.Add(new GrepSearchResult.GrepLine(lineNumber, line.TrimEndOfLine(), true, null));
                    }
                    else if (currentAfterLine == afterLines)
                    {
                        currentAfterLine = 0;
                        startRecordingAfterLines = false;
                    }

                    while (moreMatches && bodyMatchesClone.Count > 0)
                    {
                        // Head of match found
                        if (bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                        {
                            startMatched = true;
                            moreMatches = true;
                            lineQueue = new Queue<string>();
                            startLine = lineNumber;
                            startIndex = bodyMatchesClone[0].StartLocation - currentIndex;
                            tempLinesTotalLength = 0;
                        }

                        // Add line to queue
                        if (startMatched)
                        {
                            lineQueue.Enqueue(line);
                            tempLinesTotalLength += line.Length;
                        }

                        // Tail of match found
                        if (bodyMatchesClone[0].StartLocation + bodyMatchesClone[0].Length <= currentIndex + line.Length && startMatched)
                        {
                            startMatched = false;
                            moreMatches = false;
                            // Start creating matches
                            for (int i = startLine; i <= lineNumber; i++)
                            {
                                lineNumbers.Add(i);
                                string tempLine = lineQueue.Dequeue();
                                lineStrings[i] = tempLine;
                                // Recording context lines (before)
                                while (beforeQueue.Count > 0)
                                {
                                    // If only 1 line - it is the same as matched line
                                    if (beforeQueue.Count == 1)
                                        beforeQueue.Dequeue();
                                    else
                                        contextLines.Add(new GrepSearchResult.GrepLine(i - beforeQueue.Count + 1 + (lineNumber - startLine),
                                            beforeQueue.Dequeue(), true, null));
                                }
                                // First and only line
                                if (i == startLine && i == lineNumber)
                                    matches.Add(new GrepSearchResult.GrepMatch(i, startIndex, bodyMatchesClone[0].Length));
                                // First but not last line
                                else if (i == startLine)
                                    matches.Add(new GrepSearchResult.GrepMatch(i, startIndex, tempLine.TrimEndOfLine().Length - startIndex));
                                // Middle line
                                else if (i > startLine && i < lineNumber)
                                    matches.Add(new GrepSearchResult.GrepMatch(i, 0, tempLine.TrimEndOfLine().Length));
                                // Last line
                                else
                                    matches.Add(new GrepSearchResult.GrepMatch(i, 0, bodyMatchesClone[0].Length - tempLinesTotalLength + line.Length + startIndex));

                                startRecordingAfterLines = true;
                            }
                            bodyMatchesClone.RemoveAt(0);
                        }

                        // Another match on this line
                        if (bodyMatchesClone.Count > 0 && bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                            moreMatches = true;
                        else
                            moreMatches = false;
                    }

                    currentIndex += line.Length;
                }
            }

            if (lineStrings.Count == 0)
            {
                return new List<GrepSearchResult.GrepLine>();
            }

            // Removing duplicate lines (when more than 1 match is on the same line) and grouping all matches belonging to the same line
            for (int i = 0; i < matches.Count; i++)
            {
                addGrepMatch(results, matches[i], lineStrings[matches[i].LineNumber]);
            }
            for (int i = 0; i < contextLines.Count; i++)
            {
                if (!results.ContainsKey(contextLines[i].LineNumber))
                    results[contextLines[i].LineNumber] = contextLines[i];
            }

            return results.Values.OrderBy(l => l.LineNumber).ToList();
        }

        private static void addGrepMatch(Dictionary<int, GrepSearchResult.GrepLine> lines, GrepSearchResult.GrepMatch match, string lineText)
        {
            if (!lines.ContainsKey(match.LineNumber))
                lines[match.LineNumber] = new GrepSearchResult.GrepLine(match.LineNumber, lineText.TrimEndOfLine(), false, null);
            lines[match.LineNumber].Matches.Add(match);
        }

        /// <summary>
        /// Returns a list of context GrepLines by line numbers provided in the input parameter. Matched line is not returned.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="linesBefore"></param>
        /// <param name="linesAfter"></param>
        /// <param name="foundLine">1 based line number</param>
        /// <returns></returns>
        [Obsolete]
        public static List<GrepSearchResult.GrepLine> GetContextLines(string body, int linesBefore, int linesAfter, int foundLine)
        {
            List<GrepSearchResult.GrepLine> result = new List<GrepSearchResult.GrepLine>();
            if (body == null || body.Trim() == "")
                return result;

            List<int> lineNumbers = new List<int>();
            string[] lines = GetLines(body);
            for (int i = foundLine - linesBefore - 1; i <= foundLine + linesAfter - 1; i++)
            {
                if (i >= 0 && i < lines.Length && (i + 1) != foundLine)
                    result.Add(new GrepSearchResult.GrepLine(i + 1, lines[i], true, null));
            }
            return result;
        }

        /// <summary>
        /// Converts result lines into blocks of text
        /// </summary>
        /// <param name="result"></param>
        /// <param name="linesBefore"></param>
        /// <param name="linesAfter"></param>
        /// <returns></returns>
        public static IEnumerable<NumberedString> GetSnippets(GrepSearchResult result, int linesBefore, int linesAfter)
        {
            if (result.Matches.Count > 0)
            {
                int lastLine = 0;
                int firstLine = 0;
                StringBuilder snippetText = new StringBuilder();
                var lines = result.GetLinesWithContext(linesBefore, linesAfter);
                foreach (var line in lines)
                {
                    // First line of a block
                    if (firstLine == 0)
                    {
                        firstLine = line.LineNumber;
                        lastLine = line.LineNumber - 1;
                    }
                    // Sequence
                    if (line.LineNumber == lastLine + 1)
                    {
                        snippetText.AppendLine(line.LineText);
                    }
                    else
                    {
                        yield return new NumberedString { Text = snippetText.ToString().TrimEndOfLine(), FirstLineNumber = firstLine, LineCount = lines.Count };
                        lastLine = 0;
                        firstLine = 0;
                        snippetText.Clear();
                    }
                    lastLine = line.LineNumber;
                }
                if (snippetText.Length > 0)
                    yield return new NumberedString { Text = snippetText.ToString().TrimEndOfLine(), FirstLineNumber = firstLine, LineCount = lines.Count };
            }
            else
            {
                yield return new NumberedString() { LineCount = 0, FirstLineNumber = 0, Text = "" };
            }
        }

        public static int[] GetIntArray(int startLine, int lineCount)
        {
            int[] result = new int[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                result[i] = startLine + i;
            }
            return result;
        }

        /// <summary>
        /// Replaces unix-style linebreaks with \r\n
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CleanLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            string textTemp = Regex.Replace(text, "(\r)([^\n])", "\r\n$2");
            textTemp = Regex.Replace(textTemp, "([^\r])(\n)", "$1\r\n");
            textTemp = Regex.Replace(textTemp, "(\v)", "\r\n");
            return textTemp;
        }

        /// <summary>
        /// Sorts and removes dupes
        /// </summary>
        /// <param name="results"></param>
        public static void CleanResults(ref List<GrepSearchResult.GrepLine> results)
        {
            if (results == null || results.Count == 0)
                return;

            results.Sort();
            for (int i = results.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < results.Count; j++)
                {
                    if (i < results.Count &&
                        results[i].LineNumber == results[j].LineNumber && i != j)
                    {
                        if (results[i].IsContext)
                            results.RemoveAt(i);
                        else if (results[i].IsContext == results[j].IsContext && results[i].IsContext == false && results[i].LineNumber != -1)
                        {
                            results[j].Matches.AddRange(results[i].Matches);
                            results.RemoveAt(i);
                        }
                    }
                }
            }

            for (int j = 0; j < results.Count; j++)
            {
                results[j].Matches.Sort();
            }
        }

        /// <summary>
        /// Merges sorted context lines into sorted result lines
        /// </summary>
        /// <param name="results"></param>
        public static void MergeResults(ref List<GrepSearchResult.GrepLine> results, List<GrepSearchResult.GrepLine> contextLines)
        {
            if (contextLines == null || contextLines.Count == 0)
                return;

            if (results == null || results.Count == 0)
            {
                results = new List<GrepSearchResult.GrepLine>();
                foreach (var line in contextLines)
                    results.Add(line);
                return;
            }

            // Current list location
            int rIndex = 0;
            int cIndex = 0;

            while (rIndex < results.Count && cIndex < contextLines.Count)
            {
                if (contextLines[cIndex].LineNumber < results[rIndex].LineNumber)
                {
                    results.Insert(rIndex, contextLines[cIndex]);
                    cIndex++;
                    rIndex++;
                }
                else if (results[rIndex].LineNumber < contextLines[cIndex].LineNumber)
                {
                    rIndex++;
                }
                else if (results[rIndex].LineNumber == contextLines[cIndex].LineNumber)
                {
                    rIndex++;
                    cIndex++;
                }
            }

            while (cIndex < contextLines.Count)
            {
                results.Add(contextLines[cIndex]);
                cIndex++;
            }
        }

        /// <summary>
        /// Returns MD5 hash for string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetHash(string input)
        {
            // step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if beginText end with a non-alphanumeric character. Copied from AtroGrep.
        /// </summary>
        /// <param name="beginText">Text to test</param>
        /// <returns></returns>
        public static bool IsValidBeginText(string beginText)
        {
            if (beginText.Equals(string.Empty) ||
               beginText.EndsWith(" ") ||
               beginText.EndsWith("<") ||
               beginText.EndsWith(">") ||
               beginText.EndsWith("$") ||
               beginText.EndsWith("+") ||
               beginText.EndsWith("*") ||
               beginText.EndsWith("[") ||
               beginText.EndsWith("{") ||
               beginText.EndsWith("(") ||
               beginText.EndsWith(".") ||
               beginText.EndsWith("?") ||
               beginText.EndsWith("!") ||
               beginText.EndsWith(",") ||
               beginText.EndsWith(":") ||
               beginText.EndsWith(";") ||
               beginText.EndsWith("-") ||
               beginText.EndsWith("\\") ||
               beginText.EndsWith("/") ||
               beginText.EndsWith("'") ||
               beginText.EndsWith("\"") ||
               beginText.EndsWith(Environment.NewLine) ||
               beginText.EndsWith("\r\n") ||
               beginText.EndsWith("\r") ||
               beginText.EndsWith("\n") ||
               beginText.EndsWith("\t")
               )
            {
                return true;
            }

            return false;
        }

        public static string ReplaceSpecialCharacters(string input)
        {
            string result = input.Replace("\\t", "\t")
                                 .Replace("\\n", "\n")
                                 .Replace("\\0", "\0")
                                 .Replace("\\b", "\b")
                                 .Replace("\\r", "\r");
            return result;
        }

        /// <summary>
        /// Returns true if endText starts with a non-alphanumeric character. Copied from AtroGrep.
        /// </summary>
        /// <param name="endText"></param>
        /// <returns></returns>
        public static bool IsValidEndText(string endText)
        {
            if (endText.Equals(string.Empty) ||
               endText.StartsWith(" ") ||
               endText.StartsWith("<") ||
               endText.StartsWith("$") ||
               endText.StartsWith("+") ||
               endText.StartsWith("*") ||
               endText.StartsWith("[") ||
               endText.StartsWith("{") ||
               endText.StartsWith("(") ||
               endText.StartsWith(".") ||
               endText.StartsWith("?") ||
               endText.StartsWith("!") ||
               endText.StartsWith(",") ||
               endText.StartsWith(":") ||
               endText.StartsWith(";") ||
               endText.StartsWith("-") ||
               endText.StartsWith(">") ||
               endText.StartsWith("]") ||
               endText.StartsWith("}") ||
               endText.StartsWith(")") ||
               endText.StartsWith("\\") ||
               endText.StartsWith("/") ||
               endText.StartsWith("'") ||
               endText.StartsWith("\"") ||
               endText.StartsWith(Environment.NewLine) ||
               endText.StartsWith("\r\n") ||
               endText.StartsWith("\r") ||
               endText.StartsWith("\n") ||
               endText.StartsWith("\t")
               )
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Extension method on TimeSpan that gets a "pretty", human readable string of a TimeSpan, e.g. "1h 23m 45.678s".
        /// Hours and minutes are left off as not needed. Hours are the largest unit of time shown (e.g. not days, weeks).
        /// </summary>
        /// <param name="duration">The time span in question.</param>
        /// <returns>"Pretty", human readable string of the time span.</returns>
        public static string GetPrettyString(this TimeSpan duration)
        {
            var durationStringBuilder = new System.Text.StringBuilder();
            var totalHoursTruncated = (int)duration.TotalHours;

            if (totalHoursTruncated > 0)
                durationStringBuilder.Append(totalHoursTruncated + "h ");

            if (duration.Minutes > 0 || totalHoursTruncated > 0)
                durationStringBuilder.Append(duration.Minutes + "m ");

            durationStringBuilder.Append(duration.Seconds + "." + duration.Milliseconds + "s");

            return durationStringBuilder.ToString();
        }

        public static bool HasUtf8ByteOrderMark(Stream inputStream)
        {
            int b1 = inputStream.ReadByte();
            int b2 = inputStream.ReadByte();
            int b3 = inputStream.ReadByte();
            inputStream.Seek(0, SeekOrigin.Begin);

            return (0xEF == b1 && 0xBB == b2 && 0xBF == b3);
        }
    }

    public class KeyValueComparer : IComparer<KeyValuePair<string, int>>
    {
        public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }

    public class NumberedString
    {
        public int FirstLineNumber;
        public int LineCount;
        public string Text;
    }

    public static class TextReaderEx
    {
        public static string TrimEndOfLine(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.EndsWith("\r\n"))
                return text.Substring(0, text.Length - 2);
            else if (text.EndsWith("\r"))
                return text.Substring(0, text.Length - 1);
            else if (text.EndsWith("\n"))
                return text.Substring(0, text.Length - 1);
            else
                return text;
        }
    }
}
