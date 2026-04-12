import { useEffect, useMemo, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, formatApiError, reviewsApi } from "../lib/api";
import { formatDate } from "../lib/format";

export function StoriesPage() {
  const [reviews, setReviews] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    async function loadStories() {
      setError("");
      setIsLoading(true);

      try {
        const response = await coursesApi.getAll({ pageNumber: 1, pageSize: 10, all: true });
        const courses = response?.items ?? [];

        const reviewLists = await Promise.all(
          courses.map(async (course) => {
            const list = await reviewsApi.getByCourse(course.courseId).catch(() => []);
            return (list ?? []).map((review) => ({
              ...review,
              courseTitle: review.courseTitle ?? course.title
            }));
          })
        );

        if (!active) {
          return;
        }

        const flat = reviewLists
          .flat()
          .sort((a, b) => new Date(b.reviewDate).getTime() - new Date(a.reviewDate).getTime())
          .slice(0, 15);

        setReviews(flat);
      } catch (err) {
        if (!active) {
          return;
        }
        setError(formatApiError(err, "Не удалось загрузить истории."));
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    loadStories();
    return () => {
      active = false;
    };
  }, []);

  const metrics = useMemo(() => {
    if (reviews.length === 0) {
      return {
        publishedReviews: 0,
        coursesWithReviews: 0,
        averageRating: 0
      };
    }

    const totalRating = reviews.reduce((sum, item) => sum + Number(item.rating ?? 0), 0);
    const courseSet = new Set(reviews.map((item) => item.courseId));

    return {
      publishedReviews: reviews.length,
      coursesWithReviews: courseSet.size,
      averageRating: (totalRating / reviews.length).toFixed(1)
    };
  }, [reviews]);

  return (
    <section className="stack">
      <section className="panel">
        <h1>Отзывы и истории студентов</h1>
        <p className="muted">Реальная обратная связь из опубликованных отзывов по курсам.</p>
      </section>

      <section className="feature-grid">
        <article className="panel">
          <h3>{metrics.publishedReviews}</h3>
          <p>Опубликованных отзывов</p>
        </article>
        <article className="panel">
          <h3>{metrics.coursesWithReviews}</h3>
          <p>Курсов с отзывами</p>
        </article>
        <article className="panel">
          <h3>{metrics.averageRating}</h3>
          <p>Средняя оценка</p>
        </article>
      </section>

      <section className="panel panel--light">
        <h2>Как проходит путь студента</h2>
        <div className="story-path">
          <article>
            <h4>1. Старт без страха</h4>
            <p>Выбери уровень и запишись на понятный маршрут.</p>
          </article>
          <article>
            <h4>2. Обучение через практику</h4>
            <p>Проходи уроки, открывай файлы и видь прогресс по шагам.</p>
          </article>
          <article>
            <h4>3. Видимый результат</h4>
            <p>Заверши уроки, оставь отзыв и помоги улучшить следующий маршрут.</p>
          </article>
        </div>
      </section>

      <ErrorBanner message={error} />

      <section className="stack">
        {isLoading ? (
          <div className="page-state">Загружаем истории...</div>
        ) : reviews.length === 0 ? (
          <div className="panel panel--light">Опубликованных историй пока нет.</div>
        ) : (
          reviews.map((review) => (
            <article className="panel panel--light review-card" key={review.reviewId}>
              <div className="review-card__head">
                <strong>{review.userName}</strong>
                <span className="chip">{"*".repeat(Math.max(1, Number(review.rating ?? 0)))}</span>
              </div>
              <p className="muted">
                {review.courseTitle} • {formatDate(review.reviewDate)}
              </p>
              <p>{review.comment || "Комментарий не указан."}</p>
            </article>
          ))
        )}
      </section>
    </section>
  );
}
