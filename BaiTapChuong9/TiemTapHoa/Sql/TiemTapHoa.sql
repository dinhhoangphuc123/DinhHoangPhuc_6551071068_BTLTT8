IF DB_ID(N'TiemTapHoa') IS NULL CREATE DATABASE TiemTapHoa;
GO
USE TiemTapHoa;
GO
IF OBJECT_ID(N'dbo.HangHoa') IS NOT NULL DROP TABLE dbo.HangHoa;
CREATE TABLE dbo.HangHoa (
    MaHang        int IDENTITY(1,1) PRIMARY KEY,
    TenHang       nvarchar(100) NOT NULL,
    LoaiHang      nvarchar(50)  NULL,
    DonVi         nvarchar(20)  NULL,
    GiaBan        decimal(18,0) NOT NULL CONSTRAINT CK_HangHoa_Gia CHECK (GiaBan > 0),
    SoLuongTon    int NOT NULL CONSTRAINT DF_HangHoa_Ton DEFAULT 0 CONSTRAINT CK_HangHoa_Ton CHECK (SoLuongTon >= 0),
    NgayNhap      datetime NOT NULL,
    ConKinhDoanh  bit NOT NULL CONSTRAINT DF_HangHoa_KD DEFAULT 1
);
INSERT INTO dbo.HangHoa (TenHang, LoaiHang, DonVi, GiaBan, SoLuongTon, NgayNhap, ConKinhDoanh) VALUES
(N'Nước suối Aquafina 500ml',  N'Đồ uống',        N'Chai', 6000,  120, '2026-10-02', 1),
(N'Mì Hảo Hảo tôm chua cay',   N'Thực phẩm khô',  N'Gói',  4500,  200, '2026-10-02', 1),
(N'Dầu ăn Tường An 1L',        N'Gia vị',         N'Chai', 48000, 35,  '2026-10-05', 1),
(N'Bột giặt OMO 800g',         N'Đồ gia dụng',    N'Túi',  52000, 40,  '2026-10-05', 1),
(N'Sữa tươi Vinamilk 1L',      N'Đồ uống',        N'Hộp',  32000, 60,  '2026-10-08', 1),
(N'Nước mắm Nam Ngư 500ml',    N'Gia vị',         N'Chai', 38000, 25,  '2026-10-01', 0);
GO
