using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using LotteryCrawler.Models;
using LotteryCrawler.Interface;

namespace LotteryCrawler.Services
{
    public class LotteryDrawService : ILotteryDrawService
    {
        private readonly HttpClient _httpClient;
        public LotteryDrawService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<List<string>>> GetDrawRowsAsync(string url, string elementId)
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var table = doc.GetElementbyId(elementId);
            var result = new List<List<string>>();
            if (table != null)
            {
                foreach (var row in table.SelectNodes(".//tr") ?? new HtmlNodeCollection(table))
                {
                    var cells = new List<string>();
                    foreach (var cell in row.SelectNodes(".//td|.//th") ?? new HtmlNodeCollection(row))
                    {
                        // Preserve <br> tags by converting them to a special marker before getting InnerText
                        var cellHtml = cell.InnerHtml;
                        cellHtml = cellHtml.Replace("<br>", "|BR|").Replace("<br/>", "|BR|").Replace("<br />", "|BR|");
                        var cellText = cell.InnerText.Trim();
                        // If we had <br> tags, restore them
                        if (cellHtml.Contains("|BR|"))
                        {
                            cellText = cellHtml.Replace("|BR|", "<br>");
                        }
                        cells.Add(cellText);
                    }
                    if (cells.Count > 0)
                        result.Add(cells);
                }
            }
            return result;
        }

        public async Task<LotteryResult> GetLotteryResultAsync(string url, string elementId)
        {
            var rows = await GetDrawRowsAsync(url, elementId);
            return ParseRowsToLotteryResult(rows, elementId);
        }

        private LotteryResult ParseRowsToLotteryResult(List<List<string>> rows, string elementId)
        {
            var result = new LotteryResult
            {
                Date = ExtractDateFromRows(rows),
                Region = GetRegionFromElementId(elementId),
                Prizes = new List<Prize>()
            };

            //var prize = new Prize
            //{
            //    Province = string.Empty,
            //    Data = new Dictionary<string, LotteryData>()
            //};

            // Extract province names from the first row (header row)
            var provinceNames = new List<string>();
            if (rows.Count > 0)
            {
                // Skip the first column (date) and extract province names
                for (int i = 1; i < rows[0].Count; i++)
                {
                    var provinceName = ExtractProvinceName(rows[0][i]);
                    if (!string.IsNullOrEmpty(provinceName))
                    {
                        provinceNames.Add(provinceName);
                    }
                }
            }

            foreach (var provinceName in provinceNames)
            {
                result.Prizes.Add(new Prize
                {
                    Province = provinceName,
                    Data = new List<LotteryData> { new LotteryData() }
                });
            }

            foreach (var row in rows)
            {
                if (row.Count < 2) continue;

                var prizeType = row[0].Trim();
                if (string.IsNullOrEmpty(prizeType) ||
                    prizeType.Contains("Thứ") ||
                    prizeType.Contains("CN") ||
                    prizeType.Contains("Đầu") ||
                    prizeType.Contains("Tìm lô tô"))
                    continue;

                for (int i = 1; i < row.Count && i - 1 < provinceNames.Count; i++)
                {
                    var numbers = row[i].Trim();
                    if (string.IsNullOrEmpty(numbers)) continue;

                    var numberList = numbers.Split(new[] { "<br>", "<br/>", "<br />", "\n", " " },
                                                    StringSplitOptions.RemoveEmptyEntries)
                                            .Select(n => n.Trim())
                                            .Where(n => !string.IsNullOrEmpty(n) && IsValidNumber(n))
                                            .ToList();

                    var prize = result.Prizes[i - 1]; // Prize tương ứng với tỉnh
                    var LotteryData = prize.Data.First();

                    switch (prizeType.ToUpper())
                    {
                        case "ĐB":
                        case "DB":
                        case "GIẢI ĐB":
                            LotteryData.DB.AddRange(numberList);
                            break;
                        case "G1":
                        case "G.1":
                        case "GIẢI NHẤT":
                            LotteryData.G1.AddRange(numberList);
                            break;
                        case "G2":
                        case "G.2":
                        case "GIẢI NHÌ":
                            LotteryData.G2.AddRange(numberList);
                            break;
                        case "G3":
                        case "G.3":
                        case "GIẢI BA":
                            LotteryData.G3.AddRange(numberList);
                            break;
                        case "G4":
                        case "G.4":
                        case "GIẢI TƯ":
                            LotteryData.G4.AddRange(numberList);
                            break;
                        case "G5":
                        case "G.5":
                        case "GIẢI NĂM":
                            LotteryData.G5.AddRange(numberList);
                            break;
                        case "G6":
                        case "G.6":
                        case "GIẢI SÁU":
                            LotteryData.G6.AddRange(numberList);
                            break;
                        case "G7":
                        case "G.7":
                        case "GIẢI BẢY":
                            LotteryData.G7.AddRange(numberList);
                            break;
                        case "G8":
                        case "G.8":
                        case "GIẢI TÁM":
                            LotteryData.G8.AddRange(numberList);
                            break;
                    }
                }
            }
            return result;
        }

        private string ExtractProvinceName(string cellContent)
        {
            // Extract province name from HTML content like:
            // <a href="/xsvl">Vĩnh Long</a><i class="dockq"...
            // or just plain text like "Vĩnh Long"

            if (string.IsNullOrEmpty(cellContent)) return string.Empty;

            // Remove HTML tags and extract text
            var cleanText = System.Text.RegularExpressions.Regex.Replace(cellContent, @"<[^>]+>", "").Trim();

            // Handle special cases
            if (cleanText.Contains("Hồ Chí Minh"))
                return "Hồ Chí Minh";
            if (cleanText.Contains("Vĩnh Long"))
                return "Vĩnh Long";
            if (cleanText.Contains("Bình Dương"))
                return "Bình Dương";
            if (cleanText.Contains("Trà Vinh"))
                return "Trà Vinh";
            if (cleanText.Contains("Tây Ninh"))
                return "Tây Ninh";
            if (cleanText.Contains("An Giang"))
                return "An Giang";
            if (cleanText.Contains("Bình Thuận"))
                return "Bình Thuận";
            if (cleanText.Contains("Đồng Nai"))
                return "Đồng Nai";
            if (cleanText.Contains("Cần Thơ"))
                return "Cần Thơ";
            if (cleanText.Contains("Sóc Trăng"))
                return "Sóc Trăng";
            if (cleanText.Contains("Bến Tre"))
                return "Bến Tre";
            if (cleanText.Contains("Vũng Tàu"))
                return "Vũng Tàu";
            if (cleanText.Contains("Bạc Liêu"))
                return "Bạc Liêu";
            if (cleanText.Contains("Đồng Tháp"))
                return "Đồng Tháp";
            if (cleanText.Contains("Cà Mau"))
                return "Cà Mau";
            if (cleanText.Contains("Tiền Giang"))
                return "Tiền Giang";
            if (cleanText.Contains("Kiên Giang"))
                return "Kiên Giang";
            if (cleanText.Contains("Đà Lạt"))
                return "Đà Lạt";
            if (cleanText.Contains("Long An"))
                return "Long An";
            if (cleanText.Contains("Bình Phước"))
                return "Bình Phước";
            if (cleanText.Contains("Hậu Giang"))
                return "Hậu Giang";

            // If no specific match, return the cleaned text
            return cleanText;
        }

        private string ExtractDateFromRows(List<List<string>> rows)
        {
            // Try to extract date from the first row (header row)
            if (rows.Count > 0 && rows[0].Count > 0)
            {
                var firstCell = rows[0][0];
                // Look for date patterns like "03/10", "02/10", etc.
                var dateMatch = System.Text.RegularExpressions.Regex.Match(firstCell, @"(\d{1,2})/(\d{1,2})");
                if (dateMatch.Success)
                {
                    var day = dateMatch.Groups[1].Value.PadLeft(2, '0');
                    var month = dateMatch.Groups[2].Value.PadLeft(2, '0');
                    var year = DateTime.Now.Year; // Assume current year
                    return $"{year}-{month}-{day}";
                }
            }

            // Fallback to current date
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private bool IsValidNumber(string number)
        {
            // Check if the string contains only digits and is not empty
            // Allow numbers from 2 to 6 digits (typical lottery number lengths)
            return !string.IsNullOrEmpty(number) &&
                   number.All(char.IsDigit) &&
                   number.Length >= 2 &&
                   number.Length <= 6;
        }

        private string GetRegionFromElementId(string elementId)
        {
            if (elementId.StartsWith("MB"))
                return "Miền Bắc";
            else if (elementId.StartsWith("MT"))
                return "Miền Trung";
            else if (elementId.StartsWith("MN"))
                return "Miền Nam";
            else
                return "Unknown";
        }
    }
}