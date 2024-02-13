namespace TechBlogCore.AOT.Dtos
{
    public class ResourceDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public string ContentType { get; set; }
        public string MD5 { get; set; }
        public long Uploader_Id { get; set; }
        public DateTime UploadTime { get; set; }
    }
}
