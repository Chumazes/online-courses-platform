import { useEffect, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, formatApiError, reviewsApi } from "../lib/api";
import { formatDate } from "../lib/format";

export function ModerationPage() {
  const [courses, setCourses] = useState([]);
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [reviews, setReviews] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    async function loadCourses() {
      setError("");
      setIsLoading(true);
      try {
        const response = await coursesApi.getAll({
          pageNumber: 1,
          pageSize: 100,
          all: true
        });
        const items = response?.items ?? [];
        setCourses(items);
        if (items.length > 0) {
          setSelectedCourseId(String(items[0].courseId));
        }
      } catch (err) {
        setError(formatApiError(err, "Не удалось загрузить курсы для модерации."));
      } finally {
        setIsLoading(false);
      }
    }

    loadCourses();
  }, []);

  useEffect(() => {
    async function loadReviews() {
      if (!selectedCourseId) {
        setReviews([]);
        return;
      }

      setError("");
      try {
        const data = await reviewsApi.getModeration(Number(selectedCourseId));
        setReviews(data ?? []);
      } catch (err) {
        setError(formatApiError(err, "Не удалось загрузить отзывы."));
      }
    }

    loadReviews();
  }, [selectedCourseId]);

  async function updateApproval(reviewId, approve) {
    setError("");
    setSuccess("");
    try {
      await reviewsApi.approve(reviewId, approve);
      setSuccess(approve ? "Отзыв одобрен." : "Отзыв отклонен.");
      const latest = await reviewsApi.getModeration(Number(selectedCourseId));
      setReviews(latest ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось обновить статус отзыва."));
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем модерацию...</div>;
  }

  return (
    <section className="stack">
      <h1>Модерация отзывов</h1>
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <label className="panel form">
        <span>Курс</span>
        <select className="input" onChange={(event) => setSelectedCourseId(event.target.value)} value={selectedCourseId}>
          {courses.map((course) => (
            <option key={course.courseId} value={course.courseId}>
              {course.title}
            </option>
          ))}
        </select>
      </label>

      {reviews.length === 0 ? <div className="panel muted">Для выбранного курса отзывов пока нет.</div> : null}

      {reviews.map((review) => (
        <article className="panel" key={review.reviewId}>
          <div className="panel-row">
            <div>
              <strong>{review.userName}</strong>
              <p className="muted">Оценка: {review.rating}</p>
            </div>
            <span className={`chip ${review.isApproved ? "chip--ok" : "chip--warn"}`}>
              {review.isApproved ? "Опубликован" : "На модерации"}
            </span>
          </div>
          <p>{review.comment || "Без комментария"}</p>
          <p className="muted">{formatDate(review.reviewDate)}</p>

          <div className="card-actions">
            <button className="btn btn--primary btn--fit" onClick={() => updateApproval(review.reviewId, true)} type="button">
              Одобрить
            </button>
            <button className="btn btn--danger btn--fit" onClick={() => updateApproval(review.reviewId, false)} type="button">
              Отклонить
            </button>
          </div>
        </article>
      ))}
    </section>
  );
}

