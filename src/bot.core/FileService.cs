using bot.model;

namespace bot.core
{
    public interface IFileService:IService
    {
        void Write(string message);
    }

    public class FileService : IFileService
    {
        public void Write(string message)
        {
            using (var file = new System.IO.StreamWriter($"h:\\error_log.txt", true))
            {
                file.WriteLine(message);
                file.Close();
            }
        }
    }
}
