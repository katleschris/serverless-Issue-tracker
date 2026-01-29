import axios from 'axios';

// API URL from environment variable
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3000';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

// Type definitions
export type IssueStatus = 'Open' | 'InProgress' | 'Done';
export type IssuePriority = 'Low' | 'Medium' | 'High';

export interface Issue {
  id: string;
  title: string;
  description: string;
  status: IssueStatus;
  priority: IssuePriority;
  createdAt: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    message: string;
  };
}

export interface CreateIssueData {
  title: string;
  description: string;
  priority: IssuePriority;
}

export interface UpdateIssueData {
  title?: string;
  description?: string;
  status?: string;
  priority?: IssuePriority;
}

export const issueApi = {
  // Get all issues
  getAllIssues: async (status?: IssueStatus | null): Promise<ApiResponse<Issue[]>> => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<Issue[]>>('/issues', { params });
    return response.data;
  },

  // Get single issue
  getIssue: async (id: string): Promise<ApiResponse<Issue>> => {
    const response = await api.get<ApiResponse<Issue>>(`/issues/${id}`);
    return response.data;
  },

  // Create new issue
  createIssue: async (issueData: CreateIssueData): Promise<ApiResponse<Issue>> => {
    const response = await api.post<ApiResponse<Issue>>('/issues', issueData);
    return response.data;
  },

  // Update issue
  updateIssue: async (id: string, updates: UpdateIssueData): Promise<ApiResponse<Issue>> => {
    const response = await api.put<ApiResponse<Issue>>(`/issues/${id}`, updates);
    return response.data;
  },

  // Delete issue
  deleteIssue: async (id: string): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/issues/${id}`);
    return response.data;
  },
};

export default api;
