import type { SongStat } from '../interfaces/common.interfaces';
import { API_BASE_URL } from './api';

export async function loadSongStats(): Promise<SongStat[]> {
  const response = await fetch(`${API_BASE_URL}/statistics-all`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return await response.json();
}

export async function loadTopSongsToday(): Promise<SongStat[]> {
  const response = await fetch(`${API_BASE_URL}/statistics-today`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return await response.json();
}

export function cleanTitle(title?: string | null): string {
  if (!title) return '';
  const normalized = removeFancyUnicode(title);
  return normalized
    .replace(/[\s|]*[([|]?\s*(official|lirik)[^)\]|]*[)\]|]?[\s|]*/gi, '')
    .replace(/[\s|]+$/, '')
    .trim();
}

function removeFancyUnicode(input: string): string {
  return input
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[\u{1D400}-\u{1D7FF}]/gu, (c) => {
      const baseA = 0x1d400;
      const baseZ = 0x1d419;
      const ch = c.codePointAt(0)!;

      if (ch >= baseA && ch <= baseZ) {
        return String.fromCharCode(0x41 + (ch - baseA));
      }

      if (ch >= 0x1d41a && ch <= 0x1d433) {
        return String.fromCharCode(0x61 + (ch - 0x1d41a));
      }

      return c;
    });
}
