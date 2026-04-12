import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, enrollmentsApi, filesApi, formatApiError, lessonsApi, progressApi, reviewsApi, sectionsApi } from "../lib/api";
import { formatDate, formatLevel, formatMoney } from "../lib/format";

function getInitials(name) {
  if (!name) {
    return "?";
  }

  return String(name)
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

function getRatingTitle(rating) {
  if (!rating?.totalReviews) {
    return "Пока нет оценок";
  }

  return `${Number(rating.averageRating ?? 0).toFixed(1)} / 5`;
}

function getReviewHelper(myReview, isEnrolled) {
  if (!isEnrolled) {
    return "Отзывы доступны только студентам, которые записаны на курс.";
  }

  if (!myReview) {
    return "Поделитесь впечатлением о курсе.";
  }

  if (myReview.isApproved) {
    return "Ваш отзыв опубликован и виден другим пользователям.";
  }

  return "Ваш отзыв отправлен и ждёт модерации.";
}

function getReviewFooter(myReview) {
  if (!myReview) {
    return "";
  }

  return myReview.isApproved
    ? "Отзыв уже опубликован. Можешь обновить текст или оценку."
    : "Отзыв отправлен. После модерации он появится в общем списке.";
}

export function CourseDetailsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const { isAuthenticated, role } = useAuth();

  const [course, setCourse] = useState(null);
  const [sections, setSections] = useState([]);
  const [lessonsBySection, setLessonsBySection] = useState({});
  const [rating, setRating] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [myReview, setMyReview] = useState(null);
  const [isEnrolled, setIsEnrolled] = useState(false);
  const [progress, setProgress] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [infoMessage, setInfoMessage] = useState("");
  const [isEnrollmentPending, setIsEnrollmentPending] = useState(false);
  const [isReviewPending, setIsReviewPending] = useState(false);
  const [reviewForm, setReviewForm] = useState({
    rating: 5,
    comment: ""
  });

  async function loadReviewsState() {
    const [ratingData, reviewData] = await Promise.all([reviewsApi.getRating(numericCourseId), reviewsApi.getByCourse(numericCourseId)]);
    setRating(ratingData);
    setReviews(reviewData ?? []);
  }

  useEffect(() => {
    let cancelled = false;

    async function loadPage() {
      setError("");
      setInfoMessage("");
      setIsLoading(true);

      try {
        const [courseData, sectionData] = await Promise.all([
          coursesApi.getById(numericCourseId),
          sectionsApi.getByCourseId(numericCourseId)
        ]);

        const lessonsPairs = await Promise.all(
          (sectionData ?? []).map(async (section) => {
            const lessons = await lessonsApi.getBySectionId(section.sectionId);
            return [section.sectionId, lessons ?? []];
          })
        );

        const [ratingData, reviewData] = await Promise.all([reviewsApi.getRating(numericCourseId), reviewsApi.getByCourse(numericCourseId)]);

        let enrolled = false;
        let ownReview = null;
        let progressData = null;

        if (isAuthenticated && role === "student") {
          const [enrollments, myReviews] = await Promise.all([enrollmentsApi.getMy(), reviewsApi.getMy()]);
          enrolled = (enrollments ?? []).some((item) => item.courseId === numericCourseId && item.status !== "expired");
          ownReview = (myReviews ?? []).find((item) => item.courseId === numericCourseId) ?? null;

          if (enrolled) {
            try {
              progressData = await progressApi.getCourse(numericCourseId);
            } catch {
              progressData = null;
            }
          }
        }

        if (!cancelled) {
          setCourse(courseData);
          setSections(sectionData ?? []);
          setLessonsBySection(Object.fromEntries(lessonsPairs));
          setRating(ratingData);
          setReviews(reviewData ?? []);
          setIsEnrolled(enrolled);
          setProgress(progressData);
          setMyReview(ownReview);
          setReviewForm({
            rating: ownReview?.rating ?? 5,
            comment: ownReview?.comment ?? ""
          });
        }
      } catch (err) {
        if (!cancelled) {
          setError(formatApiError(err, "Не удалось загрузить страницу курса."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    if (!Number.isFinite(numericCourseId)) {
      setError("Некорректный идентификатор курса.");
      setIsLoading(false);
      return;
    }

    loadPage();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, numericCourseId, role]);

  async function handleToggleEnrollment() {
    setError("");
    setInfoMessage("");
    setIsEnrollmentPending(true);

    try {
      if (isEnrolled) {
        await enrollmentsApi.unenroll(numericCourseId);
        setIsEnrolled(false);
        setProgress(null);
        setInfoMessage(`Вы отписались от курса "${course?.title ?? ""}".`);
      } else {
        await enrollmentsApi.enroll(numericCourseId);
        setIsEnrolled(true);
        try {
          const progressData = await progressApi.getCourse(numericCourseId);
          setProgress(progressData);
        } catch {
          setProgress(null);
        }
        setInfoMessage(`Вы записаны на курс "${course?.title ?? ""}".`);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось обновить запись на курс."));
    } finally {
      setIsEnrollmentPending(false);
    }
  }

  async function handleSaveReview(event) {
    event.preventDefault();
    setError("");
    setInfoMessage("");
    setIsReviewPending(true);

    try {
      if (myReview) {
        await reviewsApi.update(myReview.reviewId, reviewForm);
      } else {
        await reviewsApi.create(numericCourseId, reviewForm);
      }

      const myReviews = await reviewsApi.getMy();
      const ownReview = (myReviews ?? []).find((item) => item.courseId === numericCourseId) ?? null;
      setMyReview(ownReview);
      await loadReviewsState();
      setInfoMessage(myReview ? "Отзыв обновлён." : "Отзыв отправлен на модерацию.");
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
    setInfoMessage("");
    setIsReviewPending(true);

    try {
      await reviewsApi.remove(myReview.reviewId);
      setMyReview(null);
      setReviewForm({ rating: 5, comment: "" });
      await loadReviewsState();
      setInfoMessage("Отзыв удалён.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить отзыв."));
    } finally {
      setIsReviewPending(false);
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем курс...</div>;
  }

  if (!course) {
    return <div className="page-state">Курс не найден.</div>;
  }

  return (
    <section className="stack">
      <section className="panel course-detail-hero">
        <div className="course-detail-hero__main">
          <h1>{course.title}</h1>
          <p className="muted course-detail-hero__description">{course.description}</p>

          <div className="chip-row">
            <span className="chip">{course.categoryName ?? "Без категории"}</span>
            <span className="chip">{formatLevel(course.level)}</span>
            <span className="chip chip--price">{course.price > 0 ? formatMoney(course.price) : "Бесплатно"}</span>
          </div>

          <p className="course-detail-hero__meta">Автор: {course.authorName ?? "Команда LLT"}</p>

          {isAuthenticated && role === "student" ? (
            <div className="card-actions">
              <button className="btn btn--primary" disabled={isEnrollmentPending} onClick={handleToggleEnrollment} type="button">
                {isEnrollmentPending ? "Сохраняем..." : isEnrolled ? "Вы записаны" : "Записаться на курс"}
              </button>
            </div>
          ) : null}
        </div>

        <aside className="course-detail-hero__side">
          <div className="course-progress-card">
            <h3>Прогресс</h3>
            <div className="progress-track">
              <div className="progress-value" style={{ width: `${Math.max(0, Math.min(100, Number(progress?.overallProgressPercentage ?? 0)))}%` }} />
            </div>
            <p>Общий прогресс: {Math.round(Number(progress?.overallProgressPercentage ?? 0))}%</p>
            <span>Low-Level to Top</span>
          </div>
        </aside>
      </section>

      {infoMessage ? <div className="success-banner">{infoMessage}</div> : null}
      <ErrorBanner message={error} />

      <section className="panel panel--light">
        <h2>Программа курса</h2>
        <p className="muted">Реальные разделы и уроки, которые уже пришли из API и доступны внутри web-клиента.</p>

        {sections.length === 0 ? (
          <div className="panel course-empty-state">Разделы пока не добавлены.</div>
        ) : (
          <div className="course-sections">
            {sections.map((section) => {
              const sectionLessons = lessonsBySection[section.sectionId] ?? [];

              return (
                <article className="panel section-card" key={section.sectionId}>
                  <div className="panel-row">
                    <h3>{section.title}</h3>
                    <span className="chip">{sectionLessons.length} уроков</span>
                  </div>

                  <p className="muted">{section.description || "Вводная часть по разделу и интерфейсу."}</p>

                  <div className="list">
                    {sectionLessons.map((lesson) => (
                      <Link className="list-item lesson-link" key={lesson.lessonId} to={`/lessons/${section.sectionId}/${lesson.lessonId}`}>
                        <div>
                          <strong>{lesson.title}</strong>
                          <p className="muted">
                            {lesson.lessonType} • {lesson.durationMinutes ? `${lesson.durationMinutes} мин` : "без длительности"} •{" "}
                            {lesson.isFree ? "бесплатный" : "по подписке"}
                          </p>
                          <span className="lesson-link__hint">{lesson.content || "Открой урок, чтобы посмотреть содержание."}</span>
                        </div>
                        <span className="btn btn--primary btn--fit">Открыть</span>
                      </Link>
                    ))}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="panel panel--light reviews-summary">
        <div>
          <h2>Отзывы и рейтинг</h2>
          <p className="muted">
            {rating?.totalReviews ? `${rating.totalReviews} отзыв(ов) опубликовано` : "Станьте первым, кто оставит отзыв."}
          </p>
        </div>

        <div className="rating-box">
          <strong>{getRatingTitle(rating)}</strong>
          <span>Всего отзывов: {rating?.totalReviews ?? 0}</span>
        </div>
      </section>

      {isAuthenticated && role === "student" ? (
        <section className="panel review-editor">
          <h3>Ваш отзыв</h3>
          <p className="muted">{getReviewHelper(myReview, isEnrolled)}</p>

          <form className="form review-editor__form" onSubmit={handleSaveReview}>
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

            <div className="card-actions">
              <button className="btn btn--primary" disabled={!isEnrolled || isReviewPending} type="submit">
                {isReviewPending ? "Сохраняем..." : myReview ? "Сохранить отзыв" : "Оставить отзыв"}
              </button>

              {myReview ? (
                <button className="btn btn--danger" disabled={isReviewPending} onClick={handleDeleteReview} type="button">
                  Удалить отзыв
                </button>
              ) : null}
            </div>

            {getReviewFooter(myReview) ? <p className="review-editor__note">{getReviewFooter(myReview)}</p> : null}
          </form>
        </section>
      ) : null}

      <section className="review-list">
        {reviews.map((review) => (
          <article className="panel panel--light public-review-card" key={review.reviewId}>
            <div className="public-review-card__head">
              <div className="review-author">
                {review.userAvatarUrl ? (
                  <img alt={review.userName} className="review-avatar" src={filesApi.buildFileUrl(review.userAvatarUrl)} />
                ) : (
                  <div className="review-avatar review-avatar--fallback">{getInitials(review.userName)}</div>
                )}

                <div>
                  <strong>{review.userName}</strong>
                  <p className="muted">{formatDate(review.reviewDate)}</p>
                </div>
              </div>

              <span className="review-stars">{"★".repeat(Math.max(0, Number(review.rating ?? 0)))}</span>
            </div>

            <p className="public-review-card__comment">{review.comment}</p>
          </article>
        ))}
      </section>
    </section>
  );
}
