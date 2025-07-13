export interface UserStatsDto {
    username: string;
    totalPlays: number;
    uniqueSongs: number;
    memberSince: Date;
    lastPlayed: Date;
    displayName: string;
}
export interface SongStat {
    title: string;
    artist: string;
    playCount: number;
    lastPlayed: Date;
}

export interface RadioSource {
    id: string;
    name: string;
    sourceUrl: string;
    isActive: boolean;
    createdAt: Date;
    updatedAt: Date;
}