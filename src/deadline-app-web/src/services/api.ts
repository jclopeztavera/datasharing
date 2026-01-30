import { IPublicClientApplication } from '@azure/msal-browser';
import { apiRequest } from './authConfig';
import type {
  Matter,
  CreateMatterRequest,
  Deadline,
  OverrideDeadlineRequest,
  TriggerEvent,
  CreateTriggerEventRequest,
  CourtRule,
  DashboardData,
} from '../types';

const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api';

let msalInstance: IPublicClientApplication | null = null;

export function setMsalInstance(instance: IPublicClientApplication) {
  msalInstance = instance;
}

async function getToken(): Promise<string> {
  if (!msalInstance) throw new Error('MSAL not initialized');

  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) throw new Error('No authenticated user');

  const response = await msalInstance.acquireTokenSilent({
    ...apiRequest,
    account: accounts[0],
  });

  return response.accessToken;
}

async function fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getToken();
  const response = await fetch(`${API_BASE}${url}`, {
    ...options,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || `HTTP ${response.status}`);
  }

  return response;
}

// Dashboard
export async function getDashboard(attorneyId?: string): Promise<DashboardData> {
  const params = attorneyId ? `?attorneyId=${attorneyId}` : '';
  const response = await fetchWithAuth(`/dashboard${params}`);
  return response.json();
}

// Matters
export async function getMatters(): Promise<Matter[]> {
  const response = await fetchWithAuth('/matters');
  return response.json();
}

export async function getMatter(id: string): Promise<Matter> {
  const response = await fetchWithAuth(`/matters/${id}`);
  return response.json();
}

export async function createMatter(request: CreateMatterRequest): Promise<Matter> {
  const response = await fetchWithAuth('/matters', {
    method: 'POST',
    body: JSON.stringify(request),
  });
  return response.json();
}

export async function updateMatter(id: string, request: Partial<CreateMatterRequest>): Promise<Matter> {
  const response = await fetchWithAuth(`/matters/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
  return response.json();
}

export async function closeMatter(id: string): Promise<void> {
  await fetchWithAuth(`/matters/${id}/close`, { method: 'POST' });
}

// Deadlines
export async function getDeadlinesByMatter(matterId: string): Promise<Deadline[]> {
  const response = await fetchWithAuth(`/deadlines/matter/${matterId}`);
  return response.json();
}

export async function overrideDeadline(id: string, request: OverrideDeadlineRequest): Promise<Deadline> {
  const response = await fetchWithAuth(`/deadlines/${id}/override`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
  return response.json();
}

export async function completeDeadline(id: string): Promise<Deadline> {
  const response = await fetchWithAuth(`/deadlines/${id}/complete`, {
    method: 'POST',
  });
  return response.json();
}

// Trigger Events
export async function getTriggerEvents(matterId: string): Promise<TriggerEvent[]> {
  const response = await fetchWithAuth(`/triggerevents/matter/${matterId}`);
  return response.json();
}

export async function createTriggerEvent(request: CreateTriggerEventRequest): Promise<TriggerEvent> {
  const response = await fetchWithAuth('/triggerevents', {
    method: 'POST',
    body: JSON.stringify(request),
  });
  return response.json();
}

// Court Rules
export async function getCourtRules(state: string, caseType: string): Promise<CourtRule[]> {
  const response = await fetchWithAuth(`/courtrules?state=${state}&caseType=${caseType}`);
  return response.json();
}
