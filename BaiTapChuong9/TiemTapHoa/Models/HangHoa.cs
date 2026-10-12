namespace TiemTapHoa.Models;

// Tương đương lớp do Scaffold-DbContext sinh ra (các cột NOT NULL).
public class HangHoa
{
    public int MaHang { get; set; }
    public string TenHang { get; set; } = null!;
    public string LoaiHang { get; set; }
    public string DonVi { get; set; }
    public decimal GiaBan { get; set; }
    public int SoLuongTon { get; set; }
    public DateTime NgayNhap { get; set; }
    public bool ConKinhDoanh { get; set; }
}
