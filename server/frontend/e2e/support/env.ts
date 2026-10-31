import { execFileSync } from 'node:child_process'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const webApiProject = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../ServerAutomatedStorage.WebAPI')

export const e2ePort = 5090

function readUserSecrets(): Record<string, string> {
  const output = execFileSync('dotnet', ['user-secrets', 'list', '--project', webApiProject], { encoding: 'utf8' })
  const secrets: Record<string, string> = {}

  for (const line of output.split(/\r?\n/)) {
    const separator = line.indexOf(' = ')
    if (separator > 0) secrets[line.slice(0, separator).trim()] = line.slice(separator + 3).trim()
  }

  return secrets
}

export function prepareEnvironment() {
  if (process.env.E2E_ADMIN_PASSWORD) return

  const secrets = readUserSecrets()
  process.env.E2E_DATABASE = process.env.E2E_DATABASE ?? `smartstorage_e2e_${Date.now().toString(36)}`
  process.env.E2E_ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@storage.local'
  process.env.E2E_ADMIN_PASSWORD = secrets['Seed:AdminPassword']
  process.env.E2E_DEVICE_KEY = secrets['Devices:0:ApiKey']
  process.env.E2E_CONNECTION_STRING = secrets['ConnectionStrings:Postgres'].replace(
    /(Database|Initial Catalog)=[^;]*/i,
    `Database=${process.env.E2E_DATABASE}`,
  )
}

export function serverEnvironment(): Record<string, string> {
  prepareEnvironment()

  return {
    ASPNETCORE_ENVIRONMENT: 'Development',
    ConnectionStrings__Postgres: process.env.E2E_CONNECTION_STRING as string,
  }
}

export function credentials() {
  prepareEnvironment()

  return {
    adminEmail: process.env.E2E_ADMIN_EMAIL as string,
    adminPassword: process.env.E2E_ADMIN_PASSWORD as string,
    deviceKey: process.env.E2E_DEVICE_KEY as string,
  }
}

export function adminConnection() {
  prepareEnvironment()

  const parts = Object.fromEntries(
    (process.env.E2E_CONNECTION_STRING as string).split(';').map((pair) => {
      const separator = pair.indexOf('=')
      return [pair.slice(0, separator).trim().toLowerCase(), pair.slice(separator + 1)]
    }),
  )

  return {
    host: parts.host,
    port: Number(parts.port ?? 5432),
    user: parts.username,
    password: parts.password,
    database: 'postgres',
    databaseToDrop: process.env.E2E_DATABASE as string,
  }
}
