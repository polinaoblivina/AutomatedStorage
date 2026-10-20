import { useEffect, useState } from 'react'
import { Alert, Card, Col, Flex, Row, Spin, Statistic, Table, Typography } from 'antd'
import { api } from '../api/client'
import type { Dashboard, Item, RankedEntry } from '../api/types'
import { errorText } from '../utils/format'

const DAYS = 14

function RankedTable({ title, data }: { title: string; data: RankedEntry[] }) {
  return (
    <Card title={title} size="small">
      <Table<RankedEntry>
        rowKey="id"
        dataSource={data}
        pagination={false}
        size="small"
        locale={{ emptyText: 'Нет данных' }}
        columns={[
          { title: 'Название', dataIndex: 'name' },
          { title: 'Взято, шт.', dataIndex: 'quantity' },
          { title: 'Операций', dataIndex: 'operations' },
        ]}
      />
    </Card>
  )
}

export function DashboardPage() {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const offset = -new Date().getTimezoneOffset()
    api<Dashboard>(`/statistics/dashboard?days=${DAYS}&timezoneOffsetMinutes=${offset}`)
      .then(setDashboard)
      .catch((e) => setError(errorText(e)))
  }, [])

  if (error) return <Alert type="error" title={error} />
  if (!dashboard) return <Spin size="large" style={{ display: 'block', marginTop: 120 }} />

  const { totals } = dashboard
  const tiles = [
    { title: 'Шкафов', value: totals.cabinets },
    { title: 'Ячеек (свободных)', value: `${totals.cells} (${totals.freeCells})` },
    { title: 'Предметов', value: totals.items },
    { title: 'Единиц на складе', value: totals.totalUnits },
    { title: 'Заканчиваются', value: totals.lowStockItems },
    { title: 'Активных сотрудников', value: totals.activeUsers },
    { title: 'Взято сегодня, шт.', value: totals.withdrawnToday },
    { title: 'Операций сегодня', value: totals.operationsToday },
  ]

  return (
    <Flex vertical gap={16}>
      <Typography.Title level={3} style={{ margin: 0 }}>
        Сводка
      </Typography.Title>

      <Row gutter={[16, 16]}>
        {tiles.map((tile) => (
          <Col key={tile.title} xs={12} md={6}>
            <Card size="small">
              <Statistic title={tile.title} value={tile.value} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card title="Заканчиваются" size="small">
        <Table<Item>
          rowKey="id"
          dataSource={dashboard.lowStock}
          pagination={false}
          size="small"
          locale={{ emptyText: 'Всего хватает' }}
          columns={[
            { title: 'Предмет', dataIndex: 'name' },
            {
              title: 'Где',
              render: (_, item) => [item.cabinetName, item.cellName].filter(Boolean).join(' / ') || '—',
            },
            { title: 'Осталось', dataIndex: 'stockQuantity' },
            { title: 'Минимум', dataIndex: 'minQuantity' },
          ]}
        />
      </Card>

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <RankedTable title={`Чаще всего берут (за ${DAYS} дней)`} data={dashboard.topItems} />
        </Col>
        <Col xs={24} lg={12}>
          <RankedTable title={`Кто больше всех берёт (за ${DAYS} дней)`} data={dashboard.topUsers} />
        </Col>
      </Row>
    </Flex>
  )
}
