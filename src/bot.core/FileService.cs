using bot.model;

namespace bot.core
{
    public interface IFileService:IService
    {
       // void Write(string message);
        void Write(string stream, string message);
    }

    public class FileService : IFileService
    {
        private readonly IDateTime _dateTime;

        public FileService(IDateTime dateTime)
        {
            _dateTime = dateTime;
        }
        public void Write(string message)
        {
            using (var file = new System.IO.StreamWriter($"h:\\bot_{_dateTime.Now:yyMMdd}.txt", true))
            {
                file.WriteLine($"{_dateTime.Now:t}| {message}");
                file.Close();
            }
        }

        public void Write(string stream,string message)
        {
            using (var file = new System.IO.StreamWriter($"h:\\bot_{stream}_{_dateTime.Now:yyMMdd}.txt", true))
            {
                file.WriteLine($"{_dateTime.Now:t}| {message}");
                file.Close();
            }
        }
    }
}
