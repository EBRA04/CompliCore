import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api, clean } from '../api';
import { isAdmin } from '../auth';

const TYPES = [
  'Iqama', 'Passport', 'WorkPermit', 'HealthInsurance', 'EmploymentContract',
  'CommercialRegistration', 'MunicipalLicense', 'CivilDefenseCertificate', 'Other',
];
const COMPANY_TYPES = ['CommercialRegistration', 'MunicipalLicense', 'CivilDefenseCertificate'];
const EMPTY = { employeeId: '', type: 'Iqama', title: '', referenceNumber: '', issueDate: '', expiryDate: '', notes: '' };

const label = (t) => t.replace(/([a-z])([A-Z])/g, '$1 $2');

export default function ComplianceItems() {
  const [params, setParams] = useSearchParams();
  const status = params.get('status') || '';
  const type = params.get('type') || '';
  const [page, setPage] = useState(1);

  const [data, setData] = useState({ items: [], totalCount: 0, pageSize: 20 });
  const [employees, setEmployees] = useState([]);
  const [form, setForm] = useState(EMPTY);
  const [editingId, setEditingId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState('');
  const admin = isAdmin();

  function load() {
    const q = new URLSearchParams({ page, pageSize: 20 });
    if (status) q.set('status', status);
    if (type) q.set('type', type);
    api.get(`/compliance-items?${q}`).then(setData).catch((e) => setError(e.message));
  }

  useEffect(load, [status, type, page]);

  useEffect(() => {
    api.get('/employees?pageSize=100').then((res) => setEmployees(res.items)).catch(() => {});
  }, []);

  function setFilter(key, value) {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value); else next.delete(key);
    setParams(next);
    setPage(1);
  }

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });
  const employeeName = (id) => employees.find((e) => e.id === id)?.fullName || '—';
  const isCompanyType = COMPANY_TYPES.includes(form.type);

  function openCreate() {
    setForm(EMPTY);
    setEditingId(null);
    setShowForm(true);
    setError('');
  }

  function openEdit(item) {
    setForm({
      employeeId: item.employeeId || '',
      type: item.type,
      title: item.title || '',
      referenceNumber: item.referenceNumber || '',
      issueDate: item.issueDate || '',
      expiryDate: item.expiryDate,
      notes: item.notes || '',
    });
    setEditingId(item.id);
    setShowForm(true);
    setError('');
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    const body = clean({ ...form, employeeId: isCompanyType ? '' : form.employeeId });
    try {
      if (editingId) await api.put(`/compliance-items/${editingId}`, body);
      else await api.post('/compliance-items', body);
      setShowForm(false);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  async function remove(item) {
    if (!confirm(`Delete "${item.title}"?`)) return;
    try {
      await api.del(`/compliance-items/${item.id}`);
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
          <h1>Compliance items</h1>
          <p className="muted flush">{data.totalCount} matching · sorted by expiry</p>
        </div>
        {admin && !showForm && <button onClick={openCreate}>Add item</button>}
      </div>

      {showForm && (
        <div className="card spaced">
          <h2>{editingId ? 'Edit item' : 'New item'}</h2>
          {error && <div className="error">{error}</div>}
          <form onSubmit={handleSubmit} className="grid-2">
            <div>
              <label>Type</label>
              <select value={form.type} onChange={update('type')}>
                {TYPES.map((t) => <option key={t} value={t}>{label(t)}</option>)}
              </select>
            </div>
            <div>
              <label>Employee {isCompanyType ? '(company-level, not applicable)' : ''}</label>
              <select value={isCompanyType ? '' : form.employeeId} onChange={update('employeeId')} disabled={isCompanyType}>
                <option value="">— none (company-level) —</option>
                {employees.map((emp) => <option key={emp.id} value={emp.id}>{emp.fullName}</option>)}
              </select>
            </div>
            <div>
              <label>Title (optional, defaults to the type)</label>
              <input value={form.title} onChange={update('title')} />
            </div>
            <div>
              <label>Reference number (optional)</label>
              <input value={form.referenceNumber} onChange={update('referenceNumber')} />
            </div>
            <div>
              <label>Issue date (optional)</label>
              <input value={form.issueDate} onChange={update('issueDate')} type="date" />
            </div>
            <div>
              <label>Expiry date</label>
              <input value={form.expiryDate} onChange={update('expiryDate')} type="date" required />
            </div>
            <div className="span-2">
              <label>Notes (optional)</label>
              <input value={form.notes} onChange={update('notes')} />
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
        <select value={status} onChange={(e) => setFilter('status', e.target.value)}>
          <option value="">All statuses</option>
          <option value="Valid">Valid</option>
          <option value="Expiring">Expiring</option>
          <option value="Expired">Expired</option>
        </select>
        <select value={type} onChange={(e) => setFilter('type', e.target.value)}>
          <option value="">All types</option>
          {TYPES.map((t) => <option key={t} value={t}>{label(t)}</option>)}
        </select>
      </div>

      <div className="card">
        {data.items.length === 0 ? (
          <p className="muted flush">No items found.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Title</th><th>Employee</th><th>Reference</th><th>Expiry</th><th>Days left</th><th>Status</th>
                {admin && <th></th>}
              </tr>
            </thead>
            <tbody>
              {data.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.title}</td>
                  <td>{item.employeeId ? employeeName(item.employeeId) : <span className="muted-inline">Company</span>}</td>
                  <td>{item.referenceNumber || '—'}</td>
                  <td>{item.expiryDate}</td>
                  <td>{item.daysRemaining}</td>
                  <td><span className={`badge ${item.status}`}>{item.status}</span></td>
                  {admin && (
                    <td className="row-actions">
                      <button className="link" onClick={() => openEdit(item)}>Edit</button>
                      <button className="link danger" onClick={() => remove(item)}>Delete</button>
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
