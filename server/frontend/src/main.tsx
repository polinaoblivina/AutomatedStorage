import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App as AntApp, ConfigProvider } from 'antd'
import ruRU from 'antd/locale/ru_RU'
import dayjs from 'dayjs'
import 'dayjs/locale/ru'
import { BrowserRouter } from 'react-router'
import { AuthProvider } from './auth/AuthProvider'
import { theme } from './theme'
import App from './App'
import './styles.css'

dayjs.locale('ru')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider locale={ruRU} theme={theme}>
      <AntApp>
        <BrowserRouter>
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  </StrictMode>,
)
