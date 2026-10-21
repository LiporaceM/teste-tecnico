import { useState } from 'react';
import { Alert, Button, Form, Input, Tabs } from 'antd';
import { useAuth } from '../AuthContext';
import { getErrorMessage } from '../api';
import { AUTH_PROVIDER } from '../firebase';

interface Values { fullName?: string; email: string; password: string }

export default function LoginPage() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onFinish = async (v: Values) => {
    setLoading(true);
    setError(null);
    try {
      if (mode === 'login') await login(v.email, v.password);
      else await register(v.fullName!, v.email, v.password);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login">
      <section className="login__brand">
        <h1>Organize o trabalho da sua equipe</h1>
        <p>Cadastre tarefas, acompanhe o andamento de cada uma e saiba o que vence primeiro.</p>
      </section>

      <section className="login__panel">
        <div className="login__form">
          <Tabs
            activeKey={mode}
            onChange={(k) => { setMode(k as 'login' | 'register'); setError(null); }}
            items={[{ key: 'login', label: 'Entrar' }, { key: 'register', label: 'Criar conta' }]}
          />

          {error && <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />}

          <Form layout="vertical" onFinish={onFinish} requiredMark={false} key={mode}>
            {mode === 'register' && (
              <Form.Item name="fullName" label="Nome completo" rules={[{ required: true, message: 'Informe seu nome.' }]}>
                <Input autoComplete="name" />
              </Form.Item>
            )}
            <Form.Item name="email" label="E-mail" rules={[{ required: true, type: 'email', message: 'Informe um e-mail válido.' }]}>
              <Input autoComplete="email" />
            </Form.Item>
            <Form.Item
              name="password"
              label="Senha"
              rules={[{ required: true, min: 6, message: 'A senha deve ter ao menos 6 caracteres.' }]}
            >
              <Input.Password autoComplete={mode === 'login' ? 'current-password' : 'new-password'} />
            </Form.Item>
            <Button type="primary" htmlType="submit" block loading={loading}>
              {mode === 'login' ? 'Entrar' : 'Criar conta'}
            </Button>
          </Form>

          {mode === 'login' && AUTH_PROVIDER === 'local' && (
            <div className="login__hint">
              Contas de demonstração:<br />
              <b>admin@empresa.com</b> / Admin@123 (vê todas as tarefas)<br />
              <b>usuario@empresa.com</b> / User@123
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
