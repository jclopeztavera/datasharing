import React from 'react';
import ReactDOM from 'react-dom/client';
import { PublicClientApplication, EventType } from '@azure/msal-browser';
import { msalConfig } from './services/authConfig';
import { setMsalInstance } from './services/api';
import App from './App';

const msalInstance = new PublicClientApplication(msalConfig);

msalInstance.initialize().then(() => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }

  msalInstance.addEventCallback((event) => {
    if (
      event.eventType === EventType.LOGIN_SUCCESS &&
      event.payload &&
      'account' in event.payload &&
      event.payload.account
    ) {
      msalInstance.setActiveAccount(event.payload.account);
    }
  });

  setMsalInstance(msalInstance);

  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <App msalInstance={msalInstance} />
    </React.StrictMode>
  );
});
