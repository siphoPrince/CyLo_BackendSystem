namespace Cylo_Backend.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // 1. Define where the files go (wwwroot/uploads)
            string contentPath = _environment.WebRootPath;
            string path = Path.Combine(contentPath, "uploads");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // 2. Create a unique filename: 
            // Result: "a1b2c3d4-..." + ".jpg"
            string fileExtension = Path.GetExtension(file.FileName);
            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string fileNameWithPath = Path.Combine(path, uniqueFileName);

            // 3. Save the file to the disk
            using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return just the filename to be stored in the SQL Database
            return uniqueFileName;
        }
    }
}
