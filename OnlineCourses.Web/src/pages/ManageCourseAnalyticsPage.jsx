import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { enrollmentsApi, formatApiError, reviewsApi } from "../lib/api";
import { formatCourseStatus } from "../lib/format";

export function ManageCourseAnalyticsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [students, setStudents] = useState([]);
  const [rating, setRating] = useState(null);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadData() {
      setError("");
      setIsLoading(true);
      try {
        const [studentsData, ratingData] = await Promise.all([
          enrollmentsApi.getByCourse(numericCourseId),
          reviewsApi.getRating(numericCourseId).catch(() => null)
        ]);

        if (active) {
          setStudents(studentsData ?? []);
          setRating(ratingData);
        }
      } catch (err) {
        if (active) {
          setError(formatApiError(err, "Не удалось загрузить аналитику."));
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    if (!Number.isFinite(numericCourseId)) {
      setError("Некорректный идентификатор курса.");
      setIsLoading(false);
      return;
    }

    loadData();
    return () => {
      active = false;
    };
  }, [numericCourseId]);

  const summary = useMemo(() => {
    const count = students.length;
    if (count === 0) {
      return {
        students: 0,
        avgProgress: 0,
        completed: 0,
        completionRate: 0
      };
    }

    const progressList = students.map((item) => Number(item.overallProgress ?? 0));
    const avgProgress = progressList.reduce((sum, value) => sum + value, 0) / count;
    const completed = progressList.filter((value) => value >= 100).length;

    return {
      students: count,
      avgProgress: avgProgress.toFixed(0),
      completed,
      completionRate: ((completed / count) * 100).toFixed(0)
    };
  }, [students]);

  if (isLoading) {
    return <div className="page-state">Загружаем аналитику...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Аналитика курса</h1>
            <p className="muted">Курс #{numericCourseId}</p>
          </div>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to="/manage/courses">
              Назад к курсам
            </Link>
            <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${numericCourseId}/students`}>
              Студенты курса
            </Link>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />

      <section className="feature-grid">
        <article className="panel">
          <h3>{summary.students}</h3>
          <p>Студентов</p>
        </article>
        <article className="panel">
          <h3>{summary.avgProgress}%</h3>
          <p>Средний прогресс</p>
        </article>
        <article className="panel">
          <h3>{summary.completed}</h3>
          <p>Завершили курс</p>
        </article>
        <article className="panel">
          <h3>{summary.completionRate}%</h3>
          <p>Доля завершения</p>
        </article>
        <article className="panel">
          <h3>{Number(rating?.averageRating ?? 0).toFixed(1)}</h3>
          <p>Средний рейтинг</p>
          <p className="muted">Отзывов: {rating?.totalReviews ?? 0}</p>
        </article>
      </section>

      <section className="panel panel--light">
        <h2>Распределение оценок</h2>
        <div className="chip-row">
          <span className="chip">5*: {rating?.ratingDistribution?.[5] ?? 0}</span>
          <span className="chip">4*: {rating?.ratingDistribution?.[4] ?? 0}</span>
          <span className="chip">3*: {rating?.ratingDistribution?.[3] ?? 0}</span>
          <span className="chip">2*: {rating?.ratingDistribution?.[2] ?? 0}</span>
          <span className="chip">1*: {rating?.ratingDistribution?.[1] ?? 0}</span>
        </div>
      </section>

      <section className="stack">
        {students.length === 0 ? (
          <div className="panel panel--light">По этому курсу пока нет данных студентов.</div>
        ) : (
          students.map((student) => (
            <article className="panel" key={student.enrollmentId}>
              <div className="panel-row">
                <strong>{student.userName}</strong>
                <span className="chip">{formatCourseStatus(student.status)}</span>
              </div>
              <p className="muted">Прогресс: {student.overallProgress ?? 0}%</p>
              <div className="progress-track">
                <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, Number(student.overallProgress ?? 0)))}%` }} />
              </div>
            </article>
          ))
        )}
      </section>
    </section>
  );
}
