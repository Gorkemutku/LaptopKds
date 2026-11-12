import React from 'react';

export default function LaptopCard({ laptop, rank }) {
  const brand = laptop.brand ?? laptop.Brand ?? '';
  const model = laptop.model ?? laptop.Model ?? '';
  const price = laptop.price ?? laptop.Price ?? 0;
  const cpuScore = laptop.cpuScore ?? laptop.CpuScore ?? 0;
  const gpuScore = laptop.gpuScore ?? laptop.GpuScore ?? 0;
  const ram = laptop.ramCapacityGB ?? laptop.RamCapacityGB ?? 0;
  const weight = laptop.weightKG ?? laptop.WeightKG ?? 0;
  const battery = laptop.batteryLifeHours ?? laptop.BatteryLifeHours ?? 0;
  const sourceUrl = laptop.sourceUrl ?? laptop.SourceUrl ?? '#';
  const percent = laptop.compatibilityPercent ?? laptop.CompatibilityPercent ?? 0;
  const explanation = laptop.explanationText ?? laptop.ExplanationText ?? '';

  const rankClass = rank === 1 ? 'rank-1' : rank === 2 ? 'rank-2' : rank === 3 ? 'rank-3' : '';
  const scoreClass = percent >= 80 ? 'high' : percent >= 65 ? 'mid' : 'low';
  const scoreGradient = percent >= 80
    ? 'var(--gradient-score-high)'
    : percent >= 65
    ? 'var(--gradient-score-mid)'
    : 'var(--gradient-score-low)';

  return (
    <article className={`laptop-card ${rankClass}`}>
      {/* ─── Başlık ve Fiyat ─── */}
      <div className="card-header">
        <div className="card-brand">{brand}</div>
        <h3 className="card-model" title={`${brand} ${model}`}>
          {model}
        </h3>
        <div className="card-price">
          {Number(price).toLocaleString('tr-TR')}
          <span className="card-price-unit">₺</span>
        </div>
      </div>

      {/* ─── TOPSIS Skoru ─── */}
      <div className="score-section">
        <div className="score-header">
          <span className="score-label">TOPSIS Uyum Skoru</span>
          <span className={`score-value ${scoreClass}`}>%{percent.toFixed(1)}</span>
        </div>
        <div className="score-bar-track">
          <div
            className="score-bar-fill"
            style={{
              width: `${Math.min(100, Math.max(5, percent))}%`,
              background: scoreGradient,
            }}
          />
        </div>
      </div>

      {/* ─── Kriter Özellikleri ─── */}
      <div className="specs-grid">
        <div className="spec-chip">
          <span className="spec-chip-icon">⚡</span>
          <span className="spec-chip-value">{cpuScore.toLocaleString('tr-TR')}</span>
          <span className="spec-chip-label">CPU Skor</span>
        </div>
        <div className="spec-chip">
          <span className="spec-chip-icon">🎮</span>
          <span className="spec-chip-value">{gpuScore.toLocaleString('tr-TR')}</span>
          <span className="spec-chip-label">GPU Skor</span>
        </div>
        <div className="spec-chip">
          <span className="spec-chip-icon">💾</span>
          <span className="spec-chip-value">{ram} GB</span>
          <span className="spec-chip-label">RAM</span>
        </div>
        <div className="spec-chip">
          <span className="spec-chip-icon">⚖️</span>
          <span className="spec-chip-value">{Number(weight).toFixed(2)} kg</span>
          <span className="spec-chip-label">Ağırlık</span>
        </div>
        <div className="spec-chip">
          <span className="spec-chip-icon">🔋</span>
          <span className="spec-chip-value">{battery} sa</span>
          <span className="spec-chip-label">Pil Ömrü</span>
        </div>
        <div className="spec-chip">
          <span className="spec-chip-icon">🏆</span>
          <span className="spec-chip-value">#{rank}</span>
          <span className="spec-chip-label">Sıralama</span>
        </div>
      </div>

      {/* ─── KDS Açıklaması ─── */}
      {explanation && (
        <div className="explanation-box">
          <span className="explanation-icon">💡</span>
          <span className="explanation-text">{explanation}</span>
        </div>
      )}

      {/* ─── Satın Al / İncele Linki ─── */}
      <a
        href={sourceUrl}
        target="_blank"
        rel="noopener noreferrer"
        className="buy-btn"
      >
        <span>Akakçe'de İncele</span>
        <span>↗</span>
      </a>
    </article>
  );
}
