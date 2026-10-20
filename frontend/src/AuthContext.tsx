import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { TOKEN_KEY, USER_KEY, authApi } from './api';
import type { AuthResponse, UserInfo } from './types';

interface AuthContextValue {
  user: UserInfo | null;
  login: (email: string, password: string) => Promise<void>;
  register: (fullName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredUser(): UserInfo | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw && localStorage.getItem(TOKEN_KEY) ? (JSON.parse(raw) as UserInfo) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(readStoredUser);

  const persist = useCallback((auth: AuthResponse) => {
    localStorage.setItem(TOKEN_KEY, auth.token);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
    setUser(auth.user);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      login: async (email, password) => persist(await authApi.login(email, password)),
      register: async (fullName, email, password) => persist(await authApi.register(fullName, email, password)),
      logout: () => {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        setUser(null);
      },
    }),
    [user, persist],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return ctx;
}
