import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { formatApiError } from "../lib/api";

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const from = location.state?.from?.pathname ?? "/catalog";

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      await signIn(email, password);
      navigate(from, { replace: true });
    } catch (err) {
      setError(formatApiError(err, "Не удалось войти в аккаунт."));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="auth-card">
      <h2>Вход</h2>
      <p className="muted">Используй student/teacher/admin аккаунты, чтобы проверить роли.</p>
      <ErrorBanner message={error} />

      <form className="form" onSubmit={handleSubmit}>
        <label className="label">
          Email
          <input
            autoComplete="email"
            className="input"
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@example.com"
            type="email"
            value={email}
          />
        </label>

        <label className="label">
          Пароль
          <input
            autoComplete="current-password"
            className="input"
            onChange={(event) => setPassword(event.target.value)}
            placeholder="••••••••"
            type="password"
            value={password}
          />
        </label>

        <button className="btn btn--primary btn--full" disabled={isLoading} type="submit">
          {isLoading ? "Входим..." : "Войти"}
        </button>
      </form>

      <p className="muted">
        Нет аккаунта? <Link to="/register">Регистрация</Link> • <Link to="/">Назад на главную</Link>
      </p>
    </section>
  );
}
