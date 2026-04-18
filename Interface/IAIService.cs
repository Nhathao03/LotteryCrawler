namespace LotteryCrawler.Interface
{
    public interface IAIService
    {
        Task<string> SendAsync(string prompt);
    }
}