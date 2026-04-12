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
        setSelectedCourseId(nextCourses[0].courseId);
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
        setSuccess("Course updated.");
      } else {
        await coursesApi.create(payload);
        setSuccess("Course created.");
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
      setSuccess("Course deleted.");
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
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Управление курсами</h1>
            <p className="muted">
              {role === "admin" ? "Админ видит все курсы платформы." : "Преподаватель видит только свои курсы."}
            </p>
          </div>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to="/dashboard">
              Панель
            </Link>
            {role === "admin" && (
              <Link className="btn btn--ghost btn--fit" to="/manage/categories">
                Категории
              </Link>
            )}
            <button className="btn btn--primary btn--fit" onClick={startCreate} type="button">
              Новый курс
            </button>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split">
        <div className="stack">
          {courses.length === 0 ? <div className="panel panel--light">Курсы пока не найдены.</div> : null}
          {courses.map((course) => (
            <article
              className={`panel clickable${selectedCourseId === course.courseId ? " panel--selected" : ""}`}
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
              <div className="panel-row">
                <h3>{course.title}</h3>
                <span className="chip">{formatCourseStatus(course.status)}</span>
              </div>
              <p className="muted">{course.description}</p>
              <div className="chip-row">
                <span className="chip">{formatLevel(course.level)}</span>
                <span className="chip">{course.categoryName ?? "Без категории"}</span>
                <span className="chip">Студентов: {course.totalStudents ?? 0}</span>
              </div>
              <p className="price">{formatMoney(course.price)}</p>
              <div className="card-actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(course)} type="button">
                  Редактировать
                </button>
                <button className="btn btn--danger btn--fit" onClick={() => removeCourse(course.courseId)} type="button">
                  Удалить
                </button>
              </div>
            </article>
          ))}
        </div>

        <div className="stack">
          <form className="panel form" onSubmit={submitCourse}>
            <h2>{editCourseId ? "Редактирование курса" : "Создание курса"}</h2>

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
                rows={4}
                value={courseForm.description}
              />
            </label>

            <div className="grid-2">
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

              <label className="label">
                Уровень
                <select
                  className="input"
                  onChange={(event) => setCourseForm((current) => ({ ...current, level: event.target.value }))}
                  value={courseForm.level}
                >
                  <option value="beginner">Начальный</option>
                  <option value="intermediate">Средний</option>
                  <option value="advanced">Продвинутый</option>
                </select>
              </label>
            </div>

            <div className="grid-2">
              <label className="label">
                Статус
                <select
                  className="input"
                  onChange={(event) => setCourseForm((current) => ({ ...current, status: event.target.value }))}
                  value={courseForm.status}
                >
                  <option value="draft">Черновик</option>
                  <option value="published">Опубликован</option>
                  <option value="archived">Архив</option>
                </select>
              </label>

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
            </div>

            <div className="card-actions">
              <button className="btn btn--primary btn--fit" disabled={isSubmitting} type="submit">
                {isSubmitting ? "Сохраняем..." : editCourseId ? "Сохранить изменения" : "Создать курс"}
              </button>
              {editCourseId && (
                <button className="btn btn--ghost btn--fit" onClick={startCreate} type="button">
                  Отмена
                </button>
              )}
            </div>
          </form>

          <section className="panel panel--inner">
            <h2>Рабочая зона курса</h2>
            {selectedCourse ? (
              <>
                <p className="muted">Выбран курс: {selectedCourse.title}</p>
                <div className="card-actions">
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/sections`}>
                    Секции
                  </Link>
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/students`}>
                    Студенты
                  </Link>
                  <Link className="btn btn--ghost btn--fit" to={`/manage/courses/${selectedCourse.courseId}/analytics`}>
                    Аналитика
                  </Link>
                  {role === "admin" && (
                    <Link className="btn btn--danger btn--fit" to={`/manage/courses/${selectedCourse.courseId}/reviews`}>
                      Отзывы
                    </Link>
                  )}
                </div>
              </>
            ) : (
              <p className="muted">Выбери курс в списке слева.</p>
            )}
          </section>
        </div>
      </section>
    </section>
  );
}
