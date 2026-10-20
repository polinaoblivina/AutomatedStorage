import { useEffect, useState } from 'react'
import { Alert, Badge, Card, Col, Descriptions, Empty, Flex, Row, Table, Tag, Typography } from 'antd'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { api, getFreshAccessToken } from '../api/client'
import type { DeviceEvent, DeviceStatus } from '../api/types'
import { errorText, formatDateTime, formatGrams } from '../utils/format'

const EVENTS_LIMIT = 20

function byName(a: DeviceStatus, b: DeviceStatus) {
  return a.name.localeCompare(b.name)
}

function okTag(value: boolean | null) {
  if (value === null) return <Tag>нет данных</Tag>
  return value ? <Tag color="green">норма</Tag> : <Tag color="red">ошибка</Tag>
}

function DeviceCard({ device }: { device: DeviceStatus }) {
  return (
    <Card
      title={device.name}
      extra={device.isOnline ? <Tag color="green">в сети</Tag> : <Tag color="default">не в сети</Tag>}
    >
      <Descriptions
        column={1}
        size="small"
        items={[
          { label: 'Последний сигнал', children: formatDateTime(device.lastSeenUtc) },
          { label: 'Состояние', children: device.state === 'SessionOpen' ? 'Дверь открыта' : 'Ожидание' },
          { label: 'Считыватель карт', children: okTag(device.rfidOk) },
          { label: 'Весы', children: okTag(device.scaleOk) },
          ...(device.scales ?? []).map((scale, index) => ({
            label: `Весы ${index + 1}`,
            children: `${formatGrams(scale.weightGrams)}${scale.ok ? '' : ' (ошибка)'}`,
          })),
          { label: 'Прошивка', children: device.firmwareVersion ?? '—' },
          { label: 'Последнее событие', children: device.lastEvent?.message ?? '—' },
        ]}
      />
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

  return (
    <Flex vertical gap={24}>
      <Flex align="center" gap={16}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Шкафы
        </Typography.Title>
        <Badge status={live ? 'success' : 'default'} text={live ? 'обновляется вживую' : 'нет живого соединения'} />
      </Flex>

      {error && <Alert type="error" title={error} />}

      {devices.length === 0 ? (
        <Empty description="Ни одно устройство ещё не выходило на связь" />
      ) : (
        <Row gutter={[16, 16]}>
          {devices.map((device) => (
            <Col key={device.name} xs={24} md={12} xl={8}>
              <DeviceCard device={device} />
            </Col>
          ))}
        </Row>
      )}

      <Typography.Title level={4} style={{ margin: 0 }}>
        Последние события
      </Typography.Title>
      <Table<DeviceEvent>
        rowKey={(event) => `${event.occurredAtUtc}-${event.deviceName}-${event.type}`}
        dataSource={events}
        pagination={false}
        size="small"
        columns={[
          { title: 'Время', dataIndex: 'occurredAtUtc', render: formatDateTime },
          { title: 'Устройство', dataIndex: 'deviceName' },
          { title: 'Тип', dataIndex: 'type', render: (type) => (type === 'CardScan' ? 'Карта' : 'Сессия') },
          { title: 'Пользователь', render: (_, event) => event.userName ?? event.cardUid ?? '—' },
          {
            title: 'Результат',
            dataIndex: 'success',
            render: (success) => (success ? <Tag color="green">успех</Tag> : <Tag color="red">отказ</Tag>),
          },
          { title: 'Сообщение', dataIndex: 'message' },
        ]}
      />
    </Flex>
  )
}
