export function getUser() {
  return JSON.parse(localStorage.getItem('user') || 'null');
}

export function isAuthed() {
  return !!localStorage.getItem('token');
}

export function isAdmin() {
  return getUser()?.role === 'Admin';
}

export function saveSession(res) {
  localStorage.setItem('token', res.token);
  localStorage.setItem('user', JSON.stringify(res.user));
}

export function clearSession() {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
}
