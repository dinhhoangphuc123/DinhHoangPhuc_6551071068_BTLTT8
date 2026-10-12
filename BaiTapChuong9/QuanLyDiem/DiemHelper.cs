using System.Globalization;
using QuanLyDiem.Models;

namespace QuanLyDiem;

public static class DiemHelper
{
    public static readonly CultureInfo VN = new("vi-VN");

    /// <summary>Điểm TB có trọng số tín chỉ, làm tròn 2 chữ số; null nếu chưa có điểm.</summary>
    public static decimal? DiemTB(IReadOnlyCollection<DiemMon> ds)
    {
        int tc = ds.Sum(d => d.SoTinChi);
        if (ds.Count == 0 || tc == 0) return null;
        return Math.Round(ds.Sum(d => d.Diem * d.SoTinChi) / tc, 2, MidpointRounding.AwayFromZero);
    }

    public static string XepLoai(decimal? tb) =>
        tb == null ? "Chưa có điểm" : tb >= 8m ? "Giỏi" : tb >= 7m ? "Khá" : tb >= 5m ? "Trung bình" : "Yếu";

    public static string Fmt(decimal d) => d.ToString("0.00", VN);
}

/// <summary>Dòng của lưới điểm.</summary>
public class DiemRow
{
    public int MaDiem { get; set; }
    public int STT { get; set; }
    public string TenMon { get; set; } = "";
    public int SoTinChi { get; set; }
    public decimal Diem { get; set; }
}
