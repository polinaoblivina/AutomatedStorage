import { Tooltip } from 'antd'
import type { Dashboard } from '../api/types'
import { colors } from '../theme'

type Day = Dashboard['daily'][number]

function dayLabel(date: string) {
  const [, month, day] = date.split('-')
  return `${day}.${month}`
}

export function MovementChart({ days }: { days: Day[] }) {
  const max = Math.max(1, ...days.map((d) => Math.max(d.withdrawn, d.replenished)))

  return (
    <div>
      <div style={{ display: 'flex', gap: 16, marginBottom: 8 }}>
        <span className="legend">
          <span className="legend-swatch" style={{ background: colors.warning }} />
          Взято, шт.
        </span>
        <span className="legend">
          <span className="legend-swatch" style={{ background: colors.primary }} />
          Пополнено, шт.
        </span>
      </div>
      <div className="chart">
        {days.map((day) => (
          <Tooltip
            key={day.date}
            title={`${dayLabel(day.date)}: взято ${day.withdrawn}, пополнено ${day.replenished}, операций ${day.operations}`}
          >
            <div className="chart-col">
              <div className="chart-bars">
                <div
                  className="chart-bar"
                  style={{ height: `${(day.withdrawn / max) * 100}%`, background: colors.warning }}
                />
                <div
                  className="chart-bar"
                  style={{ height: `${(day.replenished / max) * 100}%`, background: colors.primary }}
                />
              </div>
              <div className="chart-label">{dayLabel(day.date)}</div>
            </div>
          </Tooltip>
        ))}
      </div>
    </div>
  )
}
