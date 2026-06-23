using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace financeAndSpendingAnalysisWinform;


public static class DbManager
{
    private const string ConnectionString = "Data Source=finans.db";

    public static readonly string[] VarsayilanGiderler = { "Market", "Ulaşım", "Fatura", "Eğlence" };
    public static readonly string[] VarsayilanGelirler = { "Maaş", "Avans", "Burs", "Ek Gelir" };

    public static void VeritabaniniHazirla()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Islemler (Id INTEGER PRIMARY KEY AUTOINCREMENT, Tur TEXT, Kategori TEXT, Miktar REAL, Tarih TEXT);
                CREATE TABLE IF NOT EXISTS PeriyodikIslemler (Id INTEGER PRIMARY KEY AUTOINCREMENT, Tur TEXT, Kategori TEXT, Miktar REAL, Gun INTEGER, SonEklemeAyYil TEXT);
                CREATE TABLE IF NOT EXISTS Kategoriler (Id INTEGER PRIMARY KEY AUTOINCREMENT, Ad TEXT, Tur TEXT, UNIQUE(Ad, Tur));"; 
            command.ExecuteNonQuery();

            // Varsayılan Giderleri Ekle
            foreach (var kat in VarsayilanGiderler)
                KategoriEkle(kat, "Gider");

            // Varsayılan Gelirleri Ekle
            foreach (var kat in VarsayilanGelirler)
                KategoriEkle(kat, "Gelir");
        }
    }

    public static void KategoriEkle(string kategoriAdi, string tur)
    {
        if (string.IsNullOrWhiteSpace(kategoriAdi) || string.IsNullOrWhiteSpace(tur)) return;

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Kategoriler (Ad, Tur) VALUES ($ad, $tur)";
            cmd.Parameters.AddWithValue("$ad", kategoriAdi.Trim());
            cmd.Parameters.AddWithValue("$tur", tur.Trim());
            cmd.ExecuteNonQuery();
        }
    }

    public static void KategoriSil(string kategoriAdi, string tur)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Kategoriler WHERE Ad=@ad AND Tur=@tur";
            cmd.Parameters.AddWithValue("@ad", kategoriAdi.Trim());
            cmd.Parameters.AddWithValue("@tur", tur.Trim());
            cmd.ExecuteNonQuery();
        }
    }
    
    public static List<string> KategorileriGetir(string tur)
    {
        var liste = new List<string>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Ad FROM Kategoriler WHERE Tur=@tur ORDER BY Ad ASC"; 
            cmd.Parameters.AddWithValue("@tur", tur);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    liste.Add(reader.GetString(0));
                }
            }
        }
        return liste;
    }

    public static bool VarsayilanKategoriMi(string kategoriAdi, string tur)
    {
        if (tur == "Gider") return VarsayilanGiderler.Contains(kategoriAdi);
        if (tur == "Gelir") return VarsayilanGelirler.Contains(kategoriAdi);
        return false;
    }

    public static void PeriyodikIslemEkleGelistirilmis(string tur, string kategori, decimal miktar, int gun, string sonEklenenAy)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO PeriyodikIslemler (Tur, Kategori, Miktar, Gun, SonEklemeAyYil) VALUES ($tur, $kategori, $miktar, $gun, $ay)";
            cmd.Parameters.AddWithValue("$tur", tur);
            cmd.Parameters.AddWithValue("$kategori", kategori);
            cmd.Parameters.AddWithValue("$miktar", miktar);
            cmd.Parameters.AddWithValue("$gun", gun);
            cmd.Parameters.AddWithValue("$ay", sonEklenenAy);
            cmd.ExecuteNonQuery();
        }
    }

    public static void OtomatikOdemeleriKontrolEt()
    {
        string buAyYil = DateTime.Now.ToString("MM/yyyy");
        int bugun = DateTime.Now.Day;

        var eklenecekler = new List<(int Id, string Tur, string Kategori, decimal Miktar)>();

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM PeriyodikIslemler WHERE Gun <= $bugun AND SonEklemeAyYil != $buAyYil";
            cmd.Parameters.AddWithValue("$bugun", bugun);
            cmd.Parameters.AddWithValue("$buAyYil", buAyYil);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    eklenecekler.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3)));
                }
            }

            foreach (var item in eklenecekler)
            {
                string gercekTarih = new DateTime(DateTime.Now.Year, DateTime.Now.Month, bugun).ToShortDateString();
                IslemEkle(item.Tur, item.Kategori, item.Miktar, gercekTarih);

                var up = connection.CreateCommand();
                up.CommandText = "UPDATE PeriyodikIslemler SET SonEklemeAyYil = $buAyYil WHERE Id = $id";
                up.Parameters.AddWithValue("$buAyYil", buAyYil);
                up.Parameters.AddWithValue("$id", item.Id);
                up.ExecuteNonQuery();
            }
        }
    }

    public static decimal AylikToplamGetir(string tur, int ayOffset)
    {
        DateTime hedef = DateTime.Now.AddMonths(ayOffset);
        decimal toplam = 0;
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Miktar, Tarih FROM Islemler WHERE Tur = $tur";
            command.Parameters.AddWithValue("$tur", tur);
            
            using (var reader = command.ExecuteReader())
            {
                while(reader.Read())
                {
                     if(DateTime.TryParse(reader.GetString(1), out DateTime dt))
                     {
                         if(dt.Month == hedef.Month && dt.Year == hedef.Year)
                         {
                             toplam += reader.GetDecimal(0);
                         }
                     }
                }
            }
        }
        return toplam;
    }

    public static void IslemEkle(string tur, string kategori, decimal miktar, string tarih)
    {
        KategoriEkle(kategori, tur);

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Islemler (Tur, Kategori, Miktar, Tarih) VALUES ($tur, $kategori, $miktar, $tarih)";
            cmd.Parameters.AddWithValue("$tur", tur);
            cmd.Parameters.AddWithValue("$kategori", kategori);
            cmd.Parameters.AddWithValue("$miktar", miktar);
            cmd.Parameters.AddWithValue("$tarih", tarih);
            cmd.ExecuteNonQuery();
        }
    }

    public static void IslemSil(int id)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Islemler WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public static void IslemGuncelle(int id, string tur, string kategori, decimal miktar, string tarih)
    {
        KategoriEkle(kategori, tur);

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Islemler SET Tur = $tur, Kategori = $kategori, Miktar = $miktar, Tarih = $tarih WHERE Id = $id";
            cmd.Parameters.AddWithValue("$tur", tur);
            cmd.Parameters.AddWithValue("$kategori", kategori);
            cmd.Parameters.AddWithValue("$miktar", miktar);
            cmd.Parameters.AddWithValue("$tarih", tarih);
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public static List<IslemModel> TumIslemleriGetir()
    {
        var liste = new List<IslemModel>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Islemler";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    liste.Add(new IslemModel { Id = reader.GetInt32(0), Tur = reader.GetString(1), Kategori = reader.GetString(2), Miktar = reader.GetDecimal(3), Tarih = reader.GetString(4) });
                }
            }
        }
        return liste;
    }
}