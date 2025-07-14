import {createContext} from "@lit/context";
import type {SongStat} from "../interfaces/common.interfaces.ts";
import {API_BASE_URL} from "./radio-source.service.ts";

export class SongStatsService {
    public async loadSongStats(): Promise<SongStat[]> {
        const response = await fetch(`${API_BASE_URL}/statistics-all`,{
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            }
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    public cleanTitle(title: string): string {
        const normalized = this.removeFancyUnicode(title);
        return normalized
            .replace(/[\s|]*[([|]?\s*(official|lirik)[^)\]|]*[)\]|]?[\s|]*/gi, '')
            .replace(/[\s|]+$/, '')
            .trim();
    }

    private removeFancyUnicode(input: string): string {
        return input.normalize('NFKD').replace(/[\u0300-\u036f]/g, '')
            .replace(/[\u{1D400}-\u{1D7FF}]/gu, (c) => {
                const baseA = 0x1D400; // 𝐀
                const baseZ = 0x1D419; // 𝐙
                const ch = c.codePointAt(0)!;

                // Uppercase A–Z
                if (ch >= baseA && ch <= baseZ) {
                    return String.fromCharCode(0x41 + (ch - baseA));
                }

                // Lowercase a–z: 𝐚 (0x1D41A) to 𝐳 (0x1D433)
                if (ch >= 0x1D41A && ch <= 0x1D433) {
                    return String.fromCharCode(0x61 + (ch - 0x1D41A));
                }

                return c;
            });
    }
}

export const songStatsServiceCtx = createContext<SongStatsService>('song-stats-service');