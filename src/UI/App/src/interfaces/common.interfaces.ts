export interface RecentSong {
  title: string;
  totalPlays: number;
  playedAt: string;
}

export interface UserStatsDto {
  username: string;
  totalPlays: number;
  uniqueSongs: number;
  memberSince: Date;
  lastPlayed?: Date | null;
  displayName?: string | null;
  recentSongs: RecentSong[];
}

export interface SongStat {
  title: string;
  artist?: string | null;
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
