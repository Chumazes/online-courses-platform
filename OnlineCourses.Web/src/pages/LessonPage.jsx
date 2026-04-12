import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { filesApi, formatApiError, lessonsApi, progressApi } from "../lib/api";

export function LessonPage() {
  const { sectionId, lessonId } = useParams();
  const numericSectionId = Number(sectionId);
  const numericLessonId = Number(lessonId);
  const { isAuthenticated, role } = useAuth();

  const [lesson, setLesson] = useState(null);
  const [progress, setProgress] = useState(null);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isProgressPending, setIsProgressPending] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadPage() {
      setError("");
      setIsLoading(true);
      try {
        const lessonData = await lessonsApi.getById(numericSectionId, numericLessonId);
        let progressData = null;

        if (isAuthenticated && role === "student") {
          progressData = await progressApi.getLesson(numericLessonId).catch(() => null);
        }

        if (!cancelled) {
          setLesson(lessonData);
          setProgress(progressData);
        }
      } catch (err) {
        if (!cancelled) {
          setError(formatApiError(err, "Не удалось загрузить урок."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadPage();
    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, numericLessonId, numericSectionId, role]);

  async function markCompleted() {
    setError("");
    setIsProgressPending(true);

    try {
      await progressApi.update({
        lessonId: numericLessonId,
        isCompleted: true,
        watchTime: Math.max(progress?.watchTime ?? 0, 300)
      });

      const latest = await progressApi.getLesson(numericLessonId);
      setProgress(latest);
    } catch (err) {
      setError(formatApiError(err, "Не удалось обновить прогресс."));
    } finally {
      setIsProgressPending(false);
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем урок...</div>;
  }

  if (!lesson) {
    return <div className="page-state">Урок не найден.</div>;
  }

  return (
    <section className="stack">
      <Link className="btn btn--ghost btn--fit" to="/courses">
        Назад в каталог
      </Link>

      <article className="panel">
        <h1>{lesson.title}</h1>
        <p className="muted">{lesson.sectionTitle}</p>
        <p>{lesson.content || "Текст урока отсутствует."}</p>
      </article>

      <ErrorBanner message={error} />

      {lesson.videoUrl && (
        <article className="panel">
          <h3>Видео</h3>
          <a href={lesson.videoUrl} rel="noreferrer" target="_blank">
            Открыть видео
          </a>
        </article>
      )}

      {lesson.fileUrl && (
        <article className="panel">
          <h3>Файл урока</h3>
          <div className="card-actions">
            <a className="btn btn--ghost" href={filesApi.buildFileUrl(lesson.fileUrl)} rel="noreferrer" target="_blank">
              Открыть файл
            </a>
            <a className="btn btn--primary" href={filesApi.buildDownloadUrl(lesson.fileUrl)} rel="noreferrer" target="_blank">
              Скачать файл
            </a>
          </div>
        </article>
      )}

      {isAuthenticated && role === "student" && (
        <article className="panel">
          <h3>Прогресс</h3>
          <p className="muted">
            Статус: {progress?.isCompleted ? "Завершено" : "Не завершено"} | Просмотрено секунд: {progress?.watchTime ?? 0}
          </p>
          <button className="btn btn--primary" disabled={isProgressPending || progress?.isCompleted} onClick={markCompleted} type="button">
            {progress?.isCompleted ? "Урок уже завершен" : isProgressPending ? "Сохраняем..." : "Отметить завершенным"}
          </button>
        </article>
      )}
    </section>
  );
}
