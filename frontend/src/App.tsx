import { Navigate, Route, Routes } from 'react-router-dom';
import type { ReactElement } from 'react';
import { Spin } from 'antd';
import { useAuth } from './AuthContext';
import LoginPage from './pages/LoginPage';
import TasksPage from './pages/TasksPage';

function RequireAuth({ children }: { children: ReactElement }) {
  const { user } = useAuth();
  return user ? children : <Navigate to="/login" replace />;
}

export default function App() {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div style={{ display: 'grid', placeItems: 'center', height: '100vh' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <Routes>
      <Route path="/login" element={user ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route path="/" element={<RequireAuth><TasksPage /></RequireAuth>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
