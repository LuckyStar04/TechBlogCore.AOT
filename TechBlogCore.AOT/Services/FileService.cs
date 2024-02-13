using TechBlogCore.AOT.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class FileService
    {
        private readonly IConfiguration config;

        public FileService(IConfiguration config)
        {
            this.config = config;
        }
        public async Task<IResult> GetFile(string name)
        {
            var path = config["UploadFilePath"];
            var location = Path.Combine(path, name);
            if (!File.Exists(location)) return Results.NotFound();
            var ext = name.Substring(name.LastIndexOf('.')).ToLower();
            if (ext != ".jpg" && ext != ".png") return Results.NotFound();
            return Results.File(await File.ReadAllBytesAsync(location), ext.Replace(".", "image/"));
        }

        public async Task<IResult> UploadFile(HttpRequest Request)
        {
            if (Request.Form.Files.Count == 0)
                throw new MessageException("无有效文件！");

            var files = Request.Form.Files;
            foreach (var file in files)
            {
                var ext = file.FileName.Substring(file.FileName.LastIndexOf('.')).ToLower();
                if ((ext != ".jpg" && ext != ".png") || !file.ContentType.Contains("image"))
                {
                    throw new MessageException("图片格式不正确");
                }
            }

            var path = config["UploadFilePath"];
            var results = new List<string>(files.Count);
            foreach (var file in files)
            {
                var filename = $"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}{file.FileName.Substring(file.FileName.LastIndexOf('.')).ToLower()}";
                var location = Path.Combine(path, filename);
                using (var stream = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    stream.Position = 0;
                    await file.OpenReadStream().CopyToAsync(stream);
                }
                results.Add(filename);
            }
            return Results.Ok(string.Join(",", results));
        }
    }
}
