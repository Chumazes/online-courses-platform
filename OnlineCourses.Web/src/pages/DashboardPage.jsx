import { Link } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, formatApiError } from "../lib/api";
import { formatCourseStatus, formatLevel } from "../lib/format";

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
        let items = [];

        if (role === "admin") {
          const response = await coursesApi.getAll({ pageNumber: 1, pageSize: 100, all: true });
          items = response?.items ?? [];
        } else {
          items = (await coursesApi.getMy()) ?? [];
        }

        if (active) {
          setCourses(items);
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
    const ratings = courses.map((course) => Number(course.avgRating ?? 0)).filter((rating) => rating > 0);
    const avgRating = ratings.length > 0 ? (ratings.reduce((sum, value) => sum + value, 0) / ratings.length).toFixed(1) : "0.0";
    const needsAttention = courses.filter(
      (course) => course.status === "draft" || Number(course.totalStudents ?? 0) === 0 || Number(course.avgRating ?? 0) === 0
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
      courses.find((course) => Number(course.totalStudents ?? 0) === 0) ||
      courses[0]
    );
  }, [courses]);

  if (isLoading) {
    return <div className="page-state">Загружаем панель...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>{role === "admin" ? "Панель администратора" : "Панель преподавателя"}</h1>
            <p className="muted">Единая сводка по курсам, студентам и следующим действиям.</p>
          </div>
          <Link className="btn btn--primary" to="/manage/courses">
            Открыть управление
          </Link>
        </div>
      </section>

      <ErrorBanner message={error} />

      <section className="feature-grid">
        <article className="panel">
          <h3>{summary.totalCourses}</h3>
          <p>Курсов</p>
        </article>
        <article className="panel">
          <h3>{summary.published}</h3>
          <p>Опубликовано</p>
          <p className="muted">Черновиков: {summary.draft}</p>
        </article>
        <article className="panel">
          <h3>{summary.totalStudents}</h3>
          <p>Студентов</p>
        </article>
        <article className="panel">
          <h3>{summary.avgRating}</h3>
          <p>Средний рейтинг</p>
        </article>
        <article className="panel">
          <h3>{summary.needsAttention}</h3>
          <p>Требуют внимания</p>
        </article>
      </section>

      {focusCourse ? (
        <section className="panel panel--inner">
          <h2>Фокус панели</h2>
          <p>
            Курс {focusCourse.title} сейчас в статусе <strong>{formatCourseStatus(focusCourse.status)}</strong>, студентов:{" "}
            {focusCourse.totalStudents ?? 0}.
          </p>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${focusCourse.courseId}/students`}>
              Студенты курса
            </Link>
            <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${focusCourse.courseId}/analytics`}>
              Аналитика
            </Link>
            <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${focusCourse.courseId}/sections`}>
              Секции
            </Link>
            {role === "admin" && (
              <Link className="btn btn--danger btn--fit" to={`/manage/courses/${focusCourse.courseId}/reviews`}>
                Модерация отзывов
              </Link>
            )}
          </div>
        </section>
      ) : (
        <section className="panel panel--light">Курсов пока нет. Создай первый курс в разделе управления.</section>
      )}

      <section className="stack">
        {courses.map((course) => (
          <article className="panel" key={course.courseId}>
            <div className="panel-row">
              <div>
                <h3>{course.title}</h3>
                <p className="muted">{course.description}</p>
              </div>
              <span className="chip">{formatCourseStatus(course.status)}</span>
            </div>
            <div className="chip-row">
              <span className="chip">{formatLevel(course.level)}</span>
              <span className="chip">Студентов: {course.totalStudents ?? 0}</span>
              <span className="chip">Рейтинг: {Number(course.avgRating ?? 0).toFixed(1)}</span>
            </div>
            <div className="card-actions">
              <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${course.courseId}/students`}>
                Студенты
              </Link>
              <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${course.courseId}/analytics`}>
                Аналитика
              </Link>
              <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${course.courseId}/sections`}>
                Секции
              </Link>
              {role === "admin" && (
                <Link className="btn btn--danger btn--fit" to={`/manage/courses/${course.courseId}/reviews`}>
                  Отзывы
                </Link>
              )}
            </div>
          </article>
        ))}
      </section>
    </section>
  );
}
