import {createContext} from "@lit/context";

export interface SongStat {
    title: string;
    artist: string;
    playCount: number;
    lastPlayed: Date;
}

export class SongStatsService {

    public cleanTitle(title: string): string {
        const normalized = this.removeFancyUnicode(title);
        return normalized
            .replace(/[\s|]*[([|]?\s*(official|lirik)[^)\]|]*[)\]|]?[\s|]*/gi, '')
            .replace(/[\s|]+$/, '')
            .trim();
    }

    public async loadSongStats(): Promise<SongStat[]> {
        try {
            const response = await fetch('http://localhost:5000/statistics-all');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return await response.json();
        } catch (error) {
            console.error('Error loading song stats:', error);
            return [];
        }
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

export const userServiceContext = createContext<SongStatsService>('song-stats-service');