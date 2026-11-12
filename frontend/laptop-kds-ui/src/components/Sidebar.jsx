import React from 'react';

const PRESET_BUDGETS = [15000, 25000, 35000, 50000, 75000, 100000];

export default function Sidebar({
  budget,
  setBudget,
  profiles,
  selectedProfileId,
  setSelectedProfileId,
  onSearch,
  loading
}) {
  const minBudget = 8000;
  const maxBudget = 150000;
  const sliderPct = Math.min(100, Math.max(0, ((budget - minBudget) / (maxBudget - minBudget)) * 100));

  return (
    <aside className="sidebar">
      {/* ─── Bütçe Bölümü ─── */}
      <div className="budget-section">
        <div className="section-label">Maksimum Bütçe</div>
        <div className="budget-display">
          {Number(budget).toLocaleString('tr-TR')} <span className="budget-unit">₺</span>
        </div>

        <div className="budget-slider-wrapper">
          <input
            type="range"
            min={minBudget}
            max={maxBudget}
            step="1000"
            value={budget}
            onChange={(e) => setBudget(Number(e.target.value))}
            className="budget-slider"
            style={{ '--slider-pct': `${sliderPct}%` }}
          />
          <div className="budget-range-labels">
            <span>{minBudget.toLocaleString('tr-TR')} ₺</span>
            <span>{maxBudget.toLocaleString('tr-TR')} ₺</span>
          </div>
        </div>

        <div className="budget-presets">
          {PRESET_BUDGETS.map((val) => (
            <button
              key={val}
              type="button"
              className={`budget-preset-btn ${budget === val ? 'active' : ''}`}
              onClick={() => setBudget(val)}
            >
              {val.toLocaleString('tr-TR')} ₺
            </button>
          ))}
        </div>
      </div>

      {/* ─── Profil Seçimi ─── */}
      <div className="profile-section">
        <div className="section-label">Kullanım Profili (AHP Ağırlıklı)</div>
        <div className="profile-grid">
          {profiles.map((p) => {
            const id = p.id ?? p.Id;
            const name = p.name ?? p.Name;
            const emoji = p.iconEmoji ?? p.IconEmoji ?? '💻';
            const desc = p.description ?? p.Description;
            const isSelected = id === selectedProfileId;

            return (
              <div
                key={id}
                className={`profile-card ${isSelected ? 'selected' : ''}`}
                onClick={() => setSelectedProfileId(id)}
                role="button"
                tabIndex={0}
              >
                <span className="profile-emoji">{emoji}</span>
                <div className="profile-name">{name}</div>
                <div className="profile-desc">{desc}</div>
              </div>
            );
          })}
        </div>
      </div>

      {/* ─── Hesapla Butonu ─── */}
      <button
        type="button"
        className="submit-btn"
        onClick={onSearch}
        disabled={loading}
      >
        <span className="submit-btn-icon">⚡</span>
        {loading ? 'Hesaplanıyor (TOPSIS)...' : 'En Uygun Laptopları Bul'}
      </button>
    </aside>
  );
}
