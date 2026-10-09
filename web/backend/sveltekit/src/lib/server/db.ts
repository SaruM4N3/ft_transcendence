import { PrismaPg } from '@prisma/adapter-pg';
import { env } from '$env/dynamic/private';
import { PrismaClient } from './generated/prisma/client';

const adapter = new PrismaPg({
	host: env.POSTGRES_HOST || 'db',
	port: Number(env.POSTGRES_PORT || 5432),
	user: env.POSTGRES_USER,
	password: env.POSTGRES_PASSWORD,
	database: env.POSTGRES_DB
});

export const prisma = new PrismaClient({ adapter });
