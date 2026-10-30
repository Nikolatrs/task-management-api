-- init.sql
-- Схема БД для приложения "TaskManagementApi"
-- Запуск: psql -d taskmanagement_test -f db/init.sql
-- Идемпотентен: можно выполнить многократно

BEGIN;

DROP TABLE IF EXISTS public.tasks CASCADE;

CREATE TABLE public.tasks (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_tasks_title ON public.tasks(title);

COMMIT;