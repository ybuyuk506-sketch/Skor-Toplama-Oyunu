/*
================================================================================
PROJE ADI: Ruya'nın Yolculuğu (2D Action-Platformer Boss Fight)

GELİŞTİRİCİ EKİP VE GÖREV DAĞILIMI:

1. Muhammed Emin Çelik (Fizik Motoru ve Savaş Sistemi - En Kapsamlı Bölüm):
   - Mermi ve Zemin Sınıflarının (OOP) Tasarımı
   - Platform Çarpışma Testleri (Aşağı geçilebilen zemin mekaniği ve Hitbox'lar)
   - Vektörel Düşman Yapay Zekası (Boss'un oyuncu konumuna göre Math.Sqrt ile açısal mermi atması)
   - Mermi Üretimi, Hareketi ve Hasar Hesaplamaları

2. Yiğit Buyuk (Oyun Durumu ve Kontrol Mekanizmaları):
   - Oyuncu Sınıfının (OOP) Tasarımı
   - Klavye Girdi Kontrolleri (Olay Dinleyicileri: KeyDown, KeyUp)
   - Karakter Hareket Döngüsü ve Animasyon Zamanlayıcısı
   - Oyun Durum Makinesi (Menü, Savaş, Oyun Sonu geçişleri)

3. Ari Can Bora (Görsel Motor ve Arayüz/UI):
   - Dusman Sınıfının (OOP) Tasarımı
   - Katmanlama (Z-Index) ve Görüntü Önceliklendirme (Arkaplan -> Boss -> Oyuncu)
   - Arayüz (Can Barları) Çizimleri
   - Savaş Arenasının Tasarımı (Platform koordinatlarının görselle eşleştirilmesi)

YAPAY ZEKA (AI) DESTEKLİ MODÜLLER (Öğrenci Kapsamı Dışı Spesifik Kütüphane Kullanımları):
   - 'DosyaBul' Metodu: Dinamik dizin taraması (Directory Traversal) ve çift uzantı kontrolü AI yardımıyla yazılmıştır.
   - 'RotateFlip': Çizim motorunda resmin yatayda aynalanması (RotateNoneFlipX) AI ile entegre edilmiştir.
   - 'SeffafCiz': Resimlerin arkasındaki kirli beyazları silen ColorMap (Toleranslı Şeffaflık) algoritması AI desteklidir.
================================================================================
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BossFightOyunu
{
    public partial class Form1 : Form
    {
        enum OyunDurumu { Menü, Savaş, OyunSonu }
        OyunDurumu mevcutDurum = OyunDurumu.Menü;
        enum BossSecimi { Sorrow, Wrath }
        BossSecimi aktifBoss;

        Timer oyunSaati = new Timer();
        string sonucMesaji = "";

        Image imgRuya, imgSorrow, imgWrath, imgMermiSorrow, imgMermiWrath, bgSorrow, bgWrath;
        const int SEFFALIK_TOLERANSI = 40;

        // NESNE OLUŞTURMALARI (OOP)
        Oyuncu ruya = new Oyuncu();
        Dusman boss = new Dusman();
        List<Mermi> mermiler = new List<Mermi>();
        List<Zemin> platformlar = new List<Zemin>();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Width = 800; this.Height = 600;
            this.Text = "Ruya'nın Yolculuğu";

            GoruntuleriYukle();

            this.KeyDown += (s, e) => GirdiKontrol(e, true);
            this.KeyUp += (s, e) => GirdiKontrol(e, false);
            this.Paint += EkranaCiz;

            oyunSaati.Interval = 16;
            oyunSaati.Tick += OyunDongusu;
            oyunSaati.Start();
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void GoruntuleriYukle()
        {
            imgRuya = DosyaBul("ruya_serit");
            imgSorrow = DosyaBul("sorrow");
            imgWrath = DosyaBul("wrath");
            imgMermiSorrow = DosyaBul("sorrow_mermi");
            imgMermiWrath = DosyaBul("wrath_mermi");
            bgSorrow = DosyaBul("arena_sorrow");
            bgWrath = DosyaBul("arena_wrath");
        }

        // [AI DESTEKLİ METOT] Karmaşık dosya dizini arama
        private Image DosyaBul(string ad)
        {
            string yol = Application.StartupPath;
            for (int i = 0; i < 5; i++)
            {
                if (yol == null) break;
                string[] uzantilar = { "", ".png", ".jpg", ".jpeg", ".png.png" };
                foreach (string u in uzantilar)
                {
                    string tamYol = System.IO.Path.Combine(yol, ad + u);
                    if (System.IO.File.Exists(tamYol)) return Image.FromFile(tamYol);
                }
                yol = System.IO.Directory.GetParent(yol)?.FullName;
            }
            return null;
        }

        // [AI DESTEKLİ METOT] Toleranslı arka plan silme
        void SeffafCiz(Graphics g, Image img, Rectangle hedef, Rectangle kaynak, int tolerans)
        {
            if (img == null) return;
            using (ImageAttributes ayar = new ImageAttributes())
            {
                Color acikBeyaz = Color.FromArgb(255 - tolerans, 255 - tolerans, 255 - tolerans);
                Color tamBeyaz = Color.FromArgb(255, 255, 255);
                ayar.SetColorKey(acikBeyaz, tamBeyaz, ColorAdjustType.Bitmap);
                g.DrawImage(img, hedef, kaynak.X, kaynak.Y, kaynak.Width, kaynak.Height, GraphicsUnit.Pixel, ayar);
            }
        }

        #region YİĞİT BUYUK: Oyun Kontrolleri ve Menü Yönetimi
        private void SavasBaslat()
        {
            ruya = new Oyuncu();
            boss = new Dusman();
            mermiler.Clear();
            mevcutDurum = OyunDurumu.Savaş;
            PlatformlariKur(); // Ari Can'ın metodu çağrılır
        }

        private void GirdiKontrol(KeyEventArgs e, bool basildi)
        {
            if (mevcutDurum == OyunDurumu.Menü && basildi)
            {
                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) { aktifBoss = BossSecimi.Sorrow; SavasBaslat(); }
                if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) { aktifBoss = BossSecimi.Wrath; SavasBaslat(); }
            }
            else if (mevcutDurum == OyunDurumu.Savaş)
            {
                if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) ruya.SolaGit = basildi;
                if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) ruya.SagaGit = basildi;
                if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) ruya.SBasildi = basildi;

                if (basildi && e.KeyCode == Keys.W && ruya.ZeminKontrol) ruya.DikeyHiz = -16;
                if (basildi && e.KeyCode == Keys.Space && !ruya.SaldiriyorMu) Saldir();
            }
            else if (mevcutDurum == OyunDurumu.OyunSonu && basildi && e.KeyCode == Keys.Enter) mevcutDurum = OyunDurumu.Menü;
        }

        private void Saldir()
        {
            ruya.SaldiriyorMu = true; ruya.AnimKare = 0;
            Rectangle vurusAlani = ruya.SagaBakiyor ? new Rectangle(ruya.X + 40, ruya.Y, 130, 90) : new Rectangle(ruya.X - 90, ruya.Y, 130, 90);
            Rectangle bossHitbox = new Rectangle(boss.X - 20, boss.Y - 20, 260, 310);

            if (vurusAlani.IntersectsWith(bossHitbox)) boss.Can -= 5;
        }
        #endregion

        #region MUHAMMED EMİN ÇELİK: Vektörel Savaş Sistemi ve Çarpışma Fiziği
        private void SavasSistemiUygula()
        {
            // Vektörel Nişan Alma (Boss -> Oyuncu)
            boss.MermiZamanlayici++;
            if (boss.MermiZamanlayici > 90)
            {
                Mermi n = new Mermi();
                n.Kutu = new Rectangle(boss.X + 80, boss.Y + 100, 35, 35);

                float dx = (ruya.X + 30) - (boss.X + 80);
                float dy = (ruya.Y + 30) - (boss.Y + 100);
                float mesafe = (float)Math.Sqrt(dx * dx + dy * dy); // Pisagor Teoremi ile mesafe

                if (mesafe != 0)
                {
                    float hiz = (aktifBoss == BossSecimi.Wrath) ? 12f : 8f;
                    n.HizX = (dx / mesafe) * hiz;
                    n.HizY = (dy / mesafe) * hiz;
                    mermiler.Add(n);
                }
                boss.MermiZamanlayici = 0;
            }

            // Mermi Hareketi ve Hasar Çarpışması
            for (int i = mermiler.Count - 1; i >= 0; i--)
            {
                Mermi m = mermiler[i];
                m.Kutu.X += (int)m.HizX;
                m.Kutu.Y += (int)m.HizY;
                mermiler[i] = m;

                if (m.Kutu.IntersectsWith(new Rectangle(ruya.X, ruya.Y, 60, 60)))
                {
                    ruya.Can -= 10; mermiler.RemoveAt(i);
                }
                else if (m.Kutu.X < -100 || m.Kutu.X > 900 || m.Kutu.Y > 600) mermiler.RemoveAt(i);
            }

            if (boss.Can <= 0) { sonucMesaji = "ZAFER!"; mevcutDurum = OyunDurumu.OyunSonu; }
            if (ruya.Can <= 0) { sonucMesaji = "KAYBETTİN..."; mevcutDurum = OyunDurumu.OyunSonu; }
        }

        private void FizikVeHareketUygula()
        {
            if (ruya.SolaGit && ruya.X > -20) { ruya.X -= 6; ruya.SagaBakiyor = false; }
            if (ruya.SagaGit && ruya.X < 720) { ruya.X += 6; ruya.SagaBakiyor = true; }

            int eskiAyakY = ruya.Y + 90;
            ruya.Y += ruya.DikeyHiz;
            ruya.DikeyHiz += ruya.Yercekimi;
            ruya.ZeminKontrol = false;

            // Platform Çarpışma Testi (One-Way Platform)
            if (ruya.DikeyHiz > 0 && !ruya.SBasildi)
            {
                foreach (var p in platformlar)
                {
                    int ayakSol = ruya.X + 30;
                    int ayakSag = ruya.X + 60;
                    int yeniAyakY = ruya.Y + 90;

                    if (ayakSag > p.Kutu.X && ayakSol < p.Kutu.Right)
                    {
                        if (eskiAyakY <= p.Kutu.Y && yeniAyakY >= p.Kutu.Y)
                        {
                            ruya.Y = p.Kutu.Y - 90;
                            ruya.DikeyHiz = 0;
                            ruya.ZeminKontrol = true;
                            break;
                        }
                    }
                }
            }

            if (ruya.Y >= 400) { ruya.Y = 400; ruya.DikeyHiz = 0; ruya.ZeminKontrol = true; }

            if (ruya.SaldiriyorMu)
            {
                ruya.AnimSayac++;
                if (ruya.AnimSayac > 3) { ruya.AnimKare++; ruya.AnimSayac = 0; }
                if (ruya.AnimKare >= 8) { ruya.AnimKare = 0; ruya.SaldiriyorMu = false; }
            }
        }
        #endregion

        #region ARİ CAN BORA: Arena Tasarımı ve Görsel Çizim Katmanları
        private void PlatformlariKur()
        {
            platformlar.Clear();
            platformlar.Add(new Zemin(0, 455, 140, 15));
            platformlar.Add(new Zemin(150, 380, 140, 15));
            platformlar.Add(new Zemin(0, 285, 130, 15));
            platformlar.Add(new Zemin(165, 205, 120, 15));
            platformlar.Add(new Zemin(660, 455, 140, 15));
            platformlar.Add(new Zemin(510, 380, 140, 15));
            platformlar.Add(new Zemin(670, 285, 130, 15));
            platformlar.Add(new Zemin(515, 205, 120, 15));
        }

        private void EkranaCiz(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (mevcutDurum == OyunDurumu.Menü)
            {
                g.Clear(Color.Black);
                g.DrawString("1: SORROW | 2: WRATH", new Font("Consolas", 25), Brushes.White, 220, 250);
            }
            else if (mevcutDurum == OyunDurumu.Savaş)
            {
                // Katman 1: Arkaplan
                Image bg = (aktifBoss == BossSecimi.Sorrow) ? bgSorrow : bgWrath;
                if (bg != null) g.DrawImage(bg, 0, 0, 800, 600); else g.Clear(Color.DarkGray);

                // Katman 2: Boss
                Image bImg = (aktifBoss == BossSecimi.Sorrow) ? imgSorrow : imgWrath;
                if (bImg != null)
                {
                    int bw = (aktifBoss == BossSecimi.Sorrow) ? bImg.Width / 5 : bImg.Width / 4;
                    SeffafCiz(g, bImg, new Rectangle(boss.X, boss.Y, 220, 270), new Rectangle(0, 0, bw, bImg.Height / 3), SEFFALIK_TOLERANSI);
                }

                // Katman 3: Oyuncu (RotateFlip AI Desteklidir)
                if (imgRuya != null)
                {
                    int satir = (ruya.AnimKare >= 4) ? 1 : 0;
                    Rectangle k = new Rectangle((ruya.AnimKare % 4) * (imgRuya.Width / 4), satir * (imgRuya.Height / 2), imgRuya.Width / 4, imgRuya.Height / 2);

                    if (!ruya.SagaBakiyor) imgRuya.RotateFlip(RotateFlipType.RotateNoneFlipX); // Ayna efekti
                    SeffafCiz(g, imgRuya, new Rectangle(ruya.X, ruya.Y, 90, 90), k, SEFFALIK_TOLERANSI);
                    if (!ruya.SagaBakiyor) imgRuya.RotateFlip(RotateFlipType.RotateNoneFlipX); // Eski haline çevir
                }

                // Katman 4: Mermiler
                Image mImg = (aktifBoss == BossSecimi.Sorrow) ? imgMermiSorrow : imgMermiWrath;
                foreach (var n in mermiler)
                {
                    if (mImg != null) SeffafCiz(g, mImg, n.Kutu, new Rectangle(0, 0, mImg.Width, mImg.Height), SEFFALIK_TOLERANSI);
                    else g.FillEllipse(Brushes.Yellow, n.Kutu);
                }

                // Katman 5: UI (Arayüz)
                g.DrawString("RUYA", new Font("Arial", 10, FontStyle.Bold), Brushes.Lime, 20, 15);
                g.FillRectangle(Brushes.DarkRed, 20, 35, 200, 15);
                g.FillRectangle(Brushes.Lime, 20, 35, ruya.Can * 2, 15);

                string bN = aktifBoss.ToString().ToUpper();
                g.DrawString(bN, new Font("Arial", 10, FontStyle.Bold), Brushes.Red, 580, 15);
                g.FillRectangle(Brushes.DarkRed, 580, 35, 200, 15);
                g.FillRectangle(Brushes.Red, 580, 35, boss.Can * 2, 15);
            }
            else
            {
                g.Clear(Color.Black);
                g.DrawString(sonucMesaji + "\nENTER: MENÜ", new Font("Consolas", 35), Brushes.White, 220, 240);
            }
        }
        #endregion

        private void OyunDongusu(object sender, EventArgs e)
        {
            // Oyun döngüsü tüm mekanikleri tetikler
            if (mevcutDurum == OyunDurumu.Savaş)
            {
                FizikVeHareketUygula();
                SavasSistemiUygula();
            }
            this.Invalidate();
        }
    }
}