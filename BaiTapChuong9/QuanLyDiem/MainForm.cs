using System.Diagnostics;
using System.Drawing.Printing;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuanLyDiem.Models;

namespace QuanLyDiem;

public class MainForm : Form
{
    class LopItem
    {
        public string MaLop { get; init; }          // null = "Tất cả"
        public string Text { get; init; } = "";
        public override string ToString() => Text;
    }

    // ----- trái / giữa -----
    readonly ListBox lstLop = new() { Dock = DockStyle.Fill };
    readonly TextBox txtTim = new() { Dock = DockStyle.Top, PlaceholderText = "Tìm MSSV / họ tên" };
    readonly DataGridView dgvSV = MakeGrid();

    // ----- phải: thông tin sinh viên -----
    readonly TextBox txtMSSV = new() { Width = 170 };
    readonly TextBox txtHoTen = new() { Width = 170 };
    readonly DateTimePicker dtpNgaySinh = new() { Format = DateTimePickerFormat.Short, Width = 170, Value = new DateTime(2005, 1, 1) };
    readonly RadioButton rbNam = new() { Text = "Nam", Checked = true, AutoSize = true };
    readonly RadioButton rbNu = new() { Text = "Nữ", AutoSize = true };
    readonly ComboBox cboLop = new() { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };

    // ----- dưới: điểm -----
    readonly DataGridView dgvDiem = MakeGrid();
    readonly TextBox txtMon = new() { Width = 220 };
    readonly NumericUpDown nudTC = new() { Minimum = 1, Maximum = 6, Value = 3, Width = 60 };
    readonly NumericUpDown nudDiem = new() { Minimum = 0, Maximum = 10, DecimalPlaces = 1, Increment = 0.1m, Value = 5, Width = 70 };
    readonly Label lblTC = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Text = "Tổng tín chỉ: 0" };
    readonly Label lblTB = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Text = "Điểm TB: --" };
    readonly Label lblXL = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Text = "Xếp loại: --" };

    readonly PhieuDiemPrinter _printer = new();
    List<DiemRow> _diem = new();
    bool _loading;

    static DataGridView MakeGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        BackgroundColor = SystemColors.AppWorkspace
    };

    public MainForm()
    {
        Text = "Quản lý sinh viên và điểm";
        Width = 1200; Height = 760; StartPosition = FormStartPosition.CenterScreen;
        BuildLayout(); BuildColumns();

        lstLop.SelectedIndexChanged += async (_, _) => { if (!_loading) await LoadSinhVienAsync(); };
        txtTim.TextChanged += async (_, _) => { if (!_loading) await LoadSinhVienAsync(); };
        dgvSV.SelectionChanged += async (_, _) => { if (!_loading) await OnChonSinhVienAsync(); };
        dgvDiem.SelectionChanged += (_, _) =>
        {
            if (dgvDiem.CurrentRow?.DataBoundItem is DiemRow r)
            { txtMon.Text = r.TenMon; nudTC.Value = Math.Clamp(r.SoTinChi, 1, 6); nudDiem.Value = Math.Clamp(r.Diem, 0, 10); }
        };
        Shown += async (_, _) => await LoadLopAsync();
    }

    // ================= giao diện =================
    void BuildLayout()
    {
        // ---- cột trái: Lớp
        var gbLop = new GroupBox { Text = "Lớp", Dock = DockStyle.Left, Width = 230 };
        gbLop.Controls.Add(lstLop);

        // ---- giữa: tìm kiếm + lưới SV
        var mid = new Panel { Dock = DockStyle.Fill };
        mid.Controls.Add(dgvSV); mid.Controls.Add(txtTim); dgvSV.BringToFront();

        // ---- phải: thông tin sinh viên
        var gbInfo = new GroupBox { Text = "Thông tin sinh viên", Dock = DockStyle.Right, Width = 300 };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8) };
        FlowLayoutPanel Row(string cap, Control c)
        {
            var p = new FlowLayoutPanel { AutoSize = true };
            p.Controls.Add(new Label { Text = cap, Width = 70, Margin = new Padding(0, 6, 0, 0) }); p.Controls.Add(c); return p;
        }
        var gt = new FlowLayoutPanel { AutoSize = true }; gt.Controls.AddRange(new Control[] { new Label { Text = "", Width = 70 }, rbNam, rbNu });
        var bSV = new FlowLayoutPanel { AutoSize = true };
        bSV.Controls.AddRange(new Control[] { Btn("Thêm", ThemSVAsync, 75), Btn("Sửa", SuaSVAsync, 75), Btn("Xóa", XoaSVAsync, 75), Btn("Làm mới", async () => { LamMoiSV(); await Task.CompletedTask; }, 75) });
        f.Controls.AddRange(new Control[] { Row("MSSV", txtMSSV), Row("Họ tên", txtHoTen), Row("Ngày sinh", dtpNgaySinh), gt, Row("Lớp", cboLop), bSV });
        gbInfo.Controls.Add(f);

        var top = new Panel { Dock = DockStyle.Fill };
        top.Controls.Add(mid); top.Controls.Add(gbInfo); top.Controls.Add(gbLop);
        mid.BringToFront();

        // ---- dưới: điểm
        var gbDiem = new GroupBox { Text = "Điểm của sinh viên", Dock = DockStyle.Bottom, Height = 270 };
        dgvDiem.Dock = DockStyle.Left; dgvDiem.Width = 520;
        var right = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10, 6, 0, 0) };
        var r1 = new FlowLayoutPanel { AutoSize = true };
        r1.Controls.AddRange(new Control[] { new Label { Text = "Tên môn", Width = 60, Margin = new Padding(0, 6, 0, 0) }, txtMon,
            new Label { Text = "Số TC", AutoSize = true, Margin = new Padding(10, 6, 0, 0) }, nudTC,
            new Label { Text = "Điểm", AutoSize = true, Margin = new Padding(10, 6, 0, 0) }, nudDiem });
        var r2 = new FlowLayoutPanel { AutoSize = true };
        r2.Controls.AddRange(new Control[] { Btn("Thêm môn", ThemMonAsync, 95), Btn("Sửa điểm", SuaDiemAsync, 95), Btn("Xóa môn", XoaMonAsync, 95) });
        var r3 = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
        lblTC.Margin = lblTB.Margin = lblXL.Margin = new Padding(0, 0, 24, 0);
        r3.Controls.AddRange(new Control[] { lblTC, lblTB, lblXL });
        right.Controls.AddRange(new Control[] { r1, r2, r3 });
        gbDiem.Controls.Add(right); gbDiem.Controls.Add(dgvDiem);

        // ---- thanh nút in / excel
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6, 6, 0, 0) };
        bar.Controls.AddRange(new Control[]
        {
            Btn("Xem trước phiếu", XemTruocAsync, 130), Btn("In...", InAsync, 90),
            Btn("Cài đặt trang...", CaiDatTrangAsync, 130), Btn("Xuất Excel điểm lớp", XuatExcelAsync, 160),
        });

        Controls.Add(top); Controls.Add(gbDiem); Controls.Add(bar);
        top.BringToFront();
    }

    static Button Btn(string text, Func<Task> h, int w)
    {
        var b = new Button { Text = text, Width = w, Height = 30 };
        b.Click += async (_, _) => await h();
        return b;
    }

    void BuildColumns()
    {
        dgvSV.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MSSV", DataPropertyName = "MaSV", Width = 70 });
        dgvSV.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ tên", DataPropertyName = "HoTen", Width = 160 });
        dgvSV.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày sinh", DataPropertyName = "NgaySinh", Width = 90, DefaultCellStyle = new() { Format = "dd/MM/yyyy" } });
        dgvSV.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giới tính", DataPropertyName = "GioiTinh", Width = 70 });
        dgvSV.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Lớp", DataPropertyName = "MaLop", Width = 80 });

        dgvDiem.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", DataPropertyName = "STT", Width = 45 });
        dgvDiem.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Môn học", DataPropertyName = "TenMon", Width = 260 });
        dgvDiem.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số TC", DataPropertyName = "SoTinChi", Width = 60 });
        dgvDiem.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Điểm", DataPropertyName = "Diem", Width = 70, DefaultCellStyle = new() { Format = "0.0" } });
    }

    // ================= nạp dữ liệu =================
    async Task LoadLopAsync()
    {
        try
        {
            using var db = new QuanLyDiemContext();
            var lops = await db.Lops.AsNoTracking().OrderBy(l => l.MaLop).ToListAsync();
            _loading = true;
            lstLop.Items.Clear(); cboLop.Items.Clear();
            lstLop.Items.Add(new LopItem { MaLop = null, Text = "Tất cả" });
            foreach (var l in lops) { lstLop.Items.Add(new LopItem { MaLop = l.MaLop, Text = $"{l.MaLop} - {l.TenLop}" }); cboLop.Items.Add(l.MaLop); }
            lstLop.SelectedIndex = 0;
            if (cboLop.Items.Count > 0) cboLop.SelectedIndex = 0;
            _loading = false;
            await LoadSinhVienAsync();
        }
        catch (Exception ex) { _loading = false; Loi("Không tải được danh sách lớp", ex); }
    }

    string LopDangChon => (lstLop.SelectedItem as LopItem)?.MaLop;
    SinhVien ChonSV() => dgvSV.CurrentRow?.DataBoundItem as SinhVien;

    async Task LoadSinhVienAsync(string chonMa = null)
    {
        try
        {
            using var db = new QuanLyDiemContext();
            var q = db.SinhViens.AsNoTracking().AsQueryable();
            var lop = LopDangChon;
            if (lop != null) q = q.Where(s => s.MaLop == lop);
            var list = await q.OrderBy(s => s.MaSV).ToListAsync();
            var kw = txtTim.Text.Trim();
            if (kw.Length > 0)      // không phân biệt hoa thường, lọc theo MSSV hoặc họ tên
                list = list.Where(s => s.MaSV.Contains(kw, StringComparison.OrdinalIgnoreCase) || s.HoTen.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();

            _loading = true;
            dgvSV.DataSource = list;
            _loading = false;
            var target = chonMa == null ? null : dgvSV.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => ((SinhVien)r.DataBoundItem).MaSV == chonMa);
            target ??= dgvSV.Rows.Cast<DataGridViewRow>().FirstOrDefault();
            if (target != null) { dgvSV.ClearSelection(); target.Selected = true; dgvSV.CurrentCell = target.Cells[0]; }
            await OnChonSinhVienAsync();
        }
        catch (Exception ex) { _loading = false; Loi("Không tải được danh sách sinh viên", ex); }
    }

    async Task OnChonSinhVienAsync()
    {
        var sv = ChonSV();
        if (sv == null) { _diem = new(); dgvDiem.DataSource = _diem; CapNhatTong(); return; }
        txtMSSV.Text = sv.MaSV; txtHoTen.Text = sv.HoTen;
        dtpNgaySinh.Value = sv.NgaySinh < dtpNgaySinh.MinDate ? dtpNgaySinh.MinDate : sv.NgaySinh;
        rbNam.Checked = sv.GioiTinh == "Nam"; rbNu.Checked = sv.GioiTinh == "Nữ";
        cboLop.SelectedItem = sv.MaLop;
        await LoadDiemAsync(sv.MaSV);
    }

    async Task LoadDiemAsync(string maSV)
    {
        try
        {
            using var db = new QuanLyDiemContext();
            var list = await db.DiemMons.AsNoTracking().Where(d => d.MaSV == maSV).OrderBy(d => d.MaDiem)
                .Select(d => new DiemRow { MaDiem = d.MaDiem, TenMon = d.TenMon, SoTinChi = d.SoTinChi, Diem = d.Diem }).ToListAsync();
            for (int i = 0; i < list.Count; i++) list[i].STT = i + 1;
            _diem = list;
            dgvDiem.DataSource = list;
            CapNhatTong();
        }
        catch (Exception ex) { Loi("Không tải được điểm", ex); }
    }

    List<DiemMon> DiemEntities() => _diem.Select(r => new DiemMon { MaDiem = r.MaDiem, TenMon = r.TenMon, SoTinChi = r.SoTinChi, Diem = r.Diem }).ToList();

    void CapNhatTong()
    {
        var ds = DiemEntities();
        var tb = DiemHelper.DiemTB(ds);
        lblTC.Text = $"Tổng tín chỉ: {ds.Sum(d => d.SoTinChi)}";
        lblTB.Text = "Điểm TB: " + (tb == null ? "--" : DiemHelper.Fmt(tb.Value));
        lblXL.Text = "Xếp loại: " + DiemHelper.XepLoai(tb);
    }

    // ================= sinh viên =================
    void LamMoiSV()
    {
        txtMSSV.Clear(); txtHoTen.Clear(); dtpNgaySinh.Value = new DateTime(2005, 1, 1); rbNam.Checked = true;
        if (cboLop.Items.Count > 0) cboLop.SelectedIndex = 0;
        txtMSSV.Focus();
    }

    bool KiemTraSV()
    {
        if (string.IsNullOrWhiteSpace(txtMSSV.Text)) { Canh("MSSV không được để trống."); txtMSSV.Focus(); return false; }
        if (txtMSSV.Text.Trim().Length > 10) { Canh("MSSV tối đa 10 ký tự."); txtMSSV.Focus(); return false; }
        if (string.IsNullOrWhiteSpace(txtHoTen.Text)) { Canh("Họ tên không được để trống."); txtHoTen.Focus(); return false; }
        if (cboLop.SelectedItem == null) { Canh("Hãy chọn lớp."); return false; }
        return true;
    }

    void GanSV(SinhVien s)
    {
        s.HoTen = txtHoTen.Text.Trim();
        s.NgaySinh = dtpNgaySinh.Value.Date;
        s.GioiTinh = rbNu.Checked ? "Nữ" : "Nam";
        s.MaLop = (string)cboLop.SelectedItem;
    }

    async Task ThemSVAsync()
    {
        if (!KiemTraSV()) return;
        try
        {
            using var db = new QuanLyDiemContext();
            var ma = txtMSSV.Text.Trim();
            if (await db.SinhViens.AnyAsync(s => s.MaSV == ma)) { Canh($"MSSV \"{ma}\" đã tồn tại, không thể thêm trùng."); return; }
            var sv = new SinhVien { MaSV = ma }; GanSV(sv);
            db.SinhViens.Add(sv);
            await db.SaveChangesAsync();
            txtTim.Clear();
            await LoadSinhVienAsync(ma);
        }
        catch (Exception ex) { Loi("Không thêm được sinh viên", ex); }
    }

    async Task SuaSVAsync()
    {
        var sel = ChonSV();
        if (sel == null) { Canh("Hãy chọn sinh viên cần sửa."); return; }
        if (!KiemTraSV()) return;
        if (!string.Equals(txtMSSV.Text.Trim(), sel.MaSV, StringComparison.Ordinal)) { Canh("Không đổi được MSSV của sinh viên đã có."); txtMSSV.Text = sel.MaSV; return; }
        try
        {
            using var db = new QuanLyDiemContext();
            var sv = await db.SinhViens.FindAsync(sel.MaSV);
            if (sv == null) { Canh("Sinh viên không còn tồn tại."); await LoadSinhVienAsync(); return; }
            GanSV(sv);
            await db.SaveChangesAsync();
            await LoadSinhVienAsync(sv.MaSV);
        }
        catch (Exception ex) { Loi("Không sửa được sinh viên", ex); }
    }

    async Task XoaSVAsync()
    {
        var sel = ChonSV();
        if (sel == null) { Canh("Hãy chọn sinh viên cần xóa."); return; }
        if (MessageBox.Show($"Xóa sinh viên {sel.MaSV} - {sel.HoTen} và toàn bộ điểm?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new QuanLyDiemContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.DiemMons.Where(d => d.MaSV == sel.MaSV).ExecuteDeleteAsync();      // xóa điểm trước
            await db.SinhViens.Where(s => s.MaSV == sel.MaSV).ExecuteDeleteAsync();
            await tx.CommitAsync();
            await LoadSinhVienAsync();
        }
        catch (Exception ex) { Loi("Không xóa được sinh viên", ex); }
    }

    // ================= điểm =================
    bool KiemTraMon()
    {
        if (ChonSV() == null) { Canh("Hãy chọn sinh viên trước."); return false; }
        if (string.IsNullOrWhiteSpace(txtMon.Text)) { Canh("Tên môn không được để trống."); txtMon.Focus(); return false; }
        return true;
    }

    async Task ThemMonAsync()
    {
        if (!KiemTraMon()) return;
        try
        {
            using var db = new QuanLyDiemContext();
            db.DiemMons.Add(new DiemMon { MaSV = ChonSV().MaSV, TenMon = txtMon.Text.Trim(), SoTinChi = (int)nudTC.Value, Diem = nudDiem.Value });
            await db.SaveChangesAsync();
            await LoadDiemAsync(ChonSV().MaSV);
        }
        catch (Exception ex) { Loi("Không thêm được môn", ex); }
    }

    async Task SuaDiemAsync()
    {
        if (dgvDiem.CurrentRow?.DataBoundItem is not DiemRow r) { Canh("Hãy chọn một môn để sửa."); return; }
        if (!KiemTraMon()) return;
        try
        {
            using var db = new QuanLyDiemContext();
            var d = await db.DiemMons.FindAsync(r.MaDiem);
            if (d == null) { Canh("Dòng điểm không còn tồn tại."); await LoadDiemAsync(ChonSV().MaSV); return; }
            d.TenMon = txtMon.Text.Trim(); d.SoTinChi = (int)nudTC.Value; d.Diem = nudDiem.Value;
            await db.SaveChangesAsync();
            await LoadDiemAsync(d.MaSV);
        }
        catch (Exception ex) { Loi("Không sửa được điểm", ex); }
    }

    async Task XoaMonAsync()
    {
        if (dgvDiem.CurrentRow?.DataBoundItem is not DiemRow r) { Canh("Hãy chọn một môn để xóa."); return; }
        if (MessageBox.Show($"Xóa môn \"{r.TenMon}\"?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new QuanLyDiemContext();
            await db.DiemMons.Where(d => d.MaDiem == r.MaDiem).ExecuteDeleteAsync();
            await LoadDiemAsync(ChonSV().MaSV);
        }
        catch (Exception ex) { Loi("Không xóa được môn", ex); }
    }

    // ================= in phiếu (PrintDocument) =================
    bool ChuanBiPhieu()
    {
        var sv = ChonSV();
        if (sv == null) { Canh("Hãy chọn một sinh viên để in phiếu."); return false; }
        if (_diem.Count == 0) { Canh("Sinh viên chưa có điểm môn nào, không thể in phiếu."); return false; }
        _printer.SetData(sv, sv.MaLop, DiemEntities());
        return true;
    }

    const string MsgKhongCoMayIn = "Máy tính chưa cài máy in nên không thể in/xem trước.\nHãy cài một máy in (ví dụ \"Microsoft Print to PDF\") rồi thử lại.";

    async Task XemTruocAsync()
    {
        if (!ChuanBiPhieu()) return;
        try { _printer.Preview(this); }
        catch (InvalidPrinterException) { Canh(MsgKhongCoMayIn); }
        catch (Exception ex) { Loi("Không xem trước được phiếu", ex); }
        await Task.CompletedTask;
    }

    async Task InAsync()
    {
        if (!ChuanBiPhieu()) return;
        try { _printer.Print(this); }
        catch (InvalidPrinterException) { Canh(MsgKhongCoMayIn); }
        catch (Exception ex) { Loi("Không in được phiếu", ex); }
        await Task.CompletedTask;
    }

    async Task CaiDatTrangAsync()
    {
        try { _printer.PageSetup(this); }
        catch (InvalidPrinterException) { Canh(MsgKhongCoMayIn); }
        catch (Exception ex) { Loi("Không mở được cài đặt trang", ex); }
        await Task.CompletedTask;
    }

    // ================= Excel điểm lớp =================
    async Task XuatExcelAsync()
    {
        try
        {
            var lop = LopDangChon;
            using var db = new QuanLyDiemContext();
            // truy vấn nối 3 bảng: DiemMon - SinhVien - Lop
            var q = from d in db.DiemMons
                    join s in db.SinhViens on d.MaSV equals s.MaSV
                    join l in db.Lops on s.MaLop equals l.MaLop
                    select new { s.MaSV, s.HoTen, Lop = l.MaLop, d.TenMon, d.SoTinChi, d.Diem, d.MaDiem };
            if (lop != null) q = q.Where(x => x.Lop == lop);
            var rows = await q.AsNoTracking().OrderBy(x => x.MaSV).ThenBy(x => x.MaDiem).ToListAsync();
            if (rows.Count == 0) { Canh("Không có dữ liệu điểm để xuất."); return; }

            using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"DiemLop_{lop ?? "TatCa"}.xlsx" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Diem");
            string[] head = { "MSSV", "Họ tên", "Lớp", "Môn học", "Số TC", "Điểm" };
            for (int c = 0; c < head.Length; c++) ws.Cell(1, c + 1).Value = head[c];
            int r = 2;
            foreach (var x in rows)
            {
                ws.Cell(r, 1).Value = x.MaSV; ws.Cell(r, 2).Value = x.HoTen; ws.Cell(r, 3).Value = x.Lop;
                ws.Cell(r, 4).Value = x.TenMon; ws.Cell(r, 5).Value = x.SoTinChi; ws.Cell(r, 6).Value = x.Diem;
                r++;
            }
            int last = r - 1;
            ws.Cell(r, 4).Value = "Điểm trung bình";
            ws.Cell(r, 6).FormulaA1 = $"AVERAGE(F2:F{last})";
            ws.Cell(r + 1, 4).Value = "Tổng số tín chỉ";
            ws.Cell(r + 1, 5).FormulaA1 = $"SUM(E2:E{last})";
            ws.Range(r, 4, r + 1, 6).Style.Font.Bold = true;
            ws.Range(2, 6, r, 6).Style.NumberFormat.Format = "0.00";
            ws.Range(2, 5, r + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
            wb.SaveAs(sfd.FileName);

            if (MessageBox.Show("Đã xuất Excel. Mở file ngay?", "Hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (IOException) { Canh("Không ghi được file Excel. Có thể file đang mở, hãy đóng file rồi thử lại."); }
        catch (Exception ex) { Loi("Không xuất được Excel", ex); }
    }

    // ================= tiện ích =================
    static void Canh(string m) => MessageBox.Show(m, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    static void Loi(string m, Exception ex) => MessageBox.Show($"{m}.\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
