using System.Collections.Generic;
using System.Threading.Tasks;
using LotteryCrawler.Models;

namespace LotteryCrawler.Interface
{
    public interface ILotteryDrawService
    {
        Task<List<List<string>>> GetDrawRowsAsync(string url, string elementId);
        Task<LotteryResult> GetLotteryResultAsync(string url, string elementId);
    }
}