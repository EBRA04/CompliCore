import { useEffect, useState } from 'react';
import { api } from '../api';

function formatValue(v) {
  if (v === null || v === undefined || v === '') return '∅';
  return String(v);
}

function Changes({ json, action }) {
  let changes = {};
  try { changes = JSON.parse(json || '{}'); } catch { /* ignore malformed */ }
  const entries = Object.entries(changes).filter(([k]) => k !== 'Id' && k !== 'TenantId');
  if (entries.length === 0) return <span className="muted-inline">—</span>;

  return (
    <div className="changes">
      {entries.map(([field, c]) => (
        <div key={field}>
          <span className="field">{field}</span>{' '}
          {action === 'Updated' && <><span className="old">{formatValue(c.old)}</span> → </>}
          {action === 'Deleted' ? <span className="old">{formatValue(c.old)}</span> : <span>{formatValue(c.new)}</span>}
        </div>
      ))}
    </div>
  );
}

export default function AuditLog() {
  const [data, setData] = useState({ items: [], totalCount: 0, pageSize: 20 });
  const [entity, setEntity] = useState('');
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');

  useEffect(() => {
    const q = new URLSearchParams({ page, pageSize: 20 });
    if (entity) q.set('entityName', entity);
    api.get(`/audit-logs?${q}`).then(setData).catch((e) => setError(e.message));
  }, [entity, page]);

  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  return (
    <div className="container">
      <h1>Audit log</h1>
      <p className="muted">Every create, update and delete in this workspace. Append-only.</p>

      {error && <div className="error">{error}</div>}

      <div className="filters">
        <select value={entity} onChange={(e) => { setEntity(e.target.value); setPage(1); }}>
          <option value="">All records</option>
          <option value="Employee">Employees</option>
          <option value="ComplianceItem">Compliance items</option>
          <option value="User">Users</option>
        </select>
      </div>

      <div className="card">
        {data.items.length === 0 ? (
          <p className="muted flush">No entries.</p>
        ) : (
          <table>
            <thead>
              <tr><th>When</th><th>Who</th><th>Record</th><th>Action</th><th>Changes</th></tr>
            </thead>
            <tbody>
              {data.items.map((a) => (
                <tr key={a.id}>
                  <td className="nowrap">{new Date(a.timestamp).toLocaleString()}</td>
                  <td>{a.userEmail || <span className="muted-inline">system</span>}</td>
                  <td>{a.entityName}</td>
                  <td><span className={`badge action-${a.action}`}>{a.action}</span></td>
                  <td><Changes json={a.changesJson} action={a.action} /></td>
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
