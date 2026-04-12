import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { enrollmentsApi, filesApi, formatApiError } from "../lib/api";
import { formatCourseStatus, formatDate } from "../lib/format";

export function ManageCourseStudentsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [students, setStudents] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadData() {
      setError("");
      setIsLoading(true);
      try {
        const data = await enrollmentsApi.getByCourse(numericCourseId);
        if (active) {
          setStudents(data ?? []);
        }
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

  if (isLoading) {
    return <div className="page-state">Загружаем студентов...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Студенты курса</h1>
            <p className="muted">Курс #{numericCourseId}</p>
          </div>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to="/manage/courses">
              Назад к курсам
            </Link>
            <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${numericCourseId}/analytics`}>
              Аналитика курса
            </Link>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />

      {students.length === 0 ? <div className="panel panel--light">В этом курсе пока нет активных студентов.</div> : null}

      <section className="stack">
        {students.map((student) => (
          <article className="panel" key={student.enrollmentId}>
            <div className="student-row">
              <img
                alt={student.userName ?? "Student"}
                className="mini-avatar"
                src={student.userAvatarUrl ? filesApi.buildFileUrl(student.userAvatarUrl) : "https://placehold.co/64x64?text=U"}
              />
              <div>
                <h3>{student.userName}</h3>
                <p className="muted">Запись: {formatDate(student.enrollmentDate)}</p>
                <p className="muted">Статус: {formatCourseStatus(student.status)}</p>
              </div>
              <span className="chip">Прогресс: {student.overallProgress ?? 0}%</span>
            </div>
            <div className="progress-track">
              <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, Number(student.overallProgress ?? 0)))}%` }} />
            </div>
          </article>
        ))}
      </section>
    </section>
  );
}
