import { useEffect, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, filesApi, formatApiError, lessonsApi, sectionsApi } from "../lib/api";
import { formatMoney } from "../lib/format";

const emptyCourseForm = {
  title: "",
  description: "",
  price: 0,
  level: "beginner",
  status: "draft",
  categoryId: ""
};

export function ManageCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [categories, setCategories] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [courseForm, setCourseForm] = useState(emptyCourseForm);
  const [editCourseId, setEditCourseId] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedCourseId, setSelectedCourseId] = useState(null);
  const [sections, setSections] = useState([]);
  const [lessonsBySection, setLessonsBySection] = useState({});
  const [newSectionTitle, setNewSectionTitle] = useState("");
  const [newSectionDescription, setNewSectionDescription] = useState("");
  const [newLessonBySection, setNewLessonBySection] = useState({});

  async function loadInitial() {
    setError("");
    setSuccess("");
    setIsLoading(true);

    try {
      const [myCourses, categoryList] = await Promise.all([coursesApi.getMy(), coursesApi.getCategories()]);
      setCourses(myCourses ?? []);
      setCategories(categoryList ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить управление курсами."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadInitial();
  }, []);

  async function loadStructure(courseId) {
    setError("");
    try {
      const sectionList = await sectionsApi.getByCourseId(courseId);
      const pairs = await Promise.all(
        (sectionList ?? []).map(async (section) => {
          const lessonList = await lessonsApi.getBySectionId(section.sectionId);
          return [section.sectionId, lessonList ?? []];
        })
      );

      setSelectedCourseId(courseId);
      setSections(sectionList ?? []);
      setLessonsBySection(Object.fromEntries(pairs));
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить структуру курса."));
    }
  }

  function startEdit(course) {
    setEditCourseId(course.courseId);
    setCourseForm({
      title: course.title,
      description: course.description,
      price: Number(course.price ?? 0),
      level: course.level ?? "beginner",
      status: course.status ?? "draft",
      categoryId: course.categoryId ?? ""
    });
  }

  function resetForm() {
    setEditCourseId(null);
    setCourseForm(emptyCourseForm);
  }

  async function submitCourse(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    const payload = {
      title: courseForm.title,
      description: courseForm.description,
      price: Number(courseForm.price),
      level: courseForm.level,
      status: courseForm.status,
      categoryId: courseForm.categoryId ? Number(courseForm.categoryId) : null,
      coverImageUrl: null
    };

    try {
      if (editCourseId) {
        await coursesApi.update(editCourseId, payload);
        setSuccess("Курс обновлен.");
      } else {
        await coursesApi.create(payload);
        setSuccess("Курс создан.");
      }

      await loadInitial();
      resetForm();
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить курс."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function removeCourse(courseId) {
    const confirmed = window.confirm("Удалить курс?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await coursesApi.remove(courseId);
      setSuccess("Курс удален.");
      await loadInitial();
      if (selectedCourseId === courseId) {
        setSelectedCourseId(null);
        setSections([]);
        setLessonsBySection({});
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить курс."));
    }
  }

  async function createSection(event) {
    event.preventDefault();
    if (!selectedCourseId) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      const nextOrder = Math.max(0, ...sections.map((section) => section.sectionOrder ?? 0)) + 1;
      await sectionsApi.create(selectedCourseId, {
        title: newSectionTitle,
        description: newSectionDescription,
        sectionOrder: nextOrder
      });

      setNewSectionTitle("");
      setNewSectionDescription("");
      await loadStructure(selectedCourseId);
      setSuccess("Секция создана.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось создать секцию."));
    }
  }

  async function removeSection(sectionId) {
    if (!selectedCourseId) {
      return;
    }

    const confirmed = window.confirm("Удалить секцию?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await sectionsApi.remove(selectedCourseId, sectionId);
      await loadStructure(selectedCourseId);
      setSuccess("Секция удалена.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить секцию."));
    }
  }

  async function createLesson(sectionId) {
    const draft = newLessonBySection[sectionId];
    if (!draft?.title) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      const lessonOrder = Math.max(0, ...(lessonsBySection[sectionId] ?? []).map((lesson) => lesson.lessonOrder ?? 0)) + 1;
      await lessonsApi.create(sectionId, {
        title: draft.title,
        content: draft.content || "",
        lessonType: draft.lessonType || "text",
        videoUrl: draft.videoUrl || null,
        durationMinutes: draft.durationMinutes ? Number(draft.durationMinutes) : null,
        lessonOrder,
        isFree: Boolean(draft.isFree)
      });

      setNewLessonBySection((current) => ({
        ...current,
        [sectionId]: {
          title: "",
          content: "",
          lessonType: "text",
          durationMinutes: "",
          videoUrl: "",
          isFree: false
        }
      }));

      await loadStructure(selectedCourseId);
      setSuccess("Урок создан.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось создать урок."));
    }
  }

  async function removeLesson(sectionId, lessonId) {
    const confirmed = window.confirm("Удалить урок?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await lessonsApi.remove(sectionId, lessonId);
      await loadStructure(selectedCourseId);
      setSuccess("Урок удален.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить урок."));
    }
  }

  async function uploadLessonFile(lessonId, file) {
    if (!file) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await filesApi.uploadLessonFile(lessonId, file, file.name);
      await loadStructure(selectedCourseId);
      setSuccess("Файл урока загружен.");
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить файл урока."));
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем управление курсами...</div>;
  }

  return (
    <section className="stack">
      <h1>Управление курсами</h1>
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <form className="panel form" onSubmit={submitCourse}>
        <h2>{editCourseId ? "Редактировать курс" : "Создать курс"}</h2>

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
              <option value="beginner">beginner</option>
              <option value="intermediate">intermediate</option>
              <option value="advanced">advanced</option>
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
              <option value="draft">draft</option>
              <option value="published">published</option>
              <option value="archived">archived</option>
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
          <button className="btn btn--primary" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Сохраняем..." : editCourseId ? "Обновить" : "Создать"}
          </button>
          {editCourseId && (
            <button className="btn btn--ghost" onClick={resetForm} type="button">
              Отмена
            </button>
          )}
        </div>
      </form>

      <div className="courses-grid">
        {courses.map((course) => (
          <article className="card course-card" key={course.courseId}>
            <p className="chip">{course.level}</p>
            <h3>{course.title}</h3>
            <p className="muted">{course.description}</p>
            <p className="muted">Статус: {course.status}</p>
            <p className="price">{formatMoney(course.price)}</p>
            <div className="card-actions">
              <button className="btn btn--ghost" onClick={() => startEdit(course)} type="button">
                Редактировать
              </button>
              <button className="btn btn--ghost" onClick={() => loadStructure(course.courseId)} type="button">
                Секции и уроки
              </button>
              <button className="btn btn--danger" onClick={() => removeCourse(course.courseId)} type="button">
                Удалить
              </button>
            </div>
          </article>
        ))}
      </div>

      {selectedCourseId && (
        <section className="stack">
          <h2>Структура курса #{selectedCourseId}</h2>

          <form className="panel form" onSubmit={createSection}>
            <h3>Новая секция</h3>
            <input
              className="input"
              onChange={(event) => setNewSectionTitle(event.target.value)}
              placeholder="Название секции"
              required
              type="text"
              value={newSectionTitle}
            />
            <textarea
              className="input"
              onChange={(event) => setNewSectionDescription(event.target.value)}
              placeholder="Описание секции"
              rows={3}
              value={newSectionDescription}
            />
            <button className="btn btn--primary btn--fit" type="submit">
              Добавить секцию
            </button>
          </form>

          {sections.map((section) => {
            const lessonDraft = newLessonBySection[section.sectionId] ?? {
              title: "",
              content: "",
              lessonType: "text",
              durationMinutes: "",
              videoUrl: "",
              isFree: false
            };

            return (
              <article className="panel" key={section.sectionId}>
                <div className="panel-row">
                  <h3>
                    {section.sectionOrder}. {section.title}
                  </h3>
                  <button className="btn btn--danger" onClick={() => removeSection(section.sectionId)} type="button">
                    Удалить секцию
                  </button>
                </div>

                <p className="muted">{section.description || "Без описания"}</p>

                <div className="stack">
                  {(lessonsBySection[section.sectionId] ?? []).map((lesson) => (
                    <article className="panel panel--inner" key={lesson.lessonId}>
                      <div className="panel-row">
                        <div>
                          <strong>
                            {lesson.lessonOrder}. {lesson.title}
                          </strong>
                          <p className="muted">Тип: {lesson.lessonType}</p>
                        </div>
                        <button className="btn btn--danger" onClick={() => removeLesson(section.sectionId, lesson.lessonId)} type="button">
                          Удалить
                        </button>
                      </div>

                      <div className="card-actions">
                        <label className="btn btn--ghost btn--fit">
                          Загрузить файл урока
                          <input
                            hidden
                            onChange={(event) => uploadLessonFile(lesson.lessonId, event.target.files?.[0])}
                            type="file"
                          />
                        </label>
                        {lesson.fileUrl && (
                          <a className="btn btn--primary btn--fit" href={filesApi.buildDownloadUrl(lesson.fileUrl)} rel="noreferrer" target="_blank">
                            Скачать текущий файл
                          </a>
                        )}
                      </div>
                    </article>
                  ))}
                </div>

                <div className="panel panel--inner form">
                  <h4>Добавить урок</h4>
                  <input
                    className="input"
                    onChange={(event) =>
                      setNewLessonBySection((current) => ({
                        ...current,
                        [section.sectionId]: { ...lessonDraft, title: event.target.value }
                      }))
                    }
                    placeholder="Название урока"
                    type="text"
                    value={lessonDraft.title}
                  />
                  <textarea
                    className="input"
                    onChange={(event) =>
                      setNewLessonBySection((current) => ({
                        ...current,
                        [section.sectionId]: { ...lessonDraft, content: event.target.value }
                      }))
                    }
                    placeholder="Контент"
                    rows={3}
                    value={lessonDraft.content}
                  />
                  <div className="grid-3">
                    <select
                      className="input"
                      onChange={(event) =>
                        setNewLessonBySection((current) => ({
                          ...current,
                          [section.sectionId]: { ...lessonDraft, lessonType: event.target.value }
                        }))
                      }
                      value={lessonDraft.lessonType}
                    >
                      <option value="text">text</option>
                      <option value="video">video</option>
                    </select>

                    <input
                      className="input"
                      min={0}
                      onChange={(event) =>
                        setNewLessonBySection((current) => ({
                          ...current,
                          [section.sectionId]: { ...lessonDraft, durationMinutes: event.target.value }
                        }))
                      }
                      placeholder="Минуты"
                      type="number"
                      value={lessonDraft.durationMinutes}
                    />

                    <label className="checkbox">
                      <input
                        checked={Boolean(lessonDraft.isFree)}
                        onChange={(event) =>
                          setNewLessonBySection((current) => ({
                            ...current,
                            [section.sectionId]: { ...lessonDraft, isFree: event.target.checked }
                          }))
                        }
                        type="checkbox"
                      />
                      Бесплатный
                    </label>
                  </div>

                  <input
                    className="input"
                    onChange={(event) =>
                      setNewLessonBySection((current) => ({
                        ...current,
                        [section.sectionId]: { ...lessonDraft, videoUrl: event.target.value }
                      }))
                    }
                    placeholder="Ссылка на видео (опционально)"
                    type="text"
                    value={lessonDraft.videoUrl}
                  />

                  <button className="btn btn--primary btn--fit" onClick={() => createLesson(section.sectionId)} type="button">
                    Добавить урок
                  </button>
                </div>
              </article>
            );
          })}
        </section>
      )}
    </section>
  );
}

