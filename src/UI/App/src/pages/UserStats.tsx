import { Fragment, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Legend,
  Tooltip,
} from 'recharts';
import { loadUsers } from '../services/user-service';
import { cleanTitle } from '../services/song-stats-service';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { AppError } from '../components/AppError';
import { Pagination } from '../components/Pagination';
import { SortableTh } from '../components/SortableTh';
import { useIsMobile } from '../hooks/useIsMobile';
import { exportCsv } from '../utils/csv';

const PAGE_SIZE = 10;

// CVD-validated categorical palette in fixed slot order (see dataviz palette).
// Sub-3:1 contrast on the glass surface is relieved by the legend, tooltip,
// and the table view toggle.
const COLORS = [
  '#3987e5',
  '#199e70',
  '#c98500',
  '#008300',
  '#9085e9',
  '#e66767',
  '#d55181',
  '#d95926',
];

type SortKey = 'totalPlays' | 'uniqueSongs' | 'lastPlayed' | 'memberSince';

function formatDate(value?: Date | string | null): string {
  return value ? String(value).split('T')[0] : '';
}

function toTime(value?: Date | string | null): number {
  return value ? new Date(value).getTime() : 0;
}

export function UserStats() {
  const [viewMode, setViewMode] = useState<'table' | 'chart'>('table');
  const [expandedUser, setExpandedUser] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [sortKey, setSortKey] = useState<SortKey>('totalPlays');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [page, setPage] = useState(1);
  const isMobile = useIsMobile();

  const {
    data: userStats,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['userStats'],
    queryFn: loadUsers,
  });

  if (isLoading) return <LoadingSpinner />;
  if (error) return <AppError message={String(error)} />;
  if (!userStats) return null;

  const totalUsers = userStats.length;
  const totalPlays = userStats.reduce((sum, user) => sum + user.totalPlays, 0);
  const avgPlays = totalPlays > 0 ? Math.round(totalPlays / totalUsers) : 0;
  const topUser = userStats.reduce(
    (max, user) => (user.totalPlays > max.totalPlays ? user : max),
    userStats[0],
  );

  const query = search.trim().toLowerCase();
  const filtered = query
    ? userStats.filter((user) =>
        `${user.username} ${user.displayName ?? ''}`
          .toLowerCase()
          .includes(query),
      )
    : userStats;

  const sorted = [...filtered].sort((a, b) => {
    let delta: number;
    switch (sortKey) {
      case 'totalPlays':
        delta = a.totalPlays - b.totalPlays;
        break;
      case 'uniqueSongs':
        delta = a.uniqueSongs - b.uniqueSongs;
        break;
      case 'lastPlayed':
        delta = toTime(a.lastPlayed) - toTime(b.lastPlayed);
        break;
      case 'memberSince':
        delta = toTime(a.memberSince) - toTime(b.memberSince);
        break;
    }
    return sortDir === 'asc' ? delta : -delta;
  });

  const pageCount = Math.max(1, Math.ceil(sorted.length / PAGE_SIZE));
  const safePage = Math.min(page, pageCount);
  const pageRows = sorted.slice(
    (safePage - 1) * PAGE_SIZE,
    safePage * PAGE_SIZE,
  );

  const chartData = [...filtered]
    .sort((a, b) => b.totalPlays - a.totalPlays)
    .slice(0, 8)
    .map((user) => ({
      name: user.username,
      value: user.totalPlays,
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
      'user-stats.csv',
      [
        'Username',
        'Display Name',
        'Total Plays',
        'Unique Songs',
        'Member Since',
        'Last Played',
      ],
      sorted.map((user) => [
        user.username,
        user.displayName ?? '',
        user.totalPlays,
        user.uniqueSongs,
        formatDate(user.memberSince),
        formatDate(user.lastPlayed),
      ]),
    );
  }

  return (
    <>
      <div className="header">
        <h1 className="title">User Statistics</h1>
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
          <h2 className="stat-value">{totalUsers}</h2>
          <p className="stat-label">Active Users</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{totalPlays.toLocaleString()}</h2>
          <p className="stat-label">Total User Plays</p>
        </div>
        <div className="glass-card stat-card">
          <h2 className="stat-value">{avgPlays.toLocaleString()}</h2>
          <p className="stat-label">Average Plays per User</p>
        </div>
        <div className="glass-card stat-card">
          <h3 className="stat-song-title">{topUser?.username || 'N/A'}</h3>
          <p className="stat-label">Top User</p>
        </div>
      </div>

      <div className="toolbar">
        <input
          type="search"
          className="form-input search-input"
          placeholder="Search users..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
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
                    <th>User</th>
                    <SortableTh
                      label="Total Plays"
                      active={sortKey === 'totalPlays'}
                      direction={sortDir}
                      onSort={() => handleSort('totalPlays')}
                    />
                    <SortableTh
                      label="Unique Songs"
                      active={sortKey === 'uniqueSongs'}
                      direction={sortDir}
                      onSort={() => handleSort('uniqueSongs')}
                      className="hide-sm"
                    />
                    <SortableTh
                      label="Member Since"
                      active={sortKey === 'memberSince'}
                      direction={sortDir}
                      onSort={() => handleSort('memberSince')}
                      className="hide-md"
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
                  {pageRows.map((user, index) => (
                    <Fragment key={user.username}>
                      <tr
                        onClick={() =>
                          setExpandedUser(
                            expandedUser === user.username
                              ? null
                              : user.username,
                          )
                        }
                        style={{ cursor: 'pointer' }}
                        title="Click to see recently played songs"
                      >
                        <td>{(safePage - 1) * PAGE_SIZE + index + 1}</td>
                        <td>
                          <div className="user-info">
                            <div className="user-avatar">
                              {user.username.slice(0, 2)}
                            </div>
                            <div className="user-details">
                              <div className="username">{user.username}</div>
                              <div className="discriminator">
                                {user.displayName}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td>
                          <span className="play-count">{user.totalPlays}</span>
                        </td>
                        <td className="hide-sm">
                          <span className="unique-song">
                            {user.uniqueSongs}
                          </span>
                        </td>
                        <td className="hide-md">
                          {formatDate(user.memberSince)}
                        </td>
                        <td className="hide-sm">
                          {formatDate(user.lastPlayed)}
                        </td>
                      </tr>
                      {expandedUser === user.username && (
                        <tr className="recent-songs-row">
                          <td colSpan={6}>
                            {user.recentSongs.length === 0 ? (
                              <span className="song-artist">
                                No songs played yet.
                              </span>
                            ) : (
                              <div className="recent-songs">
                                <div className="recent-songs-header">
                                  <span>Recently Played</span>
                                  <span>Plays / Last Played</span>
                                </div>
                                <div className="recent-songs-list">
                                  {user.recentSongs.map((song, songIndex) => {
                                    const title = cleanTitle(song.title);
                                    return (
                                      <div
                                        className="recent-song-item"
                                        key={songIndex}
                                      >
                                        <span className="recent-song-rank">
                                          {songIndex + 1}
                                        </span>
                                        <span
                                          className="recent-song-title"
                                          title={title}
                                        >
                                          {title}
                                        </span>
                                        <span className="play-count">
                                          {song.totalPlays}
                                        </span>
                                        <span className="recent-song-date">
                                          {song.playedAt.split('T')[0]}
                                        </span>
                                      </div>
                                    );
                                  })}
                                </div>
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  ))}
                  {pageRows.length === 0 && (
                    <tr>
                      <td colSpan={6} className="empty-state">
                        No users match your search.
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
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  innerRadius={isMobile ? 45 : 60}
                  outerRadius={isMobile ? 85 : 120}
                  dataKey="value"
                  paddingAngle={2}
                >
                  {chartData.map((_entry, index) => (
                    <Cell
                      key={`cell-${index}`}
                      fill={COLORS[index % COLORS.length]}
                      stroke="none"
                    />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{
                    background: 'rgba(0, 0, 0, 0.8)',
                    border: '1px solid rgba(255, 255, 255, 0.2)',
                    borderRadius: '0.5rem',
                    color: 'rgba(255, 255, 255, 0.9)',
                  }}
                />
                <Legend
                  layout={isMobile ? 'horizontal' : 'vertical'}
                  align={isMobile ? 'center' : 'right'}
                  verticalAlign={isMobile ? 'bottom' : 'middle'}
                  formatter={(value) => (
                    <span style={{ color: 'rgba(255, 255, 255, 0.8)' }}>
                      {value}
                    </span>
                  )}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </>
  );
}
