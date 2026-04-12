import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import {
  coursesApi,
  enrollmentsApi,
  filesApi,
  formatApiError,
  lessonsApi,
  progressApi,
  reviewsApi,
  sectionsApi
} from "../lib/api";
import { formatDate, formatLevel, formatMoney } from "../lib/format";

function getInitials(name) {
  return String(name ?? "U")
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function CourseDetailsPage() {
  const { courseId } = useParams();
  const { isAuthenticated, role } = useAuth();
  const numericCourseId = Number(courseId);

  const [course, setCourse] = useState(null);
  const [sections, setSections] = useState([]);
  const [lessonsBySection, setLessonsBySection] = useState({});
  const [progress, setProgress] = useState(null);
  const [rating, setRating] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [myReview, setMyReview] = useState(null);
  const [reviewForm, setReviewForm] = useState({ rating: 5, comment: "" });
  const [isEnrolled, setIsEnrolled] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isEnrollmentPending, setIsEnrollmentPending] = useState(false);
  const [isReviewPending, setIsReviewPending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const visibleReviews = useMemo(() => reviews.filter((item) => item.isApproved), [reviews]);
  const totalLessons = Number(progress?.totalLessons ?? 0);
  const completedLessons = Number(progress?.completedLessons ?? 0);
  const overallProgress = Number(progress?.overallProgress ?? 0);

  useEffect(() => {
    let active = true;

    async function loadData() {
      setError("");
      setSuccess("");
      setIsLoading(true);

      try {
        const [courseData, sectionData, ratingData, reviewData] = await Promise.all([
          coursesApi.getById(numericCourseId),
          sectionsApi.getByCourseId(numericCourseId),
          reviewsApi.getRating(numericCourseId).catch(() => null),
          reviewsApi.getByCourse(numericCourseId).catch(() => [])
        ]);

        if (!active) {
          return;
        }

        setCourse(courseData);
        setSections(sectionData ?? []);
        setRating(ratingData);
        setReviews(reviewData ?? []);

        const lessonEntries = await Promise.all(
          (sectionData ?? []).map(async (section) => {
            try {
              const lessons = await lessonsApi.getBySectionId(section.sectionId);
              return [section.sectionId, lessons ?? []];
            } catch {
              return [section.sectionId, []];
            }
          })
        );

        if (!active) {
          return;
        }

        setLessonsBySection(Object.fromEntries(lessonEntries));

        if (isAuthenticated && role === "student") {
          const [enrollments, myReviews, courseProgress] = await Promise.all([
            enrollmentsApi.getMy().catch(() => []),
            reviewsApi.getMy().catch(() => []),
            progressApi.getCourse(numericCourseId).catch(() => null)
          ]);

          if (!active) {
            return;
          }

          const enrollment = (enrollments ?? []).find(
            (item) => item.courseId === numericCourseId && String(item.status ?? "").toLowerCase() !== "expired"
          );
          const ownReview = (myReviews ?? []).find((item) => item.courseId === numericCourseId) ?? null;

          setIsEnrolled(Boolean(enrollment));
          setProgress(courseProgress);
          setMyReview(ownReview);
          setReviewForm({
            rating: ownReview?.rating ?? 5,
            comment: ownReview?.comment ?? ""
          });
        } else {
          setIsEnrolled(false);
          setProgress(null);
          setMyReview(null);
        }
      } catch (err) {
        if (active) {
          setError(formatApiError(err, "Не удалось загрузить страницу курса."));
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
  }, [isAuthenticated, numericCourseId, role]);

  async function handleEnroll() {
    if (role !== "student" || isEnrolled) {
      return;
    }

    setError("");
    setSuccess("");
    setIsEnrollmentPending(true);

    try {
      await enrollmentsApi.enroll(numericCourseId);
      setSuccess(`Вы записаны на курс "${course?.title ?? "курс"}".`);

      const [courseProgress, enrollments] = await Promise.all([
        progressApi.getCourse(numericCourseId).catch(() => null),
        enrollmentsApi.getMy().catch(() => [])
      ]);

      const enrollment = (enrollments ?? []).find(
        (item) => item.courseId === numericCourseId && String(item.status ?? "").toLowerCase() !== "expired"
      );

      setIsEnrolled(Boolean(enrollment));
      setProgress(courseProgress);
    } catch (err) {
      setError(formatApiError(err, "Не удалось записаться на курс."));
    } finally {
      setIsEnrollmentPending(false);
    }
  }

  async function handleSaveReview(event) {
    event.preventDefault();

    if (!isEnrolled) {
      setError("Оставить отзыв можно только после записи на курс.");
      return;
    }

    setError("");
    setSuccess("");
    setIsReviewPending(true);

    try {
      if (myReview) {
        await reviewsApi.update(myReview.reviewId, reviewForm);
        setSuccess("Отзыв обновлён.");
      } else {
        await reviewsApi.create(numericCourseId, reviewForm);
        setSuccess("Отзыв отправлен на модерацию.");
      }

      const [ratingData, reviewData, myReviews] = await Promise.all([
        reviewsApi.getRating(numericCourseId).catch(() => null),
        reviewsApi.getByCourse(numericCourseId).catch(() => []),
        reviewsApi.getMy().catch(() => [])
      ]);

      setRating(ratingData);
      setReviews(reviewData ?? []);
      const ownReview = (myReviews ?? []).find((item) => item.courseId === numericCourseId) ?? null;
      setMyReview(ownReview);
      setReviewForm({
        rating: ownReview?.rating ?? reviewForm.rating,
        comment: ownReview?.comment ?? reviewForm.comment
      });
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить отзыв."));
    } finally {
      setIsReviewPending(false);
    }
  }

  async function handleDeleteReview() {
    if (!myReview) {
      return;
    }

    setError("");
    setSuccess("");
    setIsReviewPending(true);

    try {
      await reviewsApi.remove(myReview.reviewId);
      setMyReview(null);
      setReviewForm({ rating: 5, comment: "" });
      setSuccess("Отзыв удалён.");

      const [ratingData, reviewData] = await Promise.all([
        reviewsApi.getRating(numericCourseId).catch(() => null),
        reviewsApi.getByCourse(numericCourseId).catch(() => [])
      ]);

      setRating(ratingData);
      setReviews(reviewData ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить отзыв."));
    } finally {
      setIsReviewPending(false);
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем курс...</div>;
  }

  return (
    <section className="stack">
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="panel course-detail-hero">
        <div className="course-detail-hero__main">
          <h1>{course?.title}</h1>
          <p className="course-detail-hero__description">{course?.description}</p>

          <div className="chip-row">
            <span className="chip">{course?.categoryName || "Без категории"}</span>
            <span className="chip">{formatLevel(course?.level)}</span>
            <span className="chip chip--price">{formatMoney(course?.price ?? 0)}</span>
          </div>

          <p className="course-detail-hero__meta">Автор: {course?.teacherName ?? "Low-Level to Top"}</p>

          {role === "student" ? (
            <button
              className={`btn ${isEnrolled ? "btn--chrome" : "btn--primary"} btn--fit`}
              disabled={isEnrolled || isEnrollmentPending}
              onClick={handleEnroll}
              type="button"
            >
              {isEnrollmentPending ? "Записываем..." : isEnrolled ? "Вы записаны" : "Записаться на курс"}
            </button>
          ) : null}
        </div>

        <aside className="course-detail-hero__side">
          <div className="course-progress-card">
            <h3>Прогресс</h3>
            <div className="progress-track">
              <div className="progress-value" style={{ width: `${Math.min(100, Math.max(0, overallProgress))}%` }} />
            </div>
            <p>Общий прогресс: {overallProgress}%</p>
            <p>{totalLessons > 0 ? `${completedLessons} из ${totalLessons} уроков завершено` : "Уроки пока не загружены"}</p>
            <span>Low-Level to Top</span>
          </div>
        </aside>
      </section>

      <section className="panel">
        <h2>Программа курса</h2>
        <p className="muted">Реальные разделы и уроки, которые уже пришли из API и доступны внутри web-клиента.</p>

        {sections.length === 0 ? <div className="panel panel--light course-empty-state">Разделы пока не добавлены.</div> : null}

        <div className="course-sections">
          {sections.map((section) => {
            const lessons = lessonsBySection[section.sectionId] ?? [];

            return (
              <article className="panel section-card" key={section.sectionId}>
                <div className="panel-row">
                  <div>
                    <h3>{section.title}</h3>
                    <p className="muted">{section.description}</p>
                  </div>
                  <span className="chip">{lessons.length} уроков</span>
                </div>

                {lessons.length > 0 ? (
                  <div className="stack">
                    {lessons.map((lesson) => (
                      <div className="list-item lesson-link" key={lesson.lessonId}>
                        <div>
                          <strong>{lesson.title}</strong>
                          <p className="muted">
                            {lesson.lessonType} • {lesson.durationMinutes ? `${lesson.durationMinutes} мин` : "без длительности"} •{" "}
                            {lesson.isFree ? "бесплатный" : "платный"}
                          </p>
                          <span className="lesson-link__hint">{lesson.content}</span>
                        </div>
                        <Link className="btn btn--primary btn--fit" to={`/lessons/${lesson.lessonId}`}>
                          Открыть
                        </Link>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="panel panel--inner">В этом разделе пока нет уроков.</div>
                )}
              </article>
            );
          })}
        </div>
      </section>

      <section className="panel">
        <div className="reviews-summary">
          <div>
            <h2>Отзывы и рейтинг</h2>
            <p className="muted">{visibleReviews.length} отзыв(ов) опубликовано</p>
          </div>
          <div className="rating-box">
            <strong>{Number(rating?.totalReviews ?? 0) > 0 ? Number(rating?.averageRating ?? 0).toFixed(1) : "Пока нет оценок"}</strong>
            <span>Всего отзывов: {rating?.totalReviews ?? 0}</span>
          </div>
        </div>
      </section>

      {isAuthenticated && role === "student" ? (
        <form className="panel review-editor" onSubmit={handleSaveReview}>
          <h2>Ваш отзыв</h2>
          <p className="review-editor__note">
            {!isEnrolled
              ? "Оставить отзыв можно после записи на курс."
              : myReview
                ? myReview.isApproved
                  ? "Ваш отзыв уже опубликован. Здесь можно обновить его или удалить."
                  : "Ваш отзыв отправлен на модерацию. Здесь можно обновить его или удалить."
                : "Поделитесь впечатлением о курсе."}
          </p>

          <div className="grid-2 review-editor__form">
            <label className="label">
              Оценка
              <select
                className="input"
                disabled={!isEnrolled || isReviewPending}
                onChange={(event) => setReviewForm((current) => ({ ...current, rating: Number(event.target.value) }))}
                value={reviewForm.rating}
              >
                <option value={5}>5</option>
                <option value={4}>4</option>
                <option value={3}>3</option>
                <option value={2}>2</option>
                <option value={1}>1</option>
              </select>
            </label>

            <label className="label review-editor__comment">
              Комментарий
              <textarea
                className="input"
                disabled={!isEnrolled || isReviewPending}
                onChange={(event) => setReviewForm((current) => ({ ...current, comment: event.target.value }))}
                rows={5}
                value={reviewForm.comment}
              />
            </label>
          </div>

          <div className="card-actions">
            <button className="btn btn--primary btn--fit" disabled={!isEnrolled || isReviewPending} type="submit">
              {isReviewPending ? "Сохраняем..." : myReview ? "Сохранить отзыв" : "Оставить отзыв"}
            </button>
            {myReview ? (
              <button className="btn btn--danger btn--fit" disabled={isReviewPending} onClick={handleDeleteReview} type="button">
                Удалить отзыв
              </button>
            ) : null}
          </div>
        </form>
      ) : null}

      <section className="review-list">
        {visibleReviews.map((review) => (
          <article className="panel panel--light public-review-card" key={review.reviewId}>
            <div className="public-review-card__head">
              <div className="review-author">
                {review.userAvatar ? (
                  <img alt={review.userName} className="review-avatar" src={filesApi.buildFileUrl(review.userAvatar)} />
                ) : (
                  <div className="review-avatar review-avatar--fallback">{getInitials(review.userName)}</div>
                )}
                <div>
                  <strong>{review.userName}</strong>
                  <p className="muted">{formatDate(review.reviewDate)}</p>
                </div>
              </div>
              <div className="review-stars">{"★".repeat(Math.max(1, Number(review.rating ?? 0)))}</div>
            </div>

            <p className="public-review-card__comment">{review.comment}</p>
          </article>
        ))}
      </section>
    </section>
  );
}
