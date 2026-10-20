export type TaskStatus = 'Pending' | 'InProgress' | 'Completed';
export type TaskPriority = 'Low' | 'Medium' | 'High';

export interface Task {
  id: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string | null;
  createdAt: string;
  updatedAt: string;
  ownerName: string;
}

export interface TaskInput {
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string | null;
}

export interface TaskFilter {
  search?: string;
  status?: TaskStatus;
  priority?: TaskPriority;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface TaskSummary {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
}

export interface UserInfo {
  id: string;
  email: string;
  fullName: string;
  role: 'Admin' | 'User';
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserInfo;
}

export const STATUS_LABEL: Record<TaskStatus, string> = {
  Pending: 'Pendente',
  InProgress: 'Em andamento',
  Completed: 'Concluída',
};

export const PRIORITY_LABEL: Record<TaskPriority, string> = {
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
};
