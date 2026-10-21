import type { ReactNode } from "react";
import { useAuth } from "../contexts/AuthContext";

interface Props { children: ReactNode }

export default function ProtectedRoute({ children }: Props) {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div className="admin-loading">
        <span>⏳</span> Đang kiểm tra phiên đăng nhập…
      </div>
    );
  }

  if (!user) {
    // Redirect về /admin/login
    window.location.replace("/admin/login");
    return null;
  }

  return <>{children}</>;
}
