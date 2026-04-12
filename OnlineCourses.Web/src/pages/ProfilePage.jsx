import { useEffect, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { filesApi, formatApiError, authApi } from "../lib/api";

export function ProfilePage() {
  const { user, refreshUser } = useAuth();
  const [fullName, setFullName] = useState("");
  const [bio, setBio] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

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
      <h1>Профиль</h1>
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <article className="panel">
        <div className="profile-header">
          <img
            alt="Avatar"
            className="avatar"
            src={user?.avatarUrl ? filesApi.buildFileUrl(user.avatarUrl) : "https://placehold.co/120x120?text=Avatar"}
          />
          <div>
            <p className="muted">Email: {user?.email}</p>
            <p className="muted">Роль: {user?.role}</p>
            <label className="btn btn--ghost btn--fit">
              {isUploading ? "Загрузка..." : "Загрузить аватар"}
              <input hidden onChange={handleAvatar} type="file" />
            </label>
          </div>
        </div>
      </article>

      <form className="panel form" onSubmit={handleSubmit}>
        <label className="label">
          Имя
          <input className="input" onChange={(event) => setFullName(event.target.value)} type="text" value={fullName} />
        </label>

        <label className="label">
          Bio
          <textarea className="input" onChange={(event) => setBio(event.target.value)} rows={5} value={bio} />
        </label>

        <button className="btn btn--primary btn--fit" disabled={isSaving} type="submit">
          {isSaving ? "Сохраняем..." : "Сохранить изменения"}
        </button>
      </form>
    </section>
  );
}

