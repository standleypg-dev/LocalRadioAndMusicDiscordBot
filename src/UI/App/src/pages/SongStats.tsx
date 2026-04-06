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
import { loadSongStats, cleanTitle } from '../services/song-stats-service';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { AppError } from '../components/AppError';

export function SongStats() {
  const [viewMode, setViewMode] = useState<'table' | 'chart'>('table');

  const {
    data: songs,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['songStats'],
    queryFn: loadSongStats,
  });

  if (isLoading) return <LoadingSpinner />;
  if (error) return <AppError message={String(error)} />;
  if (!songs) return null;

  const totalPlays = songs.reduce((sum, song) => sum + song.playCount, 0);
  const avgPlays = totalPlays > 0 ? Math.round(totalPlays / songs.length) : 0;
  const topSong = songs[0];

  const chartData = songs.slice(0, 10).map((song) => ({
    name: `${cleanTitle(song.title)} ${song.artist ? `- ${song.artist}` : ''}`,
    playCount: song.playCount,
  }));

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

      <div
        className="stats-grid"
        style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))' }}
      >
        <div className="glass-card stat-card">
          <h2 className="stat-value">{totalPlays.toLocaleString()}</h2>
          <p className="stat-label">Total Plays</p>
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

      <div className="content-card">
        {viewMode === 'table' ? (
          <table className="table">
            <thead>
              <tr>
                <th>Rank</th>
                <th>Song</th>
                <th>Plays</th>
              </tr>
            </thead>
            <tbody>
              {songs.slice(0, 10).map((song, index) => (
                <tr key={index}>
                  <td>{index + 1}</td>
                  <td>
                    <div className="song-info">
                      <div className="song-title">{cleanTitle(song.title)}</div>
                      <div className="song-artist">{song.artist}</div>
                    </div>
                  </td>
                  <td>
                    <span className="play-count">
                      {song.playCount.toLocaleString()}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="chart-container">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData}>
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="rgba(255, 255, 255, 0.1)"
                />
                <XAxis
                  dataKey="name"
                  tick={{ fill: 'rgba(255, 255, 255, 0.7)', fontSize: 12 }}
                  angle={-45}
                  textAnchor="end"
                  height={100}
                />
                <YAxis tick={{ fill: 'rgba(255, 255, 255, 0.7)' }} />
                <Tooltip
                  contentStyle={{
                    background: 'rgba(0, 0, 0, 0.8)',
                    border: '1px solid rgba(255, 255, 255, 0.2)',
                    borderRadius: '0.5rem',
                    color: 'rgba(255, 255, 255, 0.9)',
                  }}
                />
                <Bar
                  dataKey="playCount"
                  fill="rgba(78, 205, 196, 0.8)"
                  stroke="rgba(78, 205, 196, 1)"
                  strokeWidth={2}
                  radius={[8, 8, 0, 0]}
                />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </>
  );
}
