import { useEffect, useState } from 'react';
import { api, clean } from '../api';
import { isAdmin } from '../auth';

const EMPTY = { fullName: '', nationality: '', iqamaNumber: '', jobTitle: '' };

export default function Employees() {
  const [data, setData] = useState({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [form, setForm] = useState(EMPTY);
  const [editingId, setEditingId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState('');
  const admin = isAdmin();

  function load() {
    const q = new URLSearchParams({ page, pageSize: 20 });
    if (search) q.set('search', search);
    api.get(`/employees?${q}`).then(setData).catch((e) => setError(e.message));
  }

  useEffect(load, [page, search]);

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  function openCreate() {
    setForm(EMPTY);
    setEditingId(null);
    setShowForm(true);
    setError('');
  }

  function openEdit(emp) {
    setForm({
      fullName: emp.fullName,
      nationality: emp.nationality,
      iqamaNumber: emp.iqamaNumber || '',
      jobTitle: emp.jobTitle || '',
    });
    setEditingId(emp.id);
    setShowForm(true);
    setError('');
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    try {
      if (editingId) await api.put(`/employees/${editingId}`, clean(form));
      else await api.post('/employees', clean(form));
      setShowForm(false);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  async function remove(emp) {
    if (!confirm(`Delete ${emp.fullName} and all their compliance items?`)) return;
    try {
      await api.del(`/employees/${emp.id}`);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  return (
    <div className="container">
      <div className="row">
        <div>
          <h1>Employees</h1>
          <p className="muted flush">{data.totalCount} total</p>
        </div>
        {admin && !showForm && <button onClick={openCreate}>Add employee</button>}
      </div>

      {showForm && (
        <div className="card spaced">
          <h2>{editingId ? 'Edit employee' : 'New employee'}</h2>
          {error && <div className="error">{error}</div>}
          <form onSubmit={handleSubmit} className="grid-2">
            <div>
              <label>Full name</label>
              <input value={form.fullName} onChange={update('fullName')} required />
            </div>
            <div>
              <label>Nationality</label>
              <input value={form.nationality} onChange={update('nationality')} required />
            </div>
            <div>
              <label>Iqama number (optional, 10 digits starting with 2)</label>
              <input value={form.iqamaNumber} onChange={update('iqamaNumber')} maxLength={10} />
            </div>
            <div>
              <label>Job title (optional)</label>
              <input value={form.jobTitle} onChange={update('jobTitle')} />
            </div>
            <div className="actions">
              <button type="submit">Save</button>
              <button type="button" className="secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {!showForm && error && <div className="error">{error}</div>}

      <div className="filters">
        <input
          placeholder="Search by name or iqama"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
        />
      </div>

      <div className="card">
        {data.items.length === 0 ? (
          <p className="muted flush">No employees found.</p>
        ) : (
          <table>
            <thead>
              <tr><th>Name</th><th>Nationality</th><th>Iqama</th><th>Job title</th>{admin && <th></th>}</tr>
            </thead>
            <tbody>
              {data.items.map((emp) => (
                <tr key={emp.id}>
                  <td>{emp.fullName}</td>
                  <td>{emp.nationality}</td>
                  <td>{emp.iqamaNumber || '—'}</td>
                  <td>{emp.jobTitle || '—'}</td>
                  {admin && (
                    <td className="row-actions">
                      <button className="link" onClick={() => openEdit(emp)}>Edit</button>
                      <button className="link danger" onClick={() => remove(emp)}>Delete</button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="pager">
          <button className="secondary small" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
          <span className="muted-inline">Page {page} of {totalPages}</span>
          <button className="secondary small" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
        </div>
      )}
    </div>
  );
}
