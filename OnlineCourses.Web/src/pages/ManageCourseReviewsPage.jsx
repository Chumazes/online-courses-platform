import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, filesApi, formatApiError, reviewsApi } from "../lib/api";
import { formatDate } from "../lib/format";

export function ManageCourseReviewsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [course, setCourse] = useState(null);
  const [rating, setRating] = useState(null);
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
      const [courseData, ratingData, reviewData] = await Promise.all([
        coursesApi.getById(numericCourseId),
        reviewsApi.getRating(numericCourseId).catch(() => null),
        reviewsApi.getModeration(numericCourseId)
      ]);

      const items = reviewData ?? [];
      setCourse(courseData);
      setRating(ratingData);
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
      <section className="panel management-hero">
        <div className="management-hero__copy">
          <h1>Отзывы курса</h1>
          <p className="management-hero__subtitle">{course?.title ?? `Курс #${numericCourseId}`}</p>
          <p className="management-hero__meta">
            {Number(rating?.totalReviews ?? 0) > 0
              ? `Средняя оценка ${Number(rating?.averageRating ?? 0).toFixed(1)} из 5`
              : "Пока нет отзывов"}
          </p>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split management-split">
        <div className="stack management-column">
          {reviews.length === 0 ? (
            <div className="panel panel--light management-empty">Отзывов на модерацию пока нет.</div>
          ) : null}

          {reviews.map((review) => (
            <article
              className={`panel management-card clickable${selectedReviewId === review.reviewId ? " management-card--selected" : ""}`}
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
                <div className="review-author">
                  <span className="review-avatar review-avatar--fallback">{(review.userName ?? "U").slice(0, 2).toUpperCase()}</span>
                  <div>
                    <strong>{review.userName}</strong>
                    <p className="muted">{formatDate(review.reviewDate)}</p>
                  </div>
                </div>

                <span className="chip">{review.isApproved ? "Опубликован" : "На модерации"}</span>
              </div>

              <div className="management-strip">
                <span>{review.rating}/5</span>
                <span>{review.isApproved ? "Публичный отзыв" : "Ожидает решения"}</span>
                <span>{formatDate(review.reviewDate)}</span>
              </div>

              <p className="muted">{review.comment || "Без комментария."}</p>
            </article>
          ))}
        </div>

        <div className="panel management-form">
          {selectedReview ? (
            <div className="stack">
              <h2>Карточка отзыва</h2>

              <article className="panel panel--inner management-card">
                <div className="review-author">
                  <img
                    alt={selectedReview.userName}
                    className="review-avatar"
                    src={selectedReview.userAvatar ? filesApi.buildFileUrl(selectedReview.userAvatar) : "https://placehold.co/64x64?text=U"}
                  />
                  <div>
                    <strong>{selectedReview.userName}</strong>
                    <p className="muted">{formatDate(selectedReview.reviewDate)}</p>
                  </div>
                </div>

                <div className="management-strip">
                  <span>{selectedReview.rating}/5</span>
                  <span>{selectedReview.isApproved ? "Опубликован" : "На модерации"}</span>
                  <span>{formatDate(selectedReview.reviewDate)}</span>
                </div>

                <div className="review-stars">{"★".repeat(Math.max(1, Number(selectedReview.rating ?? 0)))}</div>
                <p>{selectedReview.comment || "Без комментария."}</p>
              </article>

              <div className="card-actions management-form__actions">
                <button className="btn btn--primary btn--fit" disabled={isBusy} onClick={() => setApproval(true)} type="button">
                  Опубликовать
                </button>
                <button className="btn btn--ghost btn--fit" disabled={isBusy} onClick={() => setApproval(false)} type="button">
                  На доработку
                </button>
                <button className="btn btn--danger btn--fit" disabled={isBusy} onClick={removeReview} type="button">
                  Удалить отзыв
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
