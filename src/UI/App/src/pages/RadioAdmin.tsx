import { useEffect, useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useAuth } from '../hooks/useAuth';
import type { RadioSource } from '../interfaces/common.interfaces';
import {
  loadRadioSources,
  updateRadioSource,
  deleteRadioSource,
  addRadioSource,
} from '../services/radio-source-service';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { AppError } from '../components/AppError';

export function RadioAdmin() {
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingStation, setEditingStation] = useState<RadioSource | null>(
    null,
  );
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { logout } = useAuth();

  const {
    data: radioStations,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['radioSources'],
    queryFn: loadRadioSources,
  });

  // The route guard only checks that a token exists; an expired one still
  // reaches this page and fails with 401, so send the user back to login.
  const unauthorized = error !== null && String(error).includes('401');
  useEffect(() => {
    if (unauthorized) {
      logout();
      navigate({ to: '/login' });
    }
  }, [unauthorized, logout, navigate]);

  const deleteMutation = useMutation({
    mutationFn: deleteRadioSource,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['radioSources'] });
      toast.success('Radio station deleted successfully.');
    },
    onError: () => {
      toast.error('Failed to delete radio station. Please try again.');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      name,
      sourceUrl,
      isActive,
    }: {
      id: string;
      name: string;
      sourceUrl: string;
      isActive: boolean;
    }) => updateRadioSource(id, name, sourceUrl, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['radioSources'] });
      toast.success('Radio station updated successfully.');
    },
    onError: () => {
      toast.error('Failed to update radio station. Please try again.');
    },
  });

  const addMutation = useMutation({
    mutationFn: ({ name, sourceUrl }: { name: string; sourceUrl: string }) =>
      addRadioSource(name, sourceUrl),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['radioSources'] });
      toast.success('Radio station added successfully.');
    },
    onError: (e) => {
      toast.error(
        `Failed to add radio station: ${e instanceof Error ? e.message : 'Unknown error'}`,
      );
    },
  });

  if (isLoading) return <LoadingSpinner />;
  if (unauthorized) return null;
  if (error) return <AppError message={String(error)} />;
  if (!radioStations) return null;

  const totalStations = radioStations.length;
  const activeStations = radioStations.filter((s) => s.isActive).length;

  function hideModal() {
    setShowAddForm(false);
    setEditingStation(null);
  }

  function editStation(station: RadioSource) {
    setEditingStation({ ...station });
    setShowAddForm(true);
  }

  function handleDelete(stationId: string) {
    if (confirm('Are you sure you want to delete this radio station?')) {
      deleteMutation.mutate(stationId);
    }
  }

  async function handleFormSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const name = formData.get('name') as string;
    const sourceUrl = formData.get('url') as string;
    const isActive = formData.get('isActive') === 'on';

    try {
      if (editingStation) {
        await updateMutation.mutateAsync({
          id: editingStation.id,
          name,
          sourceUrl,
          isActive,
        });
      } else {
        await addMutation.mutateAsync({ name, sourceUrl });
      }
      hideModal();
    } catch {
      // Error toast is handled by mutation onError callbacks
    }
  }

  return (
    <>
      <div className="header">
        <h1 className="title">Radio Station Management</h1>
        <button
          className="add-button glass-card"
          onClick={() => setShowAddForm(true)}
        >
          Add New Station
        </button>
      </div>

      <div className="stats-grid">
        <div className="stat-card glass-card">
          <h2 className="stat-value">{totalStations}</h2>
          <p className="stat-label">Total Stations</p>
        </div>
        <div className="stat-card glass-card">
          <h2 className="stat-value">{activeStations}</h2>
          <p className="stat-label">Active Stations</p>
        </div>
      </div>

      <div className="content-card glass-card">
        <div className="radio-grid">
          {radioStations.map((station) => (
            <div className="radio-card" key={station.id}>
              <div className="radio-header">
                <h3 className="radio-name">{station.name}</h3>
                <span
                  className={`radio-status ${station.isActive ? 'active' : 'inactive'}`}
                >
                  {station.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              <div className="radio-info">
                <div className="radio-url" title={station.sourceUrl}>
                  {station.sourceUrl}
                </div>
              </div>
              <div className="radio-actions">
                <button
                  className="action-button edit"
                  onClick={() => editStation(station)}
                >
                  Edit
                </button>
                <button
                  className="action-button delete"
                  onClick={() => handleDelete(station.id)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {showAddForm && (
        <div
          className="modal"
          onClick={(e) => e.target === e.currentTarget && hideModal()}
        >
          <div className="modal-content">
            <div className="modal-header">
              <h2 className="modal-title">
                {editingStation
                  ? 'Edit Radio Station'
                  : 'Add New Radio Station'}
              </h2>
              <button className="close-button" onClick={hideModal}>
                &times;
              </button>
            </div>
            <form onSubmit={handleFormSubmit}>
              <div className="form-group">
                <label className="form-label">Station Name</label>
                <input
                  type="text"
                  name="name"
                  className="form-input"
                  required
                  defaultValue={editingStation?.name ?? ''}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Stream URL</label>
                <textarea
                  name="url"
                  className="form-input"
                  required
                  defaultValue={editingStation?.sourceUrl ?? ''}
                />
              </div>
              <div className="form-group">
                <label className="form-checkbox">
                  <input
                    type="checkbox"
                    name="isActive"
                    defaultChecked={editingStation?.isActive !== false}
                    disabled={editingStation === null}
                  />
                  <span>Station is active</span>
                </label>
              </div>
              <div className="form-actions">
                <button
                  type="button"
                  className="form-button glass-card secondary"
                  onClick={hideModal}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="form-button glass-card"
                  style={{ color: 'white' }}
                >
                  {editingStation ? 'Update Station' : 'Add Station'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
