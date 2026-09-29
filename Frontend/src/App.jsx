import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import Nav from './components/Nav.jsx';
import Login from './pages/Login.jsx';
import Register from './pages/Register.jsx';
import Dashboard from './pages/Dashboard.jsx';
import Employees from './pages/Employees.jsx';
import ComplianceItems from './pages/ComplianceItems.jsx';
import Notifications from './pages/Notifications.jsx';
import Users from './pages/Users.jsx';
import AuditLog from './pages/AuditLog.jsx';
import { isAuthed, isAdmin } from './auth.js';

function Protected({ children, adminOnly = false }) {
  if (!isAuthed()) return <Navigate to="/login" replace />;
  if (adminOnly && !isAdmin()) return <Navigate to="/" replace />;
  return children;
}

export default function App() {
  // Subscribing to the location makes App re-render on every navigation,
  // so the nav appears right after login and disappears after logout.
  useLocation();

  return (
    <>
      {isAuthed() && <Nav />}
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/" element={<Protected><Dashboard /></Protected>} />
        <Route path="/employees" element={<Protected><Employees /></Protected>} />
        <Route path="/compliance-items" element={<Protected><ComplianceItems /></Protected>} />
        <Route path="/notifications" element={<Protected><Notifications /></Protected>} />
        <Route path="/users" element={<Protected adminOnly><Users /></Protected>} />
        <Route path="/audit" element={<Protected adminOnly><AuditLog /></Protected>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  );
}
