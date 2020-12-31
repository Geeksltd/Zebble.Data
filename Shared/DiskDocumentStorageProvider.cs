namespace Zebble.Data
{
    using System.IO;
    using Olive;

    class DiskDocumentStorageProvider : IDocumentStorageProvider
    {
        FileInfo GetFile(Document document) => Device.IO.File(document.DetermineLocalPath());

        public void Save(Document document)
        {
            var fileDataToSave = document.FileData;

            var file = GetFile(document);
            file.Delete();

            file.WriteAllBytes(fileDataToSave);
        }

        public void Delete(Document document) => GetFile(document).Delete();

        public byte[] Load(Document document)
        {
            var file = GetFile(document);

            if (file.Exists()) return file.ReadAllBytes();

            return new byte[0];
        }

        public bool FileExists(Document document)
        {
            if (document.DetermineLocalPath().IsEmpty()) return false;

            return GetFile(document).Exists();
        }
    }
}