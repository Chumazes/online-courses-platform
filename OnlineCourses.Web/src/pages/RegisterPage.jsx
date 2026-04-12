import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { formatApiError } from "../lib/api";

function isValidEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

export function RegisterPage() {
  const { signUp } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");

    if (!fullName.trim()) {
      setError("Введите имя.");
      return;
    }

    if (!email.trim()) {
      setError("Введите email.");
      return;
    }

    if (!isValidEmail(email.trim())) {
      setError("Введите корректный email.");
      return;
    }

    if (password.trim().length < 6) {
      setError("Пароль должен содержать минимум 6 символов.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Пароли не совпадают.");
      return;
    }

    setIsLoading(true);

    try {
      await signUp({ fullName: fullName.trim(), email: email.trim(), password });
      navigate("/catalog", { replace: true });
    } catch (err) {
      setError(formatApiError(err, "Не удалось зарегистрироваться."));
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="auth-card">
      <h2>Регистрация</h2>
      <p className="muted" />
      <ErrorBanner message={error} />

      <form className="form" noValidate onSubmit={handleSubmit}>
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
          <input className="input" onChange={(event) => setPassword(event.target.value)} type="password" value={password} />
        </label>

        <label className="label">
          Повторите пароль
          <input className="input" onChange={(event) => setConfirmPassword(event.target.value)} type="password" value={confirmPassword} />
        </label>

        <button className="btn btn--primary btn--full" disabled={isLoading} type="submit">
          {isLoading ? "Создаём..." : "Зарегистрироваться"}
        </button>
      </form>

      <p className="muted">
        Уже есть аккаунт? <Link to="/login">Войти</Link>
      </p>
    </section>
  );
}
