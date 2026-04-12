const SESSION_KEY = "online_courses_session";

export function loadSession() {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw);
    if (!parsed?.accessToken) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export function saveSession(session) {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession() {
  localStorage.removeItem(SESSION_KEY);
}

export function updateSessionTokens(accessToken, refreshToken) {
  const current = loadSession();
  if (!current) {
    return null;
  }

  const next = {
    ...current,
    accessToken: accessToken ?? current.accessToken,
    refreshToken: refreshToken ?? current.refreshToken
  };

  saveSession(next);
  return next;
}
