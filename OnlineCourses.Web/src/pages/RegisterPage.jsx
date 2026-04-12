import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { formatApiError } from "../lib/api";

export function RegisterPage() {
  const { signUp } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      await signUp({ fullName, email, password });
      navigate("/courses", { replace: true });
    } catch (err) {
      setError(formatApiError(err, "Не удалось зарегистрироваться."));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="auth-card">
      <h2>Регистрация</h2>
      <p className="muted">После регистрации роль по умолчанию: student.</p>
      <ErrorBanner message={error} />

      <form className="form" onSubmit={handleSubmit}>
        <label className="label">
          Имя
          <input className="input" onChange={(event) => setFullName(event.target.value)} type="text" value={fullName} />
        </label>

        <label className="label">
          Email
          <input className="input" onChange={(event) => setEmail(event.target.value)} type="email" value={email} />
        </label>

        <label className="label">
          Пароль
          <input
            className="input"
            minLength={6}
            onChange={(event) => setPassword(event.target.value)}
            type="password"
            value={password}
          />
        </label>

        <button className="btn btn--primary btn--full" disabled={isLoading} type="submit">
          {isLoading ? "Создаем..." : "Зарегистрироваться"}
        </button>
      </form>

      <p className="muted">
        Уже есть аккаунт? <Link to="/login">Войти</Link>
      </p>
    </section>
  );
}

