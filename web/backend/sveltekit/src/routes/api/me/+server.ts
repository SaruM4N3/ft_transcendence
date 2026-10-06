import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ locals }) => {
    if (!locals.user) {
        return json({ success: false, error: 'Not authenticated' }, { status: 401 });
    }
    return json(
        { success: true, user: locals.user },
        { headers: { 'Cache-Control': 'no-store' } }
    );
};