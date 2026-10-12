# Bài tập chương 9 – Tạo Report & xuất tài liệu

3 project WinForms (.NET 9), EF Core Database First, async/await, try–catch + MessageBox.

| Project | Nội dung | Thư viện |
|---|---|---|
| `TiemTapHoa` (Câu 1) | CRUD + tìm kiếm (Timer 350 ms), bảng giá FastReport (DataTable) + PDF, Excel lưới + Excel tồn kho | EF Core, FastReport, ClosedXML |
| `QuanCaPhe` (Câu 2) | Hóa đơn chính–phụ, tìm nhiều điều kiện, FastReport (List<T> + tham số), PDF, In | EF Core, FastReport |
| `QuanLyDiem` (Câu 3) | Sinh viên + điểm, ĐTB trọng số, PrintDocument (xem trước / in / cài đặt trang / nhiều trang), Excel điểm lớp | EF Core, ClosedXML |

## Chạy
1. Mở SSMS (hoặc `sqlcmd -S "(localdb)\MSSQLLocalDB" -i Sql\<file>.sql`) và chạy script trong `Sql/` của từng project.
2. `dotnet restore` rồi chạy (hoặc mở `.csproj` bằng Visual Studio 2022+).
3. Khi nộp bài, scaffold lại cho đúng yêu cầu "Database First" (lệnh mẫu bên dưới) và thay thư mục `Models`:
   ```
   Scaffold-DbContext "Server=(localdb)\MSSQLLocalDB;Database=TiemTapHoa;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Force
   ```
   (Models trong bài viết tay theo đúng dạng scaffold; scaffold sẽ sinh thêm `OnModelCreatingPartial`, không ảnh hưởng code.)

## Lưu ý FastReport
- Mẫu `.frx` nằm ở `Reports/` và tự copy ra thư mục build. `DoublePass="true"` để `[TotalPages#]` hiện đúng.
- Code dùng `rp.Show()`, `rp.Prepare()`, `rp.Export(new PDFSimpleExport(), ...)`, `rp.PrintPrepared()`.
  Nếu môn dùng gói **FastReport .NET** (có cửa sổ Preview), đổi package + `using FastReport.Export.Pdf;` và `new PDFExport()` thay cho `PDFSimpleExport`.
  Gói OpenSource thuần có thể không có cửa sổ xem trước (`Show()`).
- Có thể mở `.frx` bằng FastReport Designer để chỉnh lại giao diện.

## Số liệu kiểm tra (theo đề)
- Câu 1: report 5 dòng; Excel tồn kho Tổng = 7.300.000 (công thức).
- Câu 2: HD000001 = 142.000; thêm 1 Nước suối → 154.000; HD000002 = 56.000.
- Câu 3: SV001 ĐTB 7,77 (Khá); SV002 8,50 (Giỏi); Excel CNTT01: AVERAGE = 8,14, SUM TC = 19.
