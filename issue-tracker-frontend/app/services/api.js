import axios from 'axios';

// IMPORTANT: You'll update this URL after deploying your backend
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:3000';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add response interceptor to handle errors consistently
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export const issueApi = {
  // Get all issues
  getAllIssues: async (status = null) => {
    const params = status ? { status } : {};
    const response = await api.get('/issues', { params });
    return response.data;
  },

  // Get single issue
  getIssue: async (id) => {
    const response = await api.get(`/issues/${id}`);
    return response.data;
  },

  // Create new issue
  createIssue: async (issueData) => {
    const response = await api.post('/issues', issueData);
    return response.data;
  },

  // Update issue
  updateIssue: async (id, updates) => {
    const response = await api.put(`/issues/${id}`, updates);
    return response.data;
  },

  // Delete issue
  deleteIssue: async (id) => {
    const response = await api.delete(`/issues/${id}`);
    return response.data;
  },
};

export default api;