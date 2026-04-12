const points = [
  {
    title: "Понятный старт",
    text: "Не нужно гадать, с чего начать: уровень, маршрут и следующий шаг видны сразу."
  },
  {
    title: "Практическое обучение",
    text: "Каждое направление строится на реальных уроках, секциях и измеримом прогрессе."
  },
  {
    title: "Рост без хаоса",
    text: "Студент, преподаватель и админ работают в одном продукте и одном процессе."
  }
];

const capabilities = [
  "Каталог курсов с фильтрами и пагинацией",
  "Запись на курс и личный прогресс",
  "Файлы уроков: открыть и скачать",
  "Панель преподавателя и управление курсами",
  "Модерация отзывов и управление категориями"
];

export function WhyItPage() {
  return (
    <section className="stack">
      <section className="panel">
        <h1>Почему IT и почему сейчас</h1>
        <p className="muted">
          Цель простая: сделать практичный путь обучения, где каждой роли понятно, что делать дальше.
        </p>
      </section>

      <section className="feature-grid">
        {points.map((item) => (
          <article className="panel" key={item.title}>
            <h3>{item.title}</h3>
            <p>{item.text}</p>
          </article>
        ))}
      </section>

      <section className="panel panel--light">
        <h2>Что платформа уже умеет</h2>
        <ul className="clean-list">
          {capabilities.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </section>
    </section>
  );
}
