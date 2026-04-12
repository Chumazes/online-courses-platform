import { useDeferredValue, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, enrollmentsApi, formatApiError } from "../lib/api";
import { formatMoney } from "../lib/format";

const levelOptions = [
  { value: "", label: "Любой уровень" },
  { value: "beginner", label: "Beginner" },
  { value: "intermediate", label: "Intermediate" },
  { value: "advanced", label: "Advanced" }
];

const sortOptions = [
  { value: "createdAt_desc", label: "Новые" },
  { value: "rating_desc", label: "Рейтинг" },
  { value: "price_asc", label: "Цена: по возрастанию" },
  { value: "price_desc", label: "Цена: по убыванию" }
];

export function CoursesPage() {
  const { isAuthenticated, role } = useAuth();
  const [categories, setCategories] = useState([]);
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isEnrollingId, setIsEnrollingId] = useState(null);
  const [page, setPage] = useState(1);
  const [meta, setMeta] = useState({ hasPrevious: false, hasNext: false, totalPages: 1, totalCount: 0 });
  const [filters, setFilters] = useState({
    search: "",
    categoryId: "",
    level: "",
    sort: "createdAt_desc"
  });
  const deferredSearch = useDeferredValue(filters.search);

  useEffect(() => {
    let cancelled = false;

    async function loadCategories() {
      try {
        const data = await coursesApi.getCategories();
        if (!cancelled) {
          setCategories(data ?? []);
        }
      } catch {
        if (!cancelled) {
          setCategories([]);
        }
      }
    }

    loadCategories();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadCourses() {
      setError("");
      setIsLoading(true);

      try {
        const [sortBy, sortOrder] = filters.sort.split("_");
        const response = await coursesApi.getAll({
          pageNumber: page,
          pageSize: 8,
          search: deferredSearch,
          categoryId: filters.categoryId || undefined,
          level: filters.level || undefined,
          sortBy,
          sortOrder
        });

        if (!cancelled) {
          setItems(response?.items ?? []);
          setMeta({
            hasPrevious: Boolean(response?.hasPrevious),
            hasNext: Boolean(response?.hasNext),
            totalPages: response?.totalPages ?? 1,
            totalCount: response?.totalCount ?? 0
          });
        }
      } catch (err) {
        if (!cancelled) {
          setError(formatApiError(err, "Не удалось загрузить курсы."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadCourses();

    return () => {
      cancelled = true;
    };
  }, [deferredSearch, filters.categoryId, filters.level, filters.sort, page]);

  function updateFilter(name, value) {
    setPage(1);
    setFilters((current) => ({
      ...current,
      [name]: value
    }));
  }

  async function handleEnroll(courseId) {
    setError("");
    setIsEnrollingId(courseId);

    try {
      await enrollmentsApi.enroll(courseId);
    } catch (err) {
      setError(formatApiError(err, "Не удалось записаться на курс."));
    } finally {
      setIsEnrollingId(null);
    }
  }

  return (
    <section className="stack">
      <div className="section-head">
        <div>
          <h1>Каталог курсов</h1>
          <p className="muted">Найдено курсов: {meta.totalCount}</p>
        </div>
      </div>

      <ErrorBanner message={error} />

      <div className="panel filters">
        <input
          className="input"
          onChange={(event) => updateFilter("search", event.target.value)}
          placeholder="Поиск по названию или описанию"
          type="text"
          value={filters.search}
        />

        <select className="input" onChange={(event) => updateFilter("categoryId", event.target.value)} value={filters.categoryId}>
          <option value="">Все категории</option>
          {categories.map((item) => (
            <option key={item.categoryId} value={item.categoryId}>
              {item.name}
            </option>
          ))}
        </select>

        <select className="input" onChange={(event) => updateFilter("level", event.target.value)} value={filters.level}>
          {levelOptions.map((item) => (
            <option key={item.value || "all"} value={item.value}>
              {item.label}
            </option>
          ))}
        </select>

        <select className="input" onChange={(event) => updateFilter("sort", event.target.value)} value={filters.sort}>
          {sortOptions.map((item) => (
            <option key={item.value} value={item.value}>
              {item.label}
            </option>
          ))}
        </select>
      </div>

      {isLoading ? (
        <div className="page-state">Загружаем курсы...</div>
      ) : (
        <div className="courses-grid">
          {items.map((course) => (
            <article className="card course-card" key={course.courseId}>
              <p className="chip">{course.level}</p>
              <h3>{course.title}</h3>
              <p className="muted">{course.description}</p>
              <p className="muted">Категория: {course.categoryName ?? "—"}</p>
              <p className="price">{formatMoney(course.price)}</p>

              <div className="card-actions">
                <Link className="btn btn--ghost" to={`/courses/${course.courseId}`}>
                  Открыть
                </Link>

                {isAuthenticated && role === "student" && (
                  <button
                    className="btn btn--primary"
                    disabled={isEnrollingId === course.courseId}
                    onClick={() => handleEnroll(course.courseId)}
                    type="button"
                  >
                    {isEnrollingId === course.courseId ? "Запись..." : "Записаться"}
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}

      <div className="pagination">
        <button className="btn btn--ghost" disabled={!meta.hasPrevious} onClick={() => setPage((p) => Math.max(1, p - 1))} type="button">
          Назад
        </button>
        <span>
          Страница {page} из {meta.totalPages}
        </span>
        <button className="btn btn--ghost" disabled={!meta.hasNext} onClick={() => setPage((p) => p + 1)} type="button">
          Дальше
        </button>
      </div>
    </section>
  );
}
