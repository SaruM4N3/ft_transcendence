import dotenv from 'dotenv';
import { defineConfig } from 'prisma/config';

dotenv.config({ path: '../../.env' });

const databaseUrl = process.env.DATABASE_URL ?? (() => {
	const url = new URL(`postgresql://${process.env.POSTGRES_HOST || 'db'}:${process.env.POSTGRES_PORT || 5432}`);
	url.username = process.env.POSTGRES_USER || '';
	url.password = process.env.POSTGRES_PASSWORD || '';
	url.pathname = `/${process.env.POSTGRES_DB || ''}`;
	return url.toString();
})();

export default defineConfig({
	schema: 'prisma/schema.prisma',
	migrations: { path: 'prisma/migrations' },
	datasource: { url: databaseUrl }
});
