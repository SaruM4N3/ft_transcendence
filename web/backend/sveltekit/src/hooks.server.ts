import type { Handle } from '@sveltejs/kit';
import { SESSION_COOKIE, validateSession } from '$lib/server/session.js';

export const handle: Handle = async ({ event, resolve }) => {
    const token = event.cookies.get(SESSION_COOKIE);
    event.locals.user = token ? await validateSession(token) : null;

    if (token && !event.locals.user) {
        event.cookies.delete(SESSION_COOKIE, { path: '/' }); // stale cookie
    }
    return resolve(event);
};