namespace QuanCaPhe.Models;

// Tương đương lớp do Scaffold-DbContext sinh ra.
public class Mon
{
    public int MaMon { get; set; }
    public string TenMon { get; set; } = null!;
    public string NhomMon { get; set; } = null!;
    public decimal DonGia { get; set; }
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}

public class HoaDon
{
    public int MaHD { get; set; }
    public DateTime NgayLap { get; set; }
    public int SoBan { get; set; }
    public string HinhThucTT { get; set; } = null!;
    public string GhiChu { get; set; }
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}

public class ChiTietHoaDon
{
    public int MaCT { get; set; }
    public int MaHD { get; set; }
    public int MaMon { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public virtual HoaDon HoaDon { get; set; } = null!;
    public virtual Mon Mon { get; set; } = null!;
}
