IF DB_ID(N'QuanLyDiem') IS NULL CREATE DATABASE QuanLyDiem;
GO
USE QuanLyDiem;
GO
IF OBJECT_ID(N'dbo.DiemMon') IS NOT NULL DROP TABLE dbo.DiemMon;
IF OBJECT_ID(N'dbo.SinhVien') IS NOT NULL DROP TABLE dbo.SinhVien;
IF OBJECT_ID(N'dbo.Lop') IS NOT NULL DROP TABLE dbo.Lop;

CREATE TABLE dbo.Lop (
    MaLop  varchar(10) PRIMARY KEY,
    TenLop nvarchar(100) NOT NULL
);
CREATE TABLE dbo.SinhVien (
    MaSV     varchar(10) PRIMARY KEY,
    HoTen    nvarchar(100) NOT NULL,
    NgaySinh datetime NOT NULL,
    GioiTinh nvarchar(3) NOT NULL CHECK (GioiTinh IN (N'Nam', N'Nữ')),
    MaLop    varchar(10) NOT NULL CONSTRAINT FK_SV_Lop REFERENCES dbo.Lop(MaLop)
);
CREATE TABLE dbo.DiemMon (
    MaDiem   int IDENTITY(1,1) PRIMARY KEY,
    MaSV     varchar(10) NOT NULL CONSTRAINT FK_Diem_SV REFERENCES dbo.SinhVien(MaSV),
    TenMon   nvarchar(100) NOT NULL,
    SoTinChi int NOT NULL CHECK (SoTinChi BETWEEN 1 AND 6),
    Diem     decimal(4,2) NOT NULL CHECK (Diem BETWEEN 0 AND 10)
);

INSERT INTO dbo.Lop (MaLop, TenLop) VALUES
('CNTT01', N'Công nghệ thông tin K01'),
('KT01',   N'Kế toán K01');

INSERT INTO dbo.SinhVien (MaSV, HoTen, NgaySinh, GioiTinh, MaLop) VALUES
('SV001', N'Nguyễn Minh Anh', '2005-03-15', N'Nữ',  'CNTT01'),
('SV002', N'Trần Quốc Bảo',   '2005-07-22', N'Nam', 'CNTT01'),
('SV003', N'Lê Thị Cẩm Tú',   '2005-11-09', N'Nữ',  'KT01');

INSERT INTO dbo.DiemMon (MaSV, TenMon, SoTinChi, Diem) VALUES
('SV001', N'Lập trình trực quan',     3, 8.5),
('SV001', N'Cơ sở dữ liệu',           3, 7.5),
('SV001', N'Tiếng Anh chuyên ngành',  2, 9.0),
('SV001', N'Toán rời rạc',            3, 6.5),
('SV002', N'Lập trình trực quan',     3, 9.0),
('SV002', N'Cơ sở dữ liệu',           3, 8.0),
('SV002', N'Tiếng Anh chuyên ngành',  2, 8.5);
GO
