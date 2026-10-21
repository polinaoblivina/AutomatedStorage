import { useEffect, useState } from 'react'
import { Alert, Card, Col, Empty, Row, Table, Tag } from 'antd'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { api, getFreshAccessToken } from '../api/client'
import type { DeviceEvent, DeviceStatus } from '../api/types'
import { PageHeader } from '../components/PageHeader'
import { StatusDot } from '../components/StatusDot'
import { errorText, formatDateTime, formatGrams, formatTime } from '../utils/format'

const EVENTS_LIMIT = 20

function byName(a: DeviceStatus, b: DeviceStatus) {
  return a.name.localeCompare(b.name)
}

function sensor(value: boolean | null) {
  if (value === null) return <StatusDot tone="muted">нет данных</StatusDot>
  return value ? <StatusDot tone="success">норма</StatusDot> : <StatusDot tone="danger">ошибка</StatusDot>
}

function DeviceCard({ device }: { device: DeviceStatus }) {
  const sessionOpen = device.isOnline && device.state === 'SessionOpen'

  return (
    <Card
      title={
        <StatusDot tone={device.isOnline ? 'success' : 'muted'} pulse={device.isOnline}>
          {device.name}
        </StatusDot>
      }
      extra={
        device.isOnline ? (
          <Tag color={sessionOpen ? 'orange' : 'green'}>{sessionOpen ? 'дверь открыта' : 'в сети'}</Tag>
        ) : (
          <Tag>не в сети</Tag>
        )
      }
      style={{ height: '100%', opacity: device.isOnline ? 1 : 0.75 }}
    >
      <div className="muted" style={{ fontSize: 12 }}>
        Общий вес
      </div>
      <div className="device-weight">{formatGrams(device.weightGrams)}</div>

      <div style={{ marginTop: 12 }}>
        <div className="device-row">
          <span className="muted">Считыватель карт</span>
          {sensor(device.rfidOk)}
        </div>
        <div className="device-row">
          <span className="muted">Весы</span>
          {sensor(device.scaleOk)}
        </div>
        {(device.scales ?? []).map((scale, index) => (
          <div key={scale.cellId ?? index} className="device-row">
            <span className="muted">Весы {index + 1}</span>
            <StatusDot tone={scale.ok ? 'success' : 'danger'}>{formatGrams(scale.weightGrams)}</StatusDot>
          </div>
        ))}
        <div className="device-row">
          <span className="muted">Последний сигнал</span>
          <span>{formatDateTime(device.lastSeenUtc)}</span>
        </div>
        <div className="device-row">
          <span className="muted">Прошивка</span>
          <span className="mono">{device.firmwareVersion ?? '—'}</span>
        </div>
        <div className="device-row">
          <span className="muted">Адрес</span>
          <span className="mono">{device.remoteAddress ?? '—'}</span>
        </div>
      </div>

      {device.lastEvent && (
        <div style={{ marginTop: 12, padding: '10px 12px', background: '#f8fafc', borderRadius: 8 }}>
          <div className="muted" style={{ fontSize: 12 }}>
            Последнее событие, {formatTime(device.lastEvent.occurredAtUtc)}
          </div>
          <div>{device.lastEvent.message}</div>
        </div>
      )}
    </Card>
  )
}

export function DevicesPage() {
  const [devices, setDevices] = useState<DeviceStatus[]>([])
  const [events, setEvents] = useState<DeviceEvent[]>([])
  const [error, setError] = useState<string | null>(null)
  const [live, setLive] = useState(false)

  useEffect(() => {
    api<DeviceStatus[]>('/devices')
      .then((list) => setDevices([...list].sort(byName)))
      .catch((e) => setError(errorText(e)))
    api<DeviceEvent[]>(`/devices/events?take=${EVENTS_LIMIT}`)
      .then(setEvents)
      .catch((e) => setError(errorText(e)))

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/devices', { accessTokenFactory: getFreshAccessToken })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('DeviceStatusChanged', (status: DeviceStatus) => {
      setDevices((current) => [status, ...current.filter((d) => d.name !== status.name)].sort(byName))
    })
    connection.on('DeviceEventRecorded', (event: DeviceEvent) => {
      setEvents((current) => [event, ...current].slice(0, EVENTS_LIMIT))
    })
    connection.onreconnecting(() => setLive(false))
    connection.onreconnected(() => setLive(true))
    connection.onclose(() => setLive(false))

    connection
      .start()
      .then(() => setLive(true))
      .catch(() => setLive(false))

    return () => {
      connection.stop()
    }
  }, [])

  const online = devices.filter((d) => d.isOnline).length

  return (
    <>
      <PageHeader
        title="Шкафы на связи"
        subtitle={`В сети ${online} из ${devices.length}`}
        extra={
          <StatusDot tone={live ? 'success' : 'muted'} pulse={live}>
            {live ? 'Обновляется вживую' : 'Нет живого соединения'}
          </StatusDot>
        }
      />

      {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}

      {devices.length === 0 ? (
        <Card>
          <Empty description="Ни одно устройство ещё не выходило на связь" />
        </Card>
      ) : (
        <Row gutter={[16, 16]}>
          {devices.map((device) => (
            <Col key={device.name} xs={24} md={12} xl={8}>
              <DeviceCard device={device} />
            </Col>
          ))}
        </Row>
      )}

      <Card title="Последние события" style={{ marginTop: 16 }} styles={{ body: { padding: 0, overflowX: 'auto' } }}>
        <Table<DeviceEvent>
          rowKey={(event) => `${event.occurredAtUtc}-${event.deviceName}-${event.type}`}
          dataSource={events}
          pagination={false}
          locale={{ emptyText: 'Событий пока нет' }}
          columns={[
            { title: 'Время', dataIndex: 'occurredAtUtc', render: formatDateTime, width: 170 },
            { title: 'Шкаф', dataIndex: 'deviceName' },
            { title: 'Тип', dataIndex: 'type', render: (type) => (type === 'CardScan' ? 'Карта' : 'Сессия') },
            {
              title: 'Кто',
              render: (_, event) => event.userName ?? <span className="mono">{event.cardUid ?? '—'}</span>,
            },
            {
              title: 'Результат',
              dataIndex: 'success',
              render: (success) => (success ? <Tag color="green">успех</Tag> : <Tag color="red">отказ</Tag>),
            },
            { title: 'Сообщение', dataIndex: 'message' },
          ]}
        />
      </Card>
    </>
  )
}
