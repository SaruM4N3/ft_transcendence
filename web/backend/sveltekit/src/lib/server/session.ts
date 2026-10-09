import { createHash, randomBytes } from 'node:crypto';
import type { Cookies } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';

export const SESSION_COOKIE = 'session';
const SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000;

const hashToken = (token: string) =>
    createHash('sha256').update(token).digest('hex');

export async function createSession(userId: string, cookies: Cookies) {
    const token = randomBytes(32).toString('hex');
    const expiresAt = new Date(Date.now() + SESSION_TTL_MS);

    await prisma.session.create({
        data: { id: hashToken(token), userId, expiresAt }
    });

    cookies.set(SESSION_COOKIE, token, {
        httpOnly: true,
        secure: true,
        sameSite: 'lax',
        path: '/',
        expires: expiresAt
    });
}

export async function validateSession(token: string) {
    const session = await prisma.session.findFirst({
        where: { id: hashToken(token), expiresAt: { gt: new Date() } },
        select: {
            user: { select: { id: true, email: true, displayName: true } }
        }
    });
    return session ? {
        id: session.user.id,
        email: session.user.email,
        display_name: session.user.displayName
    } : null;
}

export async function deleteSession(token: string) {
    await prisma.session.deleteMany({ where: { id: hashToken(token) } });
}
