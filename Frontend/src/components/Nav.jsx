import { NavLink, useNavigate } from 'react-router-dom';
import { getUser, isAdmin, clearSession } from '../auth.js';

const link = ({ isActive }) => (isActive ? 'active' : '');

export default function Nav() {
  const navigate = useNavigate();
  const user = getUser() || {};

  function logout() {
    clearSession();
    navigate('/login');
  }

  return (
    <nav>
      <span className="brand">CompliCore</span>
      <NavLink to="/" end className={link}>Dashboard</NavLink>
      <NavLink to="/employees" className={link}>Employees</NavLink>
      <NavLink to="/compliance-items" className={link}>Compliance items</NavLink>
      <NavLink to="/notifications" className={link}>Notifications</NavLink>
      {isAdmin() && <NavLink to="/users" className={link}>Users</NavLink>}
      {isAdmin() && <NavLink to="/audit" className={link}>Audit log</NavLink>}
      <div className="nav-right">
        <span className="muted-inline">{user.companyName} · {user.role}</span>
        <button className="secondary small" onClick={logout}>Log out</button>
      </div>
    </nav>
  );
}
