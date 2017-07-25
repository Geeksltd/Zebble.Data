namespace Zebble.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using Zebble;

    /// <summary> 
    /// Provides an utility for handling Binary property types.
    /// </summary>
    public class Document : IComparable<Document>, IComparable
    {
        const string EMPTY_FILE = "NoFile.Empty";
        static HashSet<string> CreatedFoldersCache = new HashSet<string>();
        static ConcurrentDictionary<string, bool> DocumentExistsCache = new ConcurrentDictionary<string, bool>();

        Entity Owner;
        bool IsEmptyDocument;
        Stream Stream;
        public string FileName;
        string fileNameWithoutExtension, folderName, fileExtension;
        public string OwnerProperty { get; private set; }

        public Document() { }

        public Document(FileInfo file) : this(new MemoryStream(file.ReadAllBytes()), file.Name) { }

        public Document(Stream stream, string fileName = null)
        {
            Stream = stream;
            FileName = fileName.Or((stream as FileStream)?.Name);
            FileName = FileName.Or("No.Name").ToSafeFileName();
        }

        IDocumentStorageProvider GetStorageProvider() => DocumentStorageProviderFactory.GetProvider(FolderName);

        [EscapeGCop("This is needed to get past VS warning on '==' overload")]
        public override int GetHashCode() => base.GetHashCode();

        public Stream GetStream()
        {
            if (Stream != null) return Stream;

            if (IsEmpty()) return null;

            return new FileStream(Device.IO.File(DetermineLocalPath()).FullName, FileMode.Open);
        }

        public string FileExtension
        {
            get
            {
                if (fileExtension != null) return fileExtension;

                if (FileName.LacksValue()) return fileExtension = string.Empty;

                var result = System.IO.Path.GetExtension(FileName).OrEmpty();
                if (result.HasValue() && result[0] != '.') result = "." + result;
                return fileExtension = result;
            }
        }

        /// <summary>Gets an empty document object. </summary>
        public static Document Empty() => new Document(null, EMPTY_FILE) { IsEmptyDocument = true };

        /// <summary>
        /// Gets or sets the data of this document.
        /// </summary>
        public byte[] FileData
        {
            get
            {
                if (IsEmpty()) return new byte[0];

                if (Stream?.Length > 0)
                    return Stream.ReadAllBytes();

                return GetStorageProvider().Load(this);
            }
        }

        public override string ToString() => IsEmpty() ? string.Empty : Path;

        public string FolderName
        {
            get
            {
                return folderName ?? (folderName = Owner?.GetType().Name.WithSuffix("_") + OwnerProperty);
            }
        }

        /// <summary> Determines whether this is an empty document. </summary>
        public bool IsEmpty()
        {
            if (IsEmptyDocument) return true;

            var exists = DocumentExistsCache.GetOrAdd(DetermineLocalPath(), x => GetStorageProvider().FileExists(this));

            if (exists) return false;

            if (Stream == null) return true;

            return Stream.Length == 0;
        }

        public bool HasValue() => !IsEmpty();

        public Document Clone() => new Document(new MemoryStream(FileData), FileName);

        public Document Attach(Entity owner, string propertyName)
        {
            Owner = owner;
            OwnerProperty = propertyName;

            owner.Saving += Save;
            owner.Deleting += Delete;

            return this;
        }

        /// <summary>
        /// Detaches this Document.
        /// </summary>
        public void Detach()
        {
            if (Owner != null)
            {
                Owner.Saving -= Save;
                Owner.Deleting -= Delete;
            }
        }

        /// <summary>
        /// Deletes this document from the disk.
        /// </summary>
        void Delete(object sender, EventArgs e) => DeleteFromDisk();

        void DeleteFromDisk()
        {
            if (Owner == null) throw new InvalidOperationException();

            DocumentExistsCache.TryRemove(DetermineLocalPath());

            GetStorageProvider().Delete(this);

            Stream = null;

            DocumentExistsCache.TryRemove(DetermineLocalPath());
        }

        void Save(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DocumentExistsCache.TryRemove(DetermineLocalPath());

            if (Stream != null && Stream.Length > 0)
            {
                GetStorageProvider().Save(this);
            }
            else if (IsEmptyDocument)
            {
                DeleteFromDisk();
            }

            DocumentExistsCache.TryRemove(DetermineLocalPath());
        }

        internal DirectoryInfo LocalFolder
        {
            get
            {
                if (Owner == null) return null;

                return Device.IO.Directory("AppFiles/" + FolderName);
            }
        }

        public string Path
        {
            get
            {
                if (IsEmpty()) return null;
                else return DetermineLocalPath();
            }
        }

        /// <summary>
        /// Gets the relative local path of this file.
        /// </summary>
        public string DetermineLocalPath()
        {
            if (Owner == null) return null;

            var result = "AppFiles/" + FolderName;

            // Ensure the folder exists:
            if (CreatedFoldersCache.Lacks(result))
            {
                Device.IO.Directory(result).EnsureExists();
                CreatedFoldersCache.Add(result);
            }

            return result + "/" + GetFileNameWithoutExtension() + FileExtension;
        }

        public string GetFileNameWithoutExtension()
        {
            return fileNameWithoutExtension ?? (fileNameWithoutExtension = Owner?.GetId().ToStringOrEmpty());
        }

        public override bool Equals(object obj)
        {
            var otherDocument = obj as Document;

            if (ReferenceEquals(otherDocument, null)) return false;
            else if (ReferenceEquals(this, otherDocument)) return true;
            else if (IsEmpty() && otherDocument.IsEmpty()) return true;

            return false;
        }

        public static bool operator ==(Document left, Document right)
        {
            if (ReferenceEquals(left, right)) return true;

            if (ReferenceEquals(left, null)) return false;

            return left.Equals(right);
        }

        public static bool operator !=(Document left, Document right) => !(left == right);

        /// <summary>
        /// Gets this document if it has a value, otherwise another specified document.
        /// </summary>
        public Document Or(Document other) => IsEmpty() ? other : this;

        /// <summary>
        /// Compares this document versus a specified other document.
        /// </summary>
        public int CompareTo(Document other)
        {
            if (other == null) return 1;

            if (IsEmpty())
            {
                if (other.IsEmpty())
                    return 0;
                else return -1;
            }
            else
            {
                if (other.IsEmpty())
                    return 1;
                else
                {
                    var me = FileData.Length;
                    var him = other.FileData.Length;
                    if (me == him) return 0;
                    if (me > him) return 1;
                    else return -1;
                }
            }
        }

        public int CompareTo(object obj) => CompareTo(obj as Document);

        /// <summary>Gets the mime type based on the file extension.</summary>
        public string GetMimeType() => Device.IO.File(FileName).GetMimeType();

        /// <summary>Determines if this document's file extension is for audio or video.</summary>
        public bool IsMedia() => GetMimeType().StartsWithAny("audio/", "video/");
    }
}