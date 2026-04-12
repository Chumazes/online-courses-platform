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
    const confirmed = window.confirm("Удалить категорию?");
    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    try {
      await coursesApi.removeCategory(id);
      setSuccess("Категория удалена.");
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
      <h1>Управление категориями</h1>
      <ErrorBanner message={error} />
      {success ? <div className="success-banner">{success}</div> : null}

      <form className="panel form" onSubmit={handleSubmit}>
        <h2>{editingId ? "Редактировать категорию" : "Новая категория"}</h2>
        <input className="input" onChange={(event) => setName(event.target.value)} placeholder="Название" required type="text" value={name} />
        <textarea
          className="input"
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Описание"
          rows={3}
          value={description}
        />
        <div className="card-actions">
          <button className="btn btn--primary btn--fit" type="submit">
            {editingId ? "Обновить" : "Создать"}
          </button>
          {editingId && (
            <button className="btn btn--ghost btn--fit" onClick={resetForm} type="button">
              Отмена
            </button>
          )}
        </div>
      </form>

      <div className="stack">
        {items.map((item) => (
          <article className="panel" key={item.categoryId}>
            <div className="panel-row">
              <div>
                <strong>{item.name}</strong>
                <p className="muted">{item.description || "Без описания"}</p>
              </div>
              <div className="card-actions">
                <button className="btn btn--ghost btn--fit" onClick={() => startEdit(item)} type="button">
                  Редактировать
                </button>
                <button className="btn btn--danger btn--fit" onClick={() => handleDelete(item.categoryId)} type="button">
                  Удалить
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

