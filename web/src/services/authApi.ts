import { api } from "./api";
import type { AdminUser, LoginRequest, LoginResponse } from "../types/auth";

export const authApi = {
  login:  (body: LoginRequest) => api.post<LoginResponse>("/api/auth/login", body),
  logout: ()                   => api.post<void>("/api/auth/logout", {}),
  me:     ()                   => api.get<AdminUser>("/api/auth/me"),
};
