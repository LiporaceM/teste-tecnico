import axios, { AxiosError } from 'axios';
import type { AuthResponse, PagedResult, Task, TaskFilter, TaskInput, TaskStatus, TaskSummary } from './types';

export const TOKEN_KEY = 'tm.token';
export const USER_KEY = 'tm.user';

const http = axios.create({ baseURL: '/api' });

http.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Token expirado ou inválido: limpa a sessão e volta para o login.
http.interceptors.response.use(
  (r) => r,
  (error: AxiosError) => {
    const isAuthCall = error.config?.url?.startsWith('/auth/');
    if (error.response?.status === 401 && !isAuthCall) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      window.location.assign('/login');
    }
    return Promise.reject(error);
  },
);

/** Extrai a mensagem do ProblemDetails retornado pela API. */
export function getErrorMessage(error: unknown): string {
  const data = (error as AxiosError<{ title?: string; errors?: Record<string, string[]> }>)?.response?.data;
  if (data?.errors) return Object.values(data.errors).flat().join(' ');
  if (data?.title) return data.title;
  return 'Não foi possível concluir a operação. Tente novamente.';
}

export const authApi = {
  login: (email: string, password: string) =>
    http.post<AuthResponse>('/auth/login', { email, password }).then((r) => r.data),
  register: (fullName: string, email: string, password: string) =>
    http.post<AuthResponse>('/auth/register', { fullName, email, password }).then((r) => r.data),
};

export const tasksApi = {
  list: (filter: TaskFilter) =>
    http.get<PagedResult<Task>>('/tasks', { params: filter }).then((r) => r.data),
  summary: () => http.get<TaskSummary>('/tasks/summary').then((r) => r.data),
  create: (input: TaskInput) => http.post<Task>('/tasks', input).then((r) => r.data),
  update: (id: string, input: TaskInput) => http.put<Task>(`/tasks/${id}`, input).then((r) => r.data),
  updateStatus: (id: string, status: TaskStatus) =>
    http.patch<Task>(`/tasks/${id}/status`, { status }).then((r) => r.data),
  remove: (id: string) => http.delete(`/tasks/${id}`).then(() => undefined),
};
