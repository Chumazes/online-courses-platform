export function formatMoney(value) {
  const amount = Number(value ?? 0);
  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency: "RUB",
    maximumFractionDigits: 0
  }).format(amount);
}

export function formatDate(value) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "—";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(date);
}

export function formatLevel(value) {
  if (!value) {
    return "Не указан";
  }

  const normalized = String(value).toLowerCase();
  if (normalized === "beginner") {
    return "Начальный";
  }
  if (normalized === "intermediate") {
    return "Средний";
  }
  if (normalized === "advanced") {
    return "Продвинутый";
  }

  return value;
}

export function formatCourseStatus(value) {
  if (!value) {
    return "Не указан";
  }

  const normalized = String(value).toLowerCase();
  if (normalized === "draft") {
    return "Черновик";
  }
  if (normalized === "published") {
    return "Опубликован";
  }
  if (normalized === "archived") {
    return "Архив";
  }
  if (normalized === "active") {
    return "Активный";
  }
  if (normalized === "expired") {
    return "Завершён";
  }

  return value;
}
