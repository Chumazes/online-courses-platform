import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
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
      setError(formatApiError(err, "Не удалось загрузить секции."));
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
      <section className="panel">
        <div className="panel-row">
          <div>
            <h1>Секции курса</h1>
            <p className="muted">{course?.title ?? `Курс #${numericCourseId}`}</p>
          </div>
          <div className="card-actions">
            <Link className="btn btn--ghost btn--fit" to="/manage/courses">
              Назад к курсам
            </Link>
            <button className="btn btn--primary btn--fit" onClick={startCreate} type="button">
              Новая секция
            </button>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split">
        <div className="stack">
          {sections.length === 0 ? <div className="panel panel--light">Секций пока нет.</div> : null}
          {sections.map((section) => (
            <article className="panel" key={section.sectionId}>
              <div className="panel-row">
                <div>
                  <h3>
                    {section.sectionOrder}. {section.title}
                  </h3>
                  <p className="muted">{section.description || "Без описания."}</p>
                </div>
                <span className="chip">Уроков: {section.lessonsCount ?? 0}</span>
              </div>
              <div className="card-actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(section)} type="button">
                  Редактировать
                </button>
                <Link className="btn btn--primary btn--fit" to={`/manage/sections/${section.sectionId}/lessons`}>
                  Уроки
                </Link>
                <button className="btn btn--danger btn--fit" onClick={() => handleDelete(section.sectionId)} type="button">
                  Удалить
                </button>
              </div>
            </article>
          ))}
        </div>

        <form className="panel form" onSubmit={handleSubmit}>
          <h2>{editingId ? "Редактирование секции" : "Создание секции"}</h2>
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
            Описание
            <textarea
              className="input"
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              rows={6}
              value={form.description}
            />
          </label>

          <label className="label">
            Порядок
            <input
              className="input"
              min={1}
              onChange={(event) => setForm((current) => ({ ...current, sectionOrder: event.target.value }))}
              required
              type="number"
              value={form.sectionOrder}
            />
          </label>

          <div className="card-actions">
            <button className="btn btn--primary btn--fit" disabled={isSubmitting} type="submit">
              {isSubmitting ? "Сохраняем..." : "Сохранить секцию"}
            </button>
            {editingId && (
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
