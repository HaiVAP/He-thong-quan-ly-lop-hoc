export const API = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export function token(): string | null {
  return localStorage.getItem('eduhub_token');
}

export async function api(path: string, options: RequestInit = {}) {
  const t = token();
  const r = await fetch(API + path, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(t ? { Authorization: `Bearer ${t}` } : {}),
      ...options.headers,
    },
  });

  if (!r.ok) {
    const b = await r.json().catch(() => ({ message: 'Có lỗi xảy ra' }));
    const errorMsg = b.message || (b.errors && Array.isArray(b.errors) ? b.errors.join(', ') : null) || `Lỗi HTTP ${r.status}`;
    throw new Error(errorMsg);
  }

  if (r.status === 204) return null;

  const json = await r.json();
  // Tự động unwrap trường 'data' nếu API trả về dạng ApiResponse<T>
  if (json && typeof json === 'object' && 'success' in json && 'data' in json) {
    return json.data !== null && json.data !== undefined ? json.data : json;
  }
  return json;
}
