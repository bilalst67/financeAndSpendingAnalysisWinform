using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Color = System.Drawing.Color;

namespace financeAndSpendingAnalysisWinform;

public class MainForm : Form
{
    private TableLayoutPanel mainGrid, rightGrid, dashboardGrid;
    private Panel leftPanel, searchPanel, statsPanel, chartWrapper;
    private DataGridView dgvIslemler;
    private ComboBox cmbTur, cmbKategori;
    private NumericUpDown nudMiktar;
    private DateTimePicker dtpTarih;
    private CheckBox chkPeriyodik;
    private Button btnEkle, btnSil, btnDuzenle, btnPdfExport, btnThemeToggle ,btnKategoriDlt;
    private TextBox txtArama;
    private Label lblToplamGelir, lblToplamGider, lblBakiye, lblKiyaslama;
    private Chart chartKategori;
    private bool isDarkMode = true;
    
    // ARAYÜZ FONKSİYONLARI --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    
    //Temel Arayüz Çizimi Fonk.
    public MainForm()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        this.Text = "Finans Yönetimi";
        this.Size = new System.Drawing.Size(1250, 850);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 13f);

        DbManager.VeritabaniniHazirla();
        DbManager.OtomatikOdemeleriKontrolEt();

        InitializeUI();
        KategorileriComboBoxaYukle();
        TabloyuYenile();
        ApplyTheme();
    }
    
    //Arayüz Oluşturma Fonk.
    private void InitializeUI()
    {
        mainGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));
        mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        this.Controls.Add(mainGrid);

        leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        mainGrid.Controls.Add(leftPanel, 0, 0);

        int curY = 20;
        leftPanel.Controls.Add(new Label { Text = "Islem Yönetimi", Location = new Point(25, curY), Font = new Font("Arial", 16, FontStyle.Bold), AutoSize = true });
        curY += 60;

        AddInput("Islem Türü:", cmbTur = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Standard }, ref curY);
        cmbTur.Items.AddRange(new string[] { "Gider", "Gelir" }); cmbTur.SelectedIndex = 0;

        leftPanel.Controls.Add(new Label { Text = "Kategori (Yaz/Sec):", Location = new Point(25, curY), AutoSize = true, Font = new Font("Arial", 9, FontStyle.Bold) });
        
        cmbKategori = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Standard };
        cmbKategori.Location = new Point(25, curY + 22);
        cmbKategori.Width = 200;
        leftPanel.Controls.Add(cmbKategori);
        
        btnKategoriDlt = new Button { Text = "SİL", Location = new Point(230, curY + 21), Width = 40, Height = 28, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 8, FontStyle.Bold) };
        btnKategoriDlt.Click += BtnKategoriDlt_Click;
        leftPanel.Controls.Add(btnKategoriDlt);
        
        
        AddInput("Miktar (TL):", nudMiktar = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, BorderStyle = BorderStyle.Fixed3D }, ref curY);
        AddInput("Tarih:", dtpTarih = new DateTimePicker { Format = DateTimePickerFormat.Short }, ref curY);

        chkPeriyodik = new CheckBox { Text = "Her ay düzenli ödensin", Location = new Point(25, curY), AutoSize = true };
        leftPanel.Controls.Add(chkPeriyodik);
        curY += 40;
        btnEkle = CreateBtn("EKLE", Color.FromArgb(40, 167, 69), ref curY); btnEkle.Click += BtnEkle_Click;
        btnDuzenle = CreateBtn("DEGISIKLIGI KAYDET", Color.FromArgb(255, 193, 7), ref curY); btnDuzenle.Click += BtnDuzenle_Click; btnDuzenle.ForeColor = Color.Black;
        btnSil = CreateBtn("SIL", Color.FromArgb(220, 53, 69), ref curY); btnSil.Click += BtnSil_Click;
        curY += 20;
        btnPdfExport = CreateBtn("PDF RAPORU", Color.FromArgb(200,0, 0, 255), ref curY); btnPdfExport.Click += BtnPdfExport_Click;
        
        btnThemeToggle = new Button { Text = "TEMA DEGISTIR", Dock = DockStyle.Bottom, Height = 45, FlatStyle = FlatStyle.Standard ,BackColor = isDarkMode ? Color.White:Color.Black};
        btnThemeToggle.Click += (s, e) => { isDarkMode = !isDarkMode; ApplyTheme(); };
        leftPanel.Controls.Add(btnThemeToggle);

        rightGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(15) };
        rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 240f));
        rightGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f)); 
        rightGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); 
        mainGrid.Controls.Add(rightGrid, 1, 0);

        Color txt = isDarkMode ? Color.White : Color.Black;
        dashboardGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = txt };
        dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); 
        dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); 
        rightGrid.Controls.Add(dashboardGrid, 0, 0);

        statsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        dashboardGrid.Controls.Add(statsPanel, 0, 0);

        lblToplamGelir = CreateDashLabel("Gelir: 0.00 TL", 10, statsPanel, Color.Green);
        lblToplamGider = CreateDashLabel("Gider: 0.00 TL", 55, statsPanel, Color.Red);
        lblBakiye = CreateDashLabel("Bakiye: 0.00 TL", 105, statsPanel, Color.Blue, true);
        lblKiyaslama = CreateDashLabel("Analiz ediliyor...", 165, statsPanel, Color.Gray);

        chartWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        dashboardGrid.Controls.Add(chartWrapper, 1, 0);

        chartKategori = new Chart { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        ChartArea ca = new ChartArea("MainArea") { BackColor = Color.Transparent };
        ca.Position = new ElementPosition(0, 0, 100, 100); 
        chartKategori.ChartAreas.Add(ca);
        
        Series s = new Series("Harcamalar") { ChartType = SeriesChartType.Doughnut };
        s["PieLabelStyle"] = "Outside";
        s["DoughnutRadius"] = "50";
        chartKategori.Series.Add(s);
        chartWrapper.Controls.Add(chartKategori);

        searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 10) };
        rightGrid.Controls.Add(searchPanel, 0, 1);
        Label lblAra = new Label { Text = "Kategori Ara:", Location = new Point(0, 15), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };
        txtArama = new TextBox { Location = new Point(120, 12), Width = 350, Font = new Font("Arial", 11) };
        txtArama.TextChanged += (s, e) => TabloyuYenile(txtArama.Text);
        searchPanel.Controls.AddRange(new Control[] { lblAra, txtArama });

        dgvIslemler = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        dgvIslemler.Columns.Add("Id", "ID"); dgvIslemler.Columns["Id"].Visible = false;
        dgvIslemler.Columns.Add("Tur", "Tur"); dgvIslemler.Columns.Add("Kategori", "Kategori"); dgvIslemler.Columns.Add("Miktar", "Miktar"); dgvIslemler.Columns.Add("Tarih", "Tarih");
        dgvIslemler.CellClick += DgvIslemler_CellClick;
        rightGrid.Controls.Add(dgvIslemler, 0, 2);
    }

    //Arayüz Renk ayarı VS. fONK.
    private void ApplyTheme()
    {
        Color bg = isDarkMode ? Color.FromArgb(33, 37, 41) : Color.FromArgb(240, 242, 245);
        Color card = isDarkMode ? Color.FromArgb(52, 58, 64) : Color.White;
        Color txt = isDarkMode ? Color.White : Color.Black;
        Color btnClrPdf = isDarkMode ? Color.DarkBlue:Color.Blue;
        Color btnClrDlt = isDarkMode ? Color.DarkRed:Color.Red;
        Color btnClrAdd = isDarkMode ? Color.DarkGreen:Color.Green;
        Color btnClrUpd = isDarkMode ? Color.DarkOrange:Color.Orange;
        
        this.BackColor = bg;
        leftPanel.BackColor = card;
        dashboardGrid.BackColor = card;
        rightGrid.BackColor = bg;
        dgvIslemler.BackgroundColor = card;
        dgvIslemler.DefaultCellStyle.BackColor = card;
        dgvIslemler.DefaultCellStyle.ForeColor = txt;
        btnEkle.BackColor = btnClrAdd;
        btnDuzenle.BackColor = btnClrUpd;
        btnSil.BackColor = btnClrDlt;
        btnKategoriDlt.BackColor = btnClrDlt;
        btnPdfExport.BackColor = btnClrPdf;
        
        if (chartKategori.Series.Count > 0)
        {
            chartKategori.Series[0].LabelForeColor = txt;
        }

        foreach (Control c in leftPanel.Controls) if (c is Label || c is CheckBox) c.ForeColor = txt;
        foreach (Control c in statsPanel.Controls) if (c is Label) c.ForeColor = txt;
        foreach (Control c in searchPanel.Controls) if (c is Label) c.ForeColor = txt;
        
        lblToplamGelir.ForeColor = isDarkMode ? Color.LightGreen : Color.Green;
        lblToplamGider.ForeColor = isDarkMode ? Color.LightPink : Color.Red;
        lblBakiye.ForeColor = isDarkMode ? Color.LightSkyBlue : Color.Blue;
        btnThemeToggle.Text = isDarkMode ? "AYDINLIK MOD" : "KARANLIK MOD";
        btnThemeToggle.ForeColor = txt;
    }
    
    //TEMEL FONKSİYONLARI ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    
    //Veri Tbanındaki Kategorileri Combo Boxa Yükleme Fonk.
    private void KategorileriComboBoxaYukle()
    {
        string seciliKategori = cmbKategori.Text;
        cmbKategori.Items.Clear();
        var kategoriler = DbManager.KategorileriGetir();
        foreach (var kat in kategoriler)
        {
            cmbKategori.Items.Add(kat); 
        }
        cmbKategori.Text = seciliKategori;
    }

    //Yeni Label Ekleme Fonk.
    private Label CreateDashLabel(string txt, int y, Panel p, Color c, bool bold = false)
    {
        Label l = new Label { Text = txt, Location = new Point(10, y), AutoSize = true, ForeColor = c, Font = new Font("Arial", bold ? 15 : 12, bold ? FontStyle.Bold : FontStyle.Regular) };
        p.Controls.Add(l);
        return l;
    }

    //Yeni İnput Ekleme Fonk.
    private void AddInput(string txt, Control ctrl, ref int y)
    {
        leftPanel.Controls.Add(new Label { Text = txt, Location = new Point(25, y), AutoSize = true, Font = new Font("Arial", 9, FontStyle.Bold) });
        ctrl.Location = new Point(25, y + 22); ctrl.Width = 240;
        leftPanel.Controls.Add(ctrl);
        y += 65;
    }

    //Yeni Buton Ekleme Fonk.
    private Button CreateBtn(string txt, Color c, ref int y)
    {
        Button b = new Button { Text = txt, Location = new Point(25, y), Width = 240, Height = 42, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold) };
        leftPanel.Controls.Add(b);
        y += 48;
        return b;
    }
    
    //Yeni Kayıt Eklendiğinde Tabloyu Yenileme Fonk.
    private void TabloyuYenile(string filtre = "")
    {
        dgvIslemler.Rows.Clear();
        var liste = DbManager.TumIslemleriGetir();
        var filtrelenmis = string.IsNullOrEmpty(filtre) ? liste : liste.Where(x => x.Kategori.ToLower().Contains(filtre.ToLower())).ToList();
        foreach (var i in filtrelenmis) dgvIslemler.Rows.Add(i.Id, i.Tur, i.Kategori, i.Miktar, i.Tarih);
        GuncelleDashboard();
        KategorileriComboBoxaYukle();
    }

    //Yeni Kayıt Eklendiğinde Dashboardı Güncelleme Fonk.
    private void GuncelleDashboard()
    {
        decimal gel = 0, gid = 0;
        chartKategori.Series[0].Points.Clear();
        
        DateTime bugun = DateTime.Now.Date;
        DateTime otuzGunOnce = bugun.AddDays(-30);
        Dictionary<string, decimal> son30GunGiderler = new Dictionary<string, decimal>(); 
       
        foreach (DataGridViewRow r in dgvIslemler.Rows)
        {
            if (r.Cells["Miktar"].Value == null) continue;
            decimal m = Convert.ToDecimal(r.Cells["Miktar"].Value);
            string tur = r.Cells["Tur"].Value.ToString();
            string kategori = r.Cells["Kategori"].Value.ToString();

            if (tur == "Gelir") gel += m;
            else 
            { 
                gid += m; 
                
                if (DateTime.TryParse(r.Cells["Tarih"].Value?.ToString(), out DateTime islemTarihi))
                {
                    if (islemTarihi.Date >= otuzGunOnce && islemTarihi.Date <= bugun)
                    {
                        if (son30GunGiderler.ContainsKey(kategori))
                            son30GunGiderler[kategori] += m;
                        else
                            son30GunGiderler[kategori] = m;
                    }
                }
            }
        }
        
        foreach(var item in son30GunGiderler)
        {
            chartKategori.Series[0].Points.AddXY(item.Key, item.Value);
        }

        lblToplamGelir.Text = $"Gelir: {gel:N2} TL";
        lblToplamGider.Text = $"Gider: {gid:N2} TL";
        lblBakiye.Text = $"Bakiye: {(gel - gid):N2} TL";

        decimal buAy = DbManager.AylikToplamGetir("Gider", 0);
        decimal gecenAy = DbManager.AylikToplamGetir("Gider", -1);
        if (gecenAy > 0) {
            decimal fark = ((buAy - gecenAy) / gecenAy) * 100;
            lblKiyaslama.Text = $"Son 2 Ay Kiyaslamasi:\nGecen aya gore gideriniz %{Math.Abs(fark):N1} {(fark > 0 ? "arttı" : "azaldı")}";
        } else {
            lblKiyaslama.Text = "Son 2 Ay Kiyaslamasi:\nYeterli veri bekleniyor...";
        }
    }

    //BUTON BASMA OLAYLARI ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    
    //DataGridView seçme Fonk.
    private void DgvIslemler_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) {
            var r = dgvIslemler.Rows[e.RowIndex];
            cmbTur.Text = r.Cells["Tur"].Value?.ToString();
            cmbKategori.Text = r.Cells["Kategori"].Value?.ToString();
            nudMiktar.Value = Convert.ToDecimal(r.Cells["Miktar"].Value);
            if (DateTime.TryParse(r.Cells["Tarih"].Value?.ToString(), out DateTime dt)) dtpTarih.Value = dt;
        }
    }

    //Yeni Kayıt OLuşturma Fonk.
    private void BtnEkle_Click(object sender, EventArgs e)
    {
        if (chkPeriyodik.Checked) 
        {
            DateTime islemTarihi = dtpTarih.Value.Date;
            DateTime bugun = DateTime.Now.Date;
            DateTime donguTarihi = islemTarihi;
            string sonEklenenAyYil = "";
            if (donguTarihi <= bugun)
            {
                while (donguTarihi <= bugun)
                {
                    DbManager.IslemEkle(cmbTur.Text, cmbKategori.Text, nudMiktar.Value, donguTarihi.ToShortDateString());
                    sonEklenenAyYil = donguTarihi.ToString("MM/yyyy");
                    donguTarihi = donguTarihi.AddMonths(1);
                }
            }
            else
            {
                MessageBox.Show($"Düzenli işlem kaydedildi. Zamanı geldiğinde (Ayın {islemTarihi.Day}. günü) bakiyenize yansıyacaktır.", "Bilgi");
            }
            DbManager.PeriyodikIslemEkleGelistirilmis(cmbTur.Text, cmbKategori.Text, nudMiktar.Value, islemTarihi.Day, sonEklenenAyYil);
        }
        else 
        {
            DbManager.IslemEkle(cmbTur.Text, cmbKategori.Text, nudMiktar.Value, dtpTarih.Value.ToShortDateString());
        }

        TabloyuYenile();
        nudMiktar.Value = 0;
        chkPeriyodik.Checked = false;
    }

    //Kayıt Düzenleme Kayıt Fonk.
    private void BtnDuzenle_Click(object sender, EventArgs e) {
        if (dgvIslemler.SelectedRows.Count > 0)
        {
            DbManager.IslemGuncelle(Convert.ToInt32(dgvIslemler.SelectedRows[0].Cells["Id"].Value), cmbTur.Text, cmbKategori.Text, nudMiktar.Value, dtpTarih.Value.ToShortDateString()); TabloyuYenile();
        } }

    //Kayıt Silme Fonk.
    private void BtnSil_Click(object sender, EventArgs e) {
        if (dgvIslemler.SelectedRows.Count > 0)
        {
            DbManager.IslemSil(Convert.ToInt32(dgvIslemler.SelectedRows[0].Cells["Id"].Value)); TabloyuYenile();
        } }

    //Kayıtları Pdf Olarak Dışarı Çıkartan Fonk.
    private void BtnPdfExport_Click(object sender, EventArgs e)
    {
        var save = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = "Finans_Raporu.pdf" };
        if (save.ShowDialog() == DialogResult.OK) {
            Document.Create(c => {
                c.Page(p => {
                    p.Margin(50);
                    p.Header().Text("FİNANSAL RAPOR").FontSize(20).Bold();
                    p.Content().PaddingVertical(10).Table(t => {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        t.Header(h => { h.Cell().Text("Kategori"); h.Cell().Text("Tür"); h.Cell().Text("Miktar"); });
                        foreach (DataGridViewRow r in dgvIslemler.Rows) {
                            t.Cell().Text(r.Cells["Kategori"].Value?.ToString() ?? "");
                            t.Cell().Text(r.Cells["Tur"].Value?.ToString() ?? "");
                            t.Cell().Text((r.Cells["Miktar"].Value?.ToString() ?? "0") + " TL");
                        }
                    });
                    p.Footer().Text(x => { x.Span("Bakiye: "); x.Span(lblBakiye.Text).Bold(); });
                });
            }).GeneratePdf(save.FileName);
            MessageBox.Show("PDF Kaydedildi!");
        }
    }
    
    //Kayıtlı Kategorileri Silme Fonk.
    private void BtnKategoriDlt_Click(object sender, EventArgs e)
    {
        string silinecek = cmbKategori.Text;
        if (!string.IsNullOrWhiteSpace(silinecek))
        {
            if (MessageBox.Show($"'{silinecek}' kategorisini veritabanından kalıcı olarak silmek istediğinize emin misiniz? (Mevcut işlemleriniz silinmez)", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                DbManager.KategoriSil(silinecek);
                cmbKategori.Text = "";
                KategorileriComboBoxaYukle();
                MessageBox.Show("Kategori başarıyla silindi!");
            }
        }
    }
}