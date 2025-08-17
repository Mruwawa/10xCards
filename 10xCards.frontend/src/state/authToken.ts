// Simple module to store token in memory + localStorage
let token: string | null = null;

export function initToken() {
  if (!token) {
    token = localStorage.getItem('token');
  }
}

export function getToken() { return token; }
export function setToken(t: string | null) {
  token = t;
  if (t) localStorage.setItem('token', t); else localStorage.removeItem('token');
}
