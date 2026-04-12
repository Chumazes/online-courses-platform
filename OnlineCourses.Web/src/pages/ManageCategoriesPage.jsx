import { useEffect, useState } from "react";
import { ErrorBanner } from "../components/ErrorBanner";
import { coursesApi, formatApiError } from "../lib/api";

export function ManageCategoriesPage() {
  const [items, setItems] = useState([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  async function loadCategories() {
    setError("");
    setIsLoading(true);
    try {
      const data = await coursesApi.getCategories();
      setItems(data ?? []);
    } catch (err) {
      setError(formatApiError(err, "Не удалось загрузить категории."));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadCategories();
  }, []);

  function startEdit(item) {
    setEditingId(item.categoryId);
    setName(item.name);
    setDescription(item.description ?? "");
  }

  function resetForm() {
    setEditingId(null);
    setName("");
    setDescription("");
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    try {
      const payload = {
        name,
        description: description || null,
        parentCategoryId: null
      };

      if (editingId) {
        await coursesApi.updateCategory(editingId, payload);
        setSuccess("Категория обновлена.");
      } else {
        await coursesApi.createCategory(payload);
        setSuccess("Категория создана.");
      }

      resetForm();
      await loadCategories();
    } catch (err) {
      setError(formatApiError(err, "Не удалось сохранить категорию."));
    }
  }

  async function handleDelete(id) {
    if (!window.confirm("Удалить категорию?")) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await coursesApi.removeCategory(id);
      setSuccess("Категория удалена.");
      if (editingId === id) {
        resetForm();
      }
      await loadCategories();
    } catch (err) {
      setError(formatApiError(err, "Не удалось удалить категорию."));
    }
  }

  if (isLoading) {
    return <div className="page-state">Загружаем категории...</div>;
  }

  return (
    <section className="stack">
      <section className="panel management-hero">
        <div className="panel-row management-hero__row">
          <div className="management-hero__copy">
            <h1>Категории платформы</h1>
            <p className="management-hero__subtitle">Администратор управляет структурами каталога и фильтрами витрины.</p>
          </div>
          <div className="card-actions management-hero__actions">
            <button className="btn btn--ghost btn--fit" onClick={resetForm} type="button">
              Новая категория
            </button>
          </div>
        </div>
      </section>

      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <section className="manage-split management-split">
        <div className="stack management-column">
          {items.map((item) => (
            <article className={`panel management-card${editingId === item.categoryId ? " management-card--selected" : ""}`} key={item.categoryId}>
              <div className="panel-row">
                <div>
                  <h3>{item.name}</h3>
                  <p className="muted">{item.description || "Без описания. Добавь короткое пояснение для каталога и фильтров."}</p>
                </div>
              </div>

              <div className="management-strip">
                <span>Самостоятельная категория</span>
              </div>

              <div className="card-actions management-card__actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(item)} type="button">
                  Редактировать
                </button>
                <button className="btn btn--danger btn--fit" onClick={() => handleDelete(item.categoryId)} type="button">
                  Удалить
                </button>
              </div>
            </article>
          ))}
        </div>

        <form className="panel form management-form" onSubmit={handleSubmit}>
          <h2>{editingId ? "Редактирование категории" : "Новая категория"}</h2>
          <p className="management-form__hint">
            Категории используются в фильтрах каталога, карточках курсов и аналитике. Если категория уже назначена курсам, удаление может быть недоступно.
          </p>

          <label className="label">
            Название категории
            <input className="input" onChange={(event) => setName(event.target.value)} required type="text" value={name} />
          </label>

          <label className="label">
            Описание
            <textarea className="input" onChange={(event) => setDescription(event.target.value)} rows={8} value={description} />
          </label>

          <div className="card-actions management-form__actions">
            <button className="btn btn--primary btn--fit" type="submit">
              {editingId ? "Сохранить категорию" : "Создать категорию"}
            </button>
            {editingId ? (
              <button className="btn btn--danger btn--fit" onClick={() => handleDelete(editingId)} type="button">
                Удалить категорию
              </button>
            ) : null}
          </div>
        </form>
      </section>
    </section>
  );
}
