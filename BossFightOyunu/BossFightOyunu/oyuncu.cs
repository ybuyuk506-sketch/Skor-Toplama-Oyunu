using System;

namespace BossFightOyunu
{
    // YAZAN: Yiğit Buyuk (Karakter verileri ve durum yönetimi)
    public class Oyuncu
    {
        public int X = 100, Y = 400, Can = 100;
        public int DikeyHiz = 0, Yercekimi = 1;
        public bool ZeminKontrol = true, SagaBakiyor = true, SaldiriyorMu = false;
        public bool SolaGit, SagaGit, SBasildi;
        public int AnimKare = 0, AnimSayac = 0;
    }
}