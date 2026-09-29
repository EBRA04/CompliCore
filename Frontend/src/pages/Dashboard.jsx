import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get('/dashboard').then(setData).catch((e) => setError(e.message));
  }, []);

  if (error) return <div className="container"><div className="error">{error}</div></div>;
  if (!data) return <div className="container muted">Loading…</div>;

  return (
    <div className="container">
      <h1>Dashboard</h1>
      <p className="muted">{data.total} compliance items tracked</p>

      <div className="stats">
        <Link to="/compliance-items?status=Valid" className="card stat">
          <div className="stat-value">{data.valid}</div>
          <div className="stat-label">Valid</div>
        </Link>
        <Link to="/compliance-items?status=Expiring" className="card stat">
          <div className="stat-value expiring">{data.expiring}</div>
          <div className="stat-label">Expiring within 60 days</div>
        </Link>
        <Link to="/compliance-items?status=Expired" className="card stat">
          <div className="stat-value expired">{data.expired}</div>
          <div className="stat-label">Expired</div>
        </Link>
      </div>

      <div className="card">
        <h2>Next to expire</h2>
        {data.nextToExpire.length === 0 ? (
          <p className="muted flush">Nothing coming up.</p>
        ) : (
          <table>
            <thead>
              <tr><th>Title</th><th>Type</th><th>Expiry</th><th>Days left</th><th>Status</th></tr>
            </thead>
            <tbody>
              {data.nextToExpire.map((item) => (
                <tr key={item.id}>
                  <td>{item.title}</td>
                  <td>{item.type}</td>
                  <td>{item.expiryDate}</td>
                  <td>{item.daysRemaining}</td>
                  <td><span className={`badge ${item.status}`}>{item.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
