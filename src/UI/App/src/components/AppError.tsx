interface AppErrorProps {
  message?: string;
}

export function AppError({
  message = 'An unexpected error occurred.',
}: AppErrorProps) {
  return (
    <div className="glass-card error">
      <h3>Error</h3>
      <p>{message}</p>
    </div>
  );
}
