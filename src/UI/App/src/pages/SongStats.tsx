import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
} from 'recharts';
import {
  loadSongStats,
  loadTopSongsToday,
  cleanTitle,
} from '../services/song-stats-service';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { AppError } from '../components/AppError';
import { Pagination } from '../components/Pagination';
import { SortableTh } from '../components/SortableTh';
import { useIsMobile } from '../hooks/useIsMobile';
import { exportCsv } from '../utils/csv';

const PAGE_SIZE = 10;
// Validated single-hue mark color: 3.09:1 contrast on the dashboard surface.
const BAR_COLOR = '#86b6ef';

type SortKey = 'playCount' | 'lastPlayed';
type TimeRange = 'all' | 'today';

function truncate(text: string, max: number): string {
  return text.length > max ? `${text.slice(0, max - 3)}...` : text;
}

function formatDate(value?: Date | string | null): string {
  return value ? String(value).split('T')[0] : '';
}

export function SongStats() {
  const [viewMode, setViewMode] = useState<'table' | 'chart'>('table');
  const [timeRange, setTimeRange] = useState<TimeRange>('all');
  const [search, setSearch] = useState('');
  const [sortKey, setSortKey] = useState<SortKey>('playCount');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [page, setPage] = useState(1);
  const isMobile = useIsMobile();

  const {
    data: songs,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['songStats', timeRange],
    queryFn: () =>
      timeRange === 'today' ? loadTopSongsToday() : loadSongStats(),
  });

  if (isLoading) return <LoadingSpinner />;
  if (error) return <AppError message={String(error)} />;
  if (!songs) return null;

  const totalPlays = songs.reduce((sum, song) => sum + song.playCount, 0);
  const avgPlays = songs.length > 0 ? Math.round(totalPlays / songs.length) : 0;
  const topSong = songs.reduce(
    (max, song) => (song.playCount > (max?.playCount ?? 0) ? song : max),
    songs[0],
  );

  const query = search.trim().toLowerCase();
  const filtered = query
    ? songs.filter((song) =>
        `${cleanTitle(song.title)} ${song.artist ?? ''}`
          .toLowerCase()
          .includes(query),
      )
    : songs;

  const sorted = [...filtered].sort((a, b) => {
    const delta =
      sortKey === 'playCount'
        ? a.playCount - b.playCount
        : new Date(a.lastPlayed).getTime() - new Date(b.lastPlayed).getTime();
    return sortDir === 'asc' ? delta : -delta;
  });

  const pageCount = Math.max(1, Math.ceil(sorted.length / PAGE_SIZE));
  const safePage = Math.min(page, pageCount);
  const pageRows = sorted.slice(
    (safePage - 1) * PAGE_SIZE,
    safePage * PAGE_SIZE,
  );

  const chartData = [...filtered]
    .sort((a, b) => b.playCount - a.playCount)
    .slice(0, 10)
    .map((song) => ({
      name: truncate(
        `${cleanTitle(song.title)}${song.artist ? ` - ${song.artist}` : ''}`,
        isMobile ? 22 : 28,
      ),
      playCount: song.playCount,
    }));

  function handleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir(sortDir === 'desc' ? 'asc' : 'desc');
    } else {
      setSortKey(key);
      setSortDir('desc');
    }
    setPage(1);
  }

  function handleExport() {
    exportCsv(
      `song-stats-${timeRange}.csv`,
      ['Title', 'Artist', 'Play Count', 'Last Played'],
      sorted.map((song) => [
        cleanTitle(song.title),
        song.artist ?? '',
        song.playCount,
        formatDate(song.lastPlayed),
      ]),
    );
  }

  return (
    <>
      <div className="header">
        <h1 className="title">Song Statistics</h1>
        <div className="view-toggle">
          <button
            className={`toggle-button ${viewMode === 'table' ? 'active' : ''}`}
            onClick={() => setViewMode('table')}
          >
            Table View
          </button>
          <button
            className={`toggle-button ${viewMode === 'chart' ? 'active' : ''}`}
            onClick={() => setViewMode('chart')}
          >
            Chart View
          </button>
        </div>
      </div>

      <div className="stats-grid">
        <div className="glass-card stat-card">
          <h2 className="stat-value">{totalPlays.toLocaleString()}</h2>
          <p className="stat-label">
            {timeRange === 'today' ? 'Plays Today' : 'Total Plays'}
          </p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{songs.length}</h2>
          <p className="stat-label">Unique Songs</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{avgPlays.toLocaleString()}</h2>
          <p className="stat-label">Average Plays</p>
        </div>
        <div className="glass-card stat-card">
          <h3 className="stat-song-title">
            {cleanTitle(topSong?.title) || 'N/A'}
          </h3>
          <p className="stat-label">Most Played Song</p>
        </div>
      </div>

      <div className="toolbar">
        <input
          type="search"
          className="form-input search-input"
          placeholder="Search songs..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
        <div className="view-toggle">
          <button
            className={`toggle-button ${timeRange === 'all' ? 'active' : ''}`}
            onClick={() => {
              setTimeRange('all');
              setPage(1);
            }}
          >
            All Time
          </button>
          <button
            className={`toggle-button ${timeRange === 'today' ? 'active' : ''}`}
            onClick={() => {
              setTimeRange('today');
              setPage(1);
            }}
          >
            Today
          </button>
        </div>
        <button className="glass-button export-button" onClick={handleExport}>
          Export CSV
        </button>
      </div>

      <div className="content-card glass-card">
        {viewMode === 'table' ? (
          <>
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Rank</th>
                    <th>Song</th>
                    <SortableTh
                      label="Plays"
                      active={sortKey === 'playCount'}
                      direction={sortDir}
                      onSort={() => handleSort('playCount')}
                    />
                    <SortableTh
                      label="Last Played"
                      active={sortKey === 'lastPlayed'}
                      direction={sortDir}
                      onSort={() => handleSort('lastPlayed')}
                      className="hide-sm"
                    />
                  </tr>
                </thead>
                <tbody>
                  {pageRows.map((song, index) => (
                    <tr key={`${song.title}-${index}`}>
                      <td>{(safePage - 1) * PAGE_SIZE + index + 1}</td>
                      <td>
                        <div className="song-info">
                          <div className="song-title">
                            {cleanTitle(song.title)}
                          </div>
                          <div className="song-artist">{song.artist}</div>
                        </div>
                      </td>
                      <td>
                        <span className="play-count">
                          {song.playCount.toLocaleString()}
                        </span>
                      </td>
                      <td className="hide-sm">{formatDate(song.lastPlayed)}</td>
                    </tr>
                  ))}
                  {pageRows.length === 0 && (
                    <tr>
                      <td colSpan={4} className="empty-state">
                        {timeRange === 'today'
                          ? 'No songs played today.'
                          : 'No songs match your search.'}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <Pagination
              page={safePage}
              pageCount={pageCount}
              totalItems={sorted.length}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          </>
        ) : (
          <div className="chart-container">
            <ResponsiveContainer width="100%" height="100%">
              {isMobile ? (
                <BarChart
                  data={chartData}
                  layout="vertical"
                  margin={{ top: 8, right: 16, bottom: 8, left: 0 }}
                >
                  <CartesianGrid
                    strokeDasharray="3 3"
                    stroke="rgba(255, 255, 255, 0.1)"
                    horizontal={false}
                  />
                  <XAxis
                    type="number"
                    tick={{ fill: 'rgba(255, 255, 255, 0.7)', fontSize: 11 }}
                  />
                  <YAxis
                    type="category"
                    dataKey="name"
                    width={120}
                    tick={{ fill: 'rgba(255, 255, 255, 0.7)', fontSize: 11 }}
                  />
                  <Tooltip
                    cursor={{ fill: 'rgba(255, 255, 255, 0.05)' }}
                    contentStyle={{
                      background: 'rgba(0, 0, 0, 0.8)',
                      border: '1px solid rgba(255, 255, 255, 0.2)',
                      borderRadius: '0.5rem',
                      color: 'rgba(255, 255, 255, 0.9)',
                    }}
                  />
                  <Bar
                    dataKey="playCount"
                    name="Plays"
                    fill={BAR_COLOR}
                    radius={[0, 4, 4, 0]}
                    maxBarSize={20}
                  />
                </BarChart>
              ) : (
                <BarChart data={chartData}>
                  <CartesianGrid
                    strokeDasharray="3 3"
                    stroke="rgba(255, 255, 255, 0.1)"
                    vertical={false}
                  />
                  <XAxis
                    dataKey="name"
                    tick={{ fill: 'rgba(255, 255, 255, 0.7)', fontSize: 12 }}
                    angle={-45}
                    textAnchor="end"
                    height={110}
                    interval={0}
                  />
                  <YAxis tick={{ fill: 'rgba(255, 255, 255, 0.7)' }} />
                  <Tooltip
                    cursor={{ fill: 'rgba(255, 255, 255, 0.05)' }}
                    contentStyle={{
                      background: 'rgba(0, 0, 0, 0.8)',
                      border: '1px solid rgba(255, 255, 255, 0.2)',
                      borderRadius: '0.5rem',
                      color: 'rgba(255, 255, 255, 0.9)',
                    }}
                  />
                  <Bar
                    dataKey="playCount"
                    name="Plays"
                    fill={BAR_COLOR}
                    radius={[4, 4, 0, 0]}
                    maxBarSize={48}
                  />
                </BarChart>
              )}
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </>
  );
}
