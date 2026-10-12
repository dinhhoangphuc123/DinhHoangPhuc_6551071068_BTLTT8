using System.Diagnostics;
using System.Globalization;
using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.EntityFrameworkCore;
using QuanCaPhe.Models;

namespace QuanCaPhe;

public class MainForm : Form
{
    static readonly CultureInfo VN = new("vi-VN");
    const string TenQuan = "QUÁN CÀ PHÊ GÓC PHỐ";

    // ----- tìm kiếm -----
    readonly DateTimePicker dtpTu = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    readonly DateTimePicker dtpDen = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    readonly ComboBox cboBanLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    readonly ComboBox cboHTLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly TextBox txtMonLoc = new() { Width = 160 };

    // ----- lưới trái -----
    readonly DataGridView dgvHD = MakeGrid();
    readonly Label lblDoanhThu = new() { Dock = DockStyle.Bottom, Height = 36, Font = new Font("Segoe UI", 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

    // ----- phải -----
    readonly DateTimePicker dtpNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 150 };
    readonly ComboBox cboBan = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
    readonly RadioButton rbTM = new() { Text = "Tiền mặt", Checked = true, AutoSize = true };
    readonly RadioButton rbCK = new() { Text = "Chuyển khoản", AutoSize = true };
    readonly TextBox txtGhiChu = new() { Multiline = true, Height = 50, Dock = DockStyle.Top, ScrollBars = ScrollBars.Vertical };
    readonly ComboBox cboMon = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    readonly NumericUpDown nudSL = new() { Minimum = 1, Maximum = 50, Value = 1, Width = 60 };
    readonly DataGridView dgvCT = MakeGrid();
    readonly Label lblTong = new() { Dock = DockStyle.Bottom, Height = 36, Font = new Font("Segoe UI", 15, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
    readonly GroupBox gbCT = new() { Text = "Chi tiết hóa đơn", Dock = DockStyle.Fill };

    bool _loading;
    int? _maHD;
    List<ChiTietRow> _ct = new();

    static DataGridView MakeGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        BackgroundColor = SystemColors.AppWorkspace
    };

    public MainForm()
    {
        Text = "Quán cà phê Góc Phố - Quản lý hóa đơn";
        Width = 1250; Height = 720; StartPosition = FormStartPosition.CenterScreen;

        dtpTu.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dtpDen.Value = DateTime.Today;
        cboBanLoc.Items.Add("Tất cả");
        for (int i = 1; i <= 10; i++) { cboBanLoc.Items.Add(i.ToString()); cboBan.Items.Add(i.ToString()); }
        cboBanLoc.SelectedIndex = 0; cboBan.SelectedIndex = 0;
        cboHTLoc.Items.AddRange(new object[] { "Tất cả", "Tiền mặt", "Chuyển khoản" }); cboHTLoc.SelectedIndex = 0;

        BuildLayout();
        BuildColumns();

        dgvHD.SelectionChanged += async (_, _) => { if (!_loading) await OnChonHoaDonAsync(); };
        Shown += async (_, _) => { await LoadMonAsync(); await LoadHoaDonAsync(); };
    }

    // ================= giao diện =================
    void BuildLayout()
    {
        // vùng tìm kiếm
        var gbTim = new GroupBox { Text = "Tìm kiếm hóa đơn", Dock = DockStyle.Top, Height = 78 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 4, 0, 0) };
        FlowLayoutPanel Cap(string t, Control c)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            p.Controls.Add(new Label { Text = t, AutoSize = true }); p.Controls.Add(c); return p;
        }
        var btnTim = new Button { Text = "Tìm", Width = 80, Height = 28, Margin = new Padding(20, 18, 0, 0) };
        btnTim.Click += async (_, _) => await LoadHoaDonAsync(_maHD);
        f.Controls.AddRange(new Control[] { Cap("Từ ngày", dtpTu), Cap("Đến ngày", dtpDen), Cap("Số bàn", cboBanLoc), Cap("Hình thức", cboHTLoc), Cap("Tên món", txtMonLoc), btnTim });
        gbTim.Controls.Add(f);

        // lưới trái
        var gbHD = new GroupBox { Text = "Danh sách hóa đơn", Dock = DockStyle.Fill };
        gbHD.Controls.Add(dgvHD); gbHD.Controls.Add(lblDoanhThu);

        // vùng nhập + lưới chi tiết (phải)
        var nhap = new TableLayoutPanel { Dock = DockStyle.Top, Height = 170, ColumnCount = 1, RowCount = 3 };
        var r1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        r1.Controls.AddRange(new Control[] { dtpNgay, new Label { Text = "Số bàn:", AutoSize = true, Margin = new Padding(10, 6, 0, 0) }, cboBan, rbTM, rbCK });
        var r2 = new Panel { Dock = DockStyle.Fill, Height = 56 };
        r2.Controls.Add(txtGhiChu); r2.Controls.Add(new Label { Text = "Ghi chú:", Dock = DockStyle.Top, Height = 16 });
        var btnThem = new Button { Text = "Thêm món", Width = 90, Height = 28 };
        btnThem.Click += async (_, _) => await ThemMonAsync();
        var r3 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        r3.Controls.AddRange(new Control[] { new Label { Text = "Chọn món:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, cboMon, new Label { Text = "SL:", AutoSize = true, Margin = new Padding(8, 6, 0, 0) }, nudSL, btnThem });
        nhap.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        nhap.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        nhap.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        nhap.Controls.Add(r1, 0, 0); nhap.Controls.Add(r2, 0, 1); nhap.Controls.Add(r3, 0, 2);
        gbCT.Controls.Add(dgvCT); gbCT.Controls.Add(lblTong); gbCT.Controls.Add(nhap);
        dgvCT.BringToFront();

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 560 };
        split.Panel1.Controls.Add(gbHD); split.Panel2.Controls.Add(gbCT);

        // thanh nút
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6, 6, 0, 0) };
        Button B(string t, Func<Task> h) { var b = new Button { Text = t, Width = 105, Height = 30 }; b.Click += async (_, _) => await h(); return b; }
        bar.Controls.AddRange(new Control[]
        {
            B("Hóa đơn mới", HoaDonMoiAsync), B("Sửa hóa đơn", SuaHoaDonAsync), B("Xóa hóa đơn", XoaHoaDonAsync),
            B("Sửa SL", SuaSLAsync), B("Xóa món", XoaMonAsync),
            B("Xem hóa đơn", XemAsync), B("Xuất PDF", XuatPdfAsync), B("In", InAsync),
        });

        Controls.Add(split); Controls.Add(gbTim); Controls.Add(bar);
        split.BringToFront();
    }

    void BuildColumns()
    {
        dgvHD.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã HD", DataPropertyName = "MaHienThi", Width = 80 });
        dgvHD.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày lập", DataPropertyName = "NgayLap", Width = 125, DefaultCellStyle = new() { Format = "dd/MM/yyyy HH:mm" } });
        dgvHD.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số bàn", DataPropertyName = "SoBan", Width = 55 });
        dgvHD.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hình thức TT", DataPropertyName = "HinhThucTT", Width = 100 });
        dgvHD.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng tiền", DataPropertyName = "TongTien", Width = 100, DefaultCellStyle = new() { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });

        dgvCT.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", DataPropertyName = "STT", Width = 45 });
        dgvCT.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên món", DataPropertyName = "TenMon", Width = 200 });
        dgvCT.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SL", DataPropertyName = "SoLuong", Width = 45 });
        dgvCT.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đơn giá", DataPropertyName = "DonGia", Width = 90, DefaultCellStyle = new() { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvCT.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thành tiền", DataPropertyName = "ThanhTien", Width = 100, DefaultCellStyle = new() { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgvCT.SelectionChanged += (_, _) =>
        {
            if (dgvCT.CurrentRow?.DataBoundItem is ChiTietRow r)
            {
                nudSL.Value = Math.Clamp(r.SoLuong, 1, 50);
            }
        };
    }

    // ================= nạp dữ liệu =================
    async Task LoadMonAsync()
    {
        try
        {
            using var db = new QuanCaPheContext();
            var mons = await db.Mons.AsNoTracking().OrderBy(m => m.MaMon).ToListAsync();
            cboMon.Items.Clear();
            foreach (var m in mons)
                cboMon.Items.Add(new MonItem { MaMon = m.MaMon, DonGia = m.DonGia, Hien = $"{m.TenMon} - {m.DonGia.ToString("N0", VN)}" });
            if (cboMon.Items.Count > 0) cboMon.SelectedIndex = 0;
        }
        catch (Exception ex) { Loi("Không tải được danh sách món", ex); }
    }

    async Task LoadHoaDonAsync(int? chon = null)
    {
        try
        {
            using var db = new QuanCaPheContext();
            var tu = dtpTu.Value.Date; var den = dtpDen.Value.Date.AddDays(1);
            var q = db.HoaDons.AsNoTracking().Where(h => h.NgayLap >= tu && h.NgayLap < den);
            if (cboBanLoc.SelectedIndex > 0) { int ban = int.Parse(cboBanLoc.Text); q = q.Where(h => h.SoBan == ban); }
            if (cboHTLoc.SelectedIndex > 0) { var ht = cboHTLoc.Text; q = q.Where(h => h.HinhThucTT == ht); }
            var kw = txtMonLoc.Text.Trim();
            if (kw.Length > 0) q = q.Where(h => h.ChiTietHoaDons.Any(c => c.Mon.TenMon.Contains(kw)));   // lọc qua bảng chi tiết + bảng món

            var rows = await q.OrderBy(h => h.MaHD).Select(h => new HoaDonRow
            {
                MaHD = h.MaHD, NgayLap = h.NgayLap, SoBan = h.SoBan, HinhThucTT = h.HinhThucTT, GhiChu = h.GhiChu,
                TongTien = h.ChiTietHoaDons.Sum(c => c.SoLuong * c.DonGia)
            }).ToListAsync();

            _loading = true;
            dgvHD.DataSource = rows;
            _loading = false;
            lblDoanhThu.Text = $"Tổng doanh thu đang hiển thị: {rows.Sum(r => r.TongTien).ToString("N0", VN)} đ";

            DataGridViewRow target = null;
            if (chon != null) target = dgvHD.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => ((HoaDonRow)r.DataBoundItem).MaHD == chon);
            target ??= dgvHD.Rows.Cast<DataGridViewRow>().FirstOrDefault();
            if (target != null) { dgvHD.ClearSelection(); target.Selected = true; dgvHD.CurrentCell = target.Cells[0]; }
            await OnChonHoaDonAsync();
        }
        catch (Exception ex) { _loading = false; Loi("Không tải được danh sách hóa đơn", ex); }
    }

    HoaDonRow ChonHD() => dgvHD.CurrentRow?.DataBoundItem as HoaDonRow;

    async Task OnChonHoaDonAsync()
    {
        var h = ChonHD();
        if (h == null)
        {
            _maHD = null; _ct = new(); dgvCT.DataSource = _ct; lblTong.Text = "Tổng cộng: 0 đ"; gbCT.Text = "Chi tiết hóa đơn"; return;
        }
        _maHD = h.MaHD;
        gbCT.Text = "Chi tiết hóa đơn " + h.MaHienThi;
        dtpNgay.Value = h.NgayLap;
        cboBan.SelectedItem = h.SoBan.ToString();
        rbTM.Checked = h.HinhThucTT == "Tiền mặt"; rbCK.Checked = h.HinhThucTT == "Chuyển khoản";
        txtGhiChu.Text = h.GhiChu ?? "";
        await LoadChiTietAsync(h.MaHD);
    }

    async Task LoadChiTietAsync(int maHD)
    {
        try
        {
            using var db = new QuanCaPheContext();
            var list = await db.ChiTietHoaDons.AsNoTracking().Where(c => c.MaHD == maHD).OrderBy(c => c.MaCT)
                .Select(c => new ChiTietRow { MaCT = c.MaCT, TenMon = c.Mon.TenMon, SoLuong = c.SoLuong, DonGia = c.DonGia }).ToListAsync();
            int i = 1;
            foreach (var r in list) { r.STT = i++; r.ThanhTien = r.SoLuong * r.DonGia; }
            _ct = list;
            dgvCT.DataSource = list;
            lblTong.Text = $"Tổng cộng: {list.Sum(r => r.ThanhTien).ToString("N0", VN)} đ";
        }
        catch (Exception ex) { Loi("Không tải được chi tiết hóa đơn", ex); }
    }

    // ================= hóa đơn =================
    bool KiemTraNhap()
    {
        if (cboBan.SelectedIndex < 0) { Canh("Hãy chọn số bàn."); return false; }
        return true;
    }

    async Task HoaDonMoiAsync()
    {
        if (!KiemTraNhap()) return;
        try
        {
            using var db = new QuanCaPheContext();
            var ngay = dtpNgay.Value; ngay = new DateTime(ngay.Year, ngay.Month, ngay.Day, ngay.Hour, ngay.Minute, 0);
            var h = new HoaDon
            {
                NgayLap = ngay, SoBan = int.Parse(cboBan.Text), HinhThucTT = rbCK.Checked ? "Chuyển khoản" : "Tiền mặt",
                GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim()
            };
            db.HoaDons.Add(h);
            await db.SaveChangesAsync();
            // nới bộ lọc để hóa đơn mới chắc chắn hiện ra
            if (ngay.Date < dtpTu.Value.Date) dtpTu.Value = ngay.Date;
            if (ngay.Date > dtpDen.Value.Date) dtpDen.Value = ngay.Date;
            cboBanLoc.SelectedIndex = 0; cboHTLoc.SelectedIndex = 0; txtMonLoc.Clear();
            await LoadHoaDonAsync(h.MaHD);
        }
        catch (Exception ex) { Loi("Không tạo được hóa đơn", ex); }
    }

    async Task SuaHoaDonAsync()
    {
        if (_maHD == null) { Canh("Hãy chọn một hóa đơn để sửa."); return; }
        if (!KiemTraNhap()) return;
        try
        {
            using var db = new QuanCaPheContext();
            var h = await db.HoaDons.FindAsync(_maHD.Value);
            if (h == null) { Canh("Hóa đơn không còn tồn tại."); await LoadHoaDonAsync(); return; }
            var ngay = dtpNgay.Value;
            h.NgayLap = new DateTime(ngay.Year, ngay.Month, ngay.Day, ngay.Hour, ngay.Minute, 0);
            h.SoBan = int.Parse(cboBan.Text);
            h.HinhThucTT = rbCK.Checked ? "Chuyển khoản" : "Tiền mặt";
            h.GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();
            await db.SaveChangesAsync();
            await LoadHoaDonAsync(h.MaHD);
        }
        catch (Exception ex) { Loi("Không sửa được hóa đơn", ex); }
    }

    async Task XoaHoaDonAsync()
    {
        var h = ChonHD();
        if (h == null) { Canh("Hãy chọn một hóa đơn để xóa."); return; }
        if (MessageBox.Show($"Xóa hóa đơn {h.MaHienThi} và toàn bộ món trong hóa đơn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new QuanCaPheContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.ChiTietHoaDons.Where(c => c.MaHD == h.MaHD).ExecuteDeleteAsync();   // xóa chi tiết trước
            await db.HoaDons.Where(x => x.MaHD == h.MaHD).ExecuteDeleteAsync();          // rồi mới xóa hóa đơn
            await tx.CommitAsync();
            await LoadHoaDonAsync();
        }
        catch (Exception ex) { Loi("Không xóa được hóa đơn", ex); }
    }

    // ================= chi tiết =================
    async Task ThemMonAsync()
    {
        if (_maHD == null) { Canh("Hãy chọn (hoặc tạo) một hóa đơn trước."); return; }
        if (cboMon.SelectedItem is not MonItem mi) { Canh("Hãy chọn món."); return; }
        try
        {
            using var db = new QuanCaPheContext();
            var mon = await db.Mons.FindAsync(mi.MaMon);                     // đơn giá lấy từ bảng Mon tại thời điểm thêm
            if (mon == null) { Canh("Món không còn tồn tại."); return; }
            db.ChiTietHoaDons.Add(new ChiTietHoaDon { MaHD = _maHD.Value, MaMon = mon.MaMon, SoLuong = (int)nudSL.Value, DonGia = mon.DonGia });
            await db.SaveChangesAsync();
            await LoadHoaDonAsync(_maHD);
        }
        catch (Exception ex) { Loi("Không thêm được món", ex); }
    }

    async Task SuaSLAsync()
    {
        if (dgvCT.CurrentRow?.DataBoundItem is not ChiTietRow r) { Canh("Hãy chọn một dòng món để sửa số lượng."); return; }
        try
        {
            using var db = new QuanCaPheContext();
            var ct = await db.ChiTietHoaDons.FindAsync(r.MaCT);
            if (ct == null) { Canh("Dòng này không còn tồn tại."); await LoadHoaDonAsync(_maHD); return; }
            ct.SoLuong = (int)nudSL.Value;
            await db.SaveChangesAsync();
            await LoadHoaDonAsync(_maHD);
        }
        catch (Exception ex) { Loi("Không sửa được số lượng", ex); }
    }

    async Task XoaMonAsync()
    {
        if (dgvCT.CurrentRow?.DataBoundItem is not ChiTietRow r) { Canh("Hãy chọn một dòng món để xóa."); return; }
        if (MessageBox.Show($"Xóa món \"{r.TenMon}\" khỏi hóa đơn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new QuanCaPheContext();
            await db.ChiTietHoaDons.Where(c => c.MaCT == r.MaCT).ExecuteDeleteAsync();
            await LoadHoaDonAsync(_maHD);
        }
        catch (Exception ex) { Loi("Không xóa được món", ex); }
    }

    // ================= FastReport (List<T> + tham số) =================
    Report TaoReport()
    {
        var h = ChonHD();
        if (h == null || _maHD == null) { Canh("Hãy chọn một hóa đơn."); return null; }
        if (_ct.Count == 0) { Canh("Hóa đơn chưa có món nào, không thể in."); return null; }

        var data = _ct.Select(c => new ChiTietIn { STT = c.STT, TenMon = c.TenMon, SoLuong = c.SoLuong, DonGia = c.DonGia, ThanhTien = c.ThanhTien }).ToList();
        var path = Path.Combine(AppContext.BaseDirectory, "Reports", "HoaDon.frx");
        if (!File.Exists(path)) throw new FileNotFoundException("Không tìm thấy mẫu report.", path);

        var rp = new Report();
        rp.Load(path);
        rp.RegisterData(data, "ChiTiet");
        rp.GetDataSource("ChiTiet").Enabled = true;
        ((DataBand)rp.FindObject("Data1")).DataSource = rp.GetDataSource("ChiTiet");
        rp.SetParameterValue("TenQuan", TenQuan);
        rp.SetParameterValue("SoHD", h.MaHienThi);
        rp.SetParameterValue("NgayLap", h.NgayLap.ToString("dd/MM/yyyy HH:mm"));
        rp.SetParameterValue("SoBan", h.SoBan.ToString());
        rp.SetParameterValue("HinhThuc", h.HinhThucTT);
        return rp;
    }

    async Task XemAsync()
    {
        try { using var rp = TaoReport(); rp?.Show(); }
        catch (Exception ex) { Loi("Không xem được hóa đơn", ex); }
        await Task.CompletedTask;
    }

    async Task XuatPdfAsync()
    {
        try
        {
            using var rp = TaoReport();
            if (rp == null) return;
            using var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"HoaDon_{ChonHD().MaHienThi}.pdf" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            rp.Prepare();
            using (var pdf = new PDFSimpleExport()) rp.Export(pdf, sfd.FileName);
            if (MessageBox.Show("Đã xuất PDF. Mở file ngay?", "Hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (IOException) { Canh("Không ghi được file PDF. Có thể file đang mở, hãy đóng file rồi thử lại."); }
        catch (Exception ex) { Loi("Không xuất được PDF", ex); }
        await Task.CompletedTask;
    }

    async Task InAsync()
    {
        try
        {
            using var rp = TaoReport();
            if (rp == null) return;
            rp.Prepare();
            rp.PrintPrepared();       // hộp thoại in của FastReport
        }
        catch (Exception ex) { Loi("Không in được hóa đơn", ex); }
        await Task.CompletedTask;
    }

    // ================= tiện ích =================
    static void Canh(string m) => MessageBox.Show(m, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    static void Loi(string m, Exception ex) => MessageBox.Show($"{m}.\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
