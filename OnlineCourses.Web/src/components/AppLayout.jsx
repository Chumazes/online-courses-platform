import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import mascot from "../assets/mascot.jpg";
import { useAuth } from "../context/AuthContext";
import { filesApi } from "../lib/api";

function formatRole(role) {
  if (!role) {
    return "Пользователь";
  }

  const normalized = String(role).toLowerCase();
  if (normalized === "student") {
    return "Студент";
  }
  if (normalized === "teacher") {
    return "Преподаватель";
  }
  if (normalized === "admin") {
    return "Администратор";
  }

  return role;
}

function getInitials(name) {
  if (!name) {
    return "?";
  }

  return String(name)
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function AppLayout() {
  const { isAuthenticated, role, user, signOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const showBackButton = location.pathname !== "/";
  const canManageCourses = isAuthenticated && (role === "teacher" || role === "admin");

  async function handleLogout() {
    await signOut();
    navigate("/");
  }

  function handleBack() {
    navigate(-1);
  }

  function handleProfileOpen() {
    if (location.pathname === "/profile") {
      return;
    }

    navigate("/profile");
  }

  function handleManageOpen() {
    if (!canManageCourses || location.pathname === "/manage/courses") {
      return;
    }

    navigate("/manage/courses");
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div className="topbar__content">
          <Link className="brand" to="/">
            <img alt="LLT" className="brand-logo" src={mascot} />
            <span className="brand-block">
              <span className="brand-title">Low-Level to Top</span>
              <span className="brand-subtitle">Платформа курсов, практики и роста в IT</span>
            </span>
          </Link>

          <span className="brand-center">Online Courses Platform</span>

          <div className="topbar__actions">
            {showBackButton ? (
              <button className="btn btn--ghost" onClick={handleBack} type="button">
                Назад
              </button>
            ) : null}

            {isAuthenticated ? (
              <>
                {canManageCourses ? (
                  <button className="btn btn--chrome" onClick={handleManageOpen} type="button">
                    Управление
                  </button>
                ) : null}

                <button className="user-chip user-chip--clickable" onClick={handleProfileOpen} type="button">
                  {user?.avatarUrl ? (
                    <img alt={user?.fullName ?? "Профиль"} className="user-chip__avatar" src={filesApi.buildFileUrl(user.avatarUrl)} />
                  ) : (
                    <span className="user-chip__avatar user-chip__avatar--fallback">{getInitials(user?.fullName)}</span>
                  )}
                  <span className="user-chip__meta">
                    <strong>{user?.fullName ?? "Профиль"}</strong>
                    <small>{formatRole(role)}</small>
                  </span>
                </button>

                <button className="btn btn--ghost" onClick={handleLogout} type="button">
                  Выйти
                </button>
              </>
            ) : (
              <>
                <Link className="btn btn--ghost" to="/login">
                  Войти
                </Link>
                <Link className="btn btn--primary" to="/register">
                  Регистрация
                </Link>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="page">
        <Outlet />
      </main>
    </div>
  );
}
