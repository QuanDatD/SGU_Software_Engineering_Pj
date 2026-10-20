import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import AdminPage from "./pages/admin/AdminPage";

/* Routing đơn giản — không cần react-router
   /admin  → AdminPage
   bất kỳ  → App (trang người dùng)
*/
const isAdmin = window.location.pathname.startsWith("/admin");

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {isAdmin ? <AdminPage /> : <App />}
  </StrictMode>
);
