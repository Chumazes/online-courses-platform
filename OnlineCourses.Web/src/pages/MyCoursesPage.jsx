import { useEffect, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { enrollmentsApi, formatApiError } from "../lib/api";
import { formatCourseStatus } from "../lib/format";

export function MyCoursesPage() {
  const { role } = useAuth();
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isBusyCourseId, setIsBusyCourseId] = useState(null);

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const enrollments = await enrollmentsApi.getMy();
      setItems(enrollments ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить страницу."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  async function handleUnenroll(courseId) {
    setError("");
    setIsBusyCourseId(courseId);

    try {
      await enrollmentsApi.unenroll(courseId);
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось отписаться от курса."));
    } finally {
      setIsBusyCourseId(null);
    }
  }

  if (role !== "student") {
    return <Navigate replace to={role === "teacher" || role === "admin" ? "/dashboard" : "/catalog"} />;
  }

  if (isLoading) {
    return <div className="page-state">Загружаем страницу...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="catalog-head">
          <div>
            <h1>Мои курсы</h1>
            <p className="muted">Личный кабинет студента: прогресс, активные записи и готовые маршруты обучения.</p>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />

      {items.length === 0 ? <div className="panel panel--light">Вы пока не записаны ни на один курс.</div> : null}

      <div className="my-courses-grid">
        {items.map((item) => (
          <article className="panel my-course-card" key={item.enrollmentId}>
            <div className="panel-row">
              <h3>{item.courseTitle}</h3>
              <span className="chip">{formatCourseStatus(item.status)}</span>
            </div>

            <p className="muted">Запись: {new Date(item.enrollmentDate).toLocaleDateString("ru-RU")}</p>
            <p className="muted">Прогресс: {item.overallProgress}%</p>
            <p className="muted">
              {item.completedLessons ?? 0} из {item.totalLessons ?? 0} уроков завершено
            </p>

            <div className="progress-track">
              <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, Number(item.overallProgress ?? 0)))}%` }} />
            </div>

            <div className="my-course-card__summary">
              <span>Прогресс: {item.overallProgress}%</span>
              <span>
                {item.completedLessons ?? 0} из {item.totalLessons ?? 0} уроков завершено
              </span>
            </div>

            <div className="card-actions">
              <Link className="btn btn--primary" to={`/courses/${item.courseId}`}>
                Открыть курс
              </Link>
              <button
                className="btn btn--ghost"
                disabled={isBusyCourseId === item.courseId}
                onClick={() => handleUnenroll(item.courseId)}
                type="button"
              >
                {isBusyCourseId === item.courseId ? "Обновляем..." : "Отписаться"}
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
