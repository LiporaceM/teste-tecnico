import { useEffect } from 'react';
import { DatePicker, Form, Input, Modal, Select } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { PRIORITY_LABEL, STATUS_LABEL, type Task, type TaskInput, type TaskPriority, type TaskStatus } from '../types';

interface Props {
  open: boolean;
  task: Task | null;
  saving: boolean;
  onCancel: () => void;
  onSubmit: (input: TaskInput) => void;
}

interface FormValues {
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: Dayjs | null;
}

const toOptions = (labels: Record<string, string>) =>
  Object.entries(labels).map(([value, label]) => ({ value, label }));

export default function TaskFormModal({ open, task, saving, onCancel, onSubmit }: Props) {
  const [form] = Form.useForm<FormValues>();

  useEffect(() => {
    if (!open) return;
    form.setFieldsValue(
      task
        ? { ...task, description: task.description ?? undefined, dueDate: task.dueDate ? dayjs(task.dueDate) : null }
        : { title: '', description: undefined, status: 'Pending', priority: 'Medium', dueDate: null },
    );
  }, [open, task, form]);

  return (
    <Modal
      open={open}
      title={task ? 'Editar tarefa' : 'Nova tarefa'}
      okText="Salvar tarefa"
      cancelText="Cancelar"
      confirmLoading={saving}
      onCancel={onCancel}
      onOk={() => form.submit()}
      destroyOnClose
      forceRender
    >
      <Form
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={(v) =>
          onSubmit({
            title: v.title.trim(),
            description: v.description?.trim() || null,
            status: v.status,
            priority: v.priority,
            dueDate: v.dueDate ? v.dueDate.startOf('day').toISOString() : null,
          })
        }
      >
        <Form.Item
          name="title"
          label="Título"
          rules={[
            { required: true, whitespace: true, message: 'Informe o título.' },
            { max: 150, message: 'No máximo 150 caracteres.' },
          ]}
        >
          <Input />
        </Form.Item>
        <Form.Item name="description" label="Descrição" rules={[{ max: 2000, message: 'No máximo 2000 caracteres.' }]}>
          <Input.TextArea rows={3} />
        </Form.Item>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
          <Form.Item name="status" label="Status">
            <Select options={toOptions(STATUS_LABEL)} />
          </Form.Item>
          <Form.Item name="priority" label="Prioridade">
            <Select options={toOptions(PRIORITY_LABEL)} />
          </Form.Item>
          <Form.Item name="dueDate" label="Vencimento">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
        </div>
      </Form>
    </Modal>
  );
}
