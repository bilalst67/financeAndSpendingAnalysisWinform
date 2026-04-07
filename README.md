# 💹 Kişisel Finans ve Harcama Analiz MVP

![Platform](https://img.shields.io/badge/Platform-Linux%20%2F%20Wine-blue)
![Framework](https://img.shields.io/badge/.NET-10.0--windows-purple)
![Database](https://img.shields.io/badge/Database-SQLite-green)
![OS](https://img.shields.io/badge/OS-Arch%20Linux-brightgreen)

Bu proje, **Arch Linux** üzerinde **Wine** katmanı kullanılarak geliştirilmiş, modern ve kararlı bir masaüstü finans yönetim uygulamasıdır. Klasik WinForms altyapısını, modern yazılım mimarisi (**TableLayoutPanel / Grid System**) ve güncel kütüphanelerle birleştirerek Linux ve diğer winforms ortamlarında kusursuz bir kullanıcı deneyimi sunar.

---

## 🚀 Öne Çıkan Özellikler

* **Dinamik Dashboard:** Gelir, gider ve net bakiye durumunu anlık olarak takip edin.
* **Harcama Analizi:** Kategorize edilmiş harcamalarınızı interaktif **Doughnut Chart** ile görselleştirin.
* **Akıllı Kıyaslama:** Geçen aya göre harcama alışkanlıklarınızdaki değişimi (yüzdesel artış/azalış) otomatik hesaplar.
* **Periyodik Ödemeler:** Kira, fatura gibi her ay tekrarlanan ödemeleri işaretleyin; uygulama günü geldiğinde otomatik olarak eklesin.
* **Gelişmiş Filtreleme:** Kategori bazlı anlık arama motoru ile verilerinize saniyeler içinde ulaşın.
* **Profesyonel Raporlama:** Tüm finansal dökümünüzü **QuestPDF** altyapısıyla fatura şablonunda PDF olarak dışa aktarın.
* **Karanlık Mod:** Göz yormayan modern koyu tema desteği.
* **Esnek Kategori Yönetimi:** Mevcut kategorileri kullanın veya kendi özel kategorinizi elle yazarak ekleyin.

---

## 🛠 Teknik Altyapı

* **Dil/Runtime:** C# | .NET 10.0-windows
* **Veritabanı:** [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite) (Dosya tabanlı yerel depolama)
* **Grafik Motoru:** [System.Windows.Forms.DataVisualization](https://www.nuget.org/packages/System.Windows.Forms.DataVisualization)
* **PDF Motoru:** [QuestPDF](https://www.questpdf.com/) (Kod tabanlı doküman tasarımı)
* **UI Mimari:** Gelişmiş `TableLayoutPanel` (Grid) yapısı ile Wine/Linux ortamında %100 ölçeklenebilir ve kaymayan arayüz.

---

## 🔧 Kurulum ve Çalıştırma (Linux için)

Uygulama Linux üzerinde Wine ile çalışacak şekilde optimize edilmiştir.

### 1. Bağımlılıkları Yükleyin (Arch Linux)

```bash
# .NET SDK kurulumu
sudo pacman -S dotnet-sdk

# Wine kurulumu
sudo pacman -S wine
```

# 2. Projeyi Derleyin
```bash

dotnet publish -c Release -r win-x64 --self-contained true
```

# 3. Uygulamayı Başlatın
```bash

wine bin/Release/net10.0-windows/win-x64/publish/FinansAndHarcamaAnaliz.exe
```

## 📁 Proje Yapısı

MainForm.cs: Uygulamanın Grid tabanlı arayüz mantığı ve tema motoru.

DbManager.cs: SQLite CRUD işlemleri ve periyodik ödeme kontrolörü.

IslemModel.cs: Veri transfer objesi (DTO).

finans.db: Yerel veritabanı dosyası (ilk çalıştırmada otomatik oluşturulur).

## 👨‍💻 Geliştirici
Bilal Sarıtaş

GitHub: @bilalst67

LinkedIn: Bilal Sarıtaş

Konum: Bursa / Türkiye

## ⚖️ Lisans
Bu proje MIT Lisansı altında lisanslanmıştır.