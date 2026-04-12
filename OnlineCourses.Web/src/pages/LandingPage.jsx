import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import mascot from "../assets/mascot.jpg";
import { coursesApi } from "../lib/api";
import { formatLevel, formatMoney } from "../lib/format";

function getPriceCaption(course) {
  const price = Number(course?.price ?? 0);
  return price > 0 ? formatMoney(price) : "Бесплатно";
}

function getMetaCaption(course) {
  const parts = [];

  if (course?.authorName) {
    parts.push(`Автор: ${course.authorName}`);
  }

  if (course?.status) {
    parts.push(`Статус: ${course.status}`);
  }

  return parts.join(" • ");
}

export function LandingPage() {
  const [courseCount, setCourseCount] = useState(0);
  const [categories, setCategories] = useState([]);
  const [featuredCourses, setFeaturedCourses] = useState([]);

  useEffect(() => {
    let active = true;

    async function loadData() {
      try {
        const [courses, categoryList] = await Promise.all([
          coursesApi.getAll({ pageNumber: 1, pageSize: 3 }),
          coursesApi.getCategories()
        ]);

        if (!active) {
          return;
        }

        setCourseCount(courses?.totalCount ?? 0);
        setCategories(categoryList ?? []);
        setFeaturedCourses(courses?.items ?? []);
      } catch {
        if (!active) {
          return;
        }

        setCourseCount(0);
        setCategories([]);
        setFeaturedCourses([]);
      }
    }

    loadData();
    return () => {
      active = false;
    };
  }, []);

  return (
    <section className="stack">
      <section className="landing-hero panel">
        <div className="landing-hero__content">
          <h1>Старт в IT без лишнего шума</h1>
          <p className="landing-hero__text">
            Low-Level to Top — это платформа, где студенту понятно, что изучать, преподавателю удобно вести курсы, а
            администратору видно, как всё растёт.
          </p>

          <div className="landing-hero__buttons">
            <Link className="btn btn--ghost landing-hero__button" to="/why-it">
              Почему стоит попробовать IT?
            </Link>
            <Link className="btn btn--ghost landing-hero__button" to="/faq">
              FAQ по платформе
            </Link>
            <Link className="btn btn--ghost landing-hero__button" to="/stories">
              Отзывы и истории
            </Link>
          </div>

          <div className="landing-hero__buttons landing-hero__buttons--primary">
            <Link className="btn btn--primary landing-hero__button landing-hero__button--accent" to="/login">
              Войти в платформу
            </Link>
            <Link className="btn btn--ghost landing-hero__button" to="/register">
              Создать аккаунт
            </Link>
          </div>

          <div className="landing-stats">
            <article className="landing-stat">
              <strong>{courseCount}</strong>
              <span>Курсов в каталоге</span>
            </article>
            <article className="landing-stat">
              <strong>{categories.length}</strong>
              <span>Направлений</span>
            </article>
            <article className="landing-stat">
              <strong>3</strong>
              <span>Роли в системе</span>
            </article>
          </div>
        </div>

        <aside className="landing-hero__media">
          <div className="landing-hero__media-label">LLT</div>
          <div className="landing-hero__media-frame">
            <img alt="LLT mascot" src={mascot} />
          </div>
          <div className="landing-hero__media-foot">Low-Level to Top</div>
        </aside>
      </section>

      <section className="panel panel--light">
        <h2>Маршруты, которые уже доступны</h2>
        <div className="landing-categories">
          {categories.length === 0 ? (
            <span className="landing-category-chip">Категории скоро появятся</span>
          ) : (
            categories.map((category) => (
              <span className="landing-category-chip" key={category.categoryId}>
                {category.name}
              </span>
            ))
          )}
        </div>
      </section>

      <section className="panel panel--light">
        <h2>Что можно начать уже сейчас</h2>

        <div className="landing-course-grid">
          {featuredCourses.length === 0 ? (
            <div className="landing-loading-state">Публичная витрина скоро появится.</div>
          ) : (
            featuredCourses.map((course) => (
              <article className="landing-feature-card" key={course.courseId}>
                <h3>{course.title}</h3>
                <p>{course.description}</p>

                <div className="landing-feature-card__chips">
                  <span className="chip">{course.categoryName ?? "Без категории"}</span>
                  <span className="chip">{formatLevel(course.level)}</span>
                  <span className="chip chip--price">{getPriceCaption(course)}</span>
                </div>

                <span className="landing-feature-card__meta">{getMetaCaption(course) || "Маршрут уже доступен в каталоге"}</span>
              </article>
            ))
          )}
        </div>
      </section>

      <section className="landing-cta panel panel--inner">
        <h2>Хочешь увидеть весь путь внутри платформы?</h2>
        <p>
          Создай аккаунт, открой курс, проходи уроки, оставляй отзывы и следи за прогрессом в одном месте.
        </p>

        <div className="landing-hero__buttons landing-hero__buttons--primary">
          <Link className="btn btn--primary landing-hero__button landing-hero__button--accent" to="/register">
            Начать бесплатно
          </Link>
          <Link className="btn btn--ghost landing-hero__button" to="/login">
            Уже есть аккаунт
          </Link>
        </div>
      </section>
    </section>
  );
}
