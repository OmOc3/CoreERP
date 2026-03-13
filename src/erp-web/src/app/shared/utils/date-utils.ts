export function toDateInputValue(value: string | Date | null | undefined): string {
  if (!value) {
    return '';
  }

  const date = typeof value === 'string' ? new Date(value) : value;
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
}

export function toUtcIso(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  return new Date(`${value}T00:00:00`).toISOString();
}
