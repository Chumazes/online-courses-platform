import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, formatApiError, sectionsApi } from "../lib/api";

const emptyForm = {
  title: "",
  description: "",
  sectionOrder: 1
};

export function ManageSectionsPage() {
  const { courseId } = useParams();
  const numericCourseId = Number(courseId);
  const [course, setCourse] = useState(null);
  const [sections, setSections] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function loadData() {
    setError("");
    setIsLoading(true);

    try {
      const [courseData, sectionData] = await Promise.all([
        coursesApi.getById(numericCourseId),
        sectionsApi.getByCourseId(numericCourseId)
      ]);

      setCourse(courseData);
      setSections(sectionData ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить секции курса."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (!Number.isFinite(numericCourseId)) {
      setError("Некорректный идентификатор курса.");
      setIsLoading(false);
      return;
    }

    loadData();
  }, [numericCourseId]);

  function startCreate() {
    const nextOrder = Math.max(0, ...sections.map((section) => Number(section.sectionOrder ?? 0))) + 1;
    setEditingId(null);
    setForm({
      title: "",
      description: "",
      sectionOrder: nextOrder
    });
  }

  function startEdit(section) {
    setEditingId(section.sectionId);
    setForm({
      title: section.title ?? "",
      description: section.description ?? "",
      sectionOrder: Number(section.sectionOrder ?? 1)
    });
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    try {
      const payload = {
        title: form.title.trim(),
        description: form.description.trim(),
        sectionOrder: Number(form.sectionOrder)
      };

      if (editingId) {
        await sectionsApi.update(numericCourseId, editingId, payload);
        setSuccess("Секция обновлена.");
      } else {
        await sectionsApi.create(numericCourseId, payload);
        setSuccess("Секция создана.");
      }

      setEditingId(null);
      setForm(emptyForm);
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить секцию."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDelete(sectionId) {
    if (!window.confirm("Удалить секцию?")) {
      return;
    }

    setError("");
    setSuccess("");

    try {
      await sectionsApi.remove(numericCourseId, sectionId);
      setSuccess("Секция удалена.");
      await loadData();
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить секцию."));
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем секции...</div>;
  }

  return (
    <section className="stack">
      <section className="panel management-hero">
        <div className="panel-row management-hero__row">
          <div className="management-hero__copy">
            <h1>Секции курса</h1>
            <p className="management-hero__subtitle">{course?.title ?? `Курс #${numericCourseId}`}</p>
            <p className="management-hero__meta">
              Здесь собирается программа курса: порядок секций, краткие описания и переход к урокам.
            </p>
          </div>

          <div className="card-actions management-hero__actions">
            <button className="btn btn--ghost btn--fit" onClick={startCreate} type="button">
              Новая секция
            </button>
          </div>
        </div>

        <div className="management-summary">
          <article className="management-summary__card">
            <strong>{sections.length}</strong>
            <span>Секций в этом курсе</span>
          </article>
          <article className="management-summary__card">
            <strong>{Math.max(0, ...sections.map((section) => Number(section.lessonsCount ?? 0)))}</strong>
            <span>Максимум уроков в секции</span>
          </article>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split management-split">
        <div className="stack management-column">
          {sections.length === 0 ? (
            <div className="panel panel--light management-empty">У курса пока нет секций. Начни с первой структуры.</div>
          ) : null}

          {sections.map((section) => (
            <article className={`panel management-card${editingId === section.sectionId ? " management-card--selected" : ""}`} key={section.sectionId}>
              <div className="panel-row">
                <div>
                  <h3>
                    {section.sectionOrder}. {section.title}
                  </h3>
                  <p className="muted">{section.description || "Без описания. Добавь короткое пояснение для структуры курса."}</p>
                </div>
                <span className="chip">Уроков: {section.lessonsCount ?? 0}</span>
              </div>

              <div className="management-strip">
                <span>Порядок: {section.sectionOrder}</span>
                <span>Уроков внутри: {section.lessonsCount ?? 0}</span>
              </div>

              <div className="card-actions management-card__actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(section)} type="button">
                  Редактировать
                </button>
                <a className="btn btn--chrome btn--fit" href={`/manage/sections/${section.sectionId}/lessons`}>
                  Уроки
                </a>
                <button className="btn btn--danger btn--fit" onClick={() => handleDelete(section.sectionId)} type="button">
                  Удалить
                </button>
              </div>
            </article>
          ))}
        </div>

        <form className="panel form management-form" onSubmit={handleSubmit}>
          <h2>{editingId ? "Редактирование секции" : "Новая секция"}</h2>
          <p className="management-form__hint">
            Секция задаёт блок программы. Здесь важно коротко и ясно объяснить, что студент изучит перед переходом к урокам.
          </p>

          <label className="label">
            Название секции
            <input
              className="input"
              onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
              required
              type="text"
              value={form.title}
            />
          </label>

          <label className="label">
            Описание
            <textarea
              className="input"
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              rows={8}
              value={form.description}
            />
          </label>

          <label className="label">
            Порядок в курсе
            <input
              className="input"
              min={1}
              onChange={(event) => setForm((current) => ({ ...current, sectionOrder: event.target.value }))}
              required
              type="number"
              value={form.sectionOrder}
            />
          </label>

          <div className="card-actions management-form__actions">
            <button className="btn btn--primary btn--fit" disabled={isSubmitting} type="submit">
              {isSubmitting ? "Сохраняем..." : editingId ? "Сохранить секцию" : "Создать секцию"}
            </button>
            {editingId ? (
              <button className="btn btn--ghost btn--fit" onClick={startCreate} type="button">
                Отмена
              </button>
            ) : null}
          </div>
        </form>
      </section>
    </section>
  );
}
