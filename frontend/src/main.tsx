import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App as AntApp, ConfigProvider } from 'antd';
import ptBR from 'antd/locale/pt_BR';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import App from './App';
import { AuthProvider } from './AuthContext';
import './styles.css';

dayjs.locale('pt-br');

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ConfigProvider
      locale={ptBR}
      theme={{
        token: {
          colorPrimary: '#1d4e89',
          colorInfo: '#1d4e89',
          colorBgLayout: '#f2f4f7',
          colorTextBase: '#1c2430',
          borderRadius: 4,
          fontFamily: "'IBM Plex Sans', system-ui, -apple-system, 'Segoe UI', sans-serif",
        },
        components: { Table: { headerBg: '#e9edf3', headerColor: '#1c2430' } },
      }}
    >
      <AntApp>
        <BrowserRouter>
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  </React.StrictMode>,
);
