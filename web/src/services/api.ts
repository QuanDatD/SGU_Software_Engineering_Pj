/* ======================================================
   Base fetch wrapper
   - credentials: "include"  → gửi cookie session
   - CSRF token lấy từ GET /api/auth/csrf, đính vào header
     X-CSRF-TOKEN cho mọi mutating request (POST/PUT/DELETE)
   ====================================================== */

const BASE = (import.meta.env.VITE_API_BASE ?? "").replace(/\/$/, "");
export function mediaUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path;
  // Local UI artwork remains on the frontend; API/storage URLs belong to the backend.
  return path.startsWith("/api/") || path.startsWith("/storage/") ? `${BASE}${path}` : path;
}

export class ApiError extends Error {
  public status: number;
  public code: string;
  constructor(
    status: number,
    code: string,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

/* Cache CSRF token trong memory — lấy lại nếu 403 */
let csrfToken: string | null = null;

async function fetchCsrf(): Promise<string> {
  const res = await fetch(`${BASE}/api/auth/csrf`, { credentials: "include" });
  if (!res.ok) return "";
  const data = await res.json();
  return (data.token as string) ?? "";
}

async function getCsrf(): Promise<string> {
  csrfToken = await fetchCsrf();
  return csrfToken;
}

async function request<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const method = (init?.method ?? "GET").toUpperCase();
  const mutating = ["POST", "PUT", "PATCH", "DELETE"].includes(method);

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(init?.headers as Record<string, string> ?? {}),
  };

  if (mutating) {
    headers["X-CSRF-TOKEN"] = await getCsrf();
  }

  const res = await fetch(`${BASE}${path}`, {
    credentials: "include",
    cache: "no-store",
    ...init,
    headers,
  });

  /* CSRF expired → lấy lại và thử lại 1 lần */
  if (res.status === 403 && mutating) {
    csrfToken = await fetchCsrf();
    headers["X-CSRF-TOKEN"] = csrfToken;
    const retry = await fetch(`${BASE}${path}`, {
      credentials: "include",
      ...init,
      headers,
    });
    return handleResponse<T>(retry);
  }

  return handleResponse<T>(res);
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let code = "UNKNOWN";
    let message = res.statusText;
    try {
      const body = await res.json();
      code    = body.code    ?? code;
      message = body.message ?? (body.errors ? Object.values(body.errors).flat().join(" ") : body.title) ?? message;
    } catch { /* ignore */ }
    throw new ApiError(res.status, code, message);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  get:    <T>(path: string)                => request<T>(path),
  post:   <T>(path: string, body: unknown) => request<T>(path, { method: "POST",   body: JSON.stringify(body) }),
  put:    <T>(path: string, body: unknown) => request<T>(path, { method: "PUT",    body: JSON.stringify(body) }),
  patch:  <T>(path: string, body: unknown) => request<T>(path, { method: "PATCH",  body: JSON.stringify(body) }),
  delete: <T>(path: string)               => request<T>(path, { method: "DELETE" }),
};
export async function uploadForm<T>(
  path: string,
  method: "POST" | "PUT",
  body: FormData,
): Promise<T> {
  // Lấy token mới theo phiên đăng nhập hiện tại.
  const csrfResponse = await fetch(`${BASE}/api/auth/csrf`, {
    credentials: "include",
    cache: "no-store",
  });

  if (!csrfResponse.ok) {
    throw new ApiError(
      csrfResponse.status,
      "CSRF_FAILED",
      "Không lấy được mã xác thực yêu cầu.",
    );
  }

  const csrfData = await csrfResponse.json();

  if (typeof csrfData.token !== "string" || !csrfData.token) {
    throw new ApiError(
      500,
      "CSRF_INVALID",
      "Mã xác thực yêu cầu không hợp lệ.",
    );
  }

  const response = await fetch(`${BASE}${path}`, {
    method,
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": csrfData.token,
    },
    body,
  });

  // Không tự đặt Content-Type khi gửi FormData.
  // Trình duyệt sẽ thêm multipart/form-data cùng boundary.
  return handleResponse<T>(response);
}
