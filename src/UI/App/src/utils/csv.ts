type CsvValue = string | number | null | undefined;

function escapeCell(value: CsvValue): string {
  const text = value == null ? '' : String(value);
  return /[",\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

export function exportCsv(
  filename: string,
  header: string[],
  rows: CsvValue[][],
): void {
  const lines = [header, ...rows].map((row) => row.map(escapeCell).join(','));
  const blob = new Blob([lines.join('\r\n')], {
    type: 'text/csv;charset=utf-8;',
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
