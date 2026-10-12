using Microsoft.EntityFrameworkCore;

namespace QuanCaPhe.Models;

public class QuanCaPheContext : DbContext
{
    public virtual DbSet<Mon> Mons { get; set; }
    public virtual DbSet<HoaDon> HoaDons { get; set; }
    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o) =>
        o.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=QuanCaPhe;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Mon>(e =>
        {
            e.ToTable("Mon"); e.HasKey(x => x.MaMon);
            e.Property(x => x.MaMon).ValueGeneratedNever();
            e.Property(x => x.TenMon).HasMaxLength(100);
            e.Property(x => x.NhomMon).HasMaxLength(30);
            e.Property(x => x.DonGia).HasColumnType("decimal(18, 0)");
        });
        mb.Entity<HoaDon>(e =>
        {
            e.ToTable("HoaDon"); e.HasKey(x => x.MaHD);
            e.Property(x => x.NgayLap).HasColumnType("datetime");
            e.Property(x => x.HinhThucTT).HasMaxLength(20);
            e.Property(x => x.GhiChu).HasMaxLength(200);
        });
        mb.Entity<ChiTietHoaDon>(e =>
        {
            e.ToTable("ChiTietHoaDon"); e.HasKey(x => x.MaCT);
            e.Property(x => x.DonGia).HasColumnType("decimal(18, 0)");
            e.HasOne(x => x.HoaDon).WithMany(h => h.ChiTietHoaDons).HasForeignKey(x => x.MaHD).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Mon).WithMany(m => m.ChiTietHoaDons).HasForeignKey(x => x.MaMon).OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
