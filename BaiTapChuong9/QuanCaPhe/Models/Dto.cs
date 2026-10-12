namespace QuanCaPhe.Models;

/// <summary>DTO đưa vào FastReport (đăng ký tên "ChiTiet").</summary>
public class ChiTietIn
{
    public int STT { get; set; }
    public string TenMon { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

/// <summary>Dòng của lưới chi tiết (thêm MaCT để biết dòng nào đang chọn).</summary>
public class ChiTietRow : ChiTietIn
{
    public int MaCT { get; set; }
}

/// <summary>Dòng của lưới hóa đơn; TongTien do truy vấn tính, không lưu trong bảng.</summary>
public class HoaDonRow
{
    public int MaHD { get; set; }
    public string MaHienThi => "HD" + MaHD.ToString("D6");
    public DateTime NgayLap { get; set; }
    public int SoBan { get; set; }
    public string HinhThucTT { get; set; } = "";
    public string GhiChu { get; set; }
    public decimal TongTien { get; set; }
}

public class MonItem
{
    public int MaMon { get; set; }
    public decimal DonGia { get; set; }
    public string Hien { get; set; } = "";
    public override string ToString() => Hien;
}
