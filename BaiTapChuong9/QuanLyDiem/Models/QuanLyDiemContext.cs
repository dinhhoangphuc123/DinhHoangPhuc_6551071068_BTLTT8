using Microsoft.EntityFrameworkCore;

namespace QuanLyDiem.Models;

public class QuanLyDiemContext : DbContext
{
    public virtual DbSet<Lop> Lops { get; set; }
    public virtual DbSet<SinhVien> SinhViens { get; set; }
    public virtual DbSet<DiemMon> DiemMons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o) =>
        o.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=QuanLyDiem;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Lop>(e =>
        {
            e.ToTable("Lop"); e.HasKey(x => x.MaLop);
            e.Property(x => x.MaLop).HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.TenLop).HasMaxLength(100);
        });
        mb.Entity<SinhVien>(e =>
        {
            e.ToTable("SinhVien"); e.HasKey(x => x.MaSV);
            e.Property(x => x.MaSV).HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.MaLop).HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.HoTen).HasMaxLength(100);
            e.Property(x => x.GioiTinh).HasMaxLength(3);
            e.Property(x => x.NgaySinh).HasColumnType("datetime");
            e.HasOne(x => x.MaLopNavigation).WithMany(l => l.SinhViens).HasForeignKey(x => x.MaLop).OnDelete(DeleteBehavior.ClientSetNull);
        });
        mb.Entity<DiemMon>(e =>
        {
            e.ToTable("DiemMon"); e.HasKey(x => x.MaDiem);
            e.Property(x => x.MaSV).HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.TenMon).HasMaxLength(100);
            e.Property(x => x.Diem).HasColumnType("decimal(4, 2)");
            e.HasOne(x => x.MaSVNavigation).WithMany(s => s.DiemMons).HasForeignKey(x => x.MaSV).OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
