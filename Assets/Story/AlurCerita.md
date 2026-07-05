# ALUR CERITA — "Jamu Desa" (Kelompok 5) v2

---

## PROLOG — Tutorial Dasar (Step 0–6)

| # | Tipe | Param | Objektif |
|---|------|-------|----------|
| 0 | Move | — | Gunakan W A S D untuk bergerak |
| 1 | Hoe | — | Pergi ke kebun dan cangkul satu petak tanah |
| 2 | Talk | Nenek | Kembali ke Nenek — dia kasih bibit Jahe & Kunyit |
| 3 | OpenBag | — | Tekan [B] untuk melihat Tas / Inventory |
| 4 | OpenRecipe | — | Tekan [Tab] untuk melihat Buku Resep turun-temurun |
| 5 | Cook | Jamu Jahe | Buat Jamu Jahe untuk Nenek (lihat bahan → tanam → masak) |
| 6 | GiveItem | Nenek (item: Jamu Jahe) | Berikan Jamu Jahe ke Nenek |

**→ "Prolog Selesai • Chapter 1 Dimulai"**

---

## CHAPTER 1 — Jamu untuk Bapaknya Laras (Step 7–12)

| # | Tipe | Param | Objektif |
|---|------|-------|----------|
| 7 | Talk | Laras | Jalan-jalan keliling map, temui Laras — dia cerita bapaknya sakit |
| 8 | Talk | Darma | Temui Pak Darma dan bicara dengannya |
| 9 | OpenRecipe | — | Buka [Tab] untuk lihat resep yang cocok |
| 10 | Talk | Nisa | Cari toko Nisa, beli bibit untuk jamu |
| 11 | Cook | Jamu Pegal Linu | Masak Jamu Pegal Linu di tungku |
| 12 | GiveItem | Darma (item: Jamu Pegal Linu) | Antar jamu ke Pak Darma — dia sembuh |

**Reward:** Resep Lv.2 terbuka • Area baru  
**→ "Chapter 1 Selesai • Chapter 2 Dimulai"**

---

## CHAPTER 2 — Sembuhkan 3 Warga (Step 13–15)
**Mekanik: Paralel / Counter Quest (urutan bebas)**

| # | Tipe | Param | Objektif |
|---|------|-------|----------|
| 13–15 | Counter | heal3 | Sembuhkan 3 warga desa (0/3) |

### Pasien & Jamu yang Dibutuhkan
| NPC | Sakit | Jamu yang harus diserahkan |
|-----|-------|---------------------------|
| **Ratri** (Pemburu) | Keracunan hutan | Ramuan Penurun Panas |
| **Bahri** (Nelayan) | Mual karena air sungai | Ramuan Anti Mual |
| **Darsono** (Kepala Desa) | Pusing & kelelahan | Jamu Sehat Desa |

Player bebas pilih urutan. Mekaniknya:
1. Bicara NPC pasien (dialog keluhan)
2. Lihat resep di Tab → tanam bahan → masak
3. Bicara lagi dengan NPC sambil bawa jamu → otomatis diserahkan → counter +1
4. Saat counter = 3/3 → chapter selesai

**Reward:** Resep Lv.3 terbuka  
**→ "Chapter 2 Selesai • Chapter 3 Dimulai"**

---

## CHAPTER 3 — Akhir Cerita & Pilih Pacar (Step 16–19)

| # | Tipe | Param | Objektif |
|---|------|-------|----------|
| 16 | Talk | Ratri | Ngobrol dengan Ratri (dia berterima kasih) |
| 17 | Talk | Laras | Ngobrol dengan Laras (dia senang bapaknya sembuh) |
| 18 | Talk | Nisa | Ngobrol dengan Nisa (dia senang tokonya makin ramai) |
| 19 | Choice | pacar | Pilih: siapa yang ingin jadi pasangan Robby? (Laras / Nisa / Ratri) |

**→ "Chapter 3 Selesai • Terima Kasih Sudah Bermain!"**
**Ending dialog berbeda tergantung siapa yang dipilih.**

---

## DAFTAR KARAKTER

| Nama | npcId | Peran | Chapter |
|------|-------|-------|---------|
| Robby | (MC) | Peracik Jamu / Hero | Semua |
| Nenek Rukmini | `Nenek` | Mentor, penjaga resep | Prolog |
| Laras Ayuningtyas | `Laras` | Peternak, pembuka Ch1 | Ch1, Ch3 |
| Pak Darma | `Darma` | Pasien sendi (Ch1) | Ch1 |
| Nisa Rahma | `Nisa` | Penjaga toko bahan | Ch1, Ch3 |
| Pak Seno | `Seno` | Pemilik toko (side) | Side |
| Ratri Mahesa | `Ratri` | Pemburu, keracunan | Ch2, Ch3 |
| Bahri Salam | `Bahri` | Nelayan, mual | Ch2 |
| Darsono | `Darsono` | Kepala Desa, pusing | Ch2 |

---

## SISTEM RESEP BERTAHAP (GEMBOK)

| Tier | Syarat unlock | Resep |
|------|---------------|-------|
| Terbuka | Awal game | Jamu Jahe, Jamu Kunyit, Wedang Jahe |
| level1 | Reward Prolog selesai | Pegal Linu, Beras Kencur, Kencur, Temulawak, Wedang Herbal |
| level2 | Reward Chapter 1 selesai | Penurun Panas, Sehat Desa, Penyegar, Anti Mual |
| level3 | Reward Chapter 2 selesai | Stamina, Pernapasan |

---

## KONTROL INPUT

| Tombol | Fungsi |
|--------|--------|
| W A S D | Bergerak |
| G | Interaksi NPC / lanjut dialog |
| F | Cangkul / Siram |
| H | Pilih bibit tanam |
| B | Buka Tas (Inventory) |
| Tab | Buka Buku Resep |
| Esc | Tutup panel terbuka |
