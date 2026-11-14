using System.Text.Json;

namespace BookStoreWeb.Models
{
    public class CartItem
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
        public string? CoverImage { get; set; }
    }

    public class PromoData
    {
        public bool IsActive { get; set; } = true;
        public string ProgramTitle { get; set; } = "🔥 SIÊU HỘI SÁCH TRỰC TUYẾN - KHUYẾN MÃI ĐẶC BIỆT";
        public string MarqueeText { get; set; } = "🎉 Chào mừng bạn đến với BookStoreWeb! Tích lũy điểm thưởng nhận ngay ưu đãi thành viên VIP - Tổng đài: 1900 6868! 📚";
        public Dictionary<int, int> CategoryDiscounts { get; set; } = new();
        public Dictionary<int, int> BookDiscounts { get; set; } = new();
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
    }

    // Mô hình Thông báo khuyến mãi gửi cho Khách hàng
    public class NotificationMessage
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string TargetUser { get; set; } = "All"; // "All" hoặc username cụ thể
    }

    public static class PromoManager
    {
        private static string FilePath => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "promotions.json");
        private static string TicketPath => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "support_tickets.json");
        private static string NotifPath => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "notifications.json");

        public static PromoData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<PromoData>(json) ?? new PromoData();
                }
            }
            catch { }
            return new PromoData();
        }

        public static void Save(PromoData data)
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public static int GetEffectiveDiscount(int bookId, int categoryId, PromoData? data = null)
        {
            data ??= Load();
            if (!data.IsActive) return 0;
            int bookDisc = data.BookDiscounts.TryGetValue(bookId, out int bd) ? bd : 0;
            int catDisc = data.CategoryDiscounts.TryGetValue(categoryId, out int cd) ? cd : 0;
            return Math.Max(bookDisc, catDisc);
        }

        public static decimal GetFinalPrice(decimal originalPrice, int discountPercent)
        {
            if (discountPercent <= 0) return originalPrice;
            if (discountPercent >= 100) return 0;
            return Math.Round(originalPrice * (100 - discountPercent) / 100m, 0);
        }

        public static List<SupportTicket> LoadTickets()
        {
            try
            {
                if (File.Exists(TicketPath))
                {
                    string json = File.ReadAllText(TicketPath);
                    return JsonSerializer.Deserialize<List<SupportTicket>>(json) ?? new List<SupportTicket>();
                }
            }
            catch { }
            return new List<SupportTicket>();
        }

        public static void SaveTickets(List<SupportTicket> tickets)
        {
            try
            {
                string dir = Path.GetDirectoryName(TicketPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(tickets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TicketPath, json);
            }
            catch { }
        }

        // Quản lý Thông báo
        public static List<NotificationMessage> LoadNotifications()
        {
            try
            {
                if (File.Exists(NotifPath))
                {
                    string json = File.ReadAllText(NotifPath);
                    return JsonSerializer.Deserialize<List<NotificationMessage>>(json) ?? new List<NotificationMessage>();
                }
            }
            catch { }
            return new List<NotificationMessage>();
        }

        public static void SaveNotifications(List<NotificationMessage> notifs)
        {
            try
            {
                string dir = Path.GetDirectoryName(NotifPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(notifs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(NotifPath, json);
            }
            catch { }
        }

        // Tính hạng thành viên dựa trên tổng tiền đã mua của khách hàng
        public static (string TierName, string BadgeColor, int ExtraDiscount) GetMembershipTier(decimal totalSpent)
        {
            if (totalSpent >= 5000000) return ("Kim Cương (Diamond)", "bg-dark text-warning", 10); // Giảm thêm 10%
            if (totalSpent >= 2000000) return ("Vàng (Gold)", "bg-warning text-dark", 5);      // Giảm thêm 5%
            if (totalSpent >= 800000) return ("Bạc (Silver)", "bg-secondary text-white", 2);   // Giảm thêm 2%
            return ("Thành Viên Mới (Bronze)", "bg-primary text-white", 0);
        }
    }
}