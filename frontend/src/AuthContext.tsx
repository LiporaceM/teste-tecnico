import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  createUserWithEmailAndPassword, onAuthStateChanged, signInWithEmailAndPassword, signOut, updateProfile,
} from 'firebase/auth';
import { TOKEN_KEY, USER_KEY, authApi, setTokenProvider, setUnauthorizedHandler } from './api';
import { firebaseAuth } from './firebase';
import type { AuthResponse, UserInfo } from './types';

interface AuthContextValue {
  user: UserInfo | null;
  /** true enquanto a sessão do Firebase está sendo restaurada. */
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (fullName: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredUser(): UserInfo | null {
  if (firebaseAuth) return null;
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw && localStorage.getItem(TOKEN_KEY) ? (JSON.parse(raw) as UserInfo) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(readStoredUser);
  const [loading, setLoading] = useState(firebaseAuth !== null);
  // Evita buscar /auth/me duas vezes (pelo fluxo de login/cadastro e pelo evento do Firebase).
  const manualAuth = useRef(false);

  const persist = useCallback((auth: AuthResponse) => {
    localStorage.setItem(TOKEN_KEY, auth.token);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
    setUser(auth.user);
  }, []);

  const logout = useCallback(async () => {
    if (firebaseAuth) await signOut(firebaseAuth).catch(() => undefined);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => { void logout(); });
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  // Modo Firebase: restaura a sessão e mantém o usuário sincronizado.
  useEffect(() => {
    if (!firebaseAuth) return;
    setTokenProvider(async () => (firebaseAuth!.currentUser ? firebaseAuth!.currentUser.getIdToken() : null));

    return onAuthStateChanged(firebaseAuth, async (fbUser) => {
      if (manualAuth.current) return;
      if (!fbUser) {
        setUser(null);
        setLoading(false);
        return;
      }
      try {
        setUser(await authApi.me());
      } catch {
        setUser(null);
      } finally {
        setLoading(false);
      }
    });
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      login: async (email, password) => {
        if (!firebaseAuth) return persist(await authApi.login(email, password));
        manualAuth.current = true;
        try {
          await signInWithEmailAndPassword(firebaseAuth, email, password);
          setUser(await authApi.me());
        } catch (e) {
          await signOut(firebaseAuth).catch(() => undefined);
          throw e;
        } finally {
          manualAuth.current = false;
        }
      },
      register: async (fullName, email, password) => {
        if (!firebaseAuth) return persist(await authApi.register(fullName, email, password));
        manualAuth.current = true;
        try {
          const cred = await createUserWithEmailAndPassword(firebaseAuth, email, password);
          await updateProfile(cred.user, { displayName: fullName });
          await cred.user.getIdToken(true); // renova o token para incluir o nome
          setUser(await authApi.me());
        } catch (e) {
          await signOut(firebaseAuth).catch(() => undefined);
          throw e;
        } finally {
          manualAuth.current = false;
        }
      },
      logout,
    }),
    [user, loading, persist, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return ctx;
}
