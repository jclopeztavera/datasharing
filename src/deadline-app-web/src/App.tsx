import { MsalProvider, AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { IPublicClientApplication } from '@azure/msal-browser';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { loginRequest } from './services/authConfig';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { MattersPage } from './pages/MattersPage';
import { MatterDetailPage } from './pages/MatterDetailPage';
import { LoginPage } from './pages/LoginPage';

interface AppProps {
  msalInstance: IPublicClientApplication;
}

function AppContent() {
  return (
    <>
      <AuthenticatedTemplate>
        <Layout>
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/matters" element={<MattersPage />} />
            <Route path="/matters/:id" element={<MatterDetailPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Layout>
      </AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <LoginPage />
      </UnauthenticatedTemplate>
    </>
  );
}

export default function App({ msalInstance }: AppProps) {
  return (
    <MsalProvider instance={msalInstance}>
      <FluentProvider theme={webLightTheme}>
        <BrowserRouter>
          <AppContent />
        </BrowserRouter>
      </FluentProvider>
    </MsalProvider>
  );
}
