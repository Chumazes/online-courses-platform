import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, enrollmentsApi, filesApi, formatApiError } from "../lib/api";
import { formatDate, formatLevel, formatMoney } from "../lib/format";

export function ManageCourseStudentsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [course, setCourse] = useState(null);
  const [students, setStudents] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadData() {
      setError("");
      setIsLoading(true);

      try {
        const [courseData, studentsData] = await Promise.all([coursesApi.getById(numericCourseId), enrollmentsApi.getByCourse(numericCourseId)]);

        if (!active) {
          return;
        }

        setCourse(courseData);
        setStudents(studentsData ?? []);
      } catch (err) {
        if (active) {
          setError(formatApiError(err, "Не удалось загрузить студентов курса."));
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

  const completedCount = useMemo(
    () => students.filter((student) => Number(student.overallProgress ?? 0) >= 100).length,
    [students]
  );

  if (isLoading) {
    return <div className="page-state">Загружаем студентов...</div>;
  }

  return (
    <section className="stack">
      <section className="panel management-hero">
        <div className="management-hero__copy">
          <h1>Студенты курса</h1>
          <p className="management-hero__subtitle">{course?.title ?? `Курс #${numericCourseId}`}</p>
          <p className="management-hero__meta">
            {formatLevel(course?.level)} • {formatMoney(course?.price ?? 0)} • {students.length} студентов
          </p>
        </div>

        <div className="management-summary">
          <article className="management-summary__card">
            <strong>{students.length}</strong>
            <span>Активных записей</span>
          </article>
          <article className="management-summary__card">
            <strong>{completedCount}</strong>
            <span>Завершили курс</span>
          </article>
        </div>
      </section>

      <ErrorBanner message={error} />

      {students.length === 0 ? (
        <div className="panel panel--light management-empty">В этом курсе пока нет активных студентов.</div>
      ) : (
        <section className="stack">
          {students.map((student) => (
            <article className="panel management-card" key={student.enrollmentId}>
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
          ))}
        </section>
      )}
    </section>
  );
}
