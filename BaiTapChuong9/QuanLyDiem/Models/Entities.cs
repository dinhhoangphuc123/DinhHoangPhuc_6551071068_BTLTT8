namespace QuanLyDiem.Models;

// Tương đương lớp do Scaffold-DbContext sinh ra.
public class Lop
{
    public string MaLop { get; set; } = null!;
    public string TenLop { get; set; } = null!;
    public virtual ICollection<SinhVien> SinhViens { get; set; } = new List<SinhVien>();
}

public class SinhVien
{
    public string MaSV { get; set; } = null!;
    public string HoTen { get; set; } = null!;
    public DateTime NgaySinh { get; set; }
    public string GioiTinh { get; set; } = null!;
    public string MaLop { get; set; } = null!;
    public virtual Lop MaLopNavigation { get; set; } = null!;
    public virtual ICollection<DiemMon> DiemMons { get; set; } = new List<DiemMon>();
}

public class DiemMon
{
    public int MaDiem { get; set; }
    public string MaSV { get; set; } = null!;
    public string TenMon { get; set; } = null!;
    public int SoTinChi { get; set; }
    public decimal Diem { get; set; }
    public virtual SinhVien MaSVNavigation { get; set; } = null!;
}
