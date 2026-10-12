IF DB_ID(N'QuanCaPhe') IS NULL CREATE DATABASE QuanCaPhe;
GO
USE QuanCaPhe;
GO
IF OBJECT_ID(N'dbo.ChiTietHoaDon') IS NOT NULL DROP TABLE dbo.ChiTietHoaDon;
IF OBJECT_ID(N'dbo.HoaDon') IS NOT NULL DROP TABLE dbo.HoaDon;
IF OBJECT_ID(N'dbo.Mon') IS NOT NULL DROP TABLE dbo.Mon;

CREATE TABLE dbo.Mon (
    MaMon   int PRIMARY KEY,
    TenMon  nvarchar(100) NOT NULL,
    NhomMon nvarchar(30)  NOT NULL,
    DonGia  decimal(18,0) NOT NULL CHECK (DonGia > 0)
);
CREATE TABLE dbo.HoaDon (
    MaHD       int IDENTITY(1,1) PRIMARY KEY,
    NgayLap    datetime NOT NULL,
    SoBan      int NOT NULL CHECK (SoBan BETWEEN 1 AND 10),
    HinhThucTT nvarchar(20) NOT NULL CHECK (HinhThucTT IN (N'Tiền mặt', N'Chuyển khoản')),
    GhiChu     nvarchar(200) NULL
);
CREATE TABLE dbo.ChiTietHoaDon (
    MaCT    int IDENTITY(1,1) PRIMARY KEY,
    MaHD    int NOT NULL CONSTRAINT FK_CT_HoaDon REFERENCES dbo.HoaDon(MaHD),
    MaMon   int NOT NULL CONSTRAINT FK_CT_Mon REFERENCES dbo.Mon(MaMon),
    SoLuong int NOT NULL CHECK (SoLuong > 0),
    DonGia  decimal(18,0) NOT NULL
);

INSERT INTO dbo.Mon (MaMon, TenMon, NhomMon, DonGia) VALUES
(1, N'Cà phê sữa đá',  N'Cà phê',         29000),
(2, N'Bạc xỉu',        N'Cà phê',         32000),
(3, N'Trà đào cam sả', N'Trà',            45000),
(4, N'Bánh tiramisu',  N'Bánh',           39000),
(5, N'Nước suối',      N'Nước đóng chai', 12000);

INSERT INTO dbo.HoaDon (NgayLap, SoBan, HinhThucTT, GhiChu) VALUES
('2026-10-09 08:15', 3, N'Tiền mặt', NULL),
('2026-10-09 09:40', 5, N'Chuyển khoản', NULL);

INSERT INTO dbo.ChiTietHoaDon (MaHD, MaMon, SoLuong, DonGia) VALUES
(1, 1, 2, 29000), (1, 3, 1, 45000), (1, 4, 1, 39000),   -- HD000001 = 142.000
(2, 2, 1, 32000), (2, 5, 2, 12000);                      -- HD000002 = 56.000
GO
