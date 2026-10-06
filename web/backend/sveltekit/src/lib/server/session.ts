import { createHash, randomBytes } from 'node:crypto';
import type { Cookies } from '@sveltejs/kit';
import { pool } from '$lib/server/db.js';

export const SESSION_COOKIE = 'session';
const SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000;

const hashToken = (token: string) =>
    createHash('sha256').update(token).digest('hex');

export async function createSession(userId: string, cookies: Cookies) {
    const token = randomBytes(32).toString('hex');
    const expiresAt = new Date(Date.now() + SESSION_TTL_MS);

    await pool.query(
        `INSERT INTO sessions (id, user_id, expires_at) VALUES ($1, $2, $3)`,
        [hashToken(token), userId, expiresAt]
    );

    cookies.set(SESSION_COOKIE, token, {
        httpOnly: true,
        secure: true,
        sameSite: 'lax',
        path: '/',
        expires: expiresAt
    });
}

export async function validateSession(token: string) {
    const { rows } = await pool.query(
        `SELECT u.id, u.email, u.display_name
         FROM sessions s
         JOIN users u ON u.id = s.user_id
         WHERE s.id = $1 AND s.expires_at > NOW()`,
        [hashToken(token)]
    );
    return rows[0] ?? null;
}

export async function deleteSession(token: string) {
    await pool.query(`DELETE FROM sessions WHERE id = $1`, [hashToken(token)]);
}