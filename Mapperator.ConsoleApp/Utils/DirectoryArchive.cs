using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Mapperator.ConsoleApp.Utils;

public class DirectoryArchive : IArchive
{
    private readonly List<IArchiveEntry> entries;

    public DirectoryArchive(string directoryPath, IEnumerable<string>? fileSystemEntries = null)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException(directoryPath);

        string rootPath = Path.GetFullPath(directoryPath);
        fileSystemEntries ??= Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.AllDirectories);
        entries = fileSystemEntries
            .Select(IArchiveEntry (path) => new DirectoryArchiveEntry(this, rootPath, path))
            .ToList();
    }

    public event EventHandler<ArchiveExtractionEventArgs<IArchiveEntry>>? EntryExtractionBegin;
    public event EventHandler<ArchiveExtractionEventArgs<IArchiveEntry>>? EntryExtractionEnd;
    public event EventHandler<CompressedBytesReadEventArgs>? CompressedBytesRead;
    public event EventHandler<FilePartExtractionBeginEventArgs>? FilePartExtractionBegin;

    public IEnumerable<IArchiveEntry> Entries => entries;
    public IEnumerable<IVolume> Volumes => [];
    public ArchiveType Type => ArchiveType.Zip; // arbitrary placeholder
    public bool IsSolid => false;
    public bool IsComplete => true;
    public long TotalSize => entries.Sum(e => e.CompressedSize);
    public long TotalUncompressSize => entries.Sum(e => e.Size);

    public IReader ExtractAllEntries() {
        throw new InvalidOperationException();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private class DirectoryArchiveEntry : IArchiveEntry
    {
        private readonly string fullPath;
        private readonly string relativePath;

        public DirectoryArchiveEntry(IArchive archive, string rootPath, string path)
        {
            Archive = archive;
            fullPath = Path.GetFullPath(path);
            relativePath = Path.GetRelativePath(rootPath, fullPath);

            var info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                IsDirectory = true;
                Size = 0;
                CompressedSize = 0;
            }
            else
            {
                IsDirectory = false;
                Size = info.Length;
                CompressedSize = info.Length;
                LastModifiedTime = info.LastWriteTimeUtc;
                CreatedTime = info.CreationTimeUtc;
                LastAccessedTime = info.LastAccessTimeUtc;
            }
        }

        public Stream OpenEntryStream()
        {
            if (IsDirectory) throw new InvalidOperationException("Cannot open a directory as a stream.");
            return File.OpenRead(fullPath);
        }

        public bool IsComplete => true;
        public IArchive Archive { get; }

        public CompressionType CompressionType => CompressionType.None;
        public DateTime? ArchivedTime => null;
        public long CompressedSize { get; }
        public long Crc => 0;
        public DateTime? CreatedTime { get; }
        public string Key => relativePath.Replace(Path.DirectorySeparatorChar, '/');
        public string? LinkTarget => null;
        public bool IsDirectory { get; }
        public bool IsEncrypted => false;
        public bool IsSplitAfter => false;
        public bool IsSolid => false;
        public int VolumeIndexFirst => 0;
        public int VolumeIndexLast => 0;
        public DateTime? LastAccessedTime { get; }
        public DateTime? LastModifiedTime { get; }
        public long Size { get; }
        public int? Attrib => null;
    }
}