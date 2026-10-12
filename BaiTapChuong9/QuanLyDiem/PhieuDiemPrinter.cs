using System.Drawing.Printing;
using QuanLyDiem.Models;

namespace QuanLyDiem;

/// <summary>Chứa một PrintDocument vẽ phiếu báo điểm; mọi tọa độ tính theo e.MarginBounds.</summary>
public class PhieuDiemPrinter
{
    const string TenKhoa = "KHOA CÔNG NGHỆ THÔNG TIN";
    // đơn vị in = 1/100 inch
    const float RowH = 26f;        // chiều cao 1 dòng bảng
    const float HeaderP1 = 235f;   // phần đầu trang 1 (khoa, tiêu đề, thông tin SV)
    const float HeaderPn = 60f;    // phần đầu các trang sau
    const float FooterBlock = 190f;// khối "Điểm TB" + chỗ ký tên (chỉ trang cuối)
    const float PageNoH = 30f;     // dải "Trang x/y" cuối trang

    readonly PrintDocument _doc = new() { DocumentName = "Phiếu báo điểm" };
    bool _ready;

    SinhVien _sv = new();
    string _lop = "";
    List<DiemMon> _diem = new();
    decimal? _tb;

    // trạng thái in — đặt lại trong BeginPrint
    List<int> _pages = new();
    int _page, _idx;

    public PhieuDiemPrinter()
    {
        _doc.BeginPrint += OnBeginPrint;
        _doc.PrintPage += OnPrintPage;
    }

    public PrintDocument Document => _doc;

    public void SetData(SinhVien sv, string lop, IEnumerable<DiemMon> diem)
    {
        _sv = sv; _lop = lop; _diem = diem.ToList(); _tb = DiemHelper.DiemTB(_diem);
    }

    void EnsureReady()
    {
        if (PrinterSettings.InstalledPrinters.Count == 0)
            throw new InvalidPrinterException(new PrinterSettings());
        if (_ready) return;
        _doc.DefaultPageSettings.Margins = new Margins(80, 80, 80, 80);   // 0.8 inch
        _ready = true;
    }

    public void Preview(IWin32Window owner)
    {
        EnsureReady();
        using var dlg = new PrintPreviewDialog { Document = _doc, Width = 900, Height = 760, StartPosition = FormStartPosition.CenterParent };
        dlg.ShowDialog(owner);
    }

    public void Print(IWin32Window owner)
    {
        EnsureReady();
        using var dlg = new PrintDialog { Document = _doc, UseEXDialog = true };
        if (dlg.ShowDialog(owner) == DialogResult.OK) _doc.Print();
    }

    public void PageSetup(IWin32Window owner)
    {
        EnsureReady();
        using var dlg = new PageSetupDialog { Document = _doc };
        dlg.ShowDialog(owner);
    }

    // ---------- phân trang ----------
    int Capacity(float areaH, bool first, bool last) =>
        (int)Math.Floor((areaH - (first ? HeaderP1 : HeaderPn) - RowH - PageNoH - (last ? FooterBlock : 0)) / RowH);

    void OnBeginPrint(object sender, PrintEventArgs e)
    {
        _page = 0; _idx = 0; _pages = new List<int>();
        var ps = _doc.DefaultPageSettings;             // Bounds đã tính theo khổ ngang/dọc
        float areaH = ps.Bounds.Height - ps.Margins.Top - ps.Margins.Bottom;
        int n = _diem.Count, done = 0;
        while (true)
        {
            bool first = _pages.Count == 0;
            int remaining = n - done;
            if (remaining <= Capacity(areaH, first, true)) { _pages.Add(remaining); break; }
            int take = Math.Max(1, Capacity(areaH, first, false));
            _pages.Add(take); done += take;
        }
    }

    // ---------- vẽ ----------
    void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics!;
        var mb = e.MarginBounds;
        float x = mb.Left, y = mb.Top, w = mb.Width;
        bool first = _page == 0, last = _page == _pages.Count - 1;

        using var fKhoa = new Font("Times New Roman", 11, FontStyle.Regular);
        using var fTitle = new Font("Times New Roman", 20, FontStyle.Bold);
        using var fBody = new Font("Times New Roman", 11, FontStyle.Regular);
        using var fBold = new Font("Times New Roman", 11, FontStyle.Bold);
        using var fItalic = new Font("Times New Roman", 10, FontStyle.Italic);
        using var pen = new Pen(Color.Black, 1f);
        using var shade = new SolidBrush(Color.Gainsboro);
        using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        using var left = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

        if (first)
        {
            g.DrawString(TenKhoa, fKhoa, Brushes.Black, new RectangleF(x, y, w, 24), left);
            g.DrawString("PHIẾU BÁO ĐIỂM", fTitle, Brushes.Black, new RectangleF(x, y + 40, w, 44), center);
            float iy = y + 100;
            void Info(string label, string value)
            {
                g.DrawString(label, fBody, Brushes.Black, new RectangleF(x, iy, 110, 24), left);
                g.DrawString(value, fBold, Brushes.Black, new RectangleF(x + 110, iy, w - 110, 24), left);
                iy += 26;
            }
            Info("MSSV:", _sv.MaSV);
            Info("Họ tên:", _sv.HoTen);
            Info("Ngày sinh:", _sv.NgaySinh.ToString("dd/MM/yyyy"));
            Info("Lớp:", _lop);
        }
        else
        {
            g.DrawString($"PHIẾU BÁO ĐIỂM - {_sv.HoTen} ({_sv.MaSV})", fBold, Brushes.Black, new RectangleF(x, y, w, 28), left);
        }
        y += first ? HeaderP1 : HeaderPn;

        // ----- bảng điểm -----
        float wStt = w * 0.10f, wMon = w * 0.58f, wTc = w * 0.16f, wDiem = w - wStt - wMon - wTc;
        void Row(float ry, string a, string b, string c, string d, Font f, bool head)
        {
            var r1 = new RectangleF(x, ry, wStt, RowH);
            var r2 = new RectangleF(x + wStt, ry, wMon, RowH);
            var r3 = new RectangleF(r2.Right, ry, wTc, RowH);
            var r4 = new RectangleF(r3.Right, ry, wDiem, RowH);
            foreach (var r in new[] { r1, r2, r3, r4 })
            {
                if (head) g.FillRectangle(shade, r);
                g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            }
            g.DrawString(a, f, Brushes.Black, r1, center);
            g.DrawString(b, f, Brushes.Black, new RectangleF(r2.X + 6, r2.Y, r2.Width - 8, r2.Height), head ? center : left);
            g.DrawString(c, f, Brushes.Black, r3, center);
            g.DrawString(d, f, Brushes.Black, r4, center);
        }
        Row(y, "STT", "Môn học", "Số TC", "Điểm", fBold, true);
        y += RowH;
        int count = _pages[_page];
        for (int i = 0; i < count; i++)
        {
            var d = _diem[_idx + i];
            Row(y, (_idx + i + 1).ToString(), d.TenMon, d.SoTinChi.ToString(), d.Diem.ToString("0.0", DiemHelper.VN), fBody, false);
            y += RowH;
        }

        // ----- trang cuối: điểm TB + chữ ký -----
        if (last)
        {
            y += 18;
            string tb = _tb == null ? "Chưa có điểm" : DiemHelper.Fmt(_tb.Value);
            g.DrawString($"Điểm trung bình: {tb} - Xếp loại: {DiemHelper.XepLoai(_tb)}", fBold, Brushes.Black, new RectangleF(x, y, w, 28), center);
            y += 48;
            float c1 = x + w * 0.25f, c2 = x + w * 0.75f;
            foreach (var (cx, title) in new[] { (c1, "Giáo vụ khoa"), (c2, "Sinh viên") })
            {
                g.DrawString(title, fBold, Brushes.Black, new RectangleF(cx - w * 0.2f, y, w * 0.4f, 24), center);
                g.DrawString("(Ký, ghi rõ họ tên)", fItalic, Brushes.Black, new RectangleF(cx - w * 0.2f, y + 24, w * 0.4f, 22), center);
            }
        }

        // ----- số trang -----
        g.DrawString($"Trang {_page + 1}/{_pages.Count}", fItalic, Brushes.Black, new RectangleF(x, mb.Bottom - 22, w, 22), right);

        _idx += count;
        _page++;
        e.HasMorePages = !last;
    }
}
