import axios, { AxiosError } from 'axios';
import type { AuthResponse, PagedResult, Task, TaskFilter, TaskInput, TaskStatus, TaskSummary, UserInfo } from './types';

export const TOKEN_KEY = 'tm.token';
export const USER_KEY = 'tm.user';

const http = axios.create({ baseURL: '/api' });

// Fonte do token: no modo local vem do localStorage; no modo Firebase, do SDK (que renova o token sozinho).
let tokenProvider: () => Promise<string | null> = async () => localStorage.getItem(TOKEN_KEY);
export const setTokenProvider = (fn: () => Promise<string | null>) => { tokenProvider = fn; };

let unauthorizedHandler: (() => void) | null = null;
export const setUnauthorizedHandler = (fn: (() => void) | null) => { unauthorizedHandler = fn; };

http.interceptors.request.use(async (config) => {
  const token = await tokenProvider();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Token expirado ou inválido: encerra a sessão (o roteador volta para o login).
http.interceptors.response.use(
  (r) => r,
  (error: AxiosError) => {
    const url = error.config?.url ?? '';
    const isCredentialCall = url.startsWith('/auth/login') || url.startsWith('/auth/register');
    if (error.response?.status === 401 && !isCredentialCall) unauthorizedHandler?.();
    return Promise.reject(error);
  },
);

const FIREBASE_MESSAGES: Record<string, string> = {
  'auth/invalid-credential': 'E-mail ou senha inválidos.',
  'auth/wrong-password': 'E-mail ou senha inválidos.',
  'auth/user-not-found': 'E-mail ou senha inválidos.',
  'auth/invalid-email': 'E-mail inválido.',
  'auth/email-already-in-use': 'Este e-mail já está cadastrado.',
  'auth/weak-password': 'A senha deve ter ao menos 6 caracteres.',
  'auth/too-many-requests': 'Muitas tentativas. Aguarde um momento e tente novamente.',
  'auth/network-request-failed': 'Falha de conexão. Verifique sua internet.',
};

/** Extrai a mensagem do ProblemDetails da API ou de um erro do Firebase Auth. */
export function getErrorMessage(error: unknown): string {
  const code = (error as { code?: unknown })?.code;
  if (typeof code === 'string' && code.startsWith('auth/'))
    return FIREBASE_MESSAGES[code] ?? 'Falha na autenticação. Tente novamente.';

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
  me: () => http.get<UserInfo>('/auth/me').then((r) => r.data),
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
