import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function navClassName({ isActive }) {
  return `nav-link${isActive ? " nav-link--active" : ""}`;
}

export function AppLayout() {
  const { isAuthenticated, role, user, signOut } = useAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await signOut();
    navigate("/");
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div className="topbar__content">
          <Link className="brand" to="/">
            Online Courses
          </Link>

          <nav className="nav">
            <NavLink className={navClassName} to="/courses">
              Каталог
            </NavLink>

            {isAuthenticated && (
              <NavLink className={navClassName} to="/my-courses">
                Мои курсы
              </NavLink>
            )}

            {isAuthenticated && (
              <NavLink className={navClassName} to="/profile">
                Профиль
              </NavLink>
            )}

            {isAuthenticated && (role === "teacher" || role === "admin") && (
              <NavLink className={navClassName} to="/manage/courses">
                Управление
              </NavLink>
            )}

            {isAuthenticated && role === "admin" && (
              <>
                <NavLink className={navClassName} to="/manage/categories">
                  Категории
                </NavLink>
                <NavLink className={navClassName} to="/manage/reviews">
                  Модерация
                </NavLink>
              </>
            )}
          </nav>

          <div className="topbar__actions">
            {isAuthenticated ? (
              <>
                <span className="user-chip">{user?.fullName ?? user?.email ?? "Пользователь"}</span>
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

