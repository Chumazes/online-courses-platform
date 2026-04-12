import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, enrollmentsApi, formatApiError } from "../lib/api";
import { formatMoney } from "../lib/format";

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
      if (role === "student") {
        const enrollments = await enrollmentsApi.getMy();
        setItems(enrollments ?? []);
      } else {
        const courses = await coursesApi.getMy();
        setItems(courses ?? []);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить страницу."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [role]);

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

  if (isLoading) {
    return <div className="page-state">Загружаем...</div>;
  }

  return (
    <section className="stack">
      <h1>{role === "student" ? "Мои курсы" : "Мои курсы (преподаватель/админ)"}</h1>
      <ErrorBanner message={error} />

      {items.length === 0 ? <div className="panel muted">Пока пусто.</div> : null}

      {role === "student" ? (
        <div className="stack">
          {items.map((item) => (
            <article className="panel" key={item.enrollmentId}>
              <h3>{item.courseTitle}</h3>
              <p className="muted">
                Статус: {item.status} | Прогресс: {item.overallProgress}%
              </p>
              <div className="card-actions">
                <Link className="btn btn--ghost" to={`/courses/${item.courseId}`}>
                  Открыть
                </Link>
                <button
                  className="btn btn--danger"
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
      ) : (
        <div className="courses-grid">
          {items.map((course) => (
            <article className="card course-card" key={course.courseId}>
              <p className="chip">{course.level}</p>
              <h3>{course.title}</h3>
              <p className="muted">{course.description}</p>
              <p className="price">{formatMoney(course.price)}</p>
              <div className="card-actions">
                <Link className="btn btn--ghost" to={`/courses/${course.courseId}`}>
                  Открыть
                </Link>
                <Link className="btn btn--primary" to="/manage/courses">
                  Управлять
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

