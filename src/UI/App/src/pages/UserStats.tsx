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

const COLORS = [
  '#ff6b6b',
  '#4ecdc4',
  '#45b7d1',
  '#f9ca24',
  '#f0932b',
  '#eb4d4b',
  '#6ab04c',
  '#9c88ff',
];

export function UserStats() {
  const [viewMode, setViewMode] = useState<'table' | 'chart'>('table');
  const [expandedUser, setExpandedUser] = useState<string | null>(null);

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

  const chartData = userStats.slice(0, 8).map((user) => ({
    name: user.username,
    value: user.totalPlays,
  }));

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

      <div
        className="stats-grid"
        style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))' }}
      >
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
          <h2 className="stat-value">{topUser?.username || 'N/A'}</h2>
          <p className="stat-label">Top User</p>
        </div>
      </div>

      <div className="content-card">
        {viewMode === 'table' ? (
          <table className="table">
            <thead>
              <tr>
                <th>Rank</th>
                <th>User</th>
                <th>Total Plays</th>
                <th>Unique Songs</th>
                <th>Member Since</th>
                <th>Last Played</th>
              </tr>
            </thead>
            <tbody>
              {userStats.slice(0, 10).map((user, index) => (
                <Fragment key={user.username}>
                  <tr
                    onClick={() =>
                      setExpandedUser(
                        expandedUser === user.username ? null : user.username,
                      )
                    }
                    style={{ cursor: 'pointer' }}
                    title="Click to see recently played songs"
                  >
                    <td>{index + 1}</td>
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
                    <td>
                      <span className="unique-song">{user.uniqueSongs}</span>
                    </td>
                    <td>{String(user.memberSince).split('T')[0]}</td>
                    <td>
                      {user.lastPlayed
                        ? String(user.lastPlayed).split('T')[0]
                        : ''}
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
            </tbody>
          </table>
        ) : (
          <div className="chart-container">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={120}
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
                  layout="vertical"
                  align="right"
                  verticalAlign="middle"
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
