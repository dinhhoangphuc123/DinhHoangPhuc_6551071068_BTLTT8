using System.Data;
using System.Diagnostics;
using ClosedXML.Excel;
using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.EntityFrameworkCore;
using TiemTapHoa.Models;

namespace TiemTapHoa;

public class MainForm : Form
{
    static readonly string[] Loai = { "Đồ uống", "Thực phẩm khô", "Gia vị", "Đồ gia dụng" };
    static readonly string[] DonVi = { "Chai", "Gói", "Hộp", "Túi" };

    // ----- control nhập liệu -----
    readonly TextBox txtTen = new() { Dock = DockStyle.Fill };
    readonly ComboBox cboLoai = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox cboDonVi = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly NumericUpDown nudGia = new() { Dock = DockStyle.Fill, Maximum = 1_000_000_000, Increment = 500, ThousandsSeparator = true };
    readonly NumericUpDown nudTon = new() { Dock = DockStyle.Fill, Maximum = 1_000_000 };
    readonly DateTimePicker dtpNgay = new() { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    readonly CheckBox chkKD = new() { Text = "Còn kinh doanh", Checked = true, AutoSize = true };

    // ----- tìm kiếm -----
    readonly TextBox txtTim = new() { Dock = DockStyle.Fill };
    readonly ComboBox cboLoc = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly CheckBox chkChiCon = new() { Text = "Chỉ hiện hàng còn kinh doanh", AutoSize = true };
    readonly System.Windows.Forms.Timer timTim = new() { Interval = 350 };

    readonly DataGridView dgv = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        BackgroundColor = SystemColors.AppWorkspace, RowHeadersVisible = true
    };
    readonly ToolStripStatusLabel lblTrangThai = new();
    bool _binding;

    public MainForm()
    {
        Text = "Quản lý hàng hóa - Tạp hóa UTC2";
        Width = 1050; Height = 650; StartPosition = FormStartPosition.CenterScreen;
        cboLoai.Items.AddRange(Loai); cboDonVi.Items.AddRange(DonVi);
        cboLoc.Items.Add("Tất cả loại"); cboLoc.Items.AddRange(Loai); cboLoc.SelectedIndex = 0;
        BuildLayout();
        BuildColumns();

        timTim.Tick += async (_, _) => { timTim.Stop(); await LoadAsync(); };
        txtTim.TextChanged += (_, _) => { timTim.Stop(); timTim.Start(); };   // debounce 350 ms
        cboLoc.SelectedIndexChanged += async (_, _) => await LoadAsync();
        chkChiCon.CheckedChanged += async (_, _) => await LoadAsync();
        dgv.SelectionChanged += (_, _) => DoLenControl();
        Shown += async (_, _) => { await LoadAsync(); };
    }

    // ================= giao diện (dựng bằng code) =================
    void BuildLayout()
    {
        var gbInfo = new GroupBox { Text = "Thông tin hàng hóa", Dock = DockStyle.Fill };
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Padding = new Padding(6) };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        void Add(string lbl, Control c, int col, int row, int span = 1)
        {
            t.Controls.Add(new Label { Text = lbl, AutoSize = true, Anchor = AnchorStyles.Left }, col, row);
            t.Controls.Add(c, col + 1, row);
            if (span > 1) t.SetColumnSpan(c, span);
        }
        Add("Tên hàng:", txtTen, 0, 0, 3);
        Add("Loại hàng:", cboLoai, 0, 1); Add("Số lượng tồn:", nudTon, 2, 1);
        Add("Đơn vị:", cboDonVi, 0, 2);   Add("Ngày nhập:", dtpNgay, 2, 2);
        Add("Giá bán:", nudGia, 0, 3);
        t.Controls.Add(chkKD, 3, 3);
        gbInfo.Controls.Add(t);

        var gbTim = new GroupBox { Text = "Tìm kiếm", Dock = DockStyle.Fill };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(6) };
        txtTim.Width = cboLoc.Width = 330; txtTim.PlaceholderText = "Tìm theo tên hàng...";
        f.Controls.AddRange(new Control[] { txtTim, cboLoc, chkChiCon });
        gbTim.Controls.Add(f);

        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 160, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        top.Controls.Add(gbInfo, 0, 0); top.Controls.Add(gbTim, 1, 0);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(6, 6, 0, 0) };
        Button B(string text, EventHandler h, int w = 90) { var b = new Button { Text = text, Width = w, Height = 30 }; b.Click += h; return b; }
        bar.Controls.AddRange(new Control[]
        {
            B("Thêm", async (_, _) => await ThemAsync()), B("Sửa", async (_, _) => await SuaAsync()),
            B("Xóa", async (_, _) => await XoaAsync()), B("Làm mới", async (_, _) => await LamMoiAsync()),
            B("Xem bảng giá", async (_, _) => await XemBangGiaAsync(), 110),
            B("Xuất PDF", async (_, _) => await XuatPdfAsync()),
            B("Xuất Excel (lưới)", (_, _) => XuatExcelLuoi(), 130),
            B("Xuất Excel tồn kho", async (_, _) => await XuatExcelTonKhoAsync(), 140),
        });

        var status = new StatusStrip(); status.Items.Add(lblTrangThai);
        Controls.Add(dgv); Controls.Add(bar); Controls.Add(top); Controls.Add(status);
        dgv.BringToFront();
    }

    void BuildColumns()
    {
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaHang", HeaderText = "Mã", DataPropertyName = "MaHang", Width = 50 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenHang", HeaderText = "Tên hàng", DataPropertyName = "TenHang", Width = 230 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoaiHang", HeaderText = "Loại hàng", DataPropertyName = "LoaiHang", Width = 110 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", DataPropertyName = "DonVi", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "GiaBan", HeaderText = "Giá bán", DataPropertyName = "GiaBan", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SoLuongTon", HeaderText = "Tồn", DataPropertyName = "SoLuongTon", Width = 70,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "NgayNhap", HeaderText = "Ngày nhập", DataPropertyName = "NgayNhap", Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ConKinhDoanh", HeaderText = "Kinh doanh", DataPropertyName = "ConKinhDoanh", Width = 80 });
    }

    // ================= nạp dữ liệu + tìm kiếm =================
    IQueryable<HangHoa> BuildQuery(TiemTapHoaContext db)
    {
        var q = db.HangHoas.AsNoTracking().AsQueryable();
        var kw = txtTim.Text.Trim();
        if (kw.Length > 0) q = q.Where(h => EF.Functions.Like(h.TenHang, "%" + kw + "%"));
        if (cboLoc.SelectedIndex > 0) { var loai = (string)cboLoc.SelectedItem; q = q.Where(h => h.LoaiHang == loai); }
        if (chkChiCon.Checked) q = q.Where(h => h.ConKinhDoanh);
        return q.OrderBy(h => h.MaHang);
    }

    async Task LoadAsync(int? chonMa = null)
    {
        try
        {
            using var db = new TiemTapHoaContext();
            var list = await BuildQuery(db).ToListAsync();
            _binding = true;
            dgv.DataSource = list;
            _binding = false;
            lblTrangThai.Text = $"Tìm thấy {list.Count} mặt hàng";
            if (chonMa != null)
                foreach (DataGridViewRow r in dgv.Rows)
                    if (((HangHoa)r.DataBoundItem).MaHang == chonMa) { r.Selected = true; dgv.CurrentCell = r.Cells[1]; break; }
            DoLenControl();
        }
        catch (Exception ex) { Loi("Không tải được dữ liệu", ex); }
    }

    HangHoa Chon() => dgv.CurrentRow?.DataBoundItem as HangHoa;

    void DoLenControl()
    {
        if (_binding) return;
        var h = Chon();
        if (h == null) return;
        txtTen.Text = h.TenHang;
        cboLoai.SelectedItem = h.LoaiHang;
        cboDonVi.SelectedItem = h.DonVi;
        nudGia.Value = Math.Min(nudGia.Maximum, h.GiaBan);
        nudTon.Value = Math.Min(nudTon.Maximum, h.SoLuongTon);
        dtpNgay.Value = h.NgayNhap < dtpNgay.MinDate ? dtpNgay.MinDate : h.NgayNhap;
        chkKD.Checked = h.ConKinhDoanh;
    }

    // ================= thêm - sửa - xóa =================
    bool HopLe()
    {
        if (string.IsNullOrWhiteSpace(txtTen.Text)) { Canh("Tên hàng không được để trống."); txtTen.Focus(); return false; }
        if (nudGia.Value <= 0) { Canh("Giá bán phải lớn hơn 0."); nudGia.Focus(); return false; }
        return true;
    }

    void GanTuForm(HangHoa h)
    {
        h.TenHang = txtTen.Text.Trim();
        h.LoaiHang = cboLoai.SelectedItem as string;
        h.DonVi = cboDonVi.SelectedItem as string;
        h.GiaBan = nudGia.Value;
        h.SoLuongTon = (int)nudTon.Value;
        h.NgayNhap = dtpNgay.Value;
        h.ConKinhDoanh = chkKD.Checked;
    }

    async Task ThemAsync()
    {
        if (!HopLe()) return;
        try
        {
            using var db = new TiemTapHoaContext();
            var h = new HangHoa(); GanTuForm(h);
            db.HangHoas.Add(h);
            await db.SaveChangesAsync();
            await LoadAsync(h.MaHang);
            MessageBox.Show("Đã thêm hàng hóa.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Loi("Không thêm được", ex); }
    }

    async Task SuaAsync()
    {
        var sel = Chon();
        if (sel == null) { Canh("Hãy chọn một hàng hóa để sửa."); return; }
        if (!HopLe()) return;
        try
        {
            using var db = new TiemTapHoaContext();
            var h = await db.HangHoas.FindAsync(sel.MaHang);
            if (h == null) { Canh("Hàng hóa không còn tồn tại."); await LoadAsync(); return; }
            GanTuForm(h);
            await db.SaveChangesAsync();
            await LoadAsync(h.MaHang);
            MessageBox.Show("Đã cập nhật.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Loi("Không sửa được", ex); }
    }

    async Task XoaAsync()
    {
        var sel = Chon();
        if (sel == null) { Canh("Hãy chọn một hàng hóa để xóa."); return; }
        if (MessageBox.Show($"Xóa \"{sel.TenHang}\"?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new TiemTapHoaContext();
            await db.HangHoas.Where(x => x.MaHang == sel.MaHang).ExecuteDeleteAsync();
            await LoadAsync();
        }
        catch (Exception ex) { Loi("Không xóa được", ex); }
    }

    async Task LamMoiAsync()
    {
        txtTen.Clear(); cboLoai.SelectedIndex = -1; cboDonVi.SelectedIndex = -1;
        nudGia.Value = 0; nudTon.Value = 0; dtpNgay.Value = DateTime.Today; chkKD.Checked = true;
        txtTim.Clear(); cboLoc.SelectedIndex = 0; chkChiCon.Checked = false;
        await LoadAsync();
        dgv.ClearSelection();
        txtTen.Focus();
    }

    // ================= FastReport (nguồn DataTable) =================
    async Task<DataTable> LayBangGiaAsync()
    {
        using var db = new TiemTapHoaContext();
        var ds = await db.HangHoas.AsNoTracking().Where(h => h.ConKinhDoanh).OrderBy(h => h.MaHang).ToListAsync();
        var dt = new DataTable("HangHoa");
        dt.Columns.Add("MaHang", typeof(int));
        dt.Columns.Add("TenHang", typeof(string));
        dt.Columns.Add("DonVi", typeof(string));
        dt.Columns.Add("GiaBan", typeof(decimal));
        foreach (var h in ds) dt.Rows.Add(h.MaHang, h.TenHang, h.DonVi, h.GiaBan);
        return dt;
    }

    async Task<Report> TaoReportAsync()
    {
        var dt = await LayBangGiaAsync();
        var path = Path.Combine(AppContext.BaseDirectory, "Reports", "BangGia.frx");
        if (!File.Exists(path)) throw new FileNotFoundException("Không tìm thấy mẫu report.", path);
        var rp = new Report();
        rp.Load(path);
        rp.RegisterData(dt, "HangHoa");
        rp.GetDataSource("HangHoa").Enabled = true;
        ((FastReport.DataBand)rp.FindObject("Data1")).DataSource = rp.GetDataSource("HangHoa");
        rp.SetParameterValue("NgayIn", DateTime.Now.ToString("dd/MM/yyyy"));
        return rp;
    }

    async Task XemBangGiaAsync()
    {
        try
        {
            using var rp = await TaoReportAsync();
            rp.Show();      // cửa sổ xem trước của FastReport (modal)
        }
        catch (Exception ex) { Loi("Không xem được bảng giá", ex); }
    }

    async Task XuatPdfAsync()
    {
        using var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = "BangGia.pdf" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        try
        {
            using var rp = await TaoReportAsync();
            rp.Prepare();
            using (var pdf = new PDFSimpleExport()) rp.Export(pdf, sfd.FileName);
            MoFile(sfd.FileName);
        }
        catch (IOException) { Canh("Không ghi được file PDF. Có thể file đang mở, hãy đóng file rồi thử lại."); }
        catch (Exception ex) { Loi("Không xuất được PDF", ex); }
    }

    // ================= Excel =================
    void XuatExcelLuoi()
    {
        if (dgv.Rows.Count == 0) { Canh("Lưới đang không có dòng nào để xuất."); return; }
        using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "HangHoa_Luoi.xlsx" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("HangHoa");
            var cols = dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            for (int c = 0; c < cols.Count; c++) ws.Cell(1, c + 1).Value = cols[c].HeaderText;
            int r = 2;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    var v = row.Cells[cols[c].Index].Value;       // giữ nguyên kiểu: số là số, ngày là ngày
                    var cell = ws.Cell(r, c + 1);
                    if (v is bool b) cell.Value = b ? "Có" : "Không";
                    else cell.Value = XLCellValue.FromObject(v);
                    if (cols[c].Name == "GiaBan") cell.Style.NumberFormat.Format = "#,##0";
                    if (cols[c].Name == "NgayNhap") cell.Style.NumberFormat.Format = "dd/MM/yyyy";
                }
                r++;
            }
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            wb.SaveAs(sfd.FileName);
            MoFile(sfd.FileName);
        }
        catch (IOException) { Canh("Không ghi được file Excel. Có thể file đang mở, hãy đóng file rồi thử lại."); }
        catch (Exception ex) { Loi("Không xuất được Excel", ex); }
    }

    async Task XuatExcelTonKhoAsync()
    {
        using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "TonKho.xlsx" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        try
        {
            List<HangHoa> ds;
            using (var db = new TiemTapHoaContext())
                ds = await db.HangHoas.AsNoTracking().Where(h => h.ConKinhDoanh).OrderBy(h => h.MaHang).ToListAsync();
            if (ds.Count == 0) { Canh("Không có hàng còn kinh doanh để xuất."); return; }

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("TonKho");
            string[] head = { "Mã hàng", "Tên hàng", "Loại hàng", "Đơn vị", "Giá bán", "Số lượng tồn", "Ngày nhập", "Giá trị tồn" };
            for (int c = 0; c < head.Length; c++) ws.Cell(1, c + 1).Value = head[c];
            int r = 2;
            foreach (var h in ds)
            {
                ws.Cell(r, 1).Value = h.MaHang;
                ws.Cell(r, 2).Value = h.TenHang;
                ws.Cell(r, 3).Value = h.LoaiHang;
                ws.Cell(r, 4).Value = h.DonVi;
                ws.Cell(r, 5).Value = h.GiaBan;
                ws.Cell(r, 6).Value = h.SoLuongTon;
                ws.Cell(r, 7).Value = h.NgayNhap;
                ws.Cell(r, 8).FormulaA1 = $"E{r}*F{r}";
                r++;
            }
            int last = r - 1;
            ws.Cell(r, 7).Value = "Tổng";
            ws.Cell(r, 8).FormulaA1 = $"SUM(H2:H{last})";
            ws.Range(r, 7, r, 8).Style.Font.Bold = true;

            ws.Range(2, 5, r, 6).Style.NumberFormat.Format = "#,##0";
            ws.Range(2, 8, r, 8).Style.NumberFormat.Format = "#,##0";
            ws.Range(2, 7, last, 7).Style.NumberFormat.Format = "dd/MM/yyyy";
            ws.Range(2, 5, r, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Row(1).Style.Font.Bold = true;
            ws.Range(1, 1, last, 8).SetAutoFilter();     // AutoFilter không gồm dòng Tổng
            ws.SheetView.FreezeRows(1);                  // cố định dòng tiêu đề
            ws.Columns().AdjustToContents();
            wb.SaveAs(sfd.FileName);
            MoFile(sfd.FileName);
        }
        catch (IOException) { Canh("Không ghi được file Excel. Có thể file đang mở, hãy đóng file rồi thử lại."); }
        catch (Exception ex) { Loi("Không xuất được Excel tồn kho", ex); }
    }

    // ================= tiện ích =================
    void MoFile(string path)
    {
        if (MessageBox.Show("Đã xuất file. Bạn có muốn mở file không?", "Hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    static void Canh(string m) => MessageBox.Show(m, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    static void Loi(string m, Exception ex) => MessageBox.Show($"{m}.\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
