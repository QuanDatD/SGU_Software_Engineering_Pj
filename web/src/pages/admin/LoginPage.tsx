import { useEffect, useState, type FormEvent } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { ApiError } from "../../services/api";

export default function LoginPage() {
  const { login,  user, loading: checkingSession } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error,    setError]    = useState<string | null>(null);
  const [loading,  setLoading]  = useState(false);
  useEffect(() => {
  if (!checkingSession && user) {
    window.location.replace("/admin");
  }
}, [checkingSession, user]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(username, password);
      window.location.replace("/admin");
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError("Sai tên đăng nhập hoặc mật khẩu.");
      } else {
        setError("Lỗi kết nối. Vui lòng thử lại.");
      }
    } finally {
      setLoading(false);
    }
  }
  if (checkingSession || user) {
  return (
    <div className="login-page">
      <p>Đang mở trang quản trị…</p>
    </div>
  );
}
  return (
    <div className="login-page">
      <form className="login-form" onSubmit={handleSubmit}>
        <div className="login-form__brand">
          <span>🌿</span>
          <div>
            <strong>THẢO CẦM VIÊN</strong>
            <small>Admin Panel</small>
          </div>
        </div>

        <h1>Đăng nhập</h1>

        {error && <div className="login-form__error">{error}</div>}

        <label className="admin-form__field">
          <span>Tên đăng nhập</span>
          <input
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            required
          />
        </label>

        <label className="admin-form__field">
          <span>Mật khẩu</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>

        <button className="admin-btn admin-btn--primary admin-btn--full" type="submit" disabled={loading}>
          {loading ? "Đang đăng nhập…" : "Đăng nhập"}
        </button>

        <a className="login-form__back" href="/">← Về trang người dùng</a>
      </form>
    </div>
  );
}
