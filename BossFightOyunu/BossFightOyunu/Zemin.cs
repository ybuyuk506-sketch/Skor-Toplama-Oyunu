using System;
using System.Drawing;

namespace BossFightOyunu
{
    // YAZAN: Muhammed Emin Çelik (Tek yönlü platform hitbox yapısı)
    public class Zemin
    {
        public Rectangle Kutu;

        public Zemin(int x, int y, int genislik, int yukseklik)
        {
            Kutu = new Rectangle(x, y, genislik, yukseklik);
        }
    }
}