import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Theme } from '@radix-ui/themes'
import { AuthProvider } from './contexts/AuthContext'
import { ConfigProvider } from './contexts/ConfigContext'
import { DataSourceProvider } from './contexts/DataSourceContext'
import { DataProvider } from './contexts/DataContext'
import { MatchProvider } from './contexts/MatchContext'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Theme appearance="dark" accentColor="green" radius="medium">
      <AuthProvider>
        <DataSourceProvider>
          <ConfigProvider>
            <DataProvider>
              <MatchProvider>
                <App />
              </MatchProvider>
            </DataProvider>
          </ConfigProvider>
        </DataSourceProvider>
      </AuthProvider>
    </Theme>
  </StrictMode>,
)

