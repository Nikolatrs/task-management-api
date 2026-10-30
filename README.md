# TaskManagement API

REST API для управления списком задач.

## Стек

- .NET 8 (Minimal API)
- PostgreSQL 17
- Npgsql
- Swagger / OpenAPI
- FluentValidation

## Требования

- .NET SDK 8.0+
- PostgreSQL 17+
- Docker (опционально, для тестовой БД)

### 1. Клонировать репозиторий

```bash
git clone https://github.com/Nikolatrs/task-management-api.git
cd task-management-api
```

### 2. Создать БД

#### 2.1.1 Запустить базу данных (Docker)

Если у вас установлен Docker, самый быстрый способ поднять PostgreSQL:

```bash
# Замените your_password на свой пароль
docker run --name asp-db -e POSTGRES_USER=user -e POSTGRES_PASSWORD=your_password -e POSTGRES_DB=taskmanagement -p 5432:5432 -d postgres:17
```

#### 2.1.2 Применить `db/init.sql`

```bash
docker exec -i asp-db psql -U user -d taskmanagement < db/init.sql
```

Где:
- `asp-db` — имя контейнера
- `user` — пользователь PostgreSQL
- `taskmanagement` — имя БД

#### 2.2.1 Создать базу данных (локальный PostgreSQL)

Если вы используете локально установленный PostgreSQL:

```bash
createdb taskmanagement
```

#### 2.2.2 Применить `db/init.sql` (локальный PostgreSQL)

```bash
psql -d taskmanagement -f db/init.sql
```

### 3. Настроить `user-secrets`

```bash
# Замените your_password на свой пароль
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=taskmanagement;Username=user;Password=your_password"
```

Где:
- `Host` — сервер
- `Database` — база данных
- `Username` — пользователь
- `Password` — пароль

### 4. Запуск проекта

```bash
dotnet run
```

### Swagger

`http://localhost:5004/swagger`

### Эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| GET | `/` | Главная страница |
| GET | `/tasks` | Список задач |
| GET | `/tasks/{id}` | Задача по id |
| POST | `/tasks` | Создать задачу |
| PUT | `/tasks/{id}` | Обновить задачу |
| DELETE | `/tasks/{id}` | Удалить задачу |
| PATCH | `/tasks/{id}` | Частично обновить задачу |

### Примеры запросов

**Посмотреть задачи**

```bash
curl -X 'GET' \
  'http://localhost:5004/tasks' \
  -H 'accept: application/json'
```

**Создать задачу**

```bash
curl -X 'POST' \
  'http://localhost:5004/tasks' \
  -H 'Content-Type: application/json' \
  -d '{"title": "Создание файла README"}'
```

**Удалить задачу**

```bash
curl -X 'DELETE' \
  'http://localhost:5004/tasks/3'
```

**Изменить задачу**

```bash
curl -X 'PATCH' \
  'http://localhost:5004/tasks/3' \
  -H 'Content-Type: application/json' \
  -d '{"isCompleted": true}'
```

### Структура проекта

```text
task-management-api/
│───Common/           # Общие константы (например лимиты валидации)
├───Endpoints/        # Эндпоинты /Tasks (Minimal API)
├───Extensions/       # Вспомогательные методы
├───Models/           # DTO и сущности
├───Properties/       # Настройки запуска
├───Repositories/     # Доступ к БД (интерфейс реализация на Npgsql)
├───db/               # SQL-схема
├───Validators/       # Валидация ввода
├───.editorconfig     # Настройки проекта
├───Program.cs        # Точка входа
└───README.md
```