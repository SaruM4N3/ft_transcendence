import type { Handle } from '@sveltejs/kit';
import { pool } from '$lib/server/db.js';

export const handle: Handle = async ({ event, resolve }) => {

    const sessionId = event.cookies.get('session');

    if (!sessionId) {
        event.locals.user = null;
        return resolve(event);
    }

    const sessionResult = await pool.query(
        `SELECT user_id
         FROM sessions
         WHERE id = $1
         AND expires_at > NOW()`,
        [sessionId]
    );

    if (sessionResult.rows.length === 0) {
        event.locals.user = null;
        return resolve(event);
    }

    const userId = sessionResult.rows[0].user_id;

    const userResult = await pool.query(
        `SELECT id, email, display_name
         FROM users
         WHERE id = $1`,
        [userId]
    );

    if (userResult.rows.length === 0) {
        event.locals.user = null;
        return resolve(event);
    }
    
    event.locals.user = userResult.rows[0];

    return resolve(event);
};