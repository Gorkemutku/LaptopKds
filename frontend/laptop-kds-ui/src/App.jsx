import React, { useState, useEffect, useCallback } from 'react';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import ResultsPanel from './components/ResultsPanel';
import { getProfiles, getRecommendations } from './api/kdsApi';

const DEFAULT_PROFILES = [
  { id: 1, name: 'Yazılımcı', iconEmoji: '👨‍💻', description: 'Derleme, IDE, sanal makine ve çoklu görev için yüksek CPU ve RAM performansı önceliklidir.' },
  { id: 2, name: 'Oyuncu', iconEmoji: '🎮', description: 'Yüksek GPU performansı, hızlı RAM ve tercihen hafif bir yapı aranır.' },
  { id: 3, name: 'Öğrenci', iconEmoji: '🎓', description: 'Uygun fiyat, uzun pil ömrü ve taşınabilirlik ön plandadır.' },
  { id: 4, name: 'Tasarımcı', iconEmoji: '🎨', description: 'Yüksek GPU, yeterli RAM ve renk doğruluğu için dengeli bir sistem tercih edilir.' },
];

export default function App() {
  const [profiles, setProfiles] = useState(DEFAULT_PROFILES);
  const [selectedProfileId, setSelectedProfileId] = useState(1);
  const [budget, setBudget] = useState(50000);
  const [recommendations, setRecommendations] = useState([]);
  const [totalFound, setTotalFound] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Karar profillerini API'den yükle
  useEffect(() => {
    async function loadProfiles() {
      try {
        const data = await getProfiles();
        if (Array.isArray(data) && data.length > 0) {
          setProfiles(data);
          const firstId = data[0].id ?? data[0].Id;
          if (firstId) setSelectedProfileId(firstId);
        }
      } catch (err) {
        console.warn('Profiller API’den alınırken hata oluştu, varsayılan profiller kullanılıyor:', err.message);
      }
    }
    loadProfiles();
  }, []);

  // TOPSIS önerilerini getir
  const fetchRecommendations = useCallback(async (targetBudget, targetProfileId) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getRecommendations(targetBudget, targetProfileId);
      const items = data.recommendations ?? [];
      setRecommendations(items);
      setTotalFound(data.totalFound ?? items.length);
    } catch (err) {
      console.error('Öneri getirme hatası:', err);
      const msg = err.response?.data?.error || err.message || 'Öneriler getirilirken beklenmeyen bir hata oluştu.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, []);

  // Sayfa ilk yüklendiğinde varsayılan bütçe ve profille otomatik hesapla
  useEffect(() => {
    fetchRecommendations(budget, selectedProfileId);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const handleSearch = () => {
    fetchRecommendations(budget, selectedProfileId);
  };

  return (
    <div className="app-wrapper">
      <Header />
      <div className="main-layout">
        <Sidebar
          budget={budget}
          setBudget={setBudget}
          profiles={profiles}
          selectedProfileId={selectedProfileId}
          setSelectedProfileId={setSelectedProfileId}
          onSearch={handleSearch}
          loading={loading}
        />
        <ResultsPanel
          recommendations={recommendations}
          totalFound={totalFound}
          loading={loading}
          error={error}
          onRetry={handleSearch}
        />
      </div>
    </div>
  );
}
