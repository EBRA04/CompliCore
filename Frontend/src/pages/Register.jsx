import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { api } from '../api';
import { saveSession } from '../auth';

export default function Register() {
  const [form, setForm] = useState({ companyName: '', fullName: '', email: '', password: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      saveSession(await api.post('/auth/register', form));
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Register your company</h1>
        <p className="muted">Creates your workspace and the first Admin account</p>
        {error && <div className="error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <label>Company name</label>
          <input value={form.companyName} onChange={update('companyName')} required />
          <label>Your full name</label>
          <input value={form.fullName} onChange={update('fullName')} required />
          <label>Email</label>
          <input value={form.email} onChange={update('email')} type="email" required />
          <label>Password (min 8 characters)</label>
          <input value={form.password} onChange={update('password')} type="password" minLength={8} required />
          <button type="submit" className="full" disabled={busy}>{busy ? 'Creating…' : 'Create workspace'}</button>
        </form>
        <p className="muted footnote">Already registered? <Link to="/login">Sign in</Link></p>
      </div>
    </div>
  );
}
