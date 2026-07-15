interface SortableThProps {
  label: string;
  active: boolean;
  direction: 'asc' | 'desc';
  onSort: () => void;
  className?: string;
}

export function SortableTh({
  label,
  active,
  direction,
  onSort,
  className,
}: SortableThProps) {
  return (
    <th
      className={`sortable ${className ?? ''}`}
      onClick={onSort}
      title={`Sort by ${label}`}
      aria-sort={
        active ? (direction === 'asc' ? 'ascending' : 'descending') : 'none'
      }
    >
      <span className="sort-header">
        {label}
        <span
          className={`sort-arrow ${active ? `active ${direction}` : ''}`}
          aria-hidden="true"
        />
      </span>
    </th>
  );
}
