import http from './http';

const awardApi = {
  // Get all awards with user's progress
  getAwards: async () => {
    return http.get('/api/awards');
  },

  // Check and grant new awards
  checkAwards: async () => {
    return http.post('/api/awards/check');
  },

  // Get progress toward all awards
  getAwardProgress: async () => {
    return http.get('/api/awards/progress');
  },
};

export default awardApi;

