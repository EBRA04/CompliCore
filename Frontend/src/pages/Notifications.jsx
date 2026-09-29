import { useEffect, useState } from 'react';
import { api } from '../api';

const thresholdLabel = (t) => (t === 0 ? 'Expired' : `${t} days`);

export default function Notifications() {
  const [items, setItems] = useState([]);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [error, setError] = useState('');

  function load() {
    api.get('/notifications').then(setItems).catch((e) => setError(e.message));
  }

  useEffect(load, []);

  async function markRead(n) {
    try {
      await api.post(`/notifications/${n.id}/read`);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  const shown = unreadOnly ? items.filter((n) => !n.readAt) : items;
  const unreadCount = items.filter((n) => !n.readAt).length;

  return (
    <div className="container">
      <div className="row">
        <div>
          <h1>Notifications</h1>
          <p className="muted flush">{unreadCount} unread · generated daily by the reminder job</p>
        </div>
        <label className="checkbox">
          <input type="checkbox" checked={unreadOnly} onChange={(e) => setUnreadOnly(e.target.checked)} />
          Unread only
        </label>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="card">
        {shown.length === 0 ? (
          <p className="muted flush">No notifications.</p>
        ) : (
          <ul className="list">
            {shown.map((n) => (
              <li key={n.id} className={n.readAt ? 'read' : ''}>
                <div>
                  <div className="list-title">{n.message}</div>
                  <div className="muted-inline">
                    {thresholdLabel(n.threshold)} · {new Date(n.createdAt).toLocaleString()}
                  </div>
                </div>
                {!n.readAt && <button className="link" onClick={() => markRead(n)}>Mark read</button>}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
