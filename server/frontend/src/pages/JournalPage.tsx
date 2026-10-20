import { useEffect, useState } from 'react'
import { Alert, Flex, Select, Table, Tag, Typography } from 'antd'
import { api } from '../api/client'
import type { PagedList, Transaction } from '../api/types'
import { errorText, formatDateTime, formatGrams } from '../utils/format'

const PAGE_SIZE = 20

const typeLabels: Record<Transaction['type'], { text: string; color: string }> = {
  Withdrawal: { text: 'Взято', color: 'orange' },
  Replenishment: { text: 'Пополнено', color: 'green' },
  Adjustment: { text: 'Инвентаризация', color: 'blue' },
}

export function JournalPage() {
  const [page, setPage] = useState(1)
  const [type, setType] = useState<Transaction['type'] | undefined>(undefined)
  const [data, setData] = useState<PagedList<Transaction> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const query = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
    if (type) query.set('type', type)

    api<PagedList<Transaction>>(`/transactions?${query}`)
      .then((result) => {
        setData(result)
        setError(null)
      })
      .catch((e) => setError(errorText(e)))
      .finally(() => setLoading(false))
  }, [page, type])

  function handleTypeChange(value: Transaction['type'] | undefined) {
    setLoading(true)
    setType(value)
    setPage(1)
  }

  function handlePageChange(value: number) {
    setLoading(true)
    setPage(value)
  }

  return (
    <Flex vertical gap={16}>
      <Flex align="center" justify="space-between" gap={16} wrap>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Журнал операций
        </Typography.Title>
        <Select
          allowClear
          placeholder="Все операции"
          style={{ width: 220 }}
          value={type}
          onChange={handleTypeChange}
          options={Object.entries(typeLabels).map(([value, label]) => ({ value, label: label.text }))}
        />
      </Flex>

      {error && <Alert type="error" title={error} />}

      <Table<Transaction>
        rowKey="id"
        loading={loading}
        dataSource={data?.items ?? []}
        size="small"
        pagination={{
          current: page,
          pageSize: PAGE_SIZE,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          onChange: handlePageChange,
        }}
        columns={[
          { title: 'Время', dataIndex: 'createdAtUtc', render: formatDateTime },
          { title: 'Сотрудник', dataIndex: 'userFullName', render: (name) => name ?? '—' },
          { title: 'Предмет', dataIndex: 'itemName', render: (name) => name ?? '—' },
          {
            title: 'Где',
            render: (_, t) => [t.cabinetName, t.cellName].filter(Boolean).join(' / ') || '—',
          },
          {
            title: 'Операция',
            dataIndex: 'type',
            render: (value: Transaction['type']) => (
              <Tag color={typeLabels[value]?.color}>{typeLabels[value]?.text ?? value}</Tag>
            ),
          },
          { title: 'Кол-во', dataIndex: 'quantity' },
          {
            title: 'Вес до → после',
            render: (_, t) => `${formatGrams(t.weightBefore)} → ${formatGrams(t.weightAfter)}`,
          },
        ]}
      />
    </Flex>
  )
}
