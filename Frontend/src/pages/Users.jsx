import { useEffect, useState } from 'react';
import { api } from '../api';
import { getUser } from '../auth';

const EMPTY = { fullName: '', email: '', password: '', role: 'Viewer' };

export default function Users() {
  const [users, setUsers] = useState([]);
  const [form, setForm] = useState(EMPTY);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState('');
  const me = getUser();

  function load() {
    api.get('/users').then(setUsers).catch((e) => setError(e.message));
  }

  useEffect(load, []);

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    try {
      await api.post('/users', form);
      setForm(EMPTY);
      setShowForm(false);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  async function remove(u) {
    if (!confirm(`Remove ${u.fullName}?`)) return;
    try {
      await api.del(`/users/${u.id}`);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="container">
      <div className="row">
        <div>
          <h1>Users</h1>
          <p className="muted flush">People with access to this workspace</p>
        </div>
        {!showForm && <button onClick={() => { setShowForm(true); setError(''); }}>Add user</button>}
      </div>

      {showForm && (
        <div className="card spaced">
          <h2>New user</h2>
          {error && <div className="error">{error}</div>}
          <form onSubmit={handleSubmit} className="grid-2">
            <div>
              <label>Full name</label>
              <input value={form.fullName} onChange={update('fullName')} required />
            </div>
            <div>
              <label>Email</label>
              <input value={form.email} onChange={update('email')} type="email" required />
            </div>
            <div>
              <label>Temporary password (min 8)</label>
              <input value={form.password} onChange={update('password')} type="password" minLength={8} required />
            </div>
            <div>
              <label>Role</label>
              <select value={form.role} onChange={update('role')}>
                <option value="Viewer">Viewer (read-only)</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            <div className="actions">
              <button type="submit">Save</button>
              <button type="button" className="secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {!showForm && error && <div className="error">{error}</div>}

      <div className="card">
        <table>
          <thead>
            <tr><th>Name</th><th>Email</th><th>Role</th><th>Added</th><th></th></tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{u.fullName}{u.id === me?.id && <span className="muted-inline"> (you)</span>}</td>
                <td>{u.email}</td>
                <td><span className="badge neutral">{u.role}</span></td>
                <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                <td className="row-actions">
                  {u.id !== me?.id && <button className="link danger" onClick={() => remove(u)}>Remove</button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
