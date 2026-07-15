interface PaginationProps {
  page: number;
  pageCount: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export function Pagination({
  page,
  pageCount,
  totalItems,
  pageSize,
  onPageChange,
}: PaginationProps) {
  if (pageCount <= 1) return null;

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  return (
    <div className="pagination">
      <span className="pagination-info">
        Showing {start}-{end} of {totalItems}
      </span>
      <div className="pagination-controls">
        <button
          className="glass-button pagination-button"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Prev
        </button>
        <span className="pagination-page">
          {page} / {pageCount}
        </span>
        <button
          className="glass-button pagination-button"
          disabled={page >= pageCount}
          onClick={() => onPageChange(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
