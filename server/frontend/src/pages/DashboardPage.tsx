import { useEffect, useState } from 'react'
import { Alert, Button, Card, Col, Empty, Progress, Row, Spin, Table, Tag } from 'antd'
import { useNavigate } from 'react-router'
import { api } from '../api/client'
import type { Dashboard, DeviceStatus, PagedList, RankedEntry, Transaction } from '../api/types'
import { MovementChart } from '../components/MovementChart'
import { PageHeader } from '../components/PageHeader'
import { StatCard } from '../components/StatCard'
import { StatusDot } from '../components/StatusDot'
import { colors } from '../theme'
import { errorText, formatDateTime, formatPlace, formatTime } from '../utils/format'
import { signedQuantity, transactionTypes } from '../utils/labels'

const DAYS = 14
const RECENT = 8

function RankedList({ data }: { data: RankedEntry[] }) {
  if (data.length === 0) return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Нет данных" />
  const max = Math.max(...data.map((entry) => entry.quantity))

  return (
    <div>
      {data.map((entry, index) => (
        <div key={entry.id} style={{ padding: '8px 0' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
            <span>
              <span className="muted" style={{ marginRight: 10 }}>
                {index + 1}
              </span>
              {entry.name}
            </span>
            <span className="muted">
              {entry.quantity} шт., {entry.operations} опер.
            </span>
          </div>
          <Progress percent={(entry.quantity / max) * 100} showInfo={false} size="small" strokeColor={colors.primary} />
        </div>
      ))}
    </div>
  )
}

function deviceState(device: DeviceStatus) {
  if (!device.lastSeenUtc) return 'нет сигнала'
  if (!device.isOnline) return `был в ${formatTime(device.lastSeenUtc)}`
  return device.state === 'SessionOpen' ? 'дверь открыта' : 'в сети'
}

export function DashboardPage() {
  const navigate = useNavigate()
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [devices, setDevices] = useState<DeviceStatus[]>([])
  const [recent, setRecent] = useState<Transaction[]>([])
  const [error, setError] = useState<string | null>(null)
  const [today] = useState(() => new Date().toLocaleDateString('ru-RU', { weekday: 'long', day: 'numeric', month: 'long' }))

  useEffect(() => {
    const offset = -new Date().getTimezoneOffset()
    api<Dashboard>(`/statistics/dashboard?days=${DAYS}&timezoneOffsetMinutes=${offset}`)
      .then(setDashboard)
      .catch((e) => setError(errorText(e)))
    api<DeviceStatus[]>('/devices')
      .then(setDevices)
      .catch(() => setDevices([]))
    api<PagedList<Transaction>>(`/transactions?page=1&pageSize=${RECENT}`)
      .then((result) => setRecent(result.items))
      .catch(() => setRecent([]))
  }, [])

  if (error) return <Alert type="error" title={error} />
  if (!dashboard) return <Spin size="large" style={{ display: 'block', marginTop: 120 }} />

  const { totals } = dashboard
  const online = devices.filter((d) => d.isOnline).length
  const busyCells = totals.cells - totals.freeCells

  return (
    <>
      <PageHeader title="Сводка" subtitle={`Сегодня ${today}`} />

      <Row gutter={[16, 16]}>
        <Col xs={12} xl={6}>
          <StatCard
            label="Шкафы на связи"
            value={`${online} из ${devices.length}`}
            hint={devices.length === 0 ? 'ещё не подключались' : 'по последнему сигналу'}
            accent={online > 0 ? colors.success : colors.muted}
          />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard label="Операций сегодня" value={totals.operationsToday} hint={`взято ${totals.withdrawnToday} шт.`} />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard
            label="Заканчиваются"
            value={totals.lowStockItems}
            hint="товаров ниже минимума"
            accent={totals.lowStockItems > 0 ? colors.danger : colors.success}
          />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard label="Активных сотрудников" value={totals.activeUsers} accent="#2563eb" />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard label="Шкафов" value={totals.cabinets} accent={colors.sidebar} />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard
            label="Ячеек занято"
            value={`${busyCells} из ${totals.cells}`}
            hint={`свободно ${totals.freeCells}`}
            accent={colors.sidebar}
          />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard label="Товаров" value={totals.items} accent={colors.sidebar} />
        </Col>
        <Col xs={12} xl={6}>
          <StatCard label="Единиц на складе" value={totals.totalUnits} accent={colors.sidebar} />
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} xl={16}>
          <Card
            title="Последние операции"
            extra={
              <Button type="link" onClick={() => navigate('/journal')}>
                Весь журнал
              </Button>
            }
            styles={{ body: { padding: 0, overflowX: 'auto' } }}
          >
            <Table<Transaction>
              rowKey="id"
              dataSource={recent}
              pagination={false}
              locale={{ emptyText: 'Операций пока не было' }}
              columns={[
                { title: 'Время', dataIndex: 'createdAtUtc', render: formatDateTime, width: 170 },
                { title: 'Сотрудник', dataIndex: 'userFullName', render: (name) => name ?? '—' },
                { title: 'Товар', dataIndex: 'itemName', render: (name) => name ?? '—' },
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
              ]}
            />
          </Card>
        </Col>
        <Col xs={24} xl={8}>
          <Card
            title="Шкафы"
            extra={
              <Button type="link" onClick={() => navigate('/devices')}>
                Подробнее
              </Button>
            }
            style={{ height: '100%' }}
          >
            {devices.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Ни один шкаф не выходил на связь" />
            ) : (
              devices.map((device) => (
                <div key={device.name} className="list-row">
                  <StatusDot tone={device.isOnline ? 'success' : 'muted'} pulse={device.isOnline}>
                    <strong>{device.name}</strong>
                  </StatusDot>
                  <span className="muted">{deviceState(device)}</span>
                </div>
              ))
            )}
          </Card>
        </Col>
      </Row>

      <Card title={`Движение за ${DAYS} дней`} style={{ marginTop: 16 }}>
        <MovementChart days={dashboard.daily} />
      </Card>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} xl={8}>
          <Card title="Заканчиваются" style={{ height: '100%' }}>
            {dashboard.lowStock.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Всего хватает" />
            ) : (
              dashboard.lowStock.map((item) => (
                <div key={item.id} className="list-row">
                  <div>
                    <div>{item.name}</div>
                    <div className="muted" style={{ fontSize: 12 }}>
                      {formatPlace(item.cabinetName, item.cellName)}
                    </div>
                  </div>
                  <span style={{ whiteSpace: 'nowrap' }}>
                    <strong style={{ color: colors.danger }}>{item.stockQuantity}</strong>
                    <span className="muted"> / мин. {item.minQuantity}</span>
                  </span>
                </div>
              ))
            )}
          </Card>
        </Col>
        <Col xs={24} md={12} xl={8}>
          <Card title={`Чаще всего берут, ${DAYS} дн.`} style={{ height: '100%' }}>
            <RankedList data={dashboard.topItems} />
          </Card>
        </Col>
        <Col xs={24} md={12} xl={8}>
          <Card title={`Кто больше всех берёт, ${DAYS} дн.`} style={{ height: '100%' }}>
            <RankedList data={dashboard.topUsers} />
          </Card>
        </Col>
      </Row>
    </>
  )
}
