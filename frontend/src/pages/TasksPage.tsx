import { useCallback, useEffect, useState } from 'react';
import { App, Button, Input, Popconfirm, Select, Space, Table, Tag, Tooltip } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, EditOutlined, LogoutOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useAuth } from '../AuthContext';
import { getErrorMessage, tasksApi } from '../api';
import TaskFormModal from '../components/TaskFormModal';
import {
  PRIORITY_LABEL, STATUS_LABEL,
  type PagedResult, type Task, type TaskFilter, type TaskInput, type TaskPriority, type TaskStatus, type TaskSummary,
} from '../types';

const PRIORITY_COLOR: Record<TaskPriority, string> = { Low: 'default', Medium: 'blue', High: 'red' };
const STATUS_COLOR: Record<TaskStatus | 'all', string> = {
  all: '#1d4e89', Pending: '#8a94a6', InProgress: '#d48806', Completed: '#2f8f4e',
};

const options = (labels: Record<string, string>) => Object.entries(labels).map(([value, label]) => ({ value, label }));

export default function TasksPage() {
  const { user, logout } = useAuth();
  const { message } = App.useApp();

  const [filter, setFilter] = useState<TaskFilter>({ page: 1, pageSize: 10 });
  const [data, setData] = useState<PagedResult<Task> | null>(null);
  const [summary, setSummary] = useState<TaskSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [editing, setEditing] = useState<Task | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const isAdmin = user?.role === 'Admin';

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [list, totals] = await Promise.all([tasksApi.list(filter), tasksApi.summary()]);
      setData(list);
      setSummary(totals);
    } catch (e) {
      message.error(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, [filter, message]);

  useEffect(() => { void load(); }, [load]);

  const patchFilter = (patch: Partial<TaskFilter>) => setFilter((f) => ({ ...f, page: 1, ...patch }));

  const save = async (input: TaskInput) => {
    setSaving(true);
    try {
      if (editing) await tasksApi.update(editing.id, input);
      else await tasksApi.create(input);
      message.success(editing ? 'Tarefa atualizada.' : 'Tarefa criada.');
      setFormOpen(false);
      await load();
    } catch (e) {
      message.error(getErrorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  const changeStatus = async (task: Task, status: TaskStatus) => {
    try {
      await tasksApi.updateStatus(task.id, status);
      message.success('Status atualizado.');
      await load();
    } catch (e) {
      message.error(getErrorMessage(e));
    }
  };

  const remove = async (task: Task) => {
    try {
      await tasksApi.remove(task.id);
      message.success('Tarefa excluída.');
      // Se era o último item da página, volta uma página.
      if (data && data.items.length === 1 && filter.page > 1) setFilter((f) => ({ ...f, page: f.page - 1 }));
      else await load();
    } catch (e) {
      message.error(getErrorMessage(e));
    }
  };

  const columns: ColumnsType<Task> = [
    {
      title: 'Tarefa',
      dataIndex: 'title',
      render: (_, t) => (
        <div>
          <div className="task-title">{t.title}</div>
          {t.description && <div className="task-desc">{t.description}</div>}
        </div>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'status',
      width: 170,
      render: (status: TaskStatus, t) => (
        <Select
          size="small"
          value={status}
          variant="borderless"
          style={{ width: 140, color: STATUS_COLOR[status], fontWeight: 500 }}
          options={options(STATUS_LABEL)}
          onChange={(s) => changeStatus(t, s)}
          aria-label={`Status da tarefa ${t.title}`}
        />
      ),
    },
    {
      title: 'Prioridade',
      dataIndex: 'priority',
      width: 110,
      render: (p: TaskPriority) => <Tag color={PRIORITY_COLOR[p]}>{PRIORITY_LABEL[p]}</Tag>,
    },
    {
      title: 'Vencimento',
      dataIndex: 'dueDate',
      width: 130,
      render: (d: string | null | undefined, t) => {
        if (!d) return '—';
        const overdue = t.status !== 'Completed' && dayjs(d).endOf('day').isBefore(dayjs());
        return overdue
          ? <Tooltip title="Atrasada"><span className="overdue">{dayjs(d).format('DD/MM/YYYY')}</span></Tooltip>
          : dayjs(d).format('DD/MM/YYYY');
      },
    },
    ...(isAdmin ? [{ title: 'Responsável', dataIndex: 'ownerName', width: 170 } as ColumnsType<Task>[number]] : []),
    {
      title: 'Ações',
      width: 96,
      render: (_, t) => (
        <Space size={4}>
          <Tooltip title="Editar">
            <Button type="text" icon={<EditOutlined />} aria-label={`Editar ${t.title}`}
              onClick={() => { setEditing(t); setFormOpen(true); }} />
          </Tooltip>
          <Popconfirm title="Excluir esta tarefa?" okText="Excluir" cancelText="Cancelar" okButtonProps={{ danger: true }}
            onConfirm={() => remove(t)}>
            <Tooltip title="Excluir">
              <Button type="text" danger icon={<DeleteOutlined />} aria-label={`Excluir ${t.title}`} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const cards: { key: TaskStatus | 'all'; label: string; value?: number }[] = [
    { key: 'all', label: 'Todas', value: summary?.total },
    { key: 'Pending', label: 'Pendentes', value: summary?.pending },
    { key: 'InProgress', label: 'Em andamento', value: summary?.inProgress },
    { key: 'Completed', label: 'Concluídas', value: summary?.completed },
  ];

  return (
    <>
      <header className="app-header">
        <span className="app-header__title">Gestor de Tarefas</span>
        <span className="app-header__user">
          {user?.fullName} ({isAdmin ? 'Administrador' : 'Usuário'})
          <Button size="small" ghost icon={<LogoutOutlined />} onClick={logout}>Sair</Button>
        </span>
      </header>

      <main className="page">
        <div className="page__top">
          <div>
            <h2>Tarefas</h2>
            <span>{isAdmin ? 'Você está vendo as tarefas de todos os usuários.' : 'Suas tarefas.'}</span>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditing(null); setFormOpen(true); }}>
            Nova tarefa
          </Button>
        </div>

        <div className="summary">
          {cards.map((c) => (
            <button
              key={c.key}
              aria-pressed={(filter.status ?? 'all') === c.key}
              style={{ borderLeftColor: STATUS_COLOR[c.key] }}
              onClick={() => patchFilter({ status: c.key === 'all' ? undefined : c.key })}
            >
              <b>{c.value ?? '–'}</b>
              <small>{c.label}</small>
            </button>
          ))}
        </div>

        <div className="filters">
          <Input.Search
            allowClear
            placeholder="Buscar por título ou descrição"
            style={{ width: 320 }}
            onSearch={(v) => patchFilter({ search: v.trim() || undefined })}
          />
          <Select
            allowClear
            placeholder="Prioridade"
            style={{ width: 160 }}
            value={filter.priority}
            options={options(PRIORITY_LABEL)}
            onChange={(v) => patchFilter({ priority: v })}
          />
        </div>

        <Table<Task>
          rowKey="id"
          loading={loading}
          columns={columns}
          dataSource={data?.items ?? []}
          locale={{ emptyText: 'Nenhuma tarefa encontrada. Crie a primeira em "Nova tarefa".' }}
          scroll={{ x: 800 }}
          pagination={{
            current: filter.page,
            pageSize: filter.pageSize,
            total: data?.totalItems ?? 0,
            showSizeChanger: true,
            pageSizeOptions: [10, 20, 50],
            showTotal: (t) => `${t} tarefa(s)`,
            onChange: (page, pageSize) => setFilter((f) => ({ ...f, page, pageSize })),
          }}
        />
      </main>

      <TaskFormModal open={formOpen} task={editing} saving={saving} onCancel={() => setFormOpen(false)} onSubmit={save} />
    </>
  );
}
