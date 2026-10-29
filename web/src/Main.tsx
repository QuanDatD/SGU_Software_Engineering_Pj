import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import AdminPage    from "./pages/admin/AdminPage";
import LoginPage   from "./pages/admin/LoginPage";
import { LanguageProvider } from "./contexts/LanguageContext";
import { AudioProvider }    from "./contexts/AudioContext";
import { AuthProvider }     from "./contexts/AuthContext";

/*
  Routing đơn giản theo pathname — không cần react-router
  /admin/login → LoginPage
  /admin/*     → AdminPage (bảo vệ bởi ProtectedRoute)
  bất kỳ       → App (trang người dùng)
*/
const path = window.location.pathname;

function Root() {
  if (path === "/admin/login") {
    return (
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    );
  }

  if (path.startsWith("/admin")) {
    return (
      <AuthProvider>
        <AdminPage />
      </AuthProvider>
    );
  }

  return (
    <LanguageProvider>
      <AudioProvider>
        <App />
      </AudioProvider>
    </LanguageProvider>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Root />
  </StrictMode>
);
