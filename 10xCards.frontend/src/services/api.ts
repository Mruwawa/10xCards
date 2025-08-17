// Simple fetch wrapper with auth token injection
import { getToken } from '../state/authToken';

const base = '';// proxied via Vite

async function request(path: string, options: RequestInit & { auth?: boolean } = {}) {
  const headers: Record<string,string> = {
    'Content-Type': 'application/json',
    ...(options.headers as any)
  };
  if (options.auth !== false) {
    const token = getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
  }
  const res = await fetch(base + path, { ...options, headers });
  if (!res.ok) {
    let message = res.status + ' ' + res.statusText;
    try { const data = await res.json(); message = data.error || JSON.stringify(data); } catch {}
    throw new Error(message);
  }
  if (res.status === 204) return null;
  return res.json();
}

export const api = {
  get: (p: string) => request(p, { method: 'GET' }),
  post: (p: string, body?: any) => request(p, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
  put: (p: string, body?: any) => request(p, { method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
  del: (p: string) => request(p, { method: 'DELETE' }),
};
