using Microsoft.EntityFrameworkCore;

namespace TiemTapHoa.Models;

public class TiemTapHoaContext : DbContext
{
    public virtual DbSet<HangHoa> HangHoas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o) =>
        o.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=TiemTapHoa;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<HangHoa>(e =>
        {
            e.ToTable("HangHoa");
            e.HasKey(x => x.MaHang);
            e.Property(x => x.TenHang).HasMaxLength(100);
            e.Property(x => x.LoaiHang).HasMaxLength(50);
            e.Property(x => x.DonVi).HasMaxLength(20);
            e.Property(x => x.GiaBan).HasColumnType("decimal(18, 0)");
            e.Property(x => x.NgayNhap).HasColumnType("datetime");
        });
    }
}
