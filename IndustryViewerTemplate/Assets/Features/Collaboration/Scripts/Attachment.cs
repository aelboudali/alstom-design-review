using System;
using System.IO;
using UnityEngine;

namespace Unity.Industry.Viewer.Collaboration
{
    public class Attachment: IEquatable<Attachment>
    {
        public readonly string FilePath;
        
        public readonly string FileType;
        public readonly string ContentType;
        private readonly Guid _attachmentId;
        public string FileName => Path.GetFileName(FilePath);
        public byte[] Bytes => File.ReadAllBytes(FilePath);
        public long FileSize => Bytes.LongLength;

        public Attachment(string filePath)
        {
            _attachmentId = Guid.NewGuid();
            FilePath = filePath;
            var extension = Path.GetExtension(filePath).Replace(".", "");
            FileType = extension;
            ContentType = GetContentType(Path.GetExtension(filePath));
        }

        private static string GetContentType(string extension)
        {
            extension = extension?.ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".tiff" => "image/tiff",

                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",

                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".aac" => "audio/aac",
                ".m4a" => "audio/mp4",

                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                ".csv" => "text/csv",

                ".fbx" => "application/octet-stream",
                ".obj" => "application/octet-stream",
                ".gltf" => "model/gltf+json",
                ".glb" => "model/gltf-binary",

                _ => "application/octet-stream"
            };
        }
        
        public static bool operator ==(Attachment left, Attachment right)
        {
            if (ReferenceEquals(left, right))
                return true;
    
            if (left is null || right is null)
                return false;
    
            return left._attachmentId.Equals(right._attachmentId);
        }

        public static bool operator !=(Attachment left, Attachment right)
        {
            return !(left == right);
        }

        public bool Equals(Attachment other)
        {
            return other != null && _attachmentId.Equals(other._attachmentId);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Attachment);
        }

        public override int GetHashCode()
        {
            return _attachmentId.GetHashCode();
        }
    }
}
