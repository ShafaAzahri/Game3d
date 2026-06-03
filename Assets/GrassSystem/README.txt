========================================
  INTERACTIVE GRASS SYSTEM - Setup Guide
========================================

Sistem rumput interaktif yang bergerak kena angin dan bereaksi 
ketika player/object menyentuhnya. Bisa di-paint di terrain.

========================================
  CARA SETUP
========================================

1. GENERATE GRASS MESH
   - Di Unity, klik menu: Tools > Grass System > Generate Grass Blade Mesh
   - Atau: Tools > Grass System > Generate Grass Clump Mesh
   - Mesh akan muncul di Assets/GrassSystem/Meshes/

2. SETUP MATERIAL
   - Material sudah ada di: Assets/GrassSystem/Materials/InteractiveGrass.mat
   - Pastikan shader-nya "Custom/InteractiveGrass"
   - Atur warna Base Color dan Tip Color sesuai style game kamu

3. PAINT DI TERRAIN
   - Select Terrain kamu
   - Buka tab "Paint Details" (ikon rumput)
   - Klik "Edit Details..." > "Add Detail Mesh"
   - Detail Mesh: pilih GrassBlade atau GrassClump dari step 1
   - Material: pilih InteractiveGrass.mat
   - Min/Max Width: 0.5 / 1.5
   - Min/Max Height: 0.5 / 1.2
   - Noise Spread: 0.5
   - Sekarang kamu bisa PAINT rumput di terrain!

4. SETUP INTERACTION MANAGER
   - Buat Empty GameObject, nama "GrassManager"
   - Add Component: GrassInteractionManager
   - (Opsional) Add Component: GrassTerrainPainter

5. SETUP PLAYER INTERACTION
   - Select Player GameObject
   - Add Component: GrassInteractor
   - Atur Position Offset agar sesuai kaki player (biasanya Y = -0.5)

6. TAMBAH INTERACTOR LAIN (opsional)
   - Bisa tambah GrassInteractor ke hewan, NPC, atau object lain
   - Max 10 interactor aktif bersamaan

========================================
  SETTINGS
========================================

WIND:
- Wind Speed: kecepatan angin (1.0 default)
- Wind Strength: seberapa kuat rumput bergoyang (0.3 default)
- Wind Direction: arah angin (X, Y, Z)

INTERACTION:
- Interaction Strength: seberapa kuat rumput membungkuk (1.5 default)
- Interaction Radius: jarak deteksi dari player (2.0 default)

COLOR:
- Base Color: warna pangkal rumput (hijau gelap)
- Tip Color: warna ujung rumput (hijau terang)

========================================
  TIPS
========================================

- Untuk style low-poly/stylized, gunakan warna solid tanpa texture
- Set Alpha Cutoff ke 0 jika pakai mesh tanpa texture
- Kombinasikan dengan TreeWind.cs yang sudah ada untuk konsistensi
- Brush size dan density bisa diatur di terrain paint tool
