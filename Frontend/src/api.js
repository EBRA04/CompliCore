const BASE_URL = import.meta.env.VITE_API_URL || '/api';

async function request(path, options = {}) {
  const token = localStorage.getItem('token');
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers });

  // A 401 on login/register means wrong credentials: show the message.
  // A 401 anywhere else means the session expired: log out.
  if (res.status === 401 && !path.startsWith('/auth/')) {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    // Guard: never redirect to /login from /login, or it loops forever.
    if (window.location.pathname !== '/login') {
      window.location.replace('/login');
    }
    throw new Error('Session expired. Please sign in again.');
  }

  if (!res.ok) {
    const body = await res.json().catch(() => null);
    if (!body && res.status >= 500) {
      throw new Error('Cannot reach the server. Is the API running?');
    }
    const validation = body?.errors ? Object.values(body.errors).flat().join(' ') : null;
    throw new Error(body?.detail || validation || body?.title || 'Request failed');
  }

  if (res.status === 204) return null;
  return res.json();
}

export const api = {
  get: (path) => request(path),
  post: (path, body) => request(path, { method: 'POST', body: JSON.stringify(body ?? {}) }),
  put: (path, body) => request(path, { method: 'PUT', body: JSON.stringify(body) }),
  del: (path) => request(path, { method: 'DELETE' }),
};

// Turns "" into null so optional fields are sent as missing, not empty.
export function clean(obj) {
  return Object.fromEntries(
    Object.entries(obj).map(([k, v]) => [k, typeof v === 'string' && v.trim() === '' ? null : v]),
  );
}
