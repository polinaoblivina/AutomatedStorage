import { useEffect, useState } from 'react'
import { Alert, Button, Card, DatePicker, Flex, Segmented, Select, Table, Tag } from 'antd'
import type { Dayjs } from 'dayjs'
import { api } from '../api/client'
import type { Item, PagedList, Transaction, User } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'
import { PageHeader } from '../components/PageHeader'
import { errorText, formatDateTime, formatGrams, formatPlace } from '../utils/format'
import { signedQuantity, transactionTypes } from '../utils/labels'

type TypeFilter = Transaction['type'] | 'All'
type Range = [Dayjs | null, Dayjs | null] | null

interface Filters {
  type: TypeFilter
  range: Range
  userId?: string
  itemId?: string
}

const typeOptions = [
  { value: 'All', label: 'Все' },
  ...Object.entries(transactionTypes).map(([value, label]) => ({ value, label: label.text })),
]

export function JournalPage() {
  const { user } = useAuth()
  const [filters, setFilters] = useState<Filters>({ type: 'All', range: null })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [reloadKey, setReloadKey] = useState(0)
  const [data, setData] = useState<PagedList<Transaction> | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const canSeeUsers = can(user, 'ManageUsers')

  useEffect(() => {
    api<Item[]>('/items?includeArchived=true')
      .then(setItems)
      .catch(() => setItems([]))
    if (canSeeUsers) {
      api<User[]>('/users')
        .then(setUsers)
        .catch(() => setUsers([]))
    }
  }, [canSeeUsers])

  useEffect(() => {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (filters.type !== 'All') query.set('type', filters.type)
    if (filters.userId) query.set('userId', filters.userId)
    if (filters.itemId) query.set('itemId', filters.itemId)
    const [from, to] = filters.range ?? [null, null]
    if (from) query.set('fromUtc', from.startOf('day').toISOString())
    if (to) query.set('toUtc', to.endOf('day').toISOString())

    api<PagedList<Transaction>>(`/transactions?${query}`)
      .then((result) => {
        setData(result)
        setError(null)
      })
      .catch((e) => setError(errorText(e)))
      .finally(() => setLoading(false))
  }, [filters, page, pageSize, reloadKey])

  function changeFilters(patch: Partial<Filters>) {
    setLoading(true)
    setFilters((current) => ({ ...current, ...patch }))
    setPage(1)
  }

  function handlePageChange(nextPage: number, nextSize: number) {
    setLoading(true)
    setPage(nextSize === pageSize ? nextPage : 1)
    setPageSize(nextSize)
  }

  function reload() {
    setLoading(true)
    setReloadKey((key) => key + 1)
  }

  const hasFilters = filters.type !== 'All' || filters.range !== null || filters.userId || filters.itemId

  return (
    <>
      <PageHeader
        title="Журнал операций"
        subtitle={data ? `Найдено записей: ${data.totalCount}` : 'Загрузка'}
        extra={<Button onClick={reload}>Обновить</Button>}
      />

      <Card style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap align="center">
          <Segmented<string>
            options={typeOptions}
            value={filters.type}
            onChange={(value) => changeFilters({ type: value as TypeFilter })}
          />
          <DatePicker.RangePicker
            value={filters.range}
            format="DD.MM.YYYY"
            suffixIcon={null}
            placeholder={['Начало', 'Конец']}
            separator={<span className="muted">—</span>}
            onChange={(range) => changeFilters({ range })}
          />
          <Select
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Товар"
            style={{ minWidth: 200 }}
            value={filters.itemId}
            onChange={(itemId) => changeFilters({ itemId })}
            options={items.map((item) => ({ value: item.id, label: item.name }))}
          />
          {canSeeUsers && (
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Сотрудник"
              style={{ minWidth: 200 }}
              value={filters.userId}
              onChange={(userId) => changeFilters({ userId })}
              options={users.map((u) => ({ value: u.id, label: u.fullName }))}
            />
          )}
          {hasFilters && (
            <Button type="link" onClick={() => changeFilters({ type: 'All', range: null, userId: undefined, itemId: undefined })}>
              Сбросить
            </Button>
          )}
        </Flex>
      </Card>

      {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}

      <Card styles={{ body: { padding: 0, overflowX: 'auto' } }}>
        <Table<Transaction>
          rowKey="id"
          loading={loading}
          dataSource={data?.items ?? []}
          locale={{ emptyText: 'Операций не найдено' }}
          pagination={{
            current: page,
            pageSize,
            total: data?.totalCount ?? 0,
            showSizeChanger: true,
            pageSizeOptions: [20, 50, 100],
            onChange: handlePageChange,
            style: { padding: '0 16px' },
          }}
          columns={[
            { title: 'Время', dataIndex: 'createdAtUtc', render: formatDateTime, width: 170 },
            { title: 'Сотрудник', dataIndex: 'userFullName', render: (name) => name ?? '—' },
            { title: 'Товар', dataIndex: 'itemName', render: (name) => <strong>{name ?? '—'}</strong> },
            { title: 'Где', render: (_, t) => <span className="muted">{formatPlace(t.cabinetName, t.cellName)}</span> },
            {
              title: 'Операция',
              dataIndex: 'type',
              render: (value: Transaction['type']) => (
                <Tag color={transactionTypes[value].tag}>{transactionTypes[value].text}</Tag>
              ),
            },
            {
              title: 'Шт.',
              align: 'right',
              render: (_, t) => <strong style={{ color: transactionTypes[t.type].color }}>{signedQuantity(t)}</strong>,
            },
            { title: 'Вес до', align: 'right', dataIndex: 'weightBefore', render: formatGrams },
            { title: 'Вес после', align: 'right', dataIndex: 'weightAfter', render: formatGrams },
          ]}
        />
      </Card>
    </>
  )
}
