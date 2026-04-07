using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace financeAndSpendingAnalysisWinform;

public static class DbManager
{
    private const string ConnectionString = "Data Source=finans.db";

    public static void VeritabaniniHazirla()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Islemler (Id INTEGER PRIMARY KEY AUTOINCREMENT, Tur TEXT, Kategori TEXT, Miktar REAL, Tarih TEXT);
                CREATE TABLE IF NOT EXISTS PeriyodikIslemler (Id INTEGER PRIMARY KEY AUTOINCREMENT, Tur TEXT, Kategori TEXT, Miktar REAL, Gun INTEGER, SonEklemeAyYil TEXT);";
            command.ExecuteNonQuery();
        }
    }

    public static void PeriyodikIslemEkle(string tur, string kategori, decimal miktar, int gun)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO PeriyodikIslemler (Tur, Kategori, Miktar, Gun, SonEklemeAyYil) VALUES ($tur, $kategori, $miktar, $gun, '')";
            command.Parameters.AddWithValue("$tur", tur);
            command.Parameters.AddWithValue("$kategori", kategori);
            command.Parameters.AddWithValue("$miktar", miktar);
            command.Parameters.AddWithValue("$gun", gun);
            command.ExecuteNonQuery();
        }
    }

    public static void OtomatikOdemeleriKontrolEt()
    {
        string buAyYil = DateTime.Now.ToString("MM/yyyy");
        int bugun = DateTime.Now.Day;

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
                    IslemEkle(reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), DateTime.Now.ToShortDateString());
                    var up = connection.CreateCommand();
                    up.CommandText = "UPDATE PeriyodikIslemler SET SonEklemeAyYil = $buAyYil WHERE Id = $id";
                    up.Parameters.AddWithValue("$buAyYil", buAyYil);
                    up.Parameters.AddWithValue("$id", reader.GetInt32(0));
                    up.ExecuteNonQuery();
                }
            }
        }
    }

    public static decimal AylikToplamGetir(string tur, int ayOffset)
    {
        DateTime hedef = DateTime.Now.AddMonths(ayOffset);
        string format = hedef.ToString("M/d/yyyy").Split('/')[0] + "/" + hedef.ToString("M/d/yyyy").Split('/')[2]; // Basit ay/yil kontrolu
        decimal toplam = 0;
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT SUM(Miktar) FROM Islemler WHERE Tur = $tur AND Tarih LIKE $tarih";
            command.Parameters.AddWithValue("$tur", tur);
            command.Parameters.AddWithValue("$tarih", "%" + hedef.Year.ToString());
            var res = command.ExecuteScalar();
            if (res != DBNull.Value && res != null) toplam = Convert.ToDecimal(res);
        }
        return toplam;
    }

    public static void IslemEkle(string tur, string kategori, decimal miktar, string tarih)
    {
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