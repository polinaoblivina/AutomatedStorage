import { Client } from 'pg'
import { adminConnection } from './env'

export default async function globalTeardown() {
  const { databaseToDrop, ...connection } = adminConnection()
  const client = new Client(connection)

  await client.connect()
  await client.query(`DROP DATABASE IF EXISTS "${databaseToDrop}" WITH (FORCE)`)
  await client.end()
}
