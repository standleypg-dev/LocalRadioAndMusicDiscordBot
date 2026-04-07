import type { RadioSource } from '../interfaces/common.interfaces';
import { API_BASE_URL } from './api';

function authHeader(): Record<string, string> {
  return {
    Authorization: `Bearer ${localStorage.getItem('authToken')}`,
    'Content-Type': 'application/json',
  };
}

export async function loadRadioSources(): Promise<RadioSource[]> {
  const response = await fetch(`${API_BASE_URL}/radio-sources`, {
    method: 'GET',
    headers: authHeader(),
  });
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return await response.json();
}

export async function updateRadioSource(
  sourceId: string,
  name: string,
  sourceUrl: string,
  isActive: boolean,
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/radio-sources/${sourceId}`, {
    method: 'PUT',
    headers: authHeader(),
    body: JSON.stringify({
      name: name,
      newSourceUrl: sourceUrl,
      isActive: isActive,
    }),
  });
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
}

export async function deleteRadioSource(sourceId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/radio-sources/${sourceId}`, {
    method: 'DELETE',
    headers: {
      Authorization: `Bearer ${localStorage.getItem('authToken')}`,
    },
  });
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
}

export async function addRadioSource(
  name: string,
  sourceUrl: string,
): Promise<RadioSource> {
  const response = await fetch(`${API_BASE_URL}/radio-sources/add`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      name: name,
      sourceUrl: sourceUrl,
    }),
  });
  if (response.ok) {
    return await response.json();
  }
  const errorText = await response.json();
  throw new Error(JSON.stringify(errorText));
}
