import React from 'react';
import LaptopCard from './LaptopCard';

export default function ResultsPanel({
  recommendations,
  totalFound,
  loading,
  error,
  onRetry
}) {
  return (
    <main className="results-panel">
      {/* ─── Başlık ve Özet Bilgi ─── */}
      <div className="results-header">
        <h2 className="results-title">Önerilen Laptoplar</h2>
        {!loading && !error && (
          <span className="results-count-badge">
            {totalFound} Alternatif Listelendi
          </span>
        )}
      </div>

      {/* ─── Yükleniyor (Skeleton) ─── */}
      {loading && (
        <div className="cards-grid">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton" style={{ height: '14px', width: '30%', marginBottom: '10px' }} />
              <div className="skeleton" style={{ height: '20px', width: '75%', marginBottom: '14px' }} />
              <div className="skeleton" style={{ height: '28px', width: '45%', marginBottom: '20px' }} />
              <div className="skeleton" style={{ height: '10px', width: '100%', marginBottom: '20px' }} />
              <div className="skeleton" style={{ height: '64px', width: '100%', marginBottom: '16px' }} />
              <div className="skeleton" style={{ height: '40px', width: '100%' }} />
            </div>
          ))}
        </div>
      )}

      {/* ─── Hata Durumu ─── */}
      {!loading && error && (
        <div className="error-state">
          <div style={{ fontSize: '36px' }}>⚠️</div>
          <div style={{ fontWeight: 700, fontSize: '17px' }}>Hesaplama Hatası</div>
          <p style={{ maxWidth: '400px', margin: '4px 0 12px' }}>{error}</p>
          {onRetry && (
            <button
              type="button"
              className="budget-preset-btn active"
              onClick={onRetry}
              style={{ padding: '8px 20px', fontSize: '13px' }}
            >
              🔄 Yeniden Dene
            </button>
          )}
        </div>
      )}

      {/* ─── Boş Sonuç ─── */}
      {!loading && !error && recommendations.length === 0 && (
        <div className="empty-state">
          <div className="empty-state-icon">🔍</div>
          <div className="empty-state-title">Uygun laptop bulunamadı</div>
          <div className="empty-state-desc">
            Belirttiğiniz bütçe ve profil kriterlerine uygun cihaz bulunamadı. Bütçeyi yükseltmeyi veya farklı bir profil seçmeyi deneyebilirsiniz.
          </div>
        </div>
      )}

      {/* ─── Sonuç Kartları ─── */}
      {!loading && !error && recommendations.length > 0 && (
        <div className="cards-grid">
          {recommendations.map((laptop, index) => (
            <LaptopCard
              key={laptop.id ?? laptop.Id ?? index}
              laptop={laptop}
              rank={index + 1}
            />
          ))}
        </div>
      )}
    </main>
  );
}
