# CryptoFinder

Kripto para analizi ve taraması için temiz, SOLID, test edilebilir C# uygulaması. CryptoFinder, teknik analiz ve piyasa verisi filtreleme kullanarak umut verici kripto para adaylarını belirler.

## Özellikler

- **Piyasa Değeri Filtreleme**: Piyasa değeri, hacim, FDV/MC oranı, cirolama ve sıralamaya göre kripto paraları tarar
- **Teknik Analiz**: EMA200, ADX, ATR, Donchian Kanalı ve Göreceli Güç göstergelerini hesaplar
- **Skorlama Sistemi**: Birden fazla teknik faktöre dayalı kapsamlı 0-100 skorlama
- **Spread & Derinlik Analizi**: Bid-ask spread'leri ve emir defteri derinliğine göre filtreler
- **Erken Durma**: Yeni aday bulunamadığında erken sonlandırma ile optimize edilmiş sayfalama
- **Hata Yönetimi**: API çağrıları için üstel geri çekilme ile sağlam yeniden deneme mantığı
- **CSV Dışa Aktarma**: Sonuçları CSV formatında isteğe bağlı dışa aktarma

## Mimari

Uygulama temiz mimari ile SOLID prensiplerini takip eder:

- **Config/**: Yapılandırma sabitleri ve ayarlar
- **Data/**: Veri sağlayıcıları (CoinGecko, Binance API'leri)
- **Interfaces/**: Bağımlılık enjeksiyonu için servis sözleşmeleri
- **Models/**: Veri transfer nesneleri ve domain modelleri
- **Net/**: Ağ sağlayıcıları ve API istemcileri
- **Services/**: İş mantığı ve analiz servisleri
- **Util/**: HTTP, CSV dışa aktarma vb. için yardımcı sınıflar

## Kullanım

### Temel Kullanım

```bash
dotnet run
```

### CSV'ye Dışa Aktarma

```bash
dotnet run -- --export-csv
```

### Yapılandırma

Tüm eşik değerleri ve parametreler `Config/Settings.cs` dosyasında merkezi olarak tutulur:

- **Piyasa Değeri Filtreleri**: MC_MIN, VOL24H_MIN, FDV_MC_MAX, TURNOVER_MIN, RANK_MAX
- **Teknik Analiz**: EMA_LEN, ADX_MIN, ATR_MAX_PCT, RS_LOOKBACK_DAYS
- **Skorlama Ağırlıkları**: W_EMA, W_ADX, W_RS, W_ATR, W_DCH
- **API Ayarları**: HTTP_TIMEOUT_SECONDS, MAX_RETRY_ATTEMPTS, MAX_CONCURRENT_REQUESTS

### Sıkı Rejim Yapılandırması

Daha muhafazakar filtreleme için `Settings.TightRegime` içinde alternatif daha sıkı ayarlar mevcuttur.

## İş Kuralları

Uygulama aşağıdaki filtreleme ve skorlama kurallarını uygular:

### Piyasa Değeri Filtreleri
- Minimum piyasa değeri: $100M
- Minimum 24 saatlik hacim: $10M
- Maksimum FDV/MC oranı: 10x
- Minimum cirolama: %3
- Maksimum sıralama: 500
- Maksimum spread: %0.5
- Minimum derinlik: $100K

### Teknik Analiz Kuralları
- Kapanış fiyatı EMA200'ün üstünde olmalı
- ADX ≥ 15 olmalı
- BTC'ye göre Göreceli Güç (30 günlük) pozitif olmalı
- ATR yüzdesi ≤ %25 olmalı
- Fiyat Donchian alt bandına yakın olmamalı (%5 eşik)

### Skorlama Sistemi (0-100)
- EMA Kuralı: 35 puan
- ADX Gücü: 15 puan (ölçekli)
- Göreceli Güç: 25 puan (ölçekli)
- ATR Yüzdesi: 15 puan (ters ölçekli)
- Donchian Kuralı: 10 puan

## Bağımlılıklar

- **Microsoft.Extensions.Hosting**: Bağımlılık enjeksiyonu ve hosting
- **Microsoft.Extensions.Http**: HTTP istemci fabrikası
- **Microsoft.Extensions.Logging**: Yapılandırılmış günlükleme
- **Skender.Stock.Indicators**: Teknik analiz göstergeleri
- **System.Text.Json**: JSON serileştirme

## Hata Yönetimi

- **Yeniden Deneme Mantığı**: HTTP istekleri için üstel geri çekilme
- **Hız Sınırlaması**: 429 yanıtlarını gecikmelerle işler
- **Zaman Aşımları**: İstek başına (15s) ve genel (5dk) zaman aşımları
- **Zarif Bozulma**: Çökmek yerine boş sonuçlar döndürür
- **Eşzamanlılık Kontrolü**: SemaphoreSlim eşzamanlı istekleri sınırlar

## Performans Optimizasyonları

- **Tek API Çağrıları**: Book ticker'lar ve işlem gören semboller için toplu istekler
- **Derinlik Filtreleme**: Sadece kısa listeye alınan semboller için emir defteri derinliği alır
- **Erken Durma**: Yeni aday olmayan 2 ardışık sayfa sonrası sonlandırır
- **Eşzamanlılık Limitleri**: Maksimum 6-8 eşzamanlı istek
- **Bellek Verimliliği**: Veriyi akışla işler ve kaynakları düzgün şekilde serbest bırakır

## Geliştirme

### Derleme

```bash
dotnet build
```

### Testleri Çalıştırma

```bash
dotnet test
```

### Kod Stili

- C# 10+ özellikleri
- Nullable referans türleri etkin
- DTO'lar için değişmez record türleri
- Her yerde async/await (hiç .Result veya .Wait() yok)
- Tüm G/Ç işlemleri için CancellationToken desteği
- Tek Sorumluluk Prensibi
- Test edilebilirlik için Bağımlılık Enjeksiyonu

## Lisans

Bu proje eğitim ve araştırma amaçlıdır. CoinGecko ve Binance API hizmet şartlarına uyumluluğu sağlayın.
