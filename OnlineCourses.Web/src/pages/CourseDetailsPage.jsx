import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, enrollmentsApi, formatApiError, lessonsApi, reviewsApi, sectionsApi } from "../lib/api";
import { formatDate, formatMoney } from "../lib/format";

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
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [isEnrollmentPending, setIsEnrollmentPending] = useState(false);
  const [isReviewPending, setIsReviewPending] = useState(false);
  const [reviewForm, setReviewForm] = useState({
    rating: 5,
    comment: ""
  });

  async function loadReviewsState() {
    const [ratingData, listData] = await Promise.all([reviewsApi.getRating(numericCourseId), reviewsApi.getByCourse(numericCourseId)]);
    setRating(ratingData);
    setReviews(listData ?? []);
  }

  useEffect(() => {
    let cancelled = false;

    async function loadPage() {
      setError("");
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

        const lessonsMap = Object.fromEntries(lessonsPairs);
        const [ratingData, reviewData] = await Promise.all([
          reviewsApi.getRating(numericCourseId),
          reviewsApi.getByCourse(numericCourseId)
        ]);

        let enrolled = false;
        let ownReview = null;

        if (isAuthenticated && role === "student") {
          const [enrollments, myReviews] = await Promise.all([enrollmentsApi.getMy(), reviewsApi.getMy()]);
          enrolled = (enrollments ?? []).some((item) => item.courseId === numericCourseId && item.status !== "expired");
          ownReview = (myReviews ?? []).find((item) => item.courseId === numericCourseId) ?? null;
        }

        if (!cancelled) {
          setCourse(courseData);
          setSections(sectionData ?? []);
          setLessonsBySection(lessonsMap);
          setRating(ratingData);
          setReviews(reviewData ?? []);
          setIsEnrolled(enrolled);
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
    setIsEnrollmentPending(true);

    try {
      if (isEnrolled) {
        await enrollmentsApi.unenroll(numericCourseId);
        setIsEnrolled(false);
      } else {
        await enrollmentsApi.enroll(numericCourseId);
        setIsEnrolled(true);
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
    setIsReviewPending(true);

    try {
      await reviewsApi.remove(myReview.reviewId);
      setMyReview(null);
      setReviewForm({
        rating: 5,
        comment: ""
      });
      await loadReviewsState();
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
      <div className="panel">
        <p className="chip">{course.level}</p>
        <h1>{course.title}</h1>
        <p className="muted">{course.description}</p>
        <p className="price">{formatMoney(course.price)}</p>
        <p className="muted">
          Студентов: {course.totalStudents} | Рейтинг: {rating?.averageRating ?? 0} ({rating?.totalReviews ?? 0})
        </p>

        {isAuthenticated && role === "student" && (
          <button className="btn btn--primary" disabled={isEnrollmentPending} onClick={handleToggleEnrollment} type="button">
            {isEnrollmentPending ? "Сохраняем..." : isEnrolled ? "Отписаться" : "Записаться"}
          </button>
        )}
      </div>

      <ErrorBanner message={error} />

      <div className="stack">
        <h2>Программа курса</h2>
        {sections.length === 0 ? (
          <div className="panel muted">Разделы пока не добавлены.</div>
        ) : (
          sections.map((section) => (
            <article className="panel" key={section.sectionId}>
              <h3>
                {section.sectionOrder}. {section.title}
              </h3>
              <p className="muted">{section.description || "Без описания"}</p>

              <div className="list">
                {(lessonsBySection[section.sectionId] ?? []).map((lesson) => (
                  <Link className="list-item" key={lesson.lessonId} to={`/lessons/${section.sectionId}/${lesson.lessonId}`}>
                    <span>
                      {lesson.lessonOrder}. {lesson.title}
                    </span>
                    <span className="muted">{lesson.durationMinutes ? `${lesson.durationMinutes} мин` : "—"}</span>
                  </Link>
                ))}
              </div>
            </article>
          ))
        )}
      </div>

      <div className="stack">
        <h2>Отзывы</h2>
        {reviews.length === 0 ? <div className="panel muted">Отзывов пока нет.</div> : null}
        {reviews.map((review) => (
          <article className="panel" key={review.reviewId}>
            <div className="panel-row">
              <strong>{review.userName}</strong>
              <span className="chip">★ {review.rating}</span>
            </div>
            <p>{review.comment || "Без комментария"}</p>
            <p className="muted">{formatDate(review.reviewDate)}</p>
          </article>
        ))}
      </div>

      {isAuthenticated && role === "student" && isEnrolled && (
        <section className="panel">
          <h3>{myReview ? "Редактировать отзыв" : "Оставить отзыв"}</h3>
          <form className="form" onSubmit={handleSaveReview}>
            <label className="label">
              Оценка
              <select
                className="input"
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

            <label className="label">
              Комментарий
              <textarea
                className="input"
                onChange={(event) => setReviewForm((current) => ({ ...current, comment: event.target.value }))}
                rows={4}
                value={reviewForm.comment}
              />
            </label>

            <div className="card-actions">
              <button className="btn btn--primary" disabled={isReviewPending} type="submit">
                {isReviewPending ? "Сохраняем..." : "Сохранить отзыв"}
              </button>

              {myReview && (
                <button className="btn btn--danger" disabled={isReviewPending} onClick={handleDeleteReview} type="button">
                  Удалить
                </button>
              )}
            </div>
          </form>
        </section>
      )}
    </section>
  );
}

