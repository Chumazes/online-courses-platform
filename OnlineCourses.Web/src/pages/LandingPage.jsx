import { Link } from "react-router-dom";
import mascot from "../assets/mascot.jpg";

export function LandingPage() {
  return (
    <section className="hero">
      <div className="hero__content">
        <p className="eyebrow">Учимся системно</p>
        <h1>Платформа онлайн-курсов с ролями student, teacher и admin</h1>
        <p className="hero__text">
          Веб-клиент перенесен с WPF на React: авторизация, каталог, прогресс, отзывы, управление курсами и
          админ-модерация.
        </p>
        <div className="hero__actions">
          <Link className="btn btn--primary" to="/courses">
            Открыть каталог
          </Link>
          <Link className="btn btn--ghost" to="/register">
            Создать аккаунт
          </Link>
        </div>
      </div>
      <div className="hero__media">
        <img alt="Mascot" src={mascot} />
      </div>
    </section>
  );
}

