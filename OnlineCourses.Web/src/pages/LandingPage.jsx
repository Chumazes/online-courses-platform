import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import mascot from "../assets/mascot.jpg";
import { coursesApi } from "../lib/api";

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
        setCategories((categoryList ?? []).slice(0, 6));
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
      <section className="hero">
        <div className="hero__content">
          <h1>Старт в IT без лишнего шума</h1>
          <p className="hero__text">
            Low-Level to Top помогает студенту видеть следующий шаг, преподавателю управлять курсами без хаоса, а
            администратору контролировать качество и рост платформы.
          </p>

          <div className="hero__actions hero__actions--small">
            <Link className="btn btn--ghost" to="/why-it">
              Почему IT?
            </Link>
            <Link className="btn btn--ghost" to="/faq">
              FAQ
            </Link>
            <Link className="btn btn--ghost" to="/stories">
              Отзывы и истории
            </Link>
          </div>

          <div className="hero__actions">
            <Link className="btn btn--primary" to="/catalog">
              Войти в платформу
            </Link>
            <Link className="btn btn--ghost" to="/register">
              Создать аккаунт
            </Link>
          </div>

          <div className="stats-row">
            <article className="stat-card">
              <strong>{courseCount}</strong>
              <span>Курсов в каталоге</span>
            </article>
            <article className="stat-card">
              <strong>{categories.length}</strong>
              <span>Направлений</span>
            </article>
            <article className="stat-card">
              <strong>3</strong>
              <span>Роли в системе</span>
            </article>
          </div>
        </div>

        <div className="hero__media">
          <div className="hero__media-label">LLT</div>
          <img alt="Mascot" src={mascot} />
          <div className="hero__media-foot">Low-Level to Top</div>
        </div>
      </section>

      <section className="panel panel--light">
        <h2>Маршруты, которые уже доступны</h2>
        <div className="chip-row">
          {categories.length === 0 ? (
            <span className="chip">Категории скоро появятся</span>
          ) : (
            categories.map((category) => (
              <span className="chip" key={category.categoryId}>
                {category.name}
              </span>
            ))
          )}
        </div>
      </section>

      <section className="panel panel--light">
        <h2>Что можно начать уже сейчас</h2>
        <div className="landing-cards">
          {featuredCourses.length === 0 ? (
            <p className="muted">Публичных курсов пока нет.</p>
          ) : (
            featuredCourses.map((course) => (
              <article className="landing-course" key={course.courseId}>
                <h3>{course.title}</h3>
                <p className="muted">{course.description}</p>
                <div className="chip-row">
                  <span className="chip">{course.level}</span>
                  <span className="chip">{course.categoryName ?? "Без категории"}</span>
                </div>
                <Link className="btn btn--ghost btn--fit" to={`/courses/${course.courseId}`}>
                  Открыть курс
                </Link>
              </article>
            ))
          )}
        </div>
      </section>
    </section>
  );
}
