import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, formatApiError } from "../lib/api";
import { formatCourseStatus, formatLevel, formatMoney } from "../lib/format";

export function DashboardPage() {
  const { role } = useAuth();
  const [courses, setCourses] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadData() {
      setError("");
      setIsLoading(true);
      try {
        const response =
          role === "admin"
            ? await coursesApi.getAll({
                pageNumber: 1,
                pageSize: 100,
                all: true
              })
            : await coursesApi.getMy();

        if (active) {
          setCourses(Array.isArray(response) ? response : response?.items ?? []);
        }
      } catch (err) {
        if (active) {
          setError(formatApiError(err, "Не удалось загрузить панель."));
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    loadData();
    return () => {
      active = false;
    };
  }, [role]);

  const summary = useMemo(() => {
    const totalCourses = courses.length;
    const published = courses.filter((course) => course.status === "published").length;
    const draft = courses.filter((course) => course.status === "draft").length;
    const totalStudents = courses.reduce((sum, course) => sum + Number(course.totalStudents ?? 0), 0);
    const ratings = courses.map((course) => Number(course.avgRating ?? 0)).filter((value) => value > 0);
    const avgRating = ratings.length > 0 ? (ratings.reduce((sum, value) => sum + value, 0) / ratings.length).toFixed(1) : null;
    const needsAttention = courses.filter(
      (course) => course.status === "draft" || (course.status === "published" && Number(course.totalStudents ?? 0) === 0)
    ).length;

    return {
      totalCourses,
      published,
      draft,
      totalStudents,
      avgRating,
      needsAttention
    };
  }, [courses]);

  const focusCourse = useMemo(() => {
    if (courses.length === 0) {
      return null;
    }

    return (
      courses.find((course) => course.status === "draft") ||
      courses.find((course) => course.status === "published" && Number(course.totalStudents ?? 0) === 0) ||
      courses[0]
    );
  }, [courses]);

  if (isLoading) {
    return <div className="page-state">Загружаем панель...</div>;
  }

  return (
    <section className="stack">
      <section className="panel management-hero">
        <div className="panel-row management-hero__row">
          <div className="management-hero__copy">
            <h1>{role === "admin" ? "Панель администратора" : "Панель преподавателя"}</h1>
            <p className="management-hero__subtitle">
              Сводка по твоим курсам, студентам и следующему действию без лишних переходов между экранами.
            </p>
          </div>

          <div className="card-actions management-hero__actions">
            <Link className="btn btn--primary btn--fit" to="/manage/courses">
              Открыть управление
            </Link>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />

      <section className="feature-grid management-metrics">
        <article className="panel management-metric">
          <p className="muted">Курсы</p>
          <h3>{summary.totalCourses}</h3>
          <span>Всего в этой панели</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Опубликовано</p>
          <h3>{summary.published}</h3>
          <span>Черновиков: {summary.draft}</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Студенты</p>
          <h3>{summary.totalStudents}</h3>
          <span>Во всех курсах панели</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Средний рейтинг</p>
          <h3>{summary.avgRating ? summary.avgRating : "Без оценок"}</h3>
          <span>По курсам с оценками</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Требуют внимания</p>
          <h3>{summary.needsAttention}</h3>
          <span>Черновики и пустые опубликованные</span>
        </article>
      </section>

      {focusCourse ? (
        <section className="panel panel--inner dashboard-focus">
          <div className="dashboard-focus__main">
            <h2>
              {focusCourse.status === "draft"
                ? `Нужен фокус на курсе «${focusCourse.title}»`
                : `Пора усилить курс «${focusCourse.title}»`}
            </h2>
            <p>
              {focusCourse.status === "draft"
                ? "В панели есть черновик. Его стоит проверить первым, чтобы не оставлять курс вне витрины и рабочего сценария."
                : "Курс уже опубликован, но у него пока нет студентов. Стоит посмотреть структуру, описание и позиционирование в каталоге."}
            </p>
          </div>
          <div className="dashboard-focus__aside">
            <span className="muted">Следующий шаг</span>
            <strong>
              {focusCourse.status === "draft"
                ? "Следующий шаг: открой управление курсами и доведи черновик до публикации."
                : "Следующий шаг: открой аналитику или управление курсом и проверь, как его можно сделать понятнее для студента."}
            </strong>
          </div>
        </section>
      ) : (
        <section className="panel panel--light management-empty">Курсов пока нет. Создай первый курс в разделе управления.</section>
      )}

      <section className="panel panel--light dashboard-surface">
        <div className="panel-row">
          <div>
            <h2>Курсы в работе</h2>
            <p className="muted">Ниже собраны курсы с быстрыми действиями без захода в лишние промежуточные экраны.</p>
          </div>
        </div>

        <div className="stack">
          {courses.map((course) => (
            <article className="panel management-card" key={course.courseId}>
              <div className="panel-row">
                <div>
                  <h3>{course.title}</h3>
                  <p className="muted">{course.description || "Добавь описание, чтобы преподавательская панель выглядела законченной."}</p>
                </div>
                <span className="chip">{formatCourseStatus(course.status)}</span>
              </div>

              <div className="management-strip">
                <span>{formatLevel(course.level)}</span>
                <span>{formatMoney(course.price ?? 0)}</span>
                <span>{Number(course.totalStudents ?? 0)} студент</span>
                <span>{Number(course.avgRating ?? 0) > 0 ? `${Number(course.avgRating).toFixed(1)} рейтинг` : "Без оценок"}</span>
              </div>

              <p className="muted">
                {course.status === "draft"
                  ? "Это черновик: сначала проверь описание, статус и секции."
                  : Number(course.totalStudents ?? 0) > 0
                    ? "Студенты уже есть, но отзывы пока нет."
                    : "Курс опубликован, но студентов пока нет. Есть смысл проверить карточку в каталоге."}
              </p>

              <div className="card-actions management-card__actions">
                <Link className="btn btn--primary btn--fit" to={`/manage/courses/${course.courseId}/students`}>
                  Студенты курса
                </Link>
                <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${course.courseId}/analytics`}>
                  Аналитика
                </Link>
                <Link className="btn btn--chrome btn--fit" to={`/manage/courses/${course.courseId}/sections`}>
                  Секции
                </Link>
                {role === "admin" ? (
                  <Link className="btn btn--danger btn--fit" to={`/manage/courses/${course.courseId}/reviews`}>
                    Отзывы
                  </Link>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      </section>
    </section>
  );
}
