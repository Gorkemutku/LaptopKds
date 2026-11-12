# 💻 LaptopKDS — Akıllı Laptop Karar Destek Sistemi

**LaptopKDS**, kullanıcıların bütçelerine ve kullanım profillerine (Yazılımcı, Oyuncu, Öğrenci, Tasarımcı) göre piyasadaki en uygun dizüstü bilgisayarları **Çok Kriterli Karar Verme (ÇKKV / MCDM)** yöntemlerinden **AHP (Analitik Hiyerarşi Süreci)** ve **TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution)** algoritmalarıyla analiz edip sıralayan modern bir Karar Destek Sistemidir.

---

## 🚀 Proje Mimarisi

```
laptopkds/
├── backend/
│   └── LaptopKDS.Api/          # .NET 10 Web API + EF Core + SQLite
│       ├── Controllers/        # Recommendation, Profiles, Laptops API
│       ├── Data/               # AppDbContext, DataSeeder (37+ cihaz, 4 profil)
│       ├── DTOs/               # İstek & Yanıt veri transfer modelleri
│       ├── Models/             # Laptop, DecisionProfile, ProfileCriterionWeight
│       ├── Services/           # TopsisService (TOPSIS vektör normalizasyon & skorlama)
│       └── Workers/            # ScraperBackgroundService (Akakçe veri çekme altyapısı)
│
└── frontend/
    └── laptop-kds-ui/          # React 18 + Vite SPA
        ├── src/
        │   ├── api/            # kdsApi.js (Axios API istemcisi)
        │   ├── components/     # Header, Sidebar, ResultsPanel, LaptopCard
        │   ├── App.jsx         # Ana durum ve veri yönetimi
        │   ├── index.css       # Glassmorphism dark mode tasarım sistemi
        │   └── main.jsx        # React root
```

---

## 🧮 Karar Destek Algoritması: TOPSIS & AHP

Sistem her cihaz için 6 temel kriteri değerlendirir:

| Kriter | Kriter Tipi | Amaç | Ağırlık Dağılımı |
|---|---|---|---|
| **Fiyat (TL)** | Maliyet (Cost) | Minimize etme (Düşük fiyat avantajı) | Öğrenci (%30), Tasarımcı (%12), Yazılımcı (%10), Oyuncu (%10) |
| **CPU Skoru** | Fayda (Benefit) | Maximize etme (Cinebench Multi-Core) | Yazılımcı (%35), Oyuncu (%25), Tasarımcı (%22), Öğrenci (%15) |
| **GPU Skoru** | Fayda (Benefit) | Maximize etme (3DMark TimeSpy) | Oyuncu (%40), Tasarımcı (%28), Yazılımcı (%10), Öğrenci (%5) |
| **RAM (GB)** | Fayda (Benefit) | Maximize etme | Yazılımcı (%25), Tasarımcı (%22), Oyuncu (%15), Öğrenci (%15) |
| **Ağırlık (KG)**| Maliyet (Cost) | Minimize etme (Hafiflik) | Öğrenci (%15), Yazılımcı (%8), Tasarımcı (%8), Oyuncu (%5) |
| **Pil Ömrü (sa)**| Fayda (Benefit) | Maximize etme | Öğrenci (%20), Yazılımcı (%12), Tasarımcı (%8), Oyuncu (%5) |

### TOPSIS Adımları:
1. **Karar Matrisi Oluşturma:** $m$ alternatif cihaz ve $n=6$ kriterden oluşan matris $X$.
2. **Vektör Normalizasyonu:** $R_{ij} = \frac{x_{ij}}{\sqrt{\sum_{k=1}^m x_{kj}^2}}$
3. **Ağırlıklı Normalize Matris:** $V_{ij} = R_{ij} \times w_j$ (Profil AHP ağırlıkları)
4. **Pozitif ($A^*$) ve Negatif ($A^-$) İdeal Çözümler:**
   - Fayda kriterleri için: $A^*_j = \max(V_{ij})$, $A^-_j = \min(V_{ij})$
   - Maliyet kriterleri için: $A^*_j = \min(V_{ij})$, $A^-_j = \max(V_{ij})$
5. **Öklid Uzaklıkları:** $S^*_i = \sqrt{\sum (V_{ij} - A^*_j)^2}$, $S^-_i = \sqrt{\sum (V_{ij} - A^-_j)^2}$
6. **Göreli Yakınlık Skoru:** $C^*_i = \frac{S^-_i}{S^*_i + S^-_i}$ ($0 \le C^*_i \le 1$)
7. **Sıralama:** $C^*_i$ değerine göre en yüksekten düşüğe doğru sıralanır ve profile özel dinamik açıklama üretilir.

---

## 🛠️ Kurulum ve Çalıştırma

### 1. Gereksinimler
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 18+](https://nodejs.org/)

### 2. Backend'i Başlatma
```bash
cd backend/LaptopKDS.Api
dotnet run
```
Backend `http://localhost:5000` adresinde çalışır ve ilk açılışta SQLite veritabanını (`laptopkds.db`) otomatik oluşturup tohum (seed) verilerini yükler.

### 3. Frontend'i Başlatma
```bash
cd frontend/laptop-kds-ui
npm install
npm run dev
```
Frontend `http://localhost:5173` adresinde açılır ve backend API'sine proxy üzerinden bağlanır.

---

## 📡 API Uç Noktaları

- `GET /health` — Servis sağlık kontrolü
- `GET /api/profiles` — Karar profilleri ve AHP ağırlık listesi
- `POST /api/recommendation` — TOPSIS öneri motoru
  ```json
  {
    "budgetMax": 50000,
    "profileId": 1
  }
  ```
- `GET /api/laptops` — Tüm laptop envanteri
- `POST /api/laptops` — Yeni laptop ekleme
- `PUT /api/laptops/{id}` — Laptop güncelleme
- `DELETE /api/laptops/{id}` — Laptop pasife alma (Soft delete)
