import { createContext, useContext, useEffect, useState } from "react";
import { authApi } from "../lib/api";
import { clearSession, loadSession, saveSession } from "../lib/session";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => loadSession());
  const [user, setUser] = useState(null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      const currentSession = loadSession();
      if (!currentSession?.accessToken) {
        if (!cancelled) {
          setUser(null);
          setSession(null);
          setIsBootstrapping(false);
        }
        return;
      }

      try {
        const profile = await authApi.me();
        if (!cancelled) {
          setUser(profile);
          setSession(currentSession);
        }
      } catch {
        if (!cancelled) {
          clearSession();
          setSession(null);
          setUser(null);
        }
      } finally {
        if (!cancelled) {
          setIsBootstrapping(false);
        }
      }
    }

    bootstrap();

    return () => {
      cancelled = true;
    };
  }, []);

  async function signIn(email, password) {
    const auth = await authApi.login({ email, password });
    const nextSession = {
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
      role: auth.role,
      userId: auth.userId,
      email: auth.email,
      fullName: auth.fullName
    };

    saveSession(nextSession);
    setSession(nextSession);

    const profile = await authApi.me();
    setUser(profile);
    return profile;
  }

  async function signUp(input) {
    await authApi.register(input);
    return signIn(input.email, input.password);
  }

  async function signOut() {
    const current = loadSession();
    try {
      if (current?.refreshToken) {
        await authApi.logout(current.refreshToken);
      }
    } finally {
      clearSession();
      setSession(null);
      setUser(null);
    }
  }

  async function refreshUser() {
    const profile = await authApi.me();
    setUser(profile);
    return profile;
  }

  const value = {
    session,
    user,
    isBootstrapping,
    isAuthenticated: Boolean(session?.accessToken),
    role: session?.role ?? user?.role ?? null,
    signIn,
    signUp,
    signOut,
    refreshUser
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return value;
}

