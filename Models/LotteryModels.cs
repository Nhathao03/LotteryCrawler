using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace LotteryCrawler.Models
{
    public class LotteryResult
    {
        public string Date { get; set; } = string.Empty;          // Ngày mở thưởng (yyyy-MM-dd)
        public string Region { get; set; } = string.Empty;        // Miền (Miền Nam, Miền Trung, Miền Bắc)
        public List<Prize> Prizes { get; set; }
    }

    public class Prize
    {
        public string Province { get; set; } = string.Empty; // Tên tỉnh
        public List<LotteryData> Data { get; set; }
    }
    public class LotteryData
    {
        public List<string> G8 { get; set; } = new List<string>();
        public List<string> G7 { get; set; } = new List<string>();
        public List<string> G6 { get; set; } = new List<string>();
        public List<string> G5 { get; set; } = new List<string>();
        public List<string> G4 { get; set; } = new List<string>();
        public List<string> G3 { get; set; } = new List<string>();
        public List<string> G2 { get; set; } = new List<string>();
        public List<string> G1 { get; set; } = new List<string>();
        public List<string> DB { get; set; } = new List<string>();  // Giải đặc biệt
    }

    public class LotteryEmailSender
    {
        public async Task SendLotteryPredictionEmailAsync(string recipientEmail)
        {
            // 🔹 Đường dẫn file JSON
            string filePath = Path.Combine("result", "predictor", "lottery-prediction-next-day.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Không tìm thấy file: {filePath}");
                return;
            }

            // 🔹 Đọc và parse JSON
            string json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var prediction = System.Text.Json.JsonSerializer.Deserialize<LotteryResult>(json, options);

            if (prediction == null)
            {
                Console.WriteLine("❌ Không thể đọc dữ liệu JSON.");
                return;
            }

            // 🔹 Tạo nội dung HTML
            var sb = new StringBuilder();
            sb.Append($"<h2 style='color:#2a7ae2;'>🎯 Lottery Prediction Results - {prediction.Date}</h2>");
            sb.Append($"<p><strong>Region:</strong> {prediction.Region}</p>");

            foreach (var prize in prediction.Prizes)
            {
                sb.Append($"<h3 style='color:#333;'>{prize.Province}</h3>");
                sb.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;font-family:Arial;font-size:14px;width:100%;'>");
                sb.Append("<tr style='background-color:#f2f2f2;'><th>Prize</th><th>Numbers</th></tr>");

                // Mỗi tỉnh chỉ có 1 phần tử data
                var data = prize.Data.FirstOrDefault();
                if (data != null)
                {
                    void AddRow(string label, List<string> values)
                    {
                        if (values != null && values.Count > 0)
                            sb.Append($"<tr><td><b>{label}</b></td><td>{string.Join(", ", values)}</td></tr>");
                    }

                    AddRow("G8", data.G8);
                    AddRow("G7", data.G7);
                    AddRow("G6", data.G6);
                    AddRow("G5", data.G5);
                    AddRow("G4", data.G4);
                    AddRow("G3", data.G3);
                    AddRow("G2", data.G2);
                    AddRow("G1", data.G1);
                    AddRow("DB", data.DB);
                }

                sb.Append("</table><br>");
            }

            string htmlBody = sb.ToString();

            // 🔹 Cấu hình email
            using var message = new MailMessage();
            message.From = new MailAddress("nhathaoha11@gmail.com", "Lottery Predictor Bot");
            message.To.Add(recipientEmail);
            message.Subject = $"🎯 Lottery Prediction Results - {DateTime.Now.AddDays(1):yyyy-MM-dd}";
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            // 🔹 Cấu hình SMTP
            using var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("nhathaoha11@gmail.com", "tdbx vbun mjra xgdk"), // App Password
                EnableSsl = true
            };

            try
            {
                await smtp.SendMailAsync(message);
                Console.WriteLine($"✅ Email đã gửi thành công đến: {recipientEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Gửi email thất bại: {ex.Message}");
            }
        }

    }
}
