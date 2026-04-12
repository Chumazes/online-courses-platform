import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { authApi, filesApi, formatApiError } from "../lib/api";

export function ProfilePage() {
  const { user, role, refreshUser } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [bio, setBio] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  const canOpenDashboard = role === "teacher" || role === "admin";

  const initials = useMemo(() => {
    if (!user?.fullName) {
      return "?";
    }

    return String(user.fullName)
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? "")
      .join("");
  }, [user?.fullName]);

  useEffect(() => {
    setFullName(user?.fullName ?? "");
    setBio(user?.bio ?? "");
  }, [user]);

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSaving(true);

    try {
      await authApi.updateMe({ fullName, bio });
      await refreshUser();
      setSuccess("Профиль обновлен.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось обновить профиль."));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleAvatar(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setError("");
    setSuccess("");
    setIsUploading(true);

    try {
      await filesApi.uploadAvatar(file);
      await refreshUser();
      setSuccess("Аватар обновлен.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить аватар."));
    } finally {
      setIsUploading(false);
    }
  }

  return (
    <section className="stack">
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="panel panel--light profile-shell">
        <div className="profile-shell__sidebar">
          {user?.avatarUrl ? (
            <img alt="Avatar" className="profile-avatar" src={filesApi.buildFileUrl(user.avatarUrl)} />
          ) : (
            <div className="profile-avatar profile-avatar--fallback">{initials}</div>
          )}

          <div className="profile-pill profile-pill--title">{fullName || "Профиль"}</div>
          <div className="profile-pill profile-pill--role">{user?.role ?? role}</div>

          <label className="btn btn--chrome profile-shell__button">
            {isUploading ? "Загрузка..." : "Загрузить аватар"}
            <input hidden onChange={handleAvatar} type="file" />
          </label>

          {canOpenDashboard ? (
            <button className="btn btn--ghost profile-shell__button" onClick={() => navigate("/dashboard")} type="button">
              {role === "admin" ? "Открыть панель администратора" : "Открыть панель преподавателя"}
            </button>
          ) : null}
        </div>

        <form className="profile-shell__content" onSubmit={handleSubmit}>
          <div className="profile-card">
            <h2>Карточка профиля</h2>
            <div className="profile-card__grid">
              <span>Email</span>
              <strong>{user?.email}</strong>
              <span>Роль</span>
              <strong>{user?.role ?? role}</strong>
            </div>
          </div>

          <label className="label">
            Имя
            <input className="input" onChange={(event) => setFullName(event.target.value)} type="text" value={fullName} />
          </label>

          <label className="label">
            О пользователе
            <textarea className="input profile-bio" onChange={(event) => setBio(event.target.value)} rows={8} value={bio} />
          </label>

          <button className="btn btn--primary btn--fit" disabled={isSaving} type="submit">
            {isSaving ? "Сохраняем..." : "Сохранить bio"}
          </button>
        </form>
      </section>
    </section>
  );
}
