export interface RankListItem {
  key: string;
  label: string;
  sublabel?: string | null;
  value: string;
  valueTitle?: string;
}

interface RankListProps {
  title: string;
  items: RankListItem[];
  emptyMessage: string;
  showRank?: boolean;
  onViewAll?: () => void;
}

export function RankList({
  title,
  items,
  emptyMessage,
  showRank = true,
  onViewAll,
}: RankListProps) {
  return (
    <section className="glass-card panel">
      <div className="panel-header">
        <h2 className="panel-title">{title}</h2>
        {onViewAll && (
          <button className="glass-button view-all-button" onClick={onViewAll}>
            View all
          </button>
        )}
      </div>
      {items.length === 0 ? (
        <p className="panel-empty">{emptyMessage}</p>
      ) : (
        <ol className="rank-list">
          {items.map((item, index) => (
            <li className="rank-item" key={item.key}>
              {showRank && (
                <span className={`rank-badge rank-${index + 1}`}>
                  {index + 1}
                </span>
              )}
              <div className="rank-info">
                <div className="rank-label" title={item.label}>
                  {item.label}
                </div>
                {item.sublabel && (
                  <div className="rank-sublabel" title={item.sublabel}>
                    {item.sublabel}
                  </div>
                )}
              </div>
              <span className="rank-value" title={item.valueTitle}>
                {item.value}
              </span>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
