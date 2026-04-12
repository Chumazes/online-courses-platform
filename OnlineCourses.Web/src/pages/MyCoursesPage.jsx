import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { enrollmentsApi, formatApiError, progressApi } from "../lib/api";

function getStatusPriority(status) {
  const normalized = String(status ?? "").toLowerCase();

  if (normalized === "completed") {
    return 3;
  }

  if (normalized === "active") {
    return 2;
  }

  if (normalized === "draft") {
    return 1;
  }

  return 0;
}

function formatCourseStatus(status, overallProgress) {
  const normalized = String(status ?? "").toLowerCase();
  if (Number(overallProgress ?? 0) >= 100 || normalized === "completed") {
    return "Завершён";
  }
  if (normalized === "active") {
    return "Активный";
  }
  return status || "В процессе";
}

export function MyCoursesPage() {
  const [items, setItems] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isBusyCourseId, setIsBusyCourseId] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [enrollments, progressItems] = await Promise.all([enrollmentsApi.getMy(), progressApi.getMy().catch(() => [])]);
      const enrollmentsByCourse = new Map();

      for (const enrollment of enrollments ?? []) {
        if (String(enrollment.status ?? "").toLowerCase() === "expired") {
          continue;
        }

        const current = enrollmentsByCourse.get(enrollment.courseId);
        if (!current) {
          enrollmentsByCourse.set(enrollment.courseId, enrollment);
          continue;
        }

        const currentPriority = getStatusPriority(current.status);
        const nextPriority = getStatusPriority(enrollment.status);
        const currentDate = new Date(current.enrollmentDate ?? 0).getTime();
        const nextDate = new Date(enrollment.enrollmentDate ?? 0).getTime();

        if (nextPriority > currentPriority || (nextPriority === currentPriority && nextDate > currentDate)) {
          enrollmentsByCourse.set(enrollment.courseId, enrollment);
        }
      }

      const progressByCourse = new Map();
      for (const progress of progressItems ?? []) {
        const current = progressByCourse.get(progress.courseId);
        if (!current) {
          progressByCourse.set(progress.courseId, progress);
          continue;
        }

        const currentPriority = getStatusPriority(current.status);
        const nextPriority = getStatusPriority(progress.status);
        const currentCompletedAt = new Date(current.completedAt ?? 0).getTime();
        const nextCompletedAt = new Date(progress.completedAt ?? 0).getTime();

        if (nextPriority > currentPriority || (nextPriority === currentPriority && nextCompletedAt > currentCompletedAt)) {
          progressByCourse.set(progress.courseId, progress);
        }
      }

      const merged = Array.from(enrollmentsByCourse.values())
        .map((enrollment) => {
          const courseProgress = progressByCourse.get(enrollment.courseId);
          return {
            ...enrollment,
            totalLessons: Number(courseProgress?.totalLessons ?? 0),
            completedLessons: Number(courseProgress?.completedLessons ?? 0),
            overallProgress: Number(courseProgress?.overallProgress ?? enrollment.overallProgress ?? 0),
            status: courseProgress?.status ?? enrollment.status
          };
        })
        .sort((a, b) => new Date(b.enrollmentDate ?? 0).getTime() - new Date(a.enrollmentDate ?? 0).getTime());

      setItems(merged);
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить ваши курсы."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  async function handleUnenroll(courseId) {
    setError("");
    setSuccess("");
    setIsBusyCourseId(courseId);

    try {
      await enrollmentsApi.unenroll(courseId);
      setSuccess("Вы отписались от курса.");
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось обновить запись на курс."));
    } finally {
      setIsBusyCourseId(null);
    }
  }

  const activeCount = useMemo(() => items.filter((item) => Number(item.overallProgress ?? 0) < 100).length, [items]);

  if (isLoading) {
    return <div className="page-state">Загружаем ваши курсы...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <h1>Мои курсы</h1>
        <p className="muted">Личный кабинет студента: прогресс, активные записи и готовые маршруты обучения.</p>
        {items.length > 0 ? <p className="muted">Активных курсов: {activeCount}</p> : null}
      </section>

      {success ? <div className="success-banner">{success}</div> : null}
      <ErrorBanner message={error} />

      {items.length === 0 ? <div className="panel panel--light">Вы пока не записаны ни на один курс.</div> : null}

      <div className="my-courses-grid">
        {items.map((item) => (
          <article className="panel my-course-card" key={item.enrollmentId}>
            <div className="panel-row">
              <h3>{item.courseTitle}</h3>
              <span className="chip">{formatCourseStatus(item.status, item.overallProgress)}</span>
            </div>

            <p className="muted">Запись: {new Date(item.enrollmentDate).toLocaleDateString("ru-RU")}</p>
            <p className="muted">Прогресс: {item.overallProgress}%</p>
            <p className="muted">
              {item.totalLessons > 0 ? `${item.completedLessons ?? 0} из ${item.totalLessons ?? 0} уроков завершено` : "Уроки пока не загружены"}
            </p>

            <div className="progress-track">
              <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, Number(item.overallProgress ?? 0)))}%` }} />
            </div>

            <div className="my-course-card__summary">
              <span>{Number(item.overallProgress) >= 100 ? "Курс завершён" : `Прогресс: ${item.overallProgress}%`}</span>
              <span>{item.totalLessons > 0 ? `${item.completedLessons ?? 0} из ${item.totalLessons ?? 0} уроков завершено` : "Уроки пока не загружены"}</span>
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
