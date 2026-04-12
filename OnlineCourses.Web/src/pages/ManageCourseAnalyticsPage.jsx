import { useEffect, useMemo, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, enrollmentsApi, filesApi, formatApiError, reviewsApi } from "../lib/api";
import { formatDate, formatLevel, formatMoney } from "../lib/format";

export function ManageCourseAnalyticsPage() {
  const numericCourseId = Number(window.location.pathname.split("/")[3]);
  const [course, setCourse] = useState(null);
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
        const [courseData, studentsData, ratingData] = await Promise.all([
          coursesApi.getById(numericCourseId),
          enrollmentsApi.getByCourse(numericCourseId),
          reviewsApi.getRating(numericCourseId).catch(() => null)
        ]);

        if (!active) {
          return;
        }

        setCourse(courseData);
        setStudents(studentsData ?? []);
        setRating(ratingData);
      } catch (err) {
        if (active) {
          setError(formatApiError(err, "Не удалось загрузить аналитику курса."));
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
      <section className="panel management-hero">
        <div className="management-hero__copy">
          <h1>Аналитика курса</h1>
          <p className="management-hero__subtitle">{course?.title ?? `Курс #${numericCourseId}`}</p>
          <p className="management-hero__meta">
            {formatLevel(course?.level)} • {formatMoney(course?.price ?? 0)} • {students.length} студентов
          </p>
        </div>
      </section>

      <ErrorBanner message={error} />

      <section className="feature-grid management-metrics">
        <article className="panel management-metric">
          <p className="muted">Студенты</p>
          <h3>{summary.students}</h3>
          <span>Все актуальные записи на курс</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Средний прогресс</p>
          <h3>{summary.avgProgress}%</h3>
          <span>По всем студентам на курсе</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Завершение</p>
          <h3>{summary.completionRate}%</h3>
          <span>Завершили: {summary.completed}</span>
        </article>
        <article className="panel management-metric">
          <p className="muted">Рейтинг</p>
          <h3>{Number(rating?.totalReviews ?? 0) > 0 ? Number(rating?.averageRating ?? 0).toFixed(1) : "Пока без оценок"}</h3>
          <span>{rating?.totalReviews ?? 0} отзыв(ов)</span>
        </article>
      </section>

      <section className="panel">
        <h2>Распределение оценок</h2>
        <div className="chip-row">
          <span className="chip">5*: {rating?.ratingDistribution?.[5] ?? 0}</span>
          <span className="chip">4*: {rating?.ratingDistribution?.[4] ?? 0}</span>
          <span className="chip">3*: {rating?.ratingDistribution?.[3] ?? 0}</span>
          <span className="chip">2*: {rating?.ratingDistribution?.[2] ?? 0}</span>
          <span className="chip">1*: {rating?.ratingDistribution?.[1] ?? 0}</span>
        </div>
      </section>

      <section className="panel">
        <h2>Студенты и прогресс</h2>
        <div className="stack">
          {students.length === 0 ? (
            <div className="panel panel--inner management-empty">По этому курсу пока нет данных студентов.</div>
          ) : (
            students.map((student) => (
              <article className="panel panel--inner management-card" key={student.enrollmentId}>
                <div className="student-row">
                  <img
                    alt={student.userName ?? "Student"}
                    className="mini-avatar"
                    src={student.userAvatarUrl ? filesApi.buildFileUrl(student.userAvatarUrl) : "https://placehold.co/64x64?text=U"}
                  />
                  <div>
                    <h3>{student.userName}</h3>
                    <p className="muted">Записан: {formatDate(student.enrollmentDate)}</p>
                    <p className="muted">Прогресс: {student.overallProgress ?? 0}%</p>
                  </div>
                  <span className="chip">{student.status || "active"}</span>
                </div>

                <div className="progress-track">
                  <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, Number(student.overallProgress ?? 0)))}%` }} />
                </div>
              </article>
            ))
          )}
        </div>
      </section>
    </section>
  );
}
