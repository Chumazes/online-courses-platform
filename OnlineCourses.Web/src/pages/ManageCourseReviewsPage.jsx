import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { filesApi, formatApiError, reviewsApi } from "../lib/api";
import { formatDate } from "../lib/format";

export function ManageCourseReviewsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [reviews, setReviews] = useState([]);
  const [selectedReviewId, setSelectedReviewId] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isBusy, setIsBusy] = useState(false);

  async function loadData() {
    setError("");
    setIsLoading(true);
    try {
      const data = await reviewsApi.getModeration(numericCourseId);
      const items = data ?? [];
      setReviews(items);
      if (items.length > 0 && !items.some((item) => item.reviewId === selectedReviewId)) {
        setSelectedReviewId(items[0].reviewId);
      }
      if (items.length === 0) {
        setSelectedReviewId(null);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить отзывы на модерацию."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (!Number.isFinite(numericCourseId)) {
      setError("Некорректный идентификатор курса.");
      setIsLoading(false);
      return;
    }
    loadData();
  }, [numericCourseId]);

  const selectedReview = useMemo(
    () => reviews.find((review) => review.reviewId === selectedReviewId) ?? null,
    [reviews, selectedReviewId]
  );

  async function setApproval(approve) {
    if (!selectedReview) {
      return;
    }

    setError("");
    setSuccess("");
    setIsBusy(true);
    try {
      await reviewsApi.approve(selectedReview.reviewId, approve);
      setSuccess(approve ? "Отзыв опубликован." : "Отзыв отправлен на доработку.");
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось изменить статус отзыва."));
    } finally {
      setIsBusy(false);
    }
  }

  async function removeReview() {
    if (!selectedReview) {
      return;
    }

    if (!window.confirm("Удалить выбранный отзыв?")) {
      return;
    }

    setError("");
    setSuccess("");
    setIsBusy(true);
    try {
      await reviewsApi.remove(selectedReview.reviewId);
      setSuccess("Отзыв удалён.");
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить отзыв."));
    } finally {
      setIsBusy(false);
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем модерацию...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Модерация отзывов курса</h1>
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
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split">
        <div className="stack">
          {reviews.length === 0 ? <div className="panel panel--light">Отзывов на модерацию пока нет.</div> : null}
          {reviews.map((review) => (
            <article
              className={`panel clickable${selectedReviewId === review.reviewId ? " panel--selected" : ""}`}
              key={review.reviewId}
              onClick={() => setSelectedReviewId(review.reviewId)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  setSelectedReviewId(review.reviewId);
                }
              }}
              role="button"
              tabIndex={0}
            >
              <div className="panel-row">
                <strong>{review.userName}</strong>
                <span className={`chip ${review.isApproved ? "chip--ok" : "chip--warn"}`}>
                  {review.isApproved ? "Опубликован" : "На модерации"}
                </span>
              </div>
              <p className="muted">{formatDate(review.reviewDate)}</p>
              <p>{review.comment || "Без комментария."}</p>
            </article>
          ))}
        </div>

        <div className="panel">
          {selectedReview ? (
            <div className="stack">
              <h2>Карточка отзыва</h2>
              <div className="student-row">
                <img
                  alt={selectedReview.userName}
                  className="mini-avatar"
                  src={selectedReview.userAvatar ? filesApi.buildFileUrl(selectedReview.userAvatar) : "https://placehold.co/64x64?text=U"}
                />
                <div>
                  <h3>{selectedReview.userName}</h3>
                  <p className="muted">{formatDate(selectedReview.reviewDate)}</p>
                </div>
              </div>
              <div className="chip-row">
                <span className="chip">Оценка: {selectedReview.rating}/5</span>
                <span className={`chip ${selectedReview.isApproved ? "chip--ok" : "chip--warn"}`}>
                  {selectedReview.isApproved ? "Опубликован" : "На модерации"}
                </span>
              </div>
              <p>{selectedReview.comment || "Без комментария."}</p>

              <div className="card-actions">
                <button className="btn btn--primary btn--fit" disabled={isBusy} onClick={() => setApproval(true)} type="button">
                  Опубликовать
                </button>
                <button className="btn btn--ghost btn--fit" disabled={isBusy} onClick={() => setApproval(false)} type="button">
                  На доработку
                </button>
                <button className="btn btn--danger btn--fit" disabled={isBusy} onClick={removeReview} type="button">
                  Удалить
                </button>
              </div>
            </div>
          ) : (
            <p className="muted">Выбери отзыв слева в списке.</p>
          )}
        </div>
      </section>
    </section>
  );
}
