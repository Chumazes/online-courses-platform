import { clearSession, loadSession, updateSessionTokens } from "./session";

const BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5064").replace(/\/+$/, "");

export class ApiError extends Error {
  constructor(status, message, payload = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.payload = payload;
  }
}

function toQueryString(query = {}) {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") {
      continue;
    }

    params.set(key, String(value));
  }

  const raw = params.toString();
  return raw ? `?${raw}` : "";
}

function extractMessage(payload, fallback = "Request failed") {
  if (!payload) {
    return fallback;
  }

  if (typeof payload === "string") {
    return payload;
  }

  if (typeof payload?.message === "string") {
    return payload.message;
  }

  return fallback;
}

async function parseResponseBody(response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (response.status === 204) {
    return null;
  }

  if (contentType.includes("application/json")) {
    return response.json();
  }

  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

async function refreshAccessToken() {
  const session = loadSession();
  if (!session?.refreshToken) {
    return false;
  }

  const response = await fetch(`${BASE_URL}/api/auth/refresh`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      refreshToken: session.refreshToken
    })
  });

  if (!response.ok) {
    clearSession();
    return false;
  }

  const payload = await parseResponseBody(response);
  updateSessionTokens(payload?.accessToken, payload?.refreshToken);
  return true;
}

export async function apiRequest(path, options = {}, retrying = false) {
  const {
    method = "GET",
    body,
    query,
    auth = true,
    headers = {},
    signal
  } = options;

  const session = loadSession();
  const url = `${BASE_URL}${path}${toQueryString(query)}`;
  const requestHeaders = { ...headers };

  let requestBody = body;
  if (body && !(body instanceof FormData)) {
    requestHeaders["Content-Type"] = requestHeaders["Content-Type"] ?? "application/json";
    requestBody = JSON.stringify(body);
  }

  if (auth && session?.accessToken) {
    requestHeaders.Authorization = `Bearer ${session.accessToken}`;
  }

  const response = await fetch(url, {
    method,
    headers: requestHeaders,
    body: requestBody,
    signal
  });

  if (response.status === 401 && auth && !retrying && session?.refreshToken) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return apiRequest(path, options, true);
    }
  }

  const payload = await parseResponseBody(response);

  if (!response.ok) {
    throw new ApiError(response.status, extractMessage(payload, response.statusText), payload);
  }

  return payload;
}

export const authApi = {
  register: (input) => apiRequest("/api/auth/register", { method: "POST", body: input, auth: false }),
  login: (input) => apiRequest("/api/auth/login", { method: "POST", body: input, auth: false }),
  me: () => apiRequest("/api/auth/me"),
  updateMe: (input) => apiRequest("/api/auth/me", { method: "PUT", body: input }),
  logout: (refreshToken) =>
    apiRequest("/api/auth/logout", { method: "POST", body: { refreshToken } }).catch(() => null),
  logoutAll: () => apiRequest("/api/auth/logout-all", { method: "POST" })
};

export const coursesApi = {
  getAll: (query) => apiRequest("/api/courses", { query, auth: false }),
  getById: (id) => apiRequest(`/api/courses/${id}`, { auth: false }),
  getMy: () => apiRequest("/api/courses/my"),
  create: (input) => apiRequest("/api/courses", { method: "POST", body: input }),
  update: (id, input) => apiRequest(`/api/courses/${id}`, { method: "PUT", body: input }),
  remove: (id) => apiRequest(`/api/courses/${id}`, { method: "DELETE" }),
  getCategories: () => apiRequest("/api/courses/categories", { auth: false }),
  createCategory: (input) => apiRequest("/api/courses/categories", { method: "POST", body: input }),
  updateCategory: (id, input) => apiRequest(`/api/courses/categories/${id}`, { method: "PUT", body: input }),
  removeCategory: (id) => apiRequest(`/api/courses/categories/${id}`, { method: "DELETE" })
};

export const sectionsApi = {
  getByCourseId: (courseId) => apiRequest(`/api/courses/${courseId}/sections`, { auth: false }),
  getById: (courseId, sectionId) => apiRequest(`/api/courses/${courseId}/sections/${sectionId}`, { auth: false }),
  create: (courseId, input) => apiRequest(`/api/courses/${courseId}/sections`, { method: "POST", body: input }),
  update: (courseId, sectionId, input) =>
    apiRequest(`/api/courses/${courseId}/sections/${sectionId}`, { method: "PUT", body: input }),
  remove: (courseId, sectionId) => apiRequest(`/api/courses/${courseId}/sections/${sectionId}`, { method: "DELETE" })
};

export const lessonsApi = {
  getBySectionId: (sectionId) => apiRequest(`/api/sections/${sectionId}/lessons`, { auth: false }),
  getById: (sectionId, lessonId) => apiRequest(`/api/sections/${sectionId}/lessons/${lessonId}`, { auth: false }),
  create: (sectionId, input) => apiRequest(`/api/sections/${sectionId}/lessons`, { method: "POST", body: input }),
  update: (sectionId, lessonId, input) =>
    apiRequest(`/api/sections/${sectionId}/lessons/${lessonId}`, { method: "PUT", body: input }),
  remove: (sectionId, lessonId) => apiRequest(`/api/sections/${sectionId}/lessons/${lessonId}`, { method: "DELETE" })
};

export const enrollmentsApi = {
  getMy: () => apiRequest("/api/enrollments/my"),
  enroll: (courseId) => apiRequest("/api/enrollments", { method: "POST", body: { courseId } }),
  unenroll: (courseId) => apiRequest(`/api/enrollments/${courseId}`, { method: "DELETE" }),
  getByCourse: (courseId) => apiRequest(`/api/enrollments/course/${courseId}`)
};

export const progressApi = {
  update: (input) => apiRequest("/api/progress/update", { method: "POST", body: input }),
  getCourse: (courseId) => apiRequest(`/api/progress/course/${courseId}`),
  getLesson: (lessonId) => apiRequest(`/api/progress/lesson/${lessonId}`),
  getMy: () => apiRequest("/api/progress/my")
};

export const reviewsApi = {
  getByCourse: (courseId) => apiRequest(`/api/reviews/course/${courseId}`, { auth: false }),
  getRating: (courseId) => apiRequest(`/api/reviews/course/${courseId}/rating`, { auth: false }),
  getModeration: (courseId) => apiRequest(`/api/reviews/course/${courseId}/moderation`),
  getMy: () => apiRequest("/api/reviews/my"),
  create: (courseId, input) => apiRequest(`/api/reviews/course/${courseId}`, { method: "POST", body: input }),
  update: (reviewId, input) => apiRequest(`/api/reviews/${reviewId}`, { method: "PUT", body: input }),
  remove: (reviewId) => apiRequest(`/api/reviews/${reviewId}`, { method: "DELETE" }),
  approve: (reviewId, approve) => apiRequest(`/api/reviews/${reviewId}/approve`, { method: "PUT", query: { approve } })
};

function toAbsolute(url) {
  if (!url) {
    return "";
  }

  if (url.startsWith("http://") || url.startsWith("https://")) {
    return url;
  }

  return `${BASE_URL}/${url.replace(/^\/+/, "")}`;
}

export const filesApi = {
  uploadAvatar: (file) => {
    const formData = new FormData();
    formData.append("file", file);
    return apiRequest("/api/files/avatar", { method: "POST", body: formData });
  },
  uploadLessonFile: (lessonId, file, title) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("title", title ?? file.name);
    return apiRequest(`/api/files/lesson/${lessonId}`, { method: "POST", body: formData });
  },
  buildFileUrl: (fileUrl) => toAbsolute(fileUrl),
  buildDownloadUrl: (fileUrl) => `${BASE_URL}/api/files/download${toQueryString({ fileUrl })}`
};

export function formatApiError(error, fallback = "Произошла ошибка.") {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return "Сессия истекла. Войди снова.";
    }

    if (error.status === 403) {
      return "Недостаточно прав для этого действия.";
    }

    if (error.status === 404) {
      return "Ресурс не найден.";
    }

    return error.message;
  }

  if (error instanceof TypeError) {
    return "Не удалось подключиться к API.";
  }

  return fallback;
}

