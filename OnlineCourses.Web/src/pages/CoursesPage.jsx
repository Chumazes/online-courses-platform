import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import mascot from "../assets/mascot.jpg";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, enrollmentsApi, formatApiError } from "../lib/api";
import { formatLevel, formatMoney } from "../lib/format";

const levelOptions = [
  { value: "", label: "Все уровни" },
  { value: "beginner", label: "Начальный" },
  { value: "intermediate", label: "Средний" },
  { value: "advanced", label: "Продвинутый" }
];

const sortOptions = [
  { value: "createdAt_desc", label: "Сначала новые" },
  { value: "rating_desc", label: "По рейтингу" },
  { value: "title_asc", label: "По названию" },
  { value: "price_asc", label: "Цена по возрастанию" },
  { value: "price_desc", label: "Цена по убыванию" }
];

export function CoursesPage() {
  const { isAuthenticated, role } = useAuth();
  const navigate = useNavigate();
  const [categories, setCategories] = useState([]);
  const [items, setItems] = useState([]);
  const [selectedCourseId, setSelectedCourseId] = useState(null);
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
          const nextItems = response?.items ?? [];
          setItems(nextItems);
          setMeta({
            hasPrevious: Boolean(response?.hasPrevious),
            hasNext: Boolean(response?.hasNext),
            totalPages: response?.totalPages ?? 1,
            totalCount: response?.totalCount ?? 0
          });

          if (nextItems.length === 0) {
            setSelectedCourseId(null);
          } else if (!nextItems.some((course) => course.courseId === selectedCourseId)) {
            setSelectedCourseId(nextItems[0].courseId);
          }
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
  }, [deferredSearch, filters.categoryId, filters.level, filters.sort, page, selectedCourseId]);

  const selectedCourse = useMemo(
    () => items.find((course) => course.courseId === selectedCourseId) ?? null,
    [items, selectedCourseId]
  );

  function updateFilter(name, value) {
    setPage(1);
    setFilters((current) => ({
      ...current,
      [name]: value
    }));
  }

  function resetFilters() {
    setPage(1);
    setFilters({
      search: "",
      categoryId: "",
      level: "",
      sort: "createdAt_desc"
    });
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

  function openSelectedCourse() {
    if (!selectedCourseId) {
      return;
    }

    navigate(`/courses/${selectedCourseId}`);
  }

  return (
    <section className="stack">
      <section className="panel catalog-head">
        <div>
          <h1>Каталог курсов</h1>
          <p className="muted">Выбирай направление, фильтруй каталог и собирай свой маршрут от low-level to top.</p>
        </div>
        {isAuthenticated && role === "student" && (
          <Link className="btn btn--ghost" to="/my-courses">
            Мои курсы
          </Link>
        )}
      </section>

      <ErrorBanner message={error} />

      <section className="panel panel--light filters-wrap">
        <div className="filters-label">Поиск и фильтры</div>

        <div className="filters-layout">
          <img alt="LLT" className="filters-logo" src={mascot} />

          <div className="filters-content">
            <div className="filters-first-row">
              <input
                className="input"
                onChange={(event) => updateFilter("search", event.target.value)}
                placeholder="Поиск по названию или описанию"
                type="text"
                value={filters.search}
              />
              <button className="btn btn--ghost btn--fit" onClick={resetFilters} type="button">
                Очистить
              </button>
            </div>

            <div className="filters-second-row">
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
          </div>
        </div>
      </section>

      {isLoading ? (
        <div className="page-state">Загружаем курсы...</div>
      ) : (
        <div className="stack">
          {items.map((course) => (
            <article
              className={`card course-card course-card--wide${selectedCourseId === course.courseId ? " course-card--selected" : ""}`}
              key={course.courseId}
              onClick={() => setSelectedCourseId(course.courseId)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  setSelectedCourseId(course.courseId);
                }
              }}
              role="button"
              tabIndex={0}
            >
              <div className="course-card__main">
                <h3>{course.title}</h3>
                <p className="muted">{course.description}</p>
                <div className="chip-row">
                  <span className="chip">{course.categoryName ?? "Без категории"}</span>
                  <span className="chip">{formatLevel(course.level)}</span>
                  <span className="chip chip--price">{formatMoney(course.price)}</span>
                </div>
              </div>

              <div className="course-card__side">
                <div className="course-avatar">LLT</div>
                {isAuthenticated && role === "student" && (
                  <button
                    className="btn btn--primary btn--fit"
                    disabled={isEnrollingId === course.courseId}
                    onClick={(event) => {
                      event.stopPropagation();
                      handleEnroll(course.courseId);
                    }}
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
          Предыдущая
        </button>
        <button className="btn btn--ghost" disabled={!meta.hasNext} onClick={() => setPage((p) => p + 1)} type="button">
          Следующая
        </button>
        <span>
          Страница {page} из {meta.totalPages} · Найдено курсов: {meta.totalCount}
        </span>
      </div>

      <div className="floating-action">
        <button className="btn btn--primary" disabled={!selectedCourse} onClick={openSelectedCourse} type="button">
          Открыть курс
        </button>
      </div>
    </section>
  );
}
