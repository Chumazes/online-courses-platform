import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { filesApi, formatApiError, lessonsApi } from "../lib/api";

const emptyForm = {
  title: "",
  content: "",
  lessonType: "text",
  videoUrl: "",
  durationMinutes: "",
  lessonOrder: 1,
  isFree: false
};

export function ManageLessonsPage() {
  const { sectionId } = useParams();
  const numericSectionId = Number(sectionId);
  const [lessons, setLessons] = useState([]);
  const [selectedLessonId, setSelectedLessonId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isUploadingId, setIsUploadingId] = useState(null);

  async function loadLessons() {
    setError("");
    setIsLoading(true);
    try {
      const data = await lessonsApi.getBySectionId(numericSectionId);
      const items = data ?? [];
      setLessons(items);
      if (items.length > 0 && !items.some((item) => item.lessonId === selectedLessonId)) {
        setSelectedLessonId(items[0].lessonId);
        fillForm(items[0]);
      }
      if (items.length === 0) {
        setSelectedLessonId(null);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить уроки."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (!Number.isFinite(numericSectionId)) {
      setError("Некорректный идентификатор секции.");
      setIsLoading(false);
      return;
    }
    loadLessons();
  }, [numericSectionId]);

  function fillForm(lesson) {
    setForm({
      title: lesson.title ?? "",
      content: lesson.content ?? "",
      lessonType: lesson.lessonType ?? "text",
      videoUrl: lesson.videoUrl ?? "",
      durationMinutes: lesson.durationMinutes ?? "",
      lessonOrder: Number(lesson.lessonOrder ?? 1),
      isFree: Boolean(lesson.isFree)
    });
  }

  function startCreate() {
    const nextOrder = Math.max(0, ...lessons.map((lesson) => Number(lesson.lessonOrder ?? 0))) + 1;
    setSelectedLessonId(null);
    setForm({
      ...emptyForm,
      lessonOrder: nextOrder
    });
  }

  function startEdit(lesson) {
    setSelectedLessonId(lesson.lessonId);
    fillForm(lesson);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    try {
      const payload = {
        title: form.title.trim(),
        content: form.content.trim(),
        lessonType: form.lessonType,
        videoUrl: form.videoUrl.trim() || null,
        durationMinutes: form.durationMinutes === "" ? null : Number(form.durationMinutes),
        lessonOrder: Number(form.lessonOrder),
        isFree: Boolean(form.isFree)
      };

      if (selectedLessonId) {
        await lessonsApi.update(numericSectionId, selectedLessonId, payload);
        setSuccess("Урок обновлён.");
      } else {
        await lessonsApi.create(numericSectionId, payload);
        setSuccess("Урок создан.");
      }

      await loadLessons();
      if (!selectedLessonId) {
        setForm(emptyForm);
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить урок."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDelete(lessonId) {
    if (!window.confirm("Удалить урок?")) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await lessonsApi.remove(numericSectionId, lessonId);
      setSuccess("Урок удалён.");
      await loadLessons();
      if (selectedLessonId === lessonId) {
        startCreate();
      }
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить урок."));
    }
  }

  async function handleUploadFile(lessonId, file) {
    if (!file) {
      return;
    }

    setError("");
    setSuccess("");
    setIsUploadingId(lessonId);

    try {
      await filesApi.uploadLessonFile(lessonId, file, file.name);
      setSuccess("Файл урока загружен.");
      await loadLessons();
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить файл урока."));
    } finally {
      setIsUploadingId(null);
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем уроки...</div>;
  }

  return (
    <section className="stack">
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Уроки секции</h1>
            <p className="muted">
              Секция #{numericSectionId}
              {lessons[0]?.sectionTitle ? ` • ${lessons[0].sectionTitle}` : ""}
            </p>
          </div>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to="/manage/courses">
              Назад к курсам
            </Link>
            <button className="btn btn--primary btn--fit" onClick={startCreate} type="button">
              Новый урок
            </button>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split">
        <div className="stack">
          {lessons.length === 0 ? <div className="panel panel--light">Уроков пока нет.</div> : null}
          {lessons.map((lesson) => (
            <article className="panel" key={lesson.lessonId}>
              <div className="panel-row">
                <div>
                  <h3>
                    {lesson.lessonOrder}. {lesson.title}
                  </h3>
                  <p className="muted">
                    {lesson.lessonType} {lesson.durationMinutes ? `• ${lesson.durationMinutes} мин` : ""}{" "}
                    {lesson.isFree ? "• бесплатный" : ""}
                  </p>
                </div>
                <span className="chip">{lesson.fileName ? "Файл прикреплён" : "Без файла"}</span>
              </div>

              <div className="card-actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(lesson)} type="button">
                  Редактировать
                </button>
                <label className="btn btn--ghost btn--fit">
                  {isUploadingId === lesson.lessonId ? "Загрузка..." : "Загрузить файл"}
                  <input hidden onChange={(event) => handleUploadFile(lesson.lessonId, event.target.files?.[0])} type="file" />
                </label>
                {lesson.fileUrl && (
                  <>
                    <a className="btn btn--ghost btn--fit" href={filesApi.buildFileUrl(lesson.fileUrl)} rel="noreferrer" target="_blank">
                      Открыть файл
                    </a>
                    <a className="btn btn--primary btn--fit" href={filesApi.buildDownloadUrl(lesson.fileUrl)} rel="noreferrer" target="_blank">
                      Скачать
                    </a>
                  </>
                )}
                <button className="btn btn--danger btn--fit" onClick={() => handleDelete(lesson.lessonId)} type="button">
                  Удалить
                </button>
              </div>
            </article>
          ))}
        </div>

        <form className="panel form" onSubmit={handleSubmit}>
          <h2>{selectedLessonId ? "Редактирование урока" : "Создание урока"}</h2>
          <label className="label">
            Название
            <input
              className="input"
              onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
              required
              type="text"
              value={form.title}
            />
          </label>

          <label className="label">
            Содержимое
            <textarea
              className="input"
              onChange={(event) => setForm((current) => ({ ...current, content: event.target.value }))}
              rows={7}
              value={form.content}
            />
          </label>

          <div className="grid-2">
              <label className="label">
                Тип
                <select
                  className="input"
                  onChange={(event) => setForm((current) => ({ ...current, lessonType: event.target.value }))}
                  value={form.lessonType}
                >
                  <option value="text">Текст</option>
                  <option value="video">Видео</option>
                </select>
              </label>

              <label className="label">
                Порядок
              <input
                className="input"
                min={1}
                onChange={(event) => setForm((current) => ({ ...current, lessonOrder: event.target.value }))}
                required
                type="number"
                value={form.lessonOrder}
              />
            </label>
          </div>

          <div className="grid-2">
            <label className="label">
              Длительность (мин)
              <input
                className="input"
                min={0}
                onChange={(event) => setForm((current) => ({ ...current, durationMinutes: event.target.value }))}
                type="number"
                value={form.durationMinutes}
              />
            </label>

            <label className="label">
              Ссылка на видео
              <input
                className="input"
                onChange={(event) => setForm((current) => ({ ...current, videoUrl: event.target.value }))}
                type="text"
                value={form.videoUrl}
              />
            </label>
          </div>

          <label className="checkbox">
            <input
              checked={form.isFree}
              onChange={(event) => setForm((current) => ({ ...current, isFree: event.target.checked }))}
              type="checkbox"
            />
            Бесплатный урок
          </label>

          <div className="card-actions">
            <button className="btn btn--primary btn--fit" disabled={isSubmitting} type="submit">
              {isSubmitting ? "Сохраняем..." : "Сохранить урок"}
            </button>
            {selectedLessonId && (
              <button className="btn btn--ghost btn--fit" onClick={startCreate} type="button">
                Отмена
              </button>
            )}
          </div>
        </form>
      </section>
    </section>
  );
}
