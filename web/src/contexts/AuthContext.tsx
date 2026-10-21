import {
  createContext, useContext, useState, useEffect,
  useCallback, type ReactNode,
} from "react";
import type { AdminUser } from "../types/auth";
import { authApi } from "../services/authApi";
import { ApiError } from "../services/api";

interface AuthCtx {
  user: AdminUser | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const Ctx = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user,    setUser]    = useState<AdminUser | null>(null);
  const [loading, setLoading] = useState(true);

  // Kiểm tra phiên đăng nhập khi load trang
  useEffect(() => {
    authApi.me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    const res = await authApi.login({ username, password });
    setUser(res.user);
  }, []);

  const logout = useCallback(async () => {
    try { await authApi.logout(); } catch (e) {
      if (!(e instanceof ApiError)) throw e;
    }
    setUser(null);
  }, []);

  return (
    <Ctx.Provider value={{ user, loading, login, logout }}>
      {children}
    </Ctx.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
