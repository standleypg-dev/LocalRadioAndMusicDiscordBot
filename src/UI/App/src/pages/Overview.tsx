import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import {
  loadSongStats,
  loadTopSongsToday,
  cleanTitle,
} from '../services/song-stats-service';
import { loadUsers } from '../services/user-service';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { AppError } from '../components/AppError';
import { RankList } from '../components/RankList';
import { formatDate, formatRelative } from '../utils/time';

const REFETCH_MS = 60_000;

export function Overview() {
  const navigate = useNavigate();

  const allSongsQuery = useQuery({
    queryKey: ['songStats', 'all'],
    queryFn: loadSongStats,
    refetchInterval: REFETCH_MS,
  });
  const todaySongsQuery = useQuery({
    queryKey: ['songStats', 'today'],
    queryFn: loadTopSongsToday,
    refetchInterval: REFETCH_MS,
  });
  const usersQuery = useQuery({
    queryKey: ['userStats'],
    queryFn: loadUsers,
    refetchInterval: REFETCH_MS,
  });

  if (
    allSongsQuery.isLoading ||
    todaySongsQuery.isLoading ||
    usersQuery.isLoading
  ) {
    return <LoadingSpinner />;
  }
  const error =
    allSongsQuery.error ?? todaySongsQuery.error ?? usersQuery.error;
  if (error) return <AppError message={String(error)} />;

  const songs = allSongsQuery.data ?? [];
  const todaySongs = todaySongsQuery.data ?? [];
  const users = usersQuery.data ?? [];

  const totalPlays = songs.reduce((sum, song) => sum + song.playCount, 0);

  const songLabel = (song: { title: string; artist?: string | null }) =>
    cleanTitle(song.title);

  const topToday = [...todaySongs]
    .sort((a, b) => b.playCount - a.playCount)
    .slice(0, 5)
    .map((song, index) => ({
      key: `${song.title}-${index}`,
      label: songLabel(song),
      sublabel: song.artist,
      value: `${song.playCount.toLocaleString()} plays`,
    }));

  const topAllTime = [...songs]
    .sort((a, b) => b.playCount - a.playCount)
    .slice(0, 5)
    .map((song, index) => ({
      key: `${song.title}-${index}`,
      label: songLabel(song),
      sublabel: song.artist,
      value: `${song.playCount.toLocaleString()} plays`,
    }));

  const topListeners = [...users]
    .sort((a, b) => b.totalPlays - a.totalPlays)
    .slice(0, 5)
    .map((user) => ({
      key: user.username,
      label: user.username,
      sublabel: user.displayName,
      value: `${user.totalPlays.toLocaleString()} plays`,
    }));

  const activity = users
    .flatMap((user) =>
      user.recentSongs.map((song) => ({
        username: user.username,
        title: cleanTitle(song.title),
        playedAt: song.playedAt,
      })),
    )
    .sort(
      (a, b) => new Date(b.playedAt).getTime() - new Date(a.playedAt).getTime(),
    )
    .slice(0, 8)
    .map((entry, index) => ({
      key: `${entry.username}-${entry.playedAt}-${index}`,
      label: entry.title,
      sublabel: entry.username,
      value: formatRelative(entry.playedAt),
      valueTitle: formatDate(entry.playedAt),
    }));

  return (
    <>
      <div className="header">
        <h1 className="title">Overview</h1>
      </div>

      <div className="stats-grid">
        <div className="glass-card stat-card">
          <h2 className="stat-value">{totalPlays.toLocaleString()}</h2>
          <p className="stat-label">Total Plays</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{songs.length.toLocaleString()}</h2>
          <p className="stat-label">Unique Songs</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{users.length.toLocaleString()}</h2>
          <p className="stat-label">Active Users</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{todaySongs.length.toLocaleString()}</h2>
          <p className="stat-label">Songs Played Today</p>
        </div>
      </div>

      <div className="overview-grid">
        <RankList
          title="Top Songs Today"
          items={topToday}
          emptyMessage="No songs played today yet."
          onViewAll={() => navigate({ to: '/songs' })}
        />
        <RankList
          title="All-Time Favorites"
          items={topAllTime}
          emptyMessage="No plays recorded yet."
          onViewAll={() => navigate({ to: '/songs' })}
        />
        <RankList
          title="Top Listeners"
          items={topListeners}
          emptyMessage="No listeners yet."
          onViewAll={() => navigate({ to: '/users' })}
        />
        <RankList
          title="Latest Activity"
          items={activity}
          emptyMessage="No recent activity."
          showRank={false}
          onViewAll={() => navigate({ to: '/users' })}
        />
      </div>
    </>
  );
}
