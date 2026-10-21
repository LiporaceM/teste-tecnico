import { initializeApp } from 'firebase/app';
import { getAuth, type Auth } from 'firebase/auth';

export type AuthProvider = 'local' | 'firebase';

/** Definido em build por VITE_AUTH_PROVIDER ("local" é o padrão). */
export const AUTH_PROVIDER: AuthProvider = import.meta.env.VITE_AUTH_PROVIDER === 'firebase' ? 'firebase' : 'local';

export const firebaseAuth: Auth | null =
  AUTH_PROVIDER === 'firebase'
    ? getAuth(
        initializeApp({
          apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
          authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
          projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
          appId: import.meta.env.VITE_FIREBASE_APP_ID,
        }),
      )
    : null;
