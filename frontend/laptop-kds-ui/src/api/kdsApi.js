import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

/**
 * Tüm karar profillerini getirir (Yazılımcı, Oyuncu, Öğrenci, Tasarımcı)
 * @returns {Promise<Array>}
 */
export async function getProfiles() {
  const response = await api.get('/profiles');
  return response.data;
}

/**
 * TOPSIS algoritmasıyla laptop önerilerini getirir
 * @param {number} budgetMax - Maksimum bütçe (TL)
 * @param {number} profileId - Seçilen profil ID'si
 * @returns {Promise<{totalFound: number, recommendations: Array}>}
 */
export async function getRecommendations(budgetMax, profileId) {
  const response = await api.post('/recommendation', { budgetMax, profileId });
  return response.data;
}

export default api;
