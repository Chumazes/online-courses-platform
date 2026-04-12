import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { useAuth } from "../context/AuthContext";
import { coursesApi, formatApiError } from "../lib/api";
import { formatCourseStatus, formatLevel, formatMoney } from "../lib/format";

const emptyCourseForm = {
  title: "",
  description: "",
  price: 0,
  level: "beginner",
  status: "draft",
  categoryId: ""
};

export function ManageCoursesPage() {
  const { role } = useAuth();
  const [courses, setCourses] = useState([]);
  const [categories, setCategories] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [courseForm, setCourseForm] = useState(emptyCourseForm);
  const [editCourseId, setEditCourseId] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedCourseId, setSelectedCourseId] = useState(null);

  async function loadInitial() {
    setError("");
    setSuccess("");
    setIsLoading(true);

    try {
      const [courseData, categoryList] = await Promise.all([
        role === "admin"
          ? coursesApi.getAll({
              pageNumber: 1,
              pageSize: 100,
              all: true
            })
          : coursesApi.getMy(),
        coursesApi.getCategories()
      ]);

      const nextCourses = Array.isArray(courseData) ? courseData : courseData?.items ?? [];
      setCourses(nextCourses);
      setCategories(categoryList ?? []);

      if (nextCourses.length > 0 && !nextCourses.some((course) => course.courseId === selectedCourseId)) {
        const nextSelected = nextCourses[0];
        setSelectedCourseId(nextSelected.courseId);
        if (!editCourseId) {
          startEdit(nextSelected);
        }
      }

      if (nextCourses.length === 0) {
        setSelectedCourseId(null);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить управление курсами."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadInitial();
  }, [role]);

  const selectedCourse = useMemo(
    () => courses.find((course) => course.courseId === selectedCourseId) ?? null,
    [courses, selectedCourseId]
  );

  function startEdit(course) {
    setEditCourseId(course.courseId);
    setSelectedCourseId(course.courseId);
    setCourseForm({
      title: course.title ?? "",
      description: course.description ?? "",
      price: Number(course.price ?? 0),
      level: course.level ?? "beginner",
      status: course.status ?? "draft",
      categoryId: course.categoryId ?? ""
    });
  }

  function startCreate() {
    setEditCourseId(null);
    setSelectedCourseId(null);
    setCourseForm(emptyCourseForm);
  }

  async function submitCourse(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    const payload = {
      title: courseForm.title.trim(),
      description: courseForm.description.trim(),
      price: Number(courseForm.price),
      level: courseForm.level,
      status: courseForm.status,
      categoryId: courseForm.categoryId ? Number(courseForm.categoryId) : null,
      coverImageUrl: null
    };

    try {
      if (editCourseId) {
        await coursesApi.update(editCourseId, payload);
        setSuccess("Курс обновлён.");
      } else {
        await coursesApi.create(payload);
        setSuccess("Курс создан.");
      }

      await loadInitial();
      startCreate();
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить курс."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function removeCourse(courseId) {
    if (!window.confirm("Удалить курс?")) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await coursesApi.remove(courseId);
      setSuccess("Курс удалён.");
      await loadInitial();
      if (selectedCourseId === courseId) {
        setSelectedCourseId(null);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить курс."));
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем управление...</div>;
  }

  return (
    <section className="stack">
      <section className="panel management-hero">
        <div className="panel-row management-hero__row">
          <div className="management-hero__copy">
            <h1>Управление курсами</h1>
            <p className="management-hero__subtitle">
              {role === "admin" ? "Администратор видит курсы всех преподавателей." : "Авторская панель Low-Level to Top"}
            </p>
          </div>

          <div className="card-actions management-hero__actions">
            <Link className="btn btn--ghost btn--fit" to="/dashboard">
              Панель
            </Link>
            {role === "admin" ? (
              <Link className="btn btn--ghost btn--fit" to="/manage/categories">
                Категории
              </Link>
            ) : null}
            <button className="btn btn--ghost btn--fit" onClick={startCreate} type="button">
              Новый курс
            </button>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split management-split">
        <div className="stack management-column">
          {courses.length === 0 ? <div className="panel panel--light management-empty">Курсы пока не найдены.</div> : null}

          {courses.map((course) => (
            <article
              className={`panel management-card clickable${selectedCourseId === course.courseId ? " management-card--selected" : ""}`}
              key={course.courseId}
              onClick={() => startEdit(course)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  startEdit(course);
                }
              }}
              role="button"
              tabIndex={0}
            >
              <div className="panel-row">
                <div>
                  <h3>{course.title}</h3>
                  <p className="muted">
                    {course.description || "Добавь короткое описание, чтобы курс выглядел увереннее и читался лучше в каталоге."}
                  </p>
                </div>
                <span className="chip">{formatCourseStatus(course.status)}</span>
              </div>

              <div className="management-strip">
                <span>{formatLevel(course.level)}</span>
                <span>{formatMoney(course.price)}</span>
                <span>{course.totalStudents ?? 0} студентов</span>
                {role === "admin" ? <span>Автор: {course.authorName ?? "Не указан"}</span> : null}
              </div>
            </article>
          ))}
        </div>

        <div className="stack">
          <form className="panel form management-form" onSubmit={submitCourse}>
            <h2>{editCourseId ? "Редактирование курса" : "Новый курс"}</h2>
            <p className="management-form__hint">
              Это главный редактор курса: здесь задаются статус, цена, категория и основные переходы к секциям, студентам и аналитике.
            </p>

            <label className="label">
              Название
              <input
                className="input"
                onChange={(event) => setCourseForm((current) => ({ ...current, title: event.target.value }))}
                required
                type="text"
                value={courseForm.title}
              />
            </label>

            <label className="label">
              Описание
              <textarea
                className="input"
                onChange={(event) => setCourseForm((current) => ({ ...current, description: event.target.value }))}
                required
                rows={6}
                value={courseForm.description}
              />
            </label>

            <div className="grid-2">
              <label className="label">
                Уровень
                <select
                  className="input"
                  onChange={(event) => setCourseForm((current) => ({ ...current, level: event.target.value }))}
                  value={courseForm.level}
                >
                  <option value="beginner">beginner</option>
                  <option value="intermediate">intermediate</option>
                  <option value="advanced">advanced</option>
                </select>
              </label>

              <label className="label">
                Цена
                <input
                  className="input"
                  min={0}
                  onChange={(event) => setCourseForm((current) => ({ ...current, price: event.target.value }))}
                  type="number"
                  value={courseForm.price}
                />
              </label>
            </div>

            <label className="label">
              Категория
              <select
                className="input"
                onChange={(event) => setCourseForm((current) => ({ ...current, categoryId: event.target.value }))}
                value={courseForm.categoryId}
              >
                <option value="">Без категории</option>
                {categories.map((category) => (
                  <option key={category.categoryId} value={category.categoryId}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="label">
              Статус
              <select
                className="input"
                onChange={(event) => setCourseForm((current) => ({ ...current, status: event.target.value }))}
                value={courseForm.status}
              >
                <option value="draft">draft</option>
                <option value="published">published</option>
                <option value="archived">archived</option>
              </select>
            </label>

            <div className="card-actions management-form__actions">
              <button className="btn btn--primary btn--fit" disabled={isSubmitting} type="submit">
                {isSubmitting ? "Сохраняем..." : editCourseId ? "Сохранить курс" : "Создать курс"}
              </button>
              {editCourseId ? (
                <button className="btn btn--danger btn--fit" onClick={() => removeCourse(editCourseId)} type="button">
                  Удалить курс
                </button>
              ) : null}
            </div>
          </form>

          <section className="panel management-form">
            <h2>Рабочая зона курса</h2>
            {selectedCourse ? (
              <>
                <div className="management-strip">
                  <span>{selectedCourse.title}</span>
                  <span>{formatCourseStatus(selectedCourse.status)}</span>
                  <span>{selectedCourse.categoryName ?? "Без категории"}</span>
                </div>

                <div className="card-actions management-form__actions">
                  <Link className="btn btn--chrome btn--fit" to="/profile">
                    Профиль
                  </Link>
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/sections`}>
                    Секции курса
                  </Link>
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/students`}>
                    Студенты курса
                  </Link>
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/analytics`}>
                    Аналитика курса
                  </Link>
                  {role === "admin" ? (
                    <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/reviews`}>
                      Отзывы курса
                    </Link>
                  ) : null}
                </div>
              </>
            ) : (
              <p className="muted">Выбери курс слева или начни с создания нового.</p>
            )}
          </section>
        </div>
      </section>
    </section>
  );
}
