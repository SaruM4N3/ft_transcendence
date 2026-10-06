import { json } from '@sveltejs/kit';
import { SESSION_COOKIE, deleteSession } from '$lib/server/session.js';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ cookies }) => {
    try {
        const token = cookies.get(SESSION_COOKIE);

        if (token) {
            await deleteSession(token);
        }

        cookies.delete(SESSION_COOKIE, { path: '/' });

        return json({ success: true });
    } catch (error) {
        console.error('Logout failed:', error);
        return json({ success: false, error: 'Logout failed' }, { status: 500 });
    }
};